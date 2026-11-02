using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class EtiquetaTutorial : MonoBehaviour
{
    public string accion;
    TextMesh texto;
    string anterior;
    void Awake(){texto=GetComponent<TextMesh>();}
    void LateUpdate()
    {
        string boton=accion.Contains("Combinacion")?EntradaCrater.BotonFiltro(1)+" → "+EntradaCrater.BotonFiltro(0):
            (accion.Contains("FiltroSol")||accion.Contains("Anclas"))?EntradaCrater.BotonFiltro(0):(accion.Contains("FiltroLuna")||accion.Contains("Trampilla"))?EntradaCrater.BotonFiltro(1):
            accion.Contains("Mover")?(EntradaCrater.UsandoMando?"STICK IZQ.":"WASD"):
            accion.Contains("Mirar")?(EntradaCrater.UsandoMando?"STICK DER.":"MOUSE"):
            accion.Contains("Linterna")||accion.Contains("Apagar")||accion.Contains("Cresta")||accion.Contains("Ancla_Puerta")?EntradaCrater.BotonLinterna:EntradaCrater.BotonInteractuar;
        if(boton==anterior)return;anterior=boton;texto.text="[ "+boton+" ]";
    }
}
