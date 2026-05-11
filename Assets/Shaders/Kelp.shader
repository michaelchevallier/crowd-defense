// Kelp shader — port de createKelpMaterial() (Shaders.js V5)
// Vertex bend proportionnel à la hauteur UV.y — racine fixe, sommet ondule
// Gradient root → tip sur la couleur de base
Shader "CrowdDefense/Kelp"
{
    Properties
    {
        _BaseColor  ("Base Color",        Color)        = (0.18,0.49,0.20,1)
        _BendAmp    ("Bend Amplitude",    Range(0,0.5)) = 0.15
        _BendFreqX  ("Bend Freq X",       Range(0,5))   = 1.2
        _BendFreqZ  ("Bend Freq Z",       Range(0,5))   = 0.9
        _BendSpeed  ("Bend Speed",        Range(0,5))   = 1.5
        _TipBright  ("Tip Brightness",    Range(1,2))   = 1.2
        _RootDark   ("Root Darkness",     Range(0,1))   = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "KelpForward"
            Tags { "LightMode"="ForwardBase" }
            Cull Off
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

            fixed4 _BaseColor;
            float  _BendAmp;
            float  _BendFreqX;
            float  _BendFreqZ;
            float  _BendSpeed;
            float  _TipBright;
            float  _RootDark;

            v2f vert(appdata v)
            {
                v2f o;
                float4 pos = v.vertex;
                // Bend proportionnel à UV.y^2 (port de bend = pow(uv.y,2) V5)
                float bend = v.uv.y * v.uv.y;
                pos.x += sin(_Time.y * _BendSpeed * _BendFreqX + pos.y * 1.5) * _BendAmp * bend;
                pos.z += cos(_Time.y * _BendSpeed * _BendFreqZ + pos.y * 1.5) * _BendAmp * bend;
                o.pos = UnityObjectToClipPos(pos);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Gradient root → tip (port de mix(color*0.6, color*1.2, uv.y) V5)
                fixed3 col = lerp(_BaseColor.rgb * _RootDark, _BaseColor.rgb * _TipBright, i.uv.y);
                return fixed4(col, 1.0);
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
