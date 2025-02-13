Shader "Custom/SpacePointShader"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 0.05
        _BaseColor ("Base Color", Color) = (1,1,1,0.5)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 5.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 worldPos : TEXCOORD0;
                float4 color : COLOR;
                float size : PSIZE;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            StructuredBuffer<float4> _PositionBuffer;
            float _PointSize;
            float4 _BaseColor;

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Hole Position aus dem Buffer
                float4 worldPos = _PositionBuffer[instanceID];
                o.worldPos = worldPos;

                // Transformiere in Clip Space
                o.pos = UnityWorldToClipPos(worldPos);

                // Berechne Punktgröße basierend auf Entfernung
                float dist = length(_WorldSpaceCameraPos - worldPos.xyz);
                o.size = _PointSize * _ScreenParams.y / dist;

                // Setze Farbe basierend auf Position
                float3 color = _BaseColor.rgb;
                
                // Färbe Hauptachsen
                if (abs(worldPos.x) < 0.1) color = float3(1,0,0);
                if (abs(worldPos.y) < 0.1) color = float3(0,1,0);
                if (abs(worldPos.z) < 0.1) color = float3(0,0,1);

                o.color = float4(color, _BaseColor.a);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}