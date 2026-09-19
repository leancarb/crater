using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Herramienta de editor para levantar el proyecto CRATER a partir del modelo
/// exportado desde Blender (Assets/Models/Crater/Crater_completo.fbx).
///
/// Uso: menu "Crater" en la barra superior de Unity. Correr los botones
/// en orden, una sola vez cada uno (son seguros de re-ejecutar si hace falta).
///
/// 1. Crear materiales            -> los materiales URP/Lit del GDD (v3.1, seccion 10).
/// 2. Reasignar en el modelo       -> los conecta al FBX combinado.
/// 3. Crear Global Volume         -> Bloom + ACES + Vignette (GDD seccion 14).
/// 4. Armar escena unica          -> UNA escena con las salas conectadas en el
///                                    espacio (Explanada -> Umbral -> Campo Abierto ->
///                                    Hondonada -> Confluencia -> Cresta, mas el tramo
///                                    exterior hasta la Capilla).
/// 5. Crear layers necesarias     -> Jugador / Ancla / Atravesable.
/// 6. Crear prefab del Jugador    -> CharacterController + camara + linterna
///                                    + los dos FiltroDefinicion (Cuerpo, Hueco).
/// 7. Poner el jugador en la escena -> lo instancia en el punto de arranque real
///                                    (centro de la plaza del Descampado).
///
/// IMPORTANTE (encontrado caminando el nivel en vivo, 2026-09-18): el FBX combinado
/// se exporta SIN anclas/recentrado -- las coordenadas de Unity son las reales del
/// archivo de Blender, convertidas eje a eje: Unity(X,Y,Z) = Blender(-X, Z, -Y).
/// El punto de arranque NO es (0,0,0): es el centro real de la plaza de la Explanada,
/// que en Blender esta en (0, 70, ~1) y por lo tanto en Unity es (0, 1, -70). El
/// bug anterior (spawn en el origen) hacia que el jugador cayera al vacio absoluto
/// desde el primer frame -- confirmado caminandolo.
///
/// Lo que esto NO hace (queda para vos, a proposito): ajustar luces puntuales
/// de cada sala, calibrar el rango de la linterna a ojo, y todo el trabajo real
/// de diseño de nivel.
/// </summary>
public static class CrearProyectoCrater
{
    const string CARPETA_MATERIALES = "Assets/Materials/Crater";
    const string CARPETA_SETTINGS = "Assets/Settings/Crater";
    const string CARPETA_MODELOS = "Assets/Models/Crater";
    const string CARPETA_ESCENAS = "Assets/Scenes/Crater";
    const string CARPETA_PREFABS = "Assets/Prefabs";

    const string NOMBRE_MODELO = "Crater_completo";
    const string NOMBRE_ESCENA = "Crater";

    // punto de arranque real: centro de la plaza del Descampado (ver nota arriba)
    static readonly Vector3 SPAWN_JUGADOR = new Vector3(0f, 1.0f, -70f);

    // ---------------------------------------------------------------------------- 1

    [MenuItem("Crater/1. Crear materiales")]
    public static void CrearMateriales()
    {
        AsegurarCarpeta("Assets/Materials");
        AsegurarCarpeta(CARPETA_MATERIALES);

        CrearMaterial("Roca_facetada", Hex("1A1917"), false, 0f, 0.95f);
        CrearMaterial("Hormigon", Hex("9B958A"), false, 0f, 0.85f);
        CrearMaterial("Ceniza_piso", Hex("4A453E"), false, 0f, 0.95f);
        CrearMaterial("Ambar_emisivo", Hex("E8A24A"), true, 4f, 0.6f);
        CrearMaterial("Ambar_grabado", Hex("E8A24A"), true, 2f, 0.6f);
        CrearMaterial("Rojo_piloto", Hex("E51408"), true, 8f, 0.3f);
        CrearMaterial("Indigo_emisivo", Hex("4A5BE8"), true, 3f, 0.5f);
        CrearMaterial("Puente_cristal", Hex("E8A24A"), true, 3f, 0.2f, transparente: true, alfa: 0.5f);
        CrearMaterial("Cielo_estrellado", Hex("1A2333"), true, 2f, 1f);
        CrearMaterial("Estrella", Color.white, true, 20f, 1f);
        CrearMaterial("Cielo_atardecer", Hex("2E4159"), true, 2f, 1f);
        CrearMaterial("Tierra_saltena", Hex("A6493A"), false, 0f, 0.95f);
        CrearMaterial("Cerros_violaceos", Hex("6E5B72"), false, 0f, 1f);
        CrearMaterial("Arbusto", Hex("4D4729"), false, 0f, 1f);
        CrearMaterial("Puente_fijado", Hex("D1BFE0"), true, 2.2f, 0.15f, transparente: true, alfa: 0.6f);
        CrearMaterial("Gallina_ambar", Hex("E8A24A"), false, 0f, 0.18f);
        CrearMaterial("Gallina_indigo", Hex("4A5BE8"), false, 0f, 0.18f);
        CrearMaterial("Cielo_dia", Hex("BFE0F0"), true, 2.5f, 1f);
        CrearMaterial("Adobe_capilla", Hex("C98B5E"), false, 0f, 0.9f);
        CrearMaterial("Techo_paja", Hex("59422A"), false, 0f, 1f);
        CrearMaterial("Piedra_base_capilla", Hex("736B61"), false, 0f, 0.95f);
        CrearMaterial("Metal_campana", Hex("59481F"), false, 0f, 0.3f, metalico: 0.6f);

        AssetDatabase.SaveAssets();
        Debug.Log("[Crater] Materiales creados/actualizados en " + CARPETA_MATERIALES);
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    static void CrearMaterial(string nombre, Color color, bool emisivo, float intensidad, float suavidad,
                               bool transparente = false, float alfa = 1f, float metalico = 0f)
    {
        string ruta = $"{CARPETA_MATERIALES}/{nombre}.mat";
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        else
        {
            mat.shader = shader;
        }

        if (transparente)
        {
            color.a = alfa;
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetShaderPassEnabled("DepthOnly", false);
        }

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", suavidad);
        if (metalico > 0f) mat.SetFloat("_Metallic", metalico);

        if (emisivo)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", color * intensidad);
        }

        EditorUtility.SetDirty(mat);
    }

    // ---------------------------------------------------------------------------- 2

    [MenuItem("Crater/2. Reasignar materiales en el modelo")]
    public static void ReasignarMateriales()
    {
        string[] nombresMateriales =
        {
            "Roca_facetada", "Hormigon", "Ceniza_piso", "Ambar_emisivo", "Ambar_grabado",
            "Rojo_piloto", "Indigo_emisivo", "Puente_cristal", "Cielo_estrellado",
            "Estrella", "Cielo_atardecer", "Tierra_saltena", "Cerros_violaceos", "Arbusto",
            "Puente_fijado", "Gallina_ambar", "Gallina_indigo", "Cielo_dia", "Adobe_capilla",
            "Techo_paja", "Piedra_base_capilla", "Metal_campana"
        };

        string rutaModelo = $"{CARPETA_MODELOS}/{NOMBRE_MODELO}.fbx";
        var importer = AssetImporter.GetAtPath(rutaModelo) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[Crater] No encontre el modelo en {rutaModelo}.");
            return;
        }

        foreach (var nombreMat in nombresMateriales)
        {
            var canonico = AssetDatabase.LoadAssetAtPath<Material>($"{CARPETA_MATERIALES}/{nombreMat}.mat");
            if (canonico == null) continue;
            var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), nombreMat);
            importer.AddRemap(id, canonico);
        }

        importer.SaveAndReimport();
        Debug.Log($"[Crater] Materiales reasignados en {rutaModelo}.");
    }

    // ---------------------------------------------------------------------------- 3

    [MenuItem("Crater/3. Crear Global Volume (post-procesado)")]
    public static void CrearGlobalVolume()
    {
        AsegurarCarpeta("Assets/Settings");
        AsegurarCarpeta(CARPETA_SETTINGS);

        string rutaPerfil = $"{CARPETA_SETTINGS}/PerfilGlobalCrater.asset";
        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(rutaPerfil);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, rutaPerfil);
        }

        if (!perfil.TryGet(out Bloom bloom)) bloom = perfil.Add<Bloom>();
        bloom.threshold.Override(1.1f);
        bloom.intensity.Override(2f);

        if (!perfil.TryGet(out Tonemapping tonemap)) tonemap = perfil.Add<Tonemapping>();
        tonemap.mode.Override(TonemappingMode.ACES);

        if (!perfil.TryGet(out Vignette vignette)) vignette = perfil.Add<Vignette>();
        vignette.intensity.Override(0.35f);

        if (!perfil.TryGet(out ColorAdjustments colorAdj)) colorAdj = perfil.Add<ColorAdjustments>();
        colorAdj.postExposure.Override(0f);

        EditorUtility.SetDirty(perfil);
        AssetDatabase.SaveAssets();
        Debug.Log("[Crater] Perfil de post-procesado listo en " + rutaPerfil);
    }

    // ---------------------------------------------------------------------------- 4

    [MenuItem("Crater/4. Armar escena unica")]
    public static void ArmarEscenaUnica()
    {
        AsegurarCarpeta("Assets/Scenes");
        AsegurarCarpeta(CARPETA_ESCENAS);

        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{CARPETA_SETTINGS}/PerfilGlobalCrater.asset");
        if (perfil == null)
            Debug.LogWarning("[Crater] No encontre el Volume Profile. Corre primero el paso 3.");

        string rutaEscena = $"{CARPETA_ESCENAS}/{NOMBRE_ESCENA}.unity";
        var escena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var prefabModelo = AssetDatabase.LoadAssetAtPath<GameObject>($"{CARPETA_MODELOS}/{NOMBRE_MODELO}.fbx");
        if (prefabModelo != null)
        {
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefabModelo);
            instancia.name = "Crater";
            instancia.transform.position = Vector3.zero;

            foreach (var mf in instancia.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                var mc = mf.GetComponent<MeshCollider>();
                if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;   // AddComponent no copia esto solo -- hay que asignarlo a mano
            }
        }
        else
        {
            Debug.LogWarning($"[Crater] No encontre {CARPETA_MODELOS}/{NOMBRE_MODELO}.fbx. Exporta primero el FBX desde Blender.");
        }

        if (perfil != null)
        {
            var volumenGO = new GameObject("Global Volume");
            var volumen = volumenGO.AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.profile = perfil;
        }

        EditorSceneManager.SaveScene(escena, rutaEscena);
        Debug.Log($"[Crater] Escena unica creada en {rutaEscena}.");
    }

    // ---------------------------------------------------------------------------- 5

    [MenuItem("Crater/5. Crear layers necesarias")]
    public static void CrearLayers()
    {
        CrearLayerSiFalta("Jugador");
        CrearLayerSiFalta("Ancla");
        CrearLayerSiFalta("Atravesable");
        Debug.Log("[Crater] Layers listas. Revisa a mano en Edit > Project Settings > Physics " +
                   "que 'Jugador' y 'Atravesable' NO colisionen entre si (asi funciona el filtro HUECO).");
    }

    static void CrearLayerSiFalta(string nombre)
    {
        var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets == null || tagManagerAssets.Length == 0)
        {
            Debug.LogWarning("[Crater] No pude abrir TagManager.asset.");
            return;
        }
        var tagManager = new SerializedObject(tagManagerAssets[0]);
        var layers = tagManager.FindProperty("layers");

        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).stringValue == nombre) return;

        for (int i = 8; i < layers.arraySize; i++)
        {
            var slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = nombre;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[Crater] Layer '{nombre}' creada en el slot {i}.");
                return;
            }
        }
        Debug.LogWarning($"[Crater] No quedan slots de layer libres para '{nombre}'.");
    }

    // ---------------------------------------------------------------------------- 6

    [MenuItem("Crater/6. Crear prefab del Jugador")]
    public static void CrearPrefabJugador()
    {
        AsegurarCarpeta(CARPETA_PREFABS);

        int capaJugador = LayerMask.NameToLayer("Jugador");
        int capaAncla = LayerMask.NameToLayer("Ancla");
        if (capaJugador < 0 || capaAncla < 0)
        {
            Debug.LogWarning("[Crater] Falta correr el paso 5 (Crear layers) antes de este.");
            return;
        }

        var jugador = new GameObject("Jugador");
        jugador.layer = capaJugador;
        var cc = jugador.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.35f;

        var camaraGO = new GameObject("Camara");
        camaraGO.transform.SetParent(jugador.transform);
        camaraGO.transform.localPosition = new Vector3(0, 1.6f, 0);
        camaraGO.AddComponent<Camera>();
        camaraGO.AddComponent<AudioListener>();

        var jugadorFPS = jugador.AddComponent<JugadorFPS>();
        var soJugador = new SerializedObject(jugadorFPS);
        soJugador.FindProperty("camara").objectReferenceValue = camaraGO.transform;
        soJugador.ApplyModifiedProperties();

        var linternaGO = new GameObject("Linterna");
        linternaGO.transform.SetParent(camaraGO.transform);
        linternaGO.transform.localPosition = Vector3.zero;
        linternaGO.transform.localRotation = Quaternion.identity;

        var luz = linternaGO.AddComponent<Light>();
        luz.type = LightType.Spot;
        luz.color = Color.white;
        luz.range = 14f;
        luz.spotAngle = 28f;
        luz.intensity = 800f;
        luz.shadows = LightShadows.Soft;

        linternaGO.AddComponent<AudioSource>();
        var controlador = linternaGO.AddComponent<LinternaController>();

        var filtroCuerpo = CrearFiltro("Cuerpo", FiltroDefinicion.Canal.Cuerpo, Hex("E8A24A"), 28f, 14f, 3f, 0.35f);
        var filtroHueco = CrearFiltro("Hueco", FiltroDefinicion.Canal.Hueco, Hex("4A5BE8"), 28f, 14f, 3f, 0.35f);

        var soControl = new SerializedObject(controlador);
        soControl.FindProperty("spot").objectReferenceValue = luz;
        soControl.FindProperty("capaReceptores").intValue = 1 << capaAncla;
        var listaFiltros = soControl.FindProperty("filtros");
        listaFiltros.arraySize = 2;
        listaFiltros.GetArrayElementAtIndex(0).objectReferenceValue = filtroCuerpo;
        listaFiltros.GetArrayElementAtIndex(1).objectReferenceValue = filtroHueco;
        soControl.ApplyModifiedProperties();

        string ruta = $"{CARPETA_PREFABS}/Jugador.prefab";
        PrefabUtility.SaveAsPrefabAsset(jugador, ruta);
        Object.DestroyImmediate(jugador);

        AssetDatabase.SaveAssets();
        Debug.Log("[Crater] Prefab del jugador listo en " + ruta + ". " +
                   "'Capa Obstaculos' del LinternaController quedo vacia: revisala a mano.");
    }

    static FiltroDefinicion CrearFiltro(string nombre, FiltroDefinicion.Canal canal, Color color,
                                         float angulo, float alcance, float intensidad, float tiempoDeCarga)
    {
        string ruta = $"{CARPETA_PREFABS}/Filtro_{nombre}.asset";
        var filtro = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(ruta);
        if (filtro == null)
        {
            filtro = ScriptableObject.CreateInstance<FiltroDefinicion>();
            AssetDatabase.CreateAsset(filtro, ruta);
        }

        filtro.nombreVisible = nombre.ToUpperInvariant();
        filtro.canal = canal;
        filtro.color = color;
        filtro.anguloCono = angulo;
        filtro.alcance = alcance;
        filtro.intensidad = intensidad;
        filtro.tiempoDeCarga = tiempoDeCarga;

        EditorUtility.SetDirty(filtro);
        return filtro;
    }

    // ---------------------------------------------------------------------------- 7

    [MenuItem("Crater/7. Poner el jugador en la escena")]
    public static void PonerJugadorEnEscena()
    {
        var prefabJugador = AssetDatabase.LoadAssetAtPath<GameObject>($"{CARPETA_PREFABS}/Jugador.prefab");
        if (prefabJugador == null)
        {
            Debug.LogWarning("[Crater] No encontre el prefab del jugador. Corre el paso 6 primero.");
            return;
        }

        string rutaEscena = $"{CARPETA_ESCENAS}/{NOMBRE_ESCENA}.unity";
        if (!System.IO.File.Exists(rutaEscena))
        {
            Debug.LogWarning($"[Crater] No existe {rutaEscena}. Corre el paso 4 primero.");
            return;
        }

        var escena = EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);

        var existente = GameObject.Find("Jugador");
        GameObject instancia = existente;
        if (instancia == null)
            instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefabJugador, escena);

        instancia.transform.position = SPAWN_JUGADOR;

        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Crater] Jugador puesto en " + SPAWN_JUGADOR.ToString("F1") +
                   " (centro de la plaza del Descampado).");
    }

    // ---------------------------------------------------------------------------- utilidades

    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace("\\", "/");
        string nombre = System.IO.Path.GetFileName(ruta);
        AssetDatabase.CreateFolder(padre, nombre);
    }
}
