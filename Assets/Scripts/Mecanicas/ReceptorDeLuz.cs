using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Clase base de todo lo que reacciona al haz. No se usa sola:
/// se usan sus hijas (Ancla y MateriaHueca).
///
/// Se encarga de: filtrar por canal, acumular carga, retener la activación
/// unos segundos después de perder la luz, y avisar cuando cambia de estado.
///
/// CÓMO FUNCIONA
///  1. La linterna, cada frame, llama a RecibirLuz() en cada receptor que ilumina.
///  2. Si el filtro es el correcto, la 'Carga' sube de 0 a 1 en 'tiempoDeCarga' segundos.
///  3. Al llegar a 1 se activa (Activo = true) y dispara 'alActivarse'.
///  4. Si deja de recibir luz, primero espera 'retencion' segundos (sigue activo),
///     después la carga baja al doble de velocidad y, en 0, se desactiva.
/// Las hijas redefinen AlActualizar() para dibujarse (brillo, transparencia, etc.).
/// Es 'abstract': Unity no deja ponerla sola en un objeto.
/// </summary>
public abstract class ReceptorDeLuz : MonoBehaviour
{
    public const float TiempoDeCargaLuzBlanca = 0.35f;

    [Header("Receptor")]
    [Tooltip("Qué filtro lo activa. Ninguno = cualquier luz, incluso la blanca.")]
    public FiltroDefinicion.Canal canalRequerido = FiltroDefinicion.Canal.Cuerpo;

    [Tooltip("Segundos que sigue activo después de perder el haz. 0 = se apaga al instante.")]
    [Min(0f)] public float retencion = 0f;

    [Tooltip("Punto al que apunta la linterna para el cono y la línea de vista. Vacío = el propio objeto.")]
    public Transform puntoDeImpacto;

    [Header("Eventos (opcional, para conectar sin código)")]
    // se pueden conectar desde el Inspector a cualquier método público (como un botón de UI)
    public UnityEvent alActivarse = new UnityEvent();
    public UnityEvent alDesactivarse = new UnityEvent();

    // estado que leen las hijas, la interfaz y los puentes/compuertas
    public bool Activo { get; private set; }
    public float Carga { get; private set; }          // 0 a 1
    public bool Recibiendo { get; private set; }      // recibió luz en el último paso

    /// <summary>1 mientras recibe luz o recién la pierde, baja a 0 cuando se agota la retención.</summary>
    public float RetencionRestante
    {
        get
        {
            if (!Activo) return 0f;
            if (Recibiendo || retencion <= 0f) return 1f;
            return Mathf.Clamp01(1f - sinLuz / retencion);
        }
    }

    public Vector3 PuntoDeImpacto => puntoDeImpacto != null ? puntoDeImpacto.position : transform.position;

    float tiempoDeCarga = TiempoDeCargaLuzBlanca;   // se toma del último filtro que lo iluminó
    float sinLuz;                                   // segundos desde que perdió el haz
    bool luzPendiente;                              // la linterna lo iluminó desde el último Avanzar()

    /// <summary>¿Este filtro (o la luz blanca, si es null) activa a este receptor?</summary>
    public bool AceptaFiltro(FiltroDefinicion filtro)
    {
        return canalRequerido == FiltroDefinicion.Canal.Ninguno
            || (filtro != null && filtro.canal == canalRequerido);
    }

    /// <summary>La linterna lo llama cada frame que este receptor está dentro del haz.</summary>
    public void RecibirLuz(FiltroDefinicion filtro, float delta)
    {
        if (!AceptaFiltro(filtro)) return;

        tiempoDeCarga = filtro != null ? Mathf.Max(0.01f, filtro.tiempoDeCarga) : TiempoDeCargaLuzBlanca;
        luzPendiente = true;
        sinLuz = 0f;
        // delta / tiempoDeCarga: en 'tiempoDeCarga' segundos de luz la carga llega a 1
        Carga = Mathf.Clamp01(Carga + delta / tiempoDeCarga);

        if (!Activo && Carga >= 1f) Activar();
    }

    protected virtual void Update() => Avanzar(Time.deltaTime);

    /// <summary>
    /// Un paso de simulación. Se usa una bandera en vez del reloj para saber si
    /// recibió luz: así no depende del orden de ejecución ni de los FPS.
    /// Es público para que los tests puedan simular pasos sin esperar frames.
    /// </summary>
    public void Avanzar(float delta)
    {
        // "recibí luz desde el paso anterior" pasa a ser el estado de este paso
        Recibiendo = luzPendiente;
        luzPendiente = false;

        if (!Recibiendo)
        {
            sinLuz += delta;
            // mientras dure la retención, un receptor activo no pierde carga
            bool retenido = Activo && sinLuz < retencion;
            if (!retenido)
            {
                // se descarga al doble de velocidad de lo que carga
                Carga = Mathf.Clamp01(Carga - delta / (tiempoDeCarga * 0.5f));
                if (Activo && Carga <= 0f) Desactivar();
            }
        }

        AlActualizar(delta);
    }

    /// <summary>Gancho para las hijas. Se llama todos los frames.</summary>
    protected virtual void AlActualizar(float delta) { }

    // 'virtual': las hijas pueden sumar comportamiento (por ejemplo un sonido) llamando a base.Activar()
    protected virtual void Activar()
    {
        Activo = true;
        alActivarse?.Invoke();
    }

    protected virtual void Desactivar()
    {
        Activo = false;
        alDesactivarse?.Invoke();
    }

    // dibuja en la vista Scene (sólo en el editor) el punto al que apunta la linterna
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(PuntoDeImpacto, 0.15f);
    }
}
