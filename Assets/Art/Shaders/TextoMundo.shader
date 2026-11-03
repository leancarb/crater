Shader "Crater/TextoMundo"
{
    Properties { _MainTex("Font",2D)="white"{} }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
            V Vert(A a){V v;v.p=TransformObjectToHClip(a.p.xyz);v.uv=a.uv;v.c=a.c;return v;}
            half4 Frag(V v):SV_Target{return half4(v.c.rgb,v.c.a*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv).a);}
            ENDHLSL
        }
    }
}
