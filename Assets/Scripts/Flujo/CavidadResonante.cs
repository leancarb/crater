using UnityEngine;

/// <summary>Dos capas suaves cambian con la proximidad, sin reiniciar el bucle en cada contacto.</summary>
public class CavidadResonante : MonoBehaviour
{
    public Transform oyente;
    public AudioSource viento, tono;
    public float Presencia {get;private set;}
    void Update()
    {
        if(oyente==null)return;
        float distancia=Vector3.Distance(oyente.position,transform.position);
        Presencia=Mathf.MoveTowards(Presencia,Mathf.SmoothStep(0,1,Mathf.InverseLerp(8,1.4f,distancia)),Time.deltaTime*.7f);
        Capa(viento,.12f*Presencia);Capa(tono,.035f*Presencia);
    }
    static void Capa(AudioSource a,float v){if(a==null)return;a.volume=v;if(v>.001f&&!a.isPlaying)a.Play();else if(v<=.001f&&a.isPlaying)a.Stop();}
    void OnDisable(){if(viento!=null)viento.Stop();if(tono!=null)tono.Stop();}
}
