// Toon_Lava_Animated — diagonal UV scroll + NoiseTex + orange emission flicker (V4-parity)
// FlowSpeed drives diagonal: uv += float2(speed, speed*0.6) * Time
// NoiseTex adds organic lava-flow variation; sin-time flicker on emission
Shader "CrowdDefense/Toon/Lava_Animated"
{
    Properties
    {
        _MainTex         ("Base Texture",      2D)           = "white" {}
        _NoiseTex        ("Noise Texture",     2D)           = "gray"  {}
        _Tint            ("Lava Tint",         Color)        = (1,0.627,0.314,1)
        _FlowSpeed       ("Flow Speed",        Range(0,2))   = 0.12
        _NoiseStrength   ("Noise Strength",    Range(0,1))   = 0.45
        _GlowColor       ("Glow Color",        Color)        = (1,0.4,0,1)
        _GlowPulseFreq   ("Glow Pulse Freq",   Range(0.1,5)) = 1.2
        _GlowPulseAmp    ("Glow Pulse Amp",    Range(0,2))   = 0.6
        _GlowBase        ("Glow Base",         Range(0,2))   = 0.8
        _FlickerSpeed    ("Flicker Speed",     Range(0,20))  = 7.0
        _FlickerAmp      ("Flicker Amp",       Range(0,1))   = 0.15
        _CrackScale      ("Crack Scale",       Range(1,30))  = 8.0
        _CrackContrast   ("Crack Contrast",    Range(0,2))   = 1.4
        _EmissionStrength("Emission Strength", Range(0,4))   = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "LavaAnimatedForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4  _Tint;
                float  _FlowSpeed;
                float  _NoiseStrength;
                half4  _GlowColor;
                float  _GlowPulseFreq;
                float  _GlowPulseAmp;
                float  _GlowBase;
                float  _FlickerSpeed;
                float  _FlickerAmp;
                float  _CrackScale;
                float  _CrackContrast;
                float  _EmissionStrength;
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

            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
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
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Diagonal UV flow (V4-style)
                float2 flowUV = i.uv + float2(_FlowSpeed, _FlowSpeed * 0.6) * _Time.y;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, flowUV) * _Tint;

                // NoiseTex at counter-diagonal — organic slow-churn look
                float2 noiseUV  = i.uv + float2(-_FlowSpeed * 0.5, _FlowSpeed * 0.3) * _Time.y;
                half   noiseVal = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // Procedural crack (unchanged from Toon_Lava)
                float n1    = valueNoise(i.uv * _CrackScale);
                float n2    = valueNoise(i.uv * (_CrackScale * 0.5) + float2(1.7, 3.3));
                float crack = pow(n1 * n2, _CrackContrast);

                // Blend noise into crack mask for organic variation
                crack = lerp(crack, crack * noiseVal, _NoiseStrength);

                half3 crustColor = _Tint.rgb * 0.25;
                col.rgb = lerp(crustColor, col.rgb, crack);

                // Glow pulse + high-freq flicker (V4 ember-like)
                float pulse   = _GlowBase + sin(_Time.y * _GlowPulseFreq) * _GlowPulseAmp;
                float flicker = 1.0 + sin(_Time.y * _FlickerSpeed + i.uv.x * 23.7 + i.uv.y * 17.3) * _FlickerAmp;
                pulse         = max(0.0, pulse) * flicker;
                float hotMask = crack * pulse;
                col.rgb += _GlowColor.rgb * hotMask * _EmissionStrength;

                return col;
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
