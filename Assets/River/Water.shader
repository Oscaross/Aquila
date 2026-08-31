Shader "Aquila/Water"
{
    Properties
    {
        _BaseColour         ("Base Colour", Color) = (0.09, 0.14, 0.28, 1)
        _SurfaceColour      ("Surface Line Colour", Color) = (0.45, 0.62, 0.75, 1)
        _SurfaceThickness   ("Surface Line (px)", Float) = 2
        _ReflectionStrength ("Reflection Strength", Range(0,1)) = 0.55
        _ReflectionFade     ("Reflection Fade Depth (uv)", Range(0.01, 1)) = 0.35
        _WaveAmpPixels      ("Wave Amplitude (px)", Float) = 1
        _WaveFreq           ("Wave Frequency", Float) = 45
        _WaveSpeed          ("Wave Speed", Float) = 1.5
        _PlaneHeightPixels  ("Plane Height (px)", Float) = 180
        _SkyInfluence       ("Sky Influence", Range(0,1)) = 0.3
        _ReflectionSquash   ("Reflection Squash", Range(0,1)) = 0.9
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
                float4 _SurfaceColour;
                float  _SurfaceThickness;
                float  _ReflectionStrength;
                float  _ReflectionFade;
                float  _WaveAmpPixels;
                float  _WaveFreq;
                float  _WaveSpeed;
                float  _PlaneHeightPixels;
                float  _SkyInfluence;
                float  _ReflectionSquash;
            CBUFFER_END

            float4 _WaterReflectionTexelSize;
            float4 _SkyHorizonColour;
            float  _WaterlineScreenY; 
            float  _SunPositionX;

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
                // Depth below the surface, 0 at the waterline.
                float depth = saturate(1.0 - IN.uv.y);

                // Screen-space lookup into the mirrored render.
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                screenUV.y = _WaterlineScreenY + (screenUV.y - _WaterlineScreenY) / _ReflectionSquash;

                // Horizontal wobble, quantised to whole texels so it stays pixel-crisp.
                float wavePx = sin(IN.uv.y * _WaveFreq + _Time.y * _WaveSpeed) * _WaveAmpPixels;
                screenUV.x += round(wavePx) * _WaterReflectionTexelSize.x;

                half4 refl = SAMPLE_TEXTURE2D(_WaterReflectionTex, sampler_point_clamp, screenUV);
                refl.rgb = SnapToPalette(refl.rgb);
                
                // Composite: reflection over base, fading with depth.
                half fade = saturate(1.0 - depth / _ReflectionFade);
                half3 baseCol = lerp(_BaseColour.rgb, _SkyHorizonColour.rgb, _SkyInfluence); // applying the sky colour tinting to river surface

                half4 col = half4(baseCol, _BaseColour.a);
                col.rgb = lerp(col.rgb, refl.rgb, refl.a * _ReflectionStrength * fade);

                // Crisp surface line along the top edge.
                float linePx  = depth * _PlaneHeightPixels;
                float isLine  = 1.0 - step(_SurfaceThickness, linePx);
                col.rgb = lerp(col.rgb, _SurfaceColour.rgb, isLine);

                col.a = _BaseColour.a;
                
                return col;
            }
            ENDHLSL
        }
    }
}