Shader "Custom/3DGridWithHandCursor"
{
    Properties
    {
        _GridColor("Grid Color", Color) = (0.5,0.5,0.5,1)
        _HandCursorColor("Hand Cursor Color", Color) = (1,0,0,1)
        _GridSpacing("Grid Spacing", Float) = 1.0
        _GridSize("Grid Size", Float) = 10.0
        _CursorRadius("Cursor Radius", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _LeftHandPos;
            float4 _RightHandPos;
            float4 _GridColor;
            float4 _HandCursorColor;
            float _GridSpacing;
            float _GridSize;
            float _CursorRadius;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 pos = i.worldPos;
                float distToLeft = distance(pos, _LeftHandPos.xyz);
                float distToRight = distance(pos, _RightHandPos.xyz);

                float gridLine = step(0.01, abs(fmod(pos.x, _GridSpacing))) *
                                 step(0.01, abs(fmod(pos.y, _GridSpacing))) *
                                 step(0.01, abs(fmod(pos.z, _GridSpacing)));

                float handCursorLeft = 1.0 - smoothstep(_CursorRadius - 0.05, _CursorRadius, distToLeft);
                float handCursorRight = 1.0 - smoothstep(_CursorRadius - 0.05, _CursorRadius, distToRight);

                float3 color = lerp(_GridColor.rgb, _HandCursorColor.rgb, max(handCursorLeft, handCursorRight));
                return float4(color, 1.0) * gridLine;
            }
            ENDCG
        }
    }
}
