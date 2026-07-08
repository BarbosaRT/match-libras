Shader "UI/TutorialSpotlight"
{
    // Overlay escuro full-screen com um "buraco" circular suave.
    // Aplique este material numa Image esticada (anchors 0,0 a 1,1) numa Canvas
    // separada, com sorting order acima de tudo (menos a mao/hand pointer, se quiser
    // que ela fique visivel por cima do overlay).

    Properties
    {
        _MainTex ("Texture (nao usada, exigida pela UI)", 2D) = "white" {}
        _Color   ("Cor do Overlay", Color) = (0,0,0,0.75)
        _Center  ("Centro (viewport 0-1)", Vector) = (0.5,0.5,0,0)
        _Radius  ("Raio (normalizado pela altura)", Float) = 0.15
        _Softness("Suavidade da borda", Float) = 0.04
        _Aspect  ("Aspect Ratio (largura/altura)", Float) = 1.777
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Softness;
            float _Aspect;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 d = i.uv - _Center.xy;
                d.x *= _Aspect;
                float dist = length(d);
                float alpha = smoothstep(_Radius, _Radius + _Softness, dist);
                return fixed4(_Color.rgb, _Color.a * alpha * i.color.a);
            }
            ENDCG
        }
    }
}
