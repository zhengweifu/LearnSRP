using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZRP
{
    [System.Serializable]
    public class ZShadowSettings
    {
        // 着色点法线偏移
        public float shadingPointNormalBias = 0.1f;
        // 阴影深度法线偏移
        public float depthNormalBias = 0.005f;
        // pcss搜索半径
        public float pcssSearchRadius = 1.0f;
        // pcss过滤半径
        public float pcssFilterRadius = 7.0f;
    }
}