Shader "Sprites/VR-Optimized/HorizontalSlice"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TopCutoff ("Top Cutoff", Range(0, 1)) = 0.2
        _BottomCutoff ("Bottom Cutoff", Range(0, 1)) = 0.2
        [Toggle] _HardEdges ("Hard Edges", Float) = 1
        _EdgeSmoothing ("Edge Smoothing", Range(0, 0.1)) = 0.01
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
            float _TopCutoff;
            float _BottomCutoff;
            float _HardEdges;
            float _EdgeSmoothing;

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
                
                float y = IN.texcoord.y;
                
                // Maske für die obere und untere Beschneidung
                float topMask, bottomMask;
                
                if (_HardEdges > 0.5) {
                    // Harte Kanten
                    topMask = step(y, 1.0 - _TopCutoff);
                    bottomMask = step(_BottomCutoff, y);
                } else {
                    // Weiche Kanten
                    topMask = smoothstep(1.0 - _TopCutoff, 1.0 - _TopCutoff + _EdgeSmoothing, y);
                    topMask = 1.0 - topMask;
                    
                    bottomMask = smoothstep(_BottomCutoff - _EdgeSmoothing, _BottomCutoff, y);
                }
                
                // Kombinierte Maske anwenden
                float combinedMask = topMask * bottomMask;
                
                // Alpha-Maske anwenden
                color.a *= combinedMask;
                
                return color;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}