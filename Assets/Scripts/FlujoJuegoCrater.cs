using UnityEngine;

/// <summary>Autoridad de progresion para la demo de una sola escena.</summary>
public class FlujoJuegoCrater : MonoBehaviour
{
    public enum Etapa
    {
        BuscarLinterna,
        AprenderLinterna,
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

    public Etapa EtapaActual { get; private set; } = Etapa.BuscarLinterna;

    public void Configurar(LinternaController nuevaLinterna, InterfazCrater nuevaInterfaz,
                           EclipseFinalController nuevoEclipse)
    {
        Desvincular();
        linterna = nuevaLinterna;
        interfaz = nuevaInterfaz;
        eclipse = nuevoEclipse;
        Vincular();
    }

    void Start()
    {
        Vincular();
        interfaz?.MostrarPromptTemporal("Acercate a la luz.", 4f);
    }

    void OnDestroy()
    {
        Desvincular();
    }

    void Vincular()
    {
        if (linterna == null || yaVinculado) return;
        linterna.AlRecogerLinterna += AlRecogerLinterna;
        linterna.AlCambiarEncendido += AlCambiarEncendido;
        linterna.AlDesbloquearFiltro += AlDesbloquearFiltro;
        linterna.AlEquiparFiltro += AlEquiparFiltro;
        yaVinculado = true;
    }

    void Desvincular()
    {
        if (linterna == null || !yaVinculado) return;
        linterna.AlRecogerLinterna -= AlRecogerLinterna;
        linterna.AlCambiarEncendido -= AlCambiarEncendido;
        linterna.AlDesbloquearFiltro -= AlDesbloquearFiltro;
        linterna.AlEquiparFiltro -= AlEquiparFiltro;
        yaVinculado = false;
    }

    bool yaVinculado;

    void AlRecogerLinterna()
    {
        EtapaActual = Etapa.AprenderLinterna;
        interfaz?.MostrarPrompt("F · Encender la linterna");
    }

    void AlCambiarEncendido(bool encendida)
    {
        if (EtapaActual == Etapa.AprenderLinterna && encendida)
        {
            EtapaActual = Etapa.BuscarCuerpo;
            interfaz?.MostrarPromptTemporal("Busca el filtro ambar.", 4f);
        }
        else if (EtapaActual == Etapa.Cresta && !encendida)
        {
            interfaz?.OcultarPrompt();
        }
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
        if (filtro == null)
        {
            interfaz?.OcultarPrompt();
            return;
        }

        if (filtro.canal == FiltroDefinicion.Canal.Cuerpo && EtapaActual == Etapa.UsarCuerpo)
        {
            EtapaActual = Etapa.BuscarHueco;
            interfaz?.MostrarPromptTemporal("Sostene el haz sobre las anclas. Q · Quitar filtro", 5f);
        }
        else if (filtro.canal == FiltroDefinicion.Canal.Hueco && EtapaActual == Etapa.UsarHueco)
        {
            interfaz?.MostrarPromptTemporal("Ilumina la materia y atravesala. Q · Quitar filtro", 5f);
        }
    }

    public void EntrarCresta()
    {
        if (EtapaActual >= Etapa.Cresta) return;
        EtapaActual = Etapa.Cresta;
        interfaz?.MostrarPrompt("F · Apagar la linterna");
        eclipse?.HabilitarEnCresta();
    }

    public void EntrarEpilogo()
    {
        EtapaActual = Etapa.Epilogo;
    }

    public void Finalizar()
    {
        EtapaActual = Etapa.Finalizado;
    }
}
