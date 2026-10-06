using NUnit.Framework;
using System.Collections;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class EntradaJoystickTests
{
    Gamepad mando;
    PausaCrater[] pausas;
    [SetUp] public void Conectar()
    {
        // Suspender sólo el consumidor del menú; conservar la escena del Test Runner.
        pausas = Object.FindObjectsByType<PausaCrater>(FindObjectsSortMode.None);
        foreach (var pausa in pausas) pausa.enabled = false;
        mando = InputSystem.AddDevice<Gamepad>(); mando.MakeCurrent();
    }
    [TearDown] public void Desconectar() { if (mando != null) InputSystem.RemoveDevice(mando); foreach (var pausa in pausas) if (pausa != null) pausa.enabled = true; }

    IEnumerator Estado(GamepadState estado) { InputSystem.QueueStateEvent(mando, estado); yield return null; }

    [UnityTest] public IEnumerator ElMandoMueveYAccionaLasHerramientas()
    {
        yield return Estado(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.RightShoulder));
        Assert.That(EntradaCrater.Movimiento().y, Is.GreaterThan(0.9f));
        Assert.That(EntradaCrater.Linterna, Is.True);
        yield return Estado(new GamepadState().WithButton(GamepadButton.West));
        Assert.That(EntradaCrater.Filtro(0), Is.True);
        yield return Estado(new GamepadState().WithButton(GamepadButton.North));
        Assert.That(EntradaCrater.Filtro(1), Is.True);
        yield return Estado(new GamepadState().WithButton(GamepadButton.East));
        Assert.That(EntradaCrater.QuitarFiltro, Is.True);
        yield return Estado(new GamepadState().WithButton(GamepadButton.LeftStick));
        Assert.That(EntradaCrater.Correr, Is.True);
    }

    [UnityTest] public IEnumerator LaPausaTieneTodasLasAccionesSinTeclado()
    {
        yield return Estado(new GamepadState().WithButton(GamepadButton.Start)); Assert.That(EntradaCrater.Pausa, Is.True);
        yield return Estado(new GamepadState().WithButton(GamepadButton.East)); Assert.That(EntradaCrater.Seguir, Is.True);
        yield return Estado(new GamepadState().WithButton(GamepadButton.North)); Assert.That(EntradaCrater.Reiniciar, Is.True);
        yield return Estado(new GamepadState().WithButton(GamepadButton.West)); Assert.That(EntradaCrater.Salir, Is.True);
        yield return Estado(new GamepadState().WithButton(GamepadButton.DpadUp)); Assert.That(EntradaCrater.AlternarAyudas, Is.True);
        yield return Estado(new GamepadState().WithButton(GamepadButton.South)); Assert.That(EntradaCrater.Saltar, Is.True);
    }
}
