using System.Collections;
using UnityEngine;

/// <summary>
/// Coordina el final: la adaptación a la oscuridad en la Cresta abre la puerta del
/// eclipse; cruzarla trae el silencio, el anillo de diamante y el blanco, y la demo
/// vuelve a empezar.
///
/// CÓMO FUNCIONA
///  1. HabilitarEnCresta (lo llama el flujo al entrar a la Cresta) enciende la adaptación.
///  2. La adaptación dispara AlCompletarAdaptacion: se abre la PuertaEclipse.
///  3. La zona detrás de la puerta llama a CruzarPuerta: se corta el sonido, suena el
///     tono final, el anillo de diamante tapa la pantalla de blanco y se recarga la escena.
/// Las esperas se hacen con una corrutina (IEnumerator + yield).
/// </summary>
public class EclipseFinalController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] AdaptacionOscuridad adaptacion;
    [SerializeField] LinternaController linterna;
    [SerializeField] Fundidos fundidos;
    [SerializeField] FlujoJuegoCrater flujo;
    [SerializeField] PausaCrater pausa;
    [SerializeField] Transform jugador;
    [SerializeField] PuertaEclipse puerta;

    [Header("Anillo de diamante")]
    [SerializeField] AudioSource tonoFinal;
    [SerializeField] float silencio = 0.5f;
    [SerializeField] float duracionDestello = 1.5f;
    [SerializeField] float blancoSostenido = 3f;

    // banderas para que cada paso ocurra una sola vez aunque lo llamen de nuevo
    bool habilitado;
    bool transicionIniciada;

    public void HabilitarEnCresta()
    {
        if (habilitado) return;
        habilitado = true;
        adaptacion?.Habilitar();
    }

    /// <summary>Conectado al evento 'alAdaptarse' de AdaptacionOscuridad: aparece la puerta.</summary>
    public void AlCompletarAdaptacion()
    {
        if (puerta != null) puerta.Abrir();
        else CruzarPuerta();   // sin puerta en la escena, el final es directo
    }

    /// <summary>Conectado a la zona detrás de la puerta del eclipse.</summary>
    public void CruzarPuerta()
    {
        if (!transicionIniciada) StartCoroutine(Final());
    }

    IEnumerator Final()
    {
        transicionIniciada = true;
        flujo?.Finalizar();
        if (pausa != null) pausa.enabled = false;   // que Esc no interrumpa el final

        var control = jugador != null ? jugador.GetComponent<JugadorFPS>() : null;
        if (control != null) control.enabled = false;
        if (linterna != null) linterna.enabled = false;

        // silencio total: viento, zumbido, pasos. El tono final ignora esta pausa (ignoreListenerPause)
        AudioListener.pause = true;
        yield return new WaitForSecondsRealtime(silencio);

        // anillo de diamante: la luz del sol vuelve de golpe
        if (tonoFinal != null)
        {
            tonoFinal.ignoreListenerPause = true;
            tonoFinal.Play();
        }
        if (fundidos != null) yield return fundidos.AnilloDeDiamante(duracionDestello);
        else yield return new WaitForSecondsRealtime(duracionDestello);
        yield return new WaitForSecondsRealtime(blancoSostenido);

        // vuelve a empezar desde la Explanada (también reactiva el audio)
        PausaCrater.ReiniciarEscena();
    }
}
