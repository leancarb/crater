using UnityEngine;

/// <summary>
/// Devuelve al jugador al ultimo suelo estable cuando cae fuera del recorrido.
/// Los puentes de luz no se guardan como punto seguro porque pueden desaparecer.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class RespawnPorCaida : MonoBehaviour
{
    [SerializeField] float alturaDeCaida = -10f;
    [SerializeField] float tiempoEstableRequerido = 0.5f;
    [SerializeField] float intervaloDeRegistro = 0.35f;
    [SerializeField] InterfazCrater interfaz;

    CharacterController controlador;
    JugadorFPS movimiento;
    Vector3 ultimoPuntoSeguro;
    Quaternion ultimaRotacionSegura;
    float tiempoEnSuelo;
    float proximoRegistro;
    readonly RaycastHit[] impactos = new RaycastHit[8];

    public Vector3 UltimoPuntoSeguro => ultimoPuntoSeguro;

    void Awake()
    {
        controlador = GetComponent<CharacterController>();
        movimiento = GetComponent<JugadorFPS>();
        RegistrarPuntoSeguro(transform.position, transform.rotation);
    }

    public void Configurar(InterfazCrater nuevaInterfaz)
    {
        interfaz = nuevaInterfaz;
    }

    void Update()
    {
        if (transform.position.y < alturaDeCaida)
        {
            Respawn();
            return;
        }

        if (controlador == null || !controlador.enabled || !controlador.isGrounded)
        {
            tiempoEnSuelo = 0f;
            return;
        }

        tiempoEnSuelo += Time.deltaTime;
        if (tiempoEnSuelo < tiempoEstableRequerido || Time.time < proximoRegistro) return;
        if (!EstaSobreSuperficiePermanente()) return;

        RegistrarPuntoSeguro(transform.position, transform.rotation);
        proximoRegistro = Time.time + intervaloDeRegistro;
    }

    bool EstaSobreSuperficiePermanente()
    {
        Vector3 origen = transform.position + Vector3.up * 0.3f;
        int cantidad = Physics.RaycastNonAlloc(origen, Vector3.down, impactos, 1.5f,
            ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < cantidad; i++)
        {
            Collider superficie = impactos[i].collider;
            if (superficie == null || superficie.transform.IsChildOf(transform)) continue;
            if (superficie.GetComponentInParent<PuenteLuz>() != null) return false;
            return true;
        }
        return false;
    }

    public void RegistrarPuntoSeguro(Vector3 posicion, Quaternion rotacion)
    {
        ultimoPuntoSeguro = posicion;
        ultimaRotacionSegura = Quaternion.Euler(0f, rotacion.eulerAngles.y, 0f);
    }

    /// <summary>Tambien puede llamarse desde futuros volumenes de muerte.</summary>
    public void Respawn()
    {
        bool estabaHabilitado = controlador != null && controlador.enabled;
        if (controlador != null) controlador.enabled = false;

        transform.SetPositionAndRotation(ultimoPuntoSeguro + Vector3.up * 0.15f, ultimaRotacionSegura);
        movimiento?.ReiniciarMovimiento();
        Physics.SyncTransforms();

        if (controlador != null) controlador.enabled = estabaHabilitado;
        tiempoEnSuelo = 0f;
        interfaz?.MostrarPromptTemporal("Volviste al ultimo punto seguro.", 2f);
    }
}
