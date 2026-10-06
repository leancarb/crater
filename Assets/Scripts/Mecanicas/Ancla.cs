using UnityEngine;

/// <summary>
/// FILTRO I · CUERPO.
/// Nodo de basalto que se enciende con el haz. Las anclas encendidas sostienen
/// un PuenteLuz o abren una Compuerta. Mientras dura la retención, los anillos
/// laten cada vez más rápido: el jugador sabe cuánto tiempo le queda.
///
/// Poner en: un objeto con Collider (trigger) en la capa "Ancla".
///
/// CÓMO FUNCIONA
/// Toda la lógica de carga y retención la hereda de ReceptorDeLuz. Esta clase sólo
/// agrega lo visual (color y brillo de los anillos, luz puntual) y el tono al encenderse.
/// El puente o la compuerta que depende de ella no la escucha: le pregunta cada
/// frame si está 'Activo'.
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
    public AudioSource resonancia;
    float ultimaActivacion = -10f;
    readonly System.Collections.Generic.Dictionary<Transform, Vector3> escalas = new System.Collections.Generic.Dictionary<Transform, Vector3>();
    float vibracion;

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    MaterialPropertyBlock bloque;

    /// <summary>Brillo visible de 0 a 1 (carga, o retención cuando ya perdió el haz).</summary>
    public float Nivel { get; private set; }

    // dibuja el estado inicial (apagada) antes del primer frame
    void Start()
    {
        if (acentos != null)
            foreach (var r in acentos) if (r != null && !escalas.ContainsKey(r.transform)) escalas.Add(r.transform, r.transform.localScale);
        AlActualizar(0f);
    }

    protected override void AlActualizar(float delta)
    {
        // mientras se carga, el brillo acompaña a la carga
        float nivel = Carga;

        // activa pero sin luz (en retención): late cada vez más rápido a medida que se agota
        if (Activo && !Recibiendo && retencion > 0f)
        {
            float r = RetencionRestante;                     // 1 = recién perdió la luz, 0 = se apaga
            float frecuencia = Mathf.Lerp(6f, 2f, r);       // late lento al principio, rápido al final
            float pulso = 0.5f + 0.5f * Mathf.Cos(Time.time * frecuencia);
            // el primer 40 % de la retención no late: sólo baja un poco el brillo
            nivel = Mathf.Lerp(0.35f, 1f, r) * Mathf.Lerp(0.55f, 1f, r > 0.6f ? 1f : pulso);
        }
        // tapada por una reja: la linterna la ve pero la luz no llega. Titila apagada
        if (!Activo && Tapado)
            nivel = Mathf.Max(nivel, 0.08f + 0.04f * Mathf.Sin(Time.time * 4f));
        Nivel = Mathf.MoveTowards(Nivel, nivel, delta * (nivel > Nivel ? 8f : 3f));
        nivel = Nivel;
        vibracion = Mathf.MoveTowards(vibracion, Recibiendo ? Carga : 0, delta * 4);
        foreach (var e in escalas)
            if (e.Key != null) e.Key.localScale = e.Value * (1 + Mathf.Sin(Time.time * 24) * vibracion * 0.003f);
        if (resonancia != null)
        {
            float volumen = Recibiendo ? 0.045f * Mathf.Sqrt(Carga) : 0;
            resonancia.volume = Mathf.MoveTowards(resonancia.volume, volumen, delta * 0.13f);
            if (resonancia.volume > 0.001f && !resonancia.isPlaying) resonancia.Play();
            if (resonancia.volume <= 0.001f && resonancia.isPlaying) resonancia.Stop();
        }

        // ??= crea el bloque la primera vez (AlActualizar puede llamarse antes que Awake de otros)
        bloque ??= new MaterialPropertyBlock();
        Color c = Color.Lerp(colorApagada, colorEncendida, nivel);
        // nivel² hace que el brillo arranque suave y se dispare al final de la carga
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

        // la luz puntual ilumina el entorno del ancla; apagada no gasta rendimiento
        if (brillo != null)
        {
            brillo.intensity = intensidadLuz * nivel;
            brillo.enabled = nivel > 0.01f;
        }
    }

    protected override void Activar()
    {
        base.Activar();   // lo de ReceptorDeLuz: Activo = true y el evento
        if (tono != null && Time.time - ultimaActivacion > 0.9f)
        {
            ultimaActivacion = Time.time;
            tono.Play();
        }
    }
    void OnDisable()
    {
        foreach (var e in escalas) if (e.Key != null) e.Key.localScale = e.Value;
        if (resonancia != null) { resonancia.Stop(); resonancia.volume = 0; }
    }
}
