using UnityEngine;

/// <summary>
/// Guarda la respuesta visible de ambos sellos en el centro de la rotonda.
/// Al regresar se encienden sus columnas una por una y después su cara,
/// sin tomar cámara ni movimiento. Las dos secuencias se escuchan por separado.
/// Con ambas caras, el eclipse sube de brillo y ordena abrir la puerta del Cruce.
/// Los cambios quedan visibles aunque el jugador mire a otro lado.
/// </summary>
public class ObeliscoDelEclipse : MonoBehaviour
{
    public JugadorFPS jugador;
    public TestigoDeSello sol, luna;
    public Compuerta puerta;
    public AudioSource resonanciaSol, resonanciaLuna;
    public TestigoDeSello[] columnasSol, columnasLuna;
    public AudioSource[] notasSol, notasLuna;
    public Light luzColumnasSol, luzColumnasLuna;
    [Min(.1f)] public float intervaloColumnas = .75f;
    public Vector3 centro = new Vector3(0, 0, 12);
    public float radio = 16.875f;
    public Renderer eclipse;
    public Renderer eclipseRegreso;
    public Light luz;
    public Color colorEclipse = new Color(0.85f, 0.92f, 1f);
    public float emisionApagado = 0.03f;
    public float intensidadLuz = 30f;

    public bool Encendido { get; private set; }

    /// <summary>Posición del motivo para inspección y composición.</summary>
    public Vector3 Centro => eclipse != null ? eclipse.transform.position : transform.position;

    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock bloque;
    float brillo;
    bool solSolicitado, lunaSolicitado, solIniciado, lunaIniciado;
    TestigoDeSello caraActual;
    TestigoDeSello[] columnasActuales;
    AudioSource[] notasActuales;
    AudioSource resonanciaActual;
    int siguienteColumna;
    float esperaColumna;
    public float Avance => brillo;
    public int SenalesEmitidas { get; private set; }
    public int NotasDeColumnasEmitidas { get; private set; }
    public bool SecuenciaEnCurso => caraActual != null;
    public float DuracionSol => Duracion(columnasSol, sol);
    public float DuracionLuna => Duracion(columnasLuna, luna);

    float Duracion(TestigoDeSello[] columnas, TestigoDeSello cara) =>
        (columnas?.Length ?? 0) * Mathf.Max(.1f, intervaloColumnas) + (cara != null ? cara.transicion : 0f);

    void Start() => Aplicar();

    public void Encender() => Encendido = true;

    void Update()
    {
        if (PausaCrater.EnPausa) return;
        if (EnLaRotonda())
        {
            // Registrar ambos pendientes antes de que el jugador pueda salir otra vez.
            // Así la segunda secuencia no depende de permanecer en la plaza.
            solSolicitado |= Pendiente(sol);
            lunaSolicitado |= Pendiente(luna);
        }
        if (caraActual == null)
        {
            if (solSolicitado && !solIniciado)
            {
                solIniciado = true;
                Iniciar(sol, columnasSol, notasSol, resonanciaSol);
            }
            else if (lunaSolicitado && !lunaIniciado)
            {
                lunaIniciado = true;
                Iniciar(luna, columnasLuna, notasLuna, resonanciaLuna);
            }
        }
        AvanzarColumnas();
        IluminarColumnas(luzColumnasSol, columnasSol);
        IluminarColumnas(luzColumnasLuna, columnasLuna);
        if (!Encendido && sol != null && luna != null && sol.Encendido && luna.Encendido &&
            sol.Nivel >= .99f && luna.Nivel >= .99f) Encender();
        if (!Encendido) return;
        if (brillo < 1f)
        {
            brillo = Mathf.MoveTowards(brillo, 1f, Time.deltaTime / 1.5f);
            Aplicar();
        }
        if (brillo >= 1f && puerta != null && !puerta.Abierta) puerta.Abrir();
    }

    bool EnLaRotonda()
    {
        if (jugador == null) return false;
        Vector3 d = jugador.transform.position - centro;
        return Mathf.Abs(d.y) < 2.5f && new Vector2(d.x, d.z).sqrMagnitude < radio * radio;
    }

    static bool Pendiente(TestigoDeSello cara) => cara != null && !cara.Encendido && cara.sello != null && cara.sello.Activo;

    void Iniciar(TestigoDeSello cara, TestigoDeSello[] columnas, AudioSource[] notas, AudioSource sonido)
    {
        caraActual = cara;
        columnasActuales = columnas;
        notasActuales = notas;
        resonanciaActual = sonido;
        siguienteColumna = 0;
        esperaColumna = 0f;
        SenalesEmitidas++;
    }

    void AvanzarColumnas()
    {
        if (caraActual == null) return;
        if (caraActual.Encendido)
        {
            if (caraActual.Nivel >= .99f) caraActual = null;
            return;
        }
        esperaColumna -= Time.deltaTime;
        if (esperaColumna > 0f) return;
        if (columnasActuales != null && siguienteColumna < columnasActuales.Length)
        {
            columnasActuales[siguienteColumna].Encender();
            // Una fuente fija por columna: las colas de las notas no cambian de lugar.
            if (notasActuales != null && siguienteColumna < notasActuales.Length && notasActuales[siguienteColumna] != null)
            {
                notasActuales[siguienteColumna].Play();
                NotasDeColumnasEmitidas++;
            }
            siguienteColumna++;
            esperaColumna = Mathf.Max(.1f, intervaloColumnas);
        }
        else
        {
            caraActual.Encender();
            if (resonanciaActual != null) resonanciaActual.Play();
        }
    }

    static void IluminarColumnas(Light luz, TestigoDeSello[] columnas)
    {
        if (luz == null || columnas == null || columnas.Length == 0) return;
        float nivel = 0f;
        foreach (var columna in columnas) if (columna != null) nivel += columna.Nivel;
        luz.intensity = 14f * nivel / columnas.Length;
        luz.enabled = nivel > .01f;
    }

    void Aplicar()
    {
        AplicarTallado(eclipse);
        AplicarTallado(eclipseRegreso);
        if (luz != null) { luz.enabled = brillo > 0.01f; luz.intensity = intensidadLuz * brillo; }
    }
    void AplicarTallado(Renderer tallado)
    {
        if (tallado == null) return;
        bloque ??= new MaterialPropertyBlock();
        tallado.GetPropertyBlock(bloque);
        bloque.SetColor(IdEmision, colorEclipse * Mathf.Lerp(emisionApagado, 3f, brillo));
        bloque.SetColor("_BaseColor", colorEclipse * Mathf.Lerp(.08f, .75f, brillo));
        tallado.SetPropertyBlock(bloque);
    }

}
