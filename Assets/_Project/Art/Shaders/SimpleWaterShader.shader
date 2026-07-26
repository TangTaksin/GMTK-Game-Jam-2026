Shader "Custom/SimpleWaterShader"
{
    Properties
    {
        [Header(PixelArtSettings)]
        _PixelsPerUnit ("Pixel Grid Resolution (PPU)", Float) = 16.0
        _ColorBands ("Color Bands (Posterization)", Range(2, 16)) = 6
        _PixelTimeFPS ("Pixel Animation FPS", Range(1.0, 30.0)) = 8.0
        _EnablePixelGrid ("Enable Pixel UV Snapping", Range(0, 1)) = 1

        [Header(WaterColors)]
        _ShallowColor ("Shallow Water Color (Top)", Color) = (0.25, 0.8, 0.95, 0.85)
        _DeepColor ("Deep Water Color (Bottom)", Color) = (0.05, 0.25, 0.55, 0.95)

        [Header(SurfaceFoamHighlights)]
        _FoamColor ("Foam / Highlight Color", Color) = (0.9, 0.98, 1.0, 0.9)
        _FoamHeight ("Foam Thickness (Top Surface)", Range(0.01, 0.3)) = 0.08
        _FoamNoiseScale ("Foam Wave Scale", Range(1.0, 50.0)) = 15.0
        _FoamSpeed ("Foam Wave Speed", Range(0.1, 5.0)) = 1.5

        [Header(WaveAnimation)]
        _WaveSpeed ("Wave Speed", Range(0.1, 5.0)) = 1.2
        _WaveScale ("Wave Scale / Frequency", Range(1.0, 40.0)) = 10.0
        _WaveStrength ("Wave Distortion Amount", Range(0.0, 0.1)) = 0.025

        [Header(RefractionSpecularGlint)]
        _GlintColor ("Sunlight Glint Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _GlintSpeed ("Glint Speed", Range(0.1, 10.0)) = 2.0
        _GlintScale ("Glint Scale / Density (Higher = Smaller)", Range(1.0, 50.0)) = 16.0
        _GlintThreshold ("Glint Sparkle Threshold", Range(0.7, 0.99)) = 0.94
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SimpleWaterPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _PixelsPerUnit;
                float _ColorBands;
                float _PixelTimeFPS;
                float _EnablePixelGrid;
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float _FoamHeight;
                float _FoamNoiseScale;
                float _FoamSpeed;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveStrength;
                float4 _GlintColor;
                float _GlintSpeed;
                float _GlintScale;
                float _GlintThreshold;
            CBUFFER_END

            // Pseudo-random noise function
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth 2D noise for organic water ripples
            float noise2D(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Stepped pixel time for vertex wave animation
                float pixelTime = floor(_Time.y * _PixelTimeFPS) / max(1.0, _PixelTimeFPS);
                float waveOffset = sin(pixelTime * _WaveSpeed + input.positionOS.x * _WaveScale) * _WaveStrength;
                
                float4 pos = input.positionOS;
                pos.y += waveOffset;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(pos.xyz);
                output.positionCS = positionInputs.positionCS;
                output.worldPos = positionInputs.positionWS;
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 0. Stepped time & Pixel Grid UV Snapping
                float pixelTime = floor(_Time.y * _PixelTimeFPS) / max(1.0, _PixelTimeFPS);
                
                float ppu = max(1.0, _PixelsPerUnit);
                float2 pixelUV = lerp(input.uv, floor(input.uv * ppu) / ppu, step(0.5, _EnablePixelGrid));
                float2 pixelWorld = floor(input.worldPos.xy * ppu) / ppu;

                // 1. Calculate pixelated wave distortion for UVs
                float wave1 = sin(pixelWorld.x * _WaveScale + pixelTime * _WaveSpeed);
                float wave2 = cos(pixelWorld.y * _WaveScale * 0.8 - pixelTime * _WaveSpeed * 1.2);
                float2 distortedUV = pixelUV + float2(wave1, wave2) * _WaveStrength;

                // 2. Pixelated Depth Gradient with Posterized Color Bands
                float depthFactor = saturate(1.0 - distortedUV.y);
                float posterizedDepth = floor(depthFactor * _ColorBands) / max(1.0, _ColorBands);
                half4 waterBaseColor = lerp(_ShallowColor, _DeepColor, posterizedDepth);

                // 3. Pixelated Top Edge Surface Foam
                float2 foamUV = floor(distortedUV * _FoamNoiseScale) + float2(pixelTime * _FoamSpeed, pixelTime * 0.5);
                float foamNoise = noise2D(foamUV);
                float topDistortion = (foamNoise - 0.5) * 0.05;
                float foamMask = step(1.0 - _FoamHeight + topDistortion, pixelUV.y);

                half4 colorWithFoam = lerp(waterBaseColor, _FoamColor, foamMask * _FoamColor.a);

                // 4. Pixelated Sunlight Glint / Sparkle Effect
                float2 glintUV = floor(pixelWorld * _GlintScale + float2(pixelTime * _GlintSpeed, sin(pixelTime) * 0.5));
                float glintNoise = noise2D(glintUV);
                float glint = step(_GlintThreshold, glintNoise);

                half4 finalColor = colorWithFoam + _GlintColor * glint * (1.0 - foamMask);

                // 5. Final Color Posterization for Retro Pixel Art Palette Look
                finalColor.rgb = floor(finalColor.rgb * _ColorBands) / max(1.0, _ColorBands);

                return finalColor;
            }
            ENDHLSL
        }
    }
}
