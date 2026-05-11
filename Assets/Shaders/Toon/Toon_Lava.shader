// Toon_Lava — tuile lave animée, port de createLavaMaterial() (Shaders.js V5)
// UV scroll + glow pulse + emission (HDR)
Shader "CrowdDefense/Toon/Lava"
{
    Properties
    {
        _MainTex        ("Base Texture",     2D)           = "white" {}
        _Tint           ("Lava Tint",        Color)        = (1,0.627,0.314,1)
        _ScrollSpeedX   ("Scroll Speed X",   Range(-2,2))  = 0.08
        _ScrollSpeedY   ("Scroll Speed Y",   Range(-2,2))  = 0.04
        _GlowColor      ("Glow Color",       Color)        = (1,0.4,0,1)
        _GlowPulseFreq  ("Glow Pulse Freq",  Range(0.1,5)) = 1.2
        _GlowPulseAmp   ("Glow Pulse Amp",   Range(0,2))   = 0.6
        _GlowBase       ("Glow Base",        Range(0,2))   = 0.8
        _CrackScale     ("Crack Scale",      Range(1,30))  = 8.0
        _CrackContrast  ("Crack Contrast",   Range(0,2))   = 1.4
        _EmissionStrength("Emission Strength",Range(0,4))  = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "LavaForward"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Tint;
            float     _ScrollSpeedX;
            float     _ScrollSpeedY;
            fixed4    _GlowColor;
            float     _GlowPulseFreq;
            float     _GlowPulseAmp;
            float     _GlowBase;
            float     _CrackScale;
            float     _CrackContrast;
            float     _EmissionStrength;

            // Hash-based 2D value noise — crée l'illusion de cracks de lave
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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Scrolling UV
                float2 uv = i.uv;
                uv.x += _Time.y * _ScrollSpeedX;
                uv.y += _Time.y * _ScrollSpeedY;

                fixed4 col = tex2D(_MainTex, uv) * _Tint;

                // Procedural crack noise (simule la texture V5 de lave)
                float n1 = valueNoise(i.uv * _CrackScale);
                float n2 = valueNoise(i.uv * (_CrackScale * 0.5) + float2(1.7, 3.3));
                float crack = pow(n1 * n2, _CrackContrast);
                // Mélange: zones sombres (croûte) vs zones brillantes (lave en fusion)
                fixed3 crustColor = _Tint.rgb * 0.25;
                col.rgb = lerp(crustColor, col.rgb, crack);

                // Glow pulse (port du _GlowPulse V5)
                float pulse = _GlowBase + sin(_Time.y * _GlowPulseFreq) * _GlowPulseAmp;
                pulse = max(0.0, pulse);
                // Emission: zones brillantes poussées vers la couleur de lueur
                float hotMask = crack * pulse;
                col.rgb += _GlowColor.rgb * hotMask * _EmissionStrength;

                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On Cull Back
            CGPROGRAM
            #pragma vertex vert_s
            #pragma fragment frag_s
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            struct v_s { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct f_s { V2F_SHADOW_CASTER; };
            f_s vert_s(v_s v) { f_s o; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o); return o; }
            float4 frag_s(f_s i) : SV_Target { SHADOW_CASTER_FRAGMENT(i); }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
