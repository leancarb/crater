using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class ConstructorCrater
{
    static void IteracionDiseno(Kit k,Transform nivel)
    {
        // También se usa desde la actualización local: reemplazar, nunca acumular sistemas.
        foreach(var t in nivel.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Cierre_Entrada_Templo"||t.name=="Llegada_Rotonda"||t.name=="Visibilidad_Exterior").ToArray())
            Object.DestroyImmediate(t.gameObject);
        var exterior=nivel.Find("06_Capilla");
        var inicio=exterior.Find("Inicio_Elevado_Kit");
        CrearBordesNaturales(k,inicio,exterior);
        var spawn=exterior.Find("SpawnValle");spawn.localPosition=new Vector3(InicioElevadoX,AlturaInicioElevado(InicioElevadoX,InicioElevadoZ)+.05f,InicioElevadoZ);
        spawn.localRotation=Quaternion.Euler(0,180,0);
        var jugador=Object.FindFirstObjectByType<JugadorFPS>();jugador.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
        var cielo=new SerializedObject(Object.FindFirstObjectByType<CieloEclipse>());
        cielo.FindProperty("diametroAngular").floatValue=7f;cielo.ApplyModifiedPropertiesWithoutUndo();
        var mirador=BuscarObjetoDocente("Zona_Mirador");
        mirador.position=exterior.TransformPoint(new Vector3(0,1.5f,80));
        var prologo=new SerializedObject(Object.FindFirstObjectByType<PrologoCapilla>());
        prologo.FindProperty("pausaTotalidad").floatValue=1.2f;
        prologo.FindProperty("revelado").floatValue=2.4f;
        prologo.FindProperty("duracionEntrada").floatValue=1.2f;
        prologo.FindProperty("volumenGrave").floatValue=.18f;prologo.ApplyModifiedPropertiesWithoutUndo();
        var recogible=Object.FindObjectsByType<Recogible>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(r=>r.name=="Recogible_Linterna");
        var entrada=Grupo(nivel,"Cierre_Entrada_Templo");
        entrada.position=EspacioCrater.Punto(new Vector3(0,2,-3.2f));
        var losa=GameObject.CreatePrimitive(PrimitiveType.Cube);losa.transform.SetParent(entrada,false);
        losa.name="Losa_Entrada";losa.transform.localScale=new Vector3(15,4,.45f);losa.GetComponent<Renderer>().sharedMaterial=k.basalto;
        var puerta=entrada.gameObject.AddComponent<Compuerta>();puerta.iniciaAbierta=true;puerta.abrirSoloPorOrden=true;
        puerta.desplazamiento=Vector3.down*4.5f;puerta.duracion=1.2f;
        puerta.sonido=CrearFuenteHija(entrada,"Cierre_Piedra",k.audio.compuerta,.4f);
        var llegada=Grupo(nivel,"Llegada_Rotonda").gameObject.AddComponent<LlegadaRotonda>();
        llegada.farol=Object.FindFirstObjectByType<LinternaController>();llegada.entrada=puerta;
        var rotonda=BuscarObjetoDocente("03_Rotonda");
        var centro=EspacioCrater.Punto(CentroRotonda);
        llegada.luces=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(l=>l.type!=LightType.Directional && l.name!="Luz_Abertura_Rotonda" && l.intensity>0 &&
                (l.transform.IsChildOf(rotonda)||l.name=="Luz_Pasillo_O"||l.name=="Luz_Pasillo_E"||
                 (l.transform.position.y<7.5f && Vector2.Distance(new Vector2(l.transform.position.x,l.transform.position.z),new Vector2(centro.x,centro.z))<RadioRotonda*EspacioCrater.Escala)) &&
                l.GetComponentInParent<LinternaController>()==null && !l.transform.IsChildOf(recogible.transform)).ToArray();
        var cieloLobby=BuscarObjetoDocente("Luz_Abertura_Rotonda");if(cieloLobby!=null)cieloLobby.GetComponent<Light>().intensity=6;
        var pieza=BuscarObjetoDocente("Pieza_exterior_mirador");
        if(pieza!=null)pieza.position=EspacioCrater.Punto(new Vector3(-4,.3f,0));
        var atajo=Object.FindFirstObjectByType<AtajoOcultoInicio>();if(atajo!=null)atajo.gameObject.SetActive(false);
        var vis=Grupo(nivel,"Visibilidad_Exterior").gameObject.AddComponent<VisibilidadExterior>();
        var motivo=Object.FindFirstObjectByType<MotivoPorPerspectiva>();
        if(motivo!=null)motivo.destino=recogible.PuntoInteraccion;
        var cartel=BuscarObjetoDocente("Control_Mover");
        var mirar=BuscarObjetoDocente("Control_Mirar");if(mirar!=null)Object.DestroyImmediate(mirar.gameObject);
        if(cartel!=null)PlacaInstruccion(k,cartel.parent,"Control_Mirar",cartel.position+cartel.right*3.3f,-cartel.forward,new Vector2(1.4f,.9f),PanelMover);
        vis.paisaje=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>
            (r.transform.IsChildOf(exterior)||r.name=="Roca_Cierre_Valle"||r.bounds.min.y>=8.7f||r.transform.parent?.name=="Guia_Exterior_Eclipse")
            && r.GetComponentInParent<JugadorFPS>()==null
            && !r.name.Contains("Disco")&&!r.name.Contains("Corona")&&!r.name.Contains("Cielo")).ToArray();
    }
}
