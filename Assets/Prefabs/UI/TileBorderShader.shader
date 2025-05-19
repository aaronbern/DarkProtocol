Shader "Custom/URP_TileBorder"
{
    Properties
    {
        [Header(Tile Border Settings)]
        [MainColor] _BorderColor ("Border Color", Color) = (0,1,0,1)
        _BorderThickness ("Border Thickness", Range(0.01, 0.5)) = 0.1
    }
    

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "TileBorder"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _BorderColor;
            float _BorderThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float border = _BorderThickness;

                bool isEdge = uv.x < border || uv.x > 1.0 - border || uv.y < border || uv.y > 1.0 - border;
                return isEdge ? _BorderColor : float4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
