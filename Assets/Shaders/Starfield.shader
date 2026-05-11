// Starfield shader — port de createStarfield() (Shaders.js V5)
// Point sprite twinkle — chaque étoile est un point billboard
// Usage: assigner à un objet avec un Mesh de type Points (via ParticleSystem ou MeshFilter custom)
Shader "CrowdDefense/Starfield"
{
    Properties
    {
        _StarColor  ("Star Color",      Color)        = (1.0,1.0,0.94,1.0)
        _TwinkleFreq("Twinkle Freq",    Range(0.5,10))= 3.0
        _PointSize  ("Point Size",      Range(1,20))  = 300.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "StarfieldForward"
            Tags { "LightMode"="ForwardBase" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float  size   : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos  : SV_POSITION;
                float  vSize: TEXCOORD0;
                float  pSize: PSIZE;
            };

            fixed4 _StarColor;
            float  _TwinkleFreq;
            float  _PointSize;

            v2f vert(appdata v)
            {
                v2f o;
                float4 mv = mul(UNITY_MATRIX_MV, v.vertex);
                // Port de gl_PointSize = size * (300 / -mv.z) V5
                o.pSize = v.size * (_PointSize / -mv.z);
                o.pos   = mul(UNITY_MATRIX_P, mv);
                o.vSize = v.size;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Disque soft — gl_PointCoord n'est pas standard CG, on utilise une approche fixe
                // (twinkle seul sans discard car PSIZE suffit pour le rendu point)
                float twinkle = 0.5 + 0.5 * sin(_Time.y * _TwinkleFreq + i.vSize * 10.0);
                float a = twinkle;
                return fixed4(_StarColor.rgb, a);
            }
            ENDCG
        }
    }

    FallBack Off
}
