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
/// del jugador y la pausa, funde a negro, saca la cámara del jugador y la pone en la
/// 'vista' de ese sello, funde desde negro y enciende el hilo (HiloDeTallados en modo
/// manual). Con todos los sellos, gira hacia la 'vistaFinal', enciende el eclipse y
/// abre la puerta (que espera esta orden: 'abrirSoloPorOrden'). Al final funde a
/// negro, devuelve la cámara a su lugar y devuelve el control.
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
        bool linternaPrendida = linterna != null && linterna.Encendida;

        var camara = jugador != null ? jugador.camara : Camera.main.transform;
        Transform padre = camara.parent;
        Vector3 posicionLocal = camara.localPosition;
        Quaternion rotacionLocal = camara.localRotation;

        if (interfaz != null) yield return interfaz.Fundir(Color.black, 0f, 1f, 0.6f);
        if (linternaPrendida) linterna.Encender(false);   // la linterna va con la cámara: no iluminar la rotonda
        camara.SetParent(null, true);
        if (i < vistas.Length && vistas[i] != null) camara.SetPositionAndRotation(vistas[i].position, vistas[i].rotation);
        if (interfaz != null) yield return interfaz.Fundir(Color.black, 1f, 0f, 0.8f);

        // el hilo de este sello se prende de a uno
        if (i < hilos.Length && hilos[i] != null)
        {
            hilos[i].Encender();
            yield return new WaitForSeconds(hilos[i].Duracion + 0.6f);
        }

        // los dos sellos: el eclipse sobre la puerta y la puerta que se abre
        if (TodosLosSellos() && vistaFinal != null)
        {
            Quaternion desde = camara.rotation;
            Vector3 desdePos = camara.position;
            for (float t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 1.5f);
                camara.SetPositionAndRotation(Vector3.Lerp(desdePos, vistaFinal.position, k), Quaternion.Slerp(desde, vistaFinal.rotation, k));
                yield return null;
            }
            if (eclipse != null)
            {
                eclipse.Encender();
                yield return new WaitForSeconds(eclipse.Duracion);
            }
            if (puerta != null) puerta.Abrir();
            yield return new WaitForSeconds(esperaPuerta);
        }

        if (interfaz != null) yield return interfaz.Fundir(Color.black, 0f, 1f, 0.6f);
        camara.SetParent(padre, false);
        camara.localPosition = posicionLocal;
        camara.localRotation = rotacionLocal;
        if (linternaPrendida) linterna.Encender(true);
        if (interfaz != null) yield return interfaz.Fundir(Color.black, 1f, 0f, 0.8f);

        if (jugador != null) jugador.enabled = true;
        if (pausa != null) pausa.enabled = true;
        Reproduciendo = false;
    }
}
