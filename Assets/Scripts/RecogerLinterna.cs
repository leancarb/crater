using UnityEngine;

/// <summary>
/// Trigger que le da la linterna al jugador (El Umbral). Solo tiene efecto si
/// la Linterna tiene 'Requiere Recogerla' tildado.
/// Poner en: el objeto de la linterna tirada en el piso, con un Collider marcado como Is Trigger.
/// </summary>
public class RecogerLinterna : MonoBehaviour
{
    public GameObject objetoVisual;
    public AudioSource sonido;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (LinternaController.Instancia == null) return;

        LinternaController.Instancia.Recoger();

        if (sonido != null) sonido.Play();
        if (objetoVisual != null) objetoVisual.SetActive(false);
        GetComponent<Collider>().enabled = false;
    }
}
