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

            // Fixed relationships, deliberately not exposed. If a layer seems to want a
            // different value here, its _LayerDepth is wrong instead.
            #define HAZE_RAMP_CURVE     1.60   // concentrates the ramp toward the top
            #define HAZE_RAMP_STRENGTH  0.25   // how far the ramp can push toward full haze

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

            // Published by SkyController. _GlobalDarkness is declared in Palette.hlsl —
            // don't redeclare it here, that's the duplicate-declaration compile error.
            float4 _GlobalHazeColor;         // rgb = snapped haze tint, a = atmospheric strength
            float  _GlobalHorizonY;          // world y of the horizon line
            float  _GlobalPixelsPerUnit;     // 16 — for world-space dither anchoring

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

                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                c.a *= IN.color.a;   // alpha only — an rgb tint here would feed a modified
                                     // colour into the LUT and land it on the wrong ramp
                clip(c.a - _Cutoff);
                
                // Snap the RGB value to the correct ramp and lighting index based on the current global lighting level
                c.rgb = LightWithPaletteHard(c.rgb, _GlobalDarkness);

                // ---- 2. DISTANCE --------------------------------------------------------
                // How far back is this pixel? _LayerDepth is the layer's own distance;
                // the vertical ramp adds to it so parts of a sprite further from the
                // horizon line sit deeper into the haze.
                //
                // Decide the sign here. Ramping upward (what the old code did) fogs the
                // peaks and leaves the bases clear. Ramping by |y - horizonY| fogs the
                // bases and leaves peaks clear, which is what real aerial perspective
                // does. Try both, but the second is the physical one.
                //
                // Scale the ramp by the remaining headroom (1 - _LayerDepth) so it can
                // never push past 1.
                float dy = abs(IN.positionWS.y - _GlobalHorizonY); // how many y world units above the horizon does this background sit?
                float ramp = saturate(1.0 - dy / max(_RampHeight, 0.0001)); // ramp is 1 at the horizon and 0 at _RampHeight
                ramp = pow(ramp, HAZE_RAMP_CURVE); // concentrate the haze towards the horizon line with exponential falloff above
                
                float distance = _LayerDepth + (1.0 - _LayerDepth) * ramp * HAZE_RAMP_STRENGTH;
                
                // ---- 3. QUANTISE DISTANCE, NOT THE FINAL AMOUNT -------------------------
                // Distance is fixed per pixel, so the bands stay locked to the sprite.
                // Quantise after multiplying by the time-varying strength and the band
                // edges sweep across the sprite as the day passes — same failure as
                // banding the sky ramp after the horizon multiply.
                
                float quantisedDistance = round(distance * _HazeSteps) / _HazeSteps;


                // ---- 4. HAZE AMOUNT -----------------------------------------------------
                // Quantised distance times the current atmospheric strength
                // (_GlobalHazeColor.a). This is the fog's opacity at this pixel.


                // ---- 5. COMPOSITE — SELECT, NEVER BLEND ---------------------------------
                // Every pixel must end up as either the lit sprite colour or the haze
                // colour, both already on-palette. A lerp between them produces an
                // off-palette in-between, and snapping the result afterwards is the
                // thing the conventions forbid.
                //
                // Compare AquilaBayerThreshold against the haze amount and pick one.
                //
                // The threshold needs WORLD pixels — positionWS.xy * _GlobalPixelsPerUnit
                // — because parallax layers move relative to the camera. Screen-space
                // anchoring would make the dither crawl across the mountains as you pan.


                return c;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}