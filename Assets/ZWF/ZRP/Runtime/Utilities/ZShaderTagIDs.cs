using UnityEngine.Rendering;

namespace ZRP
{
    internal static class ZShaderTagIDs
    {
        internal static readonly ShaderTagId DepthOnly = new ShaderTagId("DepthOnly");
        internal static readonly ShaderTagId DeferredBase = new ShaderTagId("DeferredBase");
        internal static readonly ShaderTagId Voxelization = new ShaderTagId("Voxelization");
    }
}