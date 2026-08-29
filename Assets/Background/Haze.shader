Shader "Aquila/Haze"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Layer Depth)]
        _LayerDepth  ("Layer Depth", Range(0,1)) = 0.3
        _RampAmount  ("Vertical Ramp Amount", Range(0,1)) = 0.05
        _RampHeight  ("Ramp Height (world units)", Float) = 12
        _RampCurve   ("Ramp Curve", Range(0.25,4)) = 1.6

        [Header(Lighting)]
        _LightAtNight ("Light At Night", Range(0,1)) = 0.05
        _LightAtDay   ("Light At Day", Range(0,1)) = 1
        _LightFalloff  ("Light Falloff", Range(1,4)) = 2
        _MinLight      ("Minimum Light Floor", Range(0,0.5)) = 0

        [Header(Appearance)]
        _Desat ("Haze Desaturation", Range(0,1)) = 0.3

        [Header(Pixel Art)]
        _Cutoff    ("Alpha Cutoff", Range(0,1)) = 0.5
        _HazeSteps ("Haze Steps", Range(3,20)) = 12
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

            // Globals published by SkyController.
            float4 _GlobalHazeColor;        // rgb = haze tint, a = atmospheric strength
            float4 _GlobalLightColor;       // tint of the current key light
            float  _GlobalLightIntensity;   // 0 = night, 1 = full daylight
            float  _GlobalHorizonY;         // world y of the horizon line

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _LayerDepth;
                float  _RampAmount;
                float  _RampHeight;
                float  _RampCurve;
                float _LightAtDay;
                float  _LightAtNight;
                float  _LightFalloff;
                float  _MinLight;
                float  _Desat;
                float  _Cutoff;
                float  _HazeSteps;
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

                // ---- SAMPLE -------------------------------------------------------------
                // Hard alpha cut, so the sprite keeps crisp pixel edges with no soft fringe.
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                c *= IN.color;
                clip(c.a - _Cutoff);

                // ---- 1. LIGHTING --------------------------------------------------------
                // Background layers are lit by the same key light as the foreground.
                //   _LightFalloff  — curves the response so dusk falls away fast and the
                //                    layer collapses toward black at night rather than just
                //                    dimming proportionally (which preserves detail).
                //   _LightResponse — this layer's overall sensitivity. Distant layers should
                //                    sit lower, since less light reaches back that far.
                //   _MinLight      — floor, if a layer should never go fully black.
                float light = lerp(_LightAtNight, _LightAtDay, pow(saturate(_GlobalLightIntensity), _LightFalloff));
                light = max(light, _MinLight);

                c.rgb *= _GlobalLightColor.rgb * light;

                // ---- 2. DISTANCE --------------------------------------------------------
                // How far back is this pixel? A per-layer constant, plus a vertical ramp so
                // the tops of tall objects sit further into the haze than their bases.
                float ramp = pow(saturate((IN.positionWS.y - _GlobalHorizonY)
                                          / max(_RampHeight, 0.0001)), _RampCurve);
                float distance = saturate(_LayerDepth + ramp * _RampAmount);

                // Quantise DISTANCE, not the final amount. Distance is fixed per pixel, so
                // the bands stay locked to the sprite; quantising after the time-varying
                // strength is applied would make band edges sweep up the sprite as the day
                // passes.
                distance = floor(distance * _HazeSteps + 0.5) / _HazeSteps;

                // ---- 3. CONDITIONS ------------------------------------------------------
                // How hazy is the atmosphere right now? Peaks around midday when there is
                // most light to scatter; dips at dawn/dusk and overnight.
                float amount = saturate(distance * _GlobalHazeColor.a);

                // ---- 4. APPLY -----------------------------------------------------------
                // Distance eats saturation first, then pulls the colour toward the haze tint.
                // lum is taken AFTER lighting so it reflects the lit colour, not the authored one.
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, lum.xxx, amount * _Desat);
                c.rgb = lerp(c.rgb, _GlobalHazeColor.rgb, amount);

                return c;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}