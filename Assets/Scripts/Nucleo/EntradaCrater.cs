using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Punto único de lectura de controles (teclado + mouse, con joystick opcional).
/// Si hay que cambiar una tecla, se cambia acá y en ningún otro lado.
///
///  WASD / flechas · Mover        Mouse · Mirar
///  F · Linterna                  1 / 2 · Filtros       Q · Quitar filtro
///  Esc · Pausa
///
/// CÓMO FUNCIONA
/// Usa el Input System nuevo de Unity leyendo los dispositivos directamente
/// (Keyboard.current, Mouse.current, Gamepad.current). Cada uno puede ser null si
/// no está conectado, por eso siempre se pregunta antes de usarlo.
///  - isPressed: la tecla está apretada ahora (sirve para caminar).
///  - wasPressedThisFrame: se apretó en este frame (sirve para acciones de una vez).
/// Es una clase estática: no va en ningún objeto, se llama como EntradaCrater.Linterna.
/// </summary>
public static class EntradaCrater
{
    /// <summary>Equivalencia con el eje "Mouse X" del Input Manager viejo.</summary>
    const float EscalaMouse = 0.1f;
    // el stick da un valor de -1 a 1 (no un desplazamiento): se multiplica por el tiempo
    const float EscalaJoystick = 32f;

    /// <summary>Dirección de caminata: x = costado, y = adelante. Largo máximo 1.</summary>
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
        // en diagonal (W + D) el vector mediría 1,41: se limita para no caminar más rápido
        return Vector2.ClampMagnitude(v, 1f);
    }

    /// <summary>Giro de cámara de este frame, antes de aplicar la sensibilidad.</summary>
    public static Vector2 Mirada()
    {
        Vector2 v = Vector2.zero;
        // el mouse ya da cuánto se movió desde el frame anterior (delta)
        if (Mouse.current != null) v += Mouse.current.delta.ReadValue() * EscalaMouse;
        // unscaledDeltaTime: el joystick sigue girando la cámara aunque Time.timeScale cambie
        if (Gamepad.current != null) v += Gamepad.current.rightStick.ReadValue() * (EscalaJoystick * Time.unscaledDeltaTime);
        return v;
    }

    /// <summary>Prender o apagar la linterna (F o RB).</summary>
    public static bool Linterna =>
        Presionada(Key.F) || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);

    /// <summary>Equipar el filtro 'indice' (0 = CUERPO con 1 o X, 1 = HUECO con 2 o Y).</summary>
    public static bool Filtro(int indice)
    {
        if (indice == 0 && (Presionada(Key.Digit1) || Presionada(Key.Numpad1))) return true;
        if (indice == 1 && (Presionada(Key.Digit2) || Presionada(Key.Numpad2))) return true;
        var g = Gamepad.current;
        if (g == null) return false;
        return indice == 0 ? g.buttonWest.wasPressedThisFrame : indice == 1 && g.buttonNorth.wasPressedThisFrame;
    }

    /// <summary>Volver a la luz blanca (Q o B).</summary>
    public static bool QuitarFiltro =>
        Presionada(Key.Q) || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

    /// <summary>Pausa (Esc o Start).</summary>
    public static bool Pausa =>
        Presionada(Key.Escape) || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

    /// <summary>True sólo en el frame en que se apretó la tecla.</summary>
    public static bool Presionada(Key tecla) =>
        Keyboard.current != null && Keyboard.current[tecla].wasPressedThisFrame;

    public static bool ClicIzquierdo =>
        Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
}
