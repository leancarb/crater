using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Chequeos no destructivos antes de entregar o compilar: que la escena esté
/// completa, bien conectada y que cada puzzle se pueda resolver desde el piso.
///
/// CÓMO FUNCIONA
/// Abre la escena, junta todos sus objetos y revisa reglas: que existan las piezas
/// clave, que los eventos tengan a quién avisar, que las anclas estén al alcance de
/// su puente, etc. Cada regla que falla suma un mensaje a 'problemas'; al final se
/// escriben en la Consola. No modifica nada. Corre solo después de "Reconstruir todo".
/// </summary>
public static class ValidarProyectoCrater
{
    [MenuItem("Crater/Validar proyecto", priority = 10)]
    public static void ValidarDesdeMenu()
    {
        int errores = Validar(true);
        EditorUtility.DisplayDialog("CRÁTER", errores == 0
            ? "El proyecto superó todos los chequeos automáticos."
            : $"Se encontraron {errores} problemas. Revisá la Consola.", "Aceptar");
    }

    public static void ValidarDesdeLineaDeComandos()
    {
        int errores = Validar(true);
        if (errores > 0) throw new BuildFailedException($"CRÁTER tiene {errores} errores de validación.");
    }

    public static int Validar(bool escribirLog)
    {
        var problemas = new List<string>();

        Comprobar(AssetDatabase.LoadAssetAtPath<SceneAsset>(ConstructorCrater.RutaEscena) != null,
            $"Falta la escena: {ConstructorCrater.RutaEscena}", problemas);
        var escenasBuild = EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
        Comprobar(escenasBuild.Length == 1 && escenasBuild[0] == ConstructorCrater.RutaEscena,
            "Build Settings debe contener únicamente la escena del vertical slice.", problemas);

        var cuerpo = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(ConstructorCrater.RutaFiltroCuerpo);
        var hueco = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(ConstructorCrater.RutaFiltroHueco);
        Comprobar(cuerpo != null && cuerpo.canal == FiltroDefinicion.Canal.Cuerpo,
            "El filtro SOL falta o está mal configurado.", problemas);
        Comprobar(hueco != null && hueco.canal == FiltroDefinicion.Canal.Hueco,
            "El filtro LUNA falta o está mal configurado.", problemas);

        foreach (var capa in new[] { ConstructorCrater.CapaAncla, ConstructorCrater.CapaReceptor, ConstructorCrater.CapaJugador })
            Comprobar(LayerMask.NameToLayer(capa) >= 0, $"Falta la capa {capa}.", problemas);

        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ConstructorCrater.RutaPerfil);
        Comprobar(perfil != null && perfil.Has<ColorAdjustments>(),
            "El perfil de post-procesado no tiene Color Adjustments (lo necesita la adaptación a la oscuridad).", problemas);

        var jugador = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorCrater.CarpetaPrefabs + "Jugador.prefab");
        Comprobar(jugador != null && jugador.CompareTag("Player") && jugador.GetComponentInChildren<LinternaController>(true) != null,
            "El prefab Jugador falta, no tiene tag Player o no tiene linterna.", problemas);

        if (System.IO.File.Exists(ConstructorCrater.RutaEscena))
            ValidarEscena(cuerpo, hueco, problemas);

        if (escribirLog)
        {
            if (problemas.Count == 0) Debug.Log("[CRÁTER] Validación completa: sin errores.");
            else foreach (string problema in problemas) Debug.LogError("[CRÁTER] " + problema);
        }
        return problemas.Count;
    }

    static void ValidarEscena(FiltroDefinicion cuerpo, FiltroDefinicion hueco, List<string> problemas)
    {
        Scene escena = SceneManager.GetSceneByPath(ConstructorCrater.RutaEscena);
        bool yaCargada = escena.IsValid() && escena.isLoaded;
        if (!yaCargada) escena = EditorSceneManager.OpenScene(ConstructorCrater.RutaEscena, OpenSceneMode.Additive);
        try
        {
            var objetos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            T[] Todos<T>() where T : Component => objetos.Select(go => go.GetComponent<T>()).Where(c => c != null).ToArray();

            foreach (GameObject go in objetos)
                Comprobar(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0,
                    $"{go.name} tiene un script faltante.", problemas);

            var jugadores = objetos.Where(go => go.CompareTag("Player")).ToArray();
            Comprobar(jugadores.Length == 1, "Tiene que haber exactamente un objeto con tag Player.", problemas);
            Comprobar(Todos<Camera>().Length == 1 && Todos<AudioListener>().Length == 1,
                "Tiene que haber exactamente una cámara y un AudioListener.", problemas);

            var linternas = Todos<LinternaController>();
            Comprobar(linternas.Length == 1, "Tiene que haber exactamente una linterna.", problemas);
            var linterna = linternas.FirstOrDefault();
            if (linterna != null)
            {
                Comprobar(linterna.filtros.Count == 2 && linterna.filtros[0] == cuerpo && linterna.filtros[1] == hueco,
                    "La linterna tiene que tener SOL (1) y LUNA (2), en ese orden.", problemas);
                Comprobar(linterna.filtrosDesbloqueados.Count == 0, "La linterna no puede empezar con filtros desbloqueados.", problemas);
                Comprobar(linterna.requiereRecogerla, "La linterna tiene que empezar sin recoger.", problemas);
            }

            var colecciones=Todos<ColeccionPiedras>();
            Comprobar(colecciones.Length==1,"Debe haber una colección opcional.",problemas);
            var coleccion=colecciones.FirstOrDefault();var piezas=Todos<PiezaTallada>();
            Comprobar(piezas.Length==6 && piezas.Select(p=>p.identificador).Distinct().Count()==6 && piezas.Select(p=>p.indice).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,6)),"Las seis piedras necesitan IDs e índices únicos.",problemas);
            if(coleccion!=null)
            {
                Comprobar(coleccion.piezas!=null && coleccion.piezas.Length==6 && coleccion.piezas.All(p=>p!=null && p.coleccion==coleccion),"Referencias de piedras incompletas.",problemas);
                Comprobar(coleccion.conjunto!=null && coleccion.conjunto.fragmentos.Length==6 && coleccion.conjunto.fragmentos.All(r=>r!=null) && coleccion.conjunto.proyeccion!=null && coleccion.conjunto.nota!=null && coleccion.conjunto.voces!=null && coleccion.conjunto.voces.Length==6 && coleccion.conjunto.voces.All(v=>v!=null),"Conjunto opcional incompleto.",problemas);
                Comprobar(coleccion.jugador!=null && coleccion.camara!=null && coleccion.interfaz!=null && coleccion.flujo!=null && coleccion.prologo!=null,"La colección debe respetar controles y flujo.",problemas);
            }
            Comprobar(Todos<AguaEnCuenco>().Length==1 && Todos<AguaEnCuenco>().All(a=>a.sonda!=null && a.sonda.resolution==64),"Falta el reflejo limitado a 64px.",problemas);
            Comprobar(Todos<ParticleSystem>().Where(p=>p.name=="Polvo_Abertura").All(p=>p.main.maxParticles<=20),"Polvo por encima del presupuesto.",problemas);
            var recogibles = Todos<Recogible>();
            Comprobar(recogibles.Count(r => r.filtro == null) == 1, "Tiene que haber una única linterna para recoger.", problemas);
            Comprobar(recogibles.Count(r => r.filtro == cuerpo) == 1 && recogibles.Count(r => r.filtro == hueco) == 1,
                "Tiene que haber un filtro SOL y un filtro LUNA para recoger.", problemas);

            foreach (var puente in Todos<PuenteLuz>())
            {
                Comprobar(puente.anclas.Count > 0 && puente.anclas.All(a => a != null),
                    $"{puente.name} no tiene anclas asignadas.", problemas);
                Comprobar(puente.colliders != null && puente.colliders.Length > 0, $"{puente.name} no tiene collider.", problemas);
                float alcance = cuerpo != null ? cuerpo.alcance : 14f;
                foreach (var ancla in puente.anclas.Where(a => a != null))
                {
                    Comprobar(ancla.canalRequerido == FiltroDefinicion.Canal.Cuerpo,
                        $"{ancla.name} sostiene un puente pero no responde al filtro SOL.", problemas);
                    Comprobar(Vector3.Distance(ancla.PuntoDeImpacto, puente.transform.position) < alcance - 2f,
                        $"{ancla.name} queda demasiado lejos de {puente.name} para alcanzarla con el haz.", problemas);
                }
            }

            foreach (var reja in Todos<MateriaHueca>())
            {
                Comprobar(reja.canalRequerido == FiltroDefinicion.Canal.Hueco, $"{reja.name} no responde al filtro LUNA.", problemas);
                var colliders = reja.GetComponents<Collider>();
                Comprobar(colliders.Any(c => c.isTrigger) && colliders.Any(c => !c.isTrigger),
                    $"{reja.name} necesita un collider sólido y un trigger en el mismo objeto.", problemas);
            }

            foreach (var ancla in Todos<Ancla>())
                Comprobar(LayerMask.LayerToName(ancla.gameObject.layer) == ConstructorCrater.CapaAncla,
                    $"{ancla.name} no está en la capa Ancla.", problemas);

            // todas las compuertas se abren con receptores, salvo el cierre de la Cresta (lo cierra la zona)
            var compuertas = Todos<Compuerta>();
            Compuerta Compuerta(string nombre) => compuertas.FirstOrDefault(c => c.name == nombre);
            Comprobar(compuertas.Where(c => c.name != "Cierre_Cresta" && !c.name.StartsWith("Losa_Red_")).All(c => c.receptores.Count > 0 && c.receptores.All(r => r != null)),
                "Hay una compuerta sin receptores (sólo el cierre de la Cresta puede no tenerlos).", problemas);
            Comprobar(Compuerta("Cierre_Cresta") != null, "Falta el cierre de la Cresta.", problemas);
            foreach (var nombre in new[] { "Compuerta_Umbral", "Puerta_Sellos", "Atajo_Oeste", "Atajo_Este" })
                Comprobar(Compuerta(nombre) != null && TieneOyentes(Compuerta(nombre).alAbrirse), $"Falta {nombre} o no avisa al flujo del juego.", problemas);
            var sellos = Compuerta("Puerta_Sellos");
            Comprobar(sellos != null && sellos.receptores.Count == 2 && sellos.receptores.All(r => r != null && r.permanente),
                "La puerta de los sellos tiene que abrirse con los dos sellos, y los sellos tienen que ser permanentes.", problemas);

            // Cresta, mirador, pozo, lugar del cráter y escaleras de recuperación
            var zonas = Todos<ZonaJugador>();
            Comprobar(zonas.Length == 8 && zonas.All(z => TieneOyentes(z.alEntrar) && z.GetComponent<Collider>().isTrigger),
                "Tiene que haber 8 zonas conectadas: Cresta, mirador, pozo, epílogo y cuatro escaleras de recuperación.", problemas);
            foreach (string n in new[] { "O1", "O3", "N1", "N2" })
                Comprobar(Compuerta("Losa_Red_" + n) != null && zonas.Any(z => z.name == "Pie_Escalera_" + n),
                    $"Falta la recuperación de {n}.", problemas);
            Comprobar(Todos<PuenteLuz>().All(p => p.anclas.Count == 2), "Cada puente debe tener dos anclas.", problemas);
            var puertaSol = Compuerta("Puerta_O_Ancla");
            Comprobar(puertaSol != null && !puertaSol.sostenida && puertaSol.receptores.Count == 1,
                "La puerta del ala SOL debe tener una ancla y quedar abierta.", problemas);

            Comprobar(Todos<PrologoCapilla>().Length == 1 && Todos<CieloEclipse>().Length == 1,
                "Falta el prólogo de la capilla o el cielo del eclipse.", problemas);
            Comprobar(Todos<AperturaTecho>().Length == 1, "Falta el techo que se abre en la Cresta.", problemas);

            var adaptacion = Todos<AdaptacionOscuridad>().FirstOrDefault();
            Comprobar(adaptacion != null && adaptacion.GetComponent<Volume>() != null && TieneOyentes(adaptacion.alAdaptarse),
                "La adaptación a la oscuridad falta, no está en el Volume o no dispara el final.", problemas);

            Comprobar(Todos<FlujoJuegoCrater>().Length == 1 && Todos<EclipseFinalController>().Length == 1 &&
                      Todos<InterfazCrater>().Length == 1 && Todos<PausaCrater>().Length == 1,
                "Faltan sistemas (flujo, eclipse, interfaz o pausa).", problemas);
        }
        finally
        {
            if (!yaCargada) EditorSceneManager.CloseScene(escena, true);
        }
    }

    static bool TieneOyentes(UnityEventBase evento) => evento != null && evento.GetPersistentEventCount() > 0;

    static void Comprobar(bool condicion, string mensaje, ICollection<string> problemas)
    {
        if (!condicion) problemas.Add(mensaje);
    }
}
