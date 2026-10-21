Shader "Crater/CieloExteriorBrumoso"
{
    Properties { _BaseColor("Cielo",Color)=(.87,.9,.79,1) }
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Background" "RenderPipeline"="UniversalPipeline"}
        Cull Front ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
            struct A {float4 p:POSITION;};
            struct V {float4 p:SV_POSITION;float3 mundo:TEXCOORD0;};
            #include "Assets/Shaders/CieloExteriorComun.hlsl"
            V vert(A i){V o;VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);o.p=p.positionCS;o.mundo=p.positionWS;return o;}
            half4 frag(V i):SV_Target
            {
                half3 c=ColorCieloBrumoso(i.mundo,_BaseColor.rgb);
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
