using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Losa de piedra que se hunde en el piso cuando todos sus receptores están
/// activos. Una vez abierta queda abierta, salvo que sea 'sostenida': esa se
/// abre sólo mientras sus receptores sigan activos (la retención de las anclas
/// da unos segundos para pasar) y nunca se cierra encima del jugador.
///
/// Poner en: la compuerta (con su Collider). 'desplazamiento' es cuánto se mueve al abrirse.
///
/// CÓMO FUNCIONA
/// Cada frame pregunta si todos los receptores de la lista están 'Activo'. La primera
/// vez que se cumple llama a Abrir(), que dispara el sonido y el evento 'alAbrirse'
/// (el FlujoJuegoCrater lo escucha para avanzar de etapa). Desde ahí anima la losa
/// hacia 'origen + desplazamiento' durante 'duracion' segundos.
/// Sostenida, Progreso va hacia 1 o hacia 0 según el estado de los receptores; antes de
/// cerrarse revisa (CheckBox) que el jugador no esté parado en el paso.
/// </summary>
public class Compuerta : MonoBehaviour
{
    public List<ReceptorDeLuz> receptores = new List<ReceptorDeLuz>();
    public Vector3 desplazamiento = new Vector3(0f, -4.4f, 0f);
    public float duracion = 3f;
    [Tooltip("Abierta sólo mientras sus receptores estén activos.")]
    public bool sostenida;
    [Tooltip("Capa del jugador, para no cerrarse encima de él (sostenida).")]
    public LayerMask capaJugador;

    [Header("Aspecto y sonido")]
    [Tooltip("Tallados que se encienden al abrirse.")]
    public Renderer[] acentos;
    public Color colorAcento = new Color(1f, 0.45f, 0.1f);
    public AudioSource sonido;

    [Header("Eventos")]
    public UnityEvent alAbrirse = new UnityEvent();

    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    public bool Abierta { get; private set; }
    public float Progreso { get; private set; }   // 0 = cerrada, 1 = terminó de hundirse

    Vector3 origen;            // posición cerrada, guardada una vez
    bool origenGuardado;
    Bounds paso;               // el volumen que ocupa la losa cerrada (sostenida)
    MaterialPropertyBlock bloque;

    void Awake() => GuardarOrigen();

    // puede llamarse desde Awake o desde Abrir (en los tests, Abrir puede llegar antes que Awake)
    void GuardarOrigen()
    {
        if (origenGuardado) return;
        origen = transform.localPosition;
        origenGuardado = true;

        // el paso: lo que ocupa la losa cerrada, un poco más grande
        paso = new Bounds(transform.position, Vector3.zero);
        bool primero = true;
        foreach (var c in GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger) continue;
            if (primero) { paso = c.bounds; primero = false; }
            else paso.Encapsulate(c.bounds);
        }
        paso.Expand(new Vector3(0.6f, 0f, 0.6f));
    }

    void Update() => Avanzar(Time.deltaTime);

    /// <summary>Un paso de simulación (público para los tests).</summary>
    public void Avanzar(float delta)
    {
        // TrueForAll: todos los receptores tienen que estar encendidos a la vez
        bool encendidos = receptores.Count > 0 && receptores.TrueForAll(r => r != null && r.Activo);
        if (!Abierta && encendidos) Abrir();
        // sostenida: se cierra al apagarse, salvo que el jugador esté en el paso
        if (sostenida && Abierta && !encendidos && !JugadorEnElPaso()) Abierta = false;

        float objetivo = Abierta ? 1f : 0f;
        if (Mathf.Approximately(Progreso, objetivo)) return;

        Progreso = Mathf.MoveTowards(Progreso, objetivo, delta / Mathf.Max(0.01f, duracion));
        // un temblor al arrancar a abrirse, después se hunde parejo (al cerrarse, sin temblor)
        float temblor = Abierta && Progreso < 0.15f ? Mathf.Sin(Time.time * 60f) * 0.015f : 0f;
        // SmoothStep: arranca y frena suave, en vez de moverse a velocidad constante
        transform.localPosition = origen + desplazamiento * Mathf.SmoothStep(0f, 1f, Progreso) + Vector3.right * temblor;

        if (acentos == null) return;
        bloque ??= new MaterialPropertyBlock();
        foreach (var r in acentos)
        {
            if (r == null) continue;
            r.GetPropertyBlock(bloque);
            // los tallados brillan más a mitad del recorrido: sin(0..π) sube y baja
            bloque.SetColor(IdEmision, colorAcento * Mathf.Lerp(0.3f, 4f, Mathf.Sin(Progreso * Mathf.PI)));
            r.SetPropertyBlock(bloque);
        }
    }

    bool JugadorEnElPaso()
    {
        int capa = capaJugador.value != 0 ? capaJugador.value : LayerMask.GetMask("Jugador");
        return Physics.CheckBox(paso.center, paso.extents, Quaternion.identity, capa, QueryTriggerInteraction.Ignore);
    }

    public void Abrir()
    {
        if (Abierta) return;
        GuardarOrigen();
        Abierta = true;
        if (sonido != null) sonido.Play();
        alAbrirse?.Invoke();
    }

    // en la vista Scene, líneas verdes hacia cada receptor que la abre
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (var r in receptores)
            if (r != null) Gizmos.DrawLine(transform.position, r.PuntoDeImpacto);
    }
}
