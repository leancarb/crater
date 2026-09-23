using UnityEngine;

/// <summary>
/// Objeto que el jugador junta al pasar: la linterna misma (si 'filtro' está
/// vacío) o uno de sus filtros. Flota, gira y brilla con el color del filtro.
///
/// Poner en: un objeto con un Collider marcado como Is Trigger.
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
    public float flotacion = 0.08f;
    public float giro = 35f;

    [Header("Sonido")]
    public AudioSource sonido;

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    public bool Recogido { get; private set; }

    Vector3 posicionBase;
    float intensidadBase;
    float fase;

    void Awake()
    {
        if (visual != null) posicionBase = visual.localPosition;
        if (brillo != null) intensidadBase = brillo.intensity;
        fase = transform.position.x * 1.3f + transform.position.z * 0.7f;
        AplicarColorDelFiltro();
    }

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
            visual.localPosition = posicionBase + Vector3.up * (Mathf.Sin(t * 1.6f) * flotacion);
            visual.Rotate(0f, giro * Time.deltaTime, 0f, Space.World);
        }
        if (brillo != null) brillo.intensity = intensidadBase * (0.8f + 0.2f * Mathf.Sin(t * 2.3f));
    }

    void OnTriggerEnter(Collider other)
    {
        if (!Recogido && other.CompareTag("Player"))
            Recoger(LinternaController.Instancia);
    }

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
