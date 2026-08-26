using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La geometria que aparece cuando las anclas estan encendidas.
///
/// IMPORTANTE: el puente NO se genera en runtime. Se modela y se coloca en la escena,
/// desactivado. Este script solo lo prende y lo apaga. Asi el diseniador de niveles
/// arma puentes sin tocar codigo.
///
/// Poner en: el GameObject del puente (con MeshRenderer + Collider).
/// </summary>
public class PuenteLuz : MonoBehaviour
{
    [Header("Anclas que lo sostienen (todas tienen que estar activas)")]
    public List<Ancla> anclas = new List<Ancla>();

    [Header("Geometria")]
    public MeshRenderer[] mallas;
    public Collider[] colliders;

    [Header("Transicion")]
    [Tooltip("Segundos que tarda en aparecer.")]
    public float tiempoDeAparicion = 0.25f;
    [Tooltip("Segundos que tarda en disolverse. Un poco mas lento = mas justo para el jugador.")]
    public float tiempoDeDisolucion = 0.4f;

    [Header("Sonido")]
    public AudioSource sonidoAparecer;
    public AudioSource sonidoDisolver;

    float visibilidad;      // 0 a 1
    bool estabaSolido;
    Material[] materiales;

    void Awake()
    {
        if (mallas == null || mallas.Length == 0) mallas = GetComponentsInChildren<MeshRenderer>();
        if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>();

        materiales = new Material[mallas.Length];
        for (int i = 0; i < mallas.Length; i++)
        {
            materiales[i] = mallas[i].material;
            materiales[i].EnableKeyword("_EMISSION");
        }
        AplicarVisibilidad(0f);
    }

    void Update()
    {
        bool deberiaExistir = anclas.Count > 0;
        foreach (var a in anclas)
        {
            if (a == null || !a.Activo) { deberiaExistir = false; break; }
        }

        float objetivo = deberiaExistir ? 1f : 0f;
        float velocidad = deberiaExistir
            ? 1f / Mathf.Max(0.01f, tiempoDeAparicion)
            : 1f / Mathf.Max(0.01f, tiempoDeDisolucion);

        visibilidad = Mathf.MoveTowards(visibilidad, objetivo, velocidad * Time.deltaTime);
        AplicarVisibilidad(visibilidad);

        // el collider se prende antes de que termine de aparecer, y se apaga al final:
        // el jugador nunca siente que "se cayo por nada".
        bool solido = visibilidad > 0.35f;
        if (solido != estabaSolido)
        {
            estabaSolido = solido;
            foreach (var c in colliders) if (c != null) c.enabled = solido;
            if (solido && sonidoAparecer != null) sonidoAparecer.Play();
            if (!solido && sonidoDisolver != null) sonidoDisolver.Play();
        }
    }

    void AplicarVisibilidad(float v)
    {
        for (int i = 0; i < mallas.Length; i++)
        {
            if (mallas[i] == null) continue;
            mallas[i].enabled = v > 0.001f;
            if (materiales[i] == null) continue;

            Color c = materiales[i].GetColor("_BaseColor");
            c.a = v;
            materiales[i].SetColor("_BaseColor", c);
            materiales[i].SetColor("_EmissionColor",
                new Color(0.91f, 0.63f, 0.29f) * Mathf.Lerp(0f, 1.6f, v));
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.91f, 0.63f, 0.29f, 0.8f);
        foreach (var a in anclas)
            if (a != null) Gizmos.DrawLine(transform.position, a.transform.position);
    }
}
