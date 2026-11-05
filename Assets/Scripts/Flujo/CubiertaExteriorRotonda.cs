using UnityEngine;

/// <summary>La superficie tapa el óculo hasta que el jugador entra al cráter.</summary>
public class CubiertaExteriorRotonda : MonoBehaviour
{
    PrologoCapilla prologo;
    Renderer superficie;
    Collider colision;
    void Awake(){prologo=FindFirstObjectByType<PrologoCapilla>();superficie=GetComponent<Renderer>();colision=GetComponent<Collider>();}
    void LateUpdate()
    {
        bool exterior=prologo==null||!prologo.EnElCrater;
        superficie.enabled=exterior;
        if(colision!=null)colision.enabled=exterior;
    }
}
