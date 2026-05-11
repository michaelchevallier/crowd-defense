// Portal shader URP port
// Anneau magique pulsant + runes rotatives + glow
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
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "PortalForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _Color;
                float  _GlowPower;
                float  _RotSpeed;
                float  _RuneCount;
                float  _PulseFreq;
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
                float2 c     = i.uv - 0.5;
                float  r     = length(c);
                float  angle = atan2(c.y, c.x);

                float ring  = smoothstep(0.45, 0.40, r) * smoothstep(0.25, 0.30, r);
                float runes = step(0.7, sin(angle * _RuneCount + _Time.y * _RotSpeed));
                float glow  = ring * (0.5 + 0.5 * sin(_Time.y * _PulseFreq)) * _GlowPower;

                half3 col = _Color.rgb * (glow + runes * ring * 0.8);
                float a   = saturate(ring + runes * ring);

                return half4(col, a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
