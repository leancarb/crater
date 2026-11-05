using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public class RevisionNoviembreTests
{
    InputSettings.BackgroundBehavior comportamientoFondo;
    InputSettings.EditorInputBehaviorInPlayMode rutaEntradaEditor;
    JugadorFPS jugador;Keyboard teclado;Mouse mouse;Gamepad mando;InputDevice[] reales;
    Transform Buscar(string n)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name==n);
    [UnitySetUp] public IEnumerator Preparar()
    {
        rutaEntradaEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;comportamientoFondo=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        var pausa=Object.FindFirstObjectByType<PausaCrater>();pausa.Reanudar();pausa.enabled=false;
        var p=Object.FindFirstObjectByType<PrologoCapilla>();p.StopAllCoroutines();p.enabled=false;
        Object.FindFirstObjectByType<FlujoJuegoCrater>().StopAllCoroutines();
        jugador=Object.FindFirstObjectByType<JugadorFPS>();jugador.enabled=false;
        reales=InputSystem.devices.Where(d=>d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)).ToArray();foreach(var d in reales)InputSystem.DisableDevice(d);
        teclado=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();mando=InputSystem.AddDevice<Gamepad>();Time.timeScale=1;
    }
    [UnityTearDown] public IEnumerator Limpiar()
    {InputSystem.settings.editorInputBehaviorInPlayMode=rutaEntradaEditor;InputSystem.settings.backgroundBehavior=comportamientoFondo;Time.timeScale=1;InputSystem.RemoveDevice(teclado);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(mando);foreach(var d in reales)if(d.added)InputSystem.EnableDevice(d);yield return null;}
    void Situar(Vector3 p){var cc=jugador.GetComponent<CharacterController>();cc.enabled=false;jugador.transform.position=p;jugador.ReiniciarMovimiento();cc.enabled=true;Physics.SyncTransforms();}
    [UnityTest] public IEnumerator CaminarAtrasEnElSpawnNoPermiteCaer()
    {
        var spawn=Buscar("SpawnValle");Situar(spawn.position);float suelo=spawn.position.y;
        var cc=jugador.GetComponent<CharacterController>();Time.timeScale=3;
        for(float t=0;t<6;t+=Time.deltaTime){cc.Move((-spawn.forward*4+Vector3.down*3)*Time.deltaTime);Assert.That(jugador.transform.position.y,Is.GreaterThan(suelo-.6f));yield return null;}
        Assert.That(Vector3.Distance(jugador.transform.position,spawn.position),Is.LessThan(15));
    }
    [UnityTest] public IEnumerator LaBajadaLateralSePuedeRecorrerHastaLasLomas()
    {
        var ext=Buscar("06_Capilla");var cc=jugador.GetComponent<CharacterController>();Situar(Buscar("SpawnValle").position);Time.timeScale=3;
        foreach(var punto in ConstructorCamino())
        {
            var destino=ext.TransformPoint(new Vector3(punto.x,0,punto.y));float hasta=Time.time+8;
            while(Vector2.Distance(new Vector2(jugador.transform.position.x,jugador.transform.position.z),new Vector2(destino.x,destino.z))>.5f&&Time.time<hasta)
            {var d=destino-jugador.transform.position;d.y=0;cc.Move((d.normalized*4+Vector3.down*8)*Time.deltaTime);yield return null;}
            Assert.That(Vector2.Distance(new Vector2(jugador.transform.position.x,jugador.transform.position.z),new Vector2(destino.x,destino.z)),Is.LessThan(.7f),punto.ToString());
            Assert.That(jugador.transform.position.y,Is.GreaterThan(5));
        }
    }
    Vector2[] ConstructorCamino()=>new[]{new Vector2(14,187),new Vector2(0,184),new Vector2(-12,184),new Vector2(-19,177),new Vector2(-19,165),new Vector2(-12,154),new Vector2(0,150)};
    [UnityTest] public IEnumerator FiltroVenceEnTresSegundosRespetaPausaYExigeSoltar()
    {
        var f=Object.FindFirstObjectByType<LinternaController>();f.Recoger();f.Encender(true);foreach(var x in f.filtros)f.Desbloquear(x);
        InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return new WaitForSeconds(.05f);
        Assert.That(f.FiltroActual,Is.EqualTo(f.filtros[0]),$"entrada={EntradaCrater.Filtro(0)}, mouse={mouse.leftButton.isPressed}, actual={Mouse.current?.deviceId}, esperado={mouse.deviceId}, activo={f.isActiveAndEnabled}, encendido={f.Encendida}, pausa={PausaCrater.EnPausa}, cine={CinematicaDeSello.Reproduciendo}, cambio={f.CambiandoFiltro}, dt={Time.deltaTime}");Assert.That(f.TiempoFiltroRestante,Is.InRange(2.8f,3));
        float antes=f.TiempoFiltroRestante;Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);Assert.That(f.TiempoFiltroRestante,Is.EqualTo(antes).Within(.02));
        Time.timeScale=5;yield return new WaitForSeconds(3.3f);Assert.That(f.FiltroActual,Is.Null);yield return new WaitForSeconds(.6f);Assert.That(f.FiltroActual,Is.Null);
        InputSystem.QueueStateEvent(mouse,new MouseState());yield return new WaitForSeconds(.05f);InputSystem.QueueStateEvent(mouse,new MouseState().WithButton(MouseButton.Left));yield return new WaitForSeconds(.05f);Assert.That(f.FiltroActual,Is.EqualTo(f.filtros[0]));
        Assert.That(f.demoraDeCambio,Is.EqualTo(.18f).Within(.001));
    }
    [UnityTest] public IEnumerator EstacionSoloAlFinalYSolEnElTercioDelPaisaje()
    {
        var estacion=Buscar("Estacion_Observacion");Assert.That(estacion.gameObject.activeInHierarchy,Is.False);
        Object.FindFirstObjectByType<PrologoCapilla>().PrepararEpilogo();yield return null;Assert.That(estacion.gameObject.activeInHierarchy,Is.True);
        var spawn=Buscar("SpawnCapilla");Situar(spawn.position);jugador.Orientar(spawn.rotation);
        var cielo=Object.FindFirstObjectByType<CieloEclipse>();var cam=Camera.main;
        var q=cam.WorldToViewportPoint(cam.transform.position+cielo.DireccionSol*100);Assert.That(q.x,Is.InRange(.57f,.78f));Assert.That(q.y,Is.InRange(.55f,.95f));
    }
    [UnityTest] public IEnumerator ElEncuadreApuntaAlFarolSinLineaYElMuralSeLee()
    {
        var m=Object.FindFirstObjectByType<MotivoPorPerspectiva>();var cam=Camera.main;cam.transform.position=m.puntoDeVista;cam.transform.LookAt(m.destino.position);
        yield return new WaitForSeconds(.3f);Assert.That(m.Alineado,Is.True);Assert.That(m.GetComponent<LineRenderer>(),Is.Null);Assert.That(m.luzCenital.intensity,Is.GreaterThan(1.5f));Assert.That(m.luzCenital.intensity,Is.LessThanOrEqualTo(9));
        var visual=m.destino.position-m.puntoDeVista;
        Assert.That(Physics.Raycast(m.puntoDeVista,visual.normalized,visual.magnitude-.15f,1,QueryTriggerInteraction.Ignore),Is.False,"El agujero deja ver el farol sin postes en el medio");
        Vector3 media=Vector3.zero;foreach(var r in m.fragmentos)media+=cam.WorldToViewportPoint(r.bounds.center);media/=m.fragmentos.Length;
        var farol=cam.WorldToViewportPoint(m.destino.position);Assert.That(Vector2.Distance(new Vector2(media.x,media.y),new Vector2(farol.x,farol.y)),Is.LessThan(.025f));
        var mural=Buscar("Historia_09_Farol").Find("PinturaMural");Assert.That(mural.localScale.x,Is.EqualTo(4.8f));Assert.That(mural.localScale.x/mural.localScale.y,Is.EqualTo(2));
        Assert.That(Buscar("Historia_09_Farol").Find("Marco_Narrativa"),Is.Not.Null);
    }
    [UnityTest] public IEnumerator PiedraApoyadaYFarolUsaAyudaContextual()
    {
        Assert.That(GameObject.Find("Cartel_03_RecogerFarol"),Is.Null);Assert.That(GameObject.Find("Cartel_04_Farol"),Is.Null);Assert.That(GameObject.Find("Mirador_Lateral_Piso"),Is.Null);
        var p=Buscar("Pieza_exterior_mirador");var banco=Buscar("Banco_Rotonda").GetComponent<Renderer>().bounds;
        Assert.That(p.position.y,Is.InRange(banco.max.y+.05f,banco.max.y+.2f));Assert.That(p.position.x,Is.InRange(banco.min.x,banco.max.x));Assert.That(p.position.z,Is.InRange(banco.min.z,banco.max.z));
        Situar(new Vector3(p.position.x+1,0.05f,p.position.z-1.2f));jugador.enabled=true;jugador.MirarHacia(p.position);InputSystem.QueueStateEvent(teclado,new KeyboardState(Key.Z));yield return new WaitForSeconds(.2f);
        var ui=Object.FindFirstObjectByType<InterfazCrater>();var texto=ui.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Interaccion_Opcional");Assert.That(texto.text,Does.Contain("Recoger piedra"));Assert.That(Mathf.Abs(texto.color.r-texto.color.g),Is.LessThan(.05f));
    }
    [UnityTest] public IEnumerator SalaFinalRedondaReflejaYLasJuntasDelLobbyEstanCerradas()
    {
        var centro=EspacioCrater.Punto(new Vector3(0,0,87));Situar(centro+Vector3.up*.05f);
        for(int i=0;i<8;i++){float a=i*Mathf.PI/4;if(i==6)continue;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Assert.That(Physics.Raycast(centro+Vector3.up*1.6f,d,out var hit,20,1,QueryTriggerInteraction.Ignore),Is.True);Assert.That(hit.collider.name,Does.StartWith("Muro_Circular_"));Assert.That(hit.distance,Is.InRange(14.5f,15.5f));}
        var f=Object.FindFirstObjectByType<LinternaController>();f.Recoger();f.Encender(true);Camera.main.transform.LookAt(centro+Vector3.right*20+Vector3.up*1.6f);yield return new WaitForSeconds(.3f);Assert.That(Buscar("ParedEspejo").GetComponent<ParedEspejo>().Encandilamiento,Is.GreaterThan(0));
        foreach(float lado in new[]{-1f,1f}){var origen=EspacioCrater.Punto(new Vector3(lado*8.5f,1,24.2f));Assert.That(Physics.Raycast(origen,Vector3.left*lado,out var hit,2,1,QueryTriggerInteraction.Ignore),Is.True);Assert.That(hit.collider.name,Does.StartWith("Muro_Vuelta_"));}
    }
    [UnityTest] public IEnumerator VelasEstablesYColumnasEmpiezanAntesDeEntrarAlCentro()
    {
        Object.FindFirstObjectByType<PrologoCapilla>().SendMessage("EntrarSinTransicion");yield return null;
        var presupuesto=Object.FindFirstObjectByType<PresupuestoSombras>();var velas=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.name=="Luz_Vela_Entrada").ToArray();Assert.That(velas.Length,Is.EqualTo(4));
        for(int i=0;i<5;i++){presupuesto.Actualizar(new Vector3(i*3,9,-35));Assert.That(velas.All(l=>l.shadows==LightShadows.None),Is.True);}
        var o=Object.FindFirstObjectByType<ObeliscoDelEclipse>();Situar(o.centro+Vector3.left*18+Vector3.up*.05f);
        var f=Object.FindFirstObjectByType<LinternaController>();o.sol.sello.RecibirLuz(f.filtros[0],2);o.sol.sello.Avanzar(2);yield return new WaitForSeconds(.2f);
        Assert.That(o.columnasSol.Any(c=>c.Encendido),Is.True);Assert.That(GameObject.Find("Luz_Rotonda"),Is.Null);Assert.That(GameObject.Find("Luz_Murales_Este"),Is.Null);
    }
    [UnityTest] public IEnumerator ElTechoCircularCierraSinHuecosYLaOscuridadConservaElFinal()
    {
        var centro=EspacioCrater.Punto(new Vector3(0,0,87));Situar(centro+Vector3.up*.05f);
        var bounds=Buscar("Techo_Circular").GetComponent<Renderer>().bounds;
        Assert.That(bounds.center.z,Is.EqualTo(centro.z).Within(.01f));
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;var p=centro+new Vector3(Mathf.Cos(a)*6.5f,1.6f,Mathf.Sin(a)*6.5f);
            Assert.That(Physics.Raycast(p,Vector3.up,out var hit,9,1,QueryTriggerInteraction.Ignore),Is.True,"Techo cerrado sin fuga hacia el cielo");
            Assert.That(hit.collider.name=="Techo_Circular"||hit.collider.name.StartsWith("Hoja_"),Is.True,hit.collider.name);
        }
        var f=Object.FindFirstObjectByType<LinternaController>();f.Recoger();f.Encender(false);
        var final=Object.FindFirstObjectByType<EclipseFinalController>();final.HabilitarEnCresta();Time.timeScale=4;
        var techo=Object.FindFirstObjectByType<AperturaTecho>();float hasta=Time.time+40;
        while(!techo.Abriendo&&Time.time<hasta)yield return null;Assert.That(techo.Abriendo,Is.True);
        hasta=Time.time+65;while(!final.EnEpilogo&&Time.time<hasta)yield return null;
        Assert.That(final.EnEpilogo,Is.True);Assert.That(Buscar("Estacion_Observacion").gameObject.activeInHierarchy,Is.True);
        Assert.That(Vector3.Distance(jugador.transform.position,Buscar("SpawnCapilla").position),Is.LessThan(.5f));
    }
}
