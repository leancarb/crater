using UnityEngine;

/// <summary>
/// La pared pulida del fondo de la Cresta. Funciona como una ventana de noche:
/// con la linterna prendida sólo se ve el reflejo encandilante del propio foco.
/// Es la pista de que hay que apagarla.
///
/// El reflejo sigue al haz: aparece donde la linterna toca la pared, y es más fuerte
/// cuanto más de frente y más cerca se apunta. Una luz de rebote devuelve el brillo
/// hacia el jugador, como un espejo.
///
/// Poner en: un objeto vacío en el centro de la cara de la pared, con el eje azul
/// (forward) entrando en la pared.
///
/// CÓMO FUNCIONA
/// Cada frame corta el rayo de la linterna (su posición y hacia dónde mira) con el plano
/// de la pared. Si el corte cae dentro de la pared, pone ahí un quad brillante (el reflejo,
/// del tamaño del círculo del haz) y, sobre el rayo reflejado, una luz de rebote. No es un
/// espejo real (sería caro): es el efecto que se vería.
/// </summary>
public class ParedEspejo : MonoBehaviour
{
    [Tooltip("Ancho y alto de la cara reflejante, en metros.")]
    [SerializeField] Vector2 tamanio = new Vector2(24f, 8f);

    [Header("Reflejo")]
    [SerializeField] Renderer reflejo;
    [SerializeField] Light luzRebote;
    [Tooltip("Tamaño del reflejo respecto del círculo del haz sobre la pared.")]
    [SerializeField] float tamanioReflejo = 1.4f;
    [SerializeField] float intensidadReflejo = 16f;
    [SerializeField] float intensidadRebote = 60f;
    [Tooltip("Más alto = el reflejo sólo es fuerte si se apunta muy de frente.")]
    [SerializeField] float concentracion = 1.2f;

    /// <summary>0 a 1: qué tan encandilado está el jugador por su propio reflejo.</summary>
    public float Encandilamiento { get; private set; }

    MaterialPropertyBlock bloque;
    static readonly int IdColor = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        Apagar();
    }

    void LateUpdate()
    {
        var linterna = LinternaController.Instancia;
        var cam = Camera.main;
        if (linterna == null || cam == null || !linterna.Encendida || linterna.spot == null || !linterna.spot.enabled)
        {
            Apagar();
            return;
        }

        Vector3 normal = -transform.forward;                 // mira hacia la sala
        Vector3 origen = linterna.spot.transform.position;
        Vector3 haz = linterna.spot.transform.forward;
        float distancia = Vector3.Dot(origen - transform.position, normal);
        float hacia = Vector3.Dot(haz, -normal);             // 1 si el haz va derecho a la pared
        if (distancia <= 0.05f || hacia <= 0.05f) { Apagar(); return; }

        // dónde toca el haz la pared
        float largo = distancia / hacia;
        if (largo > linterna.AlcanceActual * 1.5f) { Apagar(); return; }
        Vector3 toque = origen + haz * largo;
        Vector3 local = transform.InverseTransformPoint(toque);
        if (Mathf.Abs(local.x) > tamanio.x * 0.5f || Mathf.Abs(local.y) > tamanio.y * 0.5f)
        {
            Apagar();
            return;
        }

        float cercania = Mathf.Clamp01(1f - largo / (linterna.AlcanceActual * 1.5f));
        float k = Mathf.Pow(hacia, concentracion) * Mathf.Lerp(0.35f, 1f, cercania);
        Encandilamiento = k;
        Color color = linterna.ColorActual;
        // el círculo del haz sobre la pared: crece con la distancia y se estira al apuntar de costado
        float radioHaz = largo * Mathf.Tan(linterna.AnguloActual * 0.5f * Mathf.Deg2Rad);

        if (reflejo != null)
        {
            reflejo.enabled = k > 0.001f;
            reflejo.transform.SetPositionAndRotation(toque + normal * 0.03f, Quaternion.LookRotation(-normal));
            reflejo.transform.localScale = Vector3.one * Mathf.Max(0.6f, radioHaz * 2f * tamanioReflejo);
            Color c = color * intensidadReflejo * k;
            c.a = Mathf.Clamp01(k * 1.5f);
            reflejo.GetPropertyBlock(bloque);
            bloque.SetColor(IdColor, c);
            reflejo.SetPropertyBlock(bloque);
        }

        // el rayo reflejado: la luz vuelve hacia el que apunta
        Vector3 rebota = Vector3.Reflect(haz, normal);
        if (luzRebote != null)
        {
            luzRebote.enabled = k > 0.001f;
            luzRebote.transform.position = toque + rebota * 1.2f;
            luzRebote.color = color;
            luzRebote.intensity = intensidadRebote * k;
        }
    }

    void Apagar()
    {
        Encandilamiento = 0f;
        if (reflejo != null) reflejo.enabled = false;
        if (luzRebote != null) luzRebote.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(tamanio.x, tamanio.y, 0.01f));
    }
}
