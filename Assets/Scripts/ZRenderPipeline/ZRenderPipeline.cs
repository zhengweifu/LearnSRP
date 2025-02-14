using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZRP
{
    public class ZRenderPipeline : RenderPipeline
    {
        private RenderTexture _depthRT;
        private RenderTexture[] _gbufferRTs = new RenderTexture[4];
        private RenderTargetIdentifier[] _gbufferIDs = new RenderTargetIdentifier[4];

        private Matrix4x4 vpMatrix;
        private Matrix4x4 vpMatrixInv;
        private Matrix4x4 vpMatrixPrev; // 上一帧的 vp 矩阵
        private Matrix4x4 vpMatrixInvPrev;
        
        // 噪声图
        public Texture blueNoiseTex;
        
        // IBL 贴图
        private Cubemap diffuseIBL;
        private Cubemap specularIBL;
        private Texture brdfLut;
        private float intensityIBL;
        
        // 阴影管理
        public int shadowMapResolution = 1024;
        public float orthoDistance = 500.0f;
        public float lightSize = 2.0f;
        private ZCascadeShadowMap csm;
        private ZCascadeShadowSettings csmSettings;
        private RenderTexture[] shadowTextures = new RenderTexture[4];   // 阴影贴图
        private RenderTexture shadowMask;
        private RenderTexture shadowStrength;

        public ZRenderPipeline(ZRenderPipelineAsset rpAssect)
        {
            // 从RenderPipelineAsset中获取参数
            diffuseIBL = rpAssect.diffuseIBL;
            specularIBL = rpAssect.specularIBL;
            brdfLut = rpAssect.brdfLut;
            intensityIBL = rpAssect.intensityIBL;
            blueNoiseTex = rpAssect.blueNoiseTex;
            csmSettings = rpAssect.csmSettings;
            
            _depthRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth,
                RenderTextureReadWrite.Linear);
            _gbufferRTs[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            _gbufferRTs[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010,
                RenderTextureReadWrite.Linear);
            _gbufferRTs[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64,
                RenderTextureReadWrite.Linear);
            _gbufferRTs[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            for (int i = 0; i < 4; i++)
            {
                _gbufferIDs[i] = _gbufferRTs[i];
            }
            
            // 创建阴影贴图
            shadowMask = new RenderTexture(Screen.width/4, Screen.height/4, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            shadowStrength = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            for(int i=0; i<4; i++)
                shadowTextures[i] = new RenderTexture(shadowMapResolution, shadowMapResolution, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);

            csm = new ZCascadeShadowMap();
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                // 全局变量设置
                Shader.SetGlobalFloat("_far", camera.farClipPlane);
                Shader.SetGlobalFloat("_near", camera.nearClipPlane);
                Shader.SetGlobalFloat("_screenWidth", Screen.width);
                Shader.SetGlobalFloat("_screenHeight", Screen.height);
                Shader.SetGlobalTexture("_noiseTex", blueNoiseTex);
                Shader.SetGlobalFloat("_noiseTexResolution", blueNoiseTex.width);
                
                // gbuffer 和 depth 设置成全局可访问
                Shader.SetGlobalTexture("_sceneDepth", _depthRT);
                for (int i = 0; i < 4; i++)
                {
                    Shader.SetGlobalTexture("_gbuffer" + i, _gbufferRTs[i]);
                }

                // 设置相机矩阵
                Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
                Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
                vpMatrix = projMatrix * viewMatrix;
                vpMatrixInv = vpMatrix.inverse;
                Shader.SetGlobalMatrix("_vpMatrix", vpMatrix);
                Shader.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);
                Shader.SetGlobalMatrix("_vpMatrixPrev", vpMatrixPrev);
                Shader.SetGlobalMatrix("_vpMatrixInvPrev", vpMatrixInvPrev);
                
                // 设置 IBL 贴图
                Shader.SetGlobalTexture("_diffuseIBL", diffuseIBL);
                Shader.SetGlobalTexture("_specularIBL", specularIBL);
                Shader.SetGlobalTexture("_brdfLut", brdfLut);
                Shader.SetGlobalFloat("_intensityIBL", intensityIBL);
                
                // 设置 CSM 相关参数
                Shader.SetGlobalFloat("_orthoDistance", orthoDistance);
                Shader.SetGlobalFloat("_shadowMapResolution", shadowMapResolution);
                Shader.SetGlobalFloat("_lightSize", lightSize);
                Shader.SetGlobalTexture("_shadowStrength", shadowStrength);
                Shader.SetGlobalTexture("_shadoMask", shadowMask);
                for(int i=0; i<4; i++)
                {
                    Shader.SetGlobalTexture("_shadowtex"+i, shadowTextures[i]);
                    Shader.SetGlobalFloat("_split"+i, csm.splts[i]);
                }

                // Shadow Casting pass
                RenderShadowCastingPass(context, camera);

                // GBuffer Pass
                RenderBasePass(context, camera);
                
                // Shadow Mapping Pass
                RenderShadowMappingPass(context, camera);

                // Lighting Pass
                RenderLightingPass(context, camera);

                //设置天空盒
                context.DrawSkybox(camera);

                //绘制gizmo
                if (Handles.ShouldRenderGizmos())
                {
                    context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                    context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
                }

                // 提交绘制命令
                context.Submit();
            }
        }
        
        // 阴影贴图 pass
        private void RenderShadowCastingPass(ScriptableRenderContext context, Camera camera)
        {
            // 获取光源信息
            Light light = RenderSettings.sun;
            Vector3 lightDir = light.transform.rotation * Vector3.forward;

            // 更新 shadowmap 分割
            csm.Update(camera, lightDir, csmSettings);
            csmSettings.Set();

            csm.SaveMainCameraSettings(ref camera);
            for(int level=0; level<4; level++)
            {
                // 将相机移到光源方向
                csm.ConfigCameraToShadowSpace(ref camera, lightDir, level, orthoDistance, shadowMapResolution);

                // 设置阴影矩阵, 视锥分割参数
                Matrix4x4 v = camera.worldToCameraMatrix;
                Matrix4x4 p = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
                Shader.SetGlobalMatrix("_shadowVpMatrix" + level, p * v);
                Shader.SetGlobalFloat("_orthoWidth" + level, csm.orthoWidths[level]);

                CommandBuffer cmd = new CommandBuffer();
                cmd.name = "ShadowCasting" + level;

                // 绘制前准备
                context.SetupCameraProperties(camera);
                cmd.SetRenderTarget(shadowTextures[level]);
                cmd.ClearRenderTarget(true, true, Color.clear);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                // 剔除
                camera.TryGetCullingParameters(out var cullingParameters);
                var cullingResults = context.Cull(ref cullingParameters);
                // config settings
                ShaderTagId shaderTagId = new ShaderTagId("ZDepthOnly");
                SortingSettings sortingSettings = new SortingSettings(camera);
                DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
                FilteringSettings filteringSettings = FilteringSettings.defaultValue;

                // 绘制
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
                context.Submit();   // 每次 set camera 之后立即提交
            }
            csm.RevertMainCameraSettings(ref camera);
        }

        private void RenderBasePass(ScriptableRenderContext context, Camera camera)
        {
            // 绑定相机属性
            context.SetupCameraProperties(camera);

            // 创建GBuffer CommandBuffer
            var cmd = new CommandBuffer();
            cmd.name = "GBuffer";

            cmd.SetRenderTarget(_gbufferIDs, _depthRT);

            // 清屏
            cmd.ClearRenderTarget(true, true, Color.black);
            context.ExecuteCommandBuffer(cmd);

            // 相机裁剪，返回可绘制的对象
            camera.TryGetCullingParameters(out var parameters);
            var results = context.Cull(ref parameters);

            // 绘制不透明物体
            {
                ShaderTagId tagId = new ShaderTagId("ZGBuffer");
                SortingSettings ss = new SortingSettings(camera);
                DrawingSettings ds = new DrawingSettings(tagId, ss);

                FilteringSettings fs = new FilteringSettings(RenderQueueRange.opaque);

                context.DrawRenderers(results, ref ds, ref fs);
            }
            context.Submit();
        }

        // 阴影计算 pass : 输出阴影强度 texture
        private void RenderShadowMappingPass(ScriptableRenderContext context, Camera camera)
        {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "ShadowMappingPass";

            RenderTexture tempTex1 = RenderTexture.GetTemporary(Screen.width/4, Screen.height/4, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            RenderTexture tempTex2 = RenderTexture.GetTemporary(Screen.width/4, Screen.height/4, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            RenderTexture tempTex3 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);

            if(csmSettings.usingShadowMask)
            {
                // 生成 Mask, 模糊 Mask
                cmd.Blit(_gbufferIDs[0], tempTex1, new Material(Shader.Find("ZRP/ZPreShadowMapping")));
                cmd.Blit(tempTex1, tempTex2, new Material(Shader.Find("ZRP/ZBlurNx1")));
                cmd.Blit(tempTex2, shadowMask, new Material(Shader.Find("ZRP/ZBlur1xN")));
            }    

            // 生成阴影, 模糊阴影
            cmd.Blit(_gbufferIDs[0], tempTex3, new Material(Shader.Find("ZRP/ZShadowMapping")));
            cmd.Blit(tempTex3, shadowStrength, new Material(Shader.Find("ZRP/ZBlurNxN")));
        
            RenderTexture.ReleaseTemporary(tempTex1);
            RenderTexture.ReleaseTemporary(tempTex2);
            RenderTexture.ReleaseTemporary(tempTex3);

            context.ExecuteCommandBuffer(cmd);
            context.Submit();
        }
        private void RenderLightingPass(ScriptableRenderContext context, Camera camera)
        {
            var cmd = new CommandBuffer();
            cmd.name = "DeferredLighting";
            Material mat = new Material(Shader.Find("ZRP/ZDeferredLighting"));
            cmd.Blit(_gbufferIDs[0], BuiltinRenderTextureType.CameraTarget, mat);
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
        }
    }
}
