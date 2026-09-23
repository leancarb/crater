using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// FILTRO II · HUECO.
/// Rejas y tapas de piedra que se vuelven atravesables al iluminarlas.
///
/// Poner en: el objeto raíz, con un Collider sólido y un Collider trigger un poco
/// más grande (detecta si el jugador está adentro, para no cerrarse encima de él).
/// Los materiales tienen que ser transparentes para que se vea la disolución.
/// </summary>
public class MateriaHueca : ReceptorDeLuz
{
    [Header("Disolución")]
    [Range(0f, 1f)] public float opacidadMinima = 0.1f;
    public float velocidadDeTransicion = 4f;
    [Tooltip("Colliders que se apagan al disolverse. Vacío = todos los no-trigger del objeto.")]
    public Collider[] solidos;
    [Tooltip("Renderers que se desvanecen. Vacío = todos.")]
    public Renderer[] renderers;
    [Tooltip("Sello azul: brilla más a medida que se carga.")]
    public Renderer[] sellos;
    public Color colorSello = new Color(0.12f, 0.3f, 1f);

    [Header("Sonido")]
    public AudioSource siseo;

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    public float Disolucion { get; private set; }   // 0 sólido, 1 atravesable
    public bool Solido { get; private set; } = true;

    MaterialPropertyBlock bloque;
    Color[] coloresBase;
    int jugadoresDentro;
    bool sombrasApagadas;

    void Awake() => Inicializar();

    void Inicializar()
    {
        if (solidos == null || solidos.Length == 0)
            solidos = GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).ToArray();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);
        sellos ??= new Renderer[0];

        coloresBase = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var m = renderers[i] != null ? renderers[i].sharedMaterial : null;
            coloresBase[i] = m != null && m.HasProperty(IdBase) ? m.GetColor(IdBase) : Color.white;
        }
        bloque = new MaterialPropertyBlock();
    }

    protected override void AlActualizar(float delta)
    {
        if (coloresBase == null) Inicializar();

        Disolucion = Mathf.MoveTowards(Disolucion, Activo ? 1f : 0f, velocidadDeTransicion * delta);

        // Nunca reconstruir la materia alrededor del jugador: esperar a que
        // salga del volumen evita quedar atrapado dentro de la reja.
        bool solido = Disolucion < 0.5f && jugadoresDentro == 0;
        if (solido != Solido)
        {
            Solido = solido;
            foreach (var c in solidos) if (c != null) c.enabled = solido;
        }

        float alfa = Mathf.Lerp(1f, opacidadMinima, Disolucion);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            r.GetPropertyBlock(bloque);
            Color c = coloresBase[i];
            c.a *= alfa;
            bloque.SetColor(IdBase, c);
            if (System.Array.IndexOf(sellos, r) >= 0)
                bloque.SetColor(IdEmision, colorSello * Mathf.Lerp(0.9f, 4f, Mathf.Max(Carga, Disolucion)) * alfa);
            r.SetPropertyBlock(bloque);
        }

        bool apagarSombras = Disolucion > 0.3f;
        if (apagarSombras != sombrasApagadas)
        {
            sombrasApagadas = apagarSombras;
            foreach (var r in renderers)
                if (r != null) r.shadowCastingMode = apagarSombras ? ShadowCastingMode.Off : ShadowCastingMode.On;
        }

        if (siseo != null)
        {
            if (Activo && !siseo.isPlaying) siseo.Play();
            if (!Activo && siseo.isPlaying) siseo.Stop();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) jugadoresDentro++;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) jugadoresDentro = Mathf.Max(0, jugadoresDentro - 1);
    }

    void OnDisable()
    {
        jugadoresDentro = 0;
    }
}
