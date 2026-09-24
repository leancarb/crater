using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Genera el proyecto completo a partir de datos: filtros, audio, materiales,
/// ajustes de render, post-procesado, prefabs y la escena del vertical slice.
///
/// Es idempotente: se puede correr las veces que haga falta y siempre deja el
/// proyecto en el mismo estado. Para cambiar el nivel se edita
/// ConstructorCrater.Nivel.cs y se vuelve a correr "Crater > Reconstruir todo".
/// </summary>
public static partial class ConstructorCrater
{
    public const string RutaEscena = "Assets/Scenes/CraterVerticalSlice.unity";
    public const string RutaFiltroCuerpo = "Assets/Data/Filtros/Filtro_Cuerpo.asset";
    public const string RutaFiltroHueco = "Assets/Data/Filtros/Filtro_Hueco.asset";
    public const string RutaPerfil = "Assets/Settings/CraterVolumeProfile.asset";
    public const string CarpetaPrefabs = "Assets/Prefabs/";
    const string CarpetaMateriales = "Assets/Materials/";
    const string CarpetaTexturas = "Assets/Materials/Texturas/";
    const string CarpetaModelos = "Assets/Models/CraterKit/";

    public const string CapaAncla = "Ancla";
    public const string CapaReceptor = "Receptor";
    public const string CapaJugador = "Jugador";

    static readonly int IdBase = Shader.PropertyToID("_BaseColor");
    static readonly int IdEmision = Shader.PropertyToID("_EmissionColor");

    // ================================================================== menú

    [MenuItem("Crater/Reconstruir todo", priority = 0)]
    static void ReconstruirDesdeMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        ReconstruirTodo();
        int errores = ValidarProyectoCrater.Validar(true);
        EditorUtility.DisplayDialog("CRÁTER", errores == 0
            ? "Proyecto reconstruido y validado sin errores."
            : $"Proyecto reconstruido con {errores} problemas. Revisá la Consola.", "Aceptar");
    }

    [MenuItem("Crater/Construir demo Windows", priority = 20)]
    public static void ConstruirWindows()
    {
        ValidarProyectoCrater.ValidarDesdeLineaDeComandos();

        const string carpeta = "Builds/Windows";
        Directory.CreateDirectory(carpeta);
        var opciones = new BuildPlayerOptions
        {
            scenes = new[] { RutaEscena },
            locationPathName = carpeta + "/CRATER.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Falló el build de CRÁTER: " + reporte.summary.result);
        Debug.Log($"[CRÁTER] Build listo: {opciones.locationPathName} ({reporte.summary.totalSize / (1024 * 1024)} MB).");
    }

    /// <summary>Unity.exe -batchmode -quit -executeMethod ConstructorCrater.ReconstruirDesdeLineaDeComandos</summary>
    public static void ReconstruirDesdeLineaDeComandos()
    {
        ReconstruirTodo();
        ValidarProyectoCrater.ValidarDesdeLineaDeComandos();
    }

    // ================================================================== orquestación

    public static void ReconstruirTodo()
    {
        foreach (var carpeta in new[] { "Assets/Materials", "Assets/Materials/Texturas", "Assets/Prefabs", "Assets/Scenes", "Assets/Data", "Assets/Data/Filtros", "Assets/Settings" })
            AsegurarCarpeta(carpeta);

        ConfigurarCapas();

        var kit = new Kit { audio = GeneradorAudioCrater.GenerarTodo() };
        kit.cuerpo = ConfigurarFiltro(RutaFiltroCuerpo, "CUERPO", FiltroDefinicion.Canal.Cuerpo,
            new Color(1f, 0.6f, 0.24f), "Enciende las anclas de basalto. Dos anclas encendidas tienden un puente de luz.",
            kit.audio.equiparCuerpo, kit.audio.zumbidoCuerpo);
        kit.hueco = ConfigurarFiltro(RutaFiltroHueco, "HUECO", FiltroDefinicion.Canal.Hueco,
            new Color(0.32f, 0.45f, 1f), "Disuelve la materia hueca: rejas y tapas se vuelven atravesables.",
            kit.audio.equiparHueco, kit.audio.zumbidoHueco);

        CargarModelos(kit);
        CrearMateriales(kit);
        ConfigurarRender();
        kit.perfil = CrearPerfilVolumen();
        CrearPrefabs(kit);
        ConstruirEscena(kit);
        ConfigurarProyecto();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CRÁTER] Proyecto reconstruido.");
    }

    /// <summary>Todo lo que la construcción necesita a mano.</summary>
    sealed class Kit
    {
        public GeneradorAudioCrater.Clips audio;
        public FiltroDefinicion cuerpo, hueco;
        public VolumeProfile perfil;

        public GameObject modeloAncla, modeloReja, modeloPuente, modeloLinterna, modeloArquitectura, modeloMotivos;

        public Material piso, basalto, basaltoMedio, techo, piedra, metal, metalGastado;
        public Material ambar, motivoLatente, puenteVidrio, puenteBorde, rejaBasalto, rejaSello, cielo, lente;
        public Material adobe, paja, piedraCapilla, tierra;
        public Material cal, madera, cardon, pajaBrava, vela;
        public Material huella, espejo, puertaEclipse, luzEclipse, resplandor, corona, discoSol, discoLuna;

        public GameObject prefabJugador, prefabAncla, prefabPuente, prefabReja, prefabFiltro, prefabLinterna;
    }

    // ================================================================== datos

    static FiltroDefinicion ConfigurarFiltro(string ruta, string nombre, FiltroDefinicion.Canal canal, Color color,
                                             string descripcion, AudioClip alEquipar, AudioClip zumbido)
    {
        var filtro = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(ruta);
        if (filtro == null)
        {
            filtro = ScriptableObject.CreateInstance<FiltroDefinicion>();
            AssetDatabase.CreateAsset(filtro, ruta);
        }
        filtro.nombreVisible = nombre;
        filtro.canal = canal;
        filtro.color = color;
        filtro.descripcion = descripcion;
        filtro.anguloCono = 28f;
        filtro.alcance = 15f;
        filtro.intensidad = 420f;
        filtro.tiempoDeCarga = 0.4f;
        filtro.sonidoAlEquipar = alEquipar;
        filtro.zumbido = zumbido;
        EditorUtility.SetDirty(filtro);
        return filtro;
    }

    static void CargarModelos(Kit kit)
    {
        kit.modeloAncla = CargarModelo("SM_Anchor_Basalto.fbx");
        kit.modeloReja = CargarModelo("SM_Reja_Hueco.fbx");
        kit.modeloPuente = CargarModelo("SM_Puente_Cuerpo.fbx");
        kit.modeloLinterna = CargarModelo("SM_Linterna.fbx");
        kit.modeloArquitectura = CargarModelo("SM_Arquitectura_Modular.fbx");
        kit.modeloMotivos = CargarModelo("SM_Motivos_Tallados.fbx");
    }

    static GameObject CargarModelo(string nombre)
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaModelos + nombre);
        if (modelo == null) throw new InvalidOperationException("No se encontró el modelo " + nombre);
        return modelo;
    }

    // ================================================================== materiales

    static void CrearMateriales(Kit kit)
    {
        kit.piso = Opaco("PisoBasalto", new Color(0.1f, 0.1f, 0.11f), 0.22f);
        kit.basalto = Opaco("BasaltoOscuro", new Color(0.06f, 0.065f, 0.08f), 0.16f);
        kit.basaltoMedio = Opaco("BasaltoMedio", new Color(0.12f, 0.125f, 0.145f), 0.2f);
        kit.techo = Opaco("TechoProfundo", new Color(0.03f, 0.032f, 0.04f), 0.05f);
        kit.piedra = Opaco("PiedraClara", new Color(0.21f, 0.2f, 0.19f), 0.12f);
        kit.metal = Opaco("MetalLinterna", new Color(0.055f, 0.06f, 0.07f), 0.5f, 0.7f);
        kit.metalGastado = Opaco("MetalGastado", new Color(0.17f, 0.14f, 0.11f), 0.35f, 0.45f);

        kit.ambar = Emisivo(Opaco("Ambar", new Color(0.9f, 0.42f, 0.1f), 0.3f), new Color(1f, 0.32f, 0.05f) * 1.6f);
        kit.motivoLatente = Emisivo(Opaco("MotivoLatente", new Color(0.03f, 0.035f, 0.05f), 0.2f), new Color(0.3f, 0.45f, 1f) * 0.22f);
        // el óculo muestra el eclipse congelado: sol negro, corona y cielo de noche
        var texturaEclipse = Textura("OculoEclipse", 512, PixelOculo);
        kit.cielo = Emisivo(Opaco("CieloOculo", Color.white, 0f), Color.white * 1.8f);
        kit.cielo.SetTexture("_BaseMap", texturaEclipse);
        kit.cielo.SetTexture("_EmissionMap", texturaEclipse);
        kit.lente = Emisivo(Opaco("Lente", new Color(1f, 0.9f, 0.7f), 0.8f), Color.white * 2f);

        kit.puenteVidrio = Emisivo(Transparente(Opaco("PuenteVidrio", new Color(1f, 0.5f, 0.15f, 0.45f), 0.6f)), new Color(1f, 0.42f, 0.08f));
        kit.puenteBorde = Emisivo(Transparente(Opaco("PuenteBorde", new Color(1f, 0.62f, 0.25f, 0.95f), 0.4f)), new Color(1f, 0.42f, 0.08f));
        kit.rejaBasalto = Transparente(Opaco("RejaBasalto", new Color(0.085f, 0.09f, 0.11f, 1f), 0.2f));
        kit.rejaSello = Emisivo(Transparente(Opaco("RejaSello", new Color(0.1f, 0.25f, 1f, 0.35f), 0.5f)), new Color(0.08f, 0.22f, 1f) * 0.9f);

        kit.adobe = Opaco("CapillaAdobe", new Color(0.62f, 0.4f, 0.24f), 0.15f);
        kit.paja = Opaco("CapillaTechoPaja", new Color(0.3f, 0.21f, 0.1f), 0.05f);
        kit.piedraCapilla = Opaco("CapillaPiedra", new Color(0.36f, 0.34f, 0.3f), 0.12f);
        kit.tierra = Opaco("CapillaTierra", new Color(0.42f, 0.25f, 0.15f), 0.05f);
        kit.huella = Opaco("CapillaHuella", new Color(0.33f, 0.2f, 0.12f), 0.03f);
        kit.cal = Opaco("CapillaCal", new Color(0.84f, 0.81f, 0.74f), 0.08f);
        kit.madera = Opaco("CapillaMadera", new Color(0.26f, 0.16f, 0.09f), 0.2f);
        kit.cardon = Opaco("Cardon", new Color(0.24f, 0.34f, 0.19f), 0.15f);
        kit.pajaBrava = Opaco("PajaBrava", new Color(0.6f, 0.5f, 0.27f), 0.05f);
        kit.vela = Emisivo(Opaco("Vela", new Color(0.92f, 0.86f, 0.72f), 0.2f), new Color(1f, 0.6f, 0.25f) * 2.5f);

        // el eclipse y la puerta
        kit.espejo = Opaco("EspejoBasalto", new Color(0.02f, 0.022f, 0.028f), 0.95f, 0.35f);
        kit.puertaEclipse = Emisivo(Opaco("PuertaEclipse", new Color(0.05f, 0.06f, 0.08f), 0.3f), new Color(0.75f, 0.85f, 1f) * 1.2f);
        kit.luzEclipse = Emisivo(Opaco("LuzEclipse", new Color(0.8f, 0.85f, 1f), 0f), new Color(0.75f, 0.85f, 1f) * 2.5f);
        kit.resplandor = Aditivo("Resplandor", Textura("Resplandor", 128, PixelResplandor));
        kit.corona = Aditivo("CoronaEclipse", Textura("Corona", 256, PixelCorona));
        kit.discoSol = SinLuz("DiscoSol", new Color(6f, 5.4f, 4.4f));
        kit.discoLuna = SinLuz("DiscoLuna", new Color(0.004f, 0.004f, 0.006f));

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Materials" }))
            Validar(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));
        AssetDatabase.SaveAssets();
    }

    static Material Opaco(string nombre, Color color, float suavidad, float metalico = 0f)
    {
        string ruta = CarpetaMateriales + nombre + ".mat";
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.shader = shader;
        m.SetColor(IdBase, color);
        m.SetColor("_Color", color);
        m.SetFloat("_Smoothness", suavidad);
        m.SetFloat("_Metallic", metalico);

        m.SetFloat("_Surface", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.One);
        m.SetFloat("_DstBlend", (float)BlendMode.Zero);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.Zero);
        m.SetFloat("_ZWrite", 1f);
        m.SetOverrideTag("RenderType", "Opaque");
        m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = -1;
        m.SetShaderPassEnabled("ShadowCaster", true);
        m.SetShaderPassEnabled("DepthOnly", true);

        m.DisableKeyword("_EMISSION");
        m.SetColor(IdEmision, Color.black);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>Sin iluminación: el color es el que se ve (los discos del cielo).</summary>
    static Material SinLuz(string nombre, Color color)
    {
        string ruta = CarpetaMateriales + nombre + ".mat";
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, ruta);
        }
        m.shader = shader;
        m.SetColor(IdBase, color);
        m.SetFloat("_Surface", 0f);
        m.SetOverrideTag("RenderType", "Opaque");
        m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = -1;
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>Sin iluminación, suma luz (destellos, reflejo, corona). El color se cambia en runtime.</summary>
    static Material Aditivo(string nombre, Texture2D textura)
    {
        var m = SinLuz(nombre, Color.white);
        m.SetTexture("_BaseMap", textura);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 2f);   // Additive
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.One);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetShaderPassEnabled("ShadowCaster", false);
        m.SetShaderPassEnabled("DepthOnly", false);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- texturas generadas

    static Color PixelResplandor(float u, float v)
    {
        float r = Mathf.Sqrt(u * u + v * v);
        return new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - r), 2.4f));
    }

    static Color PixelCorona(float u, float v)
    {
        // el sol ocupa r < 0,29 (el quad mide 3,4 diámetros solares)
        float r = Mathf.Sqrt(u * u + v * v);
        float ang = Mathf.Atan2(v, u);
        const float borde = 0.28f;
        float rayos = 0.7f + 0.3f * Mathf.Sin(ang * 5f) * Mathf.Sin(ang * 3f + 1f);
        float a = r < borde ? 1f : Mathf.Exp(-(r - borde) / (0.1f * rayos)) + 0.6f * Mathf.Exp(-(r - borde) / 0.02f);
        a *= Mathf.Clamp01((1f - r) * 4f);
        return new Color(1f, 1f, 1f, Mathf.Clamp01(a));
    }

    static Color PixelOculo(float u, float v)
    {
        float r = Mathf.Sqrt(u * u + v * v);
        float ang = Mathf.Atan2(v, u);
        const float luna = 0.18f;
        var cielo = new Color(0.012f, 0.018f, 0.045f);
        Color c;
        if (r < luna) c = new Color(0.002f, 0.002f, 0.004f);
        else
        {
            float rayos = 0.75f + 0.25f * Mathf.Sin(ang * 5f) * Mathf.Sin(ang * 3f + 1f);
            float corona = Mathf.Exp(-(r - luna) / (0.1f * rayos)) * 0.7f + Mathf.Exp(-(r - luna) / 0.015f) * 0.9f;
            c = cielo + new Color(0.75f, 0.85f, 1f) * corona;
        }
        c *= 1f - Mathf.SmoothStep(0f, 1f, (r - 0.9f) / 0.1f);
        c.a = 1f;
        return c;
    }

    /// <summary>Genera una textura cuadrada y la guarda como PNG. 'pixel' recibe coordenadas de -1 a 1.</summary>
    static Texture2D Textura(string nombre, int tam, Func<float, float, Color> pixel)
    {
        string ruta = CarpetaTexturas + nombre + ".png";
        var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        var px = new Color[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
                px[y * tam + x] = pixel((x + 0.5f) / tam * 2f - 1f, (y + 0.5f) / tam * 2f - 1f);
        tex.SetPixels(px);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importador.wrapMode = TextureWrapMode.Clamp;
        importador.alphaIsTransparency = true;
        importador.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static Material Transparente(Material m)
    {
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetShaderPassEnabled("ShadowCaster", false);
        m.SetShaderPassEnabled("DepthOnly", false);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Emisivo(Material m, Color emision)
    {
        m.SetColor(IdEmision, emision);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>
    /// Deja las keywords coherentes con las propiedades, igual que el Inspector de URP.
    /// Sin esto, la validación de URP puede descartar _EMISSION al reimportar.
    /// </summary>
    static void Validar(Material m)
    {
        BaseShaderGUI.SetMaterialKeywords(m);
        EditorUtility.SetDirty(m);
    }

    // ================================================================== render

    static void ConfigurarRender()
    {
        foreach (var ruta in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ruta);
            if (asset == null) continue;
            asset.supportsHDR = true;
            asset.shadowDistance = 45f;
            if (ruta.Contains("PC")) asset.msaaSampleCount = 4;
            EditorUtility.SetDirty(asset);
        }

        // Forward+ saca el límite de luces por objeto: hay muchas luces chicas (anclas, filtros)
        foreach (var ruta in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
        {
            var datos = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ruta);
            if (datos == null) continue;
            datos.renderingMode = RenderingMode.ForwardPlus;
            EditorUtility.SetDirty(datos);
        }
    }

    static VolumeProfile CrearPerfilVolumen()
    {
        AssetDatabase.DeleteAsset(RutaPerfil);
        var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(perfil, RutaPerfil);

        var tono = Agregar<Tonemapping>(perfil);
        tono.mode.Override(TonemappingMode.ACES);

        var bloom = Agregar<Bloom>(perfil);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.85f);
        bloom.scatter.Override(0.7f);

        var ajustes = Agregar<ColorAdjustments>(perfil);
        ajustes.postExposure.Override(0f);
        ajustes.contrast.Override(12f);
        ajustes.saturation.Override(-6f);

        var vineta = Agregar<Vignette>(perfil);
        vineta.intensity.Override(0.34f);
        vineta.smoothness.Override(0.45f);

        var grano = Agregar<FilmGrain>(perfil);
        grano.type.Override(FilmGrainLookup.Thin1);
        grano.intensity.Override(0.16f);

        EditorUtility.SetDirty(perfil);
        AssetDatabase.SaveAssets();
        return perfil;
    }

    static T Agregar<T>(VolumeProfile perfil) where T : VolumeComponent
    {
        var componente = perfil.Add<T>();
        componente.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(componente, perfil);
        return componente;
    }

    // ================================================================== prefabs

    static void CrearPrefabs(Kit kit)
    {
        kit.prefabJugador = GuardarPrefab(CrearJugador(kit), "Jugador");
        kit.prefabAncla = GuardarPrefab(CrearAncla(kit), "Ancla");
        kit.prefabPuente = GuardarPrefab(CrearPuente(kit), "Puente");
        kit.prefabReja = GuardarPrefab(CrearReja(kit), "Reja");
        kit.prefabFiltro = GuardarPrefab(CrearRecogibleFiltro(kit), "Recogible_Filtro");
        kit.prefabLinterna = GuardarPrefab(CrearRecogibleLinterna(kit), "Recogible_Linterna");
    }

    static GameObject GuardarPrefab(GameObject raiz, string nombre)
    {
        try
        {
            return PrefabUtility.SaveAsPrefabAsset(raiz, CarpetaPrefabs + nombre + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(raiz);
        }
    }

    static GameObject CrearJugador(Kit kit)
    {
        var raiz = new GameObject("Jugador") { tag = "Player" };
        var cc = raiz.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.35f;
        cc.slopeLimit = 50f;
        cc.skinWidth = 0.04f;

        var fuentePasos = raiz.AddComponent<AudioSource>();
        ConfigurarAudio(fuentePasos, null, 0.45f, false, false);
        var fps = raiz.AddComponent<JugadorFPS>();
        fps.fuentePasos = fuentePasos;
        fps.pasos = kit.audio.pasos;
        raiz.AddComponent<RespawnPorCaida>();

        var camaraGO = new GameObject("Camara") { tag = "MainCamera" };
        camaraGO.transform.SetParent(raiz.transform, false);
        camaraGO.transform.localPosition = new Vector3(0f, 1.62f, 0f);
        var camara = camaraGO.AddComponent<Camera>();
        camara.fieldOfView = 70f;
        camara.nearClipPlane = 0.03f;
        camara.farClipPlane = 250f;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = ColorNiebla;
        camaraGO.AddComponent<AudioListener>();
        var datosCamara = camara.GetUniversalAdditionalCameraData();
        datosCamara.renderPostProcessing = true;
        datosCamara.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        datosCamara.antialiasingQuality = AntialiasingQuality.High;
        fps.camara = camaraGO.transform;

        var linternaGO = new GameObject("Linterna");
        linternaGO.transform.SetParent(camaraGO.transform, false);
        var spot = linternaGO.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.shadows = LightShadows.Soft;
        spot.shadowNearPlane = 0.2f;
        spot.enabled = false;
        var fuenteLinterna = linternaGO.AddComponent<AudioSource>();
        ConfigurarAudio(fuenteLinterna, null, 0.5f, true, false);
        var linterna = linternaGO.AddComponent<LinternaController>();
        linterna.spot = spot;
        linterna.capaReceptores = LayerMask.GetMask(CapaAncla, CapaReceptor);
        linterna.capaObstaculos = LayerMask.GetMask("Default");
        linterna.filtros = new System.Collections.Generic.List<FiltroDefinicion> { kit.cuerpo, kit.hueco };
        linterna.requiereRecogerla = true;
        linterna.intensidadBase = 420f;
        linterna.alcanceBase = 15f;
        linterna.sonidoEncender = kit.audio.linternaEncender;
        linterna.sonidoApagar = kit.audio.linternaApagar;
        spot.range = linterna.alcanceBase;
        spot.spotAngle = linterna.anguloBase;
        spot.color = linterna.colorBase;
        spot.intensity = linterna.intensidadBase;

        var enMano = new GameObject("LinternaEnMano");
        enMano.transform.SetParent(camaraGO.transform, false);
        var visual = enMano.AddComponent<LinternaVisual>();
        var modelo = Instanciar(kit.modeloLinterna, enMano.transform, "Modelo");
        modelo.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        modelo.transform.localScale = Vector3.one * 0.085f;
        modelo.transform.localPosition = visual.posicionReposo;
        PintarLinterna(kit, modelo);
        foreach (var r in modelo.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        visual.linterna = linterna;
        visual.modelo = modelo.transform;
        visual.lentes = modelo.GetComponentsInChildren<Renderer>(true).Where(r => r.name.Contains("Lens")).ToArray();

        PonerCapa(raiz, LayerMask.NameToLayer(CapaJugador));
        return raiz;
    }

    static GameObject CrearAncla(Kit kit)
    {
        var raiz = new GameObject("Ancla");
        var visual = Instanciar(kit.modeloAncla, raiz.transform, "Visual");
        visual.transform.localScale = Vector3.one * 0.75f;
        Pintar(visual, r => r.name.Contains("Ring") || r.name.Contains("Core") ? kit.ambar
            : r.name.Contains("Front") ? kit.basaltoMedio : kit.basalto);

        var nucleo = new GameObject("Nucleo");
        nucleo.transform.SetParent(raiz.transform, false);
        nucleo.transform.localPosition = new Vector3(0f, 1.01f, 0.33f);

        var detector = raiz.AddComponent<SphereCollider>();
        detector.isTrigger = true;
        detector.center = new Vector3(0f, 1f, 0.15f);
        detector.radius = 0.8f;
        var cuerpo = raiz.AddComponent<CapsuleCollider>();
        cuerpo.center = new Vector3(0f, 1.05f, -0.1f);
        cuerpo.radius = 0.62f;
        cuerpo.height = 2.1f;

        var brillo = CrearLuzHija(raiz.transform, "Brillo", new Vector3(0f, 1f, 0.9f), new Color(1f, 0.5f, 0.15f), 0f, 5f);
        var tono = raiz.AddComponent<AudioSource>();
        ConfigurarAudio(tono, kit.audio.tonosAncla[0], 0.8f, false, true);

        var ancla = raiz.AddComponent<Ancla>();
        ancla.canalRequerido = FiltroDefinicion.Canal.Cuerpo;
        ancla.retencion = 3f;
        ancla.puntoDeImpacto = nucleo.transform;
        ancla.acentos = visual.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.name.Contains("Ring") || r.name.Contains("Core")).ToArray();
        ancla.brillo = brillo;
        ancla.tono = tono;

        PonerCapa(raiz, LayerMask.NameToLayer(CapaAncla));
        return raiz;
    }

    static GameObject CrearPuente(Kit kit)
    {
        // 2,2 m de ancho por 8 m de largo, creciendo hacia +Z desde el pivote.
        // En la escena se escala para cubrir cada hueco.
        var raiz = new GameObject("Puente");
        var visual = Instanciar(kit.modeloPuente, raiz.transform, "Visual");
        visual.transform.localPosition = new Vector3(0f, -0.08f, 0f);
        visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Pintar(visual, r => r.name.Contains("Panel") ? kit.puenteVidrio : kit.puenteBorde);
        foreach (var r in visual.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;

        var collider = raiz.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, -0.1f, 4f);
        collider.size = new Vector3(2.3f, 0.2f, 8f);

        var puente = raiz.AddComponent<PuenteLuz>();
        puente.mallas = visual.GetComponentsInChildren<Renderer>(true);
        puente.colliders = new Collider[] { collider };
        puente.sonidoAparecer = CrearFuenteHija(raiz.transform, "SonidoAparecer", kit.audio.puenteAparecer, 0.7f);
        puente.sonidoDisolver = CrearFuenteHija(raiz.transform, "SonidoDisolver", kit.audio.puenteDisolver, 0.6f);
        return raiz;
    }

    static GameObject CrearReja(Kit kit)
    {
        // Módulo de 6 m de ancho. Los pasillos anchos usan dos, uno al lado del otro.
        var raiz = new GameObject("Reja");
        var visual = Instanciar(kit.modeloReja, raiz.transform, "Visual");
        visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        visual.transform.localScale = new Vector3(6f / 5.59f, 1f, 1f);
        Pintar(visual, r => r.name.Contains("BlueSeal") ? kit.rejaSello : kit.rejaBasalto);

        var solido = raiz.AddComponent<BoxCollider>();
        solido.center = new Vector3(0f, 2.3f, 0f);
        solido.size = new Vector3(6f, 4.6f, 0.9f);
        var detector = raiz.AddComponent<BoxCollider>();
        detector.isTrigger = true;
        detector.center = new Vector3(0f, 1.5f, 0f);
        detector.size = new Vector3(6f, 3f, 2.4f);

        var centro = new GameObject("Centro");
        centro.transform.SetParent(raiz.transform, false);
        centro.transform.localPosition = new Vector3(0f, 1.8f, 0f);

        var siseo = raiz.AddComponent<AudioSource>();
        ConfigurarAudio(siseo, kit.audio.siseoReja, 0.5f, true, true);

        var materia = raiz.AddComponent<MateriaHueca>();
        materia.canalRequerido = FiltroDefinicion.Canal.Hueco;
        materia.retencion = 3f;
        materia.puntoDeImpacto = centro.transform;
        materia.solidos = new Collider[] { solido };
        materia.renderers = visual.GetComponentsInChildren<Renderer>(true);
        materia.sellos = materia.renderers.Where(r => r.name.Contains("BlueSeal")).ToArray();
        materia.siseo = siseo;

        PonerCapa(raiz, LayerMask.NameToLayer(CapaReceptor));
        return raiz;
    }

    static GameObject CrearRecogibleFiltro(Kit kit)
    {
        var raiz = new GameObject("Recogible_Filtro");
        var zona = raiz.AddComponent<SphereCollider>();
        zona.isTrigger = true;
        zona.center = new Vector3(0f, 1f, 0f);
        zona.radius = 1.1f;

        var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "Pedestal";
        UnityEngine.Object.DestroyImmediate(pedestal.GetComponent<Collider>());
        pedestal.transform.SetParent(raiz.transform, false);
        pedestal.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        pedestal.transform.localScale = new Vector3(0.8f, 0.06f, 0.8f);
        pedestal.GetComponent<Renderer>().sharedMaterial = kit.basaltoMedio;

        // el "filtro" es la lente del kit de la linterna, suelta y flotando
        var visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);
        visual.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        var lente = Instanciar(kit.modeloLinterna, visual.transform, "Lente");
        foreach (var hijo in lente.GetComponentsInChildren<Renderer>(true))
            if (!hijo.name.Contains("Lens") && !hijo.name.Contains("Bevel"))
                UnityEngine.Object.DestroyImmediate(hijo.gameObject);
        const float escala = 0.5f;
        lente.transform.localScale = Vector3.one * escala;
        lente.transform.localPosition = -new Vector3(1.98f, 0.45f, 0f) * escala;
        PintarLinterna(kit, lente);

        var recogible = raiz.AddComponent<Recogible>();
        recogible.visual = visual.transform;
        recogible.tintables = lente.GetComponentsInChildren<Renderer>(true).Where(r => r.name.Contains("Lens")).ToArray();
        recogible.brillo = CrearLuzHija(raiz.transform, "Brillo", new Vector3(0f, 1.1f, 0f), Color.white, 3f, 4.5f);
        recogible.sonido = raiz.AddComponent<AudioSource>();
        ConfigurarAudio(recogible.sonido, kit.audio.recoger, 0.8f, false, false);
        recogible.flotacion = 0.08f;
        recogible.giro = 50f;

        PonerCapa(raiz, LayerMask.NameToLayer("Ignore Raycast"));
        return raiz;
    }

    static GameObject CrearRecogibleLinterna(Kit kit)
    {
        var raiz = new GameObject("Recogible_Linterna");
        var zona = raiz.AddComponent<SphereCollider>();
        zona.isTrigger = true;
        zona.center = new Vector3(0f, 0.6f, 0f);
        zona.radius = 1.2f;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(raiz.transform, false);
        var modelo = Instanciar(kit.modeloLinterna, visual.transform, "Modelo");
        modelo.transform.localScale = Vector3.one * 0.13f;
        modelo.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        modelo.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
        PintarLinterna(kit, modelo);

        var recogible = raiz.AddComponent<Recogible>();
        recogible.visual = visual.transform;
        recogible.brillo = CrearLuzHija(raiz.transform, "Brillo", new Vector3(0f, 0.5f, 0f), new Color(1f, 0.7f, 0.4f), 2.5f, 3.5f);
        recogible.sonido = raiz.AddComponent<AudioSource>();
        ConfigurarAudio(recogible.sonido, kit.audio.recoger, 0.8f, false, false);
        recogible.flotacion = 0f;
        recogible.giro = 0f;

        PonerCapa(raiz, LayerMask.NameToLayer("Ignore Raycast"));
        return raiz;
    }

    static void PintarLinterna(Kit kit, GameObject modelo)
    {
        Pintar(modelo, r => r.name.Contains("Lens") ? kit.lente
            : r.name.Contains("Bevel") || r.name.Contains("Rear") ? kit.metalGastado : kit.metal);
    }

    // ================================================================== proyecto

    static void ConfigurarCapas()
    {
        var gestor = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var capas = gestor.FindProperty("layers");
        void Fijar(int indice, string nombre) => capas.GetArrayElementAtIndex(indice).stringValue = nombre;
        Fijar(6, CapaAncla);
        Fijar(7, CapaReceptor);
        Fijar(8, "");   // Espejo y Atravesable eran de mecánicas descartadas
        Fijar(9, "");
        Fijar(10, CapaJugador);
        gestor.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigurarProyecto()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };
        PlayerSettings.productName = "CRÁTER";
        PlayerSettings.companyName = "FADU";
        PlayerSettings.bundleVersion = "0.3.0";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;
    }

    // ================================================================== utilidades

    static GameObject Instanciar(GameObject modelo, Transform padre, string nombre)
    {
        var instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo, padre);
        // se desempaqueta: el prefab o la escena guardan la geometría, no un vínculo al FBX
        PrefabUtility.UnpackPrefabInstance(instancia, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instancia.name = nombre;
        instancia.transform.localPosition = Vector3.zero;
        instancia.transform.localRotation = Quaternion.identity;
        instancia.transform.localScale = Vector3.one;
        foreach (var c in instancia.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
        return instancia;
    }

    static void Pintar(GameObject raiz, Func<Renderer, Material> material)
    {
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            var m = material(r);
            var materiales = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < materiales.Length; i++) materiales[i] = m;
            r.sharedMaterials = materiales;
            r.shadowCastingMode = m.renderQueue >= (int)RenderQueue.Transparent ? ShadowCastingMode.Off : ShadowCastingMode.On;
        }
    }

    static Light CrearLuzHija(Transform padre, string nombre, Vector3 posicion, Color color, float intensidad, float alcance)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = posicion;
        var luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.intensity = intensidad;
        luz.range = alcance;
        luz.shadows = LightShadows.None;
        return luz;
    }

    static AudioSource CrearFuenteHija(Transform padre, string nombre, AudioClip clip, float volumen)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var fuente = go.AddComponent<AudioSource>();
        ConfigurarAudio(fuente, clip, volumen, false, true);
        return fuente;
    }

    static void ConfigurarAudio(AudioSource fuente, AudioClip clip, float volumen, bool bucle, bool espacial)
    {
        fuente.clip = clip;
        fuente.volume = volumen;
        fuente.loop = bucle;
        fuente.playOnAwake = false;
        fuente.spatialBlend = espacial ? 1f : 0f;
        fuente.rolloffMode = AudioRolloffMode.Linear;
        fuente.minDistance = 2f;
        fuente.maxDistance = 28f;
        fuente.dopplerLevel = 0f;
    }

    static void PonerCapa(GameObject go, int capa)
    {
        if (capa < 0) return;
        go.layer = capa;
        foreach (Transform hijo in go.transform) PonerCapa(hijo.gameObject, capa);
    }

    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = Path.GetDirectoryName(ruta).Replace("\\", "/");
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }
}
