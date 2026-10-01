using UnityEngine;

/// <summary>
/// El espacio durante la totalidad: estrellas, la Vía Láctea y el resplandor
/// anaranjado del horizonte (en un eclipse real el cielo se oscurece como de noche
/// pero el horizonte queda iluminado en 360°, como un atardecer).
///
/// CÓMO FUNCIONA
/// Es una esfera alrededor de la cámara con una textura panorámica y un material
/// que SUMA luz (aditivo) sobre el color de fondo. Se dibuja antes que todo lo
/// demás y sin escribir profundidad, así cualquier objeto le pasa por delante,
/// aunque la esfera sea chica (la niebla no la come). En LateUpdate sigue a la
/// cámara: nunca se llega al borde, parece estar en el infinito.
/// 'Intensidad' 0 = no se ve (de día), 1 = noche cerrada.
/// </summary>
public class CieloEstrellado : MonoBehaviour
{
    [SerializeField] Renderer esfera;
    [Tooltip("Radio de la esfera. Chica a propósito: así casi no le llega la niebla.")]
    [SerializeField] float radio = 20f;
    [SerializeField, Range(0f, 1f)] float intensidad = 1f;

    MaterialPropertyBlock bloque;
    static readonly int IdColor = Shader.PropertyToID("_BaseColor");

    public float Intensidad
    {
        get => intensidad;
        set { intensidad = Mathf.Clamp01(value); Aplicar(); }
    }

    void Awake() => Aplicar();

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || esfera == null) return;
        esfera.transform.position = cam.transform.position;
        esfera.transform.localScale = Vector3.one * radio * 2f;
    }

    void Aplicar()
    {
        if (esfera == null) return;
        bloque ??= new MaterialPropertyBlock();
        esfera.enabled = intensidad > 0.001f;
        esfera.GetPropertyBlock(bloque);
        bloque.SetColor(IdColor, Color.white * intensidad);
        esfera.SetPropertyBlock(bloque);
    }
}
