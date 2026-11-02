using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class IteracionNoviembreTests
{
    Keyboard teclado;
    JugadorFPS jugador;
    GameObject piso,obstaculo;
    [UnitySetUp] public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        var pausa=Object.FindFirstObjectByType<PausaCrater>();pausa.Reanudar();pausa.enabled=false;
        var prologo=Object.FindFirstObjectByType<PrologoCapilla>();prologo.StopAllCoroutines();prologo.enabled=false;
        Object.FindFirstObjectByType<FlujoJuegoCrater>().StopAllCoroutines();
        jugador=Object.FindFirstObjectByType<JugadorFPS>();jugador.enabled=true;jugador.movimientoBloqueado=false;teclado=InputSystem.AddDevice<Keyboard>();teclado.MakeCurrent();
    }
    [UnityTearDown] public IEnumerator Limpiar()
    {
        if(teclado!=null)InputSystem.RemoveDevice(teclado);
        if(piso!=null)Object.Destroy(piso);if(obstaculo!=null)Object.Destroy(obstaculo);
        Time.timeScale=1;yield return null;
    }
    IEnumerator Suelo(float largo=12)
    {
        piso=GameObject.CreatePrimitive(PrimitiveType.Cube);piso.transform.position=new Vector3(2000,-.25f,largo==12?0:-3);
        piso.transform.localScale=new Vector3(12,.5f,largo);
        var cc=jugador.GetComponent<CharacterController>();cc.enabled=false;
        jugador.transform.SetPositionAndRotation(new Vector3(2000,.05f,largo==12?0:-.55f),Quaternion.identity);
        jugador.ReiniciarMovimiento();cc.enabled=true;Physics.SyncTransforms();yield return new WaitForSeconds(.3f);
    }
    [UnityTest] public IEnumerator RecogerFarolCierraEntradaYEnciendeLobby()
    {
        var llegada=Object.FindFirstObjectByType<LlegadaRotonda>();Assert.That(llegada,Is.Not.Null);
        Assert.That(llegada.entrada.Abierta,Is.True);
        Assert.That(llegada.luces.Length,Is.GreaterThan(0));
        foreach(var luz in llegada.luces)Assert.That(luz.intensity,Is.Zero,luz.name);
        llegada.farol.Recoger();yield return new WaitForSeconds(3);
        Assert.That(llegada.entrada.Abierta,Is.False);Assert.That(llegada.entrada.Progreso,Is.Zero);
        var piedra=GameObject.Find("Pieza_exterior_mirador");
        Assert.That(piedra,Is.Not.Null);
        Assert.That(piedra.transform.position.z,Is.GreaterThan(llegada.entrada.transform.position.z+1),"La piedra queda del lado interior tras el cierre");
        Assert.That(llegada.Progreso,Is.EqualTo(1));
        foreach(var luz in llegada.luces)Assert.That(luz.intensity,Is.GreaterThan(0),luz.name);
    }
    [UnityTest] public IEnumerator SaltoEsBajoYNoInteractua()
    {
        yield return Suelo();float baseY=jugador.transform.position.y,max=baseY;
        InputSystem.QueueStateEvent(teclado,new KeyboardState(Key.Space));yield return null;
        Assert.That(teclado.spaceKey.isPressed,Is.True,"tecla simulada");
        Assert.That(jugador.enabled,Is.True,"jugador habilitado");
        Assert.That(EntradaCrater.Interactuar,Is.False);
        for(int i=0;i<50;i++){max=Mathf.Max(max,jugador.transform.position.y);yield return null;}
        Assert.That(max-baseY,Is.InRange(.2f,.45f));
    }
    [UnityTest] public IEnumerator SaltoNoSuperaPretil()
    {
        yield return Suelo();obstaculo=GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstaculo.transform.position=new Vector3(2000,.5f,1);obstaculo.transform.localScale=new Vector3(6,1,.4f);Physics.SyncTransforms();
        InputSystem.QueueStateEvent(teclado,new KeyboardState(Key.W,Key.LeftShift,Key.Space));
        yield return new WaitForSeconds(1);
        Assert.That(jugador.transform.position.z,Is.LessThan(.6f));
    }
    [UnityTest] public IEnumerator SaltoNoCruzaHuecoDePuzzle()
    {
        yield return Suelo(6);obstaculo=GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstaculo.transform.position=new Vector3(2000,-.25f,5.5f);obstaculo.transform.localScale=new Vector3(12,.5f,6);Physics.SyncTransforms();
        InputSystem.QueueStateEvent(teclado,new KeyboardState(Key.W,Key.LeftShift,Key.Space));
        yield return new WaitForSeconds(1.1f);
        Assert.That(jugador.transform.position.y,Is.LessThan(-.5f));
        Assert.That(jugador.transform.position.z,Is.LessThan(2.2f));
    }
    [UnityTest] public IEnumerator ExteriorSeOcultaAbajoYSeRestauraSinDesactivarColisiones()
    {
        jugador.enabled=false;
        var vis=Object.FindFirstObjectByType<VisibilidadExterior>();
        Assert.That(vis.paisaje.Length,Is.GreaterThan(400));
        var originales=new bool[vis.paisaje.Length];
        // Awake conserva estos valores, aunque el prólogo haya colocado la cámara abajo.
        var guardados=(bool[])typeof(VisibilidadExterior).GetField("habilitados",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(vis);
        System.Array.Copy(guardados,originales,guardados.Length);
        var colisiones=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length;
        jugador.camara.position=new Vector3(0,1.7f,20);yield return null;yield return null;
        foreach(var r in vis.paisaje)if(r!=null)Assert.That(r.enabled,Is.False,r.name);
        Assert.That(Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length,Is.EqualTo(colisiones));
        jugador.camara.position=new Vector3(0,12,20);yield return null;yield return null;
        for(int i=0;i<vis.paisaje.Length;i++)if(vis.paisaje[i]!=null)Assert.That(vis.paisaje[i].enabled,Is.EqualTo(originales[i]),vis.paisaje[i].name);
    }
}
