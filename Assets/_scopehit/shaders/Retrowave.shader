Shader "Custom/RetrowaveWireframeVR"
{
    Properties
    {
        _WireframeColor ("Wireframe Color", Color) = (1, 0, 1, 1)
        _WireframeThickness ("Wireframe Thickness", Range(0, 1)) = 0.1
        _BackgroundColor ("Background Color", Color) = (0.5, 0, 0.5, 1)
        _FogColor ("Fog Color", Color) = (1, 0, 1, 1)
        _FogStart ("Fog Start", Float) = 10
        _FogEnd ("Fog End", Float) = 50
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float3 barycentricCoords : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _WireframeColor;
            float _WireframeThickness;
            float4 _BackgroundColor;
            float4 _FogColor;
            float _FogStart;
            float _FogEnd;

            v2g vert(appdata v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.uv;
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                UNITY_SETUP_INSTANCE_ID(IN[0]);
                
                g2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Emittiere die drei Vertices des Dreiecks mit baryzentrische Koordinaten
                o.vertex = IN[0].vertex;
                o.worldPos = IN[0].worldPos;
                o.uv = IN[0].uv;
                o.barycentricCoords = float3(1, 0, 0);
                UNITY_TRANSFER_FOG(o, o.vertex);
                triStream.Append(o);

                o.vertex = IN[1].vertex;
                o.worldPos = IN[1].worldPos;
                o.uv = IN[1].uv;
                o.barycentricCoords = float3(0, 1, 0);
                UNITY_TRANSFER_FOG(o, o.vertex);
                triStream.Append(o);

                o.vertex = IN[2].vertex;
                o.worldPos = IN[2].worldPos;
                o.uv = IN[2].uv;
                o.barycentricCoords = float3(0, 0, 1);
                UNITY_TRANSFER_FOG(o, o.vertex);
                triStream.Append(o);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Berechne Wireframe basierend auf baryzentrischen Koordinaten
                float minBary = min(min(i.barycentricCoords.x, i.barycentricCoords.y), i.barycentricCoords.z);
                float wireframe = step(minBary, _WireframeThickness);

                // Berechne Entfernungsbasierter Nebel
                float dist = length(i.worldPos.xyz - _WorldSpaceCameraPos);
                float fogFactor = saturate((dist - _FogStart) / (_FogEnd - _FogStart));

                // Farbverlauf für den Hintergrund basierend auf der Höhe
                float4 backgroundColor = lerp(_BackgroundColor, _FogColor, i.uv.y);
                
                // Kombiniere Wireframe mit Hintergrund
                float4 finalColor = lerp(backgroundColor, _WireframeColor, wireframe);
                
                // Wende Nebel an
                finalColor = lerp(finalColor, _FogColor, fogFactor);

                return finalColor;
            }
            ENDCG
        }
    }
}