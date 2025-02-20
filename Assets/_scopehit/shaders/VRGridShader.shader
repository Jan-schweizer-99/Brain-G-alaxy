Shader "Custom/WireframeGrid" 
{
    Properties
    {
        _GridSize ("Grid Size", Float) = 1.0
        _GridColor ("Grid Color", Color) = (1,1,1,1)
        _LineWidth ("Line Width", Range(0.0001, 0.01)) = 0.001
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
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
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : NORMAL;
                float3 centerPos : TEXCOORD2;
            };
            
            float _GridSize;
            float4 _GridColor;
            float _LineWidth;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.centerPos = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz; // Position des Parent-Objects
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }
            
            float isGridLine(float position, float center) 
            {
                float relativePos = position - center;
                float modPos = fmod(abs(relativePos), _GridSize);
                float epsilon = 0.00001;
                return (modPos < _LineWidth + epsilon) || (_GridSize - modPos < _LineWidth + epsilon) ? 1.0 : 0.0;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 absNormal = abs(normalize(i.normal));
                float gridValue = 0;
                
                // XZ-Ebene (Normale in Y-Richtung)
                if(absNormal.y > 0.99) {
                    gridValue = max(isGridLine(i.worldPos.x, i.centerPos.x), 
                                  isGridLine(i.worldPos.z, i.centerPos.z));
                }
                // XY-Ebene (Normale in Z-Richtung)
                else if(absNormal.z > 0.99) {
                    gridValue = max(isGridLine(i.worldPos.x, i.centerPos.x), 
                                  isGridLine(i.worldPos.y, i.centerPos.y));
                }
                // YZ-Ebene (Normale in X-Richtung)
                else if(absNormal.x > 0.99) {
                    gridValue = max(isGridLine(i.worldPos.y, i.centerPos.y), 
                                  isGridLine(i.worldPos.z, i.centerPos.z));
                }
                
                // Zeichne Achsen dicker
                float axisWidth = _LineWidth * 2;
                
                if(absNormal.y > 0.99) {
                    float onAxisX = abs(i.worldPos.x - i.centerPos.x) < axisWidth ? 1.0 : 0.0;
                    float onAxisZ = abs(i.worldPos.z - i.centerPos.z) < axisWidth ? 1.0 : 0.0;
                    gridValue = max(gridValue, max(onAxisX, onAxisZ));
                }
                else if(absNormal.z > 0.99) {
                    float onAxisX = abs(i.worldPos.x - i.centerPos.x) < axisWidth ? 1.0 : 0.0;
                    float onAxisY = abs(i.worldPos.y - i.centerPos.y) < axisWidth ? 1.0 : 0.0;
                    gridValue = max(gridValue, max(onAxisX, onAxisY));
                }
                else if(absNormal.x > 0.99) {
                    float onAxisY = abs(i.worldPos.y - i.centerPos.y) < axisWidth ? 1.0 : 0.0;
                    float onAxisZ = abs(i.worldPos.z - i.centerPos.z) < axisWidth ? 1.0 : 0.0;
                    gridValue = max(gridValue, max(onAxisY, onAxisZ));
                }
                
                // Verwerfe transparente Pixel
                clip(gridValue - 0.1);
                
                return _GridColor;
            }
            ENDCG
        }
    }
}