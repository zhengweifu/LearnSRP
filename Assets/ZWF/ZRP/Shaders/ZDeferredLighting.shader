Shader "ZRP/ZDeferredLighting"
{
    Properties
    {
        _Diffuse_Tex ("Diffuse Map", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite On ZTest Always
        LOD 100

        Pass
        {
            Tags {"LightMode"="DeferredLighting"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "ZBRDF.cginc"
            #include "ZGlobalUniform.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i, out float depthOut : SV_Depth) : SV_Target
            {
                float4 gbuffer0 = tex2D(_gbuffer0, i.uv);
                float4 gbuffer1 = tex2D(_gbuffer1, i.uv);
                float4 gbuffer2 = tex2D(_gbuffer2, i.uv);
                float4 gbuffer3 = tex2D(_gbuffer3, i.uv);

                // 从 Gbuffer 解码数据
                float3 albedo = gbuffer0.rgb;
                float3 normal = gbuffer1.rgb * 2 - 1;
                float2 motionVector = gbuffer2.rg;
                float roughness = gbuffer2.b;
                float metallic = gbuffer2.a;
                float3 emission = gbuffer3.rgb;
                float occlusion = gbuffer3.a;

                float d = UNITY_SAMPLE_DEPTH(tex2D(_sceneDepth, i.uv));
                float d_lin = Linear01Depth(d);
                depthOut = d;
                
                // 反投影重建世界坐标
                float4 ndcPos = float4(i.uv * 2 - 1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

                // 计算参数
                float3 color = float3(0, 0, 0);
                float3 N = normalize(normal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                float3 radiance = _LightColor0.rgb;

                // 计算直接光照
                float3 direct = ZRP_PBR(N, V, L, albedo, radiance, roughness, metallic);

                // 计算IBL光照
                float3 ibl = ZRP_IBL(N, V, albedo, roughness, metallic, _diffuseIBL, _specularIBL,  _brdfLut);

                color += ibl * _intensityIBL * occlusion;
                color += emission;

                // 计算阴影
                float shadow = tex2D(_shadowStrength, i.uv).r;
                color += direct * shadow;

                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}