// BossHologram shader — scanlines moving + flicker alpha + cyan emission
// Scanline double-brightness: if (frac(uv.y * 50 + _Time.y * 3) < 0.1) emission *= 2
Shader "CrowdDefense/BossHologram"
{
    Properties
    {
        _BaseColor          ("Base Color",            Color)        = (0.0,0.8,1.0,0.6)
        _EmissionColor      ("Emission Cyan",         Color)        = (0.0,1.0,1.0,1.0)
        _EmissionStrength   ("Emission Strength",     Range(0,4))   = 1.8
        _ScanlineDensity    ("Scanline Density",      Range(10,200))= 50
        _ScanlineSpeed      ("Scanline Scroll Speed", Range(-10,10))= 3.0
        _ScanlineBrightBoost("Scanline Bright Boost", Range(1,4))   = 2.0
        _FlickerSpeed       ("Flicker Speed",         Range(0,20))  = 8.0
        _FlickerAmplitude   ("Flicker Amplitude",     Range(0,0.5)) = 0.15
        _GlitchAmount       ("Glitch Amount",         Range(0,0.2)) = 0.04
        _GlitchFreq         ("Glitch Frequency",      Range(0.1,30))= 8.0
        _RimPower           ("Rim Power",             Range(0.5,8)) = 3.0
        _Alpha              ("Base Alpha",            Range(0,1))   = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "BossHologramForward"
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
                half4  _EmissionColor;
                float  _EmissionStrength;
                float  _ScanlineDensity;
                float  _ScanlineSpeed;
                float  _ScanlineBrightBoost;
                float  _FlickerSpeed;
                float  _FlickerAmplitude;
                float  _GlitchAmount;
                float  _GlitchFreq;
                float  _RimPower;
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

            float hash11(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                // Horizontal glitch jitter on vertex bands
                float bandIdx = floor(v.positionOS.y * _GlitchFreq + _Time.y * 5.0);
                float jitter  = (hash11(bandIdx) - 0.5) * 2.0 * _GlitchAmount;
                float gate    = step(0.85, hash11(bandIdx + _Time.y));
                float3 pos    = v.positionOS.xyz;
                pos.x        += jitter * gate;

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
                // Moving scanlines
                float scanCoord = i.uv.y * _ScanlineDensity + _Time.y * _ScanlineSpeed;
                float scanPhase = frac(scanCoord);

                // Bright scanline band: double emission when phase < 0.1
                float isBrightLine = step(scanPhase, 0.1);
                float emissionMul  = lerp(1.0, _ScanlineBrightBoost, isBrightLine);

                // Soft scanline shading in other areas
                float scanSoft = (sin(scanCoord * 6.28318) + 1.0) * 0.5;
                float scanMask = 0.3 + scanSoft * 0.4;

                // Fresnel rim
                float3 viewDir  = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 normal   = normalize(i.normalWS);
                float  rim      = 1.0 - saturate(dot(viewDir, normal));
                float  rimFactor = pow(rim, _RimPower);

                // Flicker: pseudo-random alpha oscillation
                float flicker = sin(_Time.y * _FlickerSpeed) * sin(_Time.y * _FlickerSpeed * 2.3 + 1.7);
                float flickerA = 1.0 - _FlickerAmplitude * saturate(flicker * flicker);

                // Cyan emission base + scanline boost
                half3 emission = _EmissionColor.rgb * _EmissionStrength * emissionMul * scanMask;
                half3 baseRGB  = _BaseColor.rgb + emission + _EmissionColor.rgb * rimFactor;
                half3 finalRGB = saturate(baseRGB);

                float finalA   = _Alpha * flickerA + rimFactor * 0.35;
                finalA = saturate(finalA);

                return half4(finalRGB, finalA);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
