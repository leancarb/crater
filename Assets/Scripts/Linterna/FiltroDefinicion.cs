using UnityEngine;

/// <summary>
/// Definición de un filtro de la linterna.
/// Se crea desde el menú: Assets > Create > Crater > Filtro.
/// No hace falta tocar código para crear o ajustar filtros: se hace desde el Inspector.
/// </summary>
[CreateAssetMenu(fileName = "Filtro", menuName = "Crater/Filtro")]
public class FiltroDefinicion : ScriptableObject
{
    public enum Canal { Ninguno, Cuerpo, Hueco }

    [Header("Identidad")]
    public string nombreVisible = "CUERPO";
    public Canal canal = Canal.Cuerpo;
    [TextArea] public string descripcion;

    [Header("Luz")]
    public Color color = new Color(0.91f, 0.63f, 0.29f);
    [Range(10f, 60f)] public float anguloCono = 28f;
    [Range(1f, 30f)] public float alcance = 14f;
    [Range(0.1f, 3000f)] public float intensidad = 800f;

    [Header("Comportamiento")]
    [Tooltip("Segundos que hay que sostener el haz para activar un receptor.")]
    [Range(0.05f, 3f)] public float tiempoDeCarga = 0.35f;

    [Header("Sonido")]
    public AudioClip sonidoAlEquipar;
    [Tooltip("Zumbido continuo mientras este filtro está puesto y la linterna encendida.")]
    public AudioClip zumbido;
}
