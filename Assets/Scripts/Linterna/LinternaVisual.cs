using UnityEngine;

/// <summary>
/// La linterna en la mano: aparece al recogerla y la lente toma el color del haz.
/// Queda quieta en su lugar (sin balanceo ni animación al cambiar de filtro).
///
/// Poner en: un hijo de la cámara, con el modelo de la linterna adentro.
///
/// CÓMO FUNCIONA
/// Es sólo visual: no afecta al juego. Cada frame lee el estado de LinternaController
/// (si está disponible y encendida) y pinta la lente con un MaterialPropertyBlock,
/// que cambia colores de un renderer sin crear una copia del material.
/// </summary>
public class LinternaVisual : MonoBehaviour
{
    public LinternaController linterna;
    public Transform modelo;
    [Tooltip("Renderers de la lente, se tiñen con el color del haz.")]
    public Renderer[] lentes;
    [Tooltip("Posición del modelo respecto de la cámara: abajo a la derecha, adelante.")]
    public Vector3 posicionReposo = new Vector3(0.23f, -0.24f, 0.42f);

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    MaterialPropertyBlock bloque;

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        if (modelo != null) modelo.localPosition = posicionReposo;
    }

    void LateUpdate()
    {
        if (linterna == null || modelo == null) return;

        // sin linterna recogida, el modelo no se ve
        bool visible = linterna.Disponible;
        if (modelo.gameObject.activeSelf != visible) modelo.gameObject.SetActive(visible);
        if (!visible) return;

        // la lente toma el color del haz y brilla (emisión) sólo con la luz encendida
        Color c = linterna.Encendida ? linterna.ColorActual : new Color(0.05f, 0.04f, 0.03f);
        float brillo = linterna.Encendida ? 3f : 0f;
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
