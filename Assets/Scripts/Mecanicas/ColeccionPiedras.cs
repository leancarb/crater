using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Estado opcional de esta escena. Reiniciar la escena reinicia la colección, sin guardado adicional.</summary>
public class ColeccionPiedras : MonoBehaviour
{
    public PiezaTallada[] piezas;
    public ConjuntoTallado conjunto;
    public JugadorFPS jugador;
    public Camera camara;
    public InterfazCrater interfaz;
    public PrologoCapilla prologo;
    public FlujoJuegoCrater flujo;
    public AudioSource recogida;
    public Light luzRecogida;
    int mascaraObstaculos=1;
    void Start(){ if(jugador!=null)mascaraObstaculos=jugador.GetComponentInChildren<LinternaController>().capaObstaculos; }
    int indicesRegistrados;
    readonly HashSet<string> encontrados = new HashSet<string>();
    public event Action AlCambiar;
    public int Cantidad => encontrados.Count;
    public int Total => 6;
    public bool Completa => Cantidad == Total;
    public bool Contiene(string id) => encontrados.Contains(id);
    public PiezaTallada Objetivo { get; private set; }
    public bool PuedeInteractuar => jugador != null && jugador.enabled && !jugador.movimientoBloqueado &&
        !PausaCrater.EnPausa && !CinematicaDeSello.Reproduciendo && (prologo == null || !prologo.Reproduciendo) &&
        (flujo == null || (flujo.EtapaActual != FlujoJuegoCrater.Etapa.Cresta && flujo.EtapaActual != FlujoJuegoCrater.Etapa.Epilogo && flujo.EtapaActual != FlujoJuegoCrater.Etapa.Finalizado));

    public bool Registrar(PiezaTallada pieza)
    {
        if (pieza == null || pieza.coleccion != this || string.IsNullOrEmpty(pieza.identificador) || pieza.indice < 0 || pieza.indice >= Total) return false;
        bool pertenece = false;
        foreach (var p in piezas) if (p == pieza) { pertenece = true; break; }
        if (!pertenece || (indicesRegistrados & (1<<pieza.indice))!=0 || !encontrados.Add(pieza.identificador)) return false;
        indicesRegistrados |= 1<<pieza.indice;
        conjunto?.MostrarPieza(pieza.indice);
        if (recogida != null) { recogida.transform.position = pieza.transform.position; recogida.pitch = 1f + pieza.indice * 0.05f; recogida.Play(); }
        if(luzRecogida!=null){luzRecogida.transform.position=pieza.transform.position+Vector3.up*.2f;luzRecogida.intensity=2;}
        pieza.gameObject.SetActive(false);
        interfaz?.ActualizarColeccion(Cantidad, Total);
        AlCambiar?.Invoke();
        return true;
    }

    public bool EnVista(Transform t, float distancia)
    {
        if (camara == null || t == null) return false;
        Vector3 d = t.position - camara.transform.position;
        if (d.magnitude > distancia || Vector3.Dot(camara.transform.forward,d.normalized) < 0.82f) return false;
        int mascara = mascaraObstaculos;
        return !Physics.Raycast(camara.transform.position,d.normalized,Mathf.Max(0,d.magnitude-0.2f),mascara,QueryTriggerInteraction.Ignore);
    }

    void Update()
    {
        if(luzRecogida!=null)luzRecogida.intensity=Mathf.MoveTowards(luzRecogida.intensity,0,Time.deltaTime*3);
        Objetivo = null;
        if (!PuedeInteractuar) { interfaz?.MostrarInteraccionOpcional(""); return; }
        foreach (var p in piezas)
            if (p != null && p.gameObject.activeInHierarchy && !p.Recogida && EnVista(p.transform,2.5f)) { Objetivo=p;break; }
        var g=Gamepad.current; var k=Keyboard.current; var m=Mouse.current;
        bool mando=g!=null && g.lastUpdateTime > Math.Max(k!=null?k.lastUpdateTime:0,m!=null?m.lastUpdateTime:0);
        if (Objetivo != null)
        {
            interfaz?.MostrarInteraccionOpcional(mando?"A (×) · Recoger piedra tallada":"Espacio · Recoger piedra tallada");
            if (EntradaCrater.Saltar) Objetivo.Recoger();
        }
        else if (conjunto != null && conjunto.Transformado && !conjunto.Reproduciendo && EnVista(conjunto.transform,3f))
        {
            interfaz?.MostrarInteraccionOpcional(mando?"A (×) · Escuchar el conjunto":"Espacio · Escuchar el conjunto");
            if (EntradaCrater.Saltar) conjunto.Reproducir();
        }
        else interfaz?.MostrarInteraccionOpcional("");
    }
    void OnDisable() { interfaz?.MostrarInteraccionOpcional(""); }
}
