// OutlineInvertedHull — URP port
// Vertex extrudé le long de la normale × _OutlineWidth (object space).
// Cull Front : seul le back-face visible → silhouette outline noire autour du mesh.
Shader "CrowdDefense/OutlineInvertedHull"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color)             = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1))   = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 extruded = v.positionOS.xyz + v.normalOS * _OutlineWidth;
                o.positionCS = TransformObjectToHClip(extruded);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
