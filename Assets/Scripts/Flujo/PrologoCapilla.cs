using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// El comienzo: la capilla de día, el eclipse y el cráter que aparece en el valle.
///
///  1. El jugador arranca lejos, del otro lado del valle, mirando la capilla: es la meta.
///     El sol está arriba de ella, delante del jugador.
///  2. Mientras camina hacia la capilla, la luna va tapando el sol (sin cinemática: la
///     cámara es siempre del jugador). Los pájaros se callan cerca de la totalidad.
///  3. En el mirador, un paso angosto entre dos cerros que encuadra la capilla, llega la
///     totalidad y, adelante, entre el jugador y la capilla, la tierra se abre: sube el
///     borde del cráter y destella su puerta. Mientras tiembla, se puede mirar pero no caminar.
///  4. El pozo es el cráter de verdad: la Explanada está en el fondo. Al cruzar la
///     puerta se baja caminando por una escalera hasta el fondo. No hay corte: la
///     luz, la niebla y el sonido pasan de a poco del valle al cráter.
///
/// Guarda el ambiente del cráter tal como lo dejó el constructor y lo restaura al entrar.
/// La apertura del cráter se saltea con Espacio, Enter o A.
///
/// CÓMO FUNCIONA
/// Cada frame mide cuánto avanzó el jugador del inicio al mirador (o cuánto tiempo pasó)
/// y le da ese progreso al cielo. El cráter del valle existe en la escena desde el
/// principio: sólo está escondido (el borde bajo tierra y una tapa de tierra sobre el
/// pozo). "Revelarlo" es animar esas posiciones y escalas. La zona de entrada cubre todo
/// el pozo: se baje por donde se baje, el ambiente cambia.
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

    [Header("El camino: el eclipse avanza mientras el jugador camina hacia la capilla")]
    [Tooltip("Donde empieza el jugador.")]
    [SerializeField] Transform inicioCamino;
    [Tooltip("El mirador entre los dos cerros: al llegar, totalidad y se abre el cráter adelante.")]
    [SerializeField] Transform mirador;
    [Tooltip("Hasta dónde llega el eclipse sólo caminando (el resto, al llegar al mirador).")]
    [SerializeField] float progresoAntesDelMirador = 0.88f;
    [Tooltip("Si el jugador no camina, el eclipse avanza igual: llega al mirador en estos segundos, y se abre el cráter al doble.")]
    [SerializeField] float segundosHastaEclipse = 50f;

    [Header("Tiempos (segundos)")]
    [SerializeField] float duracionTotalidad = 2.5f;
    [SerializeField] float pausaTotalidad = 1.2f;
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
    float tiempoAntesDelEclipse, avance;
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

        if (Activo && !cinematicaHecha) AvanzarEclipse();
    }

    // ---------------------------------------------------------------- el camino y el eclipse

    /// <summary>
    /// Sin cinemática: el sol está sobre la capilla, delante del jugador, y la luna lo va
    /// tapando a medida que camina hacia ella (o con el tiempo, si se queda quieto). Nunca
    /// retrocede. Cerca de la totalidad se callan los pájaros y entra el grave.
    /// </summary>
    void AvanzarEclipse()
    {
        float porCamino = 0f;
        if (jugador != null && inicioCamino != null && mirador != null)
        {
            Vector3 recorrido = mirador.position - inicioCamino.position;
            recorrido.y = 0f;
            Vector3 hecho = jugador.transform.position - inicioCamino.position;
            hecho.y = 0f;
            porCamino = Mathf.Clamp01(Vector3.Dot(hecho, recorrido.normalized) / Mathf.Max(0.01f, recorrido.magnitude));
        }
        tiempoAntesDelEclipse += Time.deltaTime;
        float porTiempo = segundosHastaEclipse > 0f ? tiempoAntesDelEclipse / segundosHastaEclipse : 0f;
        avance = Mathf.Max(avance, Mathf.Clamp01(Mathf.Max(porCamino, porTiempo)));
        float k = avance * progresoAntesDelMirador;
        if (cielo != null) cielo.Progreso = k;
        if (pajaros != null) pajaros.volume = volumenPajaros * (1f - Mathf.Clamp01((k - 0.7f) / 0.15f));
        if (graveEclipse != null)
        {
            if (k > 0.75f && !graveEclipse.isPlaying) { graveEclipse.volume = 0f; graveEclipse.Play(); }
            if (graveEclipse.isPlaying) graveEclipse.volume = volumenGrave * 0.5f * Mathf.Clamp01((k - 0.75f) / 0.13f);
        }
        // si no se acerca nunca, el cráter se abre igual (donde esté, mirando a la capilla)
        if (porTiempo >= 2f) IniciarCinematica();
    }

    /// <summary>Conectado a la zona del mirador (y por las dudas al umbral del atrio).</summary>
    public void IniciarCinematica()
    {
        if (!Activo || cinematicaHecha) return;
        cinematicaHecha = true;
        StartCoroutine(Revelacion());
    }

    /// <summary>
    /// En el mirador: la totalidad y, adelante, entre el jugador y la capilla, la tierra se abre
    /// en el cráter y destella su puerta. La cámara es del jugador todo el tiempo: sólo no puede
    /// caminar mientras tiembla la tierra. Cada paso es un Animar(duración, k => ...); si el
    /// jugador saltea, cada uno termina de golpe con k = 1 y el estado final queda bien aplicado.
    /// </summary>
    IEnumerator Revelacion()
    {
        Reproduciendo = true;
        saltar = false;
        if (jugador != null) { jugador.ReiniciarMovimiento(); jugador.movimientoBloqueado = true; }
        Transform camara = jugador != null ? jugador.camara : Camera.main.transform;

        // 1. totalidad
        float desde = cielo != null ? cielo.Progreso : 0f;
        float volPajaros = pajaros != null ? pajaros.volume : 0f;
        if (graveEclipse != null && !graveEclipse.isPlaying) { graveEclipse.volume = 0f; graveEclipse.Play(); }
        float volGrave = graveEclipse != null ? graveEclipse.volume : 0f;
        yield return Animar(duracionTotalidad, k =>
        {
            if (cielo != null) cielo.Progreso = Mathf.Lerp(desde, 1f, Suave(k));
            if (pajaros != null) pajaros.volume = volPajaros * (1f - k);
            if (graveEclipse != null) graveEclipse.volume = Mathf.Lerp(volGrave, volumenGrave, k);
        });
        yield return Esperar(pausaTotalidad);

        // 2. adelante, la tierra se abre: sube el borde del cráter
        if (sonidoRevelado != null && !saltar) sonidoRevelado.Play();
        yield return Animar(revelado, k => AplicarRevelado(k));

        // 3. el reflejo de la puerta: sube rápido, baja lento
        if (sonidoDestello != null && !saltar) sonidoDestello.Play();
        yield return Animar(duracionDestello, k =>
        {
            float d = k < 0.18f ? k / 0.18f : 1f - Mathf.SmoothStep(0f, 1f, (k - 0.18f) / 0.82f);
            FijarDestello(d, camara);
            FijarBrilloPuerta(Mathf.Max(d * brilloDestello * 0.25f, brilloPuertaReposo * Mathf.Clamp01(k * 2f)));
        });

        yield return Esperar(pausaFinal);

        // estado final, también si se saltó
        if (cielo != null) cielo.Progreso = 1f;
        if (pajaros != null) { pajaros.volume = 0f; pajaros.Stop(); }
        if (graveEclipse != null) { if (!graveEclipse.isPlaying) graveEclipse.Play(); graveEclipse.volume = volumenGrave; }
        AplicarRevelado(1f);
        FijarDestello(0f, camara);
        FijarBrilloPuerta(brilloPuertaReposo);

        if (jugador != null) jugador.movimientoBloqueado = false;
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
