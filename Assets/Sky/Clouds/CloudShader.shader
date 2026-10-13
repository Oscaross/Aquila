Shader "Aquila/CloudShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 0.8
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
                float _Opacity;
            CBUFFER_END
            
            float4 _CloudColours[3];

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
                OUT.uv = IN.uv;
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }
            
            // Authoring keys, gamma space (hex / 255). Order must match Publish(): 0 = hi, 1 = mid, 2 = lo
            // These are the colours that clouds are drawn in. The hi band (top of the cloud), the mid band (bulk of the cloud) and the lo band (underside).
            // This allows us to identify which pixel is in which part of the cloud this shader sits on, and then update its colour accordingly throughout the day night cycle.
            static const float3 KEYS[3] =
            {
                float3(216, 220, 228) / 255.0, // d8dce4  hi
                float3(190, 197, 208) / 255.0, // bec5d0  mid
                float3(165, 174, 188) / 255.0 // a5aebc  lo
            };
            
            int NearestBand(float3 c)
            {
                int best = 0;
                float bestD = 1e9;
                [unroll] for (int k = 0; k < 3; k++)
                {
                    float3 d = c - KEYS[k];
                    float dist = dot(d, d);
                    if (dist < bestD) { bestD = dist; best = k; }
                }
                return best;
            }


            half4 frag(Varyings IN) : SV_Target
            {
                half4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 col = _CloudColours[NearestBand(s.rgb)].rgb;
                half a = round(s.a * _Opacity * _PaletteAlphaSteps) / _PaletteAlphaSteps;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}