using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// La mecanica del final del juego, activa desde el primer minuto.
/// Con la linterna apagada, la exposicion sube lentamente y aparece lo que
/// la propia linterna tapaba. Encenderla resetea la adaptacion de golpe.
///
/// Poner en: el Global Volume. El Volume Profile necesita un Color Adjustments.
/// </summary>
public class AdaptacionOscuridad : MonoBehaviour
{
    [Header("Rangos de exposicion (EV)")]
    public float exposicionNormal = 0f;
    public float exposicionAdaptada = 3.2f;

    [Header("Tiempos")]
    [Tooltip("Segundos de oscuridad hasta la adaptacion completa.")]
    public float tiempoDeAdaptacion = 40f;
    [Tooltip("Segundos que tarda en perderse al encender. Rapido, como en la vida real.")]
    public float tiempoDeReseteo = 1.2f;
    [Tooltip("Demora antes de que empiece a adaptarse.")]
    public float demoraInicial = 4f;

    [Header("Refuerzo diegetico")]
    [Tooltip("Viento del oculo: sube de volumen mientras el jugador esta adaptado.")]
    public AudioSource ambienteDeAdaptacion;
    public float volumenMaximo = 0.6f;

    ColorAdjustments ajustes;
    float progreso;      // 0 = normal, 1 = adaptado
    float aOscuras;

    void Start()
    {
        var volume = GetComponent<Volume>();
        if (volume != null && volume.profile != null)
            volume.profile.TryGet(out ajustes);
    }

    void Update()
    {
        bool linternaApagada = LinternaController.Instancia == null
                            || !LinternaController.Instancia.Encendida;

        if (linternaApagada)
        {
            aOscuras += Time.deltaTime;
            if (aOscuras > demoraInicial)
                progreso = Mathf.MoveTowards(progreso, 1f, Time.deltaTime / tiempoDeAdaptacion);
        }
        else
        {
            aOscuras = 0f;
            progreso = Mathf.MoveTowards(progreso, 0f, Time.deltaTime / tiempoDeReseteo);
        }

        // curva lenta al principio, rapida despues: asi se siente la vision escotopica
        float curva = Mathf.SmoothStep(0f, 1f, progreso);

        if (ajustes != null)
            ajustes.postExposure.value = Mathf.Lerp(exposicionNormal, exposicionAdaptada, curva);

        if (ambienteDeAdaptacion != null)
            ambienteDeAdaptacion.volume = curva * volumenMaximo;
    }

    /// <summary>Para la ultima sala: saber si el jugador ya vio la puerta.</summary>
    public bool EstaAdaptado => progreso > 0.75f;
}
