Shader "test/XAxisSliceShaderWithStereoTilingRotation"
{
    Properties
    {
        _SliceAmount ("Slice Amount", Range(0, 1)) = 0.5
        _NeonColor ("Neon Color", Color) = (1, 0, 0, 1)
        _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 1
        _MainTex ("Main Texture", 2D) = "white" {}
        _MainTexTiling ("Main Texture Tiling and Offset", Vector) = (1,1,0,0)
        _ParticleTex ("Particle Texture", 2D) = "white" {}
        _ParticleTexTiling ("Particle Tiling and Offset", Vector) = (1,1,0,0)
        _RotationXYZ ("Slice Rotation (XYZ)", Vector) = (0, 0, 0, 0)
        _Amplitude ("Amplitude", Range(0, 1)) = 0.05
        _Speed ("Speed", Range(0, 25)) = 1.0
        _Frequency ("Frequency", Range(0.1, 10.0)) = 0.5
        _TimeOffset ("Time Offset", Float) = 0.0
        _ParticleSize ("Particle Size", Range(0.01, 0.2)) = 0.05
        _ParticleFade ("Particle Fade", Range(0.01, 1.0)) = 0.5
        _Pattern ("Pattern", Int) = 0
        _StereoCameraOffset ("Stereo Camera Offset", Float) = 0.1
        _MeshMin ("Mesh Min Bounds (World)", Vector) = (-1, -1, -1, 0)
        _MeshMax ("Mesh Max Bounds (World)", Vector) = (1, 1, 1, 0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 position : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _ParticleTex;
            float4 _MainTexTiling;
            float4 _ParticleTexTiling;
            float3 _RotationXYZ;
            float _SliceAmount;
            float4 _NeonColor;
            float _EmissionIntensity;
            float _Amplitude;
            float _Speed;
            float _Frequency;
            float _TimeOffset;
            float _ParticleSize;
            float _ParticleFade;
            int _Pattern;
            float _StereoCameraOffset;
            float4 _MeshMin;
            float4 _MeshMax;

            float3 rotate3D(float3 position, float3 rotationRadians)
            {
                // Rotate around X axis
                float3x3 rotX = float3x3(
                    1, 0, 0,
                    0, cos(rotationRadians.x), -sin(rotationRadians.x),
                    0, sin(rotationRadians.x), cos(rotationRadians.x)
                );

                // Rotate around Y axis
                float3x3 rotY = float3x3(
                    cos(rotationRadians.y), 0, sin(rotationRadians.y),
                    0, 1, 0,
                    -sin(rotationRadians.y), 0, cos(rotationRadians.y)
                );

                // Rotate around Z axis
                float3x3 rotZ = float3x3(
                    cos(rotationRadians.z), -sin(rotationRadians.z), 0,
                    sin(rotationRadians.z), cos(rotationRadians.z), 0,
                    0, 0, 1
                );

                return mul(rotZ, mul(rotY, mul(rotX, position)));
            }

            float calculatePattern(float axis, float time)
            {
                if (_Pattern == 0)
                {
                    return sin(axis * _Frequency + time) * _Amplitude;
                }
                else if (_Pattern == 1)
                {
                    return (frac(axis * _Frequency + time) > 0.5 ? 1.0 : -1.0) * _Amplitude;
                }
                else if (_Pattern == 2)
                {
                    return abs(frac(axis * _Frequency + time) * 2.0 - 1.0) * _Amplitude;
                }
                return 0.0;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                
                #ifdef UNITY_SINGLE_PASS_STEREO
                    o.vertex.x += _StereoCameraOffset * (unity_StereoEyeIndex * 2 - 1);
                #endif

                o.uv = v.uv;
                o.position = v.vertex.xyz;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float time = _Time.y * _Speed + _TimeOffset;
                float3 rotationRadians = _RotationXYZ * 2.0 * UNITY_PI;

                float3 rotatedPos = rotate3D(i.position, rotationRadians);

                float3 meshSize = _MeshMax.xyz - _MeshMin.xyz;
                float sliceAxis = (rotatedPos.x - _MeshMin.x) / meshSize.x * 2.0 - 1.0;

                float lightningEffect = calculatePattern(rotatedPos.y, time);
                float cutLine = lerp(-1, 1, _SliceAmount) + lightningEffect;

                float alpha = step(sliceAxis, cutLine);

                float2 mainUV = frac(i.uv * _MainTexTiling.xy + _MainTexTiling.zw);
                float2 particleUV = frac(i.uv * _ParticleTexTiling.xy + _ParticleTexTiling.zw);

                fixed4 col = tex2D(_MainTex, mainUV); 

                if (abs(sliceAxis - cutLine) < 0.01)
                {
                    col = _NeonColor * _EmissionIntensity;
                }

                col.a *= alpha;

                float distanceToCut = abs(sliceAxis - cutLine);
                if (distanceToCut < _ParticleSize)
                {
                    float particleAlpha = smoothstep(_ParticleSize, 0, distanceToCut) * _ParticleFade;
                    fixed4 particleCol = tex2D(_ParticleTex, particleUV);
                    col.rgb = lerp(col.rgb, particleCol.rgb, particleAlpha);
                    col.a = max(col.a, particleAlpha);
                }

                if (col.a < 0.01)
                    discard;

                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent"
}
