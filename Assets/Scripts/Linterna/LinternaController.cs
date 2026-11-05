using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Núcleo del juego. Va en un GameObject hijo de la cámara, con un Spot Light.
///
/// Reglas:
///  - F enciende y apaga.
///  - Clic izquierdo/derecho o R1/L1 sostienen SOL/LUNA. Cambiar tarda 'demoraDeCambio'.
///  - Al soltar, o tras cinco segundos continuos, vuelve a blanco con una transición breve.
///    Tras agotarlo hay que soltar antes de activarlo otra vez.
///  - Cada frame busca los receptores dentro del cono y les avisa que están iluminados.
///
/// CÓMO FUNCIONA
///  1. Lee la entrada (EntradaCrater) y prende/apaga o cambia de filtro.
///  2. Aplica el filtro al Spot Light: color, ángulo, alcance e intensidad.
///  3. Si está encendida, busca receptores con OverlapSphere (una esfera del largo
///     del alcance), descarta los que quedan fuera del ángulo del cono y los que
///     tienen una pared o una reja sólida en el medio (Raycast), y a los que quedan les
///     llama RecibirLuz().
/// Otros sistemas no leen el teclado: escuchan los eventos (AlCambiarEncendido,
/// AlEquiparFiltro, etc.) o consultan las propiedades de estado.
/// 'Instancia' da acceso a la única linterna desde cualquier script (patrón singleton).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LinternaController : MonoBehaviour
{
    public static LinternaController Instancia { get; private set; }

    [Header("Referencias")]
    public Light spot;
    [Tooltip("Sólo los objetos en estas capas pueden recibir el haz.")]
    public LayerMask capaReceptores = ~0;
    [Tooltip("Capas que bloquean el haz (paredes, piso, compuertas). Dejar sin los receptores.")]
    public LayerMask capaObstaculos = 1;   // 1 = capa Default (bit 0)

    [Header("Filtros (SOL y LUNA, por botón sostenido)")]
    public List<FiltroDefinicion> filtros = new List<FiltroDefinicion>();
    [Tooltip("Filtros que el jugador ya encontró. Empieza vacío.")]
    public List<FiltroDefinicion> filtrosDesbloqueados = new List<FiltroDefinicion>();

    [Header("Luz blanca (sin filtro)")]
    public Color colorBase = new Color(1f, 0.95f, 0.86f);
    public float anguloBase = 52f;
    public float alcanceBase = 24f;
    public float intensidadBase = 650f;

    [Header("Ajustes")]
    public float demoraDeCambio = 0.18f;
    [Min(.1f)] public float duracionFiltro = 5f;
    [Tooltip("Si está tildado, la linterna no responde hasta llamar a Recoger().")]
    public bool requiereRecogerla = true;

    [Header("Sonido")]
    public AudioClip sonidoEncender;
    public AudioClip sonidoApagar;
    [Min(.01f)] public float fundidoZumbido = .18f;

    // --- estado (lo leen la interfaz, la adaptación, la puerta, los tests) ---
    public bool Encendida { get; private set; }
    public FiltroDefinicion FiltroActual { get; private set; }
    public bool CambiandoFiltro { get; private set; }
    public bool Disponible => !requiereRecogerla || yaRecogida;
    // datos del último frame para la mira de la interfaz: ¿apunto a algo?, ¿lo acepta?, ¿cuánto cargó?
    public bool HayObjetivo { get; private set; }
    public bool ObjetivoAceptaFiltro { get; private set; }
    public float CargaObjetivo { get; private set; }
    public Color ColorActual => FiltroActual != null ? FiltroActual.color : colorBase;
    public float AlcanceActual => FiltroActual != null ? FiltroActual.alcance : alcanceBase;
    public float AnguloActual => FiltroActual != null ? FiltroActual.anguloCono : anguloBase;
    float IntensidadActual => FiltroActual != null ? FiltroActual.intensidad : intensidadBase;

    // eventos de C#: otros scripts se suscriben con += (ver FlujoJuegoCrater.Vincular)
    public event Action<bool> AlCambiarEncendido;
    public event Action AlRecogerLinterna;
    public event Action<FiltroDefinicion> AlDesbloquearFiltro;
    public event Action<FiltroDefinicion> AlEquiparFiltro;

    bool yaRecogida;
    float finDelCambio;
    float venceFiltro;
    public float TiempoFiltroRestante => FiltroActual != null && Encendida ? Mathf.Clamp(venceFiltro-Time.time,0,duracionFiltro) : 0f;
    bool solAgotado,lunaAgotada;
    bool controlandoFiltroSostenido;
    bool solAnterior, lunaAnterior;
    int ultimoFiltroSostenido;
    AudioSource audioSource;
    AudioSource zumbidoA, zumbidoB, zumbidoObjetivo;
    float volumenZumbido;
    // buffer fijo para OverlapSphereNonAlloc: no crea basura en memoria cada frame
    readonly Collider[] buffer = new Collider[32];
    // un receptor puede tener varios colliders: se procesa una sola vez por frame
    readonly HashSet<ReceptorDeLuz> receptoresProcesados = new HashSet<ReceptorDeLuz>();

    void Awake()
    {
        Instancia = this;
        // referencias vacías en el Inspector no deben romper nada
        filtros.RemoveAll(filtro => filtro == null);
        filtrosDesbloqueados.RemoveAll(filtro => filtro == null);
        if (spot == null) spot = GetComponentInChildren<Light>();
        audioSource = GetComponent<AudioSource>();
        volumenZumbido = audioSource != null ? audioSource.volume : .5f;
        zumbidoA = CrearZumbido("Zumbido_A");
        zumbidoB = CrearZumbido("Zumbido_B");
        if (audioSource != null) { audioSource.loop = false; audioSource.clip = null; }
        AplicarFiltro(null);
        Encendida = false;
        if (spot != null) spot.enabled = false;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Update()
    {
        if (!PausaCrater.EnPausa && !CinematicaDeSello.Reproduciendo) LeerEntrada();

        if(Encendida && FiltroActual!=null && Time.time>=venceFiltro)
        {
            if(filtros.Count>0 && FiltroActual==filtros[0])solAgotado=true;
            if(filtros.Count>1 && FiltroActual==filtros[1])lunaAgotada=true;
            solAgotado|=solAnterior;lunaAgotada|=lunaAnterior;
            VolverALuzBlanca();
        }

        // se recalculan en IluminarReceptores
        HayObjetivo = false;
        ObjetivoAceptaFiltro = false;
        CargaObjetivo = 0f;

        if (CambiandoFiltro && Time.time >= finDelCambio)
            CambiandoFiltro = false;


        // durante el cambio de filtro la luz baja casi a cero: el cambio se ve y se siente
        if (spot != null)
        {
            spot.color = Color.Lerp(spot.color, ColorActual, 1f - Mathf.Exp(-16f * Time.deltaTime));
            // MoveTowards: la intensidad se acerca al objetivo de a poco, sin saltos
            float objetivo = CambiandoFiltro ? IntensidadActual * 0.08f : IntensidadActual;
            // Mantener legible la pintura y la respuesta del tallado cercano: limitar iluminancia, conservando
            // potencia a distancia y el alcance del haz y de los receptores.
            if (Physics.Raycast(spot.transform.position, spot.transform.forward,
                out var superficie, 20f, capaObstaculos, QueryTriggerInteraction.Ignore))
            {
                bool cartel=superficie.collider.transform.parent!=null&&superficie.collider.transform.parent.GetComponentInChildren<CartelTutorial>()!=null;
                if(FiltroActual!=null||cartel)
                {
                    float distancia = Mathf.Max(0.4f, superficie.distance);
                    float distanciaReferencia = FiltroActual == null ? 20f : 8f;
                    objetivo *= Mathf.Min(1f, distancia * distancia / (distanciaReferencia * distanciaReferencia));
                }
            }
            spot.intensity = Mathf.MoveTowards(spot.intensity, objetivo, IntensidadActual * 6f * Time.deltaTime);
        }

        if (Encendida && !CambiandoFiltro)
            IluminarReceptores(Time.deltaTime);
        FundirZumbido(zumbidoA);
        FundirZumbido(zumbidoB);
    }

    void LeerEntrada()
    {
        if (!Disponible) return;   // todavía no la encontró

        if (EntradaCrater.Linterna)
            Encender(!Encendida);

        ProcesarFiltrosSostenidos(EntradaCrater.Filtro(0), EntradaCrater.Filtro(1));
    }

    /// <summary>El último botón presionado tiene prioridad; soltarlo recupera el otro si sigue sostenido.</summary>
    public void ProcesarFiltrosSostenidos(bool sol, bool luna)
    {
        if(!sol)solAgotado=false;
        if(!luna)lunaAgotada=false;
        if (sol && !solAnterior) ultimoFiltroSostenido = 0;
        if (luna && !lunaAnterior) ultimoFiltroSostenido = 1;
        solAnterior = sol; lunaAnterior = luna;
        FiltroDefinicion Elegir(int indice) => indice < filtros.Count && EstaDesbloqueado(filtros[indice]) ? filtros[indice] : null;
        var fSol = sol && !solAgotado ? Elegir(0) : null;
        var fLuna = luna && !lunaAgotada ? Elegir(1) : null;
        var solicitado = fSol != null && fLuna != null ? (ultimoFiltroSostenido == 0 ? fSol : fLuna) : fSol ?? fLuna;
        if (!Encendida || !Disponible) solicitado = null;
        if (solicitado != null) controlandoFiltroSostenido = true;
        if (!controlandoFiltroSostenido || solicitado == FiltroActual) return;
        CambiandoFiltro = false; // Soltar durante el cambio también debe quitar el filtro.
        EquiparFiltro(solicitado);
        if (solicitado == null)
        {
            controlandoFiltroSostenido = false;
        }
    }

    /// <summary>Cambia el filtro. Sólo uno a la vez, con demora. Null = luz blanca.</summary>
    public void EquiparFiltro(FiltroDefinicion f)
    {
        if (CambiandoFiltro || f == FiltroActual) return;
        CambiandoFiltro = true;
        finDelCambio = Time.time + demoraDeCambio;
        venceFiltro=f!=null ? Time.time+Mathf.Max(.1f,duracionFiltro) : 0;
        AplicarFiltro(f);
        AlEquiparFiltro?.Invoke(f);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) return;
        if (f != null && f.sonidoAlEquipar != null) audioSource.PlayOneShot(f.sonidoAlEquipar);
        ActualizarZumbido();
    }

    /// <summary>Vuelve a la luz blanca aunque esté en medio de un cambio de filtro.</summary>
    public void VolverALuzBlanca()
    {
        if (FiltroActual == null) return;
        CambiandoFiltro = false;
        EquiparFiltro(null);
    }

    /// <summary>Copia los valores del filtro (o de la luz blanca) al Spot Light.</summary>
    void AplicarFiltro(FiltroDefinicion f)
    {
        FiltroActual = f;
        if (spot == null) return;
        if (!CambiandoFiltro) spot.color = ColorActual;
        spot.spotAngle = AnguloActual;
        spot.innerSpotAngle = AnguloActual * (f == null ? .12f : .55f);
        spot.range = AlcanceActual;
    }

    /// <summary>Prende o apaga. Avisa con AlCambiarEncendido (lo escuchan el flujo y la interfaz).</summary>
    public void Encender(bool valor)
    {
        if (valor && !Disponible) return;
        if (Encendida == valor) return;
        Encendida = valor;
        if (spot != null)
        {
            spot.enabled = valor;
            if (valor) spot.intensity = IntensidadActual * 0.3f;   // arranque con un pequeño parpadeo
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            var click = valor ? sonidoEncender : sonidoApagar;
            if (click != null) audioSource.PlayOneShot(click);
            ActualizarZumbido();
        }
        AlCambiarEncendido?.Invoke(valor);
    }

    AudioSource CrearZumbido(string nombre)
    {
        var hijo = new GameObject(nombre);
        hijo.transform.SetParent(transform, false);
        var fuente = hijo.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.volume = 0f;
        if (audioSource != null)
        {
            fuente.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            fuente.spatialBlend = audioSource.spatialBlend;
            fuente.dopplerLevel = 0f;
        }
        return fuente;
    }

    void FundirZumbido(AudioSource fuente)
    {
        if (fuente == null || PausaCrater.EnPausa) return;
        float objetivo = fuente == zumbidoObjetivo ? volumenZumbido : 0f;
        fuente.volume = Mathf.MoveTowards(fuente.volume, objetivo,
            volumenZumbido * Time.deltaTime / Mathf.Max(.01f, fundidoZumbido));
        if (objetivo == 0f && fuente.volume == 0f && fuente.isPlaying) fuente.Stop();
    }

    /// <summary>Dos bucles cruzan su volumen; clicks y equipar siguen en la fuente original.</summary>
    void ActualizarZumbido()
    {
        var clip = Encendida && FiltroActual != null ? FiltroActual.zumbido : null;
        if (clip == null) { zumbidoObjetivo = null; return; }
        if (zumbidoA == null || zumbidoB == null) return;
        if (zumbidoA.clip == clip && zumbidoA.isPlaying) zumbidoObjetivo = zumbidoA;
        else if (zumbidoB.clip == clip && zumbidoB.isPlaying) zumbidoObjetivo = zumbidoB;
        else
        {
            zumbidoObjetivo = zumbidoA.volume <= zumbidoB.volume ? zumbidoA : zumbidoB;
            zumbidoObjetivo.Stop();
            zumbidoObjetivo.clip = clip;
            zumbidoObjetivo.volume = 0f;
            zumbidoObjetivo.Play();
        }
    }

    void OnEnable() { if (zumbidoA != null) ActualizarZumbido(); }

    void OnDisable()
    {
        zumbidoObjetivo = null;
        if (zumbidoA != null) { zumbidoA.Stop(); zumbidoA.volume = 0f; }
        if (zumbidoB != null) { zumbidoB.Stop(); zumbidoB.volume = 0f; }
    }

    /// <summary>Llamar cuando el jugador encuentra la linterna misma (El Umbral).</summary>
    public void Recoger()
    {
        if (yaRecogida) return;
        yaRecogida = true;
        AlRecogerLinterna?.Invoke();
    }

    /// <summary>Llamar cuando el jugador encuentra un filtro.</summary>
    public void Desbloquear(FiltroDefinicion f)
    {
        if (f == null || filtrosDesbloqueados.Contains(f)) return;
        filtrosDesbloqueados.Add(f);
        AlDesbloquearFiltro?.Invoke(f);
    }

    public bool EstaDesbloqueado(FiltroDefinicion f) => f != null && filtrosDesbloqueados.Contains(f);

    /// <summary>
    /// Busca todos los receptores dentro del cono y con línea de vista libre,
    /// y les avisa. Soporta varios receptores a la vez (necesario para los puentes).
    /// </summary>
    void IluminarReceptores(float delta)
    {
        // el ángulo del spot es la apertura total: el borde del cono está a la mitad
        float medio = AnguloActual * 0.5f;
        // 1. candidatos: todo collider de las capas de receptores dentro de una esfera del largo del alcance
        int cantidad = Physics.OverlapSphereNonAlloc(
            transform.position, AlcanceActual, buffer, capaReceptores, QueryTriggerInteraction.Collide);
        receptoresProcesados.Clear();

        for (int i = 0; i < cantidad; i++)
        {
            var receptor = buffer[i].GetComponentInParent<ReceptorDeLuz>();
            if (receptor == null || !receptor.isActiveAndEnabled || !receptoresProcesados.Add(receptor)) continue;

            // 2. ¿está dentro del cono? (distancia y ángulo respecto de hacia dónde apunta)
            Vector3 hacia = receptor.PuntoDeImpacto - transform.position;
            if (hacia.magnitude > AlcanceActual) continue;
            if (Vector3.Angle(transform.forward, hacia) > medio) continue;
            // las anclas sólo se encienden por la cara de los aros, no por la espalda de roca
            if (!receptor.DeFrenteA(transform.position)) continue;

            // 3. línea de vista: que no haya una pared en el medio, ni una reja sólida
            if (Physics.Raycast(transform.position, hacia.normalized, hacia.magnitude - 0.05f,
                                capaObstaculos, QueryTriggerInteraction.Ignore))
                continue;
            var reja = RejaEnElMedio(hacia, receptor);
            if (reja != null)
            {
                // se ve pero no le llega: el receptor titila y la reja avisa que es ella
                if (receptor.AceptaFiltro(FiltroActual))
                {
                    receptor.AvisarTapado();
                    reja.AvisarTapado();
                }
                continue;
            }

            // 4. se le avisa sólo si el filtro actual es el suyo
            HayObjetivo = true;
            if (!receptor.AceptaFiltro(FiltroActual))
            {
                receptor.AvisarFiltroIncompatible();
                continue;
            }
            ObjetivoAceptaFiltro = true;
            receptor.RecibirLuz(FiltroActual, delta);
            CargaObjetivo = Mathf.Max(CargaObjetivo, receptor.Carga);
        }
    }

    readonly RaycastHit[] impactosRejas = new RaycastHit[8];

    /// <summary>
    /// Las rejas de materia hueca están en la capa de receptores (no en la de obstáculos),
    /// pero mientras están sólidas tapan la luz: lo que está detrás no se ilumina hasta
    /// disolverlas. Disueltas, sus colliders se apagan y el rayo pasa.
    /// </summary>
    MateriaHueca RejaEnElMedio(Vector3 hacia, ReceptorDeLuz objetivo)
    {
        int n = Physics.RaycastNonAlloc(transform.position, hacia.normalized, impactosRejas, hacia.magnitude - 0.05f,
                                        capaReceptores, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var reja = impactosRejas[i].collider.GetComponentInParent<MateriaHueca>();
            if (reja != null && reja != objetivo && reja.Solido) return reja;
        }
        return null;
    }

    // en la vista Scene, una línea amarilla del largo del alcance
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * AlcanceActual);
    }
}
