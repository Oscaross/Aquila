Shader "Aquila/Haze"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Haze)]
        _HazeColor  ("Haze Colour", Color) = (0.72, 0.78, 0.85, 1)
        _LayerHaze  ("Layer Haze (depth)", Range(0,1)) = 0.3
        _RampAmount ("Vertical Ramp Amount", Range(0,1)) = 0.08
        _HorizonY   ("Horizon World Y", Float) = 0
        _RampHeight ("Ramp Height (world units)", Float) = 12
        _Curve      ("Ramp Curve", Range(0.25,4)) = 1.6
        _Desat      ("Desaturation", Range(0, 1)) = 0.3

        [Header(Pixel Art)]
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _Steps  ("Quantise Steps (32 = off)", Range(1,32)) = 32
        _PPU    ("Pixels Per Unit", Float) = 16
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _HazeColor;
                float4 _GlobalHazeColor;
                float  _LayerHaze;
                float  _RampAmount;
                float  _HorizonY;
                float  _RampHeight;
                float  _Curve;
                float  _Cutoff;
                float  _Steps;
                float  _PPU;
                float  _Desat;
            CBUFFER_END

            static const float BAYER[16] =
            {
                 0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                12.0/16.0,  4.0/16.0, 14.0/16.0,  6.0/16.0,
                 3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                15.0/16.0,  7.0/16.0, 13.0/16.0,  5.0/16.0
            };

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

                // hard pixel-art edges: discard anything below the cutoff
                clip(c.a - _Cutoff);

                // vertical ramp, measured in world space so it stays put when the sprite moves
                float t = saturate((IN.positionWS.y - _HorizonY) / max(_RampHeight, 0.0001));
                float amount = _LayerHaze + pow(t, _Curve) * _RampAmount;
                amount = saturate(amount);

                // ordered dither, locked to the art's pixel grid
                int2 px = int2(floor(IN.positionWS.xy * _PPU));
                int idx = ((px.y & 3) << 2) | (px.x & 3); // avoid modulo as GPUs don't support this
                amount = floor(amount * _Steps + BAYER[idx]) / _Steps;
                amount = saturate(amount);
                
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float weighted = amount * lerp(1.0, _HighlightKeep, lum);
                c.rgb = lerp(c.rgb, lum.xxx, amount * _Desat);
                return c;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
