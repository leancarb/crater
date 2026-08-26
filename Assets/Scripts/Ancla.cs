using UnityEngine;

/// <summary>
/// FILTRO I - CUERPO.
/// Nodo que se enciende con el haz. Dos o mas anclas encendidas activan un PuenteLuz.
///
/// Poner en: un objeto con Collider, en la capa "Ancla".
/// Para hacerla "ancla de retencion": subir 'retencion' a 3.
/// </summary>
public class Ancla : ReceptorDeLuz
{
    [Header("Aspecto")]
    public Renderer indicador;
    public Color colorApagada = new Color(0.12f, 0.11f, 0.10f);
    public Color colorEncendida = new Color(0.91f, 0.63f, 0.29f);
    [Tooltip("Tono que suena al encenderse. Afinar cada ancla distinto para armar un acorde.")]
    public AudioSource tono;

    Material mat;

    void Awake()
    {
        canalRequerido = FiltroDefinicion.Canal.Cuerpo;
        if (indicador != null)
        {
            mat = indicador.material;          // instancia propia
            mat.EnableKeyword("_EMISSION");
        }
    }

    protected override void AlActualizar(bool recibiendoLuz)
    {
        if (mat == null) return;
        Color c = Color.Lerp(colorApagada, colorEncendida, Carga);
        mat.SetColor("_BaseColor", c);
        mat.SetColor("_EmissionColor", c * Mathf.Lerp(0f, 4f, Carga));
    }

    protected override void Activar()
    {
        base.Activar();
        if (tono != null) tono.Play();
    }
}
