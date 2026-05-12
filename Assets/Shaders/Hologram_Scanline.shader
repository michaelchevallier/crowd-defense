// Hologram_Scanline shader — hero invul/respawn VFX
// Horizontal scanlines + flicker amplitude + URP/Unlit (V5 port)
Shader "CrowdDefense/Hologram_Scanline"
{
    Properties
    {
        _BaseColor          ("Base Color",            Color)        = (0.4,0.9,1.0,0.75)
        _ScanlineColor      ("Scanline Color",        Color)        = (1.0,1.0,1.0,1.0)
        _ScanlineDensity    ("Scanline Density",      Range(10,200))= 100
        _ScanlineSpeed      ("Scanline Speed",        Range(0,10))  = 30.0
        _ScanlineUVScale    ("Scanline UV Scale",     Range(10,300))= 100.0
        _FlickerSpeed       ("Flicker Speed",         Range(0,30))  = 8.0
        _FlickerAmplitude   ("Flicker Amplitude",     Range(0,1))   = 0.25
        _Alpha              ("Base Alpha",            Range(0,1))   = 0.7
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "HologramScanlineForward"
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
                float  _ScanlineUVScale;
                float  _FlickerSpeed;
                float  _FlickerAmplitude;
                float  _Alpha;
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
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv         = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Horizontal scanlines: sin(_Time.y * speed + UV.y * density)
                float scanCoord  = _Time.y * _ScanlineSpeed + i.uv.y * _ScanlineUVScale;
                float scanline   = sin(scanCoord) * 0.5 + 0.5;
                float scanMask   = smoothstep(0.35, 0.65, scanline);

                // Flicker: global amplitude modulation
                float flicker    = 1.0 - _FlickerAmplitude * (sin(_Time.y * _FlickerSpeed) * 0.5 + 0.5);

                half3 baseRGB    = _BaseColor.rgb;
                half3 scanRGB    = _ScanlineColor.rgb * scanMask * 0.4;
                half3 finalRGB   = (baseRGB + scanRGB) * flicker;

                float finalA     = (_Alpha * (1.0 - scanMask * 0.3)) * flicker;
                finalA           = saturate(finalA);

                return half4(finalRGB, finalA);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
