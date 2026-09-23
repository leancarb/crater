using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pausa con Esc: congela el tiempo, silencia el audio y libera el cursor.
/// En pausa: Esc sigue, R reinicia, X sale. Fuera de pausa, un clic vuelve a
/// capturar el mouse si el sistema lo soltó.
/// </summary>
public class PausaCrater : MonoBehaviour
{
    public static bool EnPausa { get; private set; }

    [SerializeField] InterfazCrater interfaz;
    [Tooltip("Pausar automáticamente si la ventana pierde el foco.")]
    public bool pausarAlPerderFoco = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico() => EnPausa = false;

    void Start() => CapturarCursor(true);

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
            if (EntradaCrater.Presionada(UnityEngine.InputSystem.Key.R)) ReiniciarEscena();
            else if (EntradaCrater.Presionada(UnityEngine.InputSystem.Key.X)) Salir();
        }
        else if (Cursor.lockState != CursorLockMode.Locked && EntradaCrater.ClicIzquierdo)
        {
            CapturarCursor(true);
        }
    }

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
        EnPausa = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        CapturarCursor(true);
        interfaz?.MostrarPausa(false);
    }

    public static void ReiniciarEscena()
    {
        EnPausa = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void Salir()
    {
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
