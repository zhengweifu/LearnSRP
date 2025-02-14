Shader "ZRP/Standard"
{
    Properties
    {
        _Diffuse_Tex ("Diffuse Map", 2D) = "white" {}
        _Metallic_Roughness_Tex ("Metallic Roughness Map", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Roughness ("Roughness", Range(0, 1)) = 0.5
        _Emission_Tex ("Emission Map", 2D) = "black" {}
        _Normal_Tex("Normal Map", 2D) = "bump" {}
         _Occlusion_Tex ("Occlusion Map", 2D) = "white" {}
    }
    SubShader
    {
        UsePass "ZRP/DepthOnly/DEPTH_ONLY"
        UsePass "ZRP/DeferredBase/DEFERRED_BASE"
//        UsePass "ZRP/VXGI/Voxelization/VOXELIZATION"
    }
}
