using UnityEngine;

/// <summary>
/// FILTRO II - HUECO.
/// Rejas, escombros y tapas de piso que se vuelven atravesables al iluminarlas.
///
/// Poner en: el objeto solido, con Collider y MeshRenderer.
/// El Collider tiene que NO ser trigger. El script lo desactiva.
/// </summary>
public class MateriaHueca : ReceptorDeLuz
{
    [Header("Disolucion")]
    [Range(0f, 1f)] public float opacidadMinima = 0.15f;
    public float velocidadDeTransicion = 4f;

    [Header("Sonido")]
    public AudioSource siseo;

    Collider[] colliders;
    Renderer[] renders;
    Material[] materiales;
    float disolucion;   // 0 solido, 1 atravesable

    void Awake()
    {
        canalRequerido = FiltroDefinicion.Canal.Hueco;
        colliders = GetComponentsInChildren<Collider>();
        renders = GetComponentsInChildren<Renderer>();
        materiales = new Material[renders.Length];
        for (int i = 0; i < renders.Length; i++) materiales[i] = renders[i].material;
    }

    protected override void AlActualizar(bool recibiendoLuz)
    {
        float objetivo = Activo ? 1f : 0f;
        disolucion = Mathf.MoveTowards(disolucion, objetivo, velocidadDeTransicion * Time.deltaTime);

        bool solido = disolucion < 0.5f;
        foreach (var c in colliders) if (c != null && !c.isTrigger) c.enabled = solido;

        for (int i = 0; i < materiales.Length; i++)
        {
            if (materiales[i] == null) continue;
            Color c = materiales[i].GetColor("_BaseColor");
            c.a = Mathf.Lerp(1f, opacidadMinima, disolucion);
            materiales[i].SetColor("_BaseColor", c);
        }

        if (siseo != null)
        {
            if (Activo && !siseo.isPlaying) siseo.Play();
            if (!Activo && siseo.isPlaying) siseo.Stop();
        }
    }
}
