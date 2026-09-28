using UnityEngine;

/// <summary>
/// Autoridad de progresión de la demo. Escucha a la linterna y a las zonas y
/// avanza de etapa. En esta versión no muestra indicaciones: la etapa actual se
/// puede ver en el Inspector (objeto SISTEMAS) mientras se juega.
///
/// CÓMO FUNCIONA
/// Es una máquina de estados: 'EtapaActual' dice en qué parte del juego está el
/// jugador. Se suscribe a los eventos de la linterna (recoger, encender, desbloquear
/// y equipar filtros) y cada evento puede hacer avanzar la etapa. Las zonas y la
/// compuerta llaman a sus métodos públicos a través de UnityEvents conectados por
/// el constructor (EntrarCresta, NotificarUmbralAbierto).
/// El orden del enum importa: se compara con <= y >= (por ejemplo "antes de la Cresta").
/// </summary>
public class FlujoJuegoCrater : MonoBehaviour
{
    public enum Etapa
    {
        BuscarLinterna,
        EncenderLinterna,
        AbrirUmbral,
        BuscarCuerpo,
        UsarCuerpo,
        BuscarHueco,
        UsarHueco,
        Cresta,
        Finalizado
    }

    [SerializeField] LinternaController linterna;
    [SerializeField] EclipseFinalController eclipse;
    [SerializeField] Compuerta compuertaUmbral;

    [Tooltip("Sólo lectura: se ve cómo avanza mientras se juega.")]
    [SerializeField] Etapa etapaVisible;

    public Etapa EtapaActual
    {
        get => etapa;
        private set { etapa = value; etapaVisible = value; }
    }

    Etapa etapa = Etapa.BuscarLinterna;
    bool vinculado;   // ya se suscribió a los eventos de la linterna

    void Start() => Vincular();

    void OnDestroy() => Desvincular();

    /// <summary>
    /// Se suscribe a los eventos de la linterna con +=. Hay que desuscribirse (-=) al
    /// destruirse; si no, la linterna seguiría llamando a un objeto que ya no existe.
    /// </summary>
    void Vincular()
    {
        if (linterna == null || vinculado) return;
        linterna.AlRecogerLinterna += AlRecogerLinterna;
        linterna.AlCambiarEncendido += AlCambiarEncendido;
        linterna.AlDesbloquearFiltro += AlDesbloquearFiltro;
        linterna.AlEquiparFiltro += AlEquiparFiltro;
        vinculado = true;
    }

    void Desvincular()
    {
        if (linterna == null || !vinculado) return;
        linterna.AlRecogerLinterna -= AlRecogerLinterna;
        linterna.AlCambiarEncendido -= AlCambiarEncendido;
        linterna.AlDesbloquearFiltro -= AlDesbloquearFiltro;
        linterna.AlEquiparFiltro -= AlEquiparFiltro;
        vinculado = false;
    }

    void AlRecogerLinterna() => EtapaActual = Etapa.EncenderLinterna;

    void AlCambiarEncendido(bool encendida)
    {
        if (EtapaActual != Etapa.EncenderLinterna || !encendida) return;
        // si la compuerta ya estaba abierta (no debería, pero por las dudas) se saltea esa etapa
        EtapaActual = compuertaUmbral != null && compuertaUmbral.Abierta ? Etapa.BuscarCuerpo : Etapa.AbrirUmbral;
    }

    /// <summary>Conectado al evento 'alAbrirse' de la compuerta del Umbral.</summary>
    public void NotificarUmbralAbierto()
    {
        if (EtapaActual <= Etapa.AbrirUmbral) EtapaActual = Etapa.BuscarCuerpo;
    }

    void AlDesbloquearFiltro(FiltroDefinicion filtro)
    {
        if (filtro == null) return;
        if (filtro.canal == FiltroDefinicion.Canal.Cuerpo) EtapaActual = Etapa.UsarCuerpo;
        else if (filtro.canal == FiltroDefinicion.Canal.Hueco) EtapaActual = Etapa.UsarHueco;
    }

    void AlEquiparFiltro(FiltroDefinicion filtro)
    {
        // equipar CUERPO por primera vez: lo siguiente es buscar HUECO
        if (filtro != null && filtro.canal == FiltroDefinicion.Canal.Cuerpo && EtapaActual == Etapa.UsarCuerpo)
            EtapaActual = Etapa.BuscarHueco;
    }

    /// <summary>Conectado a la zona de entrada de la Cresta: ahí empieza la adaptación.</summary>
    public void EntrarCresta()
    {
        if (EtapaActual >= Etapa.Cresta) return;
        EtapaActual = Etapa.Cresta;
        eclipse?.HabilitarEnCresta();
    }

    public void Finalizar() => EtapaActual = Etapa.Finalizado;
}
