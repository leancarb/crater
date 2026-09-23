using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Chequeos no destructivos para la entrega del vertical slice.</summary>
public static class ValidarProyectoCrater
{
    public const string Escena = "Assets/Scenes/CraterVerticalSlice.unity";
    public const string FiltroCuerpo = "Assets/Data/Filtros/Filtro_Cuerpo.asset";
    public const string FiltroHueco = "Assets/Data/Filtros/Filtro_Hueco.asset";

    [MenuItem("Crater/Validar vertical slice")]
    public static void ValidarDesdeMenu()
    {
        int errores = Validar(true);
        EditorUtility.DisplayDialog("CRÁTER", errores == 0
            ? "El vertical slice superó todos los chequeos automáticos."
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

        Comprobar(AssetDatabase.LoadAssetAtPath<SceneAsset>(Escena) != null,
            $"Falta la escena oficial: {Escena}", problemas);

        var escenasBuild = EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
        Comprobar(escenasBuild.Length == 1 && escenasBuild[0] == Escena,
            "Build Settings debe contener únicamente CraterVerticalSlice.", problemas);

        var cuerpo = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(FiltroCuerpo);
        var hueco = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(FiltroHueco);
        Comprobar(cuerpo != null && cuerpo.canal == FiltroDefinicion.Canal.Cuerpo && cuerpo.nombreVisible == "CUERPO",
            "La definición canónica de CUERPO falta o está mal configurada.", problemas);
        Comprobar(hueco != null && hueco.canal == FiltroDefinicion.Canal.Hueco && hueco.nombreVisible == "HUECO",
            "La definición canónica de HUECO falta o está mal configurada.", problemas);
        Comprobar(Enum.GetNames(typeof(FiltroDefinicion.Canal)).Length == 3,
            "El enum de filtros debe contener sólo Ninguno, Cuerpo y Hueco.", problemas);

        string[] descartados = AssetDatabase.FindAssets("Ras" + "tro", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .ToArray();
        Comprobar(descartados.Length == 0,
            "Quedaron assets del tercer filtro descartado: " + string.Join(", ", descartados), problemas);

        string[] prefabsArte =
        {
            "PF_Anchor_Basalto.prefab", "PF_Reja_Hueco.prefab", "PF_Puente_Cuerpo.prefab",
            "PF_Linterna_Visual.prefab", "PF_Arquitectura_Modular.prefab", "PF_Motivos_Tallados.prefab"
        };
        foreach (string prefab in prefabsArte)
            Comprobar(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Art/" + prefab) != null,
                "Falta el prefab artístico " + prefab, problemas);

        var anclaArte = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Art/PF_Anchor_Basalto.prefab");
        var rejaArte = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Art/PF_Reja_Hueco.prefab");
        var puenteArte = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Art/PF_Puente_Cuerpo.prefab");
        Comprobar(anclaArte == null || (anclaArte.GetComponent<Ancla>() != null &&
                   anclaArte.GetComponent<Collider>() != null),
            "El prefab artístico del ancla no tiene receptor o collider.", problemas);
        Comprobar(rejaArte == null || (rejaArte.GetComponent<MateriaHueca>() != null &&
                   rejaArte.GetComponentsInChildren<Collider>(true).Length >= 2),
            "El prefab artístico de la reja no tiene materia HUECO o sus dos colliders.", problemas);
        Comprobar(puenteArte == null || (puenteArte.GetComponent<PuenteLuz>() != null &&
                   puenteArte.GetComponent<Collider>() != null),
            "El prefab artístico del puente no tiene PuenteLuz o collider.", problemas);

        int capaJugador = LayerMask.NameToLayer("Jugador");
        Comprobar(capaJugador >= 0, "Falta la capa Jugador.", problemas);

        var prefabJugador = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Jugador.prefab");
        Comprobar(prefabJugador != null, "Falta el prefab Jugador.", problemas);
        if (prefabJugador != null)
        {
            Comprobar(prefabJugador.CompareTag("Player"), "El prefab Jugador no tiene el tag Player.", problemas);
            Comprobar(prefabJugador.layer == capaJugador, "El prefab Jugador no usa la capa Jugador.", problemas);
            var linterna = prefabJugador.GetComponentInChildren<LinternaController>(true);
            Comprobar(linterna != null && linterna.filtros.Count == 2 &&
                       linterna.filtros[0] == cuerpo && linterna.filtros[1] == hueco,
                "El prefab Jugador no referencia exactamente los dos filtros canónicos.", problemas);
        }

        ValidarEscena(problemas);

        if (escribirLog)
        {
            if (problemas.Count == 0) Debug.Log("[CRÁTER] Validación completa: sin errores.");
            else foreach (string problema in problemas) Debug.LogError("[CRÁTER] " + problema);
        }
        return problemas.Count;
    }

    static void ValidarEscena(List<string> problemas)
    {
        if (!System.IO.File.Exists(Escena)) return;
        Scene escena = SceneManager.GetSceneByPath(Escena);
        bool yaCargada = escena.IsValid() && escena.isLoaded;
        if (!yaCargada) escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Additive);

        var objetos = escena.GetRootGameObjects().SelectMany(Descendientes).ToArray();
        string[] requeridos =
        {
            "Jugador", "LinternaEnPiso", "FlujoVerticalSlice", "ZonaCresta",
            "Capilla_Epilogo_Graybox", "ARTE_FINAL_CRATER"
        };
        foreach (string nombre in requeridos)
            Comprobar(objetos.Any(go => go.name == nombre), $"La escena no contiene {nombre}.", problemas);

        foreach (GameObject go in objetos)
            Comprobar(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0,
                $"{go.name} tiene un script faltante.", problemas);

        var jugador = objetos.FirstOrDefault(go => go.name == "Jugador");
        if (jugador != null)
        {
            Comprobar(jugador.CompareTag("Player"), "El Jugador de la escena no tiene tag Player.", problemas);
            Comprobar(jugador.GetComponent<CharacterController>() != null, "El Jugador no tiene CharacterController.", problemas);
            Comprobar(jugador.GetComponent<RespawnPorCaida>() != null,
                "El Jugador no tiene el sistema de respawn por caida.", problemas);
        }

        var linterna = objetos.Select(go => go.GetComponent<LinternaController>()).FirstOrDefault(c => c != null);
        Comprobar(linterna != null && linterna.filtros.Count == 2,
            "La linterna de la escena debe contener exactamente dos filtros.", problemas);

        var anclas = objetos.Select(go => go.GetComponent<Ancla>()).Where(item => item != null).ToArray();
        var rejas = objetos.Select(go => go.GetComponent<MateriaHueca>()).Where(item => item != null).ToArray();
        var puentes = objetos.Select(go => go.GetComponent<PuenteLuz>()).Where(item => item != null).ToArray();
        Comprobar(anclas.Length > 0 && anclas.All(item => item.transform.Find("ArteVisual") != null && item.indicador != null),
            "Alguna ancla no fue reemplazada por su modelo de basalto.", problemas);
        Comprobar(rejas.Length > 0 && rejas.All(item => item.transform.Find("ArteVisual") != null),
            "Alguna reja HUECO conserva el graybox visible.", problemas);
        Comprobar(puentes.Length > 0 && puentes.All(item => item.transform.Find("ArteVisual") != null &&
                   item.mallas != null && item.mallas.Length >= 6),
            "Algún puente CUERPO no usa la geometría modular de luz.", problemas);
        var pickups = objetos.Select(go => go.GetComponent<RecogerFiltro>()).Where(item => item != null).ToArray();
        Comprobar(pickups.Length == 2 && pickups.All(item => item.transform.Find("ArteVisual") != null &&
                   item.objetoVisual != null),
            "Los dos filtros recogibles deben usar el modelo artístico.", problemas);

        var raizArte = objetos.FirstOrDefault(go => go.name == "ARTE_FINAL_CRATER");
        Comprobar(raizArte != null && raizArte.GetComponentsInChildren<MeshRenderer>(true).Length >= 80,
            "La capa artística está vacía o incompleta.", problemas);

        if (!yaCargada) EditorSceneManager.CloseScene(escena, true);
    }

    static IEnumerable<GameObject> Descendientes(GameObject raiz)
    {
        yield return raiz;
        foreach (Transform hijo in raiz.transform)
            foreach (GameObject go in Descendientes(hijo.gameObject)) yield return go;
    }

    static void Comprobar(bool condicion, string mensaje, ICollection<string> problemas)
    {
        if (!condicion) problemas.Add(mensaje);
    }
}
