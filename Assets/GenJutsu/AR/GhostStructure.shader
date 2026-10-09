Shader "GenJutsu/GhostStructure"
{
    Properties
    {
        _BaseTint("Base Tint", Color) = (1, 1, 1, 1)
        _GhostTint("Ghost Tint", Color) = (0.3, 0.8, 1, 1)
        _SolidMix("Solid Mix", Range(0, 1)) = 0
        _RimColor("Rim Color", Color) = (0.55, 0.95, 1, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 3
        _FillAlpha("Fill Alpha", Range(0, 1)) = 0.08
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseTint;
                half4 _GhostTint;
                half _SolidMix;
                half4 _RimColor;
                half _RimPower;
                half _FillAlpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = GetWorldSpaceViewDir(input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirN = normalize(viewDir);
                if (dot(normalWS, viewDirN) < 0)
                    normalWS = -normalWS;

                half fres = pow(1.0h - saturate(dot(normalWS, viewDirN)), _RimPower);
                half rimA = fres * (1.0h - _SolidMix);
                half3 tint = lerp(_GhostTint.rgb, _BaseTint.rgb, _SolidMix);
                half3 color = tint + _RimColor.rgb * rimA;
                half alpha = saturate(lerp(_FillAlpha, 1.0h, _SolidMix) + rimA * 0.6h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
