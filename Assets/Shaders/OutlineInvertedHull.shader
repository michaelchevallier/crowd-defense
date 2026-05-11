// Outline inverted hull — port du pattern cellShadingOutlineColor() ToonMaterial.js
// Vertex extrudé le long de la normale × _OutlineWidth (world units).
// Cull Front : seul le back-face visible → silhouette outline noire autour du mesh principal.
Shader "CrowdDefense/OutlineInvertedHull"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

        Pass
        {
            Name "Outline"
            // Inverted hull : cull front faces, seul le back-face est rendu
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            fixed4 _OutlineColor;
            float  _OutlineWidth;

            v2f vert(appdata v)
            {
                v2f o;
                // Extrude vertex along object-space normal, then to clip space
                float3 extruded = v.vertex.xyz + v.normal * _OutlineWidth;
                o.pos = UnityObjectToClipPos(float4(extruded, 1.0));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }

    FallBack Off
}
