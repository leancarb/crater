using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Núcleo del juego. Va en un GameObject hijo de la cámara, con un Spot Light.
///
/// Reglas:
///  - F enciende y apaga.
///  - 1 / 2 equipan CUERPO y HUECO. Sólo uno a la vez. Cambiar tarda 'demoraDeCambio'.
///  - Q vuelve a la luz blanca.
///  - Cada frame busca los receptores dentro del cono y les avisa que están iluminados.
///
/// CÓMO FUNCIONA
///  1. Lee la entrada (EntradaCrater) y prende/apaga o cambia de filtro.
///  2. Aplica el filtro al Spot Light: color, ángulo, alcance e intensidad.
///  3. Si está encendida, busca receptores con OverlapSphere (una esfera del largo
///     del alcance), descarta los que quedan fuera del ángulo del cono y los que
///     tienen una pared en el medio (Raycast), y a los que quedan les llama RecibirLuz().
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

    [Header("Filtros (en orden de tecla: 1, 2)")]
    public List<FiltroDefinicion> filtros = new List<FiltroDefinicion>();
    [Tooltip("Filtros que el jugador ya encontró. Empieza vacío.")]
    public List<FiltroDefinicion> filtrosDesbloqueados = new List<FiltroDefinicion>();

    [Header("Luz blanca (sin filtro)")]
    public Color colorBase = new Color(1f, 0.95f, 0.86f);
    public float anguloBase = 30f;
    public float alcanceBase = 14f;
    public float intensidadBase = 800f;

    [Header("Ajustes")]
    public float demoraDeCambio = 0.6f;
    [Tooltip("Si está tildado, la linterna no responde hasta llamar a Recoger().")]
    public bool requiereRecogerla = true;

    [Header("Sonido")]
    public AudioClip sonidoEncender;
    public AudioClip sonidoApagar;

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
    AudioSource audioSource;
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
        if (!PausaCrater.EnPausa) LeerEntrada();

        // se recalculan en IluminarReceptores
        HayObjetivo = false;
        ObjetivoAceptaFiltro = false;
        CargaObjetivo = 0f;

        if (CambiandoFiltro && Time.time >= finDelCambio)
            CambiandoFiltro = false;

        // durante el cambio de filtro la luz baja casi a cero: el cambio se ve y se siente
        if (spot != null)
        {
            // MoveTowards: la intensidad se acerca al objetivo de a poco, sin saltos
            float objetivo = CambiandoFiltro ? IntensidadActual * 0.08f : IntensidadActual;
            spot.intensity = Mathf.MoveTowards(spot.intensity, objetivo, IntensidadActual * 6f * Time.deltaTime);
        }

        if (Encendida && !CambiandoFiltro)
            IluminarReceptores(Time.deltaTime);
    }

    void LeerEntrada()
    {
        if (!Disponible) return;   // todavía no la encontró

        if (EntradaCrater.Linterna)
            Encender(!Encendida);

        for (int i = 0; i < Mathf.Min(2, filtros.Count); i++)
            if (EntradaCrater.Filtro(i)) IntentarEquipar(i);

        if (EntradaCrater.QuitarFiltro)
            EquiparFiltro(null);
    }

    /// <summary>Tecla 1 o 2: equipa ese filtro si ya lo encontró; si ya estaba puesto, vuelve a luz blanca.</summary>
    void IntentarEquipar(int indice)
    {
        if (indice < 0 || indice >= filtros.Count) return;
        var f = filtros[indice];
        if (!filtrosDesbloqueados.Contains(f)) return;   // todavía no lo encontró
        EquiparFiltro(FiltroActual == f ? null : f);
    }

    /// <summary>Cambia el filtro. Sólo uno a la vez, con demora. Null = luz blanca.</summary>
    public void EquiparFiltro(FiltroDefinicion f)
    {
        if (CambiandoFiltro || f == FiltroActual) return;
        CambiandoFiltro = true;
        finDelCambio = Time.time + demoraDeCambio;
        AplicarFiltro(f);
        AlEquiparFiltro?.Invoke(f);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) return;
        if (f != null && f.sonidoAlEquipar != null) audioSource.PlayOneShot(f.sonidoAlEquipar);
        audioSource.clip = f != null ? f.zumbido : null;
        ActualizarZumbido();
    }

    /// <summary>Copia los valores del filtro (o de la luz blanca) al Spot Light.</summary>
    void AplicarFiltro(FiltroDefinicion f)
    {
        FiltroActual = f;
        if (spot == null) return;
        spot.color = ColorActual;
        spot.spotAngle = AnguloActual;
        spot.innerSpotAngle = AnguloActual * 0.55f;
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

    /// <summary>El zumbido del filtro suena en bucle sólo con la linterna encendida.</summary>
    void ActualizarZumbido()
    {
        audioSource.loop = true;
        if (Encendida && audioSource.clip != null)
        {
            if (!audioSource.isPlaying) audioSource.Play();
        }
        else if (audioSource.isPlaying) audioSource.Stop();
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

            // 3. línea de vista: que no haya una pared en el medio
            if (Physics.Raycast(transform.position, hacia.normalized, hacia.magnitude - 0.05f,
                                capaObstaculos, QueryTriggerInteraction.Ignore))
                continue;

            // 4. se le avisa sólo si el filtro actual es el suyo
            HayObjetivo = true;
            if (!receptor.AceptaFiltro(FiltroActual)) continue;
            ObjetivoAceptaFiltro = true;
            receptor.RecibirLuz(FiltroActual, delta);
            CargaObjetivo = Mathf.Max(CargaObjetivo, receptor.Carga);
        }
    }

    // en la vista Scene, una línea amarilla del largo del alcance
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * AlcanceActual);
    }
}
