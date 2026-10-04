Shader "Pokimun/CharacterGlow"
{
    Properties
    {
        _GlowColor    ("Color del Brillo", Color)         = (0.2, 1.0, 0.4, 0.9)
        _OutlineWidth ("Grosor del borde", Range(0, 0.12)) = 0.04
        _Intensity    ("Intensidad",       Range(0, 1))   = 1.0
        _PulsoSpeed   ("Velocidad Pulso",  Float)         = 2.0
        _UsePattern   ("Patron Encantamiento (0/1)", Float) = 0.0
        _PatternSpeed ("Velocidad Patron", Float)         = 1.8
        _PatternScale ("Escala Patron",    Float)         = 7.0
    }

    SubShader
    {
        // Sin LightMode especial -> URP lo renderiza como renderer independiente
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent+50"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "GlowPass"
            Cull  Front      // Cara trasera expandida = silueta/borde
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One   // Aditivo: brilla sin tapar la skin

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float  _OutlineWidth;
                float  _Intensity;
                float  _PulsoSpeed;
                float  _UsePattern;
                float  _PatternSpeed;
                float  _PatternScale;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Expandir a lo largo de la normal en object space
                float3 expandido = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                OUT.positionHCS = TransformObjectToHClip(expandido);
                OUT.uv = IN.uv;
                return OUT;
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash21(i), Hash21(i + float2(1,0)), f.x),
                    lerp(Hash21(i + float2(0,1)), Hash21(i + float2(1,1)), f.x),
                    f.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float pulso = sin(_Time.y * _PulsoSpeed) * 0.15 + 0.85;

                float mascara = 1.0;
                if (_UsePattern > 0.5)
                {
                    float2 uvAnim1 = IN.uv * _PatternScale + float2(_Time.y * _PatternSpeed * 0.3, _Time.y * _PatternSpeed);
                    float2 uvAnim2 = IN.uv * (_PatternScale * 2.0) - float2(_Time.y * _PatternSpeed * 0.5, 0);
                    float n = Noise(uvAnim1) * 0.6 + Noise(uvAnim2) * 0.4;
                    // Chispas que parpadean
                    float chispa = step(0.9, Hash21(floor(uvAnim1 * 2.0))) * abs(sin(_Time.y * 5.0 + Hash21(floor(uvAnim1))));
                    mascara = saturate(lerp(0.45, 1.0, n) + chispa * 0.5);
                }

                half4 col = _GlowColor;
                col.a *= _Intensity * pulso * mascara;
                // Multiplicar RGB para que el blending aditivo sea visible
                col.rgb *= (1.0 + col.a * 1.5);
                return col;
            }
            ENDHLSL
        }
    }

    // Fallback Built-in para cuando URP no está disponible
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+50" }
        Pass
        {
            Cull Front
            ZWrite Off
            Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _GlowColor;
            float  _OutlineWidth;
            float  _Intensity;
            float  _PulsoSpeed;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f    { float4 pos:SV_POSITION; };
            v2f vert(appdata v) {
                v2f o;
                v.vertex.xyz += normalize(v.normal) * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            half4 frag(v2f i) : SV_Target {
                float p = sin(_Time.y * _PulsoSpeed) * 0.15 + 0.85;
                half4 c = _GlowColor;
                c.a *= _Intensity * p;
                c.rgb *= (1.0 + c.a * 1.5);
                return c;
            }
            ENDCG
        }
    }
}
