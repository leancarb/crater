using UnityEngine;

/// <summary>Conversión de las coordenadas de diseño del subsuelo ampliado.</summary>
public static class EspacioCrater
{
    public const float Escala = 1.25f;
    public static readonly Vector3 Origen = new Vector3(0, 0, -23);
    public static Vector3 Punto(Vector3 punto)
        => Origen + Vector3.Scale(punto - Origen, new Vector3(Escala, 1, Escala));
}
