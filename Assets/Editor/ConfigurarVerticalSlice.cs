using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Migración idempotente del prototipo al vertical slice de dos filtros.</summary>
public static class ConfigurarVerticalSlice
{
    const string Escena = ValidarProyectoCrater.Escena;

    [MenuItem("Crater/Configurar vertical slice")]
    public static void AplicarDesdeMenu()
    {
        Aplicar();
        EditorUtility.DisplayDialog("CRÁTER", "Vertical slice configurado. Ejecutá Crater > Validar vertical slice.", "Aceptar");
    }

    public static void AplicarDesdeLineaDeComandos()
    {
        Aplicar();
        ValidarProyectoCrater.ValidarDesdeLineaDeComandos();
    }

    public static void Aplicar()
    {
        CrearCarpeta("Assets/Data");
        CrearCarpeta("Assets/Data/Filtros");
        CrearCarpeta("Assets/Materials");
        CrearCarpeta("Assets/Materials/Capilla");

        var cuerpo = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(ValidarProyectoCrater.FiltroCuerpo);
        var hueco = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(ValidarProyectoCrater.FiltroHueco);
        if (cuerpo == null || hueco == null)
            throw new InvalidOperationException("No se encontraron los dos filtros canónicos.");

        ConfigurarFiltro(cuerpo, "CUERPO", FiltroDefinicion.Canal.Cuerpo,
            new Color(0.91f, 0.63f, 0.29f), 28f, 14f, 800f);
        ConfigurarFiltro(hueco, "HUECO", FiltroDefinicion.Canal.Hueco,
            new Color(0.29f, 0.36f, 0.91f), 28f, 14f, 800f);

        int capaJugador = AsegurarLayer("Jugador", 10);
        int capaAncla = LayerMask.NameToLayer("Ancla");
        int capaReceptor = LayerMask.NameToLayer("Receptor");
        if (capaAncla < 0 || capaReceptor < 0)
            throw new InvalidOperationException("Faltan las capas Ancla o Receptor.");

        ConfigurarJugadorPrefab(cuerpo, hueco, capaJugador, capaAncla, capaReceptor);
        ConfigurarPickup("Assets/Prefabs/PickupFiltroCuerpo.prefab", cuerpo);
        ConfigurarPickup("Assets/Prefabs/PickupFiltroHueco.prefab", hueco);
        ConfigurarMateriaHueca();

        var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
        ConfigurarEscena(escena, cuerpo, hueco, capaJugador, capaAncla, capaReceptor);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Escena, true) };
        PlayerSettings.productName = "CRÁTER";
        PlayerSettings.bundleVersion = "0.2.0";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true;

        EditorSceneManager.SaveScene(escena);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CRÁTER] Vertical slice configurado correctamente.");
    }

    static void ConfigurarEscena(Scene escena, FiltroDefinicion cuerpo, FiltroDefinicion hueco,
                                 int capaJugador, int capaAncla, int capaReceptor)
    {
        var jugador = Buscar(escena, "Jugador");
        var linternaGO = Buscar(escena, "Linterna");
        var volumenGO = Buscar(escena, "Global Volume");
        if (jugador == null || linternaGO == null || volumenGO == null)
            throw new InvalidOperationException("La escena no contiene Jugador, Linterna o Global Volume.");

        jugador.tag = "Player";
        PonerLayerRecursivo(jugador, capaJugador);

        var linterna = linternaGO.GetComponent<LinternaController>();
        linterna.filtros.Clear();
        linterna.filtros.Add(cuerpo);
        linterna.filtros.Add(hueco);
        linterna.filtrosDesbloqueados.Clear();
        linterna.capaReceptores = (1 << capaAncla) | (1 << capaReceptor);
        linterna.capaObstaculos = 1 << LayerMask.NameToLayer("Default");
        linterna.capaEspejos = 0;
        linterna.requiereRecogerla = true;

        var adaptacion = volumenGO.GetComponent<AdaptacionOscuridad>();
        if (adaptacion == null) adaptacion = volumenGO.AddComponent<AdaptacionOscuridad>();
        adaptacion.tiempoDeAdaptacion = 5f;
        adaptacion.demoraInicial = 4f;
        adaptacion.exposicionNormal = 0f;
        adaptacion.exposicionAdaptada = 3.2f;

        var finalViejo = Buscar(escena, "FinDelJuego");
        if (finalViejo != null) finalViejo.SetActive(false);

        var capilla = CrearCapilla(escena);
        var spawnCapilla = BuscarHijo(capilla.transform, "SpawnCapilla");

        var sistema = Buscar(escena, "FlujoVerticalSlice");
        if (sistema == null)
        {
            sistema = new GameObject("FlujoVerticalSlice");
            SceneManager.MoveGameObjectToScene(sistema, escena);
        }

        var interfaz = ObtenerOAgregar<InterfazCrater>(sistema);
        var eclipse = ObtenerOAgregar<EclipseFinalController>(sistema);
        var flujo = ObtenerOAgregar<FlujoJuegoCrater>(sistema);
        var respawn = ObtenerOAgregar<RespawnPorCaida>(jugador);
        interfaz.ConfigurarLinterna(linterna);
        respawn.Configurar(interfaz);
        eclipse.Configurar(adaptacion, linterna, interfaz, jugador.transform, spawnCapilla);
        flujo.Configurar(linterna, interfaz, eclipse);
        eclipse.AsignarFlujo(flujo);

        CrearZonaCresta(escena, flujo);
        var zonaFinal = BuscarHijo(capilla.transform, "ZonaFinalCapilla").GetComponent<ZonaFinalCapilla>();
        zonaFinal.Configurar(eclipse);

        // La configuración reconstruye el epílogo; regenerar al final garantiza
        // que los builds nunca vuelvan a mostrar el graybox.
        ReemplazarGrayboxCrater.AplicarSobreEscena(escena);

        EditorUtility.SetDirty(jugador);
        EditorUtility.SetDirty(linterna);
        EditorUtility.SetDirty(adaptacion);
        EditorUtility.SetDirty(sistema);
    }

    static void ConfigurarJugadorPrefab(FiltroDefinicion cuerpo, FiltroDefinicion hueco,
                                        int capaJugador, int capaAncla, int capaReceptor)
    {
        const string ruta = "Assets/Prefabs/Jugador.prefab";
        var raiz = PrefabUtility.LoadPrefabContents(ruta);
        try
        {
            raiz.tag = "Player";
            PonerLayerRecursivo(raiz, capaJugador);
            var linterna = raiz.GetComponentInChildren<LinternaController>(true);
            if (linterna == null) throw new InvalidOperationException("El prefab Jugador no contiene LinternaController.");
            linterna.filtros.Clear();
            linterna.filtros.Add(cuerpo);
            linterna.filtros.Add(hueco);
            linterna.filtrosDesbloqueados.Clear();
            linterna.capaReceptores = (1 << capaAncla) | (1 << capaReceptor);
            linterna.capaObstaculos = 1 << LayerMask.NameToLayer("Default");
            linterna.capaEspejos = 0;
            linterna.requiereRecogerla = true;
            ObtenerOAgregar<RespawnPorCaida>(raiz);
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    static void ConfigurarPickup(string ruta, FiltroDefinicion filtro)
    {
        var raiz = PrefabUtility.LoadPrefabContents(ruta);
        try
        {
            var recoger = raiz.GetComponent<RecogerFiltro>();
            if (recoger == null) throw new InvalidOperationException($"{ruta} no contiene RecogerFiltro.");
            recoger.filtro = filtro;
            recoger.equiparAlRecoger = false;
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    static void ConfigurarMateriaHueca()
    {
        const string ruta = "Assets/Prefabs/Reja.prefab";
        var raiz = PrefabUtility.LoadPrefabContents(ruta);
        try
        {
            var materia = raiz.GetComponent<MateriaHueca>();
            if (materia == null) throw new InvalidOperationException("Reja.prefab no contiene MateriaHueca.");
            materia.canalRequerido = FiltroDefinicion.Canal.Hueco;
            PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }

    static GameObject CrearCapilla(Scene escena)
    {
        var existente = Buscar(escena, "Capilla_Epilogo_Graybox");
        if (existente != null) UnityEngine.Object.DestroyImmediate(existente);

        var raiz = new GameObject("Capilla_Epilogo_Graybox");
        SceneManager.MoveGameObjectToScene(raiz, escena);
        raiz.transform.position = new Vector3(300f, 0f, 0f);

        var adobe = CrearMaterial("Assets/Materials/Capilla/Adobe.mat", new Color(0.68f, 0.43f, 0.25f), 0.85f);
        var paja = CrearMaterial("Assets/Materials/Capilla/TechoPaja.mat", new Color(0.27f, 0.18f, 0.09f), 0.95f);
        var piedra = CrearMaterial("Assets/Materials/Capilla/Piedra.mat", new Color(0.34f, 0.32f, 0.29f), 0.9f);
        var tierra = CrearMaterial("Assets/Materials/Capilla/Tierra.mat", new Color(0.48f, 0.23f, 0.16f), 1f);

        CrearCubo(raiz.transform, "PisoCapilla", new Vector3(0f, -0.1f, 0f), new Vector3(8f, 0.2f, 14f), piedra);
        CrearCubo(raiz.transform, "ParedIzquierda", new Vector3(-4f, 2.2f, 0f), new Vector3(0.35f, 4.5f, 14f), adobe);
        CrearCubo(raiz.transform, "ParedDerecha", new Vector3(4f, 2.2f, 0f), new Vector3(0.35f, 4.5f, 14f), adobe);
        CrearCubo(raiz.transform, "ParedFondo", new Vector3(0f, 2.2f, -7f), new Vector3(8f, 4.5f, 0.35f), adobe);
        CrearCubo(raiz.transform, "FrenteIzquierdo", new Vector3(-2.6f, 2.2f, 7f), new Vector3(2.8f, 4.5f, 0.35f), adobe);
        CrearCubo(raiz.transform, "FrenteDerecho", new Vector3(2.6f, 2.2f, 7f), new Vector3(2.8f, 4.5f, 0.35f), adobe);
        CrearCubo(raiz.transform, "Dintel", new Vector3(0f, 3.8f, 7f), new Vector3(2.4f, 1.3f, 0.35f), adobe);

        var techoIzq = CrearCubo(raiz.transform, "TechoIzquierdo", new Vector3(-2f, 5f, 0f), new Vector3(4.8f, 0.35f, 15f), paja);
        techoIzq.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        var techoDer = CrearCubo(raiz.transform, "TechoDerecho", new Vector3(2f, 5f, 0f), new Vector3(4.8f, 0.35f, 15f), paja);
        techoDer.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

        CrearCubo(raiz.transform, "Atrio", new Vector3(0f, -0.1f, 11f), new Vector3(7f, 0.2f, 8f), piedra);
        CrearCubo(raiz.transform, "TerrenoExterior", new Vector3(0f, -0.35f, 25f), new Vector3(45f, 0.5f, 35f), tierra);
        CrearCubo(raiz.transform, "CerroLejanoA", new Vector3(-14f, 3f, 34f), new Vector3(18f, 6f, 5f), tierra);
        CrearCubo(raiz.transform, "CerroLejanoB", new Vector3(12f, 2f, 36f), new Vector3(16f, 4f, 6f), tierra);

        var luzGO = new GameObject("LuzInterior");
        luzGO.transform.SetParent(raiz.transform, false);
        luzGO.transform.localPosition = new Vector3(0f, 3.5f, 1f);
        var luz = luzGO.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.78f, 0.55f);
        luz.intensity = 900f;
        luz.range = 12f;

        var spawn = new GameObject("SpawnCapilla");
        spawn.transform.SetParent(raiz.transform, false);
        spawn.transform.localPosition = new Vector3(0f, 0.15f, -4.5f);
        spawn.transform.localRotation = Quaternion.identity;

        var zonaGO = new GameObject("ZonaFinalCapilla");
        zonaGO.transform.SetParent(raiz.transform, false);
        zonaGO.transform.localPosition = new Vector3(0f, 1.5f, 17f);
        var collider = zonaGO.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(9f, 3f, 2f);
        zonaGO.AddComponent<ZonaFinalCapilla>();

        return raiz;
    }

    static void CrearZonaCresta(Scene escena, FlujoJuegoCrater flujo)
    {
        var anterior = Buscar(escena, "ZonaCresta");
        if (anterior != null) UnityEngine.Object.DestroyImmediate(anterior);

        var puerta = Buscar(escena, "Puerta_Cresta");
        if (puerta == null) throw new InvalidOperationException("No se encontró Puerta_Cresta.");

        var zonaGO = new GameObject("ZonaCresta");
        SceneManager.MoveGameObjectToScene(zonaGO, escena);
        zonaGO.transform.position = puerta.transform.position - Vector3.forward * 7f + Vector3.up * 1.5f;
        var collider = zonaGO.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(10f, 3f, 10f);
        zonaGO.AddComponent<ZonaCresta>().Configurar(flujo);
    }

    static GameObject CrearCubo(Transform padre, string nombre, Vector3 posicion,
                                Vector3 escala, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        go.transform.localScale = escala;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    static Material CrearMaterial(string ruta, Color color, float suavidad)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, ruta);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 1f - suavidad);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void ConfigurarFiltro(FiltroDefinicion filtro, string nombre, FiltroDefinicion.Canal canal,
                                 Color color, float angulo, float alcance, float intensidad)
    {
        filtro.nombreVisible = nombre;
        filtro.canal = canal;
        filtro.color = color;
        filtro.anguloCono = angulo;
        filtro.alcance = alcance;
        filtro.intensidad = intensidad;
        filtro.tiempoDeCarga = 0.35f;
        EditorUtility.SetDirty(filtro);
    }

    static int AsegurarLayer(string nombre, int indicePreferido)
    {
        int existente = LayerMask.NameToLayer(nombre);
        if (existente >= 0) return existente;

        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        var serializado = new SerializedObject(asset);
        var layers = serializado.FindProperty("layers");
        int indice = indicePreferido;
        if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(indice).stringValue))
            indice = Enumerable.Range(8, layers.arraySize - 8)
                .First(i => string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue));
        layers.GetArrayElementAtIndex(indice).stringValue = nombre;
        serializado.ApplyModifiedProperties();
        return indice;
    }

    static void PonerLayerRecursivo(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform hijo in go.transform) PonerLayerRecursivo(hijo.gameObject, layer);
    }

    static GameObject Buscar(Scene escena, string nombre)
    {
        return escena.GetRootGameObjects()
            .SelectMany(raiz => raiz.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == nombre)?.gameObject;
    }

    static Transform BuscarHijo(Transform raiz, string nombre)
    {
        return raiz.GetComponentsInChildren<Transform>(true).First(t => t.name == nombre);
    }

    static T ObtenerOAgregar<T>(GameObject go) where T : Component
    {
        var componente = go.GetComponent<T>();
        return componente != null ? componente : go.AddComponent<T>();
    }

    static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace("\\", "/");
        AssetDatabase.CreateFolder(padre, System.IO.Path.GetFileName(ruta));
    }
}
