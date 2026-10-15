using UnityEngine;
using UnityEngine.Events;

/// <summary>Atajo invisible fuera del camino inicial. Sólo acorta la caminata;
/// conserva el mirador, su eclipse y la bajada normal. Se reinicia con la escena.</summary>
[RequireComponent(typeof(BoxCollider))]
public class AtajoOcultoInicio : MonoBehaviour
{
    [Tooltip("Desactivado para el playtest; habilitar sólo para pruebas del recorrido.")]
    public bool habilitado;
    public JugadorFPS jugador;
    public Transform destino;
    public FlujoJuegoCrater flujo;
    public UnityEvent alLlegar = new UnityEvent();
    public bool Usado { get; private set; }

    void OnTriggerEnter(Collider other) => Intentar(other);
    void OnTriggerStay(Collider other) => Intentar(other);

    void Intentar(Collider other)
    {
        if (!habilitado || Usado || PausaCrater.EnPausa || !other.CompareTag("Player") ||
            jugador == null || destino == null || flujo == null ||
            flujo.EtapaActual != FlujoJuegoCrater.Etapa.Prologo) return;
        var controlador = jugador.GetComponent<CharacterController>();
        if (controlador == null || !controlador.enabled) return;
        Usado = true;
        controlador.enabled = false;
        jugador.transform.position = destino.position;
        jugador.ReiniciarMovimiento();
        controlador.enabled = true;
        Physics.SyncTransforms();
        alLlegar.Invoke();
    }
}
