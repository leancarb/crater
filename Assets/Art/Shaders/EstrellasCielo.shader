Shader "Crater/EstrellasCielo"
{
    Properties { [HDR] _BaseColor("Brillo", Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Background+1" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
            struct A { float4 p:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 p:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            V Vert(A i)
            {
                V o;o.p=TransformObjectToHClip(i.p.xyz);
                #if UNITY_REVERSED_Z
                o.p.z=0;
                #else
                o.p.z=o.p.w;
                #endif
                o.uv=i.uv;o.color=i.color;return o;
            }
            half4 Frag(V i):SV_Target
            {
                float r=length(i.uv*2-1);
                float borde=max(fwidth(r),.025);
                float perfil=(1-smoothstep(1-borde,1+borde,r))*exp2(-r*r*4);
                return half4(i.color.rgb*_BaseColor.rgb,perfil*i.color.a*_BaseColor.a);
            }
            ENDHLSL
        }
    }
}
