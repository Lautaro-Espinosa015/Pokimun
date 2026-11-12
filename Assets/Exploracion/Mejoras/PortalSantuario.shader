Shader "Pokimun/Portal Santuario"
{
    Properties
    {
        [MainColor] _BaseColor ("Color del portal", Color) = (0.85, 0.31, 0.065, 1)
        [ToggleUI] _Agua ("Corrientes de agua", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Agua;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 cell)
            {
                return frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
            }

            float Particulas(float2 uv, float2 density, float time, float water)
            {
                float2 drift = lerp(float2(0.15, -1.6), float2(-0.85, -0.46), water);
                float2 grid = uv * density + drift * time;
                float2 cell = floor(grid);
                float seed = Hash(cell);
                float2 center = 0.25 + 0.5 * float2(seed, Hash(cell + 19.7));
                center.x += 0.09 * sin(time * 1.4 + seed * 25.0);
                float2 offset = (frac(grid) - center) * lerp(float2(1.2, 0.72), float2(0.92, 1.1), water);
                float radius = lerp(0.04, 0.14, seed);
                float glow = 1.0 - smoothstep(0.0, radius, length(offset));
                float flicker = 0.55 + 0.45 * sin(time * 2.2 + seed * 40.0);
                return glow * flicker * smoothstep(0.18, 0.4, seed);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;
                float water = saturate(_Agua);

                float fire = sin(uv.x * 33.0 + sin(uv.y * 11.0 - time * 2.1) * 2.8
                    + uv.y * 17.0 - time * 3.4);
                float ripple = sin(uv.y * 38.0 + sin(uv.x * 11.0 + time * 1.25) * 2.3
                    + uv.x * 9.0 - time * 2.2);
                float current = pow(saturate(0.5 + 0.5 * lerp(fire, ripple, water)), 3.0);

                float2 sides = min(uv, 1.0 - uv);
                float edge = min(sides.x, sides.y);
                float rim = 1.0 - smoothstep(0.012, 0.036, edge);
                float halo = 1.0 - smoothstep(0.018, 0.15, edge);
                float motes = Particulas(uv, float2(13.0, 17.0), time, water)
                    + 0.42 * Particulas(uv + 0.17, float2(21.0, 27.0), time * 0.65, water);
                motes *= smoothstep(0.02, 0.08, edge);

                half3 accent = lerp(half3(1.0, 0.68, 0.27), half3(0.53, 0.91, 1.0), water);
                half3 color = _BaseColor.rgb * (0.64 + 0.23 * current + 0.11 * halo);
                color += accent * (0.11 * current + 0.31 * rim + 0.58 * motes);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
