using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Eclipse durante la caminata desde las lomas. En el mirador se alcanza la totalidad
/// y se abre el pozo; la totalidad espera un encuadre visible, sin girar la mirada.
/// Al bajar, el ambiente exterior se mezcla con el del cráter. En el epílogo la tapa
/// vuelve a cerrar el pozo. Las referencias las conecta ConstructorCrater.
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
    [SerializeField] Transform spawnValle;
    [SerializeField] Transform mirador;
    [SerializeField] float eclipseSinCaminar = 80f;

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

    [Header("Cierre sin rastro en el epílogo")]
    [SerializeField] GameObject[] cierresEpilogo;
    [SerializeField] GameObject[] ocultarEpilogo;

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
    [SerializeField] float pausaTotalidad = 4f;
    [SerializeField] float revelado = 4f;
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
    bool viendoExteriorDesdeElCrater;
    bool saltar;
    float tiempoExterior;
    float progresoCaminata;
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
        if (Activo && !cinematicaHecha && !PausaCrater.EnPausa) AvanzarEclipse(Time.deltaTime);
        if (Reproduciendo && !PausaCrater.EnPausa && EntradaCrater.Saltar) saltar = true;
        if (EnElCrater && !entrando && jugador != null && puerta != null && cielo != null)
        {
            // El retorno opcional por la escalera también debe recuperar el horizonte.
            // La histéresis impide alternar la dirección sobre un mismo escalón.
            float altura = jugador.transform.position.y - puerta.position.y;
            bool exterior = viendoExteriorDesdeElCrater ? altura > -1f : altura > -.2f;
            if (exterior != viendoExteriorDesdeElCrater)
            {
                viendoExteriorDesdeElCrater = exterior;
                if (exterior) cielo.Mostrar(true);
                else RestaurarAmbienteDelCrater();
            }
        }
    }

    /// <summary>La aproximación al mirador adelanta el eclipse; esperar también lo hace avanzar.</summary>
    public void AvanzarEclipse(float delta)
    {
        tiempoExterior += Mathf.Max(0f, delta);
        if (jugador != null && spawnValle != null && mirador != null)
        {
            Vector3 inicio = spawnValle.position, fin = mirador.position;
            Vector3 trayecto = fin - inicio;
            trayecto.y = 0f;
            Vector3 recorrido = jugador.transform.position - inicio;
            recorrido.y = 0f;
            progresoCaminata = Mathf.Max(progresoCaminata,
                Mathf.Clamp01(Vector3.Dot(recorrido, trayecto) / Mathf.Max(0.01f, trayecto.sqrMagnitude)));
        }
        float k = Mathf.Clamp01(Mathf.Max(progresoCaminata * 0.97f, tiempoExterior / Mathf.Max(1f, eclipseSinCaminar)));
        // La caminata y el tiempo proponen el ritmo. El último contacto queda
        // reservado para el mirador y no ocurre fuera del encuadre del jugador.
        float objetivo = Mathf.Min(k, EclipseEnVista ? 0.92f : 0.6f);
        if (cielo != null) cielo.Progreso = Mathf.MoveTowards(cielo.Progreso,
            Mathf.Max(cielo.Progreso, objetivo), Mathf.Max(0, delta) / 14f);
        if (pajaros != null) pajaros.volume = volumenPajaros * (1f - Mathf.Clamp01((k - 0.78f) / 0.06f));
        if (graveEclipse != null && k > 0.8f)
        {
            if (!graveEclipse.isPlaying) graveEclipse.Play();
            graveEclipse.volume = volumenGrave * Mathf.Clamp01((k - 0.8f) / 0.2f);
        }
    }

    /// <summary>El sol está en el encuadre y no detrás del terreno o la capilla.</summary>
    public bool EclipseEnVista
    {
        get
        {
            if (cielo == null || jugador == null || jugador.camara == null) return true;
            var camara = jugador.camara;
            Vector3 hacia = cielo.DireccionSol.normalized;
            if (Vector3.Dot(camara.forward, hacia) < Mathf.Cos(22f * Mathf.Deg2Rad)) return false;
            return !Physics.Raycast(camara.position, hacia, 150f,
                ~LayerMask.GetMask("Jugador", "Ignore Raycast"), QueryTriggerInteraction.Ignore);
        }
    }

    // ---------------------------------------------------------------- cinemática

    /// <summary>Conectado a la zona del umbral de la capilla.</summary>
    public void IniciarCinematica()
    {
        if (!Activo || cinematicaHecha) return;
        cinematicaHecha = true;
        StartCoroutine(RevelarDesdeMirador());
    }

    IEnumerator RevelarDesdeMirador()
    {
        Reproduciendo = true;
        saltar = false;
        if (jugador != null) jugador.movimientoBloqueado = true;
        float desde = cielo != null ? cielo.Progreso : 0f;
        // No se gira la cámara. Si el jugador mira a otro lado, la totalidad
        // espera: su momento clave siempre puede observarse al volver a mirar.
        for (float t = 0; t < pausaTotalidad && !saltar;)
        {
            if (!PausaCrater.EnPausa && EclipseEnVista)
            {
                t += Time.deltaTime;
                if (cielo != null) cielo.Progreso = Mathf.Lerp(desde, 1f, Suave(Mathf.Clamp01(t / pausaTotalidad)));
            }
            yield return null;
        }
        if (cielo != null) cielo.Progreso = 1f;
        if (pajaros != null) { pajaros.volume = 0f; pajaros.Stop(); }
        if (sonidoRevelado != null) sonidoRevelado.Play();
        float graveDesde = graveEclipse != null ? graveEclipse.volume : 0f;
        yield return Animar(revelado, k =>
        {
            AplicarRevelado(k);
            if (graveEclipse != null) graveEclipse.volume = graveDesde * (1f - Mathf.Clamp01(k * 4f));
        });
        if (graveEclipse != null) graveEclipse.Stop();
        AplicarRevelado(1f);
        FijarDestello(0f, null);
        FijarBrilloPuerta(brilloPuertaReposo);
        if (jugador != null) jugador.movimientoBloqueado = false;
        Reproduciendo = false;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (jugador != null) jugador.movimientoBloqueado = false;
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

    /// <summary>El eclipse terminó: terreno continuo y sin marcas del cráter.</summary>
    public void PrepararEpilogo()
    {
        EnElCrater = false;
        viendoExteriorDesdeElCrater = false;
        if (crater != null) crater.SetActive(false);
        // la tierra vuelve a cerrar el pozo
        AplicarRevelado(0f);
        if (huella != null) huella.SetActive(false);
        if (ocultarEpilogo != null) foreach (var objeto in ocultarEpilogo) if (objeto != null) objeto.SetActive(false);
        if (cierresEpilogo != null) foreach (var objeto in cierresEpilogo) if (objeto != null) objeto.SetActive(true);
        if (tapa != null && cierresEpilogo != null && cierresEpilogo.Length > 0) tapa.gameObject.SetActive(false);
        if (zonaEpilogo != null) zonaEpilogo.SetActive(true);
        // Ninguna marca nueva en la casa: la experiencia desaparece del exterior.
        if (guino != null) guino.SetActive(false);
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
            destello.transform.position = posicionDestello + haciaLaCamara * 0.25f;
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
        viendoExteriorDesdeElCrater = false;
        cielo?.MostrarEnSubsuelo();
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
