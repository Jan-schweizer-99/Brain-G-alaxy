Shader "Custom/BurnEffectLitShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _BurnSlider ("Burn Amount", Range(0, 1)) = 0.0
        _BurnColor ("Burn Color", Color) = (1, 0.5, 0.5, 1) // Farbe des verbrannten Materials
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _BurnSlider;
        fixed4 _BurnColor;
        float _Metallic;
        float _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Lade die Textur und setze die Farbe des Materials
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Abbrenn-Effekt basierend auf dem BurnSlider
            if (_BurnSlider > 0)
            {
                // Farbe ändern und Transparenz erhöhen
                c.rgb = lerp(c.rgb, _BurnColor.rgb, _BurnSlider);
                o.Albedo = c.rgb;
                o.Alpha = lerp(1.0, 0.0, _BurnSlider);
            }
            else
            {
                o.Albedo = c.rgb;
                o.Alpha = 1.0;
            }

            // Standardwerte für PBR-Material
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }

    FallBack "Standard"
}
