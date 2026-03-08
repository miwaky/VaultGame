Shader "ShelterCommand/CRTScreen"
{
    Properties
    {
        _MainTex        ("Texture",           2D)       = "white" {}
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.15
        _VignetteStrength  ("Vignette Strength",  Range(0,1)) = 0.3
        _NoiseIntensity    ("Noise Intensity",    Range(0,0.05)) = 0.005
        _AnalogShift       ("Analog RGB Shift",   Range(0,0.01)) = 0.001
        _CRTTime           ("Time",               Float)      = 0
        _TintColor         ("Tint Color",          Color)     = (0.9, 1.0, 0.85, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _ScanlineIntensity;
            float _VignetteStrength;
            float _NoiseIntensity;
            float _AnalogShift;
            float _CRTTime;
            float4 _TintColor;

            // Simple pseudo-random hash
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 uv = i.uv;

                // Analog RGB shift
                float shift = _AnalogShift;
                fixed4 col;
                col.r = tex2D(_MainTex, uv + float2( shift, 0)).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv + float2(-shift, 0)).b;
                col.a = 1.0;

                // Scanlines
                float scanline = sin(uv.y * 800.0) * 0.5 + 0.5;
                col.rgb -= scanline * _ScanlineIntensity;

                // Noise
                float noise = hash(uv + frac(_CRTTime * 0.07));
                col.rgb += (noise - 0.5) * _NoiseIntensity;

                // Vignette
                float2 vig = uv * 2.0 - 1.0;
                float vignette = 1.0 - dot(vig, vig) * _VignetteStrength;
                col.rgb *= vignette;

                // Phosphor tint
                col.rgb *= _TintColor.rgb;

                return saturate(col);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
