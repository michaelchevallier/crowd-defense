// SmokeTrail shader — port de createSmokeTrail() (Shaders.js V5)
// Particles boulet de canon / bombe: point sprites qui grossissent avec l'âge
// et s'estompent (alpha fade). Usage: Particle System avec Custom Shader.
Shader "CrowdDefense/SmokeTrail"
{
    Properties
    {
        _SmokeColor ("Smoke Color",   Color)        = (0.5,0.5,0.5,1.0)
        _MinSize    ("Min Point Size",Range(1,50))  = 10.0
        _MaxSize    ("Max Point Size",Range(1,200)) = 40.0
        _DepthScale ("Depth Scale",   Range(50,500))= 200.0
        _AlphaScale ("Alpha Scale",   Range(0.1,2)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "SmokeForward"
            Tags { "LightMode"="ForwardBase" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float  age    : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float  vAge  : TEXCOORD0;
                float  pSize : PSIZE;
            };

            fixed4 _SmokeColor;
            float  _MinSize;
            float  _MaxSize;
            float  _DepthScale;
            float  _AlphaScale;

            v2f vert(appdata v)
            {
                v2f o;
                float4 mv = mul(UNITY_MATRIX_MV, v.vertex);
                // Port de (10 + age*30) * (200/-mv.z) V5
                float baseSize = lerp(_MinSize, _MaxSize, v.age);
                o.pSize  = baseSize * (_DepthScale / max(0.01, -mv.z));
                o.pos    = mul(UNITY_MATRIX_P, mv);
                o.vAge   = v.age;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Port de alpha = (1 - vAge) * (1 - d*2) * 0.5 V5
                // Sans gl_PointCoord en CG: alpha = (1-age) * _AlphaScale
                float alpha = (1.0 - i.vAge) * _AlphaScale;
                alpha = saturate(alpha);
                return fixed4(_SmokeColor.rgb, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
