// Toon_Lit — cel-shaded lambert URP port
// 3-step gradient ramp: shadow / mid / bright + rim light + shadow caster
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
        [Enum(UnityEngine.Rendering.CullMode)] _Cull   ("Cull",   Float) = 2
        [Enum(Off,0,On,1)]                     _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _Surface ("Surface", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ToonLitForward"
            Tags { "LightMode"="UniversalForward" }

            Cull [_Cull]
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _BaseColor;
                half4  _ShadowColor;
                half4  _MidColor;
                half4  _BrightColor;
                float  _ShadowThreshold;
                float  _MidThreshold;
                half4  _RimColor;
                float  _RimPower;
                float  _RimEnabled;
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
                o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _BaseColor;

                float3 normalWS  = normalize(i.normalWS);
                Light  mainLight = GetMainLight();
                float  nDotL     = max(0.0, dot(normalWS, mainLight.direction));
                float  lit       = nDotL * mainLight.distanceAttenuation;

                half4 band;
                if (lit < _ShadowThreshold)    band = _ShadowColor;
                else if (lit < _MidThreshold)  band = _MidColor;
                else                           band = _BrightColor;

                half4 color = texColor * band;

                if (_RimEnabled > 0.5)
                {
                    float3 viewDir   = normalize(GetWorldSpaceViewDir(i.positionWS));
                    float  rim       = 1.0 - saturate(dot(viewDir, normalWS));
                    float  rimFactor = pow(rim, _RimPower);
                    color.rgb += _RimColor.rgb * _RimColor.a * rimFactor;
                }

                color.a = texColor.a * _BaseColor.a;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            Cull Back
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
