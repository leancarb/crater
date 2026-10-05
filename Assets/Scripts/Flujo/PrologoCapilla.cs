using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// El comienzo: la capilla de día, el eclipse y el cráter que aparece en el valle.
///
///  1. El jugador arranca en la capilla, libre, sin indicaciones.
///  2. Al cruzar el umbral (zona) arranca la cinemática, sin control:
///     mira al sol, la luna lo tapa, los pájaros se callan, la tierra del valle se
///     abre en un pozo, el borde del cráter sube y en el borde destella una puerta.
///  3. El pozo es el cráter de verdad: la Explanada está en el fondo. Al cruzar la
///     puerta se baja caminando por una escalera hasta el fondo. No hay corte: la
///     luz, la niebla y el sonido pasan de a poco del valle al cráter.
///
/// Guarda el ambiente del cráter tal como lo dejó el constructor y lo restaura
/// al entrar. Después de verla una vez, la cinemática se salta con Espacio.
///
/// CÓMO FUNCIONA
/// Mientras dura la cinemática, JugadorFPS está apagado y este script mueve la cámara
/// directamente. El cráter del valle existe en la escena desde el principio: sólo está
/// escondido (el borde bajo tierra y una tapa de tierra sobre el pozo). "Revelarlo" es
/// animar esas posiciones y escalas. Si ya se vio una vez (se guarda en PlayerPrefs),
/// se puede saltar. La zona de entrada cubre todo el pozo: se baje por donde se baje,
/// el ambiente cambia.
/// </summary>
public class PrologoCapilla : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] JugadorFPS jugador;
    [SerializeField] CieloEclipse cielo;
    [SerializeField] EclipseFinalController eclipse;
    [SerializeField] FlujoJuegoCrater flujo;
    [SerializeField] InterfazCrater interfaz;
    [SerializeField] Transform spawnCrater;

    [Header("El cráter del valle")]
    [SerializeField] GameObject crater;
    [Tooltip("Piezas del borde: suben desde abajo de la tierra.")]
    [SerializeField] Transform[] bordes;
    [Tooltip("Tapa de tierra sobre el pozo: se abre desde el centro y en el epílogo vuelve a cerrarse.")]
    [SerializeField] Transform tapa;
    [SerializeField] Transform puerta;
    [SerializeField] Renderer[] contornoPuerta;
    [SerializeField] Renderer destello;
    [Tooltip("Pasto aplastado donde estaba el cráter. Sólo en el epílogo.")]
    [SerializeField] GameObject huella;
    [Tooltip("Zona del lugar del cráter que dispara los créditos. Sólo en el epílogo.")]
    [SerializeField] GameObject zonaEpilogo;
    [Tooltip("El guiño: la espiral de las anclas tallada sobre la puerta de la capilla. Sólo en el epílogo.")]
    [SerializeField] GameObject guino;

    [Header("Sonido")]
    [SerializeField] AudioSource pajaros;
    [SerializeField] AudioSource graveEclipse;
    [SerializeField] float volumenGrave = 0.5f;
    [Tooltip("La campana de la espadaña: suena al empezar.")]
    [SerializeField] AudioSource campana;
    [SerializeField] float demoraCampana = 4f;
    [Tooltip("Retumbo de la tierra mientras sube el borde del cráter.")]
    [SerializeField] AudioSource sonidoRevelado;
    [Tooltip("El brillo de la puerta.")]
    [SerializeField] AudioSource sonidoDestello;

    [Header("Tiempos (segundos)")]
    [SerializeField] float mirarAlSol = 2.5f;
    [SerializeField] float duracionEclipse = 7f;
    [Tooltip("Si el jugador no cruza el arco del atrio, el eclipse empieza solo después de estos segundos (0 = nunca).")]
    [SerializeField] float segundosHastaEclipse = 45f;
    [SerializeField] float pausaTotalidad = 1.2f;
    [SerializeField] float bajarMirada = 2.5f;
    [SerializeField] float revelado = 4f;
    [SerializeField] float duracionDestello = 2.2f;
    [SerializeField] float pausaFinal = 1f;
    [Tooltip("Cuánto tarda el ambiente en pasar del valle al cráter al cruzar la puerta.")]
    [SerializeField] float duracionEntrada = 5f;

    [Header("Aspecto")]
    [SerializeField] float profundidadOculta = 6f;
    [SerializeField] Color colorPuerta = new Color(0.75f, 0.85f, 1f);
    [SerializeField] float brilloPuertaReposo = 1.2f;
    [SerializeField] float brilloDestello = 9f;

    [Header("Pruebas")]
    [Tooltip("Arrancar directo en la Explanada, sin prólogo.")]
    public bool saltarPrologo;

    const string ClaveVista = "crater.prologoVisto";

    public bool Activo { get; private set; }
    public bool Reproduciendo { get; private set; }
    public bool EnElCrater { get; private set; }

    // ambiente del cráter, guardado antes de pisarlo con el del exterior
    AmbientMode modoAmbiente;
    Color ambienteCielo, ambienteHorizonte, ambienteSuelo, colorNiebla, fondoCamara;
    float densidadNiebla;
    bool niebla;

    float volumenPajaros;
    float[] alturasVisibles;
    Vector3 escalaTapa;
    Collider colliderTapa;
    Vector3 escalaDestello, posicionDestello;
    bool cinematicaHecha;
    bool entrando;
    bool saltar;
    float tiempoAntesDelEclipse;
    MaterialPropertyBlock bloque;
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
    static readonly int IdColor = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        GuardarAmbienteDelCrater();
        volumenPajaros = pajaros != null ? pajaros.volume : 0f;

        alturasVisibles = new float[bordes != null ? bordes.Length : 0];
        for (int i = 0; i < alturasVisibles.Length; i++)
            if (bordes[i] != null) alturasVisibles[i] = bordes[i].position.y;
        if (tapa != null)
        {
            escalaTapa = tapa.localScale;
            colliderTapa = tapa.GetComponent<Collider>();
        }
        if (destello != null)
        {
            escalaDestello = destello.transform.localScale;
            posicionDestello = destello.transform.position;
        }
    }

    void Start()
    {
        OcultarCrater();
        if (huella != null) huella.SetActive(false);
        if (zonaEpilogo != null) zonaEpilogo.SetActive(false);
        if (guino != null) guino.SetActive(false);

        if (saltarPrologo)
        {
            EntrarSinTransicion();
            return;
        }

        Activo = true;
        eclipse?.AplicarAmbienteDeDia();
        cielo?.Mostrar(true);
        if (cielo != null) cielo.Progreso = 0f;
        if (pajaros != null && !pajaros.isPlaying) pajaros.Play();
        if (campana != null) campana.PlayDelayed(demoraCampana);
    }

    void Update()
    {
        // se puede saltear siempre, sin aviso en pantalla (Espacio, Enter o A)
        if (Reproduciendo && EntradaCrater.Saltar) saltar = true;

        // si el jugador no se acerca a la capilla, el eclipse llega igual
        if (Activo && !cinematicaHecha && segundosHastaEclipse > 0f)
        {
            tiempoAntesDelEclipse += Time.deltaTime;
            if (tiempoAntesDelEclipse >= segundosHastaEclipse) IniciarCinematica();
        }
    }

    // ---------------------------------------------------------------- cinemática

    /// <summary>Conectado a la zona del umbral de la capilla.</summary>
    public void IniciarCinematica()
    {
        if (!Activo || cinematicaHecha) return;
        cinematicaHecha = true;
        StartCoroutine(Cinematica());
    }

    /// <summary>
    /// La cinemática en pasos. Cada paso es un Animar(duración, k => ...), donde k va
    /// de 0 a 1 a lo largo de la duración. Si el jugador salta, cada Animar termina de
    /// golpe con k = 1, así el estado final siempre queda bien aplicado.
    /// </summary>
    IEnumerator Cinematica()
    {
        Reproduciendo = true;
        saltar = false;
        if (jugador != null) jugador.enabled = false;
        Transform camara = jugador != null ? jugador.camara : Camera.main.transform;

        // 1. la mirada sube al sol
        Quaternion desde = camara.rotation;
        Quaternion haciaSol = Quaternion.LookRotation(cielo.DireccionSol);
        yield return Animar(mirarAlSol, k => camara.rotation = Quaternion.Slerp(desde, haciaSol, Suave(k)));

        // 2. la luna tapa el sol; los pájaros se callan de golpe cerca de la totalidad
        float volPajaros = pajaros != null ? pajaros.volume : 0f;
        if (graveEclipse != null) { graveEclipse.volume = 0f; graveEclipse.Play(); }
        yield return Animar(duracionEclipse, k =>
        {
            cielo.Progreso = k;
            if (pajaros != null) pajaros.volume = volPajaros * (1f - Mathf.Clamp01((k - 0.78f) / 0.06f));
            if (graveEclipse != null) graveEclipse.volume = volumenGrave * Mathf.Clamp01((k - 0.8f) / 0.2f);
        });

        // 3. totalidad
        yield return Esperar(pausaTotalidad);

        // 4. la mirada baja al valle mientras el cráter aparece
        desde = camara.rotation;
        Vector3 objetivo = puerta != null ? puerta.position + Vector3.up * 1.2f : camara.position + Vector3.forward;
        Quaternion haciaCrater = Quaternion.LookRotation(objetivo - camara.position);
        float total = Mathf.Max(bajarMirada, revelado);
        if (sonidoRevelado != null && !saltar) sonidoRevelado.Play();
        yield return Animar(total, k =>
        {
            float t = k * total;
            camara.rotation = Quaternion.Slerp(desde, haciaCrater, Suave(Mathf.Clamp01(t / bajarMirada)));
            AplicarRevelado(Mathf.Clamp01(t / revelado));
        });

        // 5. el reflejo de la puerta: sube rápido, baja lento
        if (sonidoDestello != null && !saltar) sonidoDestello.Play();
        yield return Animar(duracionDestello, k =>
        {
            float d = k < 0.18f ? k / 0.18f : 1f - Mathf.SmoothStep(0f, 1f, (k - 0.18f) / 0.82f);
            FijarDestello(d, camara);
            FijarBrilloPuerta(Mathf.Max(d * brilloDestello * 0.25f, brilloPuertaReposo * Mathf.Clamp01(k * 2f)));
        });

        yield return Esperar(pausaFinal);

        // estado final, también si se saltó
        cielo.Progreso = 1f;
        if (pajaros != null) { pajaros.volume = 0f; pajaros.Stop(); }
        if (graveEclipse != null) { if (!graveEclipse.isPlaying) graveEclipse.Play(); graveEclipse.volume = volumenGrave; }
        AplicarRevelado(1f);
        FijarDestello(0f, camara);
        FijarBrilloPuerta(brilloPuertaReposo);

        if (jugador != null)
        {
            jugador.MirarHacia(saltar ? objetivo : camara.position + camara.forward);
            jugador.enabled = true;
        }
        PlayerPrefs.SetInt(ClaveVista, 1);
        PlayerPrefs.Save();
        Reproduciendo = false;
    }

    // ---------------------------------------------------------------- entrada al cráter

    /// <summary>Conectado a la zona de la puerta del cráter del valle.</summary>
    public void EntrarAlCrater()
    {
        if (!Activo || EnElCrater || entrando || Reproduciendo || !cinematicaHecha) return;
        StartCoroutine(Entrar());
    }

    /// <summary>
    /// Sin fundido ni teletransporte: el jugador sigue caminando y el ambiente del valle
    /// (totalidad) se mezcla con el del cráter durante 'duracionEntrada' segundos.
    /// </summary>
    IEnumerator Entrar()
    {
        entrando = true;
        Activo = false;
        EnElCrater = true;
        flujo?.IniciarCrater();
        eclipse?.MezclarAlCrater(duracionEntrada);

        // el ambiente de ahora (el del valle en la totalidad) y el del cráter
        Color cieloDesde = RenderSettings.ambientSkyColor, horizonteDesde = RenderSettings.ambientEquatorColor;
        Color sueloDesde = RenderSettings.ambientGroundColor, nieblaDesde = RenderSettings.fogColor;
        float densidadDesde = RenderSettings.fogDensity;
        var cam = jugador != null ? jugador.GetComponentInChildren<Camera>() : Camera.main;
        Color fondoDesde = cam != null ? cam.backgroundColor : colorNiebla;
        float volGrave = graveEclipse != null ? graveEclipse.volume : 0f;

        for (float t = 0f; t < duracionEntrada; t += Time.deltaTime)
        {
            float k = Suave(t / duracionEntrada);
            RenderSettings.ambientSkyColor = Color.Lerp(cieloDesde, ambienteCielo, k);
            RenderSettings.ambientEquatorColor = Color.Lerp(horizonteDesde, ambienteHorizonte, k);
            RenderSettings.ambientGroundColor = Color.Lerp(sueloDesde, ambienteSuelo, k);
            RenderSettings.fogColor = Color.Lerp(nieblaDesde, colorNiebla, k);
            RenderSettings.fogDensity = Mathf.Lerp(densidadDesde, densidadNiebla, k);
            if (cam != null) cam.backgroundColor = Color.Lerp(fondoDesde, fondoCamara, k);
            if (graveEclipse != null) graveEclipse.volume = volGrave * (1f - k);
            yield return null;
        }

        if (pajaros != null) pajaros.Stop();
        if (graveEclipse != null) graveEclipse.Stop();
        RestaurarAmbienteDelCrater();
        entrando = false;
    }

    /// <summary>Arrancar directo en la Explanada (para probar): el cráter ya abierto y en totalidad.</summary>
    void EntrarSinTransicion()
    {
        Activo = false;
        EnElCrater = true;
        cinematicaHecha = true;
        LlevarJugador(spawnCrater);
        AplicarRevelado(1f);
        FijarBrilloPuerta(brilloPuertaReposo);

        if (pajaros != null) pajaros.Stop();
        if (graveEclipse != null) graveEclipse.Stop();
        cielo?.Mostrar(true);
        if (cielo != null) cielo.Progreso = 1f;
        eclipse?.AplicarAmbienteDelCrater();
        RestaurarAmbienteDelCrater();
        flujo?.IniciarCrater();
    }

    // ---------------------------------------------------------------- epílogo

    /// <summary>El eclipse terminó: el cráter ya no está, sólo queda la huella en el pasto.</summary>
    public void PrepararEpilogo()
    {
        if (crater != null) crater.SetActive(false);
        // la tierra vuelve a cerrar el pozo
        AplicarRevelado(0f);
        if (huella != null) huella.SetActive(true);
        if (zonaEpilogo != null) zonaEpilogo.SetActive(true);
        // al volver, sobre la puerta está la espiral de las anclas: antes no estaba
        if (guino != null) guino.SetActive(true);
        cielo?.Mostrar(true);
        cielo?.PonerDespues();
        if (pajaros != null) { pajaros.volume = volumenPajaros; pajaros.Play(); }
    }

    // ---------------------------------------------------------------- estados

    void OcultarCrater()
    {
        AplicarRevelado(0f);
        FijarBrilloPuerta(0f);
        FijarDestello(0f, null);
    }

    void AplicarRevelado(float r)
    {
        if (bordes != null)
        {
            for (int i = 0; i < bordes.Length; i++)
            {
                if (bordes[i] == null) continue;
                // las piezas suben en ola, no todas juntas: cada una arranca con un retraso según su índice
                float retraso = bordes.Length > 1 ? (float)i / (bordes.Length - 1) * 0.35f : 0f;
                float k = Suave(Mathf.Clamp01((r - retraso) / 0.65f));
                Vector3 p = bordes[i].position;
                p.y = alturasVisibles[i] - profundidadOculta * (1f - k);
                bordes[i].position = p;
            }
        }

        if (tapa != null)
        {
            // la tierra se abre desde el centro, apenas empieza a subir el borde
            float k = Suave(Mathf.Clamp01(r / 0.6f));
            tapa.gameObject.SetActive(k < 0.999f);
            tapa.localScale = new Vector3(escalaTapa.x * (1f - k), escalaTapa.y, escalaTapa.z * (1f - k));
            // sólo se puede pisar cerrada (en el epílogo)
            if (colliderTapa != null) colliderTapa.enabled = k <= 0.001f;
        }

        if (puerta != null) puerta.gameObject.SetActive(r > 0.5f);
    }

    void FijarBrilloPuerta(float brillo)
    {
        if (contornoPuerta == null) return;
        foreach (var r in contornoPuerta)
        {
            if (r == null) continue;
            r.enabled = brillo > 0.001f;
            r.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, colorPuerta * brillo);
            r.SetPropertyBlock(bloque);
        }
    }

    void FijarDestello(float d, Transform camara)
    {
        if (destello == null) return;
        destello.enabled = d > 0.001f;
        destello.transform.localScale = escalaDestello * Mathf.Lerp(0.3f, 7.5f, d);
        if (camara != null)
        {
            // un poco hacia la cámara: así el quad no se corta contra el suelo ni los pilares
            Vector3 haciaLaCamara = (camara.position - posicionDestello).normalized;
            destello.transform.position = posicionDestello + haciaLaCamara * 2.5f;
            destello.transform.rotation = Quaternion.LookRotation(destello.transform.position - camara.position);
        }
        Color c = colorPuerta * brilloDestello * d;
        c.a = d;
        destello.GetPropertyBlock(bloque);
        bloque.SetColor(IdColor, c);
        destello.SetPropertyBlock(bloque);
    }

    void LlevarJugador(Transform destino)
    {
        if (jugador == null || destino == null) return;
        var cc = jugador.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        jugador.transform.SetPositionAndRotation(destino.position, destino.rotation);
        jugador.Orientar(destino.rotation);
        jugador.ReiniciarMovimiento();
        jugador.GetComponent<RespawnPorCaida>()?.RegistrarPuntoSeguro(destino.position, destino.rotation);
        Physics.SyncTransforms();
        if (cc != null) cc.enabled = true;
    }

    /// <summary>
    /// La escena se guarda con la iluminación del interior del cráter. Antes de pisarla
    /// con la del día se copia acá, para devolverla al entrar a la Explanada.
    /// </summary>
    void GuardarAmbienteDelCrater()
    {
        modoAmbiente = RenderSettings.ambientMode;
        ambienteCielo = RenderSettings.ambientSkyColor;
        ambienteHorizonte = RenderSettings.ambientEquatorColor;
        ambienteSuelo = RenderSettings.ambientGroundColor;
        niebla = RenderSettings.fog;
        colorNiebla = RenderSettings.fogColor;
        densidadNiebla = RenderSettings.fogDensity;
        var cam = jugador != null ? jugador.GetComponentInChildren<Camera>() : Camera.main;
        fondoCamara = cam != null ? cam.backgroundColor : colorNiebla;
    }

    void RestaurarAmbienteDelCrater()
    {
        RenderSettings.ambientMode = modoAmbiente;
        RenderSettings.ambientSkyColor = ambienteCielo;
        RenderSettings.ambientEquatorColor = ambienteHorizonte;
        RenderSettings.ambientGroundColor = ambienteSuelo;
        RenderSettings.fog = niebla;
        RenderSettings.fogColor = colorNiebla;
        RenderSettings.fogDensity = densidadNiebla;
        var cam = jugador != null ? jugador.GetComponentInChildren<Camera>() : Camera.main;
        if (cam != null) cam.backgroundColor = fondoCamara;
    }

    // ---------------------------------------------------------------- utilidades

    /// <summary>Llama a 'paso' cada frame con k de 0 a 1. Termina en k = 1 (también si se salta).</summary>
    IEnumerator Animar(float duracion, Action<float> paso)
    {
        for (float t = 0f; t < duracion && !saltar; t += Time.deltaTime)
        {
            paso(t / duracion);
            yield return null;
        }
        paso(1f);
    }

    IEnumerator Esperar(float segundos)
    {
        for (float t = 0f; t < segundos && !saltar; t += Time.deltaTime)
            yield return null;
    }

    static float Suave(float k) => Mathf.SmoothStep(0f, 1f, k);
}
