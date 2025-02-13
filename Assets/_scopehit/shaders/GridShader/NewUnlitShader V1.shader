Shader "Custom/CrossGridGeometryShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (0, 0.5, 0.5, 0.5)
        _BorderColor ("Border Color", Color) = (1, 0.5, 0, 1)
        _BorderWidth ("Border Width", Range(0, 0.1)) = 0.02
        _GridSize ("Grid Size", Vector) = (3, 3, 3, 0) // Grid Size auf 3x3x3 gesetzt
        _Spacing ("Grid Spacing", Float) = 1.1
        _PlaneSize ("Plane Size", Float) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct GeomData
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float facing : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BorderColor;
                float _BorderWidth;
                float4 _GridSize;
                float _Spacing;
                float _PlaneSize;
            CBUFFER_END

            Attributes vert(Attributes input)
            {
                return input;
            }

            void CreatePlane(float3 centerPos, float3 normal, float3 tangent, float facing,
                inout TriangleStream<GeomData> triStream)
            {
                float3 bitangent = cross(normal, tangent);
                float halfSize = _PlaneSize * 0.5;

                GeomData vertices[4];
                float3 positions[4] = {
                    centerPos - tangent * halfSize - bitangent * halfSize,
                    centerPos + tangent * halfSize - bitangent * halfSize,
                    centerPos - tangent * halfSize + bitangent * halfSize,
                    centerPos + tangent * halfSize + bitangent * halfSize
                };

                for(int i = 0; i < 4; i++)
                {
                    vertices[i].positionCS = TransformWorldToHClip(positions[i]);
                    vertices[i].uv = float2(i & 1, (i >> 1) & 1);
                    vertices[i].normalWS = normal;
                    vertices[i].facing = facing;
                    triStream.Append(vertices[i]);
                }
                triStream.RestartStrip();
            }

            // Erhöhe die Maximalgröße des Grids auf 4x4x4
            #define MAX_GRID_SIZE 4

            [maxvertexcount(96)]
            void geom(point Attributes input[1], inout TriangleStream<GeomData> triStream)
            {
                // Konvertiere _GridSize in ganze Zahlen
                int3 gridSize = min(int3(_GridSize.xyz), int3(MAX_GRID_SIZE, MAX_GRID_SIZE, MAX_GRID_SIZE));
                
                // Berechne den Offset relativ zum Objektursprung
                float3 offset = float3(
                    (gridSize.x - 1) * _Spacing * 0.5,
                    (gridSize.y - 1) * _Spacing * 0.5,
                    (gridSize.z - 1) * _Spacing * 0.5
                );
                
                // Hole die Weltposition des Objekts
                float3 objectPosition = GetObjectToWorldMatrix()._m03_m13_m23;
                
                // Ändere die Schleifenlogik, um die Y-Achse korrekt zu berücksichtigen
                for(int x = 0; x < gridSize.x; x++)
                {
                    for(int y = 0; y < gridSize.y; y++)
                    {
                        for(int z = 0; z < gridSize.z; z++)
                        {
                            // Berechne die lokale Position basierend auf dem Grid-Index
                            float3 localPosition = float3(
                                x * _Spacing - offset.x,
                                y * _Spacing - offset.y, // Jetzt Y-Achse korrekt berücksichtigen
                                z * _Spacing - offset.z
                            );
                            
                            // Transformiere in Weltkoordinaten
                            float3 worldPosition = mul(GetObjectToWorldMatrix(), float4(localPosition, 1.0)).xyz;
                            
                            float3 viewDir = normalize(_WorldSpaceCameraPos - worldPosition);
                            
                            // XY Plane
                            CreatePlane(worldPosition, float3(0, 0, 1), float3(1, 0, 0), 
                                dot(viewDir, float3(0, 0, 1)), triStream);
                            
                            // XZ Plane
                            CreatePlane(worldPosition, float3(0, 1, 0), float3(1, 0, 0),
                                dot(viewDir, float3(0, 1, 0)), triStream);
                            
                            // YZ Plane
                            CreatePlane(worldPosition, float3(1, 0, 0), float3(0, 1, 0),
                                dot(viewDir, float3(1, 0, 0)), triStream);
                        }
                    }
                }
            }

            half4 frag(GeomData input) : SV_Target
            {
                float facingValue = abs(input.facing);
                float alpha = lerp(0.2, 1.0, facingValue);

                float2 border = abs(input.uv - 0.5) * 2;
                float borderMask = max(border.x, border.y);
                float borderEffect = step(1 - _BorderWidth * 2, borderMask);
                
                float4 mainColor = _Color;
                mainColor.a *= alpha;
                float4 borderColorWithAlpha = _BorderColor;
                borderColorWithAlpha.a *= alpha;
                
                return lerp(mainColor, borderColorWithAlpha, borderEffect);
            }
            ENDHLSL
        }
    }
}
