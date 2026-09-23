using UnityEngine;

/// <summary>
/// La linterna en la mano. Aparece al recogerla, se balancea con la mirada,
/// baja durante el cambio de filtro y tiñe la lente con el color del haz.
///
/// Poner en: un hijo de la cámara, con el modelo de la linterna adentro.
/// </summary>
public class LinternaVisual : MonoBehaviour
{
    public LinternaController linterna;
    public Transform modelo;
    [Tooltip("Renderers de la lente, se tiñen con el color del haz.")]
    public Renderer[] lentes;

    [Header("Pose")]
    public Vector3 posicionReposo = new Vector3(0.23f, -0.24f, 0.42f);
    public float balanceo = 0.0018f;
    public float suavizado = 10f;

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    MaterialPropertyBlock bloque;
    Quaternion rotacionBase;
    Vector3 desvio;
    float bajada = 1f;

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        if (modelo != null) rotacionBase = modelo.localRotation;
    }

    void LateUpdate()
    {
        if (linterna == null || modelo == null) return;

        bool visible = linterna.Disponible;
        if (modelo.gameObject.activeSelf != visible) modelo.gameObject.SetActive(visible);
        if (!visible) return;

        float delta = Time.deltaTime;
        Vector2 mirada = PausaCrater.EnPausa ? Vector2.zero : EntradaCrater.Mirada();
        Vector3 objetivoDesvio = Vector3.ClampMagnitude(new Vector3(-mirada.x, -mirada.y, 0f) * balanceo, 0.04f);
        float k = 1f - Mathf.Exp(-suavizado * delta);
        desvio = Vector3.Lerp(desvio, objetivoDesvio, k);
        bajada = Mathf.Lerp(bajada, linterna.CambiandoFiltro ? 1f : 0f, k * 0.8f);

        modelo.localPosition = posicionReposo + desvio + new Vector3(0f, -0.14f, -0.05f) * bajada;
        modelo.localRotation = Quaternion.Euler(bajada * 28f, 0f, bajada * -12f) * rotacionBase;

        Color c = linterna.Encendida ? linterna.ColorActual : new Color(0.05f, 0.04f, 0.03f);
        float brillo = linterna.Encendida && !linterna.CambiandoFiltro ? 3f : 0f;
        foreach (var lente in lentes)
        {
            if (lente == null) continue;
            lente.GetPropertyBlock(bloque);
            bloque.SetColor(IdBase, c);
            bloque.SetColor(IdEmision, c * brillo);
            lente.SetPropertyBlock(bloque);
        }
    }
}
