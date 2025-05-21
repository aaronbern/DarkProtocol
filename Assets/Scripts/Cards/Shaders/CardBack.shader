Shader "DarkProtocol/CardBack" {
    Properties {
        _MainTex ("Card Back Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _BumpScale ("Normal Strength", Range(0, 1)) = 0.5
        
        // Card colors
        _CardColor ("Card Color Tint", Color) = (1,1,1,1)
        _TeamColor ("Team Color", Color) = (0,0.5,1,1)
        _RimColor ("Rim Light Color", Color) = (1,1,1,0.4)
        _RimPower ("Rim Light Power", Range(0.5, 8.0)) = 3.0
        
        // Back pattern animation
        _PatternSpeed ("Pattern Animation Speed", Range(0, 2)) = 0.5
        _PatternIntensity ("Pattern Intensity", Range(0, 1)) = 0.2
        
        // Card effects
        [Toggle] _AnimateBack ("Animate Back", Float) = 1
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _MaskTex;
        sampler2D _NormalMap;
        
        struct Input {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float3 viewDir;
            float3 worldPos;
        };
        
        half _Glossiness;
        half _Metallic;
        half _BumpScale;
        fixed4 _CardColor;
        fixed4 _TeamColor;
        fixed4 _RimColor;
        half _RimPower;
        half _PatternSpeed;
        half _PatternIntensity;
        half _AnimateBack;
        
        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Sample textures
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 mask = tex2D(_MaskTex, IN.uv_MainTex);
            
            // Apply card color tint
            c.rgb *= _CardColor.rgb;
            
            // Apply team color to mask regions
            c.rgb = lerp(c.rgb, _TeamColor.rgb, mask.r * 0.7);
            
            // Apply animated pattern effect
            if (_AnimateBack > 0.5) {
                float2 animatedUV = IN.uv_MainTex;
                
                // Create subtle animated distortion
                float time = _Time.y * _PatternSpeed;
                float distortionX = sin(time * 1.3 + IN.uv_MainTex.y * 6.0) * 0.02 * _PatternIntensity;
                float distortionY = cos(time * 1.7 + IN.uv_MainTex.x * 6.0) * 0.02 * _PatternIntensity;
                
                animatedUV += float2(distortionX, distortionY);
                
                // Apply distortion only to pattern areas (mask.g)
                fixed4 pattern = tex2D(_MainTex, animatedUV);
                c.rgb = lerp(c.rgb, pattern.rgb, mask.g * _PatternIntensity);
                
                // Add subtle color shift based on time
                fixed3 colorShift = 0.5 + 0.5 * sin(time * float3(0.3, 0.45, 0.6) + 0.5);
                c.rgb += colorShift * 0.1 * mask.g * _PatternIntensity;
            }
            
            // Apply normal mapping
            fixed3 normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            normal.xy *= _BumpScale;
            
            // Apply rim lighting effect
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), normal));
            half rimIntensity = pow(rim, _RimPower);
            c.rgb += _RimColor.rgb * rimIntensity * _RimColor.a;
            
            // Set output properties
            o.Albedo = c.rgb;
            o.Normal = normal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            
            // Add subtle emission on team color areas
            o.Emission = _TeamColor.rgb * mask.r * 0.1;
            
            // Add subtle emission on animated pattern
            if (_AnimateBack > 0.5) {
                float glowPulse = 0.5 + 0.5 * sin(_Time.y * 1.5);
                o.Emission += _TeamColor.rgb * mask.g * _PatternIntensity * glowPulse * 0.15;
            }
        }
        ENDCG
        
        // Card outline pass
        Pass {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }
            Cull Front
            ZWrite On
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f {
                float4 pos : POSITION;
                float4 color : COLOR;
            };
            
            uniform float _OutlineWidth;
            uniform fixed4 _TeamColor;
            
            v2f vert(appdata v) {
                v2f o;
                float3 normal = normalize(v.normal);
                
                // Apply outline effect by extruding vertices along normals
                float3 pos = v.vertex.xyz + normal * 0.003; // Fixed small outline
                o.pos = UnityObjectToClipPos(float4(pos, 1));
                
                // Darken team color for outline
                fixed4 outlineColor = _TeamColor * 0.7;
                outlineColor.a = 0.5;
                o.color = outlineColor;
                
                return o;
            }
            
            half4 frag(v2f i) : COLOR {
                return i.color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}