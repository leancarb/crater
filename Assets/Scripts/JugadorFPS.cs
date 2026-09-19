using UnityEngine;

/// <summary>
/// Controlador de primera persona minimo. Sin salto, sin correr: el ritmo es contenido.
///
/// Poner en: un GameObject con CharacterController, con la Camera como hijo.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class JugadorFPS : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 2.4f;
    public float gravedad = -14f;
    [Tooltip("Suavizado del arranque y la frenada. Mas alto = mas pesado.")]
    public float suavizado = 8f;

    [Header("Camara")]
    public Transform camara;
    public float sensibilidadX = 2.2f;
    public float sensibilidadY = 2.0f;
    public float limiteVertical = 85f;

    [Header("Comodidad")]
    public bool invertirY = false;

    CharacterController cc;
    Vector3 velocidadActual;
    float caida;
    float pitch;
    int framesIgnorados = 3;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Mirar();
        Mover();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Mirar()
    {
        if (framesIgnorados > 0)
        {
            framesIgnorados--;
            return;
        }

        float mx = Input.GetAxisRaw("Mouse X") * sensibilidadX;
        float my = Input.GetAxisRaw("Mouse Y") * sensibilidadY * (invertirY ? 1f : -1f);

        transform.Rotate(Vector3.up * mx);

        pitch = Mathf.Clamp(pitch + my, -limiteVertical, limiteVertical);
        if (camara != null) camara.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Mover()
    {
        Vector3 deseada = (transform.right * Input.GetAxisRaw("Horizontal")
                         + transform.forward * Input.GetAxisRaw("Vertical"));
        if (deseada.sqrMagnitude > 1f) deseada.Normalize();
        deseada *= velocidad;

        velocidadActual = Vector3.Lerp(velocidadActual, deseada, suavizado * Time.deltaTime);

        if (cc.isGrounded && caida < 0f) caida = -2f;
        caida += gravedad * Time.deltaTime;

        Vector3 total = velocidadActual;
        total.y = caida;
        cc.Move(total * Time.deltaTime);
    }
}
