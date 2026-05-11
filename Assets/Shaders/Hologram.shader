// Hologram shader URP port — boss_espace_entite (cosmic)
// Scanlines + glitch jitter + fresnel rim + transparency
Shader "CrowdDefense/Hologram"
{
    Properties
    {
        _BaseColor          ("Base Color",           Color)        = (0.5,0.8,1.0,0.7)
        _ScanlineColor      ("Scanline Color",       Color)        = (1.0,1.0,1.0,1.0)
        _ScanlineDensity    ("Scanline Density",     Range(10,400))= 80
        _ScanlineSpeed      ("Scanline Scroll Speed",Range(-5,5))  = 1.5
        _ScanlineIntensity  ("Scanline Intensity",   Range(0,1))   = 0.45
        _GlitchAmount       ("Glitch Amount",        Range(0,0.2)) = 0.04
        _GlitchFreq         ("Glitch Frequency",     Range(0.1,30))= 8
        _RimColor           ("Rim Glow",             Color)        = (0.5,1.0,1.0,1.0)
        _RimPower           ("Rim Power",            Range(0.5,8)) = 3.0
        _Alpha              ("Base Alpha",           Range(0,1))   = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "HologramForward"
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
                half4  _ScanlineColor;
                float  _ScanlineDensity;
                float  _ScanlineSpeed;
                float  _ScanlineIntensity;
                float  _GlitchAmount;
                float  _GlitchFreq;
                half4  _RimColor;
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
                float  glitchY    : TEXCOORD3;
            };

            float hash11(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
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
                o.glitchY    = bandIdx;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float scanCoord = i.uv.y * _ScanlineDensity + _Time.y * _ScanlineSpeed;
                float scanline  = (sin(scanCoord * 6.28318) + 1.0) * 0.5;
                float scanMask  = smoothstep(0.4, 0.6, scanline) * _ScanlineIntensity;

                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 normal  = normalize(i.normalWS);
                float  rim     = 1.0 - saturate(dot(viewDir, normal));
                float  rimFactor = pow(rim, _RimPower);

                half3 baseRGB  = _BaseColor.rgb;
                half3 scanRGB  = _ScanlineColor.rgb * scanMask;
                half3 rimRGB   = _RimColor.rgb * rimFactor * _RimColor.a;
                half3 finalRGB = baseRGB + scanRGB + rimRGB;

                float finalA = _Alpha * _BaseColor.a + rimFactor * 0.35 + scanMask * 0.2;
                finalA = saturate(finalA);

                return half4(finalRGB, finalA);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
