using System.Collections;
using UnityEngine;

/// <summary>
/// Una fila de tallados en la pared que se enciende de a uno cuando sus sellos se
/// encienden. En la rotonda, cada fila va de un ala a la puerta de los sellos: se
/// lee como un camino de luz que llega a la puerta y falta el otro.
///
/// CÓMO FUNCIONA
/// Cada frame mira si todos los 'sellos' están activos (y, si hay una compuerta en
/// 'esperarA', que haya terminado de abrirse: así se encienden cuando el jugador
/// vuelve por el atajo). Entonces arranca una corrutina que prende los tallados en
/// orden, uno cada 'retraso' segundos, con un destello y un tono que va subiendo.
/// El brillo se maneja con MaterialPropertyBlock, sin tocar el material compartido.
/// </summary>
public class HiloDeTallados : MonoBehaviour
{
    public ReceptorDeLuz[] sellos;
    [Tooltip("Opcional: esperar a que esta compuerta termine de abrirse.")]
    public Compuerta esperarA;
    [Tooltip("En orden: el primero se prende primero.")]
    public Renderer[] tallados;
    public Color color = new Color(1f, 0.42f, 0.1f);
    public float emisionApagado = 0.05f;
    public float emisionEncendido = 3f;
    public float retraso = 0.45f;
    public float demoraInicial = 0.3f;
    [Tooltip("Lo enciende otro script (la cinemática del sello) llamando a Encender().")]
    public bool manual;

    /// <summary>Segundos desde Encender() hasta que el último tallado quedó prendido.</summary>
    public float Duracion => demoraInicial + (tallados != null ? tallados.Length : 0) * Mathf.Max(retraso, 0.5f);

    [Header("Sonido")]
    public AudioSource fuente;
    public AudioClip[] tonos;

    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock bloque;
    float[] nivel;
    bool encendido;

    void Start()
    {
        nivel = new float[tallados != null ? tallados.Length : 0];
        Aplicar();
    }

    void Update()
    {
        if (!manual && !encendido && Listo()) Encender();
        Aplicar();
    }

    bool Listo()
    {
        if (sellos == null || sellos.Length == 0) return false;
        foreach (var s in sellos) if (s == null || !s.Activo) return false;
        return esperarA == null || esperarA.Progreso >= 0.99f;
    }

    public void Encender()
    {
        if (encendido) return;
        encendido = true;
        StartCoroutine(Prender());
    }

    IEnumerator Prender()
    {
        yield return new WaitForSeconds(demoraInicial);
        for (int i = 0; i < nivel.Length; i++)
        {
            if (fuente != null && tonos != null && tonos.Length > 0) fuente.PlayOneShot(tonos[i % tonos.Length], 0.7f);
            // destello: sube rápido por encima del encendido y se asienta
            for (float t = 0f; t < 0.5f; t += Time.deltaTime)
            {
                float k = t / 0.5f;
                nivel[i] = k < 0.3f ? k / 0.3f * 1.6f : Mathf.Lerp(1.6f, 1f, (k - 0.3f) / 0.7f);
                yield return null;
            }
            nivel[i] = 1f;
            yield return new WaitForSeconds(Mathf.Max(0f, retraso - 0.5f));
        }
    }

    void Aplicar()
    {
        if (tallados == null || nivel == null) return;
        bloque ??= new MaterialPropertyBlock();
        for (int i = 0; i < tallados.Length; i++)
        {
            var r = tallados[i];
            if (r == null) continue;
            r.GetPropertyBlock(bloque);
            bloque.SetColor(IdEmision, color * Mathf.LerpUnclamped(emisionApagado, emisionEncendido, nivel[i]));
            r.SetPropertyBlock(bloque);
        }
    }
}
