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
    Recogible[] recogibles;
    LinternaController linterna;
    bool esperandoPrimerEncendido;
    void Start()
    {
        if(jugador!=null) linterna=jugador.GetComponentInChildren<LinternaController>();
        if(linterna!=null) mascaraObstaculos=linterna.capaObstaculos;
        recogibles=UnityEngine.Object.FindObjectsByType<Recogible>(FindObjectsInactive.Include,FindObjectsSortMode.None);
    }
    public Recogible ObjetoRecogible { get; private set; }
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
        ObjetoRecogible = null;
        if (!PuedeInteractuar) { interfaz?.MostrarInteraccionOpcional(""); return; }
        if(linterna!=null && linterna.Encendida) esperandoPrimerEncendido=false;
        foreach(var objeto in recogibles)
            if(objeto!=null && objeto.gameObject.activeInHierarchy && !objeto.Recogido &&
                (objeto.filtro==null || (linterna!=null && linterna.Disponible)) && EnVista(objeto.PuntoInteraccion,2.5f))
            { ObjetoRecogible=objeto;break; }
        if(ObjetoRecogible!=null)
        {
            interfaz?.MostrarInteraccionOpcional(EntradaCrater.BotonInteractuar+" · Recoger "+ObjetoRecogible.NombreInteraccion);
            if(EntradaCrater.Interactuar && ObjetoRecogible.Recoger(linterna))
            {
                esperandoPrimerEncendido=ObjetoRecogible.filtro==null;
                interfaz?.MostrarInteraccionOpcional(esperandoPrimerEncendido ? EntradaCrater.BotonLinterna+" · Encender farol" : "");
            }
            return;
        }
        foreach (var p in piezas)
            if (p != null && p.gameObject.activeInHierarchy && !p.Recogida && EnVista(p.transform,2.5f)) { Objetivo=p;break; }
        if (Objetivo != null)
        {
            interfaz?.MostrarInteraccionOpcional(EntradaCrater.BotonInteractuar+" · Recoger piedra tallada");
            if (EntradaCrater.Interactuar) Objetivo.Recoger();
        }
        else if (conjunto != null && conjunto.Transformado && !conjunto.Reproduciendo && EnVista(conjunto.transform,3f))
        {
            interfaz?.MostrarInteraccionOpcional(EntradaCrater.BotonInteractuar+" · Escuchar el conjunto");
            if (EntradaCrater.Interactuar) conjunto.Reproducir();
        }
        else interfaz?.MostrarInteraccionOpcional(esperandoPrimerEncendido ? EntradaCrater.BotonLinterna+" · Encender farol" : "");
    }
    void OnDisable() { interfaz?.MostrarInteraccionOpcional(""); }
}

