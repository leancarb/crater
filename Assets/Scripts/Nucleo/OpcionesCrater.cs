using UnityEngine;

/// <summary>
/// Las opciones del jugador: sensibilidad del mouse, invertir el eje Y, campo de
/// visión, brillo y volumen. Se guardan en PlayerPrefs, así que quedan para la
/// próxima partida. Se cambian desde la pausa (ver PausaCrater).
///
/// CÓMO FUNCIONA
/// Es una clase estática: cualquier script lee los valores sin necesitar una
/// referencia. Cada opción es una fila con su valor, un mínimo, un máximo y un paso;
/// Cambiar(fila, +1/-1) la mueve un paso, la guarda y la aplica. Las que tocan
/// algo global (volumen, campo de visión) se aplican acá; la sensibilidad y el eje
/// los lee JugadorFPS y el brillo lo suma AdaptacionOscuridad a la exposición.
/// </summary>
public static class OpcionesCrater
{
    public enum Fila { Sensibilidad, InvertirY, CampoDeVision, Brillo, Volumen }
    public const int CantidadDeFilas = 5;

    // multiplica la sensibilidad base del jugador
    public static float Sensibilidad { get; private set; } = 1f;
    public static bool InvertirY { get; private set; }
    public static float CampoDeVision { get; private set; } = 70f;
    // en EV: +1 duplica el brillo de la imagen
    public static float Brillo { get; private set; }
    public static float Volumen { get; private set; } = 1f;

    public static bool AyudasEscritas { get; private set; }

    public static void AlternarAyudas()
    {
        AyudasEscritas = !AyudasEscritas;
        PlayerPrefs.SetInt("crater.ayudas", AyudasEscritas ? 1 : 0);
        PlayerPrefs.Save();
    }

    static bool cargadas;

    // al empezar cada partida (también al reiniciar el Play en el editor)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AlCargar()
    {
        Cargar();
        Aplicar();
    }

    public static void Cargar()
    {
        Sensibilidad = PlayerPrefs.GetFloat("crater.sensibilidad", 1f);
        InvertirY = PlayerPrefs.GetInt("crater.invertirY", 0) == 1;
        CampoDeVision = PlayerPrefs.GetFloat("crater.fov", 70f);
        Brillo = PlayerPrefs.GetFloat("crater.brillo", 0f);
        Volumen = PlayerPrefs.GetFloat("crater.volumen", 1f);
        AyudasEscritas = PlayerPrefs.GetInt("crater.ayudas", 0) == 1;
        cargadas = true;
    }

    /// <summary>Mueve una opción un paso (direccion +1 o -1), la guarda y la aplica.</summary>
    public static void Cambiar(Fila fila, int direccion)
    {
        if (!cargadas) Cargar();
        switch (fila)
        {
            case Fila.Sensibilidad: Sensibilidad = Paso(Sensibilidad, direccion, 0.1f, 0.25f, 3f); break;
            case Fila.InvertirY: InvertirY = !InvertirY; break;
            case Fila.CampoDeVision: CampoDeVision = Paso(CampoDeVision, direccion, 5f, 55f, 100f); break;
            case Fila.Brillo: Brillo = Paso(Brillo, direccion, 0.25f, -1f, 2f); break;
            case Fila.Volumen: Volumen = Paso(Volumen, direccion, 0.1f, 0f, 1f); break;
        }
        PlayerPrefs.SetFloat("crater.sensibilidad", Sensibilidad);
        PlayerPrefs.SetInt("crater.invertirY", InvertirY ? 1 : 0);
        PlayerPrefs.SetFloat("crater.fov", CampoDeVision);
        PlayerPrefs.SetFloat("crater.brillo", Brillo);
        PlayerPrefs.SetFloat("crater.volumen", Volumen);
        PlayerPrefs.Save();
        Aplicar();
    }

    // redondea al paso para que no se acumulen errores (0,30000001...)
    static float Paso(float valor, int direccion, float paso, float min, float max) =>
        Mathf.Clamp(Mathf.Round((valor + direccion * paso) / paso) * paso, min, max);

    public static void Aplicar()
    {
        AudioListener.volume = Volumen;
        var cam = Camera.main;
        if (cam != null) cam.fieldOfView = CampoDeVision;
    }

    /// <summary>El texto de una fila para el menú: "Sensibilidad del mouse   1,2×".</summary>
    public static string Texto(Fila fila)
    {
        switch (fila)
        {
            case Fila.Sensibilidad: return $"Sensibilidad del mouse     {Sensibilidad:0.0}×";
            case Fila.InvertirY: return $"Invertir eje vertical     {(InvertirY ? "Sí" : "No")}";
            case Fila.CampoDeVision: return $"Campo de visión     {CampoDeVision:0}°";
            case Fila.Brillo: return $"Brillo     {(Brillo >= 0f ? "+" : "")}{Brillo:0.00}";
            default: return $"Volumen     {Volumen * 100f:0} %";
        }
    }
}
