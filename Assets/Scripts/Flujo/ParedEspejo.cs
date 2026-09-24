using UnityEngine;

/// <summary>
/// La pared pulida del fondo de la Cresta. Funciona como una ventana de noche:
/// con la linterna prendida sólo se ve el reflejo encandilante del propio foco.
/// Es la pista de que hay que apagarla.
///
/// El reflejo se dibuja en el punto de la pared más cercano a la cámara (donde un
/// espejo plano muestra al que lo mira) y es más fuerte cuanto más de frente se apunta.
///
/// Poner en: un objeto vacío en el centro de la cara de la pared, con el eje azul
/// (forward) entrando en la pared.
/// </summary>
public class ParedEspejo : MonoBehaviour
{
    [Tooltip("Ancho y alto de la cara reflejante, en metros.")]
    [SerializeField] Vector2 tamanio = new Vector2(24f, 8f);

    [Header("Reflejo")]
    [SerializeField] Renderer reflejo;
    [SerializeField] Light luzRebote;
    [SerializeField] float tamanioReflejo = 0.9f;
    [SerializeField] float intensidadReflejo = 6f;
    [SerializeField] float intensidadRebote = 25f;
    [Tooltip("Más alto = el reflejo sólo aparece si se apunta muy de frente.")]
    [SerializeField] float concentracion = 3f;

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
        Vector3 ojo = cam.transform.position;
        float distancia = Vector3.Dot(ojo - transform.position, normal);
        if (distancia <= 0.05f) { Apagar(); return; }         // detrás de la pared

        Vector3 pie = ojo - normal * distancia;               // donde el espejo te muestra
        Vector3 local = transform.InverseTransformPoint(pie);
        if (Mathf.Abs(local.x) > tamanio.x * 0.5f || Mathf.Abs(local.y) > tamanio.y * 0.5f)
        {
            Apagar();
            return;
        }

        float deFrente = Mathf.Clamp01(Vector3.Dot(linterna.transform.forward, -normal));
        float cercania = Mathf.Clamp01(1f - distancia / linterna.AlcanceActual);   // el haz va y vuelve
        float k = Mathf.Pow(deFrente, concentracion) * cercania;
        Encandilamiento = k;
        Color color = linterna.ColorActual;

        if (reflejo != null)
        {
            reflejo.enabled = k > 0.001f;
            reflejo.transform.SetPositionAndRotation(pie + normal * 0.03f, Quaternion.LookRotation(-normal));
            reflejo.transform.localScale = Vector3.one * (tamanioReflejo * (1f + distancia * 0.08f));
            Color c = color * intensidadReflejo * k;
            c.a = k;
            reflejo.GetPropertyBlock(bloque);
            bloque.SetColor(IdColor, c);
            reflejo.SetPropertyBlock(bloque);
        }

        if (luzRebote != null)
        {
            luzRebote.enabled = k > 0.001f;
            luzRebote.transform.position = pie + normal * 0.6f;
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
