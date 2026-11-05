using System.Collections.Generic;
using UnityEngine;

public partial class ParedEspejo
{
    public Vector3 centroCircular;
    public float radioCircular=14.8f;
    const int ColumnasCurvas=20,FilasCurvas=8;
    Mesh mallaCurva;
    Vector3[] verticesCurvos;
    Vector2[] uvCurvos;
    bool[] columnasValidas;
    bool[] verticesValidos;
    List<int> indicesCurvos;

    void ActualizarCircular(LinternaController farol)
    {
        Vector3 origen=farol.spot.transform.position,haz=farol.spot.transform.forward;
        if(!Physics.Raycast(origen,haz,out var hit,farol.AlcanceActual*1.5f,~0,QueryTriggerInteraction.Ignore)||System.Array.IndexOf(superficiesCirculares,hit.collider)<0){Apagar();return;}
        var radial=hit.point-centroCircular;radial.y=0;var afuera=radial.normalized;var normal=-afuera;
        float frontal=Vector3.Dot(haz,afuera);if(frontal<=.05f){Apagar();return;}
        float k=Mathf.Pow(frontal,concentracion)*Mathf.Lerp(.35f,1,1-Mathf.Clamp01(hit.distance/(farol.AlcanceActual*1.5f)));
        Encandilamiento=k;aparicion=Mathf.MoveTowards(aparicion,1,Time.deltaTime/DuracionAparicion);float brillo=k*Mathf.SmoothStep(0,1,aparicion);
        if(reflejo!=null)
        {
            if(mallaCurva==null)
            {
                mallaCurva=new Mesh{name="Reflejo_Cilindrico_Runtime",hideFlags=HideFlags.DontSave};mallaCurva.MarkDynamic();
                reflejo.GetComponent<MeshFilter>().sharedMesh=mallaCurva;
            }
            int total=(ColumnasCurvas+1)*(FilasCurvas+1);
            if(verticesCurvos==null){verticesCurvos=new Vector3[total];uvCurvos=new Vector2[total];columnasValidas=new bool[ColumnasCurvas+1];indicesCurvos=new List<int>(ColumnasCurvas*FilasCurvas*6);}
            verticesValidos??=new bool[total];
            float radioHaz=Mathf.Clamp(hit.distance*Mathf.Tan(farol.AnguloActual*.5f*Mathf.Deg2Rad)*tamanioReflejo,.3f,3f);
            float centroAngulo=Mathf.Atan2(radial.z,radial.x),angulo=radioHaz/radioCircular;
            float abajo=Mathf.Max(0,hit.point.y-radioHaz),arriba=Mathf.Min(7.85f,hit.point.y+radioHaz);
            for(int x=0;x<=ColumnasCurvas;x++)
            {
                float u=x/(float)ColumnasCurvas,t=centroAngulo+Mathf.Lerp(-angulo,angulo,u);var d=new Vector3(Mathf.Cos(t),0,Mathf.Sin(t));
                columnasValidas[x]=Physics.Raycast(centroCircular+Vector3.up*Mathf.Clamp(hit.point.y,.2f,7.5f),d,out var muro,radioCircular+2,~0,QueryTriggerInteraction.Ignore)&&System.Array.IndexOf(superficiesCirculares,muro.collider)>=0;
                for(int y=0;y<=FilasCurvas;y++)
                {
                    int i=x*(FilasCurvas+1)+y;float alto=Mathf.Lerp(abajo,arriba,y/(float)FilasCurvas);
                    var inicio=centroCircular+Vector3.up*Mathf.Clamp(alto,.01f,7.84f);
                    verticesValidos[i]=Physics.Raycast(inicio,d,out var superficie,radioCircular+2,~0,QueryTriggerInteraction.Ignore)&&System.Array.IndexOf(superficiesCirculares,superficie.collider)>=0;
                    var punto=verticesValidos[i]?superficie.point+superficie.normal*.10f:centroCircular+d*radioCircular+Vector3.up*alto;
                    verticesCurvos[i]=reflejo.transform.InverseTransformPoint(punto);
                    uvCurvos[i]=new Vector2(u,(alto-hit.point.y+radioHaz)/(radioHaz*2));
                }
            }
            indicesCurvos.Clear();
            for(int x=0;x<ColumnasCurvas;x++)for(int y=0;y<FilasCurvas;y++)
            {int a=x*(FilasCurvas+1)+y,b=a+FilasCurvas+1;if(!verticesValidos[a]||!verticesValidos[b]||!verticesValidos[b+1]||!verticesValidos[a+1])continue;indicesCurvos.Add(a);indicesCurvos.Add(b);indicesCurvos.Add(b+1);indicesCurvos.Add(a);indicesCurvos.Add(b+1);indicesCurvos.Add(a+1);}
            mallaCurva.SetVertices(verticesCurvos);mallaCurva.SetUVs(0,uvCurvos);mallaCurva.SetTriangles(indicesCurvos,0);mallaCurva.RecalculateBounds();
            reflejo.enabled=brillo>.001f&&indicesCurvos.Count>0;bloque??=new MaterialPropertyBlock();reflejo.GetPropertyBlock(bloque);
            var color=farol.ColorActual*intensidadReflejo*brillo;color.a=Mathf.Clamp01(brillo);bloque.SetColor(IdColor,color);bloque.SetVector(IdMapa,new Vector4(1,1,0,0));reflejo.SetPropertyBlock(bloque);
        }
        if(luzRebote!=null){luzRebote.enabled=brillo>.001f;luzRebote.transform.position=hit.point+normal*.7f;luzRebote.color=farol.ColorActual;luzRebote.intensity=intensidadRebote*brillo;}
    }
    void OnDestroy(){if(mallaCurva!=null)Destroy(mallaCurva);}
}
