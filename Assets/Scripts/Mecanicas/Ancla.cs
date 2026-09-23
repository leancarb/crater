using UnityEngine;

/// <summary>
/// FILTRO I · CUERPO.
/// Nodo de basalto que se enciende con el haz. Las anclas encendidas sostienen
/// un PuenteLuz o abren una Compuerta. Mientras dura la retención, los anillos
/// laten cada vez más rápido: el jugador sabe cuánto tiempo le queda.
///
/// Poner en: un objeto con Collider (trigger) en la capa "Ancla".
/// </summary>
public class Ancla : ReceptorDeLuz
{
    [Header("Aspecto")]
    [Tooltip("Piezas que brillan (anillos y núcleo).")]
    public Renderer[] acentos;
    public Light brillo;
    public Color colorApagada = new Color(0.12f, 0.07f, 0.04f);
    public Color colorEncendida = new Color(1f, 0.42f, 0.1f);
    public float emisionMaxima = 2.6f;
    public float intensidadLuz = 4f;

    [Header("Sonido")]
    [Tooltip("Tono que suena al encenderse. Afinar cada ancla distinto para armar un acorde.")]
    public AudioSource tono;

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    MaterialPropertyBlock bloque;

    /// <summary>Brillo visible de 0 a 1 (carga, o retención cuando ya perdió el haz).</summary>
    public float Nivel { get; private set; }

    void Start() => AlActualizar(0f);

    protected override void AlActualizar(float delta)
    {
        float nivel = Carga;
        if (Activo && !Recibiendo && retencion > 0f)
        {
            float r = RetencionRestante;
            float frecuencia = Mathf.Lerp(16f, 3f, r);
            float pulso = 0.5f + 0.5f * Mathf.Cos(Time.time * frecuencia);
            nivel = Mathf.Lerp(0.35f, 1f, r) * Mathf.Lerp(0.55f, 1f, r > 0.6f ? 1f : pulso);
        }
        Nivel = nivel;

        bloque ??= new MaterialPropertyBlock();
        Color c = Color.Lerp(colorApagada, colorEncendida, nivel);
        Color emision = colorEncendida * (0.04f + emisionMaxima * nivel * nivel);
        if (acentos != null)
        {
            foreach (var r in acentos)
            {
                if (r == null) continue;
                r.GetPropertyBlock(bloque);
                bloque.SetColor(IdBase, c);
                bloque.SetColor(IdEmision, emision);
                r.SetPropertyBlock(bloque);
            }
        }

        if (brillo != null)
        {
            brillo.intensity = intensidadLuz * nivel;
            brillo.enabled = nivel > 0.01f;
        }
    }

    protected override void Activar()
    {
        base.Activar();
        if (tono != null) tono.Play();
    }
}
