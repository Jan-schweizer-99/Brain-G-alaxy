Shader "Custom/CrossPlaneShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (0, 0.5, 0.5, 0.5)
        _BorderColor ("Border Color", Color) = (1, 0.5, 0, 1)
        _BorderWidth ("Border Width", Range(0, 0.1)) = 0.02
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

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BorderColor;
                float _BorderWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                // Berechne den Abstand zum Rand
                float2 border = abs(input.uv - 0.5) * 2;
                float borderMask = max(border.x, border.y);
                float borderEffect = step(1 - _BorderWidth * 2, borderMask);
                
                // Kombiniere Hauptfarbe und Randfarbe
                float4 finalColor = lerp(_Color, _BorderColor, borderEffect);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}