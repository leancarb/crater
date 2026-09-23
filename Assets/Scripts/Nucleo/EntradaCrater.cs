using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Punto único de lectura de controles (teclado + mouse, con joystick opcional).
/// Si hay que cambiar una tecla, se cambia acá y en ningún otro lado.
///
///  WASD / flechas · Mover        Mouse · Mirar
///  F · Linterna                  1 / 2 · Filtros       Q · Quitar filtro
///  Esc · Pausa
/// </summary>
public static class EntradaCrater
{
    /// <summary>Equivalencia con el eje "Mouse X" del Input Manager viejo.</summary>
    const float EscalaMouse = 0.1f;
    const float EscalaJoystick = 32f;

    public static Vector2 Movimiento()
    {
        Vector2 v = Vector2.zero;
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
        }
        if (Gamepad.current != null) v += Gamepad.current.leftStick.ReadValue();
        return Vector2.ClampMagnitude(v, 1f);
    }

    /// <summary>Giro de cámara de este frame, antes de aplicar la sensibilidad.</summary>
    public static Vector2 Mirada()
    {
        Vector2 v = Vector2.zero;
        if (Mouse.current != null) v += Mouse.current.delta.ReadValue() * EscalaMouse;
        if (Gamepad.current != null) v += Gamepad.current.rightStick.ReadValue() * (EscalaJoystick * Time.unscaledDeltaTime);
        return v;
    }

    public static bool Linterna =>
        Presionada(Key.F) || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);

    public static bool Filtro(int indice)
    {
        if (indice == 0 && (Presionada(Key.Digit1) || Presionada(Key.Numpad1))) return true;
        if (indice == 1 && (Presionada(Key.Digit2) || Presionada(Key.Numpad2))) return true;
        var g = Gamepad.current;
        if (g == null) return false;
        return indice == 0 ? g.buttonWest.wasPressedThisFrame : indice == 1 && g.buttonNorth.wasPressedThisFrame;
    }

    public static bool QuitarFiltro =>
        Presionada(Key.Q) || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

    public static bool Pausa =>
        Presionada(Key.Escape) || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

    public static bool Presionada(Key tecla) =>
        Keyboard.current != null && Keyboard.current[tecla].wasPressedThisFrame;

    public static bool ClicIzquierdo =>
        Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
}
