Shader "Skybox/White Horizon Gradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.72, 0.86, 0.95, 1)
        _HorizonColor ("Horizon Color", Color) = (1, 1, 1, 1)
        _Exponent ("Upper Blend", Float) = 0.55
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half4 _TopColor;
            half4 _HorizonColor;
            half _Exponent;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float height = normalize(i.texcoord).y;
                float t = pow(saturate(height), _Exponent);
                return lerp(_HorizonColor, _TopColor, t);
            }
            ENDCG
        }
    }
}
