using UnityEngine;
/// <summary>Una huella de trabajo se descubre con la nueva luz, sin otra cinemática.</summary>
public class RevelacionAlRegresar : MonoBehaviour
{
    public Renderer tallado;
    public Light luz;
    public bool Revelado {get;private set;}
    float presencia;MaterialPropertyBlock bloque;
    static readonly int Emision=Shader.PropertyToID("_EmissionColor");
    void Awake(){bloque=new MaterialPropertyBlock();}
    public void Revelar(){Revelado=true;}
    void Update()
    {
        presencia=Mathf.MoveTowards(presencia,Revelado?1:0,Time.deltaTime*.25f);
        if(luz!=null)luz.intensity=presencia*5;
        if(tallado!=null){tallado.enabled=presencia>.01f;tallado.GetPropertyBlock(bloque);bloque.SetColor(Emision,new Color(.6f,.74f,1)*presencia*1.2f);tallado.SetPropertyBlock(bloque);}
    }
}
