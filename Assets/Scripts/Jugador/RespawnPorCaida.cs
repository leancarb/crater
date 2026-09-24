using UnityEngine;

/// <summary>
/// Devuelve al jugador al último suelo estable cuando cae fuera del recorrido.
/// Los puentes de luz no se guardan como punto seguro porque pueden desaparecer.
///
/// CÓMO FUNCIONA
/// Mientras el jugador está parado sobre algo firme (no un puente) durante medio
/// segundo, guarda esa posición como "punto seguro" (cada 0,35 s como máximo).
/// Si cae por debajo de 'alturaDeCaida', lo teletransporta al último punto seguro
/// y muestra un destello negro con un aviso.
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
    float tiempoEnSuelo;       // cuánto hace que está parado en el piso sin interrupción
    float proximoRegistro;     // no guardar más seguido que 'intervaloDeRegistro'
    readonly RaycastHit[] impactos = new RaycastHit[8];

    public Vector3 UltimoPuntoSeguro => ultimoPuntoSeguro;

    void Awake()
    {
        controlador = GetComponent<CharacterController>();
        movimiento = GetComponent<JugadorFPS>();
        RegistrarPuntoSeguro(transform.position, transform.rotation);
    }

    void Update()
    {
        // se cayó del recorrido
        if (transform.position.y < alturaDeCaida)
        {
            Respawn();
            return;
        }

        // en el aire (o durante una cinemática) no se cuenta tiempo en el piso
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

    /// <summary>Tira un rayo hacia abajo y revisa qué hay debajo: un puente no cuenta como firme.</summary>
    bool EstaSobreSuperficiePermanente()
    {
        Vector3 origen = transform.position + Vector3.up * 0.3f;
        int cantidad = Physics.RaycastNonAlloc(origen, Vector3.down, impactos, 1.5f,
            ~0, QueryTriggerInteraction.Ignore);

        // RaycastNonAlloc no ordena por distancia: el suelo es el impacto más cercano
        Collider suelo = null;
        float distancia = float.MaxValue;
        for (int i = 0; i < cantidad; i++)
        {
            Collider c = impactos[i].collider;
            if (c == null || c.transform.IsChildOf(transform)) continue;
            if (impactos[i].distance < distancia)
            {
                distancia = impactos[i].distance;
                suelo = c;
            }
        }
        return suelo != null && suelo.GetComponentInParent<PuenteLuz>() == null;
    }

    public void RegistrarPuntoSeguro(Vector3 posicion, Quaternion rotacion)
    {
        ultimoPuntoSeguro = posicion;
        ultimaRotacionSegura = Quaternion.Euler(0f, rotacion.eulerAngles.y, 0f);
    }

    /// <summary>También puede llamarse desde volúmenes de muerte.</summary>
    public void Respawn()
    {
        // el CharacterController pisa cualquier cambio de posición mientras está prendido:
        // se apaga, se mueve el objeto y se vuelve a prender
        bool estabaHabilitado = controlador != null && controlador.enabled;
        if (controlador != null) controlador.enabled = false;

        transform.SetPositionAndRotation(ultimoPuntoSeguro + Vector3.up * 0.15f, ultimaRotacionSegura);
        if (movimiento != null)
        {
            movimiento.ReiniciarMovimiento();
            movimiento.Orientar(ultimaRotacionSegura);
        }
        Physics.SyncTransforms();   // que la física se entere de la posición nueva ya

        if (controlador != null) controlador.enabled = estabaHabilitado;
        tiempoEnSuelo = 0f;

        if (interfaz != null)
        {
            interfaz.Destello(Color.black, 0.9f);
            interfaz.MostrarPromptTemporal("Volviste al último punto seguro.", 2.5f);
        }
    }
}
