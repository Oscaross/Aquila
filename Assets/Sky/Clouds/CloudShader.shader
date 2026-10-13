Shader "Aquila/CloudShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Palette.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _Opacity;
            CBUFFER_END

            // Global, published by C#. Order: 0 = hi, 1 = mid, 2 = lo.
            float4 _CloudColours[3];
            float4 _GlobalHorizonColor;

            // Authoring keys, gamma space (hex / 255). Cloud textures must have sRGB OFF.
            static const float3 KEYS[3] =
            {
                float3(216, 220, 228) / 255.0, // d8dce4  hi
                float3(190, 197, 208) / 255.0, // bec5d0  mid
                float3(165, 174, 188) / 255.0  // a5aebc  lo
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

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS);

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 s = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, IN.uv, 0);
                
                half4 curr = _CloudColours[NearestBand(s.rgb)];
                
                half3 col;
                
                // this is the underside of the cloud so lerp it with the sky's global colour based on its alpha (which increases/decreases during sunset and sunrise)
                if (NearestBand(s.rgb) == 2)
                {
                    col = lerp(curr, _GlobalHorizonColor.rgb, _GlobalHorizonColor.a).rgb;
                }
                else
                {
                    // otherwise just use the regular cloud preset as we're outside the sunrise/sunset interval
                    col = curr.rgb;
                }

                float steps = max(_PaletteAlphaSteps, 1.0);
                half a = saturate(round(s.a * _Opacity * steps) / steps);
                clip(a - 0.001); // discard fully transparent pixels, like LitSprite's cutoff

                return half4(col, a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}