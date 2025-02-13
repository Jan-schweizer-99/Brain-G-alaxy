Shader "scopehit/Stereo3DShader"
{
    Properties
    {
        _LeftTex ("Left Eye Texture", 2D) = "white" {}
        _RightTex ("Right Eye Texture", 2D) = "white" {}
        _3DStrength ("3D Effect Strength", Range(-0.1, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _LeftTex;
            sampler2D _RightTex;
            float4 _LeftTex_ST;
            float4 _RightTex_ST;
            float _3DStrength;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                float2 uv = i.uv;
                
                if (unity_StereoEyeIndex == 0)
                {
                    // Linkes Auge: Verschiebe UV nach rechts
                    uv.x += _3DStrength;
                    return tex2D(_LeftTex, uv);
                }
                else
                {
                    // Rechtes Auge: Verschiebe UV nach links
                    uv.x -= _3DStrength;
                    return tex2D(_RightTex, uv);
                }
            }
            ENDCG
        }
    }
}