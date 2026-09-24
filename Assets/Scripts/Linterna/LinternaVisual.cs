using UnityEngine;

/// <summary>
/// La linterna en la mano. Aparece al recogerla, se balancea con la mirada,
/// baja durante el cambio de filtro y tiñe la lente con el color del haz.
///
/// Poner en: un hijo de la cámara, con el modelo de la linterna adentro.
///
/// CÓMO FUNCIONA
/// Es sólo visual: no afecta al juego. Cada frame lee el estado de LinternaController
/// (si está disponible, encendida, cambiando de filtro) y acomoda el modelo 3D.
/// Corre en LateUpdate para moverse después de que la cámara ya giró en ese frame.
/// </summary>
public class LinternaVisual : MonoBehaviour
{
    public LinternaController linterna;
    public Transform modelo;
    [Tooltip("Renderers de la lente, se tiñen con el color del haz.")]
    public Renderer[] lentes;

    [Header("Pose")]
    // posición del modelo respecto de la cámara: abajo a la derecha, adelante
    public Vector3 posicionReposo = new Vector3(0.23f, -0.24f, 0.42f);
    // cuánto se desplaza el modelo cuando el jugador mueve la mirada (inercia)
    public float balanceo = 0.0018f;
    // qué tan rápido vuelve a la pose (más alto = más rígido)
    public float suavizado = 10f;

    // pedir el id de una propiedad del shader una sola vez es más rápido que usar el texto cada frame
    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    // MaterialPropertyBlock cambia colores de un renderer sin crear una copia del material
    MaterialPropertyBlock bloque;
    Quaternion rotacionBase;   // rotación original del modelo, sobre la que se suma la inclinación
    Vector3 desvio;            // balanceo actual (suavizado)
    float bajada = 1f;         // 0 = en posición, 1 = bajada (durante el cambio de filtro)

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        if (modelo != null) rotacionBase = modelo.localRotation;
    }

    void LateUpdate()
    {
        if (linterna == null || modelo == null) return;

        // sin linterna recogida, el modelo no se ve
        bool visible = linterna.Disponible;
        if (modelo.gameObject.activeSelf != visible) modelo.gameObject.SetActive(visible);
        if (!visible) return;

        float delta = Time.deltaTime;

        // balanceo: el modelo se corre al revés de hacia donde se mira, como si tuviera peso
        Vector2 mirada = PausaCrater.EnPausa ? Vector2.zero : EntradaCrater.Mirada();
        Vector3 objetivoDesvio = Vector3.ClampMagnitude(new Vector3(-mirada.x, -mirada.y, 0f) * balanceo, 0.04f);

        // 1 - e^(-s·dt) da un suavizado que se siente igual a cualquier cantidad de FPS
        float k = 1f - Mathf.Exp(-suavizado * delta);
        desvio = Vector3.Lerp(desvio, objetivoDesvio, k);
        bajada = Mathf.Lerp(bajada, linterna.CambiandoFiltro ? 1f : 0f, k * 0.8f);

        // durante el cambio de filtro baja un poco y se inclina, como si se estuviera cambiando la lente
        modelo.localPosition = posicionReposo + desvio + new Vector3(0f, -0.14f, -0.05f) * bajada;
        modelo.localRotation = Quaternion.Euler(bajada * 28f, 0f, bajada * -12f) * rotacionBase;

        // la lente toma el color del haz y brilla (emisión) sólo con la luz encendida
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
