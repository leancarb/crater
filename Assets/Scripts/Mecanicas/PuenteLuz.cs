using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La geometría que aparece cuando sus anclas están encendidas.
///
/// El puente NO se genera en runtime: se modela y se coloca en la escena.
/// Este script sólo lo materializa y lo disuelve. Cuando la retención de
/// alguna ancla está por agotarse, el puente parpadea como aviso.
///
/// Poner en: el objeto raíz del puente (con sus mallas y un Collider).
/// </summary>
public class PuenteLuz : MonoBehaviour
{
    [Header("Anclas que lo sostienen (todas tienen que estar activas)")]
    public List<ReceptorDeLuz> anclas = new List<ReceptorDeLuz>();

    [Header("Geometría")]
    public Renderer[] mallas;
    public Collider[] colliders;

    [Header("Aspecto")]
    public Color colorEmision = new Color(1f, 0.36f, 0.06f);
    public float emisionMaxima = 1.4f;

    [Header("Transición")]
    [Tooltip("Segundos que tarda en aparecer.")]
    public float tiempoDeAparicion = 0.35f;
    [Tooltip("Segundos que tarda en disolverse. Un poco más lento = más justo para el jugador.")]
    public float tiempoDeDisolucion = 0.6f;
    [Tooltip("Visibilidad a partir de la cual se puede pisar.")]
    [Range(0.05f, 0.95f)] public float umbralSolido = 0.35f;

    [Header("Sonido")]
    public AudioSource sonidoAparecer;
    public AudioSource sonidoDisolver;

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    public float Visibilidad { get; private set; }
    public bool Solido { get; private set; }

    MaterialPropertyBlock bloque;
    Color[] coloresBase;

    void Awake() => Inicializar();

    void Inicializar()
    {
        if (mallas == null || mallas.Length == 0) mallas = GetComponentsInChildren<Renderer>(true);
        if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>(true);

        coloresBase = new Color[mallas.Length];
        for (int i = 0; i < mallas.Length; i++)
        {
            var m = mallas[i] != null ? mallas[i].sharedMaterial : null;
            coloresBase[i] = m != null && m.HasProperty(IdBase) ? m.GetColor(IdBase) : Color.white;
        }
        bloque = new MaterialPropertyBlock();

        Solido = false;
        foreach (var c in colliders) if (c != null) c.enabled = false;
        AplicarVisibilidad(0f);
    }

    void Update() => Avanzar(Time.deltaTime);

    public void Avanzar(float delta)
    {
        if (coloresBase == null) Inicializar();

        bool deberiaExistir = anclas.Count > 0;
        float aviso = 1f;
        foreach (var a in anclas)
        {
            if (a == null || !a.Activo) { deberiaExistir = false; break; }
            aviso = Mathf.Min(aviso, a.RetencionRestante);
        }

        float velocidad = deberiaExistir
            ? 1f / Mathf.Max(0.01f, tiempoDeAparicion)
            : 1f / Mathf.Max(0.01f, tiempoDeDisolucion);
        Visibilidad = Mathf.MoveTowards(Visibilidad, deberiaExistir ? 1f : 0f, velocidad * delta);

        float parpadeo = 1f;
        if (deberiaExistir && aviso < 0.35f)
            parpadeo = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Cos(Time.time * Mathf.Lerp(22f, 8f, aviso / 0.35f)));
        AplicarVisibilidad(Visibilidad * parpadeo);

        // el collider se prende antes de que termine de aparecer, y se apaga al final:
        // el jugador nunca siente que "se cayó por nada".
        bool solido = Visibilidad > umbralSolido;
        if (solido != Solido)
        {
            Solido = solido;
            foreach (var c in colliders) if (c != null) c.enabled = solido;
            if (solido && sonidoAparecer != null) sonidoAparecer.Play();
            if (!solido && sonidoDisolver != null) sonidoDisolver.Play();
        }
    }

    void AplicarVisibilidad(float v)
    {
        for (int i = 0; i < mallas.Length; i++)
        {
            var r = mallas[i];
            if (r == null) continue;
            r.enabled = v > 0.001f;
            if (!r.enabled) continue;

            r.GetPropertyBlock(bloque);
            Color c = coloresBase[i];
            c.a *= v;
            bloque.SetColor(IdBase, c);
            bloque.SetColor(IdEmision, colorEmision * (emisionMaxima * v));
            r.SetPropertyBlock(bloque);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.8f);
        foreach (var a in anclas)
            if (a != null) Gizmos.DrawLine(transform.position, a.PuntoDeImpacto);
    }
}
