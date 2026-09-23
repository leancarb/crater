using System.Collections;
using UnityEngine;

/// <summary>Coordina el eclipse, el traslado encubierto y el cierre en la capilla.</summary>
public class EclipseFinalController : MonoBehaviour
{
    [SerializeField] AdaptacionOscuridad adaptacion;
    [SerializeField] LinternaController linterna;
    [SerializeField] InterfazCrater interfaz;
    [SerializeField] FlujoJuegoCrater flujo;
    [SerializeField] Transform jugador;
    [SerializeField] Transform spawnCapilla;

    bool habilitado;
    bool transicionIniciada;
    bool vinculado;

    public void Configurar(AdaptacionOscuridad nuevaAdaptacion, LinternaController nuevaLinterna,
                           InterfazCrater nuevaInterfaz, Transform nuevoJugador,
                           Transform nuevoSpawnCapilla)
    {
        Desvincular();
        adaptacion = nuevaAdaptacion;
        linterna = nuevaLinterna;
        interfaz = nuevaInterfaz;
        jugador = nuevoJugador;
        spawnCapilla = nuevoSpawnCapilla;
        Vincular();
    }

    public void AsignarFlujo(FlujoJuegoCrater nuevoFlujo)
    {
        flujo = nuevoFlujo;
    }

    void Start()
    {
        Vincular();
    }

    void OnDestroy()
    {
        Desvincular();
    }

    void Update()
    {
        if (!habilitado || adaptacion == null || transicionIniciada) return;
        float a = Mathf.SmoothStep(0f, 0.88f, adaptacion.Progreso);
        interfaz?.MostrarBlanco(a);
    }

    void Vincular()
    {
        if (adaptacion == null || vinculado) return;
        adaptacion.alAdaptarse.AddListener(AlCompletarAdaptacion);
        vinculado = true;
    }

    void Desvincular()
    {
        if (adaptacion == null || !vinculado) return;
        adaptacion.alAdaptarse.RemoveListener(AlCompletarAdaptacion);
        vinculado = false;
    }

    public void HabilitarEnCresta()
    {
        if (habilitado) return;
        habilitado = true;
        adaptacion?.Habilitar();
    }

    void AlCompletarAdaptacion()
    {
        if (!transicionIniciada) StartCoroutine(TrasladarALaCapilla());
    }

    IEnumerator TrasladarALaCapilla()
    {
        transicionIniciada = true;
        interfaz?.OcultarPrompt();

        for (float t = 0f; t < 1.25f; t += Time.unscaledDeltaTime)
        {
            interfaz?.MostrarBlanco(Mathf.Lerp(0.88f, 1f, t / 1.25f));
            yield return null;
        }
        interfaz?.MostrarBlanco(1f);

        var control = jugador != null ? jugador.GetComponent<JugadorFPS>() : null;
        var cc = jugador != null ? jugador.GetComponent<CharacterController>() : null;
        if (control != null) control.enabled = false;
        if (cc != null) cc.enabled = false;

        linterna?.Encender(false);
        if (linterna != null) linterna.enabled = false;

        if (jugador != null && spawnCapilla != null)
        {
            jugador.SetPositionAndRotation(spawnCapilla.position, spawnCapilla.rotation);
        }

        if (cc != null) cc.enabled = true;
        adaptacion?.Deshabilitar();
        yield return new WaitForSecondsRealtime(0.35f);

        if (interfaz != null) yield return interfaz.FundirDesdeBlanco(2.5f);
        if (control != null) control.enabled = true;
        interfaz?.MostrarPromptTemporal("Camina hacia el exterior.", 5f);
        flujo?.EntrarEpilogo();
    }

    public void CerrarDemo()
    {
        if (flujo != null && flujo.EtapaActual == FlujoJuegoCrater.Etapa.Finalizado) return;
        flujo?.Finalizar();
        if (jugador != null)
        {
            var control = jugador.GetComponent<JugadorFPS>();
            if (control != null) control.enabled = false;
        }
        if (interfaz != null) StartCoroutine(interfaz.MostrarCierre());
    }
}
