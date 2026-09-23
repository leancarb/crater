using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZonaCresta : MonoBehaviour
{
    [SerializeField] FlujoJuegoCrater flujo;
    bool usada;

    public void Configurar(FlujoJuegoCrater nuevoFlujo) => flujo = nuevoFlujo;

    void OnTriggerEnter(Collider other)
    {
        if (usada || !other.CompareTag("Player")) return;
        usada = true;
        flujo?.EntrarCresta();
    }
}
