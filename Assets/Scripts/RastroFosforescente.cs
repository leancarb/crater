using UnityEngine;

/// <summary>
/// FILTRO III - RASTRO.
/// Superficies que quedan brillando despues de que apagas la linterna.
///
/// Poner en: paredes de ceniza, inscripciones, o encima de un Ancla
/// (una Ancla con RastroFosforescente sostiene su puente sin el haz).
/// </summary>
public class RastroFosforescente : ReceptorDeLuz
{
    [Header("Persistencia")]
    public float duracion = 8f;
    public Color colorDelRastro = new Color(0.93f, 0.92f, 0.89f);
    [Range(0f, 6f)] public float intensidadMaxima = 2.2f;

    [Header("Si esta sobre un Ancla, la mantiene encendida")]
    public Ancla anclaAsociada;

    Renderer[] renders;
    Material[] materiales;
    float restante;

    void Awake()
    {
        canalRequerido = FiltroDefinicion.Canal.Rastro;
        retencion = duracion;
        renders = GetComponentsInChildren<Renderer>();
        materiales = new Material[renders.Length];
        for (int i = 0; i < renders.Length; i++)
        {
            materiales[i] = renders[i].material;
            materiales[i].EnableKeyword("_EMISSION");
        }
    }

    public override void RecibirLuz(FiltroDefinicion filtro, float delta)
    {
        base.RecibirLuz(filtro, delta);
        if (filtro != null && filtro.canal == FiltroDefinicion.Canal.Rastro)
            restante = duracion;
    }

    protected override void AlActualizar(bool recibiendoLuz)
    {
        if (restante > 0f) restante -= Time.deltaTime;
        float t = Mathf.Clamp01(restante / duracion);

        // curva: se mantiene fuerte y cae al final, como la fosforescencia real
        float brillo = Mathf.Pow(t, 0.45f) * intensidadMaxima;

        foreach (var m in materiales)
            if (m != null) m.SetColor("_EmissionColor", colorDelRastro * brillo);

        if (anclaAsociada != null && restante > 0f)
            anclaAsociada.RecibirLuz(null, Time.deltaTime);   // canal Ninguno = pasa siempre
    }
}
