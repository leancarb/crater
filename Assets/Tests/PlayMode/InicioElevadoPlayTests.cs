using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class InicioElevadoPlayTests
{
    float escalaAnterior;

    [UnitySetUp] public IEnumerator Preparar()
    {
        escalaAnterior=Time.timeScale;
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        Object.FindFirstObjectByType<PausaCrater>().Reanudar();
        var prologo=Object.FindFirstObjectByType<PrologoCapilla>();prologo.StopAllCoroutines();prologo.enabled=false;
        Object.FindFirstObjectByType<FlujoJuegoCrater>().StopAllCoroutines();
    }

    [UnityTearDown] public IEnumerator Restaurar()
    { Time.timeScale=escalaAnterior;yield return null; }

    [UnityTest] public IEnumerator BajadaContinuaYFarolSinPuertaDeUmbral()
    {
        var exterior=GameObject.Find("06_Capilla").transform;
        var jugador=Object.FindFirstObjectByType<JugadorFPS>();
        var cc=jugador.GetComponent<CharacterController>();
        jugador.enabled=false;
        float alturaInicial=jugador.transform.position.y;
        var puntos=new[]{new Vector2(0,184),new Vector2(-12,184),new Vector2(-19,177),new Vector2(-19,165),new Vector2(-12,154),new Vector2(0,150)};
        for(int tramo=1;tramo<puntos.Length;tramo++)
        {
            int pasos=Mathf.CeilToInt(Vector2.Distance(puntos[tramo-1],puntos[tramo])/.1f);
            for(int i=0;i<pasos;i++)
            {
                var p=Vector2.Lerp(puntos[tramo-1],puntos[tramo],(i+1)/(float)pasos);
                var destino=exterior.TransformPoint(new Vector3(p.x,0,p.y));
                var delta=destino-jugador.transform.position;delta.y=0;
                cc.Move(Vector3.ClampMagnitude(delta,.12f)+Vector3.down*.15f);
                yield return null;
            }
        }
        Assert.That(exterior.InverseTransformPoint(jugador.transform.position).z,Is.EqualTo(150).Within(.2f));
        Assert.That(alturaInicial-jugador.transform.position.y,Is.InRange(4.7f,5.3f));
        Assert.That(GameObject.Find("Ancla_Umbral"),Is.Null);
        Assert.That(GameObject.Find("Compuerta_Umbral"),Is.Null);
        Assert.That(GameObject.Find("Piso_Umbral"),Is.Not.Null);
        Assert.That(GameObject.Find("Anillo_Obelisco").GetComponent<Renderer>(),Is.Null);
        var farol=Object.FindObjectsByType<Recogible>(FindObjectsSortMode.None).Single(r=>r.filtro==null);
        Assert.That(Vector2.Distance(new Vector2(farol.transform.position.x,farol.transform.position.z),new Vector2(0,20.75f)),Is.LessThan(.01f));
        var linterna=jugador.GetComponentInChildren<LinternaController>();
        linterna.Encender(false);Assert.That(farol.Recoger(linterna),Is.True);linterna.Encender(true);
        Assert.That(Object.FindFirstObjectByType<FlujoJuegoCrater>().EtapaActual,Is.EqualTo(FlujoJuegoCrater.Etapa.Explorar));
    }

    [UnityTest] public IEnumerator LasSeisColumnasSiguenAbriendoElCruceSinObeliscoVisible()
    {
        var progreso=Object.FindFirstObjectByType<ObeliscoDelEclipse>();
        var jugador=Object.FindFirstObjectByType<JugadorFPS>();jugador.enabled=false;
        var cc=jugador.GetComponent<CharacterController>();cc.enabled=false;
        jugador.transform.position=progreso.centro+Vector3.back*5;cc.enabled=true;
        var linterna=jugador.GetComponentInChildren<LinternaController>();
        progreso.sol.sello.RecibirLuz(linterna.filtros.Single(f=>f.canal==FiltroDefinicion.Canal.Cuerpo),10);
        progreso.luna.sello.RecibirLuz(linterna.filtros.Single(f=>f.canal==FiltroDefinicion.Canal.Hueco),10);
        Time.timeScale=5;
        float limite=Time.time+20;
        while(!progreso.puerta.Abierta && Time.time<limite)yield return null;
        Assert.That(progreso.puerta.Abierta,Is.True);
        Assert.That(progreso.columnasSol.Concat(progreso.columnasLuna).All(c=>c.Encendido),Is.True);
        Assert.That(progreso.arosCompletos.Encendido,Is.True);
    }
}
