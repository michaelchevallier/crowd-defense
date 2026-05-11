// Portal shader — port de createPortalMaterial() (Shaders.js V5)
// Anneau magique pulsant + runes rotatives + glow (tour Portal)
Shader "CrowdDefense/Portal"
{
    Properties
    {
        _Color      ("Ring Color",      Color)        = (0.706,0.416,1.0,1.0)
        _GlowPower  ("Glow Power",      Range(0.1,5)) = 1.0
        _RotSpeed   ("Rune Rot Speed",  Range(-5,5))  = 2.0
        _RuneCount  ("Rune Segments",   Range(2,16))  = 8.0
        _PulseFreq  ("Pulse Frequency", Range(0.1,5)) = 3.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            Name "PortalForward"
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
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            fixed4 _Color;
            float  _GlowPower;
            float  _RotSpeed;
            float  _RuneCount;
            float  _PulseFreq;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 c = i.uv - 0.5;
                float r = length(c);
                float angle = atan2(c.y, c.x);

                // Anneau pulsant (port du ring V5)
                float ring = smoothstep(0.45, 0.40, r) * smoothstep(0.25, 0.30, r);

                // Runes rotatives (port sin(angle * 8 + uTime * 2) V5)
                float runes = step(0.7, sin(angle * _RuneCount + _Time.y * _RotSpeed));

                // Glow pulse
                float glow = ring * (0.5 + 0.5 * sin(_Time.y * _PulseFreq)) * _GlowPower;

                fixed3 col = _Color.rgb * (glow + runes * ring * 0.8);
                float  a   = ring + runes * ring;
                a = saturate(a);

                return fixed4(col, a);
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
