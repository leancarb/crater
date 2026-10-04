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
/// Desde la rotonda las dos alas se hacen en cualquier orden: las indicaciones de cada
/// filtro salen al juntarlo y al usarlo, no según la etapa.
/// </summary>
public class FlujoJuegoCrater : MonoBehaviour
{
    public enum Etapa
    {
        Prologo,
        BuscarLinterna,
        EncenderLinterna,
        AbrirUmbral,
        Explorar,     // la rotonda y las dos alas, en cualquier orden
        Cruce,        // los dos sellos abrieron el paso del norte
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
    int sellos;                // cuántos sellos se encendieron

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
        // los controles los enseñan los murales, sin texto (ConstructorCrater.Murales.cs)
        EtapaActual = Etapa.EncenderLinterna;
        interfaz?.OcultarPrompt();
    }

    void AlCambiarEncendido(bool encendida)
    {
        if (EtapaActual == Etapa.EncenderLinterna && encendida)
        {
            if (compuertaUmbral != null && compuertaUmbral.Abierta)
            {
                AvanzarAExplorar();
                return;
            }
            EtapaActual = Etapa.AbrirUmbral;
        }
        else if (EtapaActual == Etapa.Cresta)
        {
            if (!encendida) interfaz?.MostrarPromptTemporal("Esperá. Dejá que tus ojos se acostumbren.", 5f);
        }
    }

    /// <summary>Conectado al evento 'alAbrirse' de la compuerta del Umbral.</summary>
    public void NotificarUmbralAbierto()
    {
        if (EtapaActual <= Etapa.AbrirUmbral) AvanzarAExplorar();
    }

    void AvanzarAExplorar()
    {
        EtapaActual = Etapa.Explorar;
        interfaz?.MostrarPromptTemporal("El paso está abierto. Dos alas: cada una guarda un filtro y un sello.", 6f);
    }

    /// <summary>Conectado a los atajos que abre cada sello al encenderse.</summary>
    public void NotificarSello()
    {
        sellos++;
        if (sellos == 1) interfaz?.MostrarPromptTemporal("Un sello encendido. El atajo vuelve a la rotonda.", 5f);
    }

    /// <summary>Conectado a la puerta de los sellos: los dos están encendidos.</summary>
    public void EntrarCruce()
    {
        if (EtapaActual >= Etapa.Cruce) return;
        EtapaActual = Etapa.Cruce;
        interfaz?.MostrarPromptTemporal("Los dos sellos arden. Se abrió el paso del norte.", 5f);
    }

    // los filtros se enseñan con los murales de cada ala: ni la tecla ni lo que hacen van en texto
    void AlDesbloquearFiltro(FiltroDefinicion filtro) { }

    void AlEquiparFiltro(FiltroDefinicion filtro) { }

    /// <summary>Conectado a la zona de entrada de la Cresta.</summary>
    public void EntrarCresta()
    {
        if (EtapaActual >= Etapa.Cresta) return;
        EtapaActual = Etapa.Cresta;
        if (linterna == null || !linterna.Encendida) interfaz?.MostrarPromptTemporal("Esperá. Dejá que tus ojos se acostumbren.", 5f);
        eclipse?.HabilitarEnCresta();
    }

    public void EntrarEpilogo() => EtapaActual = Etapa.Epilogo;

    public void Finalizar() => EtapaActual = Etapa.Finalizado;
}
