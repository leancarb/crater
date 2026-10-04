using System.Collections;
using UnityEngine;

/// <summary>
/// Coordina el final: al entrar a la Cresta se cierra la compuerta a la espalda;
/// la adaptación a la oscuridad abre el techo, entra la luz blanca del fin del
/// eclipse hasta inundarlo todo y el jugador aparece en la capilla, de día.
/// En el epílogo el cráter ya no está; los créditos llegan al quedarse en el lugar
/// donde estaba o después de un rato.
///
/// CÓMO FUNCIONA
///  1. HabilitarEnCresta (lo llama el flujo al entrar a la Cresta) enciende la adaptación.
///  2. La adaptación dispara AlCompletarAdaptacion: se abre el techo (AperturaTecho).
///     Mientras se abre, el cielo que se ve por el hueco pasa de la oscuridad a un
///     blanco que encandila, y la luz ambiente sube con él.
///  3. Con todo blanco: silencio, el tono del anillo de diamante, y el jugador se
///     muda a la capilla sin que se note. Después el blanco se disuelve en el día.
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
    [SerializeField] AperturaTecho techo;
    [SerializeField] PrologoCapilla prologo;
    [SerializeField] CieloEclipse cielo;

    [Header("Ambiente del epílogo")]
    [SerializeField] Light luzDelCrater;
    [SerializeField] Light solEpilogo;
    // el atardecer: los mismos tonos que CieloEclipse usa en el epílogo
    [SerializeField] Color cieloEpilogo = new Color(0.94f, 0.6f, 0.42f);
    [SerializeField] Color ambienteCieloEpilogo = new Color(0.5f, 0.42f, 0.55f);
    [SerializeField] Color ambienteHorizonteEpilogo = new Color(0.72f, 0.45f, 0.3f);
    [SerializeField] Color ambienteSueloEpilogo = new Color(0.2f, 0.12f, 0.08f);
    [SerializeField] float nieblaEpilogo = 0.006f;
    [SerializeField] AudioSource ambienteCrater;
    [SerializeField] float volumenCrater = 0.55f;
    [SerializeField] AudioSource ambienteExterior;

    [Header("La luz que entra por el techo")]
    [Tooltip("Color del cielo (HDR) cuando el techo terminó de abrirse: más de 1 encandila con el bloom.")]
    [SerializeField] Color cieloBlanco = new Color(6f, 6f, 6f);

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

    [Header("El arte de la Cresta: se ilumina junto con la luz que entra por el techo")]
    [SerializeField] Renderer[] arteCresta;
    [SerializeField] Color brilloArte = new Color(0.6f, 0.74f, 1f) * 3f;

    [Header("Créditos")]
    [Tooltip("Si el jugador no va al lugar del cráter, los créditos llegan solos.")]
    [SerializeField] float segundosHastaCreditos = 120f;
    [TextArea(1, 3)]
    [SerializeField] string[] creditos =
    {
        "CRÁTER",
        InterfazCrater.TarjetaCatedra,   // los logos de UBA FADU y de la Cátedra Campos Trilnick, y 2026
    };

    // banderas para que cada paso ocurra una sola vez aunque lo llamen de nuevo
    bool habilitado;
    bool transicionIniciada;
    bool cerrado;
    bool animando, saltar;   // Espacio, Enter o A saltean la animación en curso (sin aviso en pantalla)
    float tiempoEnEpilogo;
    float intensidadLuzCrater = -1f;   // la del constructor, para volver a ella

    public bool EnEpilogo { get; private set; }

    void Update()
    {
        if (animando && EntradaCrater.Saltar) saltar = true;
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

    /// <summary>Conectado al evento 'alAdaptarse' de AdaptacionOscuridad: se abre el techo.</summary>
    public void AlCompletarAdaptacion()
    {
        if (!transicionIniciada) StartCoroutine(Final());
    }

    IEnumerator Final()
    {
        transicionIniciada = true;
        animando = true;
        saltar = false;
        interfaz?.OcultarPrompt();
        if (pausa != null) pausa.enabled = false;

        // 1. el techo se abre y la luz del fin del eclipse entra por el hueco.
        // El jugador puede seguir mirando (y mirar para arriba)
        if (techo != null) techo.Abrir();
        float duracion = techo != null ? techo.Duracion : 6f;
        var camara = jugador != null ? jugador.GetComponentInChildren<Camera>() : Camera.main;
        Color fondoDesde = camara != null ? camara.backgroundColor : Color.black;
        Color cieloDesde = RenderSettings.ambientSkyColor, horizonteDesde = RenderSettings.ambientEquatorColor;
        for (float t = 0f; t < duracion && !saltar; t += Time.deltaTime)
        {
            float k = t / duracion;
            float luz = k * k;   // casi nada al principio, todo al final
            if (camara != null) camara.backgroundColor = Color.Lerp(fondoDesde, cieloBlanco, luz);
            RenderSettings.ambientSkyColor = Color.Lerp(cieloDesde, Color.white * 2f, luz);
            RenderSettings.ambientEquatorColor = Color.Lerp(horizonteDesde, Color.white, luz);
            IluminarArte(luz);
            yield return null;
        }
        IluminarArte(1f);

        // 2. todo blanco: el último destello y silencio
        var control = jugador != null ? jugador.GetComponent<JugadorFPS>() : null;
        if (control != null) control.enabled = false;
        if (interfaz != null) yield return interfaz.Fundir(Color.white, 0f, 1f, saltar ? 0.1f : 0.8f);

        // silencio total: viento, zumbido, pasos. El tono final ignora esta pausa (ignoreListenerPause)
        AudioListener.pause = true;
        yield return EsperarReal(silencio);

        // anillo de diamante: la luz del sol vuelve de golpe
        if (tonoFinal != null)
        {
            tonoFinal.ignoreListenerPause = true;
            tonoFinal.Play();
        }
        if (interfaz != null) yield return interfaz.AnilloDeDiamante(saltar ? 0.2f : duracionDestello);
        else yield return EsperarReal(duracionDestello);
        yield return EsperarReal(blancoSostenido);

        yield return MudarseALaCapilla(control);
    }

    IEnumerator MudarseALaCapilla(JugadorFPS control)
    {
        var cc = jugador != null ? jugador.GetComponent<CharacterController>() : null;

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
        if (interfaz != null) yield return interfaz.Fundir(Color.white, 1f, 0f, saltar ? 0.4f : 3f);
        if (control != null) control.enabled = true;
        animando = false;
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

    /// <summary>Luces y sonido del interior del cráter, de golpe (arrancar directo en la Explanada).</summary>
    public void AplicarAmbienteDelCrater()
    {
        GuardarIntensidadLuzCrater();
        if (luzDelCrater != null) { luzDelCrater.enabled = true; luzDelCrater.intensity = intensidadLuzCrater; }
        if (solEpilogo != null) solEpilogo.enabled = false;
        if (ambienteExterior != null) ambienteExterior.Stop();
        if (ambienteCrater != null) { ambienteCrater.volume = volumenCrater; ambienteCrater.Play(); }
    }

    /// <summary>Lo mismo, pero de a poco: el jugador entra caminando al cráter desde el valle.</summary>
    public void MezclarAlCrater(float segundos) => StartCoroutine(Mezclar(segundos));

    IEnumerator Mezclar(float segundos)
    {
        GuardarIntensidadLuzCrater();
        float volExterior = ambienteExterior != null ? ambienteExterior.volume : 0f;
        float intensidadSol = solEpilogo != null ? solEpilogo.intensity : 0f;
        if (luzDelCrater != null) { luzDelCrater.intensity = 0f; luzDelCrater.enabled = true; }
        if (ambienteCrater != null) { ambienteCrater.volume = 0f; ambienteCrater.Play(); }

        for (float t = 0f; t < segundos; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / segundos);
            if (luzDelCrater != null) luzDelCrater.intensity = intensidadLuzCrater * k;
            if (solEpilogo != null) solEpilogo.intensity = intensidadSol * (1f - k);
            if (ambienteExterior != null) ambienteExterior.volume = volExterior * (1f - k);
            if (ambienteCrater != null) ambienteCrater.volume = volumenCrater * k;
            yield return null;
        }

        if (solEpilogo != null) { solEpilogo.enabled = false; solEpilogo.intensity = intensidadSol; }
        if (ambienteExterior != null) { ambienteExterior.Stop(); ambienteExterior.volume = volExterior; }
        AplicarAmbienteDelCrater();
    }

    void GuardarIntensidadLuzCrater()
    {
        if (intensidadLuzCrater < 0f && luzDelCrater != null) intensidadLuzCrater = luzDelCrater.intensity;
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

    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock bloqueArte;

    /// <summary>Los tallados de la Cresta suben su brillo con la luz del techo (0 a 1).</summary>
    void IluminarArte(float luz)
    {
        if (arteCresta == null) return;
        bloqueArte ??= new MaterialPropertyBlock();
        foreach (var r in arteCresta)
        {
            if (r == null) continue;
            var emision = r.sharedMaterial != null && r.sharedMaterial.HasProperty(IdEmision) ? r.sharedMaterial.GetColor(IdEmision) : Color.black;
            r.GetPropertyBlock(bloqueArte);
            bloqueArte.SetColor(IdEmision, Color.Lerp(emision, brilloArte, luz));
            r.SetPropertyBlock(bloqueArte);
        }
    }

    IEnumerator EsperarReal(float segundos)
    {
        for (float t = 0f; t < segundos && !saltar; t += Time.unscaledDeltaTime) yield return null;
    }

    IEnumerator Cierre()
    {
        animando = true;
        saltar = false;
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
            for (float t = 0f; t < 4f && !saltar; t += Time.deltaTime)
            {
                camara.rotation = Quaternion.Slerp(desde, hacia, Mathf.SmoothStep(0f, 1f, t / 4f));
                yield return null;
            }
        }

        if (interfaz != null) yield return interfaz.MostrarCreditos(creditos, () => saltar);
        animando = false;

        // pantalla final: R vuelve a empezar, Esc sale. Espera para siempre (hasta que se elija)
        while (true)
        {
            if (EntradaCrater.Presionada(UnityEngine.InputSystem.Key.R)) PausaCrater.ReiniciarEscena();
            if (EntradaCrater.Pausa) PausaCrater.Salir();
            yield return null;
        }
    }
}
