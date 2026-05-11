// Toon_Snow — tuile neige scintillante URP port
// Sparkle shimmer + glint wave + fresnel cool-blue + 3-step cel bands
Shader "CrowdDefense/Toon/Snow"
{
    Properties
    {
        _MainTex         ("Base Texture",       2D)           = "white" {}
        _NormalTex       ("Normal Map",         2D)           = "bump" {}
        _Tint            ("Snow Tint",          Color)        = (0.88,0.93,1.0,1)
        _NormalStrength  ("Normal Strength",    Range(0,2))   = 0.6
        _SparkleScale    ("Sparkle Scale",      Range(5,100)) = 30.0
        _SparkleSpeed    ("Sparkle Speed",      Range(0,10))  = 4.0
        _SparkleThreshold("Sparkle Threshold",  Range(0,1))   = 0.82
        _SparkleColor    ("Sparkle Color",      Color)        = (1,1,1,1)
        _SparkleStrength ("Sparkle Strength",   Range(0,2))   = 0.9
        _GlintFreq       ("Glint Frequency",    Range(0,10))  = 3.0
        _GlintAmp        ("Glint Amplitude",    Range(0,1))   = 0.25
        _FresnelPower    ("Fresnel Power",       Range(0.5,8)) = 3.0
        _FresnelColor    ("Fresnel Tint",        Color)        = (0.7,0.85,1.0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "SnowForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NormalTex_ST;
                half4  _Tint;
                float  _NormalStrength;
                float  _SparkleScale;
                float  _SparkleSpeed;
                float  _SparkleThreshold;
                half4  _SparkleColor;
                float  _SparkleStrength;
                float  _GlintFreq;
                float  _GlintAmp;
                float  _FresnelPower;
                half4  _FresnelColor;
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
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float sparkle(float2 uv, float t)
            {
                float2 cell    = floor(uv * _SparkleScale);
                float  h       = hash21(cell);
                float  flicker = sin(t * _SparkleSpeed * (0.5 + h) + h * 6.28318) * 0.5 + 0.5;
                return step(_SparkleThreshold, flicker * h);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs vpi = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(v.normalOS);
                o.positionCS = vpi.positionCS;
                o.positionWS = vpi.positionWS;
                o.normalWS   = vni.normalWS;
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Tint;

                half4  normalSample = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.uv);
                float3 tn = UnpackNormal(normalSample);
                tn = normalize(float3(tn.xy * _NormalStrength, tn.z));
                float3 normalWS = normalize(i.normalWS + tn.x * 0.1 + tn.y * 0.1);

                Light  mainLight = GetMainLight();
                float  nDotL     = max(0.0, dot(normalWS, mainLight.direction));
                float  lit       = nDotL * mainLight.distanceAttenuation;

                half3 band;
                if      (lit < 0.3)  band = half3(0.6, 0.65, 0.75);
                else if (lit < 0.65) band = half3(0.85, 0.88, 0.95);
                else                 band = half3(1.0, 1.0, 1.0);
                half4 color = texColor * half4(band, 1.0);

                float sp = sparkle(i.uv, _Time.y);
                color.rgb += _SparkleColor.rgb * sp * _SparkleStrength;

                float glint = sin(i.uv.x * _GlintFreq * 6.28318 + _Time.y * 2.0)
                            * sin(i.uv.y * _GlintFreq * 6.28318 + _Time.y * 1.7);
                glint = max(0.0, glint) * _GlintAmp;
                color.rgb += glint;

                float3 viewDir     = normalize(GetWorldSpaceViewDir(i.positionWS));
                float  fresnel     = 1.0 - saturate(dot(viewDir, normalWS));
                float  fresnelFactor = pow(fresnel, _FresnelPower);
                color.rgb = lerp(color.rgb, _FresnelColor.rgb, fresnelFactor * 0.4);

                return color;
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
