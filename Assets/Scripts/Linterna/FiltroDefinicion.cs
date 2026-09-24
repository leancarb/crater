using UnityEngine;

/// <summary>
/// Definición de un filtro de la linterna.
/// Se crea desde el menú: Assets > Create > Crater > Filtro.
/// No hace falta tocar código para crear o ajustar filtros: se hace desde el Inspector.
///
/// CÓMO FUNCIONA
/// Es un ScriptableObject: un archivo de datos (.asset) que vive en el proyecto, no
/// en la escena. La linterna tiene una lista de estos assets y, al equipar uno,
/// copia sus valores (color, ángulo, alcance, intensidad) al Spot Light.
/// Los receptores (anclas, rejas) miran el 'canal' para saber si ese filtro los activa.
/// En este proyecto los dos filtros los genera ConstructorCrater en Assets/Data/Filtros.
/// </summary>
[CreateAssetMenu(fileName = "Filtro", menuName = "Crater/Filtro")]
public class FiltroDefinicion : ScriptableObject
{
    /// <summary>
    /// A qué familia de receptores le habla el filtro.
    /// Ninguno = luz blanca (sin filtro). Cuerpo = anclas. Hueco = rejas.
    /// </summary>
    public enum Canal { Ninguno, Cuerpo, Hueco }

    [Header("Identidad")]
    // lo que muestra la interfaz arriba a la derecha
    public string nombreVisible = "CUERPO";
    public Canal canal = Canal.Cuerpo;
    [TextArea] public string descripcion;

    [Header("Luz")]
    // valores que se copian al Spot Light de la linterna al equipar el filtro
    public Color color = new Color(0.91f, 0.63f, 0.29f);
    [Range(10f, 60f)] public float anguloCono = 28f;     // apertura total del cono, en grados
    [Range(1f, 30f)] public float alcance = 14f;         // metros: más lejos, el haz no afecta receptores
    [Range(0.1f, 3000f)] public float intensidad = 800f;

    [Header("Comportamiento")]
    [Tooltip("Segundos que hay que sostener el haz para activar un receptor.")]
    [Range(0.05f, 3f)] public float tiempoDeCarga = 0.35f;

    [Header("Sonido")]
    public AudioClip sonidoAlEquipar;
    [Tooltip("Zumbido continuo mientras este filtro está puesto y la linterna encendida.")]
    public AudioClip zumbido;
}
