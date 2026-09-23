using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger genérico: dispara un evento cuando el jugador entra a la zona.
/// Poner en: cualquier objeto con Collider marcado como Is Trigger, y conectar
/// 'alEntrar' desde el Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaJugador : MonoBehaviour
{
    [Tooltip("Si está tildado, sólo dispara la primera vez.")]
    public bool unaVez = true;
    public UnityEvent alEntrar = new UnityEvent();

    public bool Usada { get; private set; }

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
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
