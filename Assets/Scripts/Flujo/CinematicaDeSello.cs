using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cuando se enciende un sello, el juego toma la cámara (como en la cinemática del
/// eclipse) y la lleva a la rotonda para mostrar cómo se prende, de a uno, el hilo de
/// tallados de ese sello. Con los dos sellos, además se enciende el eclipse sobre la
/// puerta y la puerta se abre a la vista. Después la cámara vuelve al jugador.
///
/// CÓMO FUNCIONA
/// Mira cada frame qué sellos se activaron y los pone en una cola (si se encienden
/// dos casi juntos, se muestran uno después del otro). Para cada uno: apaga el control
/// del jugador y la pausa y, como en la cinemática del eclipse, la cámara no se mueve
/// de los ojos del jugador: sólo gira. Primero hacia el atajo mientras se abre, después
/// (por el atajo) hacia el hilo de ese sello, que se enciende (HiloDeTallados en modo
/// manual). Con todos los sellos, gira hacia el eclipse sobre la puerta, lo enciende y
/// abre la puerta (que espera esta orden: 'abrirSoloPorOrden'). Al final la mirada
/// vuelve a donde estaba y se devuelve el control.
/// </summary>
public class CinematicaDeSello : MonoBehaviour
{
    public static bool Reproduciendo { get; private set; }

    [SerializeField] JugadorFPS jugador;
    [SerializeField] InterfazCrater interfaz;
    [SerializeField] PausaCrater pausa;
    [SerializeField] LinternaController linterna;

    [Header("Un hilo y una vista por sello (mismo orden)")]
    [SerializeField] ReceptorDeLuz[] sellos;
    [SerializeField] HiloDeTallados[] hilos;
    [SerializeField] Transform[] vistas;
    [Tooltip("Por sello: el punto de su lado del atajo y el de la rotonda (dos por sello, en orden).")]
    [SerializeField] Transform[] caminos;
    [SerializeField] float esperaAtajo = 2.2f;
    [SerializeField] float segundosPorTramo = 1.4f;

    [Header("Con todos los sellos")]
    [SerializeField] HiloDeTallados eclipse;
    [SerializeField] Transform vistaFinal;
    [SerializeField] Compuerta puerta;
    [SerializeField] float esperaPuerta = 3.5f;

    // los campos estáticos sobreviven entre partidas en el editor: se limpian al dar Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico() => Reproduciendo = false;

    readonly Queue<int> pendientes = new Queue<int>();
    bool[] mostrado;

    void Start() => mostrado = new bool[sellos != null ? sellos.Length : 0];

    void Update()
    {
        for (int i = 0; i < mostrado.Length; i++)
        {
            if (mostrado[i] || sellos[i] == null || !sellos[i].Activo) continue;
            mostrado[i] = true;
            pendientes.Enqueue(i);
        }
        if (!Reproduciendo && pendientes.Count > 0) StartCoroutine(Mostrar(pendientes.Dequeue()));
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

        // 1. mirar el atajo mientras se abre; 2. por el atajo, hacia el hilo que se prende
        Vector3 atajo = caminos != null && i * 2 + 1 < caminos.Length && caminos[i * 2 + 1] != null
            ? caminos[i * 2 + 1].position : camara.position + camara.forward;
        yield return Girar(camara, Mirar(camara, atajo), esperaAtajo);
        if (i < hilos.Length && hilos[i] != null)
        {
            yield return Girar(camara, Mirar(camara, Centro(hilos[i])), segundosPorTramo);
            hilos[i].Encender();
            yield return new WaitForSeconds(hilos[i].Duracion + 0.6f);
        }

        // 3. los dos sellos: hacia la puerta, el eclipse que se enciende y la puerta que se abre
        if (TodosLosSellos())
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

        // 4. la mirada vuelve a donde estaba
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
