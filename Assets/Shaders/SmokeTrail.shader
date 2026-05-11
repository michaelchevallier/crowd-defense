// SmokeTrail shader URP port
// Point sprites qui grossissent avec l'âge + alpha fade
Shader "CrowdDefense/SmokeTrail"
{
    Properties
    {
        _SmokeColor ("Smoke Color",    Color)         = (0.5,0.5,0.5,1.0)
        _MinSize    ("Min Point Size", Range(1,50))   = 10.0
        _MaxSize    ("Max Point Size", Range(1,200))  = 40.0
        _DepthScale ("Depth Scale",    Range(50,500)) = 200.0
        _AlphaScale ("Alpha Scale",    Range(0.1,2))  = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "SmokeForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _SmokeColor;
                float  _MinSize;
                float  _MaxSize;
                float  _DepthScale;
                float  _AlphaScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float  age        : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  vAge       : TEXCOORD0;
                float  pSize      : PSIZE;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 posVS   = mul(UNITY_MATRIX_MV, v.positionOS);
                float  baseSize = lerp(_MinSize, _MaxSize, v.age);
                o.pSize        = baseSize * (_DepthScale / max(0.01, -posVS.z));
                o.positionCS   = mul(UNITY_MATRIX_P, posVS);
                o.vAge         = v.age;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float alpha = saturate((1.0 - i.vAge) * _AlphaScale);
                return half4(_SmokeColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
