using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zona de "ganzfeld": tapa la pantalla con una capa solida de color parejo
/// mientras el jugador esta adentro, en rampa suave (nunca de golpe). A
/// diferencia de un tinte multiplicativo, esto si aplana la percepcion de
/// profundidad: por debajo de la capa no se distinguen sombras ni bordes.
///
/// Poner en: un objeto con Collider marcado como Is Trigger, cubriendo el
/// pasillo. Necesita un Canvas (Screen Space - Overlay) con una Image que
/// cubra toda la pantalla, asignada en el campo 'Overlay'.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GanzfeldZona : MonoBehaviour
{
    public Image overlay;
    public Color colorTinte = new Color(0.86f, 0.16f, 0.10f);
    [Tooltip("Opacidad maxima de la capa. Cerca de 1 = practicamente no se ve nada debajo.")]
    [Range(0f, 1f)] public float opacidadMaxima = 0.88f;
    public float tiempoRampa = 3f;

    float objetivo;
    float actual;

    void Start()
    {
        AplicarColor(0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) objetivo = 1f;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) objetivo = 0f;
    }

    void Update()
    {
        actual = Mathf.MoveTowards(actual, objetivo, Time.deltaTime / tiempoRampa);
        AplicarColor(actual * opacidadMaxima);
    }

    void AplicarColor(float alfa)
    {
        if (overlay == null) return;
        var c = colorTinte;
        c.a = alfa;
        overlay.color = c;
    }
}
