Shader "Custom/XAxisSliceShaderWithPatterns"
{
    Properties
    {
        _SliceAmount ("Slice Amount", Range(0, 1)) = 0.5 // Controls the slice amount along the X-axis
        _NeonColor ("Neon Color", Color) = (1, 0, 0, 1) // Color of the neon effect
        _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1) // Color of the emission effect
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 1 // Intensity of the emission
        _MainTex ("Texture", 2D) = "white" {} // Main texture for the shader
        _Amplitude ("Amplitude", Range(0, 0.2)) = 0.05 // Amplitude of the patterns
        _Speed ("Speed", Range(0, 5)) = 1.0 // Speed of pattern movement
        _Frequency ("Frequency", Range(0.1, 2.0)) = 0.5 // Frequency of the patterns
        _TimeOffset ("Time Offset", Float) = 0.0 // Offset for time-based patterns
        _Pattern ("Pattern", Int) = 0 // 0 = Sinus, 1 = Zick-Zack, 2 = Dreieck
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 position : TEXCOORD1;
            };

            sampler2D _MainTex;
            float _SliceAmount;
            float4 _NeonColor;
            float4 _EmissionColor;
            float _EmissionIntensity;
            float _Amplitude;
            float _Speed;
            float _Frequency;
            float _TimeOffset;
            int _Pattern;

            // Funktion zur Musterberechnung
            float calculatePattern(float y, float time)
            {
                if (_Pattern == 0)
                {
                    // Sinus-Wellenmuster
                    return sin(y * _Frequency + time) * _Amplitude;
                }
                else if (_Pattern == 1)
                {
                    // Zick-Zack-Muster (scharfe Spitzen)
                    return (frac(y * _Frequency + time) > 0.5 ? 1.0 : -1.0) * _Amplitude;
                }
                else if (_Pattern == 2)
                {
                    // Dreieck-Muster
                    return abs(frac(y * _Frequency + time) * 2.0 - 1.0) * _Amplitude;
                }
                return 0.0;
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.position = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Zeitabhängigkeit zur Animation der Musterbewegung
                float time = _Time.y * _Speed + _TimeOffset;

                // Berechnung des Schnitts entlang der X-Achse basierend auf dem gewählten Muster
                float lightningEffect = calculatePattern(i.position.y, time);

                // Berechnung der Schnittlinie mit Verzerrung
                float cutLine = lerp(-1, 1, _SliceAmount) + lightningEffect;
                float alpha = step(i.position.x, cutLine);

                // Texturfarbe
                fixed4 col = tex2D(_MainTex, i.uv);

                // Neonstreifen an der Schnittkante
                if (abs(i.position.x - cutLine) < 0.01)
                {
                    col = _NeonColor * _EmissionIntensity;
                }

                // Transparenz anwenden
                col.a *= alpha; // Nur die geschnittene Fläche wird transparent

                // Alpha-Test, um fast transparente Bereiche zu verwerfen
                if (col.a < 0.01)
                    discard;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent"
}
