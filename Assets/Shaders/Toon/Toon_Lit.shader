// Toon_Lit — cel-shaded lambert, port de ToonMaterial.js (Three.js MeshToonMaterial)
// 3-step gradient ramp: shadow (#888) / mid (#ccc) / bright (#fff)
// Rim light (fresnel silhouette) + shadow caster pass
Shader "CrowdDefense/Toon/Lit"
{
    Properties
    {
        _BaseColor        ("Base Color",       Color)        = (1,1,1,1)
        _MainTex          ("Main Texture",     2D)           = "white" {}
        _ShadowColor      ("Shadow Color",     Color)        = (0.533,0.533,0.533,1)
        _MidColor         ("Mid Color",        Color)        = (0.8,0.8,0.8,1)
        _BrightColor      ("Bright Color",     Color)        = (1,1,1,1)
        _ShadowThreshold  ("Shadow Threshold", Range(0,1))   = 0.33
        _MidThreshold     ("Mid Threshold",    Range(0,1))   = 0.66
        _RimColor         ("Rim Light Color",  Color)        = (1,1,1,0.4)
        _RimPower         ("Rim Power",        Range(0.5,8)) = 3.0
        _RimEnabled       ("Rim Enabled",      Float)        = 1.0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull  ("Cull",   Float) = 2
        [Enum(Off,0,On,1)]                     _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _Surface ("Surface", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ToonLitForward"
            Tags { "LightMode"="ForwardBase" }

            Cull [_Cull]
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos    : TEXCOORD2;
                LIGHTING_COORDS(3, 4)
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _BaseColor;
            fixed4    _ShadowColor;
            fixed4    _MidColor;
            fixed4    _BrightColor;
            float     _ShadowThreshold;
            float     _MidThreshold;
            fixed4    _RimColor;
            float     _RimPower;
            float     _RimEnabled;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv) * _BaseColor;

                float3 normal   = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float  nDotL    = max(0.0, dot(normal, lightDir));

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                float lit = nDotL * atten;

                fixed4 band;
                if (lit < _ShadowThreshold)       band = _ShadowColor;
                else if (lit < _MidThreshold)      band = _MidColor;
                else                               band = _BrightColor;

                fixed4 color = texColor * band;

                if (_RimEnabled > 0.5)
                {
                    float3 viewDir   = normalize(_WorldSpaceCameraPos - i.worldPos);
                    float  rim       = 1.0 - saturate(dot(viewDir, normal));
                    float  rimFactor = pow(rim, _RimPower);
                    color.rgb += _RimColor.rgb * _RimColor.a * rimFactor;
                }

                color.a = texColor.a * _BaseColor.a;
                return color;
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            Cull Back
            CGPROGRAM
            #pragma vertex vert_shadow
            #pragma fragment frag_shadow
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            struct v_s { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct f_s { V2F_SHADOW_CASTER; };
            f_s vert_shadow(v_s v) { f_s o; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o); return o; }
            float4 frag_shadow(f_s i) : SV_Target { SHADOW_CASTER_FRAGMENT(i); }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
