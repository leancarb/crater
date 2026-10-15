using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ExploracionCraterTests
{
    [UnityTest]
    public IEnumerator UnaPiezaRespondeAUnaAccionExistenteYNoSeDuplica()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        yield return new WaitForSeconds(6);
        var c=Object.FindFirstObjectByType<ColeccionPiedras>();
        var p=System.Array.Find(c.piezas,x=>x.indice==4);
        c.prologo.enabled=false;c.jugador.movimientoBloqueado=false;
        Object.FindFirstObjectByType<PausaCrater>().pausarAlPerderFoco=false;
        var cc=c.jugador.GetComponent<CharacterController>();
        Vector3 inicio=p.transform.position+new Vector3(2.1f,-.13f,0);
        cc.enabled=false;c.jugador.transform.position=inicio;cc.enabled=true;
        for(float t=0;t<1;t+=Time.deltaTime){cc.Move(Vector3.left*.8f*Time.deltaTime);yield return null;}
        c.jugador.MirarHacia(p.transform.position);
        yield return new WaitForSeconds(.2f);
        Assert.That(c.Objetivo,Is.EqualTo(p),"la pieza no se puede alcanzar y mirar dentro de la cavidad");
        Capturar(c.camara,"01_Prototipo_Pieza");
        var k=InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(k,new KeyboardState(Key.Space));yield return null;yield return null;
            InputSystem.QueueStateEvent(k,new KeyboardState());yield return null;
            Capturar(c.camara,"02_Prototipo_Recogida");
            Assert.That(c.Cantidad,Is.EqualTo(1));Assert.That(p.gameObject.activeSelf,Is.False);
            Assert.That(c.conjunto.fragmentos[4].enabled,Is.True);Assert.That(p.Recoger(),Is.False);
            Assert.That(c.Cantidad,Is.EqualTo(1));Assert.That(c.conjunto.Transformado,Is.False);
        }
        finally{InputSystem.RemoveDevice(k);}
        cc.enabled=false;c.jugador.transform.position=new Vector3(c.conjunto.transform.position.x,.05f,c.conjunto.transform.position.z-2.4f);cc.enabled=true;
        c.jugador.MirarHacia(c.conjunto.transform.position);yield return null;
        Capturar(c.camara,"03_Prototipo_Representacion");
        var otra=c.piezas.First(x=>x.indice==5);
        Situar(c,new Vector3(otra.transform.position.x+.9f,.05f,otra.transform.position.z-.8f),otra.transform.position);
        yield return new WaitForSeconds(.2f);Assert.That(c.Objetivo,Is.EqualTo(otra));
        var mando=InputSystem.AddDevice<Gamepad>();
        try
        {
            InputSystem.QueueStateEvent(mando,new GamepadState());yield return null;yield return null;
            Capturar(c.camara,"14_Pieza_Contexto_Mando");
            InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            InputSystem.QueueStateEvent(mando,new GamepadState());yield return null;
            Assert.That(c.Cantidad,Is.EqualTo(2));Assert.That(otra.gameObject.activeSelf,Is.False);Assert.That(otra.Recoger(),Is.False);
        }
        finally{InputSystem.RemoveDevice(mando);}

        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        yield return new WaitForSeconds(6);
        Assert.That(Object.FindFirstObjectByType<ColeccionPiedras>().Cantidad,Is.Zero,"reiniciar debe iniciar otra colección");
    }

    static void Situar(ColeccionPiedras c,Vector3 p,Vector3 mirar)
    {
        var cc=c.jugador.GetComponent<CharacterController>();cc.enabled=false;c.jugador.transform.position=p;
        c.jugador.ReiniciarMovimiento();cc.enabled=true;c.jugador.MirarHacia(mirar);Physics.SyncTransforms();
    }
    IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        yield return new WaitForSeconds(6);
        var c=Object.FindFirstObjectByType<ColeccionPiedras>();c.prologo.enabled=false;c.jugador.movimientoBloqueado=false;
        Object.FindFirstObjectByType<PausaCrater>().pausarAlPerderFoco=false;
    }
    [UnityTest]
    public IEnumerator ConjuntoEsperaLaVueltaRespetaPausaYAdmiteReescuchaConMando()
    {
        yield return Preparar();var c=Object.FindFirstObjectByType<ColeccionPiedras>();c.prologo.SendMessage("EntrarSinTransicion");var ct=c.conjunto;
        Situar(c,EspacioCrater.Punto(new Vector3(0,0,24)),Vector3.forward*100);
        foreach(var p in c.piezas){Assert.That(p.Recoger(),Is.True);Assert.That(p.Recoger(),Is.False);}
        yield return new WaitForSeconds(.5f);
        Assert.That(c.Cantidad,Is.EqualTo(6));Assert.That(ct.Reproduciendo,Is.False);Assert.That(ct.Transformado,Is.False);
        Situar(c,new Vector3(ct.transform.position.x,.05f,ct.transform.position.z-2.5f),ct.transform.position);
        yield return new WaitForSeconds(1.5f);Assert.That(ct.Reproduciendo,Is.True);
        Assert.That(c.jugador.enabled,Is.True);Assert.That(c.jugador.movimientoBloqueado,Is.False);
        Capturar(c.camara,"04_Conjunto_Durante");
        Assert.That(ct.voces.Any(v=>v.isPlaying),Is.True,"no se reprodujeron las resonancias");
        c.jugador.movimientoBloqueado=true;float detenido=ct.Avance;
        yield return new WaitForSeconds(.4f);Assert.That(ct.Avance,Is.EqualTo(detenido));
        c.jugador.movimientoBloqueado=false;
        var pausa=Object.FindFirstObjectByType<PausaCrater>();pausa.Pausar();float avance=ct.Avance;
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(ct.Avance,Is.EqualTo(avance));Assert.That(AudioListener.pause,Is.True);
        Assert.That(Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("6/6")),Is.True);
        Capturar(c.camara,"05_Conjunto_Pausa");pausa.Reanudar();
        yield return new WaitForSeconds(4);Assert.That(ct.Transformado,Is.True);Assert.That(ct.proyeccion.enabled,Is.True);
        Assert.That(ct.fragmentos.All(r=>r.enabled),Is.True);Capturar(c.camara,"06_Conjunto_Completo");
        var g=InputSystem.AddDevice<Gamepad>();
        try
        {
            InputSystem.QueueStateEvent(g,new GamepadState());yield return null;yield return null;
            Assert.That(Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("A (×) · Escuchar")),Is.True);
            InputSystem.QueueStateEvent(g,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            InputSystem.QueueStateEvent(g,new GamepadState());yield return null;
            Assert.That(ct.VecesEscuchado,Is.EqualTo(2));Assert.That(ct.Reproduciendo,Is.True);
            Assert.That(ct.luz.intensity,Is.GreaterThan(3.9f),"la reescucha no debe apagar el lugar");
        }
        finally{InputSystem.RemoveDevice(g);}
        yield return new WaitForSeconds(5.2f);Assert.That(ct.Reproduciendo,Is.False);
    }
    [UnityTest]
    public IEnumerator LaLuzBlancaMantienePotenciaYLasVelasConservanSombras()
    {
        yield return Preparar();var c=Object.FindFirstObjectByType<ColeccionPiedras>();
        c.prologo.SendMessage("EntrarSinTransicion");var l=c.jugador.GetComponentInChildren<LinternaController>();
        l.Recoger();l.EquiparFiltro(null);l.Encender(true);
        Situar(c,EspacioCrater.Punto(new Vector3(1.5f,.05f,-8)),EspacioCrater.Punto(new Vector3(2,1.62f,-8)));
        yield return new WaitForSeconds(.5f);Assert.That(l.spot.intensity,Is.EqualTo(l.intensidadBase).Within(.1f));
        var velas=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(x=>x.name=="Luz_Vela_Entrada").ToArray();
        Assert.That(velas.Length,Is.GreaterThan(0));Assert.That(velas.All(x=>x.shadows!=LightShadows.None),Is.True);
        var boca=GameObject.Find("Boca_Crater").transform.position;
        Situar(c,boca+new Vector3(0,.05f,-9),boca);yield return new WaitForSeconds(.4f);
        Assert.That(velas.All(x=>x.shadows!=LightShadows.None),Is.True);Capturar(c.camara,"07_Velas_Lejos");
        Situar(c,boca+new Vector3(2,.05f,-2),boca+Vector3.right*2);yield return new WaitForSeconds(.4f);
        Assert.That(velas.All(x=>x.shadows!=LightShadows.None),Is.True);Capturar(c.camara,"08_Velas_Cerca");
    }
    [UnityTest]
    public IEnumerator ReflejoSeCapturaSoloAlAcercarseYElRelieveRespondeAlAngulo()
    {
        yield return Preparar();var c=Object.FindFirstObjectByType<ColeccionPiedras>();var a=Object.FindFirstObjectByType<AguaEnCuenco>();
        Situar(c,EspacioCrater.Punto(new Vector3(0,.05f,-8)),Vector3.forward*100);yield return new WaitForSeconds(.2f);
        Assert.That(a.Capturas,Is.Zero);
        Situar(c,a.transform.position+new Vector3(0,-a.transform.position.y+.05f,-1.2f),a.transform.position);
        for(int i=0;i<120&&!a.Lista;i++)yield return null;
        Assert.That(a.Lista,Is.True);Assert.That(a.Capturas,Is.EqualTo(1));Capturar(c.camara,"09_Cuenco_Reflejo");
        yield return new WaitForSeconds(.4f);Assert.That(a.Capturas,Is.EqualTo(1));
        a.MarcarCambio();yield return null;Assert.That(a.Capturas,Is.EqualTo(2));
        var v=Object.FindFirstObjectByType<VetaAngular>();var l=v.linterna;l.Recoger();l.Encender(true);
        Situar(c,new Vector3(v.transform.position.x+2,.05f,v.transform.position.z),v.transform.position);
        yield return new WaitForSeconds(1);float frente=v.Respuesta;Assert.That(frente,Is.GreaterThan(.05f));Capturar(c.camara,"10_Veta_Frontal");
        Situar(c,new Vector3(v.transform.position.x+1.8f,.05f,v.transform.position.z-3.5f),v.transform.position);
        yield return new WaitForSeconds(1);Assert.That(v.Respuesta,Is.GreaterThan(frente+.1f));Capturar(c.camara,"11_Veta_Oblicua");
        var m=Object.FindFirstObjectByType<MotivoPorPerspectiva>();Situar(c,new Vector3(m.puntoDeVista.x,.05f,m.puntoDeVista.z),m.transform.position);
        yield return new WaitForSeconds(.3f);Capturar(c.camara,"12_Motivo_Alineado");
        Situar(c,new Vector3(m.puntoDeVista.x+2.6f,.05f,m.puntoDeVista.z),m.transform.position);
        yield return new WaitForSeconds(.3f);Capturar(c.camara,"13_Motivo_Desplazado");
        l.Encender(false);
        foreach(var mural in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Control_LuzBlanca"))
        {
            var posicion=mural.position-mural.forward*3;posicion.y=.05f;
            Situar(c,posicion,mural.position);yield return new WaitForSeconds(.3f);
            Capturar(c.camara,mural.position.x<0?"15_Q_SOL":"16_Q_LUNA");
        }
        l.Encender(false);
        Situar(c,EspacioCrater.Punto(new Vector3(29,.05f,4)+new Vector3(3,0,11)),EspacioCrater.Punto(new Vector3(29,1.62f,7.8f)+new Vector3(3,0,11)));
        yield return new WaitForSeconds(.5f);Capturar(c.camara,"17_LUNA_Giro_Sin_Linterna");
        var decoracion=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Mural_Cruce_Chakana");
        var vista=decoracion.position-decoracion.forward*3;vista.y=.05f;
        Situar(c,vista,decoracion.position);yield return new WaitForSeconds(.3f);Capturar(c.camara,"18_Cruce_Mural_Despejado");
    }
    [UnityTest]
    public IEnumerator CosteDeEfectosEnUnaVistaRealDelJugador()
    {
        yield return Preparar();var c=Object.FindFirstObjectByType<ColeccionPiedras>();var a=Object.FindFirstObjectByType<AguaEnCuenco>();
        Situar(c,a.transform.position+new Vector3(0,.05f-a.transform.position.y,-2),c.conjunto.transform.position);
        for(int i=0;i<120&&!a.Lista;i++)yield return null;
        long memoriaInicial=a.sonda.realtimeTexture!=null?UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(a.sonda.realtimeTexture):0;
        var raiz=c.gameObject;string informe="Editor, vista fija, 120 fotogramas por estado. No es una medición de build.\n";
        foreach(bool activo in new[]{true,false})
        {
            raiz.SetActive(activo);for(int i=0;i<30;i++)yield return null;
            using(var tiempo=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal,"Main Thread"))
            using(var draw=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render,"Draw Calls Count"))
            using(var gc=Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory,"GC Allocated In Frame"))
            {
                double ms=0,dc=0,bytes=0;for(int i=0;i<120;i++){yield return null;ms+=tiempo.LastValue/1000000.0;dc+=draw.LastValue;bytes+=gc.LastValue;}
                informe+="Efectos="+activo+"; MainThread válido="+tiempo.Valid+" media_ms="+(ms/120)+"; Draw válido="+draw.Valid+" media="+(dc/120)+"; GC válido="+gc.Valid+" media_bytes="+(bytes/120)+"\n";
            }
        }
        raiz.SetActive(true);a.MarcarCambio();yield return null;for(int i=0;i<120&&!a.Lista;i++)yield return null;
        informe+="Memoria antes de desactivar la sonda="+memoriaInicial+" bytes\n";informe+="Sonda: "+a.sonda.resolution+" px; capturas="+a.Capturas+"; memoria textura="+(a.sonda.realtimeTexture!=null?UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(a.sonda.realtimeTexture):0)+" bytes\n";
        var polvo=Object.FindFirstObjectByType<PolvoEnLuz>();Assert.That(polvo.polvo.main.maxParticles,Is.LessThanOrEqualTo(20));
        System.IO.File.WriteAllText("Logs/exploracion_coste.txt",informe);
    }

    public static void Capturar(Camera cam,string nombre)
    {
        System.IO.Directory.CreateDirectory("Docs/Previews/Exploracion");
        var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
        var anterior=RenderTexture.active;var destino=cam.targetTexture;
        var ui=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).Select(c=>(c,c.renderMode,c.worldCamera,c.planeDistance)).ToArray();
        try{cam.targetTexture=rt;foreach(var e in ui){e.c.renderMode=RenderMode.ScreenSpaceCamera;e.c.worldCamera=cam;e.c.planeDistance=.2f;}Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes("Docs/Previews/Exploracion/"+nombre+".png",tex.EncodeToPNG());}
        finally{foreach(var e in ui){e.c.renderMode=e.renderMode;e.c.worldCamera=e.worldCamera;e.c.planeDistance=e.planeDistance;}cam.targetTexture=destino;RenderTexture.active=anterior;Object.Destroy(rt);Object.Destroy(tex);}
    }
}
