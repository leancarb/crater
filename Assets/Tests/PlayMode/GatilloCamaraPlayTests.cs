using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
public class GatilloCamaraPlayTests
{
    [UnityTest] public IEnumerator GatilloAnalogicoYStickRespondenSinActivarCarreraConL3()
    {
        var g = InputSystem.AddDevice<Gamepad>(); var m = InputSystem.AddDevice<Mouse>();
        try
        {
            InputSystem.QueueStateEvent(g,new GamepadState { rightTrigger=.34f }); yield return null;
            Assert.That(EntradaCrater.Correr,Is.False);
            InputSystem.QueueStateEvent(g,new GamepadState { rightTrigger=.36f }); yield return null;
            Assert.That(EntradaCrater.Correr,Is.True);
            InputSystem.QueueStateEvent(g,new GamepadState { rightTrigger=.5f }); yield return null;
            Assert.That(EntradaCrater.Correr,Is.True);
            InputSystem.QueueStateEvent(g,new GamepadState().WithButton(GamepadButton.LeftStick)); yield return null;
            Assert.That(EntradaCrater.Correr,Is.False);
            InputSystem.QueueStateEvent(g,new GamepadState { rightStick=new Vector2(.7f,0) });
            InputSystem.QueueStateEvent(m,new MouseState()); yield return null;
            Assert.That(EntradaCrater.Mirada().x/Time.unscaledDeltaTime,
                Is.EqualTo(EntradaCrater.CurvaMiradaMando(g.rightStick.ReadValue()).x*72f).Within(.01f));
        }
        finally { InputSystem.RemoveDevice(g); InputSystem.RemoveDevice(m); }
    }
}
