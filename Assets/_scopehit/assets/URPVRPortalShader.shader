Shader "Custom/URPVRPortalShaderDebug"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PortalTex ("Portal View", 2D) = "black" {}
        _Mask ("Mask", 2D) = "white" {}
        [KeywordEnum(Normal, PortalOnly, MaskOnly, UV)] _DebugMode ("Debug Mode", Float) = 0
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #pragma multi_compile _DEBUGMODE_NORMAL _DEBUGMODE_PORTALONLY _DEBUGMODE_MASKONLY _DEBUGMODE_UV

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_PortalTex);
            SAMPLER(sampler_PortalTex);
            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                half4 portalColor = SAMPLE_TEXTURE2D(_PortalTex, sampler_PortalTex, IN.uv);
                half mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, IN.uv).r;
                
                half4 color;
                
                #if defined(_DEBUGMODE_NORMAL)
                    color = lerp(half4(0,0,0,0), portalColor, mask);
                #elif defined(_DEBUGMODE_PORTALONLY)
                    color = portalColor;
                #elif defined(_DEBUGMODE_MASKONLY)
                    color = half4(mask, mask, mask, 1);
                #elif defined(_DEBUGMODE_UV)
                    color = half4(IN.uv, 0, 1);
                #endif
                
                return color;
            }
            ENDHLSL
        }
    }
}