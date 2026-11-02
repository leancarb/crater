using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Punto único de lectura de controles (teclado + mouse, con joystick opcional).
/// Si hay que cambiar una tecla, se cambia acá y en ningún otro lado.
///
///  WASD / flechas · Mover        Mouse · Mirar
///  F · Farol                     Clic sostenido · Filtros          Shift · Correr
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

    /// <summary>Correr con Shift o R2 / RT; L3 no acelera.</summary>
    public static bool Correr => (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
        || (Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > .5f);

    /// <summary>Prender o apagar el farol (F o X de Xbox / cuadrado).</summary>
    public static bool Linterna =>
        Presionada(Key.F) || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);

    /// <summary>Filtro sostenido: SOL con izquierdo/R1; LUNA con derecho/L1.</summary>
    public static bool Filtro(int indice)
    {
        var m = Mouse.current; var g = Gamepad.current;
        return indice == 0 ? (m != null && m.leftButton.isPressed) || (g != null && g.rightShoulder.isPressed)
            : indice == 1 && ((m != null && m.rightButton.isPressed) || (g != null && g.leftShoulder.isPressed));
    }

    // Compatibilidad con consumidores anteriores: se vuelve a blanca al soltar.
    public static bool QuitarFiltro => false;
    public static string BotonFiltro(int indice) => UsandoMando
        ? (indice == 0 ? "RB (R1)" : "LB (L1)") : (indice == 0 ? "Clic izquierdo" : "Clic derecho");

    /// <summary>Saltear la animación que se esté viendo (Espacio, Enter o A). No se anuncia con texto.</summary>
    public static bool Saltar =>
        Presionada(Key.Space) || Presionada(Key.Enter)
        || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

    /// <summary>Pausa (Esc o Start).</summary>
    public static bool Pausa =>
        Presionada(Key.Escape) || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

    // Acciones de pausa completas sin teclado. No se leen durante el juego.
    public static bool Seguir => Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
    public static bool Reiniciar => Presionada(Key.R) || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
    public static bool Salir => Presionada(Key.X) || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
    public static bool AlternarAyudas => Presionada(Key.H) || (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame);

    /// <summary>
    /// Navegar un menú: (0, +1) arriba, (0, -1) abajo, (+1, 0) derecha, (-1, 0) izquierda.
    /// WASD, flechas o la cruceta del joystick. Sólo el frame en que se apretó.
    /// </summary>
    public static Vector2Int NavegarMenu()
    {
        var n = Vector2Int.zero;
        if (Presionada(Key.W) || Presionada(Key.UpArrow)) n.y += 1;
        if (Presionada(Key.S) || Presionada(Key.DownArrow)) n.y -= 1;
        if (Presionada(Key.D) || Presionada(Key.RightArrow)) n.x += 1;
        if (Presionada(Key.A) || Presionada(Key.LeftArrow)) n.x -= 1;
        var g = Gamepad.current;
        if (g != null)
        {
            if (g.dpad.up.wasPressedThisFrame) n.y += 1;
            if (g.dpad.down.wasPressedThisFrame) n.y -= 1;
            if (g.dpad.right.wasPressedThisFrame) n.x += 1;
            if (g.dpad.left.wasPressedThisFrame) n.x -= 1;
        }
        return n;
    }

    /// <summary>True sólo en el frame en que se apretó la tecla.</summary>
    public static bool Presionada(Key tecla) =>
        Keyboard.current != null && Keyboard.current[tecla].wasPressedThisFrame;

    /// <summary>Último dispositivo usado; conectar un mando no cambia los carteles por sí solo.</summary>
    public static bool UsandoMando => Gamepad.current != null && Gamepad.current.lastUpdateTime >
        System.Math.Max(Keyboard.current != null ? Keyboard.current.lastUpdateTime : 0,
            Mouse.current != null ? Mouse.current.lastUpdateTime : 0);
    public static bool Interactuar => Saltar;
    public static string BotonInteractuar => UsandoMando ? "A (×)" : "Espacio";
    public static string BotonLinterna => UsandoMando ? "X (□)" : "F";

    public static bool ClicIzquierdo =>
        Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
}
