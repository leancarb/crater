#ifndef CRATER_CIELO_EXTERIOR
#define CRATER_CIELO_EXTERIOR
float HashCieloExterior(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float RuidoCieloExterior(float2 p)
{
    float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(HashCieloExterior(a),HashCieloExterior(a+float2(1,0)),f.x),lerp(HashCieloExterior(a+float2(0,1)),HashCieloExterior(a+1),f.x),f.y);
}
half3 ColorCieloBrumoso(float3 mundo,half3 base)
{
    float3 d=normalize(mundo-_WorldSpaceCameraPos);
    float2 p=d.xz/(max(d.y,0)+.32);
    float n=RuidoCieloExterior(p*2.3)*.6+RuidoCieloExterior(p*5.7+17)*.3+RuidoCieloExterior(p*13)*.1;
    float horizonte=smoothstep(0,.25,max(d.y,0));
    return base*lerp(1,lerp(.90,1.09,smoothstep(.3,.7,n)),horizonte);
}
#endif
