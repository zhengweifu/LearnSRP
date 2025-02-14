Shader "ZRP/DeferredBase"
{
    SubShader
    {
        Pass
        {
            name "DEFERRED_BASE"
            Tags { "LightMode"="DeferredBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal: NORMAL;
            };

            sampler2D _Diffuse_Tex;
            sampler2D _Metallic_Roughness_Tex;
            sampler2D _Emission_Tex;
            sampler2D _Normal_Tex;
            sampler2D _Occlusion_Tex;
            float4 _Diffuse_Tex_ST;

            float _Metallic;
            float _Roughness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Diffuse_Tex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            void frag (v2f i,
                out float4 RT0: SV_Target0, // diffuse
                out float4 RT1: SV_Target1, // normal
                out float4 RT2: SV_Target2, // rg is motion vector, b is roughness value, a is metallic value
                out float4 RT3: SV_Target3) // rgb is emission, a is occlusion
            {
                // sample the texture
                float4 color = tex2D(_Diffuse_Tex, i.uv);
                float4 metallic_roughness = tex2D(_Metallic_Roughness_Tex, i.uv);
                float4 emission = tex2D(_Emission_Tex, i.uv);
                float4 normal = tex2D(_Normal_Tex, i.uv);
                float occlusion = tex2D(_Occlusion_Tex, i.uv);
                float metallic = _Metallic * metallic_roughness.g;
                float roughness = _Roughness * metallic_roughness.r;

                RT0 = float4(color.rgb, 1.0);
                RT1 = float4(i.normal * 0.5 + 0.5, 0);
                RT2 = float4(1, 1, roughness, metallic);
                RT3 = float4(emission.rgb, occlusion);
            }
            ENDCG
        }
    }
}
