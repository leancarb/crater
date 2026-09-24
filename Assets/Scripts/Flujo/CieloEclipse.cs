using UnityEngine;

/// <summary>
/// El cielo de la capilla: el sol, la luna que lo tapa y la corona, más toda la luz
/// del exterior. 'Progreso' va de 0 (día pleno) a 1 (totalidad).
///
/// Los discos se reubican cada frame a 'distancia' de la cámara, en la dirección
/// del sol, así parecen estar en el infinito. La dirección la da la luz del sol.
///
/// La luna nueva no se ve de día: sólo su silueta sobre el sol. Por eso se pinta
/// del color del cielo, y así desaparece en cuanto sale del disco solar.
///
/// CÓMO FUNCIONA
/// Al cambiar 'Progreso' (Aplicar) se interpolan entre día y totalidad: la intensidad y el
/// color del sol, la luz ambiente, la niebla y el fondo de la cámara. En LateUpdate
/// (Posicionar) se acomodan los discos: el sol y la corona en la dirección del sol, la
/// luna corrida según el progreso. Mostrar(false) apaga todo el exterior (dentro del cráter).
/// </summary>
public class CieloEclipse : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Light sol;
    [SerializeField] Transform discoSol;
    [SerializeField] Transform discoLuna;
    [SerializeField] Renderer corona;

    [Header("Tamaño aparente")]
    [Tooltip("Tiene que quedar dentro del plano lejano de la cámara.")]
    [SerializeField] float distancia = 150f;
    [Tooltip("Diámetro aparente del sol, en grados. El real es 0,53; más grande se lee mejor.")]
    [SerializeField] float diametroAngular = 4f;
    [Tooltip("Separación inicial de la luna, en radios del sol.")]
    [SerializeField] float separacionInicial = 2.4f;

    [Header("Día")]
    [SerializeField] Color solDia = new Color(1f, 0.88f, 0.72f);
    [SerializeField] float intensidadDia = 1.6f;
    [SerializeField] Color cieloDia = new Color(0.62f, 0.72f, 0.82f);
    [SerializeField] Color ambienteCieloDia = new Color(0.55f, 0.6f, 0.68f);
    [SerializeField] Color ambienteHorizonteDia = new Color(0.45f, 0.4f, 0.34f);
    [SerializeField] Color ambienteSueloDia = new Color(0.22f, 0.16f, 0.1f);

    [Header("Totalidad")]
    [SerializeField] Color solTotalidad = new Color(0.5f, 0.6f, 0.9f);
    [SerializeField] float intensidadTotalidad = 0.05f;
    [SerializeField] Color cieloTotalidad = new Color(0.02f, 0.025f, 0.045f);
    [SerializeField] Color ambienteCieloTotalidad = new Color(0.06f, 0.07f, 0.1f);
    [SerializeField] Color ambienteHorizonteTotalidad = new Color(0.035f, 0.035f, 0.045f);
    [SerializeField] Color ambienteSueloTotalidad = new Color(0.015f, 0.013f, 0.012f);

    [Header("Niebla")]
    [Tooltip("Más baja que la del epílogo: si no, la niebla se come los discos.")]
    [SerializeField] float densidadNiebla = 0.003f;

    [Header("Corona")]
    [SerializeField] Color colorCorona = new Color(0.8f, 0.88f, 1f);
    [SerializeField] float intensidadCorona = 3f;

    float progreso;
    bool visible;
    float ladoLuna = 1f;   // 1 = todavía no pasó, -1 = ya pasó (epílogo)
    Renderer rendererLuna;
    MaterialPropertyBlock bloque;
    static readonly int IdColor = Shader.PropertyToID("_BaseColor");

    public float Progreso
    {
        get => progreso;
        set { progreso = Mathf.Clamp01(value); Aplicar(); }
    }

    public Vector3 DireccionSol => sol != null ? -sol.transform.forward : Vector3.forward;

    /// <summary>El eclipse ya terminó: día pleno y la luna del otro lado del sol.</summary>
    public void PonerDespues()
    {
        ladoLuna = -1f;
        Progreso = 0f;
    }

    void Awake()
    {
        bloque = new MaterialPropertyBlock();
        if (discoLuna != null) rendererLuna = discoLuna.GetComponent<Renderer>();
        Mostrar(false);
    }

    /// <summary>Prende el cielo del exterior (discos, sol, niebla clara) o lo apaga del todo.</summary>
    public void Mostrar(bool valor)
    {
        visible = valor;
        if (discoSol != null) discoSol.gameObject.SetActive(valor);
        if (discoLuna != null) discoLuna.gameObject.SetActive(valor);
        if (corona != null) corona.gameObject.SetActive(valor);
        if (sol != null) sol.enabled = valor;
        if (valor) Aplicar();
    }

    void LateUpdate()
    {
        if (visible) Posicionar();
    }

    void Posicionar()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 origen = cam.transform.position;
        Vector3 d = DireccionSol;
        // tamaño aparente: a distancia D, un objeto de radio D·tan(θ/2) se ve con un ángulo θ.
        // Así el sol se ve igual de grande sin importar a qué distancia se lo ponga
        float tanRadio = Mathf.Tan(diametroAngular * 0.5f * Mathf.Deg2Rad);

        if (discoSol != null)
        {
            discoSol.position = origen + d * distancia;
            discoSol.localScale = Vector3.one * (distancia * tanRadio * 2f);
        }

        if (discoLuna != null)
        {
            // la luna llega en diagonal, desde abajo a la derecha
            // dos ejes perpendiculares a la dirección del sol, para correr la luna "sobre el cielo"
            Vector3 lateral = Vector3.Cross(Vector3.up, d);
            if (lateral.sqrMagnitude < 0.001f) lateral = Vector3.right;
            lateral.Normalize();
            Vector3 arriba = Vector3.Cross(d, lateral).normalized;
            Vector3 desvio = (lateral * 0.85f - arriba * 0.5f).normalized;

            // un poco más cerca que el sol: así lo tapa (queda delante en profundidad)
            float dist = distancia * 0.96f;
            // progreso 1 = centrada sobre el sol; 0 = corrida 'separacionInicial' radios
            float separacion = (1f - progreso) * separacionInicial * tanRadio * ladoLuna;
            discoLuna.position = origen + (d + desvio * separacion).normalized * dist;
            // un poco más grande que el sol: en la totalidad lo tapa entero
            discoLuna.localScale = Vector3.one * (dist * tanRadio * 2f * 1.04f);
        }

        // la corona va detrás del sol, siempre mirando a la cámara (billboard)
        if (corona != null)
        {
            float dist = distancia * 1.03f;
            corona.transform.position = origen + d * dist;
            corona.transform.rotation = Quaternion.LookRotation(d);
            corona.transform.localScale = Vector3.one * (dist * tanRadio * 2f * 3.4f);
        }
    }

    void Aplicar()
    {
        if (!visible) return;

        // la luz casi no baja hasta el final: en un eclipse real el cambio brusco
        // llega en los últimos minutos
        float luz = progreso < 0.8f
            ? Mathf.Lerp(1f, 0.72f, progreso / 0.8f)
            : Mathf.Lerp(0.72f, 0f, Mathf.SmoothStep(0f, 1f, (progreso - 0.8f) / 0.2f));

        if (sol != null)
        {
            sol.intensity = Mathf.Lerp(intensidadTotalidad, intensidadDia, luz);
            sol.color = Color.Lerp(solTotalidad, solDia, luz);
        }

        // luz ambiente en tres tonos: cielo (arriba), horizonte y suelo (abajo)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(ambienteCieloTotalidad, ambienteCieloDia, luz);
        RenderSettings.ambientEquatorColor = Color.Lerp(ambienteHorizonteTotalidad, ambienteHorizonteDia, luz);
        RenderSettings.ambientGroundColor = Color.Lerp(ambienteSueloTotalidad, ambienteSueloDia, luz);

        Color cielo = Color.Lerp(cieloTotalidad, cieloDia, luz);
        RenderSettings.fog = true;
        RenderSettings.fogColor = cielo;
        RenderSettings.fogDensity = densidadNiebla;
        var cam = Camera.main;
        if (cam != null) cam.backgroundColor = cielo;

        if (rendererLuna != null)
        {
            rendererLuna.GetPropertyBlock(bloque);
            bloque.SetColor(IdColor, cielo);
            rendererLuna.SetPropertyBlock(bloque);
        }

        if (corona != null)
        {
            // la corona sólo aparece en el último 5 % del eclipse (la totalidad)
            float c = Mathf.SmoothStep(0f, 1f, (progreso - 0.95f) / 0.05f);
            corona.enabled = c > 0.001f;
            Color col = colorCorona * intensidadCorona;
            col.a = c;
            corona.GetPropertyBlock(bloque);
            bloque.SetColor(IdColor, col);
            corona.SetPropertyBlock(bloque);
        }
    }
}
