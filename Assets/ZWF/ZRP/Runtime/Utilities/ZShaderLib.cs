using UnityEngine;

namespace ZRP
{
    internal static class ZShaderLib
    {
        // 阴影映射相关着色器
        internal static readonly Shader PreShadowMapping = Shader.Find("ZRP/PreShadowMapping");
        internal static readonly Shader ShadowMapping = Shader.Find("ZRP/ShadowMapping");
        internal static readonly Shader Blur1xN = Shader.Find("ZRP/Blur1xN");
        internal static readonly Shader BlurNx1 = Shader.Find("ZRP/BlurNx1");
        internal static readonly Shader BlurNxN = Shader.Find("ZRP/BlurNxN");
        
        // 延迟光照相关着色器
        internal static readonly Shader DeferredLighting = Shader.Find("ZRP/DeferredLighting");
    }
}