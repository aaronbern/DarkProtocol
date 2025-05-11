Shader "Custom/SimpleGridShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GridColor ("Grid Color", Color) = (0,0.6,1,0.8)
        _GridSize ("Grid Size", Float) = 1.0
        _LineWidth ("Line Width", Range(0.001, 0.1)) = 0.02
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GridColor;
            float _GridSize;
            float _LineWidth;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Calculate grid
                float2 grid = frac(i.worldPos.xz / _GridSize);
                
                // Calculate distance to nearest grid line
                float2 gridDist = min(grid, 1.0 - grid);
                float dist = min(gridDist.x, gridDist.y);
                
                // Apply grid lines where distance is less than line width
                float lineIntensity = 1.0 - smoothstep(0.0, _LineWidth, dist);
                
                // Blend with grid color
                col = lerp(col, _GridColor, lineIntensity);
                
                return col;
            }
            ENDCG
        }
    }
}