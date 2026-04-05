Shader "Custom/CheckerFlipTransition"
{
    Properties
    {
        _MainTex ("Screen Capture", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
        _Cols ("Columns", Float) = 8
        _Rows ("Rows", Float) = 6
        _Phase ("Phase", Range(0,0.5)) = 0.3
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
            float _Phase;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 tileID = floor(i.uv * float2(_Cols, _Rows));
                float2 tileUV = frac(i.uv * float2(_Cols, _Rows));

                float checker = fmod(tileID.x + tileID.y, 2.0);
                float groupOffset = checker * _Phase;

                // Normaliza o progresso local entre 0 e 1
                // dividindo pelo espaço restante após o offset
                float localProgress = saturate((_Progress - groupOffset) / (1.0 - _Phase));

                // Usa só metade do cosseno: PI/2 → só vai de 1 até -1 sem voltar
                // Mapeamos 0→1 para 0→PI (flip completo, sem retorno)
                float angle = localProgress * 3.14159265;
                float cosA = cos(angle);

                // Escala o cosA de [-1, 1] para [0, 1]
                // assim nunca fica negativo — sem parte preta
                float scale = (cosA + 1.0) * 0.5;

                // Quando scale < 0.5 estamos na face traseira — revela a nova cena
                if (scale < 0.5)
                    return fixed4(0, 0, 0, 0);

                // Remapeia scale de [0.5, 1] de volta para [0, 1] para o efeito de squish
                float squish = (scale - 0.5) * 2.0;

                float tileCenterX = (tileID.x + 0.5) / _Cols;
                float2 sampledUV = i.uv;
                sampledUV.x = tileCenterX + (tileUV.x - 0.5) * squish / _Cols;

                fixed4 col = tex2D(_MainTex, sampledUV);

                // Escurece proporcionalmente ao squish — mais escuro no meio da rotação
                col.rgb *= lerp(0.5, 1.0, squish);
                col.a = 1.0;

                return col;
            }
            ENDCG
        }
    }
}