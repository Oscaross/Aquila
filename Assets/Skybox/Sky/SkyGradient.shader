Shader "Aquila/SkyGradient"
{
    Properties
    {
        _BandCount     ("Band Count", Range(2, 32)) = 8
        _DitherAmount  ("Dither Amount", Range(0, 1)) = 1
        _MaxAlpha      ("Max Alpha", Range(0, 1)) = 1
        _MainTex       ("Sprite Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _BandCount;
                float _DitherAmount;
                float _MaxAlpha;
            CBUFFER_END
            
            float4 _GlobalHorizonColor; 
            float  _GlobalRampExponent;
            float _GlobalPixelScale;

            static const float bayer4[16] =
            {
                 0.0,  8.0,  2.0, 10.0,
                12.0,  4.0, 14.0,  6.0,
                 3.0, 11.0,  1.0,  9.0,
                15.0,  7.0, 13.0,  5.0
            };

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float a = pow(saturate(1.0 - IN.uv.y), max(_GlobalRampExponent, 0.01)) * _MaxAlpha;

                // Screen pixels, divided down to GAME pixels so the dither cells match the art.
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 0.0001);
                float2 gamePx = screenUV * _ScreenParams.xy / max(_GlobalPixelScale, 1.0);

                // floor before fmod so the index is integral; the offset keeps it positive
                // off the left edge of the screen, where gamePx goes negative.
                int2 p = int2(fmod(floor(gamePx) + 4096.0, 4.0));
                float threshold = (bayer4[p.y * 4 + p.x] + 0.5) / 16.0;

                // Quantise to _BandCount levels, dithering across each band boundary.
                float scaled = a * _BandCount;
                float rounded = lerp(step(0.5, frac(scaled)), step(threshold, frac(scaled)), _DitherAmount);
                a = saturate((floor(scaled) + rounded) / _BandCount);

                return half4(_GlobalHorizonColor.rgb, a * _GlobalHorizonColor.a);
            }
            ENDHLSL
        }
    }
}