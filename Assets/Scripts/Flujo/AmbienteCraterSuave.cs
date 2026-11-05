using UnityEngine;

/// <summary>Aire sin tonos musicales: dos bucles filtrados suaves, sin alterar los WAV del proyecto.</summary>
public class AmbienteCraterSuave : MonoBehaviour
{
    public AudioSource exterior,interior,cavidad;
    static AudioClip aireInterior;
    void Awake()
    {
        if(aireInterior==null)aireInterior=CrearAire("Interior_Aire_Piedra",831,1400,.10f);
        // El exterior conserva el WAV original configurado por el constructor.
        Asignar(interior,aireInterior);Asignar(cavidad,aireInterior);
    }
    static void Asignar(AudioSource fuente,AudioClip clip)
    {
        if(fuente==null)return;bool sonando=fuente.isPlaying;fuente.Stop();fuente.clip=clip;fuente.loop=true;fuente.dopplerLevel=0;if(sonando)fuente.Play();
    }
    static AudioClip CrearAire(string nombre,int semilla,float corte,float amplitud)
    {
        const int hz=22050,n=hz*40;var azar=new System.Random(semilla);var ruido=new float[n];var datos=new float[n];
        for(int i=0;i<n;i++)ruido[i]=(float)(azar.NextDouble()*2-1);
        float rapido=1-Mathf.Exp(-2*Mathf.PI*corte/hz),lento=1-Mathf.Exp(-2*Mathf.PI*180/hz),agudo=0,grave=0;
        // Una primera vuelta estabiliza el filtro para que la unión del bucle no haga click.
        for(int vuelta=0;vuelta<2;vuelta++)for(int i=0;i<n;i++)
        {
            agudo+=rapido*(ruido[i]-agudo);grave+=lento*(ruido[i]-grave);
            float fase=i/(float)n*Mathf.PI*2;
            float rafaga=.66f+.15f*Mathf.Sin(fase*3+.6f)+.10f*Mathf.Sin(fase*7+1.4f)+.06f*Mathf.Sin(fase*11);
            datos[i]=(agudo-grave)*rafaga*amplitud;
        }
        var clip=AudioClip.Create(nombre,n,1,hz,false);clip.SetData(datos,0);return clip;
    }
}
