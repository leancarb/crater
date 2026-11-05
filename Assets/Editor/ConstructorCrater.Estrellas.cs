using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class ConstructorCrater
{
    static Renderer ConstruirEstrellas(Transform padre)
    {
        const int cantidad=1300;const float radio=150;
        var vertices=new Vector3[cantidad*4];var uv=new Vector2[vertices.Length];var colores=new Color[vertices.Length];var indices=new int[cantidad*6];
        var azar=new System.Random(41673);
        float Aleatorio()=> (float)azar.NextDouble();
        for(int i=0;i<cantidad;i++)
        {
            float y=Mathf.Lerp(.035f,.999f,Aleatorio()),a=Aleatorio()*Mathf.PI*2,h=Mathf.Sqrt(1-y*y);
            var d=new Vector3(Mathf.Cos(a)*h,y,Mathf.Sin(a)*h);var derecha=Vector3.Cross(Vector3.up,d).normalized;var arriba=Vector3.Cross(d,derecha);
            float tam=radio*Mathf.Lerp(.00045f,.00125f,Mathf.Pow(Aleatorio(),2));
            var color=Color.Lerp(new Color(.69f,.80f,1),new Color(1,.91f,.76f),Aleatorio());color.a=Mathf.Lerp(.25f,1,Mathf.Pow(Aleatorio(),2))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.035f,.16f,y));
            int v=i*4;vertices[v]=d*radio+(-derecha-arriba)*tam;vertices[v+1]=d*radio+(derecha-arriba)*tam;vertices[v+2]=d*radio+(derecha+arriba)*tam;vertices[v+3]=d*radio+(-derecha+arriba)*tam;
            uv[v]=new Vector2(0,0);uv[v+1]=new Vector2(1,0);uv[v+2]=new Vector2(1,1);uv[v+3]=new Vector2(0,1);
            for(int j=0;j<4;j++)colores[v+j]=color;
            int t=i*6;indices[t]=v;indices[t+1]=v+1;indices[t+2]=v+2;indices[t+3]=v;indices[t+4]=v+2;indices[t+5]=v+3;
        }
        var mesh=new Mesh{name="Cielo_Estrellas_1300"};mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colores;mesh.triangles=indices;mesh.RecalculateBounds();
        var material=SinLuz("Cielo_Estrellas_1300",Color.white);material.shader=Shader.Find("Crater/EstrellasCielo");material.SetOverrideTag("RenderType","Transparent");material.renderQueue=1001;EditorUtility.SetDirty(material);
        var go=new GameObject("Cielo_Estrellas");go.transform.SetParent(padre,false);go.AddComponent<MeshFilter>().sharedMesh=GuardarMalla(mesh);
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.allowOcclusionWhenDynamic=false;
        return renderer;
    }
}
