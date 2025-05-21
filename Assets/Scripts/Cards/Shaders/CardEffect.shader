Shader "DarkProtocol/CardEffect" {
    Properties {
        _MainTex ("Effect Texture", 2D) = "white" {}
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _FlowMap ("Flow Map", 2D) = "gray" {}
        
        _Color ("Main Color", Color) = (1,1,1,1)
        _EmissionColor ("Emission Color", Color) = (1,0.5,0,1)
        _EmissionIntensity ("Emission Intensity", Range(0, 3)) = 1.0
        
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0.0
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0, 0.2)) = 0.05
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1,0.5,0,1)
        
        _FlowSpeed ("Flow Speed", Range(0, 2)) = 0.5
        _FlowIntensity ("Flow Intensity", Range(0, 1)) = 0.5
        
        _EffectType ("Effect Type", Range(0, 3)) = 0
        [Toggle] _UseWorldCoords ("Use World Space", Float) = 0
    }
    
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float2 maskUV : TEXCOORD1;
                float2 noiseUV : TEXCOORD2;
                float2 flowUV : TEXCOORD3;
                float4 worldPos : TEXCOORD4;
                UNITY_FOG_COORDS(5)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _NoiseTex;
            sampler2D _FlowMap;
            
            float4 _MainTex_ST;
            float4 _MaskTex_ST;
            float4 _NoiseTex_ST;
            float4 _FlowMap_ST;
            
            fixed4 _Color;
            fixed4 _EmissionColor;
            half _EmissionIntensity;
            
            half _DissolveAmount;
            half _DissolveEdgeWidth;
            fixed4 _DissolveEdgeColor;
            
            half _FlowSpeed;
            half _FlowIntensity;
            
            half _EffectType;
            half _UseWorldCoords;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                // Standard UV coordinates
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.maskUV = TRANSFORM_TEX(v.uv, _MaskTex);
                
                // Determine whether to use world coordinates for effects
                if (_UseWorldCoords > 0.5) {
                    o.noiseUV = o.worldPos.xz * 0.1;
                    o.flowUV = o.worldPos.xy * 0.1;
                } else {
                    o.noiseUV = TRANSFORM_TEX(v.uv, _NoiseTex);
                    o.flowUV = TRANSFORM_TEX(v.uv, _FlowMap);
                }
                
                o.color = v.color * _Color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // Initialize base color
                fixed4 col = fixed4(0,0,0,0);
                
                // Determine effect type and apply appropriate effect
                if (_EffectType < 1.0) {
                    // Effect Type 0: Flow-based distortion effect (energy, fire, magic)
                    
                    // Sample flow map
                    float2 flowVector = (tex2D(_FlowMap, i.flowUV).rg * 2.0 - 1.0) * _FlowIntensity;
                    
                    // Time parameters for flow animation
                    float time = _Time.y * _FlowSpeed;
                    float halfTime = time * 0.5;
                    
                    // Calculate two flow phases for smooth transition
                    float phase0 = frac(time);
                    float phase1 = frac(time + 0.5);
                    
                    // Blend factor between the two phases
                    float blend = abs((phase0 - 0.5) * 2.0);
                    
                    // Sample texture at two different flow phases
                    float2 uv0 = i.uv + flowVector * phase0;
                    float2 uv1 = i.uv + flowVector * phase1;
                    
                    fixed4 col0 = tex2D(_MainTex, uv0);
                    fixed4 col1 = tex2D(_MainTex, uv1);
                    
                    // Blend the two samples
                    col = lerp(col0, col1, blend);
                    
                    // Apply mask
                    fixed4 mask = tex2D(_MaskTex, i.maskUV);
                    col.a *= mask.r;
                    
                    // Apply emission color
                    col.rgb *= _Color.rgb;
                    col.rgb += _EmissionColor.rgb * _EmissionIntensity * col.a;
                }
                else if (_EffectType < 2.0) {
                    // Effect Type 1: Dissolve effect (disintegration, teleport)
                    
                    // Sample noise texture for dissolve pattern
                    float noise = tex2D(_NoiseTex, i.noiseUV).r;
                    
                    // Calculate dissolve edge
                    float dissolveValue = noise - _DissolveAmount;
                    
                    // Discard pixels below threshold (fully dissolved)
                    if (dissolveValue < 0) {
                        discard;
                    }
                    
                    // Apply edge effect
                    float edgeFactor = saturate(dissolveValue / _DissolveEdgeWidth);
                    
                    // Sample main texture
                    col = tex2D(_MainTex, i.uv);
                    
                    // Apply dissolve edge color
                    col.rgb = lerp(_DissolveEdgeColor.rgb * 2.0, col.rgb * _Color.rgb, edgeFactor);
                    
                    // Make edge glow with emission
                    col.rgb += _DissolveEdgeColor.rgb * _EmissionIntensity * (1.0 - edgeFactor);
                    
                    // Apply mask
                    fixed4 mask = tex2D(_MaskTex, i.maskUV);
                    col.a *= mask.r * i.color.a;
                }
                else if (_EffectType < 3.0) {
                    // Effect Type 2: Ripple/Pulse effect (shockwave, beats)
                    
                    // Calculate distance from center of UV
                    float2 center = float2(0.5, 0.5);
                    float dist = distance(i.uv, center);
                    
                    // Create ripple effect based on time
                    float rippleSpeed = _FlowSpeed * 5.0;
                    float rippleCount = 3.0;
                    float rippleWidth = 0.1;
                    float ripple = sin(dist * rippleCount * 3.14159 - _Time.y * rippleSpeed);
                    
                    // Create ripple mask 
                    float rippleMask = 1.0 - saturate(abs(ripple) / rippleWidth);
                    
                    // Fade ripple based on distance from center (weaker at edges)
                    rippleMask *= (1.0 - smoothstep(0.0, 0.5, dist));
                    
                    // Sample base texture
                    col = tex2D(_MainTex, i.uv);
                    
                    // Apply ripple effect
                    col.rgb = lerp(col.rgb * _Color.rgb, _EmissionColor.rgb, rippleMask * _EmissionIntensity);
                    
                    // Apply mask
                    fixed4 mask = tex2D(_MaskTex, i.maskUV);
                    col.a *= mask.r * i.color.a;
                    
                    // Boost alpha channel on ripple areas
                    col.a = max(col.a, rippleMask * _EmissionIntensity * mask.r);
                }
                else {
                    // Effect Type 3: Glitch effect (tech, corruption, digital)
                    
                    // Time-based values for glitch effect
                    float glitchTime = _Time.y * 10.0;
                    
                    // Create random glitch blocks
                    float blockNoiseX = floor(i.uv.x * 32.0) / 32.0;
                    float blockNoiseY = floor(i.uv.y * 32.0) / 32.0;
                    float blockThreshold = tex2D(_NoiseTex, float2(blockNoiseX, _Time.y * 0.1)).r;
                    
                    // Only affect some blocks
                    float blockEffect = step(0.75, blockThreshold);
                    
                    // Calculate UV offset for glitch
                    float2 uvGlitch = i.uv;
                    
                    if (blockEffect > 0.0) {
                        // Apply horizontal RGB shift
                        float glitchAmount = sin(glitchTime * blockNoiseY) * 0.01 * _FlowIntensity;
                        uvGlitch.x += glitchAmount;
                    }
                    
                    // Random vertical jitter
                    float lineNoise = tex2D(_NoiseTex, float2(_Time.y * 0.1, i.uv.y * 0.8)).g;
                    float lineEffect = step(0.9, lineNoise) * sin(_Time.y * 20.0) * _FlowIntensity;
                    uvGlitch.x += lineEffect * 0.02;
                    
                    // Sample base texture with glitched UVs
                    col = tex2D(_MainTex, uvGlitch);
                    
                    // Apply RGB splitting
                    if (blockEffect > 0.0) {
                        float rgbSplitAmount = 0.01 * _FlowIntensity;
                        float3 rgbSplit;
                        rgbSplit.r = tex2D(_MainTex, uvGlitch + float2(rgbSplitAmount, 0)).r;
                        rgbSplit.g = col.g;
                        rgbSplit.b = tex2D(_MainTex, uvGlitch - float2(rgbSplitAmount, 0)).b;
                        col.rgb = lerp(col.rgb, rgbSplit, blockEffect);
                    }
                    
                    // Apply mask
                    fixed4 mask = tex2D(_MaskTex, i.maskUV);
                    col.a *= mask.r * i.color.a;
                    
                    // Apply color tint
                    col.rgb *= _Color.rgb;
                    
                    // Apply glitch highlights
                    col.rgb += _EmissionColor.rgb * lineEffect * mask.r * _EmissionIntensity;
                }
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
    Fallback "Transparent/VertexLit"
}