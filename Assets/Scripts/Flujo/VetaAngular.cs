using UnityEngine;
/// <summary>Relieve decorativo de veta: cambia con la incidencia, sin carga ni eventos de progreso.</summary>
public class VetaAngular : MonoBehaviour
{
    public Renderer[] vetas;
    public LinternaController linterna;
    public Vector3 normal=Vector3.right;
    public float Respuesta {get;private set;}
    MaterialPropertyBlock bloque;
    static readonly int Base=Shader.PropertyToID("_BaseColor");
    static readonly int Suavidad=Shader.PropertyToID("_Smoothness");
    void Awake(){bloque=new MaterialPropertyBlock();}
    void Update()
    {
        float valor=0;
        if(linterna!=null && linterna.Encendida && linterna.spot != null)
        {
            Vector3 d=transform.position-linterna.spot.transform.position;float distancia=d.magnitude;
            if(distancia<9 && Vector3.Dot(linterna.spot.transform.forward,d.normalized)>Mathf.Cos(linterna.AnguloActual*.5f*Mathf.Deg2Rad) &&
                !Physics.Raycast(linterna.spot.transform.position,d.normalized,Mathf.Max(0,distancia-.25f),linterna.capaObstaculos,QueryTriggerInteraction.Ignore))
                valor=Mathf.Lerp(.5f,2.2f,1-Mathf.Abs(Vector3.Dot(normal.normalized,-d.normalized)));
        }
        Respuesta=Mathf.MoveTowards(Respuesta,valor,Time.deltaTime*3f);
        foreach(var r in vetas){r.GetPropertyBlock(bloque);bloque.SetColor(Base,new Color(.08f,.1f,.13f)+new Color(.7f,.75f,.82f)*Respuesta);bloque.SetFloat(Suavidad,.3f+Respuesta);r.SetPropertyBlock(bloque);}
    }
}
