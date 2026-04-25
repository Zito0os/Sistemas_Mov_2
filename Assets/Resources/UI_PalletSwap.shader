Shader "UI/Palette Swap 5 Colors"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _OldColor1 ("Color original 1", Color) = (0.75, 0.75, 0.75, 1)
        _NewColor1 ("Nuevo color 1", Color) = (0.20, 0.60, 1.00, 1)
        _Tolerance1 ("Tolerancia color 1", Range(0, 0.5)) = 0.10

        _OldColor2 ("Color original 2", Color) = (0.50, 0.50, 0.50, 1)
        _NewColor2 ("Nuevo color 2", Color) = (0.10, 0.30, 0.80, 1)
        _Tolerance2 ("Tolerancia color 2", Range(0, 0.5)) = 0.05

        _OldColor3 ("Color original 3", Color) = (0.25, 0.25, 0.25, 1)
        _NewColor3 ("Nuevo color 3", Color) = (0.80, 0.20, 0.20, 1)
        _Tolerance3 ("Tolerancia color 3", Range(0, 0.5)) = 0.04

        _OldColor4 ("Color original 4", Color) = (1.00, 1.00, 1.00, 1)
        _NewColor4 ("Nuevo color 4", Color) = (1.00, 0.85, 0.10, 1)
        _Tolerance4 ("Tolerancia color 4", Range(0, 0.5)) = 0.02

        _OldColor5 ("Color original 5", Color) = (0.00, 0.00, 0.00, 1)
        _NewColor5 ("Nuevo color 5", Color) = (0.00, 0.00, 0.00, 1)
        _Tolerance5 ("Tolerancia color 5", Range(0, 0.5)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            fixed4 _OldColor1;
            fixed4 _NewColor1;
            float _Tolerance1;

            fixed4 _OldColor2;
            fixed4 _NewColor2;
            float _Tolerance2;

            fixed4 _OldColor3;
            fixed4 _NewColor3;
            float _Tolerance3;

            fixed4 _OldColor4;
            fixed4 _NewColor4;
            float _Tolerance4;

            fixed4 _OldColor5;
            fixed4 _NewColor5;
            float _Tolerance5;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 ReplaceColor(fixed4 col, fixed4 oldCol, fixed4 newCol, float tolerance)
            {
                float diff = distance(col.rgb, oldCol.rgb);

                if (diff < tolerance)
                {
                    col.rgb = newCol.rgb;
                }

                return col;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                col = ReplaceColor(col, _OldColor1, _NewColor1, _Tolerance1);
                col = ReplaceColor(col, _OldColor2, _NewColor2, _Tolerance2);
                col = ReplaceColor(col, _OldColor3, _NewColor3, _Tolerance3);
                col = ReplaceColor(col, _OldColor4, _NewColor4, _Tolerance4);
                col = ReplaceColor(col, _OldColor5, _NewColor5, _Tolerance5);

                return col;
            }

            ENDCG
        }
    }
}