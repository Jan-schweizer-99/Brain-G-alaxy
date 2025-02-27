Shader "Sprites/VR-Optimized/CircularMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Radius ("Circle Radius", Range(0, 1)) = 0.5
        _Center ("Circle Center", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            fixed4 _Color;
            float _Radius;
            float2 _Center;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                
                return OUT;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f IN) : SV_Target
            {
                // Textur mit der Tint-Farbe multiplizieren
                fixed4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Abstand vom Zentrum berechnen
                float dist = distance(IN.texcoord, _Center);
                
                // Maske erstellen: 1 innerhalb des Kreises, 0 außerhalb
                float mask = step(dist, _Radius);
                
                // Anwenden der Maske auf den Alpha-Kanal
                // Nur innerhalb des Kreises ist es sichtbar
                color.a *= mask;
                
                return color;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}