using UnityEngine;

/// <summary>Material de pisada del collider, independiente del color de su textura.</summary>
public class SuperficieDePasos : MonoBehaviour
{
    public enum Tipo { Piedra, Tierra, Madera, Metal }
    public Tipo tipo;
}
