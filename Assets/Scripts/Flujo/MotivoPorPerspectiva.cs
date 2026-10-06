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
    float siguiente;
    void Update()
    {
        if(mirada==null)return;
        Alineado=Vector3.Distance(mirada.position,puntoDeVista)<2f && Vector3.Dot(mirada.forward,(transform.position-mirada.position).normalized)>.9f;
        presencia=Mathf.MoveTowards(presencia,Alineado?1:0,Time.deltaTime*2);
        if(fragmentos!=null){bloque ??= new MaterialPropertyBlock();foreach(var r in fragmentos)if(r!=null){r.GetPropertyBlock(bloque);bloque.SetColor(Emision,new Color(1,.87f,.65f)*presencia*1.3f);r.SetPropertyBlock(bloque);}}
        if(Alineado && Time.time>siguiente && !PausaCrater.EnPausa){nota?.Play();siguiente=Time.time+12;}
    }
}
