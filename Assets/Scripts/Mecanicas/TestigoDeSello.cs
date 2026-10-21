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
    [Tooltip("Segundo foco de la columna, integrado en el techo y sincronizado con el piso.")]
    public Light luzSuperior;
    public float intensidadLuzSuperior = 45f;

    [Tooltip("El obelisco espera el regreso a la rotonda y llama a Encender.") ]
    public bool manual;
    bool activado;
    public bool Encendido => manual ? activado : sello != null && sello.Activo;
    public float Nivel => nivel;
    public void Encender() => activado = true;

    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock bloque;
    float nivel;

    void Update()
    {
        float objetivo = Encendido ? 1f : 0f;
        if (nivel == objetivo) return; // Start ya dibujó el reposo; sólo cambiar durante la transición.
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
        if(luzSuperior!=null)
        {
            luzSuperior.intensity=intensidadLuzSuperior*nivel;
            luzSuperior.enabled=nivel>.01f;
        }
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, c);
            if (manual) bloque.SetColor("_BaseColor", colorEncendido * Mathf.Lerp(.08f, .7f, nivel));
            r.SetPropertyBlock(bloque);
        }
    }
}
