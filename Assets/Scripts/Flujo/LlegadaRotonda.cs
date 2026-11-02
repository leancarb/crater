using UnityEngine;

/// <summary>El farol inicia el lobby. No mueve la cámara ni bloquea al jugador.</summary>
public class LlegadaRotonda : MonoBehaviour
{
    public LinternaController farol;
    public Compuerta entrada;
    public Light[] luces;
    public float duracion = 2.5f;
    public bool Iniciada { get; private set; }
    public float Progreso { get; private set; }
    float[] intensidades;
    void Awake()
    {
        intensidades = new float[luces.Length];
        for(int i=0;i<luces.Length;i++)if(luces[i]!=null)
        { intensidades[i]=luces[i].intensity;luces[i].intensity=0; }
    }
    void OnEnable(){if(farol!=null)farol.AlRecogerLinterna+=Iniciar;}
    void OnDisable(){if(farol!=null)farol.AlRecogerLinterna-=Iniciar;}
    public void Iniciar(){Iniciada=true;}
    void Update()
    {
        if(!Iniciada || PausaCrater.EnPausa)return;
        entrada?.Cerrar();
        Progreso=Mathf.MoveTowards(Progreso,1,Time.deltaTime/Mathf.Max(.1f,duracion));
        for(int i=0;i<luces.Length;i++)if(luces[i]!=null)
            luces[i].intensity=intensidades[i]*Mathf.SmoothStep(0,1,Progreso);
    }
}
