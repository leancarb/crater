using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// LA LENTE — puzzle puramente posicional, no reacciona al haz de la linterna.
/// Entre el jugador y la lente hay un diafragma con un agujero chico: solo desde
/// una cuña de posiciones en el piso la recta ojo->centro-de-lente pasa por la
/// abertura. Hay que CAMINAR hasta encontrar el lugar, no alcanza con apuntar.
///
/// Poner en: el GameObject de la lente misma (el objeto fijo en la sala).
/// </summary>
public class Lente : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camaraJugador;
    [Tooltip("Transform en el centro del agujero del diafragma.")]
    public Transform aperturaCentro;

    [Header("Optica")]
    public float aperturaRadio = 0.2f;
    public float alcanceMaximo = 14f;

    [Header("Umbral de resolucion")]
    [Range(0f, 1f)] public float umbralAlineacion = 0.55f;
    [Tooltip("Segundos que hay que sostener la alineacion, sin cortarla, para resolver.")]
    public float tiempoSostenido = 2.2f;

    [Header("Eventos (opcional, para conectar sin codigo)")]
    public UnityEvent alResolver;

    public bool Resuelto { get; private set; }
    public float AlineacionSuave { get; private set; }   // 0 a 1, suavizada

    float sostenido;

    void Awake()
    {
        if (camaraJugador == null && Camera.main != null)
            camaraJugador = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (camaraJugador == null || aperturaCentro == null) return;

        float alineacionCruda = CalcularAlineacionCruda();
        AlineacionSuave = Mathf.MoveTowards(AlineacionSuave, alineacionCruda, Time.deltaTime * 2f);

        if (Resuelto) return;

        // umbral estricto: cortar la alineacion reinicia el conteo entero,
        // no basta con barrer la abertura de casualidad, hay que quedarse parado.
        if (AlineacionSuave > umbralAlineacion) sostenido += Time.deltaTime;
        else sostenido = 0f;

        if (sostenido > tiempoSostenido)
        {
            Resuelto = true;
            alResolver?.Invoke();
        }
    }

    float CalcularAlineacionCruda()
    {
        Vector3 ojo = camaraJugador.position;
        Vector3 haciaLente = transform.position - ojo;
        if (haciaLente.magnitude > alcanceMaximo) return 0f;

        // interseccion de la recta ojo->lente con el plano del diafragma
        Vector3 normalPlano = aperturaCentro.forward;
        Vector3 direccion = haciaLente.normalized;
        float denom = Vector3.Dot(normalPlano, direccion);
        if (Mathf.Approximately(denom, 0f)) return 0f;   // paralela al plano

        float t = Vector3.Dot(aperturaCentro.position - ojo, normalPlano) / denom;
        if (t < 0f || t > haciaLente.magnitude) return 0f;   // el cruce queda fuera del segmento ojo-lente

        Vector3 puntoDeCruce = ojo + direccion * t;

        // proyectar el cruce sobre el plano local de la abertura
        Vector3 offset = puntoDeCruce - aperturaCentro.position;
        float x = Vector3.Dot(offset, aperturaCentro.right);
        float y = Vector3.Dot(offset, aperturaCentro.up);
        float distancia = new Vector2(x, y).magnitude;

        return Mathf.Clamp01(1f - distancia / aperturaRadio);
    }

    void OnDrawGizmosSelected()
    {
        Transform camara = camaraJugador != null ? camaraJugador
            : (Camera.main != null ? Camera.main.transform : null);

        Gizmos.color = Color.cyan;
        if (camara != null)
            Gizmos.DrawLine(camara.position, transform.position);

        if (aperturaCentro != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(aperturaCentro.position, aperturaRadio);
        }
    }
}
