// Jellyfish shader URP port — boss_submarin_kraken
// Scrolling UV + fresnel rim glow + noise-based bioluminescent pulse
Shader "CrowdDefense/Jellyfish"
{
    Properties
    {
        _BaseColor     ("Base Color",        Color)       = (0.2,0.8,1.0,0.6)
        _RimColor      ("Rim Glow Color",    Color)       = (0.4,1.0,1.0,1.0)
        _RimPower      ("Rim Power",         Range(0.5,8.0)) = 2.5
        _ScrollSpeedU  ("Scroll Speed U",    Range(-2,2))  = 0.15
        _ScrollSpeedV  ("Scroll Speed V",    Range(-2,2))  = 0.4
        _NoiseScale    ("Noise Scale",       Range(0.5,20))= 4.0
        _PulseFreq     ("Pulse Frequency",   Range(0.1,5)) = 1.2
        _PulseStrength ("Pulse Strength",    Range(0,1))   = 0.35
        _Alpha         ("Alpha",             Range(0,1))   = 0.65
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "JellyfishForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                half4  _RimColor;
                float  _RimPower;
                float  _ScrollSpeedU;
                float  _ScrollSpeedV;
                float  _NoiseScale;
                float  _PulseFreq;
                float  _PulseStrength;
                float  _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs vpi = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(v.normalOS);
                o.positionCS = vpi.positionCS;
                o.positionWS = vpi.positionWS;
                o.normalWS   = vni.normalWS;
                o.uv         = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 scrolledUV = i.uv;
                scrolledUV.x += _Time.y * _ScrollSpeedU;
                scrolledUV.y += _Time.y * _ScrollSpeedV;

                float n = valueNoise(scrolledUV * _NoiseScale);

                float pulse       = (sin(_Time.y * _PulseFreq + i.uv.y * 6.28318) + 1.0) * 0.5;
                float pulseFactor = lerp(1.0, 1.0 + pulse, _PulseStrength);

                float3 viewDir   = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 normal    = normalize(i.normalWS);
                float  rim       = 1.0 - saturate(dot(viewDir, normal));
                float  rimFactor = pow(rim, _RimPower);

                half3 baseRGB  = _BaseColor.rgb * (0.6 + n * 0.5) * pulseFactor;
                half3 rimRGB   = _RimColor.rgb * rimFactor * _RimColor.a;
                half3 finalRGB = baseRGB + rimRGB;
                float finalA   = _Alpha * (_BaseColor.a * 0.4 + rimFactor * 0.6);

                return half4(finalRGB, finalA);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
