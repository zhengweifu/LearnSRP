using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZRP
{
    [System.Serializable]
    public class ZCascadeShadowSettings
    {
        public float maxDistance = 200;
        public bool usingShadowMask = false;
        public ZShadowSettings level0;
        public ZShadowSettings level1;
        public ZShadowSettings level2;
        public ZShadowSettings level3;
        public void Set()
        {
            ZShadowSettings[] levels = {level0, level1, level2, level3};
            for(int i=0; i<4; i++)
            {
                Shader.SetGlobalFloat("_shadingPointNormalBias" + i, levels[i].shadingPointNormalBias);
                Shader.SetGlobalFloat("_depthNormalBias" + i, levels[i].depthNormalBias);
                Shader.SetGlobalFloat("_pcssSearchRadius" + i, levels[i].pcssSearchRadius);
                Shader.SetGlobalFloat("_pcssFilterRadius" + i, levels[i].pcssFilterRadius);
            }
            Shader.SetGlobalFloat("_usingShadowMask", usingShadowMask ? 1.0f : 0.0f);
            Shader.SetGlobalFloat("_csmMaxDistance", maxDistance);
        }
    }
}