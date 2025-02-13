Shader "Custom/VRSpacePointShader"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 0.05
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        // Deaktiviere Culling
        Cull Off
        // Stelle sicher, dass ZWrite aktiviert ist
        ZWrite On
        // Setze einen konservativen ZTest
        ZTest LEqual

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float eyeIndex : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _PointSize;
            StructuredBuffer<float4> _PositionBuffer;

            v2g vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                float4 worldPos = _PositionBuffer[instanceID];
                o.vertex = worldPos;
                return o;
            }

            [maxvertexcount(4)]
            void geom(point v2g input[1], inout TriangleStream<g2f> triStream)
            {
                UNITY_SETUP_INSTANCE_ID(input[0]);
                
                float4 worldPos = input[0].vertex;
                float4 viewPos = mul(UNITY_MATRIX_V, worldPos);
                
                // Verwende eine konstante Größe im Screen-Space
                float halfSize = _PointSize;
                
                // Berechne die Quad-Vertices im View-Space
                float4 vertices[4];
                vertices[0] = viewPos + float4(-halfSize, -halfSize, 0, 0);
                vertices[1] = viewPos + float4(halfSize, -halfSize, 0, 0);
                vertices[2] = viewPos + float4(-halfSize, halfSize, 0, 0);
                vertices[3] = viewPos + float4(halfSize, halfSize, 0, 0);

                float2 uvs[4] = {
                    float2(0, 0),
                    float2(1, 0),
                    float2(0, 1),
                    float2(1, 1)
                };

                g2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                [unroll]
                for(int i = 0; i < 4; i++)
                {
                    o.vertex = mul(UNITY_MATRIX_P, vertices[i]);
                    o.uv = uvs[i];
                    o.worldPos = worldPos;
                    o.eyeIndex = unity_StereoEyeIndex;
                    triStream.Append(o);
                }
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // Kreisförmiger Punkt mit weichen Kanten
                float2 center = i.uv - float2(0.5, 0.5);
                float dist = length(center);
                float alpha = 1 - smoothstep(0.45, 0.5, dist);
                clip(alpha - 0.01);
                
                return float4(1, 1, 1, alpha);
            }
            ENDCG
        }
    }
}