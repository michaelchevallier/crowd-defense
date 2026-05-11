// Toon_Water — tuile eau animée URP port
// UV scroll + sin wave vertex displacement + caustics + edge foam
Shader "CrowdDefense/Toon/Water"
{
    Properties
    {
        _MainTex        ("Base Texture",    2D)           = "white" {}
        _Tint           ("Water Tint",      Color)        = (0.373,0.659,0.816,1)
        _ScrollSpeedX   ("Scroll Speed X",  Range(-2,2))  = 0.12
        _ScrollSpeedY   ("Scroll Speed Y",  Range(-2,2))  = 0.06
        _WaveAmpX       ("Wave Amp X",      Range(0,0.1)) = 0.012
        _WaveAmpY       ("Wave Amp Y",      Range(0,0.1)) = 0.010
        _WaveFreqX      ("Wave Freq X",     Range(0,20))  = 8.0
        _WaveFreqY      ("Wave Freq Y",     Range(0,20))  = 6.0
        _WaveSpeedX     ("Wave Speed X",    Range(0,5))   = 0.8
        _WaveSpeedY     ("Wave Speed Y",    Range(0,5))   = 0.6
        _CausticScale   ("Caustic Scale",   Range(5,50))  = 22.0
        _CausticStrength("Caustic Strength",Range(0,0.3)) = 0.10
        _FoamWidth      ("Foam Edge Width", Range(0,0.1)) = 0.025
        _FoamColor      ("Foam Color",      Color)        = (0.95,0.98,1.0,1)
        _FoamStrength   ("Foam Strength",   Range(0,0.5)) = 0.18
        _VertWaveAmp    ("Vertex Wave Amp", Range(0,0.2)) = 0.05
        _VertWaveFreq   ("Vert Wave Freq",  Range(0,5))   = 2.0
        _VertWaveSpeed  ("Vert Wave Speed", Range(0,5))   = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "WaterForward"
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
                float  _WaveAmpX;
                float  _WaveAmpY;
                float  _WaveFreqX;
                float  _WaveFreqY;
                float  _WaveSpeedX;
                float  _WaveSpeedY;
                float  _CausticScale;
                float  _CausticStrength;
                float  _FoamWidth;
                half4  _FoamColor;
                float  _FoamStrength;
                float  _VertWaveAmp;
                float  _VertWaveFreq;
                float  _VertWaveSpeed;
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
                float3 pos = v.positionOS.xyz;
                float wave = sin(pos.x * _VertWaveFreq + _Time.y * _VertWaveSpeed) * _VertWaveAmp
                           + cos(pos.z * (_VertWaveFreq * 1.25) + _Time.y * (_VertWaveSpeed * 0.8)) * _VertWaveAmp;
                pos.y += wave;
                o.positionCS = TransformObjectToHClip(pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                uv.x += sin(i.uv.y * _WaveFreqX + _Time.y * _WaveSpeedX) * _WaveAmpX;
                uv.y += cos(i.uv.x * _WaveFreqY + _Time.y * _WaveSpeedY) * _WaveAmpY;
                uv.x += _Time.y * _ScrollSpeedX;
                uv.y += _Time.y * _ScrollSpeedY;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Tint;

                float c1 = sin(i.uv.x * _CausticScale + _Time.y * 0.9)
                         * sin(i.uv.y * _CausticScale + _Time.y * 0.7);
                float c2 = sin(i.uv.x * (_CausticScale * 1.6) - _Time.y * 1.4)
                         * sin(i.uv.y * (_CausticScale * 1.36) + _Time.y * 1.1);
                float caustic = max(0.0, c1) * _CausticStrength + max(0.0, c2) * (_CausticStrength * 0.6);
                col.rgb += caustic;

                float edgeDist    = min(min(i.uv.x, 1.0 - i.uv.x), min(i.uv.y, 1.0 - i.uv.y));
                float foamMask    = 1.0 - smoothstep(0.0, _FoamWidth, edgeDist);
                float foamShimmer = 0.6 + 0.4 * sin(_Time.y * 3.0 + i.uv.x * 28.0 + i.uv.y * 22.0);
                col.rgb = lerp(col.rgb, _FoamColor.rgb, foamMask * foamShimmer * _FoamStrength);

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
