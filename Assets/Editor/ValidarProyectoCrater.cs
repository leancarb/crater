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
            "El filtro CUERPO falta o está mal configurado.", problemas);
        Comprobar(hueco != null && hueco.canal == FiltroDefinicion.Canal.Hueco,
            "El filtro HUECO falta o está mal configurado.", problemas);

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
                    "La linterna tiene que tener CUERPO (1) y HUECO (2), en ese orden.", problemas);
                Comprobar(linterna.filtrosDesbloqueados.Count == 0, "La linterna no puede empezar con filtros desbloqueados.", problemas);
                Comprobar(linterna.requiereRecogerla, "La linterna tiene que empezar sin recoger.", problemas);
            }

            var recogibles = Todos<Recogible>();
            Comprobar(recogibles.Count(r => r.filtro == null) == 1, "Tiene que haber una única linterna para recoger.", problemas);
            Comprobar(recogibles.Count(r => r.filtro == cuerpo) == 1 && recogibles.Count(r => r.filtro == hueco) == 1,
                "Tiene que haber un filtro CUERPO y un filtro HUECO para recoger.", problemas);

            foreach (var puente in Todos<PuenteLuz>())
            {
                Comprobar(puente.anclas.Count > 0 && puente.anclas.All(a => a != null),
                    $"{puente.name} no tiene anclas asignadas.", problemas);
                Comprobar(puente.colliders != null && puente.colliders.Length > 0, $"{puente.name} no tiene collider.", problemas);
                float alcance = cuerpo != null ? cuerpo.alcance : 14f;
                foreach (var ancla in puente.anclas.Where(a => a != null))
                {
                    Comprobar(ancla.canalRequerido == FiltroDefinicion.Canal.Cuerpo,
                        $"{ancla.name} sostiene un puente pero no responde a CUERPO.", problemas);
                    Comprobar(Vector3.Distance(ancla.PuntoDeImpacto, puente.transform.position) < alcance - 2f,
                        $"{ancla.name} queda demasiado lejos de {puente.name} para alcanzarla con el haz.", problemas);
                }
            }

            foreach (var reja in Todos<MateriaHueca>())
            {
                Comprobar(reja.canalRequerido == FiltroDefinicion.Canal.Hueco, $"{reja.name} no responde a HUECO.", problemas);
                var colliders = reja.GetComponents<Collider>();
                Comprobar(colliders.Any(c => c.isTrigger) && colliders.Any(c => !c.isTrigger),
                    $"{reja.name} necesita un collider sólido y un trigger en el mismo objeto.", problemas);
            }

            foreach (var ancla in Todos<Ancla>())
                Comprobar(LayerMask.LayerToName(ancla.gameObject.layer) == ConstructorCrater.CapaAncla,
                    $"{ancla.name} no está en la capa Ancla.", problemas);

            var compuertas = Todos<Compuerta>();
            Comprobar(compuertas.Length == 1 && compuertas[0].receptores.Count > 0,
                "Falta la compuerta del Umbral o no tiene receptores.", problemas);
            Comprobar(compuertas.All(c => TieneOyentes(c.alAbrirse)), "La compuerta no avisa al flujo del juego.", problemas);

            // Cresta, cruce de la puerta del eclipse, umbral de la capilla, puerta del cráter del valle y lugar del cráter
            var zonas = Todos<ZonaJugador>();
            Comprobar(zonas.Length == 5 && zonas.All(z => TieneOyentes(z.alEntrar) && z.GetComponent<Collider>().isTrigger),
                "Tiene que haber 5 zonas (Cresta, cruce, umbral de la capilla, puerta del cráter y lugar del cráter), todas triggers conectados.", problemas);

            Comprobar(Todos<PrologoCapilla>().Length == 1 && Todos<CieloEclipse>().Length == 1,
                "Falta el prólogo de la capilla o el cielo del eclipse.", problemas);
            var puertas = Todos<PuertaEclipse>();
            Comprobar(puertas.Length == 1, "Falta la puerta del eclipse en la Cresta.", problemas);

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
