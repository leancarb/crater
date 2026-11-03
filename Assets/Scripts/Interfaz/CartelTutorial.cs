using UnityEngine;

/// <summary>Glifo contextual del cartel físico; usa los controles vigentes, sin tecla blanca adicional.</summary>
[RequireComponent(typeof(TextMesh))]
public class CartelTutorial : MonoBehaviour
{
    [Range(1,11)] public int accion;
    public float anchoCelda=.30f, altoCelda=.30f;
    public GameObject iconoXbox;
    TextMesh texto;
    Renderer figura;
    string anterior;
    bool ajustar;
    void Awake(){texto=GetComponent<TextMesh>();figura=GetComponent<Renderer>();}
    public static string Glifo(int accion)
    {
        bool mando=EntradaCrater.UsandoMando;
        switch(accion)
        {
            case 1:return mando?"STICK\nIZQ.":"WASD";
            case 2:return mando?"A":"ESPACIO";
            case 3:return EntradaCrater.BotonInteractuar;
            case 4:case 11:return EntradaCrater.BotonLinterna;
            case 5:case 8:return mando?"RB":"CLIC\nIZQ.";
            case 6:return mando?"LB":"CLIC\nDER.";
            case 7:return "SOLTAR";
            case 9:return mando?"RT":"SHIFT";
            case 10:return mando?"LB\n→ RB":"DER.\n→ IZQ.";
            default:return "";
        }
    }
    void LateUpdate()
    {
        string actual=Glifo(accion);
        if(actual!=anterior)
        {
            texto.text=actual;anterior=actual;ajustar=true;
            bool xbox=EntradaCrater.UsandoMando&&accion!=1&&accion!=7;
            if(iconoXbox!=null)iconoXbox.SetActive(xbox);
            texto.color=!xbox?new Color(.16f,.145f,.125f):accion==2?new Color(.30f,.78f,.22f):
                accion==3?new Color(.96f,.22f,.18f):(accion==4||accion==11)?new Color(.20f,.55f,1f):Color.white;
            return;
        }
        if(!ajustar)return;
        var medida=figura.localBounds.size;
        if(medida.x<=0 || medida.y<=0)return;
        float ancho=anchoCelda,alto=altoCelda;
        if(EntradaCrater.UsandoMando&&accion!=1&&accion!=7)
        {
            bool redondo=accion==2||accion==3||accion==4||accion==11;
            if(redondo)ancho=alto=Mathf.Min(anchoCelda,altoCelda)*.58f;
            else{ancho*=.82f;alto*=accion==10?.75f:.55f;}
        }
        transform.localScale=Vector3.one*Mathf.Min(ancho/medida.x,alto/medida.y,1);
        ajustar=false;
    }
}
