using UnityEngine;

/// <summary>
/// Un tallado junto a la puerta de los sellos que se enciende cuando su sello
/// (lejos, al final de un ala) está encendido. Así, desde la rotonda, se sabe
/// cuántos sellos faltan.
///
/// CÓMO FUNCIONA
/// Cada frame mira si el receptor 'sello' está activo y lleva el brillo de los
/// renderers hacia el color encendido o apagado (con MaterialPropertyBlock, sin
/// tocar el material compartido).
/// </summary>
public class TestigoDeSello : MonoBehaviour
{
    public ReceptorDeLuz sello;
    public Renderer[] renderers;
    public Color colorEncendido = new Color(1f, 0.42f, 0.1f);
    public float emisionEncendido = 3.5f;
    public float emisionApagado = 0.15f;
    [Tooltip("Segundos para pasar de apagado a encendido.")]
    public float transicion = 1.5f;
    [Tooltip("Opcional: una luz que se prende con el sello, para verlo de lejos.")]
    public Light luz;
    public float intensidadLuz = 30f;

    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock bloque;
    float nivel;

    void Update()
    {
        float objetivo = sello != null && sello.Activo ? 1f : 0f;
        nivel = Mathf.MoveTowards(nivel, objetivo, Time.deltaTime / Mathf.Max(0.01f, transicion));
        Aplicar();
    }

    void Start() => Aplicar();

    void Aplicar()
    {
        if (renderers == null) return;
        bloque ??= new MaterialPropertyBlock();
        Color c = colorEncendido * Mathf.Lerp(emisionApagado, emisionEncendido, nivel);
        if (luz != null)
        {
            luz.intensity = intensidadLuz * nivel;
            luz.enabled = nivel > 0.01f;
        }
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, c);
            r.SetPropertyBlock(bloque);
        }
    }
}
