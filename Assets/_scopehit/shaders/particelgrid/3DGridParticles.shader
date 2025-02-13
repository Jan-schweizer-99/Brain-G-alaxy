Shader "Custom/3DGridMesh"
{
    Properties
    {
        _GridSize ("Grid Size", Float) = 10
        _GridSpacing ("Grid Spacing", Float) = 1.0
        _LineWidth ("Line Width", Range(0.001, 0.1)) = 0.02
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _GridSize;
            float _GridSpacing;
            float _LineWidth;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 wp = i.worldPos.xyz;
                
                // Berechne Abstand zum nächsten Gitterpunkt für jede Achse
                float3 grid = abs(fmod(wp + _GridSpacing * 0.5, _GridSpacing) - _GridSpacing * 0.5);
                
                // Erstelle Linien, wenn der Abstand kleiner als die Linienbreite ist
                float3 lines = step(grid, float3(_LineWidth, _LineWidth, _LineWidth));
                
                // Kombiniere die Linien
                float lineVisibility = max(max(lines.x, lines.y), lines.z);
                
                // Färbe die Hauptachsen
                float3 color = _Color.rgb;
                if(abs(wp.x) < _LineWidth) color = float3(1,0,0); // X-Achse rot
                if(abs(wp.y) < _LineWidth) color = float3(0,1,0); // Y-Achse grün
                if(abs(wp.z) < _LineWidth) color = float3(0,0,1); // Z-Achse blau
                
                // Fade basierend auf der Entfernung vom Ursprung
                float fade = 1 - saturate(length(wp) / _GridSize);
                
                return float4(color, lineVisibility * fade * _Color.a);
            }
            ENDCG
        }
    }
}