using UnityEngine;

/// <summary>
/// Objeto que el jugador junta al pasar: la linterna misma (si 'filtro' está
/// vacío) o uno de sus filtros. Está quieto y brilla con el color del filtro.
///
/// Poner en: un objeto con un Collider marcado como Is Trigger.
///
/// CÓMO FUNCIONA
/// Cuando el jugador (tag "Player") entra al trigger, Unity llama a OnTriggerEnter.
/// Ahí se le avisa a la linterna: Recoger() si es la linterna, Desbloquear() si es
/// un filtro. La linterna dispara sus eventos y el FlujoJuegoCrater avanza de
/// etapa. Después el objeto se esconde y apaga su collider.
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

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    public bool Recogido { get; private set; }

    void Awake() => AplicarColorDelFiltro();

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

    void OnTriggerEnter(Collider other)
    {
        if (!Recogido && other.CompareTag("Player"))
            Recoger(LinternaController.Instancia);
    }

    /// <summary>Devuelve true si lo recogió (false si ya estaba recogido o no hay linterna).</summary>
    public bool Recoger(LinternaController linterna)
    {
        if (Recogido || linterna == null) return false;

        if (filtro == null) linterna.Recoger();
        else linterna.Desbloquear(filtro);

        Recogido = true;
        if (visual != null) visual.gameObject.SetActive(false);
        if (brillo != null) brillo.enabled = false;
        GetComponent<Collider>().enabled = false;
        return true;
    }
}
