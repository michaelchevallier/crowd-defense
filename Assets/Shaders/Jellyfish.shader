// Jellyfish shader — overlay pour boss_submarin_kraken
// Scrolling UV + fresnel rim glow + noise-based pulse pour effet "bioluminescent"
// Surface = transparent (queue 3000) pour overlay au-dessus du toon mesh
Shader "CrowdDefense/Jellyfish"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2,0.8,1.0,0.6)
        _RimColor ("Rim Glow Color", Color) = (0.4,1.0,1.0,1.0)
        _RimPower ("Rim Power", Range(0.5,8.0)) = 2.5
        _ScrollSpeedU ("Scroll Speed U", Range(-2,2)) = 0.15
        _ScrollSpeedV ("Scroll Speed V", Range(-2,2)) = 0.4
        _NoiseScale ("Noise Scale", Range(0.5,20)) = 4.0
        _PulseFreq ("Pulse Frequency", Range(0.1,5)) = 1.2
        _PulseStrength ("Pulse Strength", Range(0,1)) = 0.35
        _Alpha ("Alpha", Range(0,1)) = 0.65
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Name "JellyfishForward"
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
            };

            fixed4 _BaseColor;
            fixed4 _RimColor;
            float  _RimPower;
            float  _ScrollSpeedU;
            float  _ScrollSpeedV;
            float  _NoiseScale;
            float  _PulseFreq;
            float  _PulseStrength;
            float  _Alpha;

            // Hash-based 2D value noise — cheap, no texture lookup
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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Scrolling UV (port "uOffset += dt * speed" of jellyfish.js)
                float2 scrolledUV = i.uv;
                scrolledUV.x += _Time.y * _ScrollSpeedU;
                scrolledUV.y += _Time.y * _ScrollSpeedV;

                // Value noise sample for bioluminescent shimmer
                float n = valueNoise(scrolledUV * _NoiseScale);

                // Pulse (sin-based brightening across UV.y)
                float pulse = (sin(_Time.y * _PulseFreq + i.uv.y * 6.28318) + 1.0) * 0.5;
                float pulseFactor = lerp(1.0, 1.0 + pulse, _PulseStrength);

                // Fresnel rim
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 normal = normalize(i.worldNormal);
                float rim = 1.0 - saturate(dot(viewDir, normal));
                float rimFactor = pow(rim, _RimPower);

                // Combine
                fixed3 baseRGB = _BaseColor.rgb * (0.6 + n * 0.5) * pulseFactor;
                fixed3 rimRGB = _RimColor.rgb * rimFactor * _RimColor.a;
                fixed3 finalRGB = baseRGB + rimRGB;
                float finalA = _Alpha * (_BaseColor.a * 0.4 + rimFactor * 0.6);

                return fixed4(finalRGB, finalA);
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
