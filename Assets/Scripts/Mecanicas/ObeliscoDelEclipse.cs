using UnityEngine;

/// <summary>
/// El obelisco del centro de la rotonda: el reloj del eclipse. En su cara oeste hay un sol y
/// en la este una luna, que se encienden solos con el sello de su ala (TestigoDeSello). En la
/// cara sur, mirando a la puerta de los sellos, un eclipse que se enciende cuando están los
/// dos: lo pide la cinemática del segundo sello (Encender), justo antes de abrir la puerta.
///
/// CÓMO FUNCIONA
/// Encender() arranca una subida de brillo de 1,5 s: la emisión del eclipse y su luz.
/// </summary>
public class ObeliscoDelEclipse : MonoBehaviour
{
    public Renderer eclipse;
    public Light luz;
    public Color colorEclipse = new Color(0.85f, 0.92f, 1f);
    public float emisionApagado = 0.03f;
    public float intensidadLuz = 30f;

    public bool Encendido { get; private set; }

    /// <summary>Hacia dónde mirar en la cinemática: el eclipse.</summary>
    public Vector3 Centro => eclipse != null ? eclipse.transform.position : transform.position;

    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock bloque;
    float brillo;

    void Start() => Aplicar();

    public void Encender() => Encendido = true;

    void Update()
    {
        if (!Encendido || brillo >= 1f) return;
        brillo = Mathf.MoveTowards(brillo, 1f, Time.deltaTime / 1.5f);
        Aplicar();
    }

    void Aplicar()
    {
        if (eclipse != null)
        {
            bloque ??= new MaterialPropertyBlock();
            eclipse.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, colorEclipse * Mathf.Lerp(emisionApagado, 3f, brillo));
            eclipse.SetPropertyBlock(bloque);
        }
        if (luz != null) { luz.enabled = brillo > 0.01f; luz.intensity = intensidadLuz * brillo; }
    }
}
