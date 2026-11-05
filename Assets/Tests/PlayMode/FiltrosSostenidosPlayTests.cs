using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class FiltrosSostenidosPlayTests
{
    Mouse mouse;
    Gamepad mando;
    LinternaController farol;
    float escala;
    InputDevice[] dispositivosReales;

    [UnitySetUp] public IEnumerator Preparar()
    {
        escala=Time.timeScale;
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        var pausa=Object.FindFirstObjectByType<PausaCrater>();pausa.Reanudar();pausa.enabled=false; // Evitar que la captura del cursor del Editor reinicie el mouse simulado.
        var prologo=Object.FindFirstObjectByType<PrologoCapilla>();prologo.StopAllCoroutines();prologo.enabled=false;
        Object.FindFirstObjectByType<FlujoJuegoCrater>().StopAllCoroutines();
        farol=Object.FindFirstObjectByType<LinternaController>();farol.Recoger();farol.Encender(true);
        foreach(var f in farol.filtros)farol.Desbloquear(f);
        dispositivosReales=InputSystem.devices.Where(d=>d.enabled && (d is Mouse || d is Keyboard || d is Gamepad)).ToArray();
        foreach(var d in dispositivosReales)InputSystem.DisableDevice(d);
        mouse=InputSystem.AddDevice<Mouse>();mando=InputSystem.AddDevice<Gamepad>();
    }

    [UnityTearDown] public IEnumerator Restaurar()
    {
        Time.timeScale=escala;
        if(mouse!=null)InputSystem.RemoveDevice(mouse);
        if(mando!=null)InputSystem.RemoveDevice(mando);
        foreach(var d in dispositivosReales ?? new InputDevice[0])if(d.added)InputSystem.EnableDevice(d);
        yield return null;
    }

    [UnityTest] public IEnumerator MouseAgotaCincoSegundosYRequiereSoltarAntesDeReactivar()
    {
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return null;
        Assert.That(farol.FiltroActual,Is.EqualTo(farol.filtros[0]));
        Time.timeScale=5;yield return new WaitForSeconds(5.3f);
        Assert.That(farol.FiltroActual,Is.Null,"Vence a los cinco segundos aunque se mantenga presionado");
        yield return new WaitForSeconds(.5f);Assert.That(farol.FiltroActual,Is.Null,"No se reactiva hasta soltar");
        InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
        Assert.That(farol.FiltroActual,Is.Null);
        Time.timeScale=1;
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Right));yield return null;
        Assert.That(farol.FiltroActual,Is.EqualTo(farol.filtros[1]));
        InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
        Assert.That(farol.FiltroActual,Is.Null,"Soltar también cancela un cambio todavía en curso");
    }

    [UnityTest] public IEnumerator MandoUsaHombrosYBotonXParaElFarol()
    {
        InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.RightShoulder));yield return null;
        Assert.That(farol.Encendida,Is.True);Assert.That(farol.FiltroActual,Is.EqualTo(farol.filtros[0]));
        InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.RightShoulder).WithButton(GamepadButton.LeftShoulder));yield return null;
        Assert.That(farol.FiltroActual,Is.EqualTo(farol.filtros[1]));
        InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.RightShoulder));yield return null;
        Assert.That(farol.FiltroActual,Is.EqualTo(farol.filtros[0]));
        InputSystem.QueueStateEvent(mando,new GamepadState());yield return null;
        Assert.That(farol.FiltroActual,Is.Null);
        InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.West));yield return null;
        Assert.That(farol.Encendida,Is.False,"X de Xbox apaga, los hombros no alternan el encendido");
    }

    [UnityTest] public IEnumerator PuertaSolVisibleMientrasSeActivaElAncla()
    {
        Vector3 P(Vector3 p)=>EspacioCrater.Punto(p+new Vector3(-3,0,11));
        var jugador=Object.FindFirstObjectByType<JugadorFPS>();var cc=jugador.GetComponent<CharacterController>();
        cc.enabled=false;jugador.transform.position=P(new Vector3(-34,.02f,11));cc.enabled=true;
        var ancla=Object.FindObjectsByType<Ancla>(FindObjectsSortMode.None).Single(a=>a.name=="Ancla_O_Puerta");
        var puerta=GameObject.Find("Puerta_O_Ancla").GetComponent<Compuerta>();
        jugador.MirarHacia(ancla.PuntoDeImpacto);
        var punto=jugador.camara.GetComponent<Camera>().WorldToViewportPoint(puerta.transform.position);
        Assert.That(punto.z,Is.GreaterThan(0));Assert.That(punto.x,Is.InRange(.05f,.95f));Assert.That(punto.y,Is.InRange(.05f,.95f));
        Assert.That(GameObject.Find("Luz_Bajo_Puerta_SOL"),Is.Not.Null);
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
        float hasta=Time.time+3;
        while(!puerta.Abierta && Time.time<hasta)yield return null;
        Assert.That(ancla.Activo,Is.True);Assert.That(puerta.Abierta,Is.True);
    }

    [UnityTest] public IEnumerator SoltarFiltroAnimaElRegresoABlanco()
    {
        Assert.That(farol.demoraDeCambio,Is.EqualTo(.18f).Within(.001f));
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));
        yield return new WaitForSeconds(.5f);
        Assert.That(farol.FiltroActual,Is.EqualTo(farol.filtros[0]));
        InputSystem.QueueStateEvent(mouse,new MouseState());yield return null;
        Assert.That(farol.FiltroActual,Is.Null);
        Assert.That(farol.CambiandoFiltro,Is.True,"Soltar debe conservar una transición visual");
        yield return new WaitForSeconds(.5f);
        Assert.That(farol.CambiandoFiltro,Is.False);
        Assert.That(Vector4.Distance(farol.spot.color,farol.colorBase),Is.LessThan(.01f));
    }

    [UnityTest] public IEnumerator MuralesMateYEntradaMasAdelanteSinCajas()
    {
        var murales=GameObject.Find("Historia_Nueva");Assert.That(murales.transform.childCount,Is.EqualTo(12));
        foreach(var r in murales.GetComponentsInChildren<Renderer>().Where(r=>r.name=="PinturaMural"))
        {
            Assert.That(r.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.False);
            var imagen=(Texture2D)r.sharedMaterial.GetTexture("_BaseMap");Assert.That(imagen,Is.Not.Null);
            Assert.That(r.transform.lossyScale.x/r.transform.lossyScale.y,Is.EqualTo(imagen.width/(float)imagen.height).Within(.005f));
            Assert.That(r.sharedMaterial.GetFloat("_AlphaClip"),Is.EqualTo(1));Assert.That(r.GetComponent<Collider>(),Is.Null);
        }
        Assert.That(GameObject.Find("Murales_Runicos"),Is.Null);
        var crater=GameObject.Find("Crater_Valle");
        Assert.That(crater.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Borde_")),Is.False);
        Assert.That(crater.GetComponentsInChildren<Light>(true).Any(l=>l.name=="Luz_Borde_Apertura"),Is.False);
        var zona=GameObject.Find("Zona_Puerta_Crater").GetComponent<BoxCollider>();
        Assert.That(zona.transform.position.z,Is.EqualTo(-35.5f).Within(.01f));
        Assert.That(zona.size,Is.EqualTo(new Vector3(16,6,8)));
        yield return null;
    }
    [UnityTest] public IEnumerator TalladosQuedanBajoElBordeAlAbrir()
    {
        var prologo=Object.FindFirstObjectByType<PrologoCapilla>();
        typeof(PrologoCapilla).GetMethod("AplicarRevelado",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(prologo,new object[]{1f});
        yield return null;
        var tallados=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Sol_Escalera_")||r.name.StartsWith("Luna_Escalera_")).ToArray();
        Assert.That(tallados.Length,Is.EqualTo(8));
        foreach(var r in tallados)Assert.That(r.bounds.max.y,Is.LessThan(8.66f),r.name);
        Assert.That(GameObject.Find("Crater_Valle").GetComponentsInChildren<Light>(true).Any(l=>l.name=="Luz_Borde_Apertura"),Is.False);
    }
}
