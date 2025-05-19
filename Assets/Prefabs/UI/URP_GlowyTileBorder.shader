Shader "Custom/URP_GlowyTileBorder_ManualTime"
{
    Properties
    {
        [Header(Border Base Settings)]
        [MainColor] _BorderColor ("Border Color", Color) = (0,1,0,1)
        _BorderThickness ("Border Thickness", Range(0.01, 0.5)) = 0.1
        
        [Header(Glow Effects)]
        _GlowColor ("Glow Color", Color) = (0,2,0,1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.5
        _GlowFalloff ("Glow Falloff", Range(0.1, 5)) = 2.0
        
        [Header(Animation)]
        [Toggle] _EnablePulse ("Enable Pulse", Float) = 1
        _PulseSpeed ("Pulse Speed", Range(0.1, 5)) = 1.0
        _PulseMinMax ("Pulse Range (Min/Max)", Vector) = (0.8, 1.2, 0, 0)
        
        [Header(Edge Effects)]
        _EdgeSoftness ("Edge Softness", Range(0, 0.1)) = 0.01
        [Toggle] _EnableCornerGlow ("Enhanced Corner Glow", Float) = 1
        _CornerGlowIntensity ("Corner Glow Intensity", Range(1, 3)) = 1.5
        
        [Header(Flow Effect)]
        [Toggle] _EnableFlow ("Enable Flow", Float) = 1
        _FlowSpeed ("Flow Speed", Range(0.1, 5)) = 1.0
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 0.3
        
        [Header(Debug)]
        _ManualTime ("Manual Time Override", Float) = 0
        [Toggle] _UseManualTime ("Use Manual Time", Float) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            // Border properties
            float4 _BorderColor;
            float _BorderThickness;
            
            // Glow properties
            float4 _GlowColor;
            float _GlowIntensity;
            float _GlowFalloff;
            
            // Animation properties
            float _EnablePulse;
            float _PulseSpeed;
            float2 _PulseMinMax;
            
            // Edge properties
            float _EdgeSoftness;
            float _EnableCornerGlow;
            float _CornerGlowIntensity;
            
            // Flow properties
            float _EnableFlow;
            float _FlowSpeed;
            float _FlowIntensity;
            
            // Manual time
            float _ManualTime;
            float _UseManualTime;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            // Helper function to calculate distance to edge
            float distanceToEdge(float2 uv, float thickness)
            {
                // Calculate distance to each edge
                float distToLeft = uv.x;
                float distToRight = 1.0 - uv.x;
                float distToBottom = uv.y;
                float distToTop = 1.0 - uv.y;
                
                // Find the minimum distance
                float minDist = min(min(distToLeft, distToRight), min(distToBottom, distToTop));
                
                // Map to 0 to 1 range within the border thickness
                return saturate(minDist / thickness);
            }
            
            // Helper function for corners
            float isCorner(float2 uv, float thickness, float cornerRange)
            {
                float2 centered = abs(uv - 0.5) * 2.0;  // Map to -1 to 1
                float cornerDist = length(max(centered - (1.0 - thickness * 2.0), 0.0));
                return saturate(1.0 - cornerDist / cornerRange);
            }
            
            // Flow effect
            float flowEffect(float2 uv, float time)
            {
                // Create a moving pattern along the edges
                float xEdge = min(uv.x, 1.0 - uv.x) < _BorderThickness ? 
                    sin(uv.y * 20.0 + time) * 0.5 + 0.5 : 0.0;
                
                float yEdge = min(uv.y, 1.0 - uv.y) < _BorderThickness ? 
                    sin(uv.x * 20.0 + time) * 0.5 + 0.5 : 0.0;
                
                return max(xEdge, yEdge);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                // Get animation time (either built-in or manual)
                float animTime = (_UseManualTime > 0.5) ? _ManualTime : _Time.y;
                
                // Apply pulsing animation to border thickness
                float animatedThickness = _BorderThickness;
                if (_EnablePulse > 0.5) {
                    float pulse = lerp(_PulseMinMax.x, _PulseMinMax.y, 
                                      (sin(animTime * _PulseSpeed) * 0.5 + 0.5));
                    animatedThickness *= pulse;
                }
                
                // Calculate distance to the edge
                float edgeDist = distanceToEdge(uv, animatedThickness);
                
                // Determine if we're in the border with soft edges
                float borderMask = 1.0 - smoothstep(0.0, max(_EdgeSoftness, 0.001), edgeDist);
                
                // Corner glow effect
                float cornerEffect = 0.0;
                if (_EnableCornerGlow > 0.5) {
                    float cornerSize = animatedThickness * 3.0;
                    float cornerIntensity = isCorner(uv, animatedThickness, cornerSize);
                    cornerEffect = cornerIntensity * _CornerGlowIntensity;
                }
                
                // Flow effect using our time variable
                float flowMask = 0.0;
                if (_EnableFlow > 0.5) {
                    flowMask = flowEffect(uv, animTime * _FlowSpeed) * _FlowIntensity * borderMask;
                }
                
                // Combine glow effects
                float glowIntensity = saturate(1.0 - edgeDist * _GlowFalloff) * _GlowIntensity;
                glowIntensity = max(glowIntensity, cornerEffect);
                glowIntensity = max(glowIntensity, flowMask);
                
                // Create final color by blending base border and glow
                float4 baseColor = _BorderColor * borderMask;
                float4 glowComponent = _GlowColor * glowIntensity * borderMask;
                
                // Final result with fog applied
                float4 finalColor = baseColor + glowComponent;
                UNITY_APPLY_FOG(i.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}