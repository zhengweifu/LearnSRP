using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZRP
{
    [CreateAssetMenu(menuName = "Rendering/ZRenderPipelineAsset")]
    public class ZRenderPipelineAsset : RenderPipelineAsset
    {
        // IBL 参数
        public Cubemap diffuseIBL;
        public Cubemap specularIBL;
        public Texture brdfLut;
        public Texture blueNoiseTex;
        public float intensityIBL;
           
        // 级联阴影
        [SerializeField] 
        public ZCascadeShadowSettings csmSettings;
        protected override RenderPipeline CreatePipeline()
        {
            ZRenderPipeline zrp = new ZRenderPipeline(this);
            return zrp;
        }
    }
}
