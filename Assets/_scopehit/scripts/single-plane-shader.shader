Shader "Custom/SinglePlaneGridShader"
{
    Properties
    {
        _GridSize ("Grid Size", Float) = 1.0
        _PrimaryColor ("Primary Grid Color", Color) = (0.5, 0.5, 0.5, 0.3)
        _SecondaryColor ("Secondary Grid Color", Color) = (0.7, 0.7, 0.7, 0.15)
        _PrimarySpacing ("Primary Grid Spacing", Int) = 1
        _SecondarySpacing ("Secondary Grid Spacing", Int) = 5
        _FadeDistance ("Fade Distance", Float) = 50.0
        _GridThickness ("Grid Thickness", Float) = 0.01
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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
                float3 worldPos : TEXCOORD1;
                float4 objectPos : TEXCOORD2;
            };

            float _GridSize;
            float4 _PrimaryColor;
            float4 _SecondaryColor;
            int _PrimarySpacing;
            int _SecondarySpacing;
            float _FadeDistance;
            float _GridThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.objectPos = v.vertex;
                o.uv = v.uv * 2 - 1; // Skaliere UVs auf -1 bis 1
                return o;
            }

            float getGrid(float position, float spacing) {
                float scaledPos = position / _GridSize * spacing;
                float modulo = fmod(abs(scaledPos), spacing);
                return step(modulo, _GridThickness) || step(spacing - _GridThickness, modulo);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Berechne Distanz zur Kamera für Fade-Effekt
                float dist = length(_WorldSpaceCameraPos - i.worldPos);
                float fade = 1 - saturate(dist / _FadeDistance);

                // Primäres und sekundäres Grid
                float primaryGridX = getGrid(i.uv.x, _PrimarySpacing);
                float primaryGridY = getGrid(i.uv.y, _PrimarySpacing);
                float secondaryGridX = getGrid(i.uv.x, _SecondarySpacing);
                float secondaryGridY = getGrid(i.uv.y, _SecondarySpacing);

                // Kombiniere Grids
                float primaryGrid = max(primaryGridX, primaryGridY);
                float secondaryGrid = max(secondaryGridX, secondaryGridY);

                // Wähle Farbe basierend auf Grid-Typ
                float4 color = lerp(_SecondaryColor, _PrimaryColor, primaryGrid);
                color = lerp(color, _PrimaryColor, secondaryGrid);

                // Finale Farbe mit Fade
                float finalAlpha = (primaryGrid || secondaryGrid) * fade * color.a;
                return float4(color.rgb, finalAlpha);
            }
            ENDCG
        }
    }
}