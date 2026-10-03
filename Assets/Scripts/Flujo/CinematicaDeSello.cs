using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cuando el jugador vuelve a la rotonda con un sello encendido, el juego toma la
/// cámara (como en la cinemática del eclipse) y la gira hacia el hilo de tallados de
/// ese sello, que se prende de a uno. Con los dos sellos, además se enciende el
/// eclipse sobre la puerta y la puerta se abre a la vista. Después la mirada vuelve.
///
/// CÓMO FUNCIONA
/// Cada frame anota qué sellos se encendieron. Un sello encendido espera a que el
/// jugador esté dentro de la rotonda (a menos de 'radio' del 'centro'): desde ahí se
/// ven los hilos. Entonces apaga el control del jugador y la pausa y, sin mover la
/// cámara de sus ojos, la gira hacia el hilo de ese sello (HiloDeTallados en modo
/// manual) y lo enciende. Con todos los sellos gira hacia el eclipse, lo enciende y
/// abre la puerta (que espera esta orden: 'abrirSoloPorOrden'). Si se encienden los
/// dos antes de volver, se muestran uno después del otro.
/// </summary>
public class CinematicaDeSello : MonoBehaviour
{
    public static bool Reproduciendo { get; private set; }

    [SerializeField] JugadorFPS jugador;
    [SerializeField] InterfazCrater interfaz;
    [SerializeField] PausaCrater pausa;
    [SerializeField] LinternaController linterna;

    [Header("Un hilo por sello (mismo orden)")]
    [SerializeField] ReceptorDeLuz[] sellos;
    [SerializeField] HiloDeTallados[] hilos;
    [SerializeField] float segundosPorTramo = 1.4f;

    [Header("La rotonda: desde ahí se ven los hilos")]
    [SerializeField] Vector3 centro = new Vector3(0f, 0f, 12f);
    [SerializeField] float radio = 13.5f;

    [Header("Con todos los sellos")]
    [SerializeField] HiloDeTallados eclipse;
    [SerializeField] Compuerta puerta;
    [SerializeField] float esperaPuerta = 3.5f;

    // los campos estáticos sobreviven entre partidas en el editor: se limpian al dar Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico() => Reproduciendo = false;

    readonly Queue<int> pendientes = new Queue<int>();
    bool[] anotado;

    void Start() => anotado = new bool[sellos != null ? sellos.Length : 0];

    void Update()
    {
        for (int i = 0; i < anotado.Length; i++)
        {
            if (anotado[i] || sellos[i] == null || !sellos[i].Activo) continue;
            anotado[i] = true;
            pendientes.Enqueue(i);
        }
        if (!Reproduciendo && pendientes.Count > 0 && EnLaRotonda()) StartCoroutine(Mostrar(pendientes.Dequeue()));
    }

    bool EnLaRotonda()
    {
        if (jugador == null) return true;
        Vector3 p = jugador.transform.position;
        return Mathf.Abs(p.y - centro.y) < 2.5f && new Vector2(p.x - centro.x, p.z - centro.z).magnitude < radio;
    }

    bool TodosLosSellos()
    {
        foreach (var s in sellos) if (s == null || !s.Activo) return false;
        return true;
    }

    IEnumerator Mostrar(int i)
    {
        Reproduciendo = true;
        if (jugador != null) jugador.enabled = false;
        if (pausa != null) pausa.enabled = false;

        // la cámara no se mueve de los ojos del jugador: sólo gira, como en el eclipse
        var camara = jugador != null ? jugador.camara : Camera.main.transform;
        Quaternion mirada = camara.rotation;

        // 1. hacia el hilo de este sello, que se prende de a uno
        if (i < hilos.Length && hilos[i] != null)
        {
            yield return Girar(camara, Mirar(camara, Centro(hilos[i])), segundosPorTramo);
            hilos[i].Encender();
            yield return new WaitForSeconds(hilos[i].Duracion + 0.6f);
        }

        // 2. los dos sellos: hacia la puerta, el eclipse que se enciende y la puerta que se abre
        // (si los dos esperaban juntos, el eclipse va después del segundo hilo)
        if (TodosLosSellos() && pendientes.Count == 0)
        {
            if (eclipse != null)
            {
                yield return Girar(camara, Mirar(camara, Centro(eclipse)), segundosPorTramo);
                eclipse.Encender();
                yield return new WaitForSeconds(eclipse.Duracion);
            }
            if (puerta != null) puerta.Abrir();
            yield return new WaitForSeconds(esperaPuerta);
        }

        // 3. la mirada vuelve a donde estaba
        yield return Girar(camara, mirada, segundosPorTramo);
        if (jugador != null) jugador.enabled = true;
        if (pausa != null) pausa.enabled = true;
        Reproduciendo = false;
    }

    static Quaternion Mirar(Transform camara, Vector3 punto) => Quaternion.LookRotation(punto - camara.position);

    static Vector3 Centro(HiloDeTallados hilo)
    {
        if (hilo.tallados == null || hilo.tallados.Length == 0) return hilo.transform.position;
        Vector3 c = Vector3.zero;
        foreach (var t in hilo.tallados) if (t != null) c += t.transform.position;
        return c / hilo.tallados.Length;
    }

    /// <summary>Gira la cámara en el lugar, con arranque y frenada suaves.</summary>
    IEnumerator Girar(Transform camara, Quaternion hacia, float segundos)
    {
        Quaternion desde = camara.rotation;
        for (float t = 0f; t < segundos; t += Time.deltaTime)
        {
            camara.rotation = Quaternion.Slerp(desde, hacia, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / (segundos * 0.6f))));
            yield return null;
        }
    }
}
