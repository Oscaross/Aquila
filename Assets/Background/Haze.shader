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
                c.a *= IN.color.a; // alpha only since an rgb tint here would feed a modified colour into the LUT and land it on the wrong ramp
                clip(c.a - _Cutoff);
                
                // Snap the RGB value to the correct ramp and lighting index based on the current global lighting level
                c.rgb = LightWithPaletteHard(c.rgb, _GlobalDarkness);
                
                float dy = abs(IN.positionWS.y - _GlobalHorizonY); // how many y world units above the horizon does this background sit?
                float ramp = saturate(1.0 - dy / max(_RampHeight, 0.0001)); // ramp is 1 at the horizon and 0 at _RampHeight
                ramp = pow(ramp, HAZE_RAMP_CURVE); // concentrate the haze towards the horizon line with exponential falloff above
                
                float hazeAmount = _LayerDepth + (1.0 - _LayerDepth) * ramp * HAZE_RAMP_STRENGTH; // baseline haze is the layer depth, add more haze to texels that are at the base of the mountain closer to the horizon
                
                float quantisedHaze = round(hazeAmount * _HazeSteps) / _HazeSteps; 
                // Recall that global haze alpha is the intensity of the haze, and is set by SkyController.
                float amount = saturate(quantisedHaze * _GlobalHazeColor.a); // snaps the quantised haze to 0-1 based on the global haze alpha
                float alpha = floor(amount * _PaletteAlphaSteps + 0.5) / _PaletteAlphaSteps;
                
                c.rgb = lerp(c.rgb, _GlobalHazeColor.rgb, alpha);
                return c;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}