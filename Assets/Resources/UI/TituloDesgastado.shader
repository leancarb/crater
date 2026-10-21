Shader "Crater/TituloDesgastado"
{
    Properties
    {
        [PerRendererData] _MainTex ("Font atlas", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float2 posicion:TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color*_Color;
                o.uv=v.uv;o.posicion=v.vertex.xy;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 c=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
                float ruido=frac(sin(dot(floor(i.posicion*1.7),float2(12.9898,78.233)))*43758.5453);
                float manchas=frac(sin(dot(floor(i.posicion*.13),float2(37.719,11.135)))*17342.71);
                // Poros pequeños y estáticos; el título nunca parpadea.
                c.a*=lerp(1,.22,step(lerp(.985,.90,manchas),ruido));
                return c;
            }
            ENDCG
        }
    }
}
