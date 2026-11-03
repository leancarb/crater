using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CartelesYCieloTests
{
    JugadorFPS jugador;
    Keyboard teclado;Gamepad mando;InputDevice[] reales;
    T Campo<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    [UnitySetUp] public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        var pausa=Object.FindFirstObjectByType<PausaCrater>();pausa.Reanudar();pausa.enabled=false;
        jugador=Object.FindFirstObjectByType<JugadorFPS>();jugador.enabled=false;
        reales=InputSystem.devices.Where(d=>d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)).ToArray();
        foreach(var d in reales)InputSystem.DisableDevice(d);
        teclado=InputSystem.AddDevice<Keyboard>();mando=InputSystem.AddDevice<Gamepad>();
    }
    [UnityTearDown] public IEnumerator Limpiar()
    {
        InputSystem.RemoveDevice(teclado);InputSystem.RemoveDevice(mando);
        foreach(var d in reales)if(d.added)InputSystem.EnableDevice(d);
        Time.timeScale=1;yield return null;
    }
    [UnityTest] public IEnumerator EntradaDuranteAperturaConservaEclipseFarolYAudioInterior()
    {
        var p=Object.FindFirstObjectByType<PrologoCapilla>();p.IniciarCinematica();p.EntrarAlCrater();
        Assert.That(jugador.movimientoBloqueado,Is.False);
        var cc=jugador.GetComponent<CharacterController>();cc.enabled=false;jugador.transform.position=new Vector3(0,.1f,6);cc.enabled=true;
        var farol=Object.FindFirstObjectByType<LinternaController>();farol.Recoger();farol.Encender(true);
        yield return new WaitForSeconds(5.5f);
        Assert.That(p.EnElCrater,Is.True,"No perder el trigger aunque se cruce durante la apertura");
        var cielo=Object.FindFirstObjectByType<CieloEclipse>();
        Assert.That(Mathf.Asin(cielo.DireccionSol.y)*Mathf.Rad2Deg,Is.EqualTo(75).Within(.1));
        Assert.That(Campo<float>(cielo,"diametroAngular"),Is.EqualTo(7));
        var visual=Object.FindFirstObjectByType<LinternaVisual>();
        Assert.That(visual.modelo.GetComponentsInChildren<Renderer>().All(r=>r.enabled),Is.True);
        Assert.That(Object.FindFirstObjectByType<VisibilidadExterior>().paisaje.Any(r=>r!=null&&r.GetComponentInParent<JugadorFPS>()!=null),Is.False);
        var e=Object.FindFirstObjectByType<EclipseFinalController>();
        Assert.That(Campo<AudioSource>(e,"ambienteExterior").isPlaying,Is.False);
        var sonido=Campo<AudioSource>(e,"ambienteCrater");Assert.That(sonido.clip.name,Is.EqualTo("viento_oculo"));
        Assert.That(sonido.isPlaying,Is.True);Assert.That(sonido.volume,Is.EqualTo(.1f).Within(.001));
    }
    [UnityTest] public IEnumerator GlifosCambianDeDispositivoYQuedanDentroDelRecuadro()
    {
        var carteles=Object.FindObjectsByType<CartelTutorial>(FindObjectsSortMode.None);Assert.That(carteles.Length,Is.EqualTo(12));
        InputSystem.QueueStateEvent(teclado,new KeyboardState(Key.Z));yield return null;yield return null;yield return null;
        Assert.That(carteles.First(c=>c.accion==3).GetComponent<TextMesh>().text,Is.EqualTo("E"));
        Assert.That(carteles.First(c=>c.accion==2).iconoXbox.activeSelf,Is.False);
        InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.RightShoulder));yield return null;yield return null;yield return null;
        Assert.That(carteles.First(c=>c.accion==3).GetComponent<TextMesh>().text,Is.EqualTo("B"));
        Assert.That(carteles.First(c=>c.accion==4).GetComponent<TextMesh>().text,Is.EqualTo("X"));
        Assert.That(carteles.First(c=>c.accion==2).iconoXbox.activeSelf,Is.True);
        Assert.That(carteles.First(c=>c.accion==3).GetComponent<TextMesh>().color.r,Is.GreaterThan(.8f));
        foreach(var cartel in carteles)
        {
            var b=cartel.GetComponent<Renderer>().localBounds.size*cartel.transform.localScale.x;
            Assert.That(b.x,Is.LessThanOrEqualTo(cartel.anchoCelda+.005f));Assert.That(b.y,Is.LessThanOrEqualTo(cartel.altoCelda+.005f));
        }
    }
    [UnityTest] public IEnumerator FarolBlancoNoQuemaLaPlacaDeCercaNiReduceElAlcance()
    {
        var cartel=GameObject.Find("Cartel_05_SOL").transform;var cam=Camera.main;
        cam.transform.position=cartel.position-cartel.forward*3;cam.transform.LookAt(cartel.position);Physics.SyncTransforms();
        var farol=Object.FindFirstObjectByType<LinternaController>();farol.Recoger();farol.Encender(true);
        float alcance=farol.AlcanceActual;yield return new WaitForSeconds(.4f);
        Assert.That(farol.spot.intensity,Is.LessThan(60),"Conservar el contraste del dibujo al apuntar a la placa desde tres metros");
        Assert.That(farol.AlcanceActual,Is.EqualTo(alcance));
    }
    int PixelesClaros(Camera cam)
    {
        var rt=new RenderTexture(64,64,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
        var t=new Texture2D(64,64,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,64,64),0,0);t.Apply();
        int n=t.GetPixels().Count(c=>c.maxColorComponent>.2f);cam.targetTexture=null;RenderTexture.active=null;rt.Release();Object.Destroy(rt);Object.Destroy(t);return n;
    }
    IEnumerator ProbarProfundidad(bool texto)
    {
        var goCam=new GameObject("Camara_Profundidad");var cam=goCam.AddComponent<Camera>();cam.enabled=false;cam.orthographic=true;cam.orthographicSize=2;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;cam.cullingMask=1<<30;cam.farClipPlane=100;
        cam.transform.position=new Vector3(2000,0,-4);
        var figura=texto?new GameObject("Glifo_Profundidad"):GameObject.CreatePrimitive(PrimitiveType.Quad);
        figura.layer=30;figura.transform.position=new Vector3(2000,0,0);
        Material m=null;
        if(texto)
        {
            var t=figura.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=64;t.characterSize=.4f;t.anchor=TextAnchor.MiddleCenter;t.text="E";t.color=Color.white;
            figura.GetComponent<Renderer>().sharedMaterial=Object.FindFirstObjectByType<CartelTutorial>().GetComponent<Renderer>().sharedMaterial;
        }
        else{m=new Material(Shader.Find("Crater/CieloSinNiebla"));m.SetColor("_BaseColor",Color.white);m.renderQueue=1002;figura.GetComponent<Renderer>().sharedMaterial=m;}
        var pared=GameObject.CreatePrimitive(PrimitiveType.Cube);pared.layer=30;pared.transform.position=new Vector3(2000,0,texto?-.5f:2);
        pared.transform.localScale=new Vector3(3,3,.1f);var negro=new Material(Shader.Find("Universal Render Pipeline/Unlit"));negro.SetColor("_BaseColor",Color.black);pared.GetComponent<Renderer>().sharedMaterial=negro;pared.SetActive(false);
        try
        {
            yield return null;yield return null;
            Assert.That(PixelesClaros(cam),Is.GreaterThan(5),"La figura debe ser visible sin pared");
            pared.SetActive(true);yield return null;
            Assert.That(PixelesClaros(cam),Is.LessThan(3),texto?"El texto no puede verse a través de una pared":"La montaña debe tapar al astro aunque esté detrás de la esfera que lo representa");
        }
        finally{Object.Destroy(goCam);Object.Destroy(figura);Object.Destroy(pared);Object.Destroy(negro);if(m!=null)Object.Destroy(m);}
    }
    [UnityTest] public IEnumerator TextoNoAtraviesaParedes(){yield return ProbarProfundidad(true);}
    [UnityTest] public IEnumerator AstroQuedaDetrasDeLaMontana(){yield return ProbarProfundidad(false);}
}
