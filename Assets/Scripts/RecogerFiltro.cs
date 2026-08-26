using UnityEngine;

/// <summary>
/// Trigger que desbloquea un filtro cuando el jugador se acerca.
/// Poner en: el objeto del filtro tirado en el piso, con un Collider marcado como Is Trigger.
/// </summary>
public class RecogerFiltro : MonoBehaviour
{
    public FiltroDefinicion filtro;
    public GameObject objetoVisual;
    public AudioSource sonido;
    [Tooltip("Se equipa solo al recogerlo.")]
    public bool equiparAlRecoger = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (LinternaController.Instancia == null) return;

        LinternaController.Instancia.Desbloquear(filtro);
        if (equiparAlRecoger) LinternaController.Instancia.EquiparFiltro(filtro);

        if (sonido != null) sonido.Play();
        if (objetoVisual != null) objetoVisual.SetActive(false);
        GetComponent<Collider>().enabled = false;
    }
}
