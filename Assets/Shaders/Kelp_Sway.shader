// Kelp_Sway shader — underwater W6 decor
// Vertex displacement sin(_Time.y + worldPos.x * 0.3) * UV.y * _SwayAmp (V5 port)
Shader "CrowdDefense/Kelp_Sway"
{
    Properties
    {
        _BaseColor  ("Base Color",     Color)        = (0.15,0.55,0.22,1)
        _TipColor   ("Tip Color",      Color)        = (0.25,0.80,0.35,1)
        _SwayAmp    ("Sway Amplitude", Range(0,1))   = 0.18
        _SwaySpeed  ("Sway Speed",     Range(0,5))   = 1.2
        _SwayFreqX  ("Sway Freq X",    Range(0,5))   = 0.3
        _SwayFreqZ  ("Sway Freq Z",    Range(0,5))   = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "KelpSwayForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                half4  _TipColor;
                float  _SwayAmp;
                float  _SwaySpeed;
                float  _SwayFreqX;
                float  _SwayFreqZ;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posOS   = v.positionOS.xyz;
                float3 posWS   = TransformObjectToWorld(posOS);

                // Vertex displacement: sin(_Time.y + worldPos.x * 0.3) * UV.y * _SwayAmp
                float swayMask = v.uv.y;  // root anchored, tip free
                posWS.x += sin(_Time.y * _SwaySpeed + posWS.x * _SwayFreqX) * swayMask * _SwayAmp;
                posWS.z += cos(_Time.y * _SwaySpeed + posWS.x * _SwayFreqZ) * swayMask * _SwayAmp * 0.6;

                o.positionCS = TransformWorldToHClip(posWS);
                o.uv         = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 col = lerp(_BaseColor.rgb, _TipColor.rgb, i.uv.y);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            Cull Back
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_shadowcaster
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
