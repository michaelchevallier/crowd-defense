// BossJellyfish shader — animated vertex breathing + chromatic glow + transparency 0.4
// Vertex displacement: sin(_Time.y * 2 + vertex.x * 5) * 0.05
Shader "CrowdDefense/BossJellyfish"
{
    Properties
    {
        _BaseColor      ("Base Color",       Color)        = (0.2,0.8,1.0,0.4)
        _GlowColor      ("Chromatic Glow",   Color)        = (0.6,1.0,1.0,1.0)
        _GlowPower      ("Glow Rim Power",   Range(0.5,6)) = 2.0
        _GlowIntensity  ("Glow Intensity",   Range(0,3))   = 1.5
        _BreatheSpeed   ("Breathe Speed",    Range(0.1,5)) = 2.0
        _BreatheAmp     ("Breathe Amplitude",Range(0,0.2)) = 0.05
        _BreatheFreqX   ("Breathe Freq X",   Range(1,20))  = 5.0
        _PulseFreq      ("Pulse Frequency",  Range(0.1,5)) = 1.2
        _PulseStrength  ("Pulse Strength",   Range(0,1))   = 0.35
        _Alpha          ("Alpha",            Range(0,1))   = 0.4
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "BossJellyfishForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                half4  _GlowColor;
                float  _GlowPower;
                float  _GlowIntensity;
                float  _BreatheSpeed;
                float  _BreatheAmp;
                float  _BreatheFreqX;
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

            Varyings vert(Attributes v)
            {
                Varyings o;
                // Breathing vertex displacement — slow undulation along X/Z
                float3 pos  = v.positionOS.xyz;
                pos.y      += sin(_Time.y * _BreatheSpeed + pos.x * _BreatheFreqX) * _BreatheAmp;
                pos.y      += sin(_Time.y * _BreatheSpeed * 0.7 + pos.z * _BreatheFreqX * 0.8) * (_BreatheAmp * 0.5);

                VertexPositionInputs vpi = GetVertexPositionInputs(pos);
                VertexNormalInputs   vni = GetVertexNormalInputs(v.normalOS);
                o.positionCS = vpi.positionCS;
                o.positionWS = vpi.positionWS;
                o.normalWS   = vni.normalWS;
                o.uv         = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Bioluminescent pulse along Y
                float pulse      = (sin(_Time.y * _PulseFreq + i.uv.y * 6.28318) + 1.0) * 0.5;
                float pulseFactor = lerp(1.0, 1.0 + pulse, _PulseStrength);

                // Fresnel-based chromatic rim glow
                float3 viewDir  = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 normal   = normalize(i.normalWS);
                float  ndotv    = saturate(dot(viewDir, normal));
                float  rim      = 1.0 - ndotv;
                float  rimFactor = pow(rim, _GlowPower);

                // Chromatic aberration on rim: shift R/B channels slightly
                float rimR = pow(max(0, rim + 0.05), _GlowPower);
                float rimB = pow(max(0, rim - 0.05), _GlowPower);
                half3 chromaRim = half3(
                    _GlowColor.r * rimR,
                    _GlowColor.g * rimFactor,
                    _GlowColor.b * rimB
                ) * _GlowIntensity * _GlowColor.a;

                half3 baseRGB  = _BaseColor.rgb * pulseFactor;
                half3 finalRGB = baseRGB + chromaRim;

                float finalA   = _Alpha * (0.3 + rimFactor * 0.5 + pulse * 0.2);
                finalA = saturate(finalA);

                return half4(finalRGB, finalA);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
