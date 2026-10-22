Shader "NextLevel/Drifting Haze"
{
    Properties
    {
        _Tint ("Haze Tint", Color) = (0.58, 0.65, 0.61, 0.085)
        _Drift ("Drift", Vector) = (0.018, 0.006, 0, 0)
        _AtmosphereTime ("Atmosphere Time", Float) = 0
        _Aspect ("Aspect", Float) = 1.77778
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float4 _Drift;
                float _AtmosphereTime;
                float _Aspect;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 blend = frac(p);
                blend = blend * blend * (3.0 - 2.0 * blend);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), blend.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), blend.x), blend.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 p = uv * float2(_Aspect * 2.6, 3.5);
                float2 drift = _Drift.xy * _AtmosphereTime;
                float broad = Noise(p - drift);
                float wisps = Noise(p * float2(1.8, 3.1) + drift * 0.7 + 17.2);
                float density = smoothstep(0.28, 0.8, broad * 0.7 + wisps * 0.3);
                float verticalFade = lerp(1.0, 0.35, smoothstep(0.1, 0.95, uv.y));
                return half4(_Tint.rgb, _Tint.a * density * verticalFade);
            }
            ENDHLSL
        }
    }
}
