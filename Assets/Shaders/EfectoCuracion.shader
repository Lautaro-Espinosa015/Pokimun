// SHADER: Curacion — Overlay verde brillante translucido
// Guardarlo en: Assets/Shaders/EfectoCuracion.shader
Shader "Pokimun/EfectoCuracion"
{
    Properties
    {
        _BaseMap ("Textura Base", 2D) = "white" {}
        _BaseColor ("Color Base", Color) = (1,1,1,1)
        _GlowColor ("Color de Brillo (verde)", Color) = (0.1, 1.0, 0.3, 0.45)
        _PulsoVelocidad ("Velocidad del Pulso", Float) = 2.5
        _PulsoIntensidad ("Intensidad del Pulso", Range(0,1)) = 0.4
        _FresnelPotencia ("Potencia Fresnel (borde)", Range(0.1, 8)) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        // Pass 1: Render normal del personaje (para no opacar la skin)
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _GlowColor;
                float  _PulsoVelocidad;
                float  _PulsoIntensidad;
                float  _FresnelPotencia;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = normalize(_WorldSpaceCameraPos - TransformObjectToWorld(IN.positionOS.xyz));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target {
                half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Fresnel: brilla mas en los bordes
                float fresnel = pow(1.0 - saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS))), _FresnelPotencia);

                // Pulso sinusoidal en el tiempo
                float pulso = (_SinTime.w * 0.5 + 0.5) * _PulsoIntensidad;

                half4 glow = _GlowColor;
                glow.a *= (fresnel + pulso);

                // Mezcla aditiva del brillo verde sobre la skin
                half4 col = base;
                col.rgb = lerp(col.rgb, glow.rgb, glow.a * 0.6);
                col.a = base.a;
                return col;
            }
            ENDHLSL
        }
    }
}
