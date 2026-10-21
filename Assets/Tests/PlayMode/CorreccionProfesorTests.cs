using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CorreccionProfesorTests
{
    ColeccionPiedras c;
    LinternaController l;
    [UnitySetUp] public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        var pausa=Object.FindFirstObjectByType<PausaCrater>();pausa.pausarAlPerderFoco=false;pausa.Reanudar();
        var p=Object.FindFirstObjectByType<PrologoCapilla>();
        typeof(PrologoCapilla).GetMethod("EntrarSinTransicion",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(p,null);
        c=Object.FindFirstObjectByType<ColeccionPiedras>();l=LinternaController.Instancia;
    }
    void Mirar(Recogible objeto)
    {
        var cc=c.jugador.GetComponent<CharacterController>();cc.enabled=false;
        c.jugador.transform.position=objeto.transform.position+Vector3.back*1.1f;
        c.jugador.ReiniciarMovimiento();cc.enabled=true;
        c.jugador.MirarHacia(objeto.PuntoInteraccion.position);Physics.SyncTransforms();
    }
    string Texto => Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name=="Interaccion_Opcional").text;
    [UnityTest] public IEnumerator ElFarolSeRecogeConConfirmacionYEnseniaElEncendido()
    {
        var objeto=Object.FindObjectsByType<Recogible>(FindObjectsSortMode.None).Single(r=>r.filtro==null);
        Mirar(objeto);yield return null;yield return null;
        Assert.That(objeto.Recogido,Is.False,"Pasar por el trigger no lo recoge");
        Assert.That(c.ObjetoRecogible,Is.SameAs(objeto));
        var teclado=InputSystem.AddDevice<Keyboard>();teclado.MakeCurrent();
        try
        {
            InputSystem.QueueStateEvent(teclado,new KeyboardState());yield return null;
            Assert.That(Texto,Does.Contain("Espacio"));
            var pausa=Object.FindFirstObjectByType<PausaCrater>();pausa.Pausar();
            InputSystem.QueueStateEvent(teclado,new KeyboardState(Key.Space));yield return null;yield return null;
            Assert.That(objeto.Recogido,Is.False,"La pausa bloquea la recogida");
            InputSystem.QueueStateEvent(teclado,new KeyboardState());yield return null;pausa.Reanudar();yield return null;
            InputSystem.QueueStateEvent(teclado,new KeyboardState(Key.Space));yield return null;yield return null;
            Assert.That(objeto.Recogido,Is.True);Assert.That(l.Disponible,Is.True);Assert.That(l.Encendida,Is.False);
            Assert.That(Texto,Does.Contain("F · Encender"));
            InputSystem.QueueStateEvent(teclado,new KeyboardState());yield return null;
            InputSystem.QueueStateEvent(teclado,new KeyboardState(Key.F));yield return null;yield return null;
            Assert.That(l.Encendida,Is.True);Assert.That(Texto,Does.Not.Contain("Encender farol"));
        }
        finally{InputSystem.RemoveDevice(teclado);}
    }
    [UnityTest] public IEnumerator ElFiltroUsaLaMismaAccionEnMandoYSoloCercaYEnVista()
    {
        l.Recoger();
        var objeto=Object.FindObjectsByType<Recogible>(FindObjectsSortMode.None).First(r=>r.filtro!=null);
        var mando=InputSystem.AddDevice<Gamepad>();mando.MakeCurrent();
        try
        {
            Mirar(objeto);c.jugador.MirarHacia(c.camara.transform.position+(c.camara.transform.position-objeto.PuntoInteraccion.position)*10);
            yield return null;yield return null;
            InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            Assert.That(objeto.Recogido,Is.False,"No se recoge sin mirarlo");
            InputSystem.QueueStateEvent(mando,new GamepadState());yield return null;
            c.jugador.MirarHacia(objeto.PuntoInteraccion.position);yield return null;yield return null;
            Assert.That(c.ObjetoRecogible,Is.SameAs(objeto));Assert.That(Texto,Does.Contain("A (×)"));
            InputSystem.QueueStateEvent(mando,new GamepadState().WithButton(GamepadButton.South));yield return null;yield return null;
            Assert.That(objeto.Recogido,Is.True);Assert.That(l.EstaDesbloqueado(objeto.filtro),Is.True);
        }
        finally{InputSystem.RemoveDevice(mando);}
    }
    [UnityTest] public IEnumerator LaCuentaAtrasEsVisibleYElPuenteAvisaAntesDeCaer()
    {
        var go=new GameObject("Ancla_Prueba_Temporal");go.transform.position=new Vector3(100,40,100);
        var a=go.AddComponent<Ancla>();a.canalRequerido=FiltroDefinicion.Canal.Ninguno;a.retencion=2;
        var puenteGO=new GameObject("Puente_Prueba_Temporal");var puente=puenteGO.AddComponent<PuenteLuz>();puente.anclas.Add(a);
        try
        {
            yield return null;
            a.RecibirLuz(null,1);a.Avanzar(0);yield return null;
            var cuenta=a.GetComponent<CuentaAtrasAncla>();Assert.That(cuenta,Is.Not.Null);
            Assert.That(cuenta.FraccionVisible,Is.GreaterThan(.9f));
            a.Avanzar(1.5f);puente.Avanzar(.5f);yield return null;
            Assert.That(cuenta.FraccionVisible,Is.InRange(.1f,.3f));
            Assert.That(go.GetComponentInChildren<LineRenderer>().enabled,Is.True);
            Assert.That(puente.AvisandoFin,Is.True);Assert.That(puente.Solido,Is.True);
            var aviso=puenteGO.GetComponentsInChildren<AudioSource>().Single(s=>s.name=="Aviso_Tiempo_Puente");
            Assert.That(aviso.isPlaying,Is.True);
            a.Avanzar(3);puente.Avanzar(1);yield return null;
            Assert.That(cuenta.FraccionVisible,Is.Zero);Assert.That(puente.Solido,Is.False);Assert.That(puente.AvisandoFin,Is.False);
        }
        finally{Object.Destroy(go);Object.Destroy(puenteGO);}
    }
}

