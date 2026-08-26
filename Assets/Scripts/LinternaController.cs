using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nucleo del juego. Va en un GameObject hijo de la camara, con un Spot Light.
///
/// Reglas:
///  - F enciende y apaga.
///  - 1 / 2 / 3 equipan filtros. Solo uno a la vez. Cambiar tarda 'demoraDeCambio'.
///  - Cada frame busca los receptores dentro del cono y les avisa que estan iluminados.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class LinternaController : MonoBehaviour
{
    public static LinternaController Instancia { get; private set; }

    [Header("Referencias")]
    public Light spot;
    [Tooltip("Solo los objetos en estas capas pueden recibir el haz.")]
    public LayerMask capaReceptores = ~0;
    [Tooltip("Capas que bloquean el haz (paredes, piso). Dejar sin los receptores.")]
    public LayerMask capaObstaculos;

    [Header("Filtros disponibles (arrastrar los ScriptableObject)")]
    public List<FiltroDefinicion> filtros = new List<FiltroDefinicion>();
    [Tooltip("Filtros que el jugador ya encontro. Empieza vacio.")]
    public List<FiltroDefinicion> filtrosDesbloqueados = new List<FiltroDefinicion>();

    [Header("Sin filtro")]
    public Color colorBase = new Color(1f, 0.96f, 0.88f);
    public float anguloBase = 28f;
    public float alcanceBase = 14f;
    public float intensidadBase = 2.5f;

    [Header("Ajustes")]
    public float demoraDeCambio = 0.8f;
    public KeyCode teclaEncender = KeyCode.F;

    // --- estado ---
    public bool Encendida { get; private set; }
    public FiltroDefinicion FiltroActual { get; private set; }
    public bool CambiandoFiltro { get; private set; }

    float finDelCambio;
    AudioSource audioSource;
    readonly Collider[] buffer = new Collider[32];

    void Awake()
    {
        Instancia = this;
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        if (spot == null) spot = GetComponentInChildren<Light>();
        AplicarFiltro(null);
        Encender(false);
    }

    void Update()
    {
        LeerInput();

        if (CambiandoFiltro && Time.time >= finDelCambio)
            CambiandoFiltro = false;

        if (Encendida && !CambiandoFiltro)
            IluminarReceptores();
    }

    void LeerInput()
    {
        if (Input.GetKeyDown(teclaEncender))
            Encender(!Encendida);

        for (int i = 0; i < 3; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                IntentarEquipar(i);
        }

        // Q saca el filtro y vuelve a luz limpia.
        if (Input.GetKeyDown(KeyCode.Q))
            EquiparFiltro(null);
    }

    void IntentarEquipar(int indice)
    {
        if (indice < 0 || indice >= filtros.Count) return;
        var f = filtros[indice];
        if (!filtrosDesbloqueados.Contains(f)) return;   // todavia no lo encontro
        if (FiltroActual == f) { EquiparFiltro(null); return; }
        EquiparFiltro(f);
    }

    /// <summary>Cambia el filtro. Solo uno a la vez, con demora.</summary>
    public void EquiparFiltro(FiltroDefinicion f)
    {
        if (CambiandoFiltro) return;
        CambiandoFiltro = true;
        finDelCambio = Time.time + demoraDeCambio;
        AplicarFiltro(f);

        if (f != null && f.sonidoAlEquipar != null)
            AudioSource.PlayClipAtPoint(f.sonidoAlEquipar, transform.position);

        audioSource.clip = (f != null) ? f.zumbido : null;
        if (Encendida && audioSource.clip != null) audioSource.Play();
        else audioSource.Stop();
    }

    void AplicarFiltro(FiltroDefinicion f)
    {
        FiltroActual = f;
        if (spot == null) return;

        spot.color      = (f != null) ? f.color        : colorBase;
        spot.spotAngle  = (f != null) ? f.anguloCono   : anguloBase;
        spot.range      = (f != null) ? f.alcance      : alcanceBase;
        spot.intensity  = (f != null) ? f.intensidad   : intensidadBase;
    }

    public void Encender(bool valor)
    {
        Encendida = valor;
        if (spot != null) spot.enabled = valor;
        if (valor && audioSource.clip != null) audioSource.Play();
        else audioSource.Stop();
    }

    /// <summary>Llamar desde un trigger cuando el jugador encuentra un filtro.</summary>
    public void Desbloquear(FiltroDefinicion f)
    {
        if (f != null && !filtrosDesbloqueados.Contains(f))
            filtrosDesbloqueados.Add(f);
    }

    /// <summary>
    /// Busca todos los receptores dentro del cono y con linea de vista libre,
    /// y les avisa. Soporta varios receptores a la vez (necesario para los puentes).
    /// </summary>
    void IluminarReceptores()
    {
        float alcance = (FiltroActual != null) ? FiltroActual.alcance : alcanceBase;
        float angulo  = (FiltroActual != null) ? FiltroActual.anguloCono : anguloBase;
        float medio   = angulo * 0.5f;

        int cantidad = Physics.OverlapSphereNonAlloc(
            transform.position, alcance, buffer, capaReceptores, QueryTriggerInteraction.Collide);

        for (int i = 0; i < cantidad; i++)
        {
            var receptor = buffer[i].GetComponentInParent<ReceptorDeLuz>();
            if (receptor == null) continue;

            Vector3 hacia = receptor.PuntoDeImpacto - transform.position;
            if (Vector3.Angle(transform.forward, hacia) > medio) continue;

            // linea de vista: que no haya una pared en el medio
            if (Physics.Raycast(transform.position, hacia.normalized, hacia.magnitude - 0.05f,
                                capaObstaculos, QueryTriggerInteraction.Ignore))
                continue;

            receptor.RecibirLuz(FiltroActual, Time.deltaTime);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float a = (FiltroActual != null) ? FiltroActual.alcance : alcanceBase;
        Gizmos.DrawRay(transform.position, transform.forward * a);
    }
}
