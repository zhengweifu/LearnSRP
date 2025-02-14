Shader "ZRP/DepthOnly"
{
    SubShader
    {
        Pass
        {
            name "DEPTH_ONLY"
            Tags { "LightMode"="DepthOnly" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 depth : TEXCOORD0;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depth = o.vertex.zw;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float d = i.depth.x / i.depth.y;
            #if defined (UNITY_REVERSED_Z)
                d = 1.0 - d;
            #endif
                fixed4 c = EncodeFloatRGBA(d);
                return c;
            }
            ENDCG 
        }
    }
}
