// Starfield shader URP port
// Point sprite twinkle — chaque étoile clignote indépendamment
Shader "CrowdDefense/Starfield"
{
    Properties
    {
        _StarColor   ("Star Color",   Color)         = (1.0,1.0,0.94,1.0)
        _TwinkleFreq ("Twinkle Freq", Range(0.5,10)) = 3.0
        _PointSize   ("Point Size",   Range(1,20))   = 300.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "StarfieldForward"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _StarColor;
                float  _TwinkleFreq;
                float  _PointSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float  size       : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  vSize      : TEXCOORD0;
                float  pSize      : PSIZE;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 posVS  = mul(UNITY_MATRIX_MV, v.positionOS);
                o.pSize       = v.size * (_PointSize / -posVS.z);
                o.positionCS  = mul(UNITY_MATRIX_P, posVS);
                o.vSize       = v.size;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float twinkle = 0.5 + 0.5 * sin(_Time.y * _TwinkleFreq + i.vSize * 10.0);
                return half4(_StarColor.rgb, twinkle);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
