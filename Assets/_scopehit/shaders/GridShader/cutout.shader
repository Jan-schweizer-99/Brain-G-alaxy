Shader "Custom/VRGridPointsShader"
{
    Properties
    {
        _ParticleTexture ("Particle Texture", 2D) = "white" {}
        _GridSize ("Grid Size", Float) = 10
        _ParticleSize ("Particle Size", Range(0.01, 0.2)) = 0.05
        _Color ("Particle Color", Color) = (1,1,1,1)
        _GridDepth ("Max Grid Depth", Range(1, 100)) = 10
        _Transparency ("Transparency", Range(0, 1)) = 0.8
        _CursorPosition ("Cursor Position", Vector) = (0,0,0,1)
        _GridRadius ("Grid Radius", Float) = 5
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
        }
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma multi_compile_stereo
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_ParticleTexture);
            SAMPLER(sampler_ParticleTexture);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _ParticleTexture_ST;
                float _GridSize;
                float _ParticleSize;
                float4 _Color;
                float _GridDepth;
                float _Transparency;
                float4 _CursorPosition;
                float _GridRadius;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _ParticleTexture);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                return output;
            }
            
            float3 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 invRaydir) {
                float3 t0 = (boundsMin - rayOrigin) * invRaydir;
                float3 t1 = (boundsMax - rayOrigin) * invRaydir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                
                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(min(tmax.x, tmax.y), tmax.z);
                
                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float3(dstToBox, dstInsideBox, dstB);
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float3 viewDir = normalize(input.positionWS - _WorldSpaceCameraPos);
                
                // Calculate bounds based on cursor position
                float3 boundsMin = _CursorPosition.xyz - float3(_GridRadius, _GridRadius, _GridRadius);
                float3 boundsMax = _CursorPosition.xyz + float3(_GridRadius, _GridRadius, _GridRadius);
                
                float3 rayDir = normalize(input.positionWS - _WorldSpaceCameraPos);
                float3 invRayDir = 1.0 / rayDir;
                
                float3 boxInfo = rayBoxDst(boundsMin, boundsMax, _WorldSpaceCameraPos, invRayDir);
                float dstToBox = boxInfo.x;
                float dstInsideBox = boxInfo.y;
                
                float stepSize = 0.1;
                float3 rayStep = rayDir * stepSize;
                float3 currentPos = _WorldSpaceCameraPos + rayDir * dstToBox;
                
                float4 finalColor = float4(0, 0, 0, 0);
                
                for (int i = 0; i < 100; i++) {
                    if (length(currentPos - _WorldSpaceCameraPos) > dstToBox + dstInsideBox) break;
                    
                    // Calculate distance to cursor
                    float distToCursor = length(currentPos - _CursorPosition.xyz);
                    
                    // Only show grid points within the radius
                    if (distToCursor <= _GridRadius) {
                        float3 gridPos = currentPos * _GridSize;
                        float3 fractPos = frac(gridPos);
                        
                        float3 distToIntersection = abs(fractPos - 0.5);
                        float isIntersection = step(max(max(distToIntersection.x, distToIntersection.y), 
                                                      distToIntersection.z), _ParticleSize);
                        
                        if (isIntersection > 0) {
                            float2 particleUV = float2(
                                (distToIntersection.x / _ParticleSize + 0.5),
                                (distToIntersection.y / _ParticleSize + 0.5)
                            );
                            
                            float4 pointColor = _Color;
                            // Fade based on distance to cursor
                            float fadeFactor = 1.0 - (distToCursor / _GridRadius);
                            pointColor.a *= _Transparency * fadeFactor;
                            
                            finalColor = lerp(finalColor, pointColor, pointColor.a * (1 - finalColor.a));
                        }
                    }
                    
                    currentPos += rayStep;
                }
                
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}