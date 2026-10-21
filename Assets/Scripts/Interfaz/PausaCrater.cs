using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pausa con Esc: congela el tiempo, silencia el audio y libera el cursor.
/// En pausa se ajusta la luminosidad y se ven los controles: Esc sigue, R reinicia, X sale.
/// (Las opciones guardadas de antes se siguen aplicando: OpcionesCrater.)
/// Fuera de pausa, un clic vuelve a capturar el mouse si el sistema lo soltó.
///
/// CÓMO FUNCIONA
/// Pausar = Time.timeScale en 0 (todo lo que usa Time.deltaTime se congela) y
/// AudioListener.pause en true (todo el audio se detiene). 'EnPausa' es estático
/// para que cualquier script lo consulte sin tener una referencia a este.
/// </summary>
public class PausaCrater : MonoBehaviour
{
    public static bool EnPausa { get; private set; }

    [SerializeField] InterfazCrater interfaz;
    [Tooltip("Pausar automáticamente si la ventana pierde el foco.")]
    public bool pausarAlPerderFoco = false;

    // los campos estáticos sobreviven entre partidas en el editor: se limpian al dar Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico() => EnPausa = false;


    void Start()
    {
        Application.runInBackground = true;
        CapturarCursor(true);
        OpcionesCrater.Aplicar();   // al reiniciar la escena, el campo de visión vuelve al guardado
    }

    void OnDisable()
    {
        if (EnPausa) Reanudar();
    }

    void Update()
    {
        if (EntradaCrater.Pausa)
        {
            if (EnPausa) Reanudar();
            else Pausar();
            return;
        }

        if (EnPausa)
        {
            if (EntradaCrater.AlternarAyudas)
            {
                OpcionesCrater.AlternarAyudas();
                if (!OpcionesCrater.AyudasEscritas) interfaz?.OcultarPrompt();
                interfaz?.MostrarPausa(true);
            }
            if (EntradaCrater.Seguir) Reanudar();
            else if (EntradaCrater.Reiniciar) ReiniciarEscena();
            else if (EntradaCrater.Salir) Salir();
        }
        else if (Cursor.lockState != CursorLockMode.Locked && EntradaCrater.ClicIzquierdo)
        {
            CapturarCursor(true);
        }
    }

    // Opción desactivada por defecto: permite capturas al cambiar de ventana.
    void OnApplicationFocus(bool foco)
    {
        if (!foco && pausarAlPerderFoco && !Application.isBatchMode && isActiveAndEnabled && !EnPausa)
            Pausar();
    }

    public void Pausar()
    {
        EnPausa = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        CapturarCursor(false);
        interfaz?.MostrarPausa(true);
    }

    public void Reanudar()
    {
        OpcionesCrater.Guardar();
        EnPausa = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        CapturarCursor(true);
        interfaz?.MostrarPausa(false);
    }

    /// <summary>Vuelve a cargar la escena desde cero (todo vuelve a su estado inicial).</summary>
    public static void ReiniciarEscena()
    {
        OpcionesCrater.Guardar();
        EnPausa = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>En el editor detiene el Play; en el juego compilado cierra la aplicación.</summary>
    public static void Salir()
    {
        OpcionesCrater.Guardar();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static void CapturarCursor(bool capturar)
    {
        Cursor.lockState = capturar ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !capturar;
    }
}
