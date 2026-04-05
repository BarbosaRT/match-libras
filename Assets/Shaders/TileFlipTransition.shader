Shader "Custom/TileFlipTransition"
{
    Properties
    {
        _MainTex ("Screen Capture", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
        _Cols ("Columns", Float) = 8
        _Rows ("Rows", Float) = 6
        _Stagger ("Stagger", Range(0,1)) = 0.4
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Progress;
            float _Cols;
            float _Rows;
            float _Stagger;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // Usa TRANSFORM_TEX para respeitar o tiling/offset do material
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 tileID = floor(i.uv * float2(_Cols, _Rows));
                float2 tileUV = frac(i.uv * float2(_Cols, _Rows));

                float delay = ((tileID.x / _Cols) + (tileID.y / _Rows)) * 0.5 * _Stagger;
                float localProgress = saturate((_Progress - delay) / (1.0 - _Stagger));

                float angle = localProgress * 3.14159265;
                float cosA = cos(angle);

                // Face traseira = preto transparente (revela a nova cena atrás)
                if (cosA < 0)
                    return fixed4(0, 0, 0, 0);

                // Recalcula UV com efeito de flip
                float tileCenterX = (tileID.x + 0.5) / _Cols;
                float2 sampledUV = i.uv;
                sampledUV.x = tileCenterX + (tileUV.x - 0.5) * cosA / _Cols;

                fixed4 col = tex2D(_MainTex, sampledUV);
                col.rgb *= lerp(0.6, 1.0, abs(cosA));
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}