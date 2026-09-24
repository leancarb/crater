using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// La mecánica del final. Con la linterna apagada, la exposición sube
/// lentamente y aparece lo que la propia linterna tapaba (los tallados
/// latentes de la Cresta). Encenderla resetea la adaptación de golpe.
///
/// Poner en: el Global Volume. El Volume Profile necesita un Color Adjustments.
/// Arranca deshabilitada: la habilita el EclipseFinalController al entrar a la Cresta.
///
/// CÓMO FUNCIONA
/// Cuenta el tiempo a oscuras. Pasada la 'demoraInicial', 'progreso' sube de 0 a 1 en
/// 'tiempoDeAdaptacion' segundos; con la linterna prendida baja rápido a 0.
/// El progreso, suavizado por una curva, mueve el postExposure del Color Adjustments
/// (la imagen se aclara, como los ojos acostumbrándose) y el volumen del viento.
/// Al pasar 'umbralApertura' dispara 'alAdaptarse' una vez: eso abre la puerta del eclipse.
/// </summary>
public class AdaptacionOscuridad : MonoBehaviour
{
    [Header("Rangos de exposición (EV)")]
    public float exposicionNormal = 0f;
    public float exposicionAdaptada = 3.2f;

    [Header("Tiempos")]
    [Tooltip("Segundos de oscuridad hasta la adaptación completa.")]
    public float tiempoDeAdaptacion = 10f;
    [Tooltip("Segundos que tarda en perderse al encender. Rápido, como en la vida real.")]
    public float tiempoDeReseteo = 1.2f;
    [Tooltip("Demora antes de que empiece a adaptarse.")]
    public float demoraInicial = 3f;

    [Header("Refuerzo diegético")]
    [Tooltip("Viento del óculo: sube de volumen mientras el jugador se adapta.")]
    public AudioSource ambienteDeAdaptacion;
    public float volumenMaximo = 0.6f;

    [Header("Final")]
    [Tooltip("Progreso de adaptación (0 a 1) a partir del cual se considera resuelto.")]
    [Range(0f, 1f)] public float umbralApertura = 0.9f;
    [Tooltip("Se dispara una sola vez, la primera vez que se llega al umbral.")]
    public UnityEvent alAdaptarse = new UnityEvent();

    ColorAdjustments ajustes;
    float progreso;      // 0 = normal, 1 = adaptado
    float aOscuras;
    bool yaAvisado;

    public bool Habilitada { get; private set; }
    public float Progreso => progreso;
    /// <summary>Progreso ya pasado por la curva de visión escotópica.</summary>
    public float Curva => Mathf.SmoothStep(0f, 1f, progreso);

    void Start()
    {
        // 'profile' (no 'sharedProfile') crea una copia: el asset no se modifica al jugar
        // TryGet busca el efecto Color Adjustments dentro del perfil de post-procesado
        var volume = GetComponent<Volume>();
        if (volume != null && volume.profile != null && volume.profile.TryGet(out ajustes))
            ajustes.postExposure.overrideState = true;
        ReiniciarVisual();
    }

    void Update()
    {
        if (!Habilitada) return;

        var linterna = LinternaController.Instancia;
        bool linternaApagada = linterna == null || !linterna.Encendida;

        // a oscuras: se adapta despacio. Con luz: se pierde rápido (como en la vida real)

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

        // exposición en EV: +1 duplica el brillo de la imagen, +3,2 lo multiplica por ~9
        if (ajustes != null)
            ajustes.postExposure.value = Mathf.Lerp(exposicionNormal, exposicionAdaptada, Curva);

        if (ambienteDeAdaptacion != null)
            ambienteDeAdaptacion.volume = Curva * volumenMaximo;

        // una sola vez: aunque después vuelva a prender la linterna, la puerta ya quedó abierta
        if (!yaAvisado && progreso >= umbralApertura)
        {
            yaAvisado = true;
            alAdaptarse?.Invoke();
        }
    }

    public void Habilitar()
    {
        Habilitada = true;
        progreso = 0f;
        aOscuras = 0f;
        yaAvisado = false;
    }

    public void Deshabilitar()
    {
        Habilitada = false;
        ReiniciarVisual();
    }

    void ReiniciarVisual()
    {
        progreso = 0f;
        aOscuras = 0f;
        if (ajustes != null) ajustes.postExposure.value = exposicionNormal;
        if (ambienteDeAdaptacion != null) ambienteDeAdaptacion.volume = 0f;
    }
}
