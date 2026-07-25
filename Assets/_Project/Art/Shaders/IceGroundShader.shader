Shader "Custom/IceGroundShader"
{
    Properties
    {
        [MainColor] _TopColor ("Pixel Ice Top Color", Color) = (0.45, 0.85, 1.0, 1.0)
        _BottomColor ("Pixel Ice Bottom Color", Color) = (0.08, 0.18, 0.45, 1.0)
        _RimColor ("Pixel Rim Highlight (Top)", Color) = (0.92, 0.98, 1.0, 1.0)
        _RimThickness ("Pixel Rim Thickness", Range(0.0, 0.3)) = 0.08

        [Header(Pixel Art Retro Settings)]
        _PixelsPerUnit ("Pixels Per Unit (Grid Resolution)", Float) = 16.0
        _ColorBands ("Color Bands (Posterization)", Range(2, 16)) = 5
        _IceNoiseScale ("Pixel Pattern Scale", Range(1.0, 50.0)) = 10.0
        _IceNoiseStrength ("Pixel Dither Intensity", Range(0.0, 1.0)) = 0.25

        [Header(Sparkle Animation Settings)]
        _SparkleColor ("Sparkle Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SparkleSpeed ("Sparkle Speed", Range(0.1, 10.0)) = 2.5
        _SparkleDensity ("Sparkle Density", Range(0.001, 0.2)) = 0.03
        _SparkleIntensity ("Sparkle Intensity", Range(0.0, 5.0)) = 1.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "Queue"="Geometry" 
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite On

        Pass
        {
            Name "PixelIcePass"

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
                float4 _TopColor;
                float4 _BottomColor;
                float4 _RimColor;
                float _RimThickness;
                float _PixelsPerUnit;
                float _ColorBands;
                float _IceNoiseScale;
                float _IceNoiseStrength;
                float4 _SparkleColor;
                float _SparkleSpeed;
                float _SparkleDensity;
                float _SparkleIntensity;
            CBUFFER_END

            // Pseudo-random pixel dithering generator
            float PixelHash(float2 p)
            {
                p = floor(p);
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // 1. Snap World Position to Pixel Art Grid
                float PPU = max(1.0, _PixelsPerUnit);
                float2 pixelWorldPos = floor(input.worldPos.xy * PPU) / PPU;

                // 2. Posterized Depth Gradient (Pixel Color Bands)
                float rawDepth = saturate(1.0 - input.uv.y);
                float depthFactor = floor(rawDepth * _ColorBands) / max(1.0, _ColorBands - 1.0);
                float4 baseColor = lerp(_TopColor, _BottomColor, depthFactor);

                // 3. Pixel Dither Pattern
                float2 noiseCoord = pixelWorldPos * _IceNoiseScale;
                float dither = PixelHash(noiseCoord);
                if (dither > 0.72)
                {
                    baseColor.rgb += _IceNoiseStrength * float3(0.35, 0.65, 0.95);
                }

                // 4. Sharp Pixel Rim Border on Top Surface
                float pixelUVY = floor(input.uv.y * (PPU * 0.5)) / (PPU * 0.5);
                float rimMask = step(1.0 - _RimThickness, pixelUVY);
                float4 finalColor = lerp(baseColor, _RimColor, rimMask);

                // 5. Animated Pixel Sparkle / Glint
                float2 gridPos = floor(input.worldPos.xy * PPU);
                float randSeed = PixelHash(gridPos + float2(17.1, 43.3));
                float canSparkle = step(1.0 - _SparkleDensity, randSeed);
                float phase = PixelHash(gridPos + float2(73.7, 19.9)) * 6.28318;
                float cycle = sin(_Time.y * _SparkleSpeed + phase) * 0.5 + 0.5;
                float glint = pow(cycle, 12.0) * canSparkle * _SparkleIntensity;

                finalColor.rgb += _SparkleColor.rgb * glint;

                return finalColor;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _TopColor;
            fixed4 _BottomColor;
            fixed4 _RimColor;
            float _RimThickness;
            float _PixelsPerUnit;
            float _ColorBands;
            float _IceNoiseScale;
            float _IceNoiseStrength;
            fixed4 _SparkleColor;
            float _SparkleSpeed;
            float _SparkleDensity;
            float _SparkleIntensity;

            float PixelHash(float2 p)
            {
                p = floor(p);
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float PPU = max(1.0, _PixelsPerUnit);
                float2 pixelPos = floor(i.worldPos.xy * PPU) / PPU;
                float rawDepth = saturate(1.0 - i.uv.y);
                float depthFactor = floor(rawDepth * _ColorBands) / max(1.0, _ColorBands - 1.0);
                fixed4 baseColor = lerp(_TopColor, _BottomColor, depthFactor);

                float2 noiseCoord = pixelPos * _IceNoiseScale;
                float dither = PixelHash(noiseCoord);
                if (dither > 0.72)
                {
                    baseColor.rgb += _IceNoiseStrength * float3(0.35, 0.65, 0.95);
                }

                float pixelUVY = floor(i.uv.y * (PPU * 0.5)) / (PPU * 0.5);
                float rimMask = step(1.0 - _RimThickness, pixelUVY);
                fixed4 finalColor = lerp(baseColor, _RimColor, rimMask);

                float2 gridPos = floor(i.worldPos.xy * PPU);
                float randSeed = PixelHash(gridPos + float2(17.1, 43.3));
                float canSparkle = step(1.0 - _SparkleDensity, randSeed);
                float phase = PixelHash(gridPos + float2(73.7, 19.9)) * 6.28318;
                float cycle = sin(_Time.y * _SparkleSpeed + phase) * 0.5 + 0.5;
                float glint = pow(cycle, 12.0) * canSparkle * _SparkleIntensity;

                finalColor.rgb += _SparkleColor.rgb * glint;

                return finalColor;
            }
            ENDCG
        }
    }
}
