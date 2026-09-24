using UnityEngine;

/// <summary>
/// La puerta del fondo de la Cresta, que sólo existe en la oscuridad.
///
/// - Con la linterna prendida no se ve: la pared sólo devuelve el reflejo del foco.
/// - Apagada: aparece un contorno tenue, que crece con la adaptación.
/// - Al completar la adaptación (EclipseFinalController llama a Abrir) la hoja
///   desaparece y queda el paso abierto hacia la luz del eclipse.
///
/// CÓMO FUNCIONA
/// Cada frame calcula una 'Visibilidad' objetivo según la linterna y la adaptación, se
/// acerca a ella de a poco y la usa como brillo (emisión) del contorno. La 'hoja' es un
/// bloque con el mismo material de la pared, con collider: al abrir se desactiva y deja pasar.
/// </summary>
public class PuertaEclipse : MonoBehaviour
{
    [Header("Partes")]
    [Tooltip("Tapa el hueco con el material de la pared. Tiene el collider.")]
    [SerializeField] GameObject hoja;
    [SerializeField] Renderer[] contorno;
    [SerializeField] AdaptacionOscuridad adaptacion;

    [Header("Aspecto")]
    [SerializeField] Color colorPuerta = new Color(0.75f, 0.85f, 1f);
    [SerializeField] float brilloMaximo = 2.2f;
    [Tooltip("Lo que se ve apenas se apaga la linterna, antes de adaptarse.")]
    [Range(0f, 1f)] [SerializeField] float bordeMinimo = 0.1f;
    [SerializeField] float velocidadAparicion = 0.8f;
    [SerializeField] float velocidadDesaparicion = 5f;

    [Header("Sonido")]
    [Tooltip("El viento del óculo sale de la puerta: sube con la adaptación.")]
    [SerializeField] AudioSource viento;
    [SerializeField] float volumenViento = 0.7f;
    [SerializeField] AudioSource sonidoAbrir;

    public bool Abierta { get; private set; }
    public float Visibilidad { get; private set; }

    MaterialPropertyBlock bloque;
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        AplicarContorno(0f);
    }

    void Update()
    {
        var linterna = LinternaController.Instancia;
        bool luz = linterna != null && linterna.Encendida;
        float adaptado = adaptacion != null && adaptacion.Habilitada ? adaptacion.Curva : 0f;
        bool habilitada = adaptacion != null && adaptacion.Habilitada;

        // qué tan visible debería estar el contorno
        float objetivo;
        if (Abierta) objetivo = 1f;
        else if (!habilitada || luz) objetivo = 0f;
        else objetivo = Mathf.Max(bordeMinimo, adaptado);

        // aparece lento (como los ojos adaptándose) y se esconde rápido al prender la luz
        float velocidad = objetivo > Visibilidad ? velocidadAparicion : velocidadDesaparicion;
        Visibilidad = Mathf.MoveTowards(Visibilidad, objetivo, velocidad * Time.deltaTime);
        AplicarContorno(Visibilidad);

        if (viento != null)
        {
            if (habilitada && !viento.isPlaying) viento.Play();
            viento.volume = volumenViento * (Abierta ? 1f : adaptado);
        }
    }

    public void Abrir()
    {
        if (Abierta) return;
        Abierta = true;
        if (hoja != null) hoja.SetActive(false);
        if (sonidoAbrir != null) sonidoAbrir.Play();
    }

    void AplicarContorno(float v)
    {
        if (contorno == null) return;
        foreach (var r in contorno)
        {
            if (r == null) continue;
            r.enabled = v > 0.001f;
            r.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, colorPuerta * (brilloMaximo * v));
            r.SetPropertyBlock(bloque);
        }
    }
}
