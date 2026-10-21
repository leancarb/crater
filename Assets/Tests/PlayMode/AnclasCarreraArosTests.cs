using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class AnclasCarreraArosTests
{
    [UnitySetUp] public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        Object.FindFirstObjectByType<PausaCrater>().Reanudar();
        Object.FindFirstObjectByType<PrologoCapilla>().enabled=false;
    }
    [UnityTest] public IEnumerator LosArosEsperanLasSeisColumnasYNoSobresalenDelTecho()
    {
        var o=Object.FindFirstObjectByType<ObeliscoDelEclipse>();var a=o.arosCompletos;
        Assert.That(a,Is.Not.Null);Assert.That(a.Encendido,Is.False);
        var columnas=o.columnasSol.Concat(o.columnasLuna).ToArray();
        foreach(var c in columnas.Take(5))c.Encender();
        yield return new WaitForSeconds(.8f);Assert.That(a.Encendido,Is.False);
        columnas[5].Encender();yield return new WaitForSeconds(.8f);Assert.That(a.Encendido,Is.True);
        yield return new WaitForSeconds(1.1f);Assert.That(a.Nivel,Is.EqualTo(1));
        Assert.That(a.luz.enabled&&a.luzSuperior.enabled,Is.True);
        var piso=a.renderers.Single(r=>r.name=="Aro_Obelisco_Piso").bounds;
        var techo=a.renderers.Single(r=>r.name=="Aro_Obelisco_Techo").bounds;
        Assert.That(piso.center.y,Is.EqualTo(-1.055f).Within(.001));
        Assert.That(piso.min.y,Is.GreaterThan(GameObject.Find("Anillo_Piso").GetComponent<Renderer>().bounds.max.y));
        Assert.That(piso.size.x,Is.EqualTo(9.62f).Within(.01));
        Assert.That(techo.max.y,Is.LessThan(7));
        var cubierta=GameObject.Find("Techo_Rotonda_Circular").GetComponent<MeshCollider>();
        var centro=new Vector2(techo.center.x,techo.center.z);
        float Radio(Vector3 v){var w=cubierta.transform.TransformPoint(v);return Vector2.Distance(new Vector2(w.x,w.z),centro);}
        float abertura=cubierta.sharedMesh.vertices.Min(Radio);
        var aro=a.renderers.Single(r=>r.name=="Aro_Obelisco_Techo").GetComponent<MeshFilter>();
        float radioAro=aro.sharedMesh.vertices.Min(v=>{var w=aro.transform.TransformPoint(v);return Vector2.Distance(new Vector2(w.x,w.z),centro);});
        Assert.That(abertura,Is.EqualTo(4.75f).Within(.001));
        Assert.That(radioAro,Is.EqualTo(abertura).Within(.001),"El aro debe coincidir con el borde de la abertura");
        Assert.That(a.transform.Find("Canto_Abertura_Obelisco"),Is.Not.Null);
        var carcasa=a.transform.Find("Carcasa_Aro_Obelisco").GetComponent<Renderer>().bounds;
        Assert.That(carcasa.min.y,Is.GreaterThan(techo.max.y));Assert.That(carcasa.max.y,Is.LessThan(7));
    }
    [Test] public void LasParejasSolaresSeAbrenACuarentaYCincoGradosYElBordeCuelgaDelTecho()
    {
        foreach(var puente in Object.FindObjectsByType<PuenteLuz>(FindObjectsSortMode.None).Where(p=>p.anclas.Count==2))
        {
            var a=puente.anclas[0].transform.forward;var b=puente.anclas[1].transform.forward;
            a.y=0;b.y=0;Assert.That(Vector3.Angle(a,b),Is.EqualTo(90).Within(.1),puente.name);
        }
        var borde=GameObject.Find("Puente_N_Borde").GetComponent<PuenteLuz>();
        foreach(var a in borde.anclas)
        {
            Assert.That(a.transform.position.y,Is.EqualTo(5.95f).Within(.01));
            Assert.That(a.transform.up.y,Is.LessThan(0));Assert.That(a.PuntoDeImpacto.y,Is.LessThan(5.95f));
            Assert.That(a.retencion,Is.EqualTo(1.9f));
        }
        var cierre=GameObject.Find("Cierre_Rocoso_Valle").GetComponentsInChildren<MeshCollider>();
        Assert.That(cierre.Length,Is.EqualTo(42));
        Assert.That(cierre.All(c=>c.sharedMesh!=null),Is.True);
    }
    [UnityTest] public IEnumerator ElSegundoPuentePermiteCorrerPeroNoAlcanzaCaminando()
    {
        var teclado=InputSystem.AddDevice<Keyboard>();teclado.MakeCurrent();
        try
        {
            foreach(bool correr in new[]{false,true})
            {
                if(correr){yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;Object.FindFirstObjectByType<PausaCrater>().Reanudar();Object.FindFirstObjectByType<PrologoCapilla>().enabled=false;}
                var j=Object.FindFirstObjectByType<JugadorFPS>();var cc=j.GetComponent<CharacterController>();
                cc.enabled=false;j.transform.position=EspacioCrater.Punto(new Vector3(0,.05f,44.5f));j.ReiniciarMovimiento();cc.enabled=true;
                var l=LinternaController.Instancia;l.Recoger();l.Encender(true);l.EquiparFiltro(l.filtros[0]);while(l.CambiandoFiltro)yield return null;
                var p=GameObject.Find("Puente_N_Borde").GetComponent<PuenteLuz>();
                foreach(var a in p.anclas){j.MirarHacia(a.PuntoDeImpacto);yield return new WaitForSeconds(.5f);Assert.That(a.Activo,Is.True,a.name);}
                yield return new WaitForSeconds(.2f);Assert.That(p.Solido,Is.True);
                j.MirarHacia(j.camara.position+Vector3.forward*10);
                InputSystem.QueueStateEvent(teclado,correr?new KeyboardState(Key.W,Key.LeftShift):new KeyboardState(Key.W));
                bool cayo=false;float destino=EspacioCrater.Punto(new Vector3(0,0,51.8f)).z;
                for(float t=0;t<3;t+=Time.deltaTime){yield return null;if(j.transform.position.y<-.5f){cayo=true;break;}if(j.transform.position.z>=destino)break;}
                InputSystem.QueueStateEvent(teclado,new KeyboardState());yield return null;
                if(correr){Assert.That(cayo,Is.False);Assert.That(j.transform.position.z,Is.GreaterThanOrEqualTo(destino));}
                else Assert.That(cayo,Is.True,"El segundo puente todavía se cruza caminando");
            }
        }
        finally{InputSystem.RemoveDevice(teclado);}
    }
}
