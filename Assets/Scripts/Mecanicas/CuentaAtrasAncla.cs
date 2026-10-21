using UnityEngine;

/// <summary>Arco junto al ancla: se vacía durante la retención y no modifica el mecanismo.</summary>
[RequireComponent(typeof(Ancla))]
public class CuentaAtrasAncla : MonoBehaviour
{
    Ancla ancla;
    LineRenderer arco;
    Material material;
    const int Segmentos=48;
    public float FraccionVisible { get; private set; }

    void Awake()
    {
        ancla=GetComponent<Ancla>();
        var go=new GameObject("Tiempo_Ancla");go.transform.SetParent(transform,false);
        arco=go.AddComponent<LineRenderer>();arco.useWorldSpace=true;arco.loop=false;
        arco.startWidth=arco.endWidth=.025f;arco.numCapVertices=2;
        arco.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;arco.receiveShadows=false;
        material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor",Color.Lerp(ancla.colorEncendida,Color.white,.2f));
        arco.sharedMaterial=material;arco.enabled=false;
    }
    void LateUpdate()
    {
        FraccionVisible=ancla.Activo && !ancla.permanente && ancla.retencion>0 ? ancla.RetencionRestante : 0;
        arco.enabled=FraccionVisible>.001f;
        if(!arco.enabled)return;
        int pasos=Mathf.Max(1,Mathf.CeilToInt(Segmentos*FraccionVisible));arco.positionCount=pasos+1;
        Vector3 centro=ancla.PuntoDeImpacto+transform.forward*.08f;
        for(int i=0;i<=pasos;i++)
        {
            float a=2*Mathf.PI*FraccionVisible*i/pasos;
            arco.SetPosition(i,centro+(transform.right*Mathf.Sin(a)+transform.up*Mathf.Cos(a))*.55f);
        }
    }
    void OnDisable(){if(arco!=null)arco.enabled=false;FraccionVisible=0;}
    void OnDestroy(){if(material!=null)Destroy(material);}
}
