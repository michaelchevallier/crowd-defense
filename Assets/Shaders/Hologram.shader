// Hologram shader — overlay pour boss_espace_entite (cosmic)
// Scanlines horizontales + glitch jitter + fresnel rim + transparency pour effet "hologram corrupted"
Shader "CrowdDefense/Hologram"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.5,0.8,1.0,0.7)
        _ScanlineColor ("Scanline Color", Color) = (1.0,1.0,1.0,1.0)
        _ScanlineDensity ("Scanline Density", Range(10,400)) = 80
        _ScanlineSpeed ("Scanline Scroll Speed", Range(-5,5)) = 1.5
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.45
        _GlitchAmount ("Glitch Amount", Range(0,0.2)) = 0.04
        _GlitchFreq ("Glitch Frequency", Range(0.1,30)) = 8
        _RimColor ("Rim Glow", Color) = (0.5,1.0,1.0,1.0)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.0
        _Alpha ("Base Alpha", Range(0,1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Name "HologramForward"
            Tags { "LightMode"="ForwardBase" }

            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos    : TEXCOORD2;
                float  glitchY     : TEXCOORD3;
            };

            fixed4 _BaseColor;
            fixed4 _ScanlineColor;
            float  _ScanlineDensity;
            float  _ScanlineSpeed;
            float  _ScanlineIntensity;
            float  _GlitchAmount;
            float  _GlitchFreq;
            fixed4 _RimColor;
            float  _RimPower;
            float  _Alpha;

            float hash11(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;

                // Glitch : random horizontal displacement bands every few rows
                float bandIdx = floor(v.vertex.y * _GlitchFreq + _Time.y * 5.0);
                float jitter = (hash11(bandIdx) - 0.5) * 2.0 * _GlitchAmount;
                // Only apply at high glitch threshold for sparse stutter effect
                float gate = step(0.85, hash11(bandIdx + _Time.y));
                float4 vert4 = v.vertex;
                vert4.x += jitter * gate;

                o.pos = UnityObjectToClipPos(vert4);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, vert4).xyz;
                o.glitchY = bandIdx;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Scanlines : sin pattern shifted by time
                float scanCoord = i.uv.y * _ScanlineDensity + _Time.y * _ScanlineSpeed;
                float scanline = (sin(scanCoord * 6.28318) + 1.0) * 0.5;
                // Step thresholded for hard-edged scanlines feel
                float scanMask = smoothstep(0.4, 0.6, scanline) * _ScanlineIntensity;

                // Fresnel
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 normal = normalize(i.worldNormal);
                float rim = 1.0 - saturate(dot(viewDir, normal));
                float rimFactor = pow(rim, _RimPower);

                // Color compose
                fixed3 baseRGB = _BaseColor.rgb;
                fixed3 scanRGB = _ScanlineColor.rgb * scanMask;
                fixed3 rimRGB = _RimColor.rgb * rimFactor * _RimColor.a;

                fixed3 finalRGB = baseRGB + scanRGB + rimRGB;
                // Alpha blend : base + rim boost + slight scanline highlight
                float finalA = _Alpha * _BaseColor.a + rimFactor * 0.35 + scanMask * 0.2;
                finalA = saturate(finalA);

                return fixed4(finalRGB, finalA);
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
