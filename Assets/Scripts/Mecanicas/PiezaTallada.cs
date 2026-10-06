using UnityEngine;

/// <summary>Fragmento opcional; su identificador permanece estable entre reconstrucciones.</summary>
public class PiezaTallada : MonoBehaviour
{
    public string identificador;
    public int indice;
    public ColeccionPiedras coleccion;
    public bool Recogida => coleccion != null && coleccion.Contiene(identificador);
    public bool Recoger() => coleccion != null && coleccion.Registrar(this);
}
