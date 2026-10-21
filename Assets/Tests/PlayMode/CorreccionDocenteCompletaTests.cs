using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CorreccionDocenteCompletaTests
{
    [UnitySetUp] public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        var p=Object.FindFirstObjectByType<PausaCrater>();p.Reanudar();
    }
    [UnityTest] public IEnumerator CadaColumnaEnciendePisoYTechoEnLaMismaTransicion()
    {
        var o=Object.FindFirstObjectByType<ObeliscoDelEclipse>();
        foreach(var t in o.columnasSol.Concat(o.columnasLuna))
        {
            Assert.That(t.luzSuperior,Is.Not.Null);Assert.That(t.luzSuperior.enabled,Is.False);
            Assert.That(t.luz.enabled,Is.False);t.Encender();
        }
        yield return new WaitForSeconds(.7f);
        foreach(var t in o.columnasSol.Concat(o.columnasLuna))
        {
            Assert.That(t.luzSuperior.enabled&&t.luz.enabled,Is.True);
            Assert.That(t.luzSuperior.intensity/t.intensidadLuzSuperior,Is.EqualTo(t.luz.intensity/t.intensidadLuz).Within(.001f));
            Assert.That(t.renderers.Count(r=>r.name=="Aro_Luminoso_Techo"),Is.EqualTo(1));
            Assert.That(t.luzSuperior.transform.position.y,Is.GreaterThan(6.5f));
            Assert.That(t.luzSuperior.transform.forward.y,Is.LessThan(-.8f));
            var aro=t.renderers.Single(r=>r.name=="Aro_Luminoso_Techo").bounds;
            var carcasa=t.transform.Find("Techo_Columna/Carcasa_Aro_Techo").GetComponent<Renderer>().bounds;
            Assert.That(carcasa.max.y,Is.LessThan(7));
            Assert.That(carcasa.min.y,Is.GreaterThan(aro.max.y));
            Assert.That(carcasa.min.x,Is.LessThan(aro.min.x));Assert.That(carcasa.max.x,Is.GreaterThan(aro.max.x));
            Assert.That(carcasa.min.z,Is.LessThan(aro.min.z));Assert.That(carcasa.max.z,Is.GreaterThan(aro.max.z));
        }
    }
    [Test] public void LasLeccionesSeLeenDesdeSueloSeguroYNoTapanLasAnclas()
    {
        var jugador=Object.FindFirstObjectByType<JugadorFPS>();var cam=jugador.camara.GetComponent<Camera>();
        var casos=new[]{(EspacioCrater.Punto(new Vector3(-26.5f,.015f,12)),"Ensenanza_Dos_Anclas","Ancla_O_Ensenar_A","Ancla_O_Ensenar_B"),
            (EspacioCrater.Punto(new Vector3(-2,.015f,29.5f)),"Ensenanza_Cruce_Combinacion","Ancla_N_Vista","Ancla_N_Oculta")};
        foreach(var c in casos)
        {
            var pie=c.Item1;
            var a=Object.FindObjectsByType<Ancla>(FindObjectsSortMode.None).Single(a=>a.name==c.Item3);
            var b=Object.FindObjectsByType<Ancla>(FindObjectsSortMode.None).Single(a=>a.name==c.Item4);
            jugador.GetComponent<CharacterController>().enabled=false;jugador.transform.position=pie;
            jugador.MirarHacia((a.PuntoDeImpacto+b.PuntoDeImpacto)*.5f);
            var panel=GameObject.Find(c.Item2).transform;
            var visible=cam.WorldToViewportPoint(panel.position);
            Assert.That(visible.z,Is.GreaterThan(0),c.Item2);
            Assert.That(visible.x,Is.InRange(.05f,.95f),c.Item2);Assert.That(visible.y,Is.InRange(.05f,.95f),c.Item2);
            Assert.That(panel.GetComponentInChildren<Renderer>().bounds.min.y,Is.GreaterThan(1.9f));
            Physics.SyncTransforms();
            foreach(var receptor in new[]{a,b})
                foreach(var hit in Physics.RaycastAll(cam.transform.position,(receptor.PuntoDeImpacto-cam.transform.position).normalized,Vector3.Distance(receptor.PuntoDeImpacto,cam.transform.position),~0,QueryTriggerInteraction.Ignore))
                    Assert.That(hit.transform.IsChildOf(panel),Is.False,"La lección no bloquea el haz");
        }
        jugador.GetComponent<CharacterController>().enabled=true;
        Assert.That(GameObject.Find("Ensenanza_Cresta_Reflejo"),Is.Not.Null);
        Assert.That(GameObject.Find("Ensenanza_Cresta_Espera"),Is.Not.Null);
        foreach(string n in new[]{"Lectura_Primer_Puente","Lectura_Cruce","Lectura_Cresta","Guia_Piso_Cresta"})
            Assert.That(GameObject.Find(n),Is.Null);
        Assert.That(LinternaController.Instancia.duracionFiltro,Is.EqualTo(3));
    }
    [Test] public void ElExteriorOrientaConRocasSinPorticoNiMarcas()
    {
        foreach(string n in new[]{"Portico_Eclipse","Encuadre_Sol","Vista_Eclipse"})Assert.That(GameObject.Find(n),Is.Null);
        var rocas=GameObject.Find("Guia_Exterior_Eclipse").GetComponentsInChildren<MeshRenderer>();
        Assert.That(rocas.Count(r=>r.name=="Piedra_Ruta"),Is.EqualTo(14));
        Assert.That(rocas.Count(r=>r.name=="Afloramiento_Guia"),Is.EqualTo(2));
        var inicio=GameObject.Find("SpawnValle").transform;
        foreach(var r in rocas)
        {
            Assert.That(r.GetComponent<Collider>(),Is.Null);
            Assert.That(r.bounds.size.y,Is.LessThan(2.2f));
            Assert.That(Mathf.Abs(Vector3.Dot(r.bounds.center-inicio.position,inicio.right))-r.bounds.extents.x,Is.GreaterThan(2.5f));
        }
    }
    [UnityTest] public IEnumerator PerderFocoNoPausaPeroLaPausaManualSigueFuncionando()
    {
        var p=Object.FindFirstObjectByType<PausaCrater>();
        Assert.That(p.pausarAlPerderFoco,Is.False);Assert.That(Application.runInBackground,Is.True);
        p.SendMessage("OnApplicationFocus",false);yield return null;
        Assert.That(PausaCrater.EnPausa,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(AudioListener.pause,Is.False);
        var panel=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="Pausa");
        Assert.That(panel.gameObject.activeSelf,Is.False);
        p.SendMessage("OnApplicationFocus",true);yield return null;Assert.That(PausaCrater.EnPausa,Is.False);
        p.Pausar();Assert.That(PausaCrater.EnPausa,Is.True);Assert.That(panel.gameObject.activeSelf,Is.True);
        p.Reanudar();Assert.That(PausaCrater.EnPausa,Is.False);Assert.That(panel.gameObject.activeSelf,Is.False);
    }
}
