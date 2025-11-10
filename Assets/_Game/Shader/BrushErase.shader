Shader "Custom/BrushErase"
{
    Properties{
        _MainTex("Base", 2D) = "white" {}
        _BrushPos("BrushPos", Vector) = (0,0,0,0) // UV 0..1
        _BrushRadius("BrushRadius", Float) = 0.09
    }
    SubShader{
        Tags{ "RenderType"="Transparent" "Queue"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _BrushPos;
            float _BrushRadius;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float2 uv:TEXCOORD0; float4 vertex:SV_POSITION; };

            v2f vert (appdata v) {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                float d = distance(i.uv, _BrushPos.xy);
                float keep = smoothstep(_BrushRadius, _BrushRadius*0.7, d); // viền mịn
                col.a *= keep; // gần tâm → alpha = 0
                return col;
            }
            ENDCG
        }
    }
}
