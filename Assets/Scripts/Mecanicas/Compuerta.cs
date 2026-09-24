using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Losa de piedra que se hunde en el piso cuando todos sus receptores están
/// activos. Una vez abierta queda abierta.
///
/// Poner en: la compuerta (con su Collider). 'desplazamiento' es cuánto se mueve al abrirse.
///
/// CÓMO FUNCIONA
/// Cada frame pregunta si todos los receptores de la lista están 'Activo'. La primera
/// vez que se cumple llama a Abrir(), que dispara el sonido y el evento 'alAbrirse'
/// (el FlujoJuegoCrater lo escucha para avanzar de etapa). Desde ahí anima la losa
/// hacia 'origen + desplazamiento' durante 'duracion' segundos.
/// En el juego la usa el Umbral, con un ancla que se abre con luz blanca.
/// </summary>
public class Compuerta : MonoBehaviour
{
    public List<ReceptorDeLuz> receptores = new List<ReceptorDeLuz>();
    public Vector3 desplazamiento = new Vector3(0f, -4.4f, 0f);
    public float duracion = 3f;

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
    MaterialPropertyBlock bloque;

    void Awake() => GuardarOrigen();

    // puede llamarse desde Awake o desde Abrir (en los tests, Abrir puede llegar antes que Awake)
    void GuardarOrigen()
    {
        if (origenGuardado) return;
        origen = transform.localPosition;
        origenGuardado = true;
    }

    void Update() => Avanzar(Time.deltaTime);

    /// <summary>Un paso de simulación (público para los tests).</summary>
    public void Avanzar(float delta)
    {
        // TrueForAll: todos los receptores tienen que estar encendidos a la vez
        if (!Abierta && receptores.Count > 0 && receptores.TrueForAll(r => r != null && r.Activo))
            Abrir();
        if (!Abierta || Progreso >= 1f) return;

        Progreso = Mathf.MoveTowards(Progreso, 1f, delta / Mathf.Max(0.01f, duracion));
        // un temblor al arrancar, después se hunde parejo
        float temblor = Progreso < 0.15f ? Mathf.Sin(Time.time * 60f) * 0.015f : 0f;
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
