using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CorreccionEclipseAudioTests
{
    PrologoCapilla prologo;
    CieloEclipse cielo;
    JugadorFPS jugador;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");
        yield return null;
        Object.FindFirstObjectByType<PausaCrater>().pausarAlPerderFoco = false;
        Object.FindFirstObjectByType<PausaCrater>().Reanudar();
        prologo = Object.FindFirstObjectByType<PrologoCapilla>();
        cielo = Object.FindFirstObjectByType<CieloEclipse>();
        jugador = Object.FindFirstObjectByType<JugadorFPS>();
        Time.timeScale = 1f;
        yield return new WaitForSeconds(6);
    }

    void Situar(Vector3 piso)
    {
        var cc = jugador.GetComponent<CharacterController>();
        cc.enabled = false; jugador.transform.position = piso;
        jugador.ReiniciarMovimiento(); cc.enabled = true;
    }
    float Elevacion => Mathf.Asin(cielo.DireccionSol.y) * Mathf.Rad2Deg;

    [UnityTest]
    public IEnumerator ElSolMantieneElHorizonteExteriorYSeVeEnLaPlaza()
    {
        Assert.That(Elevacion, Is.EqualTo(14).Within(.1f), "el sol exterior quedó alto");
        var direccionInicial = cielo.DireccionSol;
        jugador.MirarHacia(jugador.camara.position + Vector3.forward * 100);
        Assert.That(Vector3.Dot(jugador.camara.forward, direccionInicial), Is.GreaterThan(.96f), "el sol queda fuera de una mirada horizontal durante la aproximación");
        ExploracionCraterTests.Capturar(jugador.GetComponentInChildren<Camera>(), "19_Eclipse_Horizonte_Inicial");
        prologo.SendMessage("EntrarSinTransicion");
        Assert.That(Elevacion, Is.EqualTo(75).Within(.1f));
        int i=0;
        foreach (var piso in new[]{new Vector3(-2,.05f,5),new Vector3(-3,-1.15f,10),new Vector3(3,-1.15f,10)})
        {
            Situar(EspacioCrater.Punto(piso));
            jugador.MirarHacia(jugador.camara.position + cielo.DireccionSol * 100);
            yield return new WaitForSeconds(.3f);
            Assert.That(Physics.Raycast(jugador.camara.position, cielo.DireccionSol,150,~LayerMask.GetMask("Jugador","Ignore Raycast"),QueryTriggerInteraction.Ignore),Is.False,"la geometría tapa el eclipse desde "+piso);
            cielo.ActualizarVista();
            string captura="20_Eclipse_Plaza_"+i++;
            ExploracionCraterTests.Capturar(jugador.GetComponentInChildren<Camera>(),captura);
            var imagen=new Texture2D(2,2);imagen.LoadImage(System.IO.File.ReadAllBytes("Docs/Previews/Exploracion/"+captura+".png"));
            int coronaVisible=0;
            for(int x=490;x<790;x++)for(int y=210;y<510;y++)
            {
                float r=Vector2.Distance(new Vector2(x,y),new Vector2(640,360));
                var color=imagen.GetPixel(x,y);
                if(r>35&&r<140&&color.b>.2f&&color.b>=color.r*.97f)coronaVisible++;
            }
            Object.Destroy(imagen);
            Assert.That(coronaVisible,Is.GreaterThan(500),"hay línea de visión pero la corona no aparece en la captura: "+captura);
            Assert.That(RenderSettings.fogDensity,Is.EqualTo(.02f).Within(.0001f),"se alteró la niebla interior para hacer visible el cielo");
        }
        // Retorno opcional a buscar la pieza exterior, sin finalizar la partida.
        var boca=GameObject.Find("Boca_Crater").transform.position;
        Situar(boca + new Vector3(0,.05f,-4));yield return null;
        Assert.That(RenderSettings.fogDensity,Is.EqualTo(.003f).Within(.0001f));
        Assert.That(Vector3.Angle(cielo.DireccionSol,direccionInicial),Is.LessThan(.1f));
        Situar(EspacioCrater.Punto(new Vector3(-3,-1.15f,10)));yield return null;
        Assert.That(Elevacion,Is.EqualTo(75).Within(.1f));
        Assert.That(RenderSettings.fogDensity,Is.EqualTo(.02f).Within(.0001f));
        prologo.PrepararEpilogo();yield return null;
        Assert.That(Vector3.Angle(cielo.DireccionSol,direccionInicial),Is.GreaterThan(60f),"el regreso final debe mostrar paso del tiempo");
        Assert.That(Elevacion,Is.EqualTo(18f).Within(.1f));
        Assert.That(cielo.Progreso,Is.Zero);
        Assert.That(prologo.EnElCrater,Is.False);
    }

    [UnityTest]
    public IEnumerator ElBucleGlobalNoVuelveAlEntrarNiTrasUnaTransicion()
    {
        var ambiente=Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(a=>a.clip!=null&&a.clip.name=="ambiente_crater");
        Assert.That(ambiente.isPlaying,Is.False);
        prologo.SendMessage("EntrarSinTransicion");yield return null;
        Assert.That(ambiente.isPlaying,Is.False);
        Object.FindFirstObjectByType<EclipseFinalController>().MezclarAlCrater(.2f);
        yield return new WaitForSeconds(.3f);
        Assert.That(ambiente.isPlaying,Is.False,"el crossfade volvió a iniciar el oleaje");
        var cavidad=Object.FindFirstObjectByType<CavidadResonante>();
        Situar(cavidad.transform.position-Vector3.up);
        yield return new WaitForSeconds(1);
        Assert.That(cavidad.viento.isPlaying,Is.True,"se perdió el detalle localizado de la cavidad");
        Situar(EspacioCrater.Punto(new Vector3(10,.05f,12)));
        yield return new WaitForSeconds(2);
        Assert.That(cavidad.viento.isPlaying,Is.False);
    }

    [UnityTest]
    public IEnumerator LaAperturaLlegaAlMiradorYNoCompiteConElGrave()
    {
        Situar(GameObject.Find("Zona_Mirador").transform.position);
        jugador.MirarHacia(jugador.camara.position+cielo.DireccionSol*100);
        prologo.AvanzarEclipse(80);
        prologo.IniciarCinematica();
        var apertura=GameObject.Find("Apertura_Tierra").GetComponent<AudioSource>();
        var grave=Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Single(a=>a.clip!=null&&a.clip.name=="eclipse_grave");
        bool escuchado=false;float maxRms=0,primerRms=0;var muestras=new float[2048];
        for(float t=0;t<9;t+=Time.deltaTime)
        {
            if(apertura.isPlaying)
            {
                escuchado=true;
                Assert.That(Vector3.Distance(apertura.transform.position,jugador.camara.position),Is.LessThanOrEqualTo(apertura.minDistance),"la apertura se atenúa antes de llegar al mirador");
                AudioListener.GetOutputData(muestras,0);
                float rms=Mathf.Sqrt(muestras.Sum(x=>x*x)/muestras.Length);
                if(primerRms==0)primerRms=rms;maxRms=Mathf.Max(maxRms,rms);
            }
            yield return null;
        }
        Assert.That(escuchado,Is.True);
        Assert.That(grave.isPlaying,Is.False,"el grave sigue enmascarando la apertura");
        Assert.That(prologo.Reproduciendo,Is.False);
        Assert.That(jugador.movimientoBloqueado,Is.False);
        System.IO.File.WriteAllText("Logs/eclipse_apertura_salida.txt","RMS máximo salida canal 0="+maxRms+"; primer RMS="+primerRms+"; distancia="+Vector3.Distance(apertura.transform.position,jugador.camara.position)+"; mínimo="+apertura.minDistance+"; volumen="+apertura.volume+"; duración efectiva="+apertura.clip.length/apertura.pitch+"\nNo es escucha humana ni prueba de toda la mezcla.");
        Assert.That(maxRms,Is.GreaterThan(.005f),"la salida real de la apertura está ausente o silenciada");
    }
}
