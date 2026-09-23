using System.Collections;
using UnityEngine;

/// <summary>
/// Coordina el final: la adaptación a la oscuridad en la Cresta, el traslado
/// encubierto por el blanco hasta la capilla, el cambio a luz de día y el cierre.
/// </summary>
public class EclipseFinalController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] AdaptacionOscuridad adaptacion;
    [SerializeField] LinternaController linterna;
    [SerializeField] InterfazCrater interfaz;
    [SerializeField] FlujoJuegoCrater flujo;
    [SerializeField] PausaCrater pausa;
    [SerializeField] Transform jugador;
    [SerializeField] Transform spawnCapilla;

    [Header("Ambiente del epílogo")]
    [SerializeField] Light luzDelCrater;
    [SerializeField] Light solEpilogo;
    [SerializeField] Color cieloEpilogo = new Color(0.62f, 0.72f, 0.82f);
    [SerializeField] Color ambienteCieloEpilogo = new Color(0.55f, 0.6f, 0.68f);
    [SerializeField] Color ambienteHorizonteEpilogo = new Color(0.45f, 0.4f, 0.34f);
    [SerializeField] Color ambienteSueloEpilogo = new Color(0.22f, 0.16f, 0.1f);
    [SerializeField] float nieblaEpilogo = 0.006f;
    [SerializeField] AudioSource ambienteCrater;
    [SerializeField] AudioSource ambienteExterior;

    bool habilitado;
    bool transicionIniciada;
    bool cerrado;

    public bool EnEpilogo { get; private set; }

    void Update()
    {
        if (!habilitado || adaptacion == null || transicionIniciada) return;
        interfaz?.MostrarVelo(Color.white, Mathf.SmoothStep(0f, 0.88f, adaptacion.Progreso));
    }

    public void HabilitarEnCresta()
    {
        if (habilitado) return;
        habilitado = true;
        adaptacion?.Habilitar();
    }

    /// <summary>Conectado al evento 'alAdaptarse' de AdaptacionOscuridad.</summary>
    public void AlCompletarAdaptacion()
    {
        if (!transicionIniciada) StartCoroutine(TrasladarALaCapilla());
    }

    IEnumerator TrasladarALaCapilla()
    {
        transicionIniciada = true;
        interfaz?.OcultarPrompt();

        for (float t = 0f; t < 1.25f; t += Time.deltaTime)
        {
            interfaz?.MostrarVelo(Color.white, Mathf.Lerp(0.88f, 1f, t / 1.25f));
            yield return null;
        }
        interfaz?.MostrarVelo(Color.white, 1f);

        var control = jugador != null ? jugador.GetComponent<JugadorFPS>() : null;
        var cc = jugador != null ? jugador.GetComponent<CharacterController>() : null;
        if (control != null) control.enabled = false;
        if (cc != null) cc.enabled = false;

        if (linterna != null)
        {
            linterna.Encender(false);
            linterna.enabled = false;
        }

        if (jugador != null && spawnCapilla != null)
        {
            jugador.SetPositionAndRotation(spawnCapilla.position, spawnCapilla.rotation);
            control?.Orientar(spawnCapilla.rotation);
            control?.ReiniciarMovimiento();
            jugador.GetComponent<RespawnPorCaida>()?.RegistrarPuntoSeguro(spawnCapilla.position, spawnCapilla.rotation);
            Physics.SyncTransforms();
        }

        if (cc != null) cc.enabled = true;
        adaptacion?.Deshabilitar();
        AplicarAmbienteDeDia();
        EnEpilogo = true;
        yield return new WaitForSeconds(0.6f);

        if (interfaz != null) yield return interfaz.Fundir(Color.white, 1f, 0f, 3f);
        if (control != null) control.enabled = true;
        interfaz?.MostrarPromptTemporal("Caminá hacia la luz de afuera.", 6f);
        flujo?.EntrarEpilogo();
    }

    void AplicarAmbienteDeDia()
    {
        if (luzDelCrater != null) luzDelCrater.enabled = false;
        if (solEpilogo != null) solEpilogo.enabled = true;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambienteCieloEpilogo;
        RenderSettings.ambientEquatorColor = ambienteHorizonteEpilogo;
        RenderSettings.ambientGroundColor = ambienteSueloEpilogo;
        RenderSettings.fogColor = cieloEpilogo;
        RenderSettings.fogDensity = nieblaEpilogo;

        var camara = jugador != null ? jugador.GetComponentInChildren<Camera>() : Camera.main;
        if (camara != null) camara.backgroundColor = cieloEpilogo;

        if (ambienteCrater != null) ambienteCrater.Stop();
        if (ambienteExterior != null) ambienteExterior.Play();
    }

    /// <summary>Conectado a la zona de salida de la capilla.</summary>
    public void CerrarDemo()
    {
        if (cerrado) return;
        cerrado = true;
        flujo?.Finalizar();
        if (pausa != null) pausa.enabled = false;
        if (jugador != null)
        {
            var control = jugador.GetComponent<JugadorFPS>();
            if (control != null) control.enabled = false;
        }
        StartCoroutine(Cierre());
    }

    IEnumerator Cierre()
    {
        if (interfaz != null) yield return interfaz.MostrarCierre();

        // pantalla final: R vuelve a empezar, Esc sale
        while (true)
        {
            if (EntradaCrater.Presionada(UnityEngine.InputSystem.Key.R)) PausaCrater.ReiniciarEscena();
            if (EntradaCrater.Pausa) PausaCrater.Salir();
            yield return null;
        }
    }
}
