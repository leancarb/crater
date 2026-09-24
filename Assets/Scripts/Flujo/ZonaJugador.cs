using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger genérico: dispara un evento cuando el jugador entra a la zona,
/// o cuando se queda adentro 'segundosDePermanencia' sin salir.
/// Poner en: cualquier objeto con Collider marcado como Is Trigger, y conectar
/// 'alEntrar' desde el Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaJugador : MonoBehaviour
{
    [Tooltip("Si está tildado, sólo dispara la primera vez.")]
    public bool unaVez = true;
    [Tooltip("0 = al entrar. Más = hay que quedarse adentro ese tiempo.")]
    public float segundosDePermanencia = 0f;
    public UnityEvent alEntrar = new UnityEvent();

    public bool Usada { get; private set; }

    bool dentro;
    float tiempoDentro;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        dentro = true;
        tiempoDentro = 0f;
        if (segundosDePermanencia <= 0f) Disparar();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) dentro = false;
    }

    void OnDisable() => dentro = false;

    void Update()
    {
        if (!dentro || segundosDePermanencia <= 0f) return;
        tiempoDentro += Time.deltaTime;
        if (tiempoDentro >= segundosDePermanencia)
        {
            tiempoDentro = 0f;
            Disparar();
        }
    }

    void Disparar()
    {
        if (unaVez && Usada) return;
        Usada = true;
        alEntrar?.Invoke();
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
