using System.Collections;
using UnityEngine;

/// <summary>
/// El techo de la Cresta que se abre al final. Dos hojas de piedra tapan un hueco
/// en el centro del techo: primero se despegan (aparece una raya de luz) y después
/// se corren hacia los costados, encima del techo fijo. Mientras se abren, la luz
/// del cielo que entra por el hueco crece hasta encandilar.
///
/// CÓMO FUNCIONA
/// Abrir() arranca una corrutina con dos tramos: 'tiempoDespegue' (las hojas suben
/// 'despegue' metros) y 'tiempoApertura' (se corren 'apertura' metros, cada una hacia
/// su lado). La luz sube con una curva que acelera al final (k²): casi nada al
/// principio, todo al terminar. EclipseFinalController lo llama y espera 'Duracion'.
/// </summary>
public class AperturaTecho : MonoBehaviour
{
    [SerializeField] Transform hojaIzquierda;
    [SerializeField] Transform hojaDerecha;
    [SerializeField] float despegue = 0.4f;
    [SerializeField] float apertura = 6.3f;
    [SerializeField] float tiempoDespegue = 1.6f;
    [SerializeField] float tiempoApertura = 7f;

    [Header("Luz")]
    [Tooltip("Luz que entra desde arriba por el hueco.")]
    [SerializeField] Light luzDelCielo;
    [SerializeField] float intensidadFinal = 4000f;

    [Header("Sonido")]
    [SerializeField] AudioSource sonido;

    public bool Abriendo { get; private set; }
    public float Duracion => tiempoDespegue + tiempoApertura;

    Vector3 origenIzq, origenDer;

    void Awake()
    {
        if (hojaIzquierda != null) origenIzq = hojaIzquierda.localPosition;
        if (hojaDerecha != null) origenDer = hojaDerecha.localPosition;
        if (luzDelCielo != null) { luzDelCielo.intensity = 0f; luzDelCielo.enabled = false; }
    }

    public void Abrir()
    {
        if (Abriendo) return;
        Abriendo = true;
        StartCoroutine(Abrirse());
    }

    IEnumerator Abrirse()
    {
        if (sonido != null) sonido.Play();
        if (luzDelCielo != null) luzDelCielo.enabled = true;
        float total = Duracion;

        for (float t = 0f; t < total; t += Time.deltaTime)
        {
            Mover(t);
            if (luzDelCielo != null) luzDelCielo.intensity = intensidadFinal * Mathf.Pow(t / total, 2f);
            yield return null;
        }
        Mover(total);
        if (luzDelCielo != null) luzDelCielo.intensity = intensidadFinal;
    }

    void Mover(float t)
    {
        float subir = Mathf.SmoothStep(0f, 1f, t / tiempoDespegue) * despegue;
        float correr = Mathf.SmoothStep(0f, 1f, (t - tiempoDespegue) / tiempoApertura) * apertura;
        if (hojaIzquierda != null) hojaIzquierda.localPosition = origenIzq + new Vector3(-correr, subir, 0f);
        if (hojaDerecha != null) hojaDerecha.localPosition = origenDer + new Vector3(correr, subir, 0f);
    }
}
