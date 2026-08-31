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
            #include "Assets/Shaders/Palette.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _BandCount;
                float _DitherAmount;
                float _MaxAlpha;
            CBUFFER_END
            
            float4 _GlobalHorizonColor; 
            float4 _GlobalZenithColor;
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
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 0.0001);
                float2 gamePx = screenUV * _ScreenParams.xy / max(_GlobalPixelScale, 1.0);
                float threshold = AquilaBayerThreshold(gamePx);

                float a = pow(saturate(1.0 - IN.uv.y), max(_GlobalRampExponent, 0.01));
                float dither = _DitherAmount * saturate(_GlobalHorizonColor.a * 4.0);
                float scaled = a * _BandCount;
                float rounded = lerp(step(0.5, frac(scaled)), step(threshold, frac(scaled)), dither);
                a = saturate((floor(scaled) + rounded) / _BandCount);

                a *= _GlobalHorizonColor.a * _MaxAlpha;
                float3 zen = _GlobalZenithColor.rgb;
                float3 hor = _GlobalHorizonColor.rgb;
                float3 col = lerp(zen, hor, a);
                
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}