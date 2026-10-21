using UnityEngine;

/// <summary>
/// Objeto que el jugador junta al mirar de cerca y confirmar: la linterna misma (si 'filtro' está
/// vacío) o uno de sus filtros. Flota, gira y brilla con el color del filtro.
///
/// Poner en: un objeto con un Collider marcado como Is Trigger.
///
/// CÓMO FUNCIONA
/// ColeccionPiedras comprueba mirada, alcance y acción de recoger. Este componente
/// incorpora la linterna o desbloquea el filtro, confirma con sonido y oculta el objeto.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Recogible : MonoBehaviour
{
    [Tooltip("Vacío = es la linterna misma (El Umbral).")]
    public FiltroDefinicion filtro;

    [Header("Aspecto")]
    public Transform visual;
    [Tooltip("Renderers que toman el color del filtro.")]
    public Renderer[] tintables;
    public Light brillo;
    public float flotacion = 0.08f;   // metros que sube y baja
    public float giro = 35f;          // grados por segundo

    [Header("Sonido")]
    public AudioSource sonido;

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    public bool Recogido { get; private set; }

    Vector3 posicionBase;
    float intensidadBase;
    float fase;   // desfase para que dos recogibles no floten sincronizados

    void Awake()
    {
        if (visual != null) posicionBase = visual.localPosition;
        if (brillo != null) intensidadBase = brillo.intensity;
        // la fase sale de la posición: distinta para cada objeto, pero siempre la misma
        fase = transform.position.x * 1.3f + transform.position.z * 0.7f;
        AplicarColorDelFiltro();
    }

    /// <summary>El mismo prefab sirve para cualquier filtro: se tiñe con el color del asset.</summary>
    void AplicarColorDelFiltro()
    {
        if (filtro == null || tintables == null) return;
        var bloque = new MaterialPropertyBlock();
        foreach (var r in tintables)
        {
            if (r == null) continue;
            r.GetPropertyBlock(bloque);
            bloque.SetColor(IdBase, filtro.color);
            bloque.SetColor(IdEmision, filtro.color * 3f);
            r.SetPropertyBlock(bloque);
        }
        if (brillo != null) brillo.color = filtro.color;
    }

    void Update()
    {
        if (Recogido) return;
        float t = Time.time + fase;
        if (visual != null)
        {
            // flota con una onda seno y gira sobre el eje vertical del mundo
            visual.localPosition = posicionBase + Vector3.up * (Mathf.Sin(t * 1.6f) * flotacion);
            visual.Rotate(0f, giro * Time.deltaTime, 0f, Space.World);
        }
        // el brillo "respira" entre 80 % y 100 %
        if (brillo != null) brillo.intensity = intensidadBase * (0.8f + 0.2f * Mathf.Sin(t * 2.3f));
    }

    // La interacción la arbitra ColeccionPiedras, igual que las piedras talladas.
    // Pasar por el trigger ya no recoge el objeto sin que el jugador lo advierta.
    public Transform PuntoInteraccion => visual != null ? visual : transform;
    public string NombreInteraccion => filtro == null ? "farol" : "filtro " + filtro.nombreVisible;

    /// <summary>Devuelve true si lo recogió (false si ya estaba recogido o no hay linterna).</summary>
    public bool Recoger(LinternaController linterna)
    {
        if (Recogido || linterna == null) return false;

        if (filtro == null) linterna.Recoger();
        else linterna.Desbloquear(filtro);

        Recogido = true;
        if (sonido != null) sonido.Play();
        if (visual != null) visual.gameObject.SetActive(false);
        if (brillo != null) brillo.enabled = false;
        GetComponent<Collider>().enabled = false;
        return true;
    }
}

