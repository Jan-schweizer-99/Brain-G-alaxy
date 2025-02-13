Shader "Custom/LightningShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _Color ("Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0, 1)) = 1.0
        _Frequency ("Frequency", Range(1, 10)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex;
        fixed4 _Color;
        float _Intensity;
        float _Frequency;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            // Time-based modulation
            float lightning = sin(_Time.y * _Frequency) * _Intensity;

            // Apply the lightning effect
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * lightning;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
