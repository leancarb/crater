using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger generico: dispara un evento cuando el jugador entra a la zona.
/// Poner en: cualquier objeto con Collider marcado como Is Trigger.
/// </summary>
public class DisparadorSimple : MonoBehaviour
{
    public UnityEvent alEntrar;
    [Tooltip("Si esta tildado, solo dispara la primera vez.")]
    public bool unaVez = true;

    bool disparado;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (unaVez && disparado) return;

        disparado = true;
        alEntrar?.Invoke();
    }
}
