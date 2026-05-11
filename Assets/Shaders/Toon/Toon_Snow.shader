// Toon_Snow — tuile neige scintillante, port V5 snow theme
// Sparkle shimmer + normal-map style glint + cool-blue tint
Shader "CrowdDefense/Toon/Snow"
{
    Properties
    {
        _MainTex         ("Base Texture",       2D)           = "white" {}
        _NormalTex       ("Normal Map",         2D)           = "bump" {}
        _Tint            ("Snow Tint",          Color)        = (0.88,0.93,1.0,1)
        _NormalStrength  ("Normal Strength",    Range(0,2))   = 0.6
        _SparkleScale    ("Sparkle Scale",      Range(5,100)) = 30.0
        _SparkleSpeed    ("Sparkle Speed",      Range(0,10))  = 4.0
        _SparkleThreshold("Sparkle Threshold",  Range(0,1))   = 0.82
        _SparkleColor    ("Sparkle Color",      Color)        = (1,1,1,1)
        _SparkleStrength ("Sparkle Strength",   Range(0,2))   = 0.9
        _GlintFreq       ("Glint Frequency",    Range(0,10))  = 3.0
        _GlintAmp        ("Glint Amplitude",    Range(0,1))   = 0.25
        _FresnelPower    ("Fresnel Power",       Range(0.5,8)) = 3.0
        _FresnelColor    ("Fresnel Tint",        Color)        = (0.7,0.85,1.0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "SnowForward"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            ZWrite On

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
            sampler2D _NormalTex;
            fixed4    _Tint;
            float     _NormalStrength;
            float     _SparkleScale;
            float     _SparkleSpeed;
            float     _SparkleThreshold;
            fixed4    _SparkleColor;
            float     _SparkleStrength;
            float     _GlintFreq;
            float     _GlintAmp;
            float     _FresnelPower;
            fixed4    _FresnelColor;

            // Hash-based sparkle — each cell flickers independently
            float hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float sparkle(float2 uv, float t)
            {
                float2 cell = floor(uv * _SparkleScale);
                float  h    = hash21(cell);
                // Each cell flickers at its own phase/speed
                float  flicker = sin(t * _SparkleSpeed * (0.5 + h) + h * 6.28318) * 0.5 + 0.5;
                return step(_SparkleThreshold, flicker * h);
            }

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
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Tint;

                // Normal map perturbs light response for icy surface glints
                fixed4 normalSample = tex2D(_NormalTex, i.uv);
                float3 tn = UnpackNormal(normalSample);
                tn = normalize(float3(tn.xy * _NormalStrength, tn.z));
                float3 worldN = normalize(i.worldNormal + tn.x * 0.1 + tn.y * 0.1);

                // Diffuse (basic lambert — snow is matte with sparkle exceptions)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float  nDotL    = max(0.0, dot(worldN, lightDir));
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                float  lit      = nDotL * atten;

                // 3-step cel-shading (comme Toon_Lit)
                fixed3 band;
                if      (lit < 0.3) band = fixed3(0.6, 0.65, 0.75);
                else if (lit < 0.65) band = fixed3(0.85, 0.88, 0.95);
                else                 band = fixed3(1.0, 1.0, 1.0);
                fixed4 color = texColor * fixed4(band, 1.0);

                // Sparkle flicker (port du sparkle V5)
                float sp = sparkle(i.uv, _Time.y);
                color.rgb += _SparkleColor.rgb * sp * _SparkleStrength;

                // Rolling glint wave (ice crystal catch-light)
                float glint = sin(i.uv.x * _GlintFreq * 6.28318 + _Time.y * 2.0)
                            * sin(i.uv.y * _GlintFreq * 6.28318 + _Time.y * 1.7);
                glint = max(0.0, glint) * _GlintAmp;
                color.rgb += glint;

                // Fresnel (blue-white rim for icy feel)
                float3 viewDir   = normalize(_WorldSpaceCameraPos - i.worldPos);
                float  fresnel   = 1.0 - saturate(dot(viewDir, worldN));
                float  fresnelFactor = pow(fresnel, _FresnelPower);
                color.rgb = lerp(color.rgb, _FresnelColor.rgb, fresnelFactor * 0.4);

                return color;
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On Cull Back
            CGPROGRAM
            #pragma vertex vert_s
            #pragma fragment frag_s
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            struct v_s { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct f_s { V2F_SHADOW_CASTER; };
            f_s vert_s(v_s v) { f_s o; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o); return o; }
            float4 frag_s(f_s i) : SV_Target { SHADOW_CASTER_FRAGMENT(i); }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
