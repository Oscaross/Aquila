Shader "Aquila/Haze"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Placement)]
        _LayerDepth ("Layer Depth", Range(0,1)) = 0.3
        _RampHeight ("Vertical Ramp Height (world units)", Float) = 12

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
            #include "Assets/Shaders/Palette.hlsl"

            // Fixed relationships, deliberately not exposed. Each one used to be a
            // material slider that always moved in step with _LayerDepth — if a layer
            // seems to want a different value here, its depth is wrong instead.
            #define HAZE_NEAR_NIGHT_LIGHT  0.30   // light a foreground layer keeps at night
            #define HAZE_FAR_NIGHT_LIGHT   0.00   // light a distant layer keeps at night
            #define HAZE_LIGHT_FALLOFF     2.00   // >1 makes dusk fall away fast
            #define HAZE_RAMP_CURVE        1.60   // concentrates the ramp toward the top
            #define HAZE_RAMP_STRENGTH     0.25   // how far the ramp can push toward full haze

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

            // Globals published by SkyController, already quantised there so every
            // shader steps at the same moments rather than each drifting separately.
            float4 _GlobalHazeColor;        // rgb = haze tint, a = atmospheric strength
            float4 _GlobalLightColor;       // tint of the current key light
            float  _GlobalLightIntensity;   // 0 = night, 1 = full daylight
            float  _GlobalHorizonY;         // world y of the horizon line

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _LayerDepth;
                float  _RampHeight;
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

                // ---- 1. DISTANCE --------------------------------------------------------
                // How far back is this pixel? The layer's own depth, plus a vertical ramp so
                // the tops of tall objects sit further into the haze than their bases. The
                // ramp scales by the remaining headroom, so it can never overshoot 1.
                float ramp = pow(saturate((IN.positionWS.y - _GlobalHorizonY)
                                          / max(_RampHeight, 0.0001)), HAZE_RAMP_CURVE);
                float distance = _LayerDepth + (1.0 - _LayerDepth) * ramp * HAZE_RAMP_STRENGTH;

                // Quantise DISTANCE, not the final amount. Distance is fixed per pixel, so
                // the bands stay locked to the sprite; quantising after the time-varying
                // strength would make band edges sweep up the sprite as the day passes.
                distance = floor(saturate(distance) * _HazeSteps + 0.5) / _HazeSteps;

                // ---- 2. LIGHTING --------------------------------------------------------
                // Derived from depth, not tuned separately: a distant layer receives less
                // bounced light, so it collapses further toward black at night. Quantised
                // onto the shared alpha grid so all layers step together.
                float nightFloor = lerp(HAZE_NEAR_NIGHT_LIGHT, HAZE_FAR_NIGHT_LIGHT, _LayerDepth);
                float light = lerp(nightFloor, 1.0,
                                   pow(saturate(_GlobalLightIntensity), HAZE_LIGHT_FALLOFF));
                light = floor(light * _PaletteAlphaSteps + 0.5) / _PaletteAlphaSteps;

                c.rgb *= _GlobalLightColor.rgb * light;

                // ---- 3. HAZE ------------------------------------------------------------
                // Distance times current atmospheric strength. The tint pull desaturates
                // on its own — a separate desaturation term did the same job twice and
                // fed a continuous per-pixel value into the snap.
                float amount = saturate(distance * _GlobalHazeColor.a);
                c.rgb = lerp(c.rgb, _GlobalHazeColor.rgb, amount);

                return half4(SnapToPalette(c.rgb), c.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}