// Toon_Lava — tuile lave animée URP port
// UV scroll + glow pulse + procedural crack noise
Shader "CrowdDefense/Toon/Lava"
{
    Properties
    {
        _MainTex         ("Base Texture",      2D)           = "white" {}
        _Tint            ("Lava Tint",         Color)        = (1,0.627,0.314,1)
        _ScrollSpeedX    ("Scroll Speed X",    Range(-2,2))  = 0.08
        _ScrollSpeedY    ("Scroll Speed Y",    Range(-2,2))  = 0.04
        _GlowColor       ("Glow Color",        Color)        = (1,0.4,0,1)
        _GlowPulseFreq   ("Glow Pulse Freq",   Range(0.1,5)) = 1.2
        _GlowPulseAmp    ("Glow Pulse Amp",    Range(0,2))   = 0.6
        _GlowBase        ("Glow Base",         Range(0,2))   = 0.8
        _CrackScale      ("Crack Scale",       Range(1,30))  = 8.0
        _CrackContrast   ("Crack Contrast",    Range(0,2))   = 1.4
        _EmissionStrength("Emission Strength", Range(0,4))   = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "LavaForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Tint;
                float  _ScrollSpeedX;
                float  _ScrollSpeedY;
                half4  _GlowColor;
                float  _GlowPulseFreq;
                float  _GlowPulseAmp;
                float  _GlowBase;
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
                float2 uv = i.uv;
                uv.x += _Time.y * _ScrollSpeedX;
                uv.y += _Time.y * _ScrollSpeedY;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Tint;

                float n1    = valueNoise(i.uv * _CrackScale);
                float n2    = valueNoise(i.uv * (_CrackScale * 0.5) + float2(1.7, 3.3));
                float crack = pow(n1 * n2, _CrackContrast);
                half3 crustColor = _Tint.rgb * 0.25;
                col.rgb = lerp(crustColor, col.rgb, crack);

                float pulse  = _GlowBase + sin(_Time.y * _GlowPulseFreq) * _GlowPulseAmp;
                pulse        = max(0.0, pulse);
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
