// Kelp shader URP port
// Vertex bend proportionnel à UV.y^2 + gradient root → tip
Shader "CrowdDefense/Kelp"
{
    Properties
    {
        _BaseColor  ("Base Color",     Color)        = (0.18,0.49,0.20,1)
        _BendAmp    ("Bend Amplitude", Range(0,0.5)) = 0.15
        _BendFreqX  ("Bend Freq X",    Range(0,5))   = 1.2
        _BendFreqZ  ("Bend Freq Z",    Range(0,5))   = 0.9
        _BendSpeed  ("Bend Speed",     Range(0,5))   = 1.5
        _TipBright  ("Tip Brightness", Range(1,2))   = 1.2
        _RootDark   ("Root Darkness",  Range(0,1))   = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "KelpForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                float  _BendAmp;
                float  _BendFreqX;
                float  _BendFreqZ;
                float  _BendSpeed;
                float  _TipBright;
                float  _RootDark;
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
                float3 pos  = v.positionOS.xyz;
                float  bend = v.uv.y * v.uv.y;
                pos.x += sin(_Time.y * _BendSpeed * _BendFreqX + pos.y * 1.5) * _BendAmp * bend;
                pos.z += cos(_Time.y * _BendSpeed * _BendFreqZ + pos.y * 1.5) * _BendAmp * bend;
                o.positionCS = TransformObjectToHClip(pos);
                o.uv         = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 col = lerp(_BaseColor.rgb * _RootDark, _BaseColor.rgb * _TipBright, i.uv.y);
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
