using System.Collections;
using UnityEngine;

/// <summary>
/// Coordina el final: la adaptación a la oscuridad en la Cresta abre la puerta del
/// eclipse; cruzarla trae el anillo de diamante, el blanco y la capilla de día.
/// En el epílogo el cráter ya no está; los créditos llegan al quedarse en el lugar
/// donde estaba o después de un rato.
///
/// CÓMO FUNCIONA
///  1. HabilitarEnCresta (lo llama el flujo al entrar a la Cresta) enciende la adaptación.
///  2. La adaptación dispara AlCompletarAdaptacion: se abre la PuertaEclipse.
///  3. La zona detrás de la puerta llama a CruzarPuerta: silencio, anillo de diamante,
///     blanco, y el jugador se teletransporta a la capilla (x = 300) con luz de día.
///  4. En el epílogo, la zona del lugar del cráter (o el reloj de 2 minutos) llama a
///     CerrarDemo: la cámara sube al sol, fundido a blanco, créditos y la pantalla final.
/// Las esperas se hacen con corrutinas (IEnumerator + yield).
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
    [SerializeField] PuertaEclipse puerta;
    [SerializeField] PrologoCapilla prologo;
    [SerializeField] CieloEclipse cielo;

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

    [Header("Anillo de diamante")]
    [SerializeField] AudioSource tonoFinal;
    [SerializeField] float silencio = 0.5f;
    [SerializeField] float duracionDestello = 1.5f;
    [SerializeField] float blancoSostenido = 3f;

    [Header("Sonido del epílogo")]
    [Tooltip("La campana de la capilla: suena al volver.")]
    [SerializeField] AudioSource campana;
    [SerializeField] AudioSource musicaCreditos;
    [SerializeField] float volumenMusica = 0.6f;

    [Header("Créditos")]
    [Tooltip("Si el jugador no va al lugar del cráter, los créditos llegan solos.")]
    [SerializeField] float segundosHastaCreditos = 120f;
    [TextArea(1, 3)]
    [SerializeField] string[] creditos =
    {
        "CRÁTER",
        "Proyecto académico · FADU · 2026",
        "Gracias por jugar",
    };

    // banderas para que cada paso ocurra una sola vez aunque lo llamen de nuevo
    bool habilitado;
    bool transicionIniciada;
    bool cerrado;
    float tiempoEnEpilogo;

    public bool EnEpilogo { get; private set; }

    void Update()
    {
        if (!EnEpilogo || cerrado) return;
        tiempoEnEpilogo += Time.deltaTime;
        if (tiempoEnEpilogo >= segundosHastaCreditos) CerrarDemo();
    }

    public void HabilitarEnCresta()
    {
        if (habilitado) return;
        habilitado = true;
        adaptacion?.Habilitar();
    }

    /// <summary>Conectado al evento 'alAdaptarse' de AdaptacionOscuridad: aparece la puerta.</summary>
    public void AlCompletarAdaptacion()
    {
        if (puerta != null) puerta.Abrir();
        else CruzarPuerta();   // sin puerta en la escena, el final es directo
    }

    /// <summary>Conectado a la zona detrás de la puerta del eclipse.</summary>
    public void CruzarPuerta()
    {
        if (!transicionIniciada) StartCoroutine(TrasladarALaCapilla());
    }

    IEnumerator TrasladarALaCapilla()
    {
        transicionIniciada = true;
        interfaz?.OcultarPrompt();
        if (pausa != null) pausa.enabled = false;

        var control = jugador != null ? jugador.GetComponent<JugadorFPS>() : null;
        var cc = jugador != null ? jugador.GetComponent<CharacterController>() : null;
        if (control != null) control.enabled = false;

        // silencio total: viento, zumbido, pasos. El tono final ignora esta pausa (ignoreListenerPause)
        AudioListener.pause = true;
        yield return new WaitForSecondsRealtime(silencio);

        // anillo de diamante: la luz del sol vuelve de golpe
        if (tonoFinal != null)
        {
            tonoFinal.ignoreListenerPause = true;
            tonoFinal.Play();
        }
        if (interfaz != null) yield return interfaz.AnilloDeDiamante(duracionDestello);
        else yield return new WaitForSecondsRealtime(duracionDestello);
        yield return new WaitForSecondsRealtime(blancoSostenido);

        // con la pantalla ya blanca, el jugador se muda a la capilla sin que se note
        if (cc != null) cc.enabled = false;
        if (linterna != null)
        {
            linterna.Encender(false);
            linterna.enabled = false;   // en el epílogo no hay linterna
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
        prologo?.PrepararEpilogo();
        AudioListener.pause = false;
        if (pausa != null) pausa.enabled = true;
        EnEpilogo = true;
        yield return new WaitForSeconds(0.6f);

        if (campana != null) campana.PlayDelayed(1.2f);
        if (interfaz != null) yield return interfaz.Fundir(Color.white, 1f, 0f, 3f);
        if (control != null) control.enabled = true;
        flujo?.EntrarEpilogo();
    }

    /// <summary>Luz, ambiente, niebla, fondo y sonido del exterior de día.</summary>
    public void AplicarAmbienteDeDia()
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

    /// <summary>Luces y sonido del interior del cráter (al entrar desde el prólogo).</summary>
    public void AplicarAmbienteDelCrater()
    {
        if (luzDelCrater != null) luzDelCrater.enabled = true;
        if (solEpilogo != null) solEpilogo.enabled = false;
        if (ambienteExterior != null) ambienteExterior.Stop();
        if (ambienteCrater != null) ambienteCrater.Play();
    }

    /// <summary>Conectado a la zona donde estaba el cráter, en el epílogo.</summary>
    public void CerrarDemo()
    {
        if (cerrado || !EnEpilogo) return;
        cerrado = true;
        flujo?.Finalizar();
        if (pausa != null) pausa.enabled = false;
        StartCoroutine(Cierre());
    }

    IEnumerator SubirMusica()
    {
        musicaCreditos.volume = 0f;
        musicaCreditos.Play();
        for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime)
        {
            musicaCreditos.volume = Mathf.Lerp(0f, volumenMusica, t / 6f);
            yield return null;
        }
        musicaCreditos.volume = volumenMusica;
    }

    IEnumerator Cierre()
    {
        var control = jugador != null ? jugador.GetComponent<JugadorFPS>() : null;
        if (control != null) control.enabled = false;

        if (musicaCreditos != null) StartCoroutine(SubirMusica());

        // la mirada sube al sol limpio mientras todo se vuelve blanco
        var camara = control != null ? control.camara : null;
        if (camara != null && cielo != null)
        {
            Quaternion desde = camara.rotation;
            Quaternion hacia = Quaternion.LookRotation(cielo.DireccionSol);
            if (interfaz != null) StartCoroutine(interfaz.Fundir(Color.white, 0f, 1f, 4f));
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                camara.rotation = Quaternion.Slerp(desde, hacia, Mathf.SmoothStep(0f, 1f, t / 4f));
                yield return null;
            }
        }

        if (interfaz != null) yield return interfaz.MostrarCreditos(creditos);

        // pantalla final: R vuelve a empezar, Esc sale. Espera para siempre (hasta que se elija)
        while (true)
        {
            if (EntradaCrater.Presionada(UnityEngine.InputSystem.Key.R)) PausaCrater.ReiniciarEscena();
            if (EntradaCrater.Pausa) PausaCrater.Salir();
            yield return null;
        }
    }
}
