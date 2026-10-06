using System.Collections;
using UnityEngine;

/// <summary>Presentación de la colección: espera una mirada cercana, conserva control y respeta pausa.</summary>
public class ConjuntoTallado : MonoBehaviour
{
    public ColeccionPiedras coleccion;
    public Renderer[] fragmentos;
    public Renderer proyeccion;
    public Light luz;
    public AudioSource resonancias, llamada;
    public AudioSource[] voces;
    public AudioClip nota;
    public bool Transformado { get; private set; }
    public bool Reproduciendo { get; private set; }
    public float Avance { get; private set; }
    public int VecesEscuchado { get; private set; }
    readonly bool[] presentes = new bool[6];
    readonly float[] brillo = new float[6];
    MaterialPropertyBlock bloque;
    float siguienteLlamada;
    static readonly int Emision=Shader.PropertyToID("_EmissionColor");
    void Awake()
    {
        bloque=new MaterialPropertyBlock();
        foreach(var r in fragmentos) if(r!=null) r.enabled=false;
        if(proyeccion!=null) proyeccion.enabled=false;
        if(luz!=null) luz.intensity=0;
    }
    public void MostrarPieza(int i)
    {
        if(i<0 || i>=6)return; presentes[i]=true; if(fragmentos[i]!=null) fragmentos[i].enabled=true;
    }
    void Update()
    {
        for(int i=0;i<6;i++)
        {
            brillo[i]=Mathf.MoveTowards(brillo[i],presentes[i]?(Transformado?0.6f:(Reproduciendo?Mathf.Lerp(.08f,.6f,Avance):.08f)):0,Time.deltaTime*0.45f);
            if(fragmentos[i]==null)continue;
            fragmentos[i].GetPropertyBlock(bloque);bloque.SetColor(Emision,new Color(0.6f,0.74f,1)*brillo[i]);fragmentos[i].SetPropertyBlock(bloque);
        }
        if(coleccion==null || !coleccion.Completa || Transformado || Reproduciendo || !coleccion.PuedeInteractuar)return;
        if(coleccion.EnVista(transform,9f)) Reproducir();
        else if(Time.time>siguienteLlamada && llamada!=null && Vector3.Distance(coleccion.camara.transform.position,transform.position)<24)
        { llamada.Play();siguienteLlamada=Time.time+16; }
    }
    public void Reproducir()
    {
        if(Reproduciendo || coleccion==null || !coleccion.Completa)return;
        StartCoroutine(Secuencia());
    }
    IEnumerator Secuencia()
    {
        Reproduciendo=true; VecesEscuchado++; if(llamada!=null)llamada.Stop();
        int indice=0;
        for(float t=0;t<5f;)
        {
            if(!coleccion.PuedeInteractuar){foreach(var voz in voces)if(voz!=null)voz.Pause();yield return null;continue;}
            foreach(var voz in voces)if(voz!=null)voz.UnPause();
            Avance=Mathf.Clamp01(t/5);
            if(luz!=null)luz.intensity=4*(Transformado?1:Mathf.SmoothStep(0,1,Avance));
            if(proyeccion!=null){proyeccion.enabled=true;proyeccion.GetPropertyBlock(bloque);bloque.SetColor(Emision,new Color(.6f,.74f,1)*(Transformado?1:Avance)*.7f);bloque.SetColor("_BaseColor",new Color(.55f,.67f,.8f)*(Transformado?1:Mathf.SmoothStep(0,1,Avance)));proyeccion.SetPropertyBlock(bloque);}
            if(indice<6 && t>=indice*.65f && !PausaCrater.EnPausa && !CinematicaDeSello.Reproduciendo)
            { if(voces[indice]!=null)voces[indice].PlayOneShot(nota,.14f);indice++; }
            t+=Time.deltaTime;yield return null;
        }
        Avance=1; Transformado=true; Reproduciendo=false;
    }
    void OnDisable(){StopAllCoroutines();Reproduciendo=false;if(voces!=null)foreach(var voz in voces)if(voz!=null)voz.Stop();if(llamada!=null)llamada.Stop();}
}
