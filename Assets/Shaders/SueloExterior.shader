Shader "Crater/SueloExterior"
{
    Properties
    {
        _BaseColor("Tierra gris ocre",Color)=(.42,.40,.32,1)
        _BaseMap("Base",2D)="white"{}
        _Cutoff("Cutoff",Float)=.5
        _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags{"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Name "Suelo" Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float _Cutoff;
            CBUFFER_END
            struct A{float4 p:POSITION;float3 n:NORMAL;};
            struct V{float4 p:SV_POSITION;float3 mundo:TEXCOORD0;half3 normal:TEXCOORD1;half fog:TEXCOORD2;};
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float ruido(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            V vert(A i)
            {
                V o;VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);
                o.p=p.positionCS;o.mundo=p.positionWS;o.normal=TransformObjectToWorldNormal(i.n);
                o.fog=ComputeFogFactor(o.p.z);return o;
            }
            half4 frag(V i):SV_Target
            {
                float2 p=i.mundo.xz;
                float manchas=ruido(p*.19)*.6+ruido(p*.75)*.4;
                half3 tierra=_BaseColor.rgb*lerp(.74,1.18,manchas);
                float detalle=1-smoothstep(.3,1.3,max(fwidth(p.x*12),fwidth(p.y*12)));
                float grano=ruido(p*6.5)*.55+ruido(p*19.7)*.3+ruido(p*51.3)*.15;
                tierra*=lerp(1,lerp(.66,1.12,grano),detalle);
                half3 n=normalize(i.normal);
                Light sol=GetMainLight(TransformWorldToShadowCoord(i.mundo));
                half3 luz=SampleSH(n)+sol.color*(saturate(dot(n,sol.direction))*sol.shadowAttenuation);
                half3 color=tierra*luz;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
