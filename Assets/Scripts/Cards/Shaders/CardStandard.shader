Shader "DarkProtocol/CardStandard" {
    Properties {
        _MainTex ("Card Face", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _BumpScale ("Normal Strength", Range(0, 1)) = 0.5
        
        // Card colors and highlights
        _CardColor ("Card Color Tint", Color) = (1,1,1,1)
        _HighlightColor ("Highlight Color", Color) = (1,0.8,0,1)
        _HighlightIntensity ("Highlight Intensity", Range(0, 1)) = 0
        _RimColor ("Rim Light Color", Color) = (1,1,1,0.4)
        _RimPower ("Rim Light Power", Range(0.5, 8.0)) = 3.0
        
        // Rarity effects
        _RarityColor ("Rarity Color", Color) = (1,1,1,1)
        _RarityIntensity ("Rarity Intensity", Range(0, 1)) = 0.5
        _RarityPulse ("Rarity Pulse Speed", Range(0, 5)) = 0
        
        // Card info visualization
        _APCost ("Action Point Cost", Float) = 1
        _MPCost ("Movement Point Cost", Float) = 0
        _DamageAmount ("Damage Amount", Float) = 0
        _HealAmount ("Healing Amount", Float) = 0
        
        // Card states
        [Toggle] _Selected ("Is Selected", Float) = 0
        [Toggle] _Playable ("Is Playable", Float) = 1
        [Toggle] _Hovering ("Is Hovering", Float) = 0
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
        fixed4 _HighlightColor;
        half _HighlightIntensity;
        fixed4 _RimColor;
        half _RimPower;
        fixed4 _RarityColor;
        half _RarityIntensity;
        half _RarityPulse;
        half _APCost;
        half _MPCost;
        half _DamageAmount;
        half _HealAmount;
        half _Selected;
        half _Playable;
        half _Hovering;
        
        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Sample textures
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 mask = tex2D(_MaskTex, IN.uv_MainTex);
            fixed3 normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            normal.xy *= _BumpScale;
            
            // Apply card color tint
            c.rgb *= _CardColor.rgb;
            
            // Apply highlight effect based on card state (selected or hovering)
            float highlightFactor = _HighlightIntensity;
            if (_Selected > 0.5) {
                highlightFactor = max(highlightFactor, 0.5);
            }
            if (_Hovering > 0.5) {
                highlightFactor = max(highlightFactor, 0.3);
            }
            
            // Apply highlight to masked areas
            c.rgb = lerp(c.rgb, _HighlightColor.rgb, highlightFactor * mask.g);
            
            // Apply rarity color with pulsing effect
            float rarityFactor = _RarityIntensity;
            if (_RarityPulse > 0) {
                rarityFactor *= (sin(_Time.y * _RarityPulse) * 0.25 + 0.75);
            }
            c.rgb = lerp(c.rgb, _RarityColor.rgb, rarityFactor * mask.r);
            
            // Apply rim lighting effect
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), normal));
            half rimIntensity = pow(rim, _RimPower);
            c.rgb += _RimColor.rgb * rimIntensity * _RimColor.a;
            
            // Reduce brightness if card is not playable
            if (_Playable < 0.5) {
                c.rgb *= 0.6;
            }
            
            // Set output properties
            o.Albedo = c.rgb;
            o.Normal = normal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            
            // Add subtle emission on highlighted/selected areas
            o.Emission = _HighlightColor.rgb * highlightFactor * mask.g * 0.5;
            
            // Add subtle emission for rarity
            o.Emission += _RarityColor.rgb * rarityFactor * mask.r * 0.3;
        }
        ENDCG
        
        // Card outline pass for selection and highlighting
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
            uniform fixed4 _OutlineColor;
            uniform half _Selected;
            uniform half _Hovering;
            
            v2f vert(appdata v) {
                v2f o;
                float3 normal = normalize(v.normal);
                
                // Calculate outline width based on card state
                float outlineWidth = 0.003;  // Base width
                if (_Selected > 0.5) {
                    outlineWidth = 0.006;    // Wider outline when selected
                }
                else if (_Hovering > 0.5) {
                    outlineWidth = 0.004;    // Medium outline when hovering
                }
                
                // Apply outline effect by extruding vertices along normals
                float3 pos = v.vertex.xyz + normal * outlineWidth;
                o.pos = UnityObjectToClipPos(float4(pos, 1));
                
                // Set outline color based on state
                if (_Selected > 0.5) {
                    o.color = fixed4(1.0, 0.8, 0.0, 1.0);  // Gold outline when selected
                }
                else if (_Hovering > 0.5) {
                    o.color = fixed4(0.8, 0.8, 1.0, 0.8);  // Light blue outline when hovering
                }
                else {
                    o.color = fixed4(0.2, 0.2, 0.2, 0.5);  // Dark subtle outline by default
                }
                
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