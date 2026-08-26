using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Clase base de todo lo que reacciona al haz. No se usa sola:
/// se usan sus hijas (Ancla, MateriaHueca, RastroFosforescente).
///
/// Se encarga de: filtrar por canal, acumular carga, y avisar cuando se pierde la luz.
/// </summary>
public abstract class ReceptorDeLuz : MonoBehaviour
{
    [Header("Receptor")]
    [Tooltip("Que filtro lo activa. Ninguno = cualquiera.")]
    public FiltroDefinicion.Canal canalRequerido = FiltroDefinicion.Canal.Cuerpo;

    [Tooltip("Segundos que sigue activo despues de perder el haz. 0 = se apaga al instante.")]
    public float retencion = 0f;

    [Header("Eventos (opcional, para conectar sin codigo)")]
    public UnityEvent alActivarse;
    public UnityEvent alDesactivarse;

    public bool Activo { get; private set; }
    public float Carga { get; private set; }          // 0 a 1

    float ultimoFrameConLuz = -999f;
    float tiempoDeCargaActual = 0.35f;

    /// <summary>Punto que la linterna apunta para el chequeo de cono y linea de vista.</summary>
    public virtual Vector3 PuntoDeImpacto =>
        GetComponent<Collider>() ? GetComponent<Collider>().bounds.center : transform.position;

    public virtual void RecibirLuz(FiltroDefinicion filtro, float delta)
    {
        if (canalRequerido != FiltroDefinicion.Canal.Ninguno)
        {
            if (filtro == null || filtro.canal != canalRequerido) return;
        }

        tiempoDeCargaActual = (filtro != null) ? Mathf.Max(0.01f, filtro.tiempoDeCarga) : 0.35f;
        ultimoFrameConLuz = Time.time;
        Carga = Mathf.Clamp01(Carga + delta / tiempoDeCargaActual);

        if (!Activo && Carga >= 1f) Activar();
    }

    protected virtual void Update()
    {
        bool recibiendo = (Time.time - ultimoFrameConLuz) < 0.05f;

        if (!recibiendo)
        {
            // se descarga al doble de velocidad de lo que carga
            Carga = Mathf.Clamp01(Carga - Time.deltaTime / (tiempoDeCargaActual * 0.5f));

            if (Activo && (Time.time - ultimoFrameConLuz) > retencion && Carga <= 0f)
                Desactivar();
        }

        AlActualizar(recibiendo);
    }

    /// <summary>Gancho para las hijas. Se llama todos los frames.</summary>
    protected virtual void AlActualizar(bool recibiendoLuz) { }

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
}
