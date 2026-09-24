using UnityEngine;

/// <summary>
/// Controlador de primera persona. Sin salto, sin correr: el ritmo es contenido.
///
/// Poner en: un GameObject con CharacterController, con la Camera como hijo.
///
/// CÓMO FUNCIONA
///  - Mirar: el giro horizontal rota el cuerpo entero; el vertical (pitch) rota sólo la
///    cámara, limitado para no dar la vuelta.
///  - Mover: arma la dirección con los ejes del cuerpo y la suaviza (arranque y frenada
///    con peso). La gravedad se acumula en 'caida' y todo se aplica con cc.Move(), que
///    resuelve las colisiones con paredes y pisos.
///  - Cabecear: la cámara sube y baja con el paso, y en cada pisada suena un paso.
/// Las cinemáticas lo apagan (enabled = false) y usan MirarHacia / Orientar al terminar.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class JugadorFPS : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 2.6f;
    public float gravedad = -18f;
    [Tooltip("Suavizado del arranque y la frenada. Más alto = más ágil.")]
    public float suavizado = 8f;

    [Header("Cámara")]
    public Transform camara;
    [Tooltip("Grados por cada 10 píxeles de mouse.")]
    public float sensibilidad = 2.2f;
    public float limiteVertical = 85f;
    public bool invertirY;

    [Header("Cabeceo al caminar")]
    public float amplitudCabeceo = 0.035f;
    [Tooltip("Pasos por segundo a velocidad máxima.")]
    public float pasosPorSegundo = 1.8f;

    [Header("Pasos")]
    public AudioSource fuentePasos;
    public AudioClip[] pasos;

    CharacterController cc;
    Vector3 velocidadActual;   // velocidad horizontal suavizada
    Vector3 camaraBase;        // posición local de la cámara sin cabeceo
    float caida;               // velocidad vertical (negativa = cayendo)
    float pitch;               // inclinación vertical de la cámara, en grados
    float ciclo;               // fase del paso: avanza con la velocidad
    int framesIgnorados = 3;
    int ultimoPaso;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        if (camara != null) camaraBase = camara.localPosition;
    }

    void Update()
    {
        if (PausaCrater.EnPausa) return;   // en pausa no se mira ni se camina
        Mirar();
        Mover(Time.deltaTime);
    }

    void Mirar()
    {
        // el primer delta del mouse suele traer el salto de bloquear el cursor
        if (framesIgnorados > 0)
        {
            framesIgnorados--;
            return;
        }

        Vector2 giro = EntradaCrater.Mirada() * sensibilidad;
        // horizontal: gira todo el cuerpo (así "adelante" siempre es hacia donde se mira)
        transform.Rotate(0f, giro.x, 0f);
        // vertical: sólo la cámara, con límite para no mirar más allá de arriba/abajo
        pitch = Mathf.Clamp(pitch + giro.y * (invertirY ? 1f : -1f), -limiteVertical, limiteVertical);
        AplicarPitch();
    }

    void Mover(float delta)
    {
        Vector2 entrada = EntradaCrater.Movimiento();
        // la entrada (x, y) se convierte a una dirección en el mundo según hacia dónde mira el cuerpo
        Vector3 deseada = (transform.right * entrada.x + transform.forward * entrada.y) * velocidad;
        // suavizado exponencial: se siente igual a 30 o a 144 FPS
        velocidadActual = Vector3.Lerp(velocidadActual, deseada, 1f - Mathf.Exp(-suavizado * delta));

        // en el piso, una caída chica constante lo mantiene pegado (bajando rampas, por ejemplo)
        if (cc.isGrounded && caida < 0f) caida = -2f;
        caida += gravedad * delta;

        Vector3 total = velocidadActual;
        total.y = caida;
        cc.Move(total * delta);

        Cabecear(delta);
    }

    void Cabecear(float delta)
    {
        if (camara == null) return;

        // 0 = quieto, 1 = caminando a velocidad máxima (en el aire no hay cabeceo)
        float rapidez = cc.isGrounded ? Mathf.Clamp01(new Vector2(velocidadActual.x, velocidadActual.z).magnitude / velocidad) : 0f;
        ciclo += rapidez * pasosPorSegundo * Mathf.PI * delta;

        // la cabeza baja en cada paso (|sen|) y se mece un poco de costado (cos)
        float onda = Mathf.Sin(ciclo);
        camara.localPosition = camaraBase + new Vector3(
            Mathf.Cos(ciclo) * amplitudCabeceo * 0.5f * rapidez,
            -Mathf.Abs(onda) * amplitudCabeceo * rapidez,
            0f);

        // un paso cada medio ciclo, justo cuando la cabeza toca el punto más bajo
        int paso = Mathf.FloorToInt(ciclo / Mathf.PI);
        if (paso != ultimoPaso)
        {
            ultimoPaso = paso;
            if (rapidez > 0.3f && fuentePasos != null && pasos != null && pasos.Length > 0)
            {
                // altura y clip al azar: dos pasos nunca suenan idénticos
                fuentePasos.pitch = Random.Range(0.92f, 1.08f);
                fuentePasos.PlayOneShot(pasos[Random.Range(0, pasos.Length)], Mathf.Lerp(0.5f, 1f, rapidez));
            }
        }
    }

    void AplicarPitch()
    {
        if (camara != null) camara.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    /// <summary>Gira cuerpo y cámara para mirar un punto del mundo. Útil para cinemáticas y tests.</summary>
    public void MirarHacia(Vector3 punto)
    {
        Vector3 ojo = camara != null ? camara.position : transform.position;
        Vector3 dir = punto - ojo;
        Vector3 plano = new Vector3(dir.x, 0f, dir.z);
        if (plano.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(plano);
        pitch = Mathf.Clamp(-Mathf.Atan2(dir.y, plano.magnitude) * Mathf.Rad2Deg, -limiteVertical, limiteVertical);
        AplicarPitch();
    }

    /// <summary>Deja al jugador mirando al frente con la orientación dada (sólo el giro horizontal).</summary>
    public void Orientar(Quaternion rotacion)
    {
        transform.rotation = Quaternion.Euler(0f, rotacion.eulerAngles.y, 0f);
        pitch = 0f;
        AplicarPitch();
    }

    /// <summary>Frena en seco (después de teletransportar al jugador).</summary>
    public void ReiniciarMovimiento()
    {
        velocidadActual = Vector3.zero;
        caida = -2f;
    }
}
