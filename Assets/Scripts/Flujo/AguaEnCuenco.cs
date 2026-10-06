using UnityEngine;
/// <summary>Reflejo real del entorno, capturado sólo al acercarse y tras cambios de luz.</summary>
public class AguaEnCuenco : MonoBehaviour
{
    public ReflectionProbe sonda;
    public Transform oyente;
    public int Capturas {get;private set;}
    public bool Lista => id>=0 && sonda.IsFinishedRendering(id);
    bool pendiente=true;int id=-1;
    public void MarcarCambio(){pendiente=true;}
    void Update()
    {
        if(!pendiente || sonda==null || oyente==null || PausaCrater.EnPausa || Vector3.Distance(oyente.position,transform.position)>9)return;
        if(id>=0 && !sonda.IsFinishedRendering(id))return;
        id=sonda.RenderProbe();Capturas++;pendiente=false;
    }
}
