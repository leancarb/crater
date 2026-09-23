using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZonaFinalCapilla : MonoBehaviour
{
    [SerializeField] EclipseFinalController eclipse;
    bool usada;

    public void Configurar(EclipseFinalController nuevoEclipse) => eclipse = nuevoEclipse;

    void OnTriggerEnter(Collider other)
    {
        if (usada || !other.CompareTag("Player")) return;
        usada = true;
        eclipse?.CerrarDemo();
    }
}
