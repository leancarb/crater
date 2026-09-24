using System.Collections;
using UnityEngine;

/// <summary>
/// Autoridad de progresión de la demo. Escucha a la linterna y a las zonas,
/// avanza de etapa y decide qué indicación mostrar en cada momento.
///
/// CÓMO FUNCIONA
/// Es una máquina de estados: 'EtapaActual' dice en qué parte del juego está el
/// jugador. Se suscribe a los eventos de la linterna (recoger, encender, desbloquear
/// y equipar filtros) y cada evento puede hacer avanzar la etapa y mostrar una
/// indicación. Las zonas y la compuerta llaman a sus métodos públicos a través de
/// UnityEvents conectados por el constructor (EntrarCresta, NotificarUmbralAbierto...).
/// El orden del enum importa: se compara con < y >= (por ejemplo "antes de la Cresta").
/// </summary>
public class FlujoJuegoCrater : MonoBehaviour
{
    public enum Etapa
    {
        Prologo,
        BuscarLinterna,
        EncenderLinterna,
        AbrirUmbral,
        BuscarCuerpo,
        UsarCuerpo,
        BuscarHueco,
        UsarHueco,
        Cresta,
        Epilogo,
        Finalizado
    }

    [SerializeField] LinternaController linterna;
    [SerializeField] InterfazCrater interfaz;
    [SerializeField] EclipseFinalController eclipse;
    [SerializeField] Compuerta compuertaUmbral;
    [SerializeField] bool mostrarTitulo = true;
    [Tooltip("Arrancar en la capilla (prólogo). En false arranca directo en la Explanada.")]
    [SerializeField] bool empezarEnPrologo = true;

    public Etapa EtapaActual { get; private set; } = Etapa.BuscarLinterna;

    void Awake()
    {
        if (empezarEnPrologo) EtapaActual = Etapa.Prologo;
    }

    bool vinculado;            // ya se suscribió a los eventos de la linterna
    bool pistaHuecoMostrada;   // la explicación de HUECO sale una sola vez

    void Start()
    {
        Vincular();
        StartCoroutine(Inicio());
    }

    void OnDestroy() => Desvincular();

    IEnumerator Inicio()
    {
        if (mostrarTitulo && interfaz != null) yield return interfaz.MostrarTitulo();
        // en el prólogo no hay indicaciones: la capilla se descubre sola
        if (EtapaActual == Etapa.BuscarLinterna)
            interfaz?.MostrarPromptTemporal("Bajá hacia la luz.", 5f);
    }

    /// <summary>El jugador cruzó la puerta del cráter del valle y está en la Explanada.</summary>
    public void IniciarCrater()
    {
        if (EtapaActual != Etapa.Prologo) return;
        EtapaActual = Etapa.BuscarLinterna;
        interfaz?.MostrarPromptTemporal("Bajá hacia la luz.", 5f);
    }

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

    void AlRecogerLinterna()
    {
        EtapaActual = Etapa.EncenderLinterna;
        interfaz?.MostrarPrompt("F · Encender la linterna");
    }

    void AlCambiarEncendido(bool encendida)
    {
        if (EtapaActual == Etapa.EncenderLinterna && encendida)
        {
            if (compuertaUmbral != null && compuertaUmbral.Abierta)
            {
                AvanzarABuscarCuerpo();
                return;
            }
            EtapaActual = Etapa.AbrirUmbral;
            interfaz?.MostrarPrompt("Sostené la luz sobre el ancla para abrir el paso.");
        }
        else if (EtapaActual == Etapa.Cresta)
        {
            if (encendida) interfaz?.MostrarPrompt("F · Apagar la linterna");
            else interfaz?.MostrarPromptTemporal("Esperá. Dejá que tus ojos se acostumbren.", 5f);
        }
    }

    /// <summary>Conectado al evento 'alAbrirse' de la compuerta del Umbral.</summary>
    public void NotificarUmbralAbierto()
    {
        if (EtapaActual <= Etapa.AbrirUmbral) AvanzarABuscarCuerpo();
    }

    void AvanzarABuscarCuerpo()
    {
        EtapaActual = Etapa.BuscarCuerpo;
        interfaz?.MostrarPromptTemporal("El paso está abierto. Buscá el filtro ámbar.", 5f);
    }

    void AlDesbloquearFiltro(FiltroDefinicion filtro)
    {
        if (filtro == null) return;
        if (filtro.canal == FiltroDefinicion.Canal.Cuerpo)
        {
            EtapaActual = Etapa.UsarCuerpo;
            interfaz?.MostrarPrompt("1 · Equipar CUERPO");
        }
        else if (filtro.canal == FiltroDefinicion.Canal.Hueco)
        {
            EtapaActual = Etapa.UsarHueco;
            interfaz?.MostrarPrompt("2 · Equipar HUECO");
        }
    }

    void AlEquiparFiltro(FiltroDefinicion filtro)
    {
        if (filtro == null) return;

        if (filtro.canal == FiltroDefinicion.Canal.Cuerpo && EtapaActual == Etapa.UsarCuerpo)
        {
            EtapaActual = Etapa.BuscarHueco;
            interfaz?.MostrarPromptTemporal(
                "CUERPO enciende las anclas.\nSostené el haz sobre las dos para tender el puente.", 7f);
        }
        else if (filtro.canal == FiltroDefinicion.Canal.Hueco && EtapaActual == Etapa.UsarHueco && !pistaHuecoMostrada)
        {
            pistaHuecoMostrada = true;
            interfaz?.MostrarPromptTemporal(
                "HUECO disuelve la materia: iluminala y atravesala.\n1 / 2 · Cambiar filtro     Q · Luz blanca", 7f);
        }
    }

    /// <summary>Conectado a la zona de entrada de la Cresta.</summary>
    public void EntrarCresta()
    {
        if (EtapaActual >= Etapa.Cresta) return;
        EtapaActual = Etapa.Cresta;
        if (linterna != null && linterna.Encendida) interfaz?.MostrarPrompt("F · Apagar la linterna");
        else interfaz?.MostrarPromptTemporal("Esperá. Dejá que tus ojos se acostumbren.", 5f);
        eclipse?.HabilitarEnCresta();
    }

    public void EntrarEpilogo() => EtapaActual = Etapa.Epilogo;

    public void Finalizar() => EtapaActual = Etapa.Finalizado;
}
