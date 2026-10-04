using UnityEngine;

/// <summary>
/// El último puzzle de la rotonda: el obelisco del centro. Duerme hasta que los dos
/// sellos están encendidos; entonces despierta (Despertar) y en sus caras aparecen un sol
/// (oeste) y una luna (este). Cada uno es un ancla: el sol se enciende con el filtro SOL y
/// la luna con el filtro LUNA, y se mantienen unos segundos encendidos. Con los dos a la
/// vez se forma el eclipse: se enciende el de la cara sur, los dos quedan prendidos para
/// siempre y 'Resuelto' pasa a true (la cinemática del sello abre entonces la puerta).
///
/// CÓMO FUNCIONA
/// Las dos anclas empiezan deshabilitadas (la linterna no las ve) y con sus tallados
/// ocultos. Despertar() las habilita y muestra. En Update, si las dos están activas, se
/// marcan permanentes, se prende el eclipse (emisión y luz) y suena el tono de la luna.
/// </summary>
public class ObeliscoDelEclipse : MonoBehaviour
{
    public Ancla sol;
    public Ancla luna;
    public Renderer eclipse;
    public Light luz;
    public Color colorEclipse = new Color(0.85f, 0.92f, 1f);
    public float intensidadLuz = 30f;

    public bool Despierto { get; private set; }
    public bool Resuelto { get; private set; }

    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
    float brillo;

    public void Despertar()
    {
        if (Despierto) return;
        Despierto = true;
        foreach (var ancla in new[] { sol, luna })
        {
            if (ancla == null) continue;
            ancla.enabled = true;
            if (ancla.acentos != null) foreach (var r in ancla.acentos) if (r != null) r.enabled = true;
        }
    }

    /// <summary>Hacia dónde mirar al despertarlo: entre el sol y la luna.</summary>
    public Vector3 Centro => sol != null && luna != null ? (sol.PuntoDeImpacto + luna.PuntoDeImpacto) / 2f : transform.position;

    void Update()
    {
        if (Despierto && !Resuelto && sol != null && luna != null && sol.Activo && luna.Activo)
        {
            Resuelto = true;
            sol.permanente = true;
            luna.permanente = true;
            if (eclipse != null) eclipse.enabled = true;
        }
        if (!Resuelto || brillo >= 1f) return;
        brillo = Mathf.MoveTowards(brillo, 1f, Time.deltaTime / 1.5f);
        if (eclipse != null)
        {
            var bloque = new MaterialPropertyBlock();
            eclipse.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, colorEclipse * (3f * brillo));
            eclipse.SetPropertyBlock(bloque);
        }
        if (luz != null) { luz.enabled = true; luz.intensity = intensidadLuz * brillo; }
    }
}
