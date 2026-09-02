Shader "Aquila/Water"
{
    Properties
    {
        [Header(Body)]
        _BaseColour         ("Base Colour: the base colour of the water, authored on palette. Darkened and lightened according to GlobalDarkness.", Color) = (0.09, 0.14, 0.28, 1)

        [Header(Reflection)]
        _ReflectionStrength ("Reflection Strength: how much of the mirror render shows through over the base. 0 for opaque water, 1 for a perfect mirror.", Range(0,1)) = 0.55
        _ReflectionFade     ("Reflection Fade Depth (uv): how far down the plane the reflection survives in uv units. 0 and nothing is reflected, 1 and everything is reflected, regardless of depth.", Range(0.01, 1)) = 0.35
        _ReflectionSquash   ("Reflection Squash: vertical compression applied to the reflection about the waterline.", Range(0,1)) = 0.9
        _WaveAmpPixels      ("Wave Amplitude (px): how many pixels the reflection sample is displaced horizontally when waves are at their peak.", Float) = 1
        _WaveFreq           ("Wave Frequency: how many wave cycles fit vertically down the plane. Higher means tighter ripples.", Float) = 45
        _WaveSpeed          ("Wave Speed: how quickly the wobble animates.", Float) = 1.5

        [Header(Bands)]
        _BandDensity        ("Lit Row Fraction: fraction of rows eligible to carry bands. 0.2 means roughly one row in five has a band.", Range(0,1)) = 0.2
        _MaxBandOffset       ("Max Band Offset (steps): the maximum number of lighting steps a band can take a pixel along its ramp.", Range(0,4)) = 2
        _DashLength         ("Dash Length: the fraction of each cycle that's lit rather than gap. 0.35 means dashes occupy about a third of their period.", Range(0.05, 0.9)) = 0.35
        _BandFreqFar        ("Band Frequency (far): dash spacing at the far edge (furthest from the camera, near the actual scene height).", Float) = 0.06
        _BandFreqNear       ("Band Frequency (near): dash spacing at the close edge (closest to the camera).", Float) = 0.12
        _BandSpeedFar       ("Scroll Speed (far): how quickly dashes scroll when far from the camera. Should be lower than the near speed to give depth perception.", Float) = 0.4
        _BandSpeedNear      ("Scroll Speed (near): how quickly dashes scroll when close to the camera. Again, highest scroll speed should be here because it's closest to the camera.", Float) = 1.2
        _EdgeWidth          ("Edge width: how tall the waterline highlight zone is in uv units.", Range(0.005, 0.2)) = 0.03
        _EdgeOffset         ("Edge offset: the extra palette steps given at the waterline, allowing the top edge to read as a waterline.", Range(0, 6)) = 3
        _ClumpFreq          ("Clump Frequency: spatial frequency of the clumping wave.", Float) = 0.05
        _ClumpMin           ("Minimum Clump", Float) = 0.2
        _ClumpDrift         ("Clump Drift", Float) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Transparent"
        }

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

            TEXTURE2D(_WaterReflectionTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColour;
                float  _ReflectionStrength;
                float  _ReflectionFade;
                float  _ReflectionSquash;
                float  _WaveAmpPixels;
                float  _WaveFreq;
                float  _WaveSpeed;
                float  _BandDensity;
                float  _MaxBandOffset;
                float  _DashLength;
                float  _BandFreqFar;
                float  _BandFreqNear;
                float  _BandSpeedFar;
                float  _BandSpeedNear;
                float  _EdgeWidth;
                float  _EdgeOffset;
                float  _ClumpFreq;
                float  _ClumpMin;
                float  _ClumpDrift;
            CBUFFER_END

            float4 _WaterReflectionTexelSize;
            float  _WaterlineScreenUV;

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
                float3 positionWS : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.uv         = IN.uv;
                OUT.screenPos  = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // depth: 0 at the waterline, 1 at the bottom of the plane. Doubles as the
                // near/far axis for band perspective where the waterline is the far edge.
                float depth = saturate(1.0 - IN.uv.y);

                // Reflection code: reads the 0, 1 screen coordinates of this pixel then determines the pixel position after applying the squash to vertically compress reflections
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                screenUV.y = _WaterlineScreenUV + (screenUV.y - _WaterlineScreenUV) / _ReflectionSquash;

                // Horizontal wobble, rounded to whole texels so it stays pixel-crisp.
                float wavePx = sin(IN.uv.y * _WaveFreq + _Time.y * _WaveSpeed) * _WaveAmpPixels;
                screenUV.x += round(wavePx) * _WaterReflectionTexelSize.x;
                
                // Our water colour is on the _BaseColour ramp, so shift it up or down that ramp depending on how dark it currently is so the river darkens at night
                half4 refl = SAMPLE_TEXTURE2D(_WaterReflectionTex, sampler_point_clamp, screenUV);
                half3 baseCol = LightWithPaletteHard(_BaseColour.rgb, _GlobalDarkness);
                
                // Reflections should fade at the near bank
                half fade = saturate(1.0 - depth / _ReflectionFade);

                float2 worldPx = IN.positionWS.xy * PIXELS_PER_UNIT;
                float row = floor(worldPx.y);
                
                float edge = saturate(1.0 - depth / _EdgeWidth);
                
                float clump = 0.5 + 0.5 * sin(row * _ClumpFreq + _Time.y * _ClumpDrift);
                float density = lerp(_BandDensity, 1.0, edge) * lerp(_ClumpMin, 1.0, clump);
                
                // Randomised position of water distortion rows
                float rowHash  = frac(sin(row * 127.1) * 43758.5453);
                float rowIsLit = step(rowHash, density);
                
                // Randomised frequency of rows so different z positions in the river get different distributions of bands
                float freqHash = frac(sin(row * 311.7) * 24634.6345);
                float freq = lerp(_BandFreqFar, _BandFreqNear, depth) * lerp(0.6, 1.6, freqHash);
                
                float speed = lerp(_BandSpeedFar, _BandSpeedNear, depth);

                float phase = worldPx.x * freq + row * 7.3 + _Time.y * speed;
                
                // How many steps we will offset this band by from its original ramp position
                float offsetHash = frac(sin(row * 74.3 + floor(phase) * 19.1) * 27183.9);
                float steps = _MaxBandOffset * lerp(0.5, 2.0, offsetHash) + edge * _EdgeOffset;
                
                // Random length of dashes
                float dashHash = frac(sin((row * 127.1 + floor(phase)) * 91.7) * 43758.5453);
                float lit = step(frac(phase), _DashLength * lerp(0.3, 1.4, dashHash)) * rowIsLit;
                
                half3 litBase = baseCol;
                
                half3 litRefl = refl.rgb;

                if (lit > 0.0)
                {
                    litBase = ShiftPaletteSteps(baseCol,  steps);
                    litRefl = ShiftPaletteSteps(refl.rgb, steps);
                }

                half3 col = lerp(litBase, litRefl, refl.a * _ReflectionStrength * fade);
                
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}