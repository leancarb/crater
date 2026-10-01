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
/// del jugador y la pausa, y mueve la cámara sin cortes, como en la cinemática del
/// eclipse: primero gira hacia el atajo mientras se abre, después vuela por los
/// puntos del 'camino' de ese sello (pasando por el atajo) hasta la 'vista' y ahí
/// enciende el hilo (HiloDeTallados en modo manual). Con todos los sellos, gira hacia
/// la 'vistaFinal', enciende el eclipse y abre la puerta (que espera esta orden:
/// 'abrirSoloPorOrden'). Al final vuelve por el mismo camino hasta los ojos del
/// jugador y le devuelve el control.
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
        bool linternaPrendida = linterna != null && linterna.Encendida;
        if (linternaPrendida) linterna.Encender(false);   // la linterna va con la cámara: no iluminar la rotonda

        var camara = jugador != null ? jugador.camara : Camera.main.transform;
        Transform padre = camara.parent;
        Vector3 posicionLocal = camara.localPosition;
        Quaternion rotacionLocal = camara.localRotation;
        camara.SetParent(null, true);
        Vector3 ojos = camara.position;
        Quaternion mirada = camara.rotation;

        // los puntos del vuelo: el lado del atajo del ala, el de la rotonda y la vista
        var camino = new List<Vector3>();
        for (int p = i * 2; p < i * 2 + 2 && caminos != null && p < caminos.Length; p++)
            if (caminos[p] != null) camino.Add(caminos[p].position);
        Transform vista = i < vistas.Length ? vistas[i] : null;

        // 1. mirar el atajo mientras se abre
        if (camino.Count > 0)
            yield return Girar(camara, Quaternion.LookRotation(camino[camino.Count - 1] - camara.position), esperaAtajo);

        // 2. volar hasta la vista
        foreach (var punto in camino) yield return Volar(camara, punto, null);
        if (vista != null) yield return Volar(camara, vista.position, vista.rotation);

        // 3. el hilo de este sello se prende de a uno
        if (i < hilos.Length && hilos[i] != null)
        {
            hilos[i].Encender();
            yield return new WaitForSeconds(hilos[i].Duracion + 0.6f);
        }

        // 4. los dos sellos: el eclipse sobre la puerta y la puerta que se abre
        if (TodosLosSellos() && vistaFinal != null)
        {
            yield return Volar(camara, vistaFinal.position, vistaFinal.rotation);
            if (eclipse != null)
            {
                eclipse.Encender();
                yield return new WaitForSeconds(eclipse.Duracion);
            }
            if (puerta != null) puerta.Abrir();
            yield return new WaitForSeconds(esperaPuerta);
            if (vista != null) yield return Volar(camara, vista.position, vista.rotation);
        }

        // 5. de vuelta por el mismo camino, hasta los ojos del jugador
        for (int p = camino.Count - 1; p >= 0; p--) yield return Volar(camara, camino[p], null);
        yield return Volar(camara, ojos, mirada);

        camara.SetParent(padre, false);
        camara.localPosition = posicionLocal;
        camara.localRotation = rotacionLocal;
        if (linternaPrendida) linterna.Encender(true);
        if (jugador != null) jugador.enabled = true;
        if (pausa != null) pausa.enabled = true;
        Reproduciendo = false;
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

    /// <summary>
    /// Lleva la cámara hasta 'destino'. Si no se da 'rotacion', mira hacia donde va;
    /// si se da, termina mirando así.
    /// </summary>
    IEnumerator Volar(Transform camara, Vector3 destino, Quaternion? rotacion)
    {
        Vector3 desde = camara.position;
        Quaternion giroDesde = camara.rotation;
        Vector3 rumbo = destino - desde;
        Quaternion giroHasta = rotacion ?? (rumbo.sqrMagnitude > 0.01f ? Quaternion.LookRotation(new Vector3(rumbo.x, rumbo.y * 0.3f, rumbo.z)) : giroDesde);
        float segundos = Mathf.Clamp(rumbo.magnitude / 5f, 0.6f, segundosPorTramo * 1.6f);
        for (float t = 0f; t < segundos; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / segundos);
            camara.SetPositionAndRotation(Vector3.Lerp(desde, destino, k), Quaternion.Slerp(giroDesde, giroHasta, Mathf.Clamp01(k * 1.4f)));
            yield return null;
        }
        camara.SetPositionAndRotation(destino, giroHasta);
    }
}
