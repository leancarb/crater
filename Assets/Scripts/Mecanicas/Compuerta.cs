using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Losa de piedra que se hunde en el piso cuando todos sus receptores están
/// activos. Una vez abierta queda abierta.
///
/// Poner en: la compuerta (con su Collider). 'desplazamiento' es cuánto se mueve al abrirse.
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
    public float Progreso { get; private set; }

    Vector3 origen;
    bool origenGuardado;
    MaterialPropertyBlock bloque;

    void Awake() => GuardarOrigen();

    void GuardarOrigen()
    {
        if (origenGuardado) return;
        origen = transform.localPosition;
        origenGuardado = true;
    }

    void Update() => Avanzar(Time.deltaTime);

    public void Avanzar(float delta)
    {
        if (!Abierta && receptores.Count > 0 && receptores.TrueForAll(r => r != null && r.Activo))
            Abrir();
        if (!Abierta || Progreso >= 1f) return;

        Progreso = Mathf.MoveTowards(Progreso, 1f, delta / Mathf.Max(0.01f, duracion));
        // un temblor al arrancar, después se hunde parejo
        float temblor = Progreso < 0.15f ? Mathf.Sin(Time.time * 60f) * 0.015f : 0f;
        transform.localPosition = origen + desplazamiento * Mathf.SmoothStep(0f, 1f, Progreso) + Vector3.right * temblor;

        if (acentos == null) return;
        bloque ??= new MaterialPropertyBlock();
        foreach (var r in acentos)
        {
            if (r == null) continue;
            r.GetPropertyBlock(bloque);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (var r in receptores)
            if (r != null) Gizmos.DrawLine(transform.position, r.PuntoDeImpacto);
    }
}
