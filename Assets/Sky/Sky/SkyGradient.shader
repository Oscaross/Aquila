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
                float2 gamePx = floor(IN.positionCS.xy);
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