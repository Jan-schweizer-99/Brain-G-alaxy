// VRGizmoShader.shader
Shader "Custom/VRGizmoShader"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _EmissionStrength ("Emission Strength", Range(0,5)) = 1.0
        _RimPower ("Rim Power", Range(0.1,8.0)) = 3.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        struct Input
        {
            float3 viewDir;
            float3 worldPos;
        };
        
        fixed4 _Color;
        half _Glossiness;
        half _EmissionStrength;
        float _RimPower;
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Basis-Farbe
            o.Albedo = _Color.rgb;
            
            // Rim-Lighting für bessere Sichtbarkeit in VR
            float rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            float rimIntensity = pow(rim, _RimPower);
            
            // Emission für bessere Sichtbarkeit
            o.Emission = _Color.rgb * rimIntensity * _EmissionStrength;
            
            // Glanz und Smoothness
            o.Smoothness = _Glossiness;
            o.Metallic = 0;
            
            // Abstandsbasierte Intensität
            float distanceToCamera = length(_WorldSpaceCameraPos - IN.worldPos);
            float distanceIntensity = 1.0 / (1.0 + distanceToCamera * 0.1);
            o.Emission *= distanceIntensity;
        }
        ENDCG
    }
    FallBack "Diffuse"
}