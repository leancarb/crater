using UnityEngine;
/// <summary>Encuadre ancho para observar un motivo dividido en planos; sólo una resonancia breve.</summary>
public class MotivoPorPerspectiva : MonoBehaviour
{
    public Transform mirada;
    public Vector3 puntoDeVista;
    public AudioSource nota;
    public Renderer[] fragmentos;
    MaterialPropertyBlock bloque;
    float presencia;
    static readonly int Emision=Shader.PropertyToID("_EmissionColor");
    public bool Alineado {get;private set;}
    public LineRenderer guia;
    public Transform destino;
    public Light luzCenital;
    float siguiente;
    void Update()
    {
        if(mirada==null)return;
        var diferencia=mirada.position-puntoDeVista;
        // Un círculo recompuesto sigue alineado aunque no esté exactamente en el centro de pantalla.
        // El margen de salida evita que el cabeceo o pequeños ajustes del stick apaguen la respuesta.
        float radio=Alineado?.9f:.65f,angulo=Alineado?16f:12f;
        Alineado=new Vector2(diferencia.x,diferencia.z).magnitude<radio && Mathf.Abs(diferencia.y)<.45f && Vector3.Dot(mirada.forward,(transform.position-mirada.position).normalized)>Mathf.Cos(angulo*Mathf.Deg2Rad);
        if(guia!=null)guia.enabled=false;
        if(luzCenital!=null)luzCenital.intensity=Mathf.MoveTowards(luzCenital.intensity,Alineado&&destino!=null&&destino.gameObject.activeInHierarchy?9f:1.5f,Time.deltaTime*8f);
        presencia=Mathf.MoveTowards(presencia,Alineado?1:0,Time.deltaTime*2);
        if(fragmentos!=null){bloque ??= new MaterialPropertyBlock();foreach(var r in fragmentos)if(r!=null){r.GetPropertyBlock(bloque);bloque.SetColor(Emision,new Color(1,.87f,.65f)*presencia*.5f);r.SetPropertyBlock(bloque);}}
        if(Alineado && Time.time>siguiente && !PausaCrater.EnPausa){nota?.Play();siguiente=Time.time+12;}
    }
}
