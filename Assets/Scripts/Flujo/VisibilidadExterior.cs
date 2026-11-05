using UnityEngine;

/// <summary>Oculta sólo renderizadores exteriores; nunca cielo, colisión o estado del nivel.</summary>
public class VisibilidadExterior : MonoBehaviour
{
    public Renderer[] paisaje;
    bool[] habilitados;
    bool oculto;
    PrologoCapilla prologo;
    void Awake()
    {
        habilitados=new bool[paisaje.Length];
        prologo=FindFirstObjectByType<PrologoCapilla>();
        for(int i=0;i<paisaje.Length;i++)habilitados[i]=paisaje[i]!=null&&paisaje[i].enabled;
    }
    void LateUpdate()
    {
        if(Camera.main==null)return;
        bool debajo=prologo!=null && prologo.EnElCrater && Camera.main.transform.position.y<7.5f;
        if(debajo==oculto)return;
        oculto=debajo;
        for(int i=0;i<paisaje.Length;i++)if(paisaje[i]!=null)
            paisaje[i].enabled=!oculto&&habilitados[i];
    }
}
