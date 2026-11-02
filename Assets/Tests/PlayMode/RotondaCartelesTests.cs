using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RotondaCartelesTests
{
    [UnitySetUp] public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice"); yield return null;
        var p=Object.FindFirstObjectByType<PausaCrater>();p.pausarAlPerderFoco=false;p.Reanudar();
    }
    [UnityTearDown] public IEnumerator Limpiar()
    {
        Object.FindFirstObjectByType<PausaCrater>()?.Reanudar();yield return null;
    }
    [UnityTest] public IEnumerator LasTeclasUnoDosYaNoSeleccionanFiltros()
    {
        var teclado=InputSystem.AddDevice<Keyboard>();teclado.MakeCurrent();
        var l=LinternaController.Instancia;l.Recoger();l.Encender(true);
        foreach(var f in l.filtros)l.Desbloquear(f);
        try
        {
            foreach(var tecla in new[]{Key.Digit1,Key.Digit2,Key.Numpad1,Key.Numpad2})
            {
                InputSystem.QueueStateEvent(teclado,new KeyboardState(tecla));yield return null;
                Assert.That(l.FiltroActual,Is.Null);
                InputSystem.QueueStateEvent(teclado,new KeyboardState());yield return null;
            }
        }
        finally{InputSystem.RemoveDevice(teclado);}
    }
    [Test] public void LosDibujosIluminanLaEscaleraSinEmisionHDRExcesiva()
    {
        var dibujos=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(r=>r.name.StartsWith("Sol_Escalera_")||r.name.StartsWith("Luna_Escalera_")).ToArray();
        Assert.That(dibujos.Length,Is.EqualTo(8));
        foreach(var r in dibujos)
        {
            Assert.That(r.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.True);
            Assert.That(r.sharedMaterial.GetColor("_EmissionColor").maxColorComponent,Is.LessThan(1f));
            var luz=r.GetComponentInChildren<Light>();Assert.That(luz,Is.Not.Null);
            Assert.That(luz.type,Is.EqualTo(LightType.Spot));
            Assert.That(luz.intensity,Is.GreaterThan(0));Assert.That(luz.transform.forward.y,Is.LessThan(-.5f));
        }
        Assert.That(Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Any(l=>l.name=="Luz_Vela_Entrada (1)"||l.name=="Luz_Plaza"),Is.False);
    }
    [Test] public void CartelesYGeometriaRespondenALaRevision()
    {
        var ts=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Assert.That(ts.Any(t=>t.name=="Control_LuzBlanca"||t.name.StartsWith("Architecture_")||t.name=="ArquitecturaModular"),Is.False);
        var control=ts.Single(t=>t.name=="Control_Mover");
        Assert.That(control.parent.name,Is.EqualTo("Cartel_Controles_Inicio"));
        var inicio=ts.Single(t=>t.name=="SpawnValle");
        Assert.That(Mathf.Abs(Vector3.Dot((control.position-inicio.position).normalized,inicio.right)),Is.GreaterThan(.6f));
        var o=Object.FindFirstObjectByType<ObeliscoDelEclipse>();
        var columnas=ts.Select(t=>t.GetComponent<Renderer>()).Where(r=>r!=null&&r.name.StartsWith("Anillo_Columna_")&&r.GetComponent<Collider>()!=null).ToArray();
        Assert.That(columnas.Length,Is.EqualTo(6));
        foreach(var c in columnas)
        {
            Assert.That(c.bounds.min.y,Is.EqualTo(0).Within(.01f));
            Assert.That(c.bounds.max.y,Is.EqualTo(7).Within(.01f));
            Assert.That(Vector2.Distance(new Vector2(c.bounds.center.x,c.bounds.center.z),new Vector2(o.centro.x,o.centro.z)),Is.EqualTo(8.8f).Within(.03f));
        }
        Assert.That(ts.Count(t=>t.name=="Obelisco_Eclipse"),Is.Zero);
        var cuerpo=ts.Single(t=>t.name=="Anillo_Obelisco");
        Assert.That(cuerpo.GetComponent<Renderer>(),Is.Null);Assert.That(cuerpo.GetComponent<Collider>(),Is.Null);
        Assert.That(o.sol.renderers,Is.Empty);Assert.That(o.luna.renderers,Is.Empty);
        foreach(var t in o.columnasSol.Concat(o.columnasLuna))
        {
            Assert.That(t.renderers.Any(r=>r.name=="Aro_Luminoso_Piso"),Is.True);
            Assert.That(t.renderers.Any(r=>r.name=="Aro_Luminoso_Techo"),Is.True);
            Assert.That(t.luzSuperior,Is.Not.Null);
            Assert.That(t.luzSuperior.transform.position.y,Is.GreaterThan(6.5f));
            Assert.That(t.luzSuperior.transform.forward.y,Is.LessThan(-.8f));
            Assert.That(t.luz.transform.position.y,Is.LessThan(.2f));
            Assert.That(t.luz.transform.forward.y,Is.GreaterThan(.9f));
        }
    }
    [Test] public void LaTapaSeRetiraSinAchicarseYRecuperaSuCierre()
    {
        var p=Object.FindFirstObjectByType<PrologoCapilla>();
        var tapa=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="Tapa_Pozo");
        var aplicar=typeof(PrologoCapilla).GetMethod("AplicarRevelado",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        aplicar.Invoke(p,new object[]{0f});
        Vector3 escala=tapa.localScale, cerrada=tapa.position;
        foreach(float r in new[]{.15f,.3f,.5f,1f})
        {
            aplicar.Invoke(p,new object[]{r});
            Assert.That(Vector3.Distance(tapa.localScale,escala),Is.LessThan(.0001f),"La tierra se retira; no se encoge");
            Assert.That(tapa.GetComponent<Collider>().enabled,Is.False);
        }
        Assert.That(Vector3.Distance(tapa.position,cerrada),Is.GreaterThan(escala.x*.5f));
        Assert.That(tapa.gameObject.activeSelf,Is.False);
        aplicar.Invoke(p,new object[]{0f});
        Assert.That(Vector3.Distance(tapa.position,cerrada),Is.LessThan(.0001f));
        Assert.That(tapa.gameObject.activeSelf,Is.True);
        Assert.That(tapa.GetComponent<Collider>().enabled,Is.True);
    }
    [UnityTest] public IEnumerator LaAperturaSeIluminaSinMoverLaCamara()
    {
        var p=Object.FindFirstObjectByType<PrologoCapilla>();var j=Object.FindFirstObjectByType<JugadorFPS>();
        Vector3 camaraOriginal=j.camara.localPosition;
        var cc=j.GetComponent<CharacterController>();cc.enabled=false;
        j.transform.position=GameObject.Find("Zona_Mirador").transform.position;
        j.ReiniciarMovimiento();cc.enabled=true;j.movimientoBloqueado=true;
        var cielo=Object.FindFirstObjectByType<CieloEclipse>();
        j.MirarHacia(j.camara.position+cielo.DireccionSol*100);p.AvanzarEclipse(80);p.IniciarCinematica();
        var tapa=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Tapa_Pozo");
        Quaternion miradaOriginal=j.camara.rotation;
        bool vioApertura=false;
        for(float t=0;t<11;t+=Time.deltaTime)
        {
            if(!tapa.gameObject.activeSelf&&p.Reproduciendo)
            {
                vioApertura=true;
            }
            yield return null;
        }
        Assert.That(vioApertura,Is.True);
        Assert.That(Quaternion.Angle(j.camara.rotation,miradaOriginal),Is.LessThan(.1f));
        var luces=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.name=="Luz_Borde_Apertura").ToArray();
        Assert.That(luces.Length,Is.EqualTo(4));Assert.That(luces.All(l=>l.enabled&&l.intensity>0),Is.True);
        Assert.That(p.Reproduciendo,Is.False);Assert.That(j.enabled,Is.True);Assert.That(j.movimientoBloqueado,Is.False);
        Assert.That(Vector3.Distance(j.camara.localPosition,camaraOriginal),Is.LessThan(.001f));
    }
}
