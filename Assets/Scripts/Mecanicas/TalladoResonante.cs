using UnityEngine;

/// <summary>Respuesta opcional al gesto de apuntar; no abre puertas ni cambia el progreso.</summary>
public class TalladoResonante : ReceptorDeLuz
{
    public Renderer figura;
    public AudioSource resonancia;
    public Color color = new Color(0.6f, 0.74f, 1);
    public float Respuesta { get; private set; }
    MaterialPropertyBlock bloque;
    static readonly int Emision = Shader.PropertyToID("_EmissionColor");
    protected override void AlActualizar(float delta)
    {
        var linterna = LinternaController.Instancia;
        bool centrado = linterna != null && linterna.spot != null &&
            Vector3.Dot(linterna.spot.transform.forward, (PuntoDeImpacto - linterna.spot.transform.position).normalized) > 0.998f;
        float objetivo = Recibiendo && centrado ? Carga : 0;
        Respuesta = Mathf.MoveTowards(Respuesta, objetivo, delta * (objetivo > Respuesta ? 4 : 2));
        if (figura != null)
        {
            bloque ??= new MaterialPropertyBlock();
            figura.GetPropertyBlock(bloque);
            bloque.SetColor(Emision, color * (0.03f + Respuesta * 1.2f));
            figura.SetPropertyBlock(bloque);
        }
        if (resonancia != null)
        {
            resonancia.volume = Respuesta * 0.06f;
            if (Respuesta > 0.015f && !resonancia.isPlaying) resonancia.Play();
            if (Respuesta <= 0.015f && resonancia.isPlaying) resonancia.Stop();
        }
    }
    void OnDisable() { if (resonancia != null) resonancia.Stop(); Respuesta = 0; }
}
