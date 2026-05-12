// Portal_Vortex shader — spawn portal enemy VFX
// UV polar rotation vortex + rim emission (V5 port)
Shader "CrowdDefense/Portal_Vortex"
{
    Properties
    {
        _MainColor      ("Vortex Color",    Color)        = (0.5,0.2,1.0,1.0)
        _EmissionColor  ("Emission Color",  Color)        = (0.7,0.4,1.0,1.0)
        _RotSpeed       ("Rotation Speed",  Range(-5,5))  = 0.5
        _SpiralTurns    ("Spiral Turns",    Range(1,10))  = 3.0
        _RimPower       ("Rim Power",       Range(0.5,8)) = 3.0
        _Brightness     ("Brightness",      Range(0.5,3)) = 1.5
        _CenterRadius   ("Center Hole",     Range(0,0.4)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "PortalVortexForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _MainColor;
                half4  _EmissionColor;
                float  _RotSpeed;
                float  _SpiralTurns;
                float  _RimPower;
                float  _Brightness;
                float  _CenterRadius;
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
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs vpi = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(v.normalOS);
                o.positionCS = vpi.positionCS;
                o.positionWS = vpi.positionWS;
                o.normalWS   = vni.normalWS;
                o.uv         = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 c = i.uv - 0.5;
                float  r = length(c);

                // Polar UV rotation: angle increases with time + radius
                float angle = atan2(c.y, c.x) + _Time.y * _RotSpeed;
                // Spiral: combine angle with radius for vortex
                float spiral = sin(angle * _SpiralTurns - r * 12.0 + _Time.y * _RotSpeed * 2.0);
                float vortex = (spiral * 0.5 + 0.5) * smoothstep(0.5, 0.1, r);

                // Mask: hollow center + fade outer edge
                float mask = smoothstep(_CenterRadius, _CenterRadius + 0.05, r)
                           * smoothstep(0.5, 0.35, r);

                // Rim emission: pow(1 - dot(N, V), 3) * _EmissionColor
                float3 viewDir  = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 normalWS = normalize(i.normalWS);
                float  NdotV    = saturate(dot(normalWS, viewDir));
                float  rim      = pow(1.0 - NdotV, _RimPower);

                half3 col = _MainColor.rgb * vortex * _Brightness
                          + _EmissionColor.rgb * rim;
                float a   = saturate(mask * (vortex * 0.7 + 0.3) + rim * 0.5);

                return half4(col, a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
