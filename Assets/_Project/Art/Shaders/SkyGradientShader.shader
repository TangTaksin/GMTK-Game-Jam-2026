Shader "Custom/SkyGradientShader"
{
    Properties
    {
        _TopColor ("Top Sky Color", Color) = (0.08, 0.12, 0.28, 1.0)
        _MiddleColor ("Middle Sky Color", Color) = (0.22, 0.28, 0.48, 1.0)
        _BottomColor ("Bottom Horizon Color", Color) = (0.55, 0.72, 0.88, 1.0)
        _MiddlePosition ("Middle Position", Range(0.0, 1.0)) = 0.55
        _Exponent ("Gradient Smoothness", Range(0.2, 3.0)) = 1.0
        _DitherAmount ("Pixel Dither Amount", Range(0.0, 0.05)) = 0.008
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" }
        LOD 100
        ZWrite Off
        Cull Off

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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _TopColor;
            fixed4 _MiddleColor;
            fixed4 _BottomColor;
            float _MiddlePosition;
            float _Exponent;
            float _DitherAmount;

            // Simple 2D Pseudo Dither noise for crisp pixel art feel
            float PseudoDither(float2 screenPos)
            {
                float2 k = float2(12.9898, 78.233);
                return frac(sin(dot(screenPos, k)) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = saturate(i.uv.y);

                // Two-stage gradient interpolation (Bottom -> Middle -> Top)
                fixed4 color;
                if (t < _MiddlePosition)
                {
                    float factor = pow(t / max(0.001, _MiddlePosition), _Exponent);
                    color = lerp(_BottomColor, _MiddleColor, factor);
                }
                else
                {
                    float factor = pow((t - _MiddlePosition) / max(0.001, (1.0 - _MiddlePosition)), _Exponent);
                    color = lerp(_MiddleColor, _TopColor, factor);
                }

                // Apply subtle pixel art dithering to prevent harsh color banding
                float dither = (PseudoDither(i.uv * 512.0) - 0.5) * _DitherAmount;
                color.rgb += dither;

                return color;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
