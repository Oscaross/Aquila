Shader "Aquila/Haze"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Haze)]
        _LayerHaze     ("Layer Haze (depth)", Range(0,1)) = 0.3
        _HighlightKeep ("Highlight Preservation", Range(0,1)) = 0.35
        _Desat         ("Desaturation", Range(0,1)) = 0.3
        _RampAmount    ("Vertical Ramp Amount", Range(0,1)) = 0.05
        _HorizonY      ("Horizon World Y", Float) = 0
        _RampHeight    ("Ramp Height (world units)", Float) = 12
        _Curve         ("Ramp Curve", Range(0.25,4)) = 1.6

        [Header(Pixel Art)]
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
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
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float3 positionWS  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Set through Shader.GlobalColour
            float4 _GlobalHazeColor;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _LayerHaze;
                float  _HighlightKeep;
                float  _Desat;
                float  _RampAmount;
                float  _HorizonY;
                float  _RampHeight;
                float  _Curve;
                float  _Cutoff;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS);
                OUT.positionHCS = pos.positionCS;
                OUT.positionWS  = pos.positionWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                c *= IN.color;

                clip(c.a - _Cutoff);

                // depth term + vertical ramp
                float t = saturate((IN.positionWS.y - _HorizonY) / max(_RampHeight, 0.0001));
                float amount = saturate(_LayerHaze + pow(t, _Curve) * _RampAmount);

                // weight by luminance so darks haze more than lights
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                amount *= lerp(1.0, _HighlightKeep, lum);

                // quantise LAST, once all weighting is applied
                amount = saturate(amount);

                // desaturate, then lerp toward haze colour
                c.rgb = lerp(c.rgb, lum.xxx, amount * _Desat);
                c.rgb = lerp(c.rgb, _GlobalHazeColor.rgb, amount);

                return c;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}