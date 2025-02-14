Shader "ZRP/VXGI/Voxelization"
{
    SubShader
    {
        Pass
        {
            Name "VOXELIZATION"
            Tags { "LightMode"="Voxelization" }
            CGPROGRAM

            #include "UnityCG.cginc"
            #include "../../ShaderLibrary/Structs/ZVoxelData.cginc"

            ENDCG
        }
    }
}