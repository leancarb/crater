using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Sustituye la presentación del graybox sin modificar su geometría de colisión.
/// Es idempotente: regenera una capa visual separada y mantiene intacto el recorrido.
/// </summary>
public static class ReemplazarGrayboxCrater
{
    const string Escena = "Assets/Scenes/CraterVerticalSlice.unity";
    const string RaizArte = "ARTE_FINAL_CRATER";
    const string VisualMecanica = "ArteVisual";
    const string RutaModelos = "Assets/Models/CraterKit/";

    static readonly string[] PrefijosEstructura =
    {
        "Piso_", "Pared_", "Techo", "Rampa_", "Repisa_", "Puerta_", "Oculo_"
    };

    [MenuItem("Crater/Arte/Reemplazar graybox")]
    public static void AplicarDesdeMenu()
    {
        Aplicar();
        EditorUtility.DisplayDialog("CRÁTER", "El graybox fue reemplazado por la capa artística modular.", "Aceptar");
    }

    public static void AplicarDesdeLineaDeComandos()
    {
        Aplicar();
    }

    public static void RenderizarPreviewDesdeLineaDeComandos()
    {
        var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
        string carpeta = Path.GetFullPath("Assets/Art/Previews/Unity");
        Directory.CreateDirectory(carpeta);

        var camaraGO = new GameObject("CamaraPreviewTemporal");
        SceneManager.MoveGameObjectToScene(camaraGO, escena);
        var camara = camaraGO.AddComponent<Camera>();
        camara.fieldOfView = 68f;
        camara.nearClipPlane = 0.05f;
        camara.farClipPlane = 100f;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.004f, 0.005f, 0.009f);

        var luz = camaraGO.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.76f, 0.48f);
        luz.intensity = 120f;
        luz.range = 24f;

        RenderizarVista(camara, new Vector3(0f, 2.1f, -19f), new Vector3(0f, 1.5f, 7f),
            Path.Combine(carpeta, "01_Umbral.png"));
        RenderizarVista(camara, new Vector3(0f, 2.2f, 34f), new Vector3(0f, 1.8f, 54f),
            Path.Combine(carpeta, "02_Hondonada.png"));
        RenderizarVista(camara, new Vector3(0f, 2.1f, 140f), new Vector3(0f, 2.4f, 158f),
            Path.Combine(carpeta, "03_Cresta.png"));

        UnityEngine.Object.DestroyImmediate(camaraGO);
        AssetDatabase.Refresh();
        Debug.Log("[CRÁTER] Previews de la escena renderizados en Assets/Art/Previews/Unity.");
    }

    public static void AuditarArteDesdeLineaDeComandos()
    {
        var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
        foreach (var pickup in Todos<RecogerFiltro>(escena))
        foreach (var renderer in pickup.GetComponentsInChildren<Renderer>(true))
            Debug.Log($"[CRÁTER-ARTE] {pickup.name}/{renderer.gameObject.name} enabled={renderer.enabled} " +
                      $"material={renderer.sharedMaterial?.name} bounds={renderer.bounds.size}");
    }

    static void RenderizarVista(Camera camara, Vector3 posicion, Vector3 objetivo, string ruta)
    {
        camara.transform.position = posicion;
        camara.transform.LookAt(objetivo);
        var texturaRender = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
        var textura = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        camara.targetTexture = texturaRender;
        RenderTexture.active = texturaRender;
        camara.Render();
        textura.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
        textura.Apply();
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        camara.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(texturaRender);
        UnityEngine.Object.DestroyImmediate(textura);
    }

    public static void Aplicar()
    {
        var escena = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
        AplicarSobreEscena(escena);
        EditorSceneManager.SaveScene(escena);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CRÁTER] Graybox reemplazado por arte modular sin alterar colisiones.");
    }

    public static void AplicarSobreEscena(Scene escena)
    {
        AsegurarCarpeta("Assets/Materials/Art");
        var modelos = CargarModelos();
        var materiales = CrearMateriales();

        var anterior = Buscar(escena, RaizArte);
        if (anterior != null) UnityEngine.Object.DestroyImmediate(anterior);

        var raiz = new GameObject(RaizArte);
        SceneManager.MoveGameObjectToScene(raiz, escena);

        VestirEstructura(escena, materiales);
        VestirMecanicas(escena, modelos, materiales);
        CrearArquitectura(raiz.transform, modelos.arquitectura, materiales);
        CrearMotivos(raiz.transform, modelos.motivos, materiales);
        CrearIluminacion(raiz.transform);
        VestirLinterna(escena, modelos.linterna, materiales);

        EditorUtility.SetDirty(raiz);
    }

    static Modelos CargarModelos()
    {
        return new Modelos
        {
            ancla = CargarModelo("SM_Anchor_Basalto.fbx"),
            reja = CargarModelo("SM_Reja_Hueco.fbx"),
            puente = CargarModelo("SM_Puente_Cuerpo.fbx"),
            linterna = CargarModelo("SM_Linterna.fbx"),
            arquitectura = CargarModelo("SM_Arquitectura_Modular.fbx"),
            motivos = CargarModelo("SM_Motivos_Tallados.fbx")
        };
    }

    static GameObject CargarModelo(string nombre)
    {
        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelos + nombre);
        if (modelo == null) throw new InvalidOperationException("No se encontró " + nombre);
        return modelo;
    }

    static Materiales CrearMateriales()
    {
        return new Materiales
        {
            basalto = CrearMaterial("Assets/Materials/Art/BasaltoOscuro.mat", new Color(0.025f, 0.03f, 0.042f), 0.16f),
            basaltoMedio = CrearMaterial("Assets/Materials/Art/BasaltoMedio.mat", new Color(0.065f, 0.072f, 0.09f), 0.2f),
            piso = CrearMaterial("Assets/Materials/Art/PisoBasalto.mat", new Color(0.045f, 0.047f, 0.055f), 0.12f),
            techo = CrearMaterial("Assets/Materials/Art/TechoProfundo.mat", new Color(0.008f, 0.01f, 0.016f), 0.08f),
            piedra = CrearMaterial("Assets/Materials/Art/PiedraClara.mat", new Color(0.22f, 0.21f, 0.2f), 0.12f),
            metal = CrearMaterial("Assets/Materials/Art/MetalLinterna.mat", new Color(0.055f, 0.06f, 0.07f), 0.32f, 0.68f),
            metalGastado = CrearMaterial("Assets/Materials/Art/MetalGastado.mat", new Color(0.17f, 0.14f, 0.11f), 0.2f, 0.42f),
            ambar = CrearMaterialEmisivo("Assets/Materials/Art/CuerpoAmbar.mat", new Color(0.91f, 0.43f, 0.07f), 3.2f, false),
            ambarVidrio = CrearMaterialEmisivo("Assets/Materials/Art/CuerpoVidrio.mat", new Color(0.91f, 0.43f, 0.07f, 0.62f), 1.8f, true),
            hueco = CrearMaterialEmisivo("Assets/Materials/Art/HuecoAzul.mat", new Color(0.12f, 0.25f, 0.95f, 0.42f), 2.2f, true),
            cielo = CrearMaterialEmisivo("Assets/Materials/Art/CieloOculo.mat", new Color(0.025f, 0.12f, 0.42f), 1.7f, false)
        };
    }

    static Material CrearMaterial(string ruta, Color color, float suavidad, float metalico = 0.02f)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, ruta);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", suavidad);
        material.SetFloat("_Metallic", metalico);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material CrearMaterialEmisivo(string ruta, Color color, float intensidad, bool transparente)
    {
        var material = CrearMaterial(ruta, color, 0.28f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * intensidad);
        material.SetFloat("_Surface", transparente ? 1f : 0f);
        material.SetFloat("_ZWrite", transparente ? 0f : 1f);
        material.SetOverrideTag("RenderType", transparente ? "Transparent" : "Opaque");
        material.renderQueue = transparente ? (int)RenderQueue.Transparent : (int)RenderQueue.Geometry;
        if (transparente) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        else material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(material);
        return material;
    }

    static void VestirEstructura(Scene escena, Materiales materiales)
    {
        foreach (var renderer in Todos<Renderer>(escena))
        {
            string nombre = renderer.gameObject.name;
            if (!PrefijosEstructura.Any(prefijo => nombre.StartsWith(prefijo, StringComparison.Ordinal))) continue;

            Material material;
            if (nombre.StartsWith("Oculo", StringComparison.Ordinal))
                material = materiales.cielo;
            else if (nombre.StartsWith("Piso_", StringComparison.Ordinal) ||
                nombre.StartsWith("Rampa_", StringComparison.Ordinal) ||
                nombre.StartsWith("Repisa_", StringComparison.Ordinal))
                material = materiales.piso;
            else if (nombre.StartsWith("Techo", StringComparison.Ordinal))
                material = materiales.techo;
            else
                material = Math.Abs(nombre.GetHashCode()) % 3 == 0 ? materiales.basaltoMedio : materiales.basalto;

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            EditorUtility.SetDirty(renderer);
        }
    }

    static void VestirMecanicas(Scene escena, Modelos modelos, Materiales materiales)
    {
        foreach (var ancla in Todos<Ancla>(escena))
        {
            BorrarVisualAnterior(ancla.transform);
            OcultarRenderers(ancla.gameObject);
            var visual = InstanciarModelo(modelos.ancla, ancla.transform, VisualMecanica);
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * 0.75f;
            PonerLayerRecursivo(visual, ancla.gameObject.layer);
            AplicarMateriales(visual, renderer => renderer.name.Contains("Ring") || renderer.name.Contains("Core")
                ? materiales.ambar
                : renderer.name.Contains("Front") ? materiales.basaltoMedio : materiales.basalto);
            ancla.indicador = visual.GetComponentsInChildren<Renderer>(true)
                .FirstOrDefault(item => item.name == "Anchor_Core");
            EditorUtility.SetDirty(ancla);
        }

        foreach (var reja in Todos<MateriaHueca>(escena))
        {
            BorrarVisualAnterior(reja.transform);
            OcultarRenderers(reja.gameObject);
            var visual = InstanciarModelo(modelos.reja, reja.transform, VisualMecanica);
            visual.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(1f / 5.4f, 1f / 4.5f, 1f / 1.1f);
            PonerLayerRecursivo(visual, reja.gameObject.layer);
            AplicarMateriales(visual, renderer => renderer.name.Contains("BlueSeal")
                ? materiales.hueco
                : renderer.name.Contains("Vertical") || renderer.name.Contains("Horizontal")
                    ? materiales.basaltoMedio : materiales.basalto);
            EditorUtility.SetDirty(reja);
        }

        foreach (var puente in Todos<PuenteLuz>(escena))
        {
            BorrarVisualAnterior(puente.transform);
            OcultarRenderers(puente.gameObject);
            var visual = InstanciarModelo(modelos.puente, puente.transform, VisualMecanica);
            visual.transform.localPosition = new Vector3(0f, 0f, -0.5f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(1f / 2.2f, 1f, 1f / 8f);
            PonerLayerRecursivo(visual, puente.gameObject.layer);
            AplicarMateriales(visual, renderer => renderer.name.Contains("Panel")
                ? materiales.ambarVidrio : materiales.ambar);
            puente.mallas = visual.GetComponentsInChildren<MeshRenderer>(true);
            puente.colliders = puente.GetComponents<Collider>().Where(item => !item.isTrigger).ToArray();
            EditorUtility.SetDirty(puente);
        }

        foreach (var pickup in Todos<RecogerFiltro>(escena))
        {
            BorrarVisualAnterior(pickup.transform);
            OcultarRenderers(pickup.gameObject);
            var visual = InstanciarModelo(modelos.ancla, pickup.transform, VisualMecanica);
            visual.transform.localPosition = new Vector3(0f, -1f, 0f);
            visual.transform.localRotation = Quaternion.Euler(0f, 28f, 0f);
            visual.transform.localScale = Vector3.one * 0.36f;
            PonerLayerRecursivo(visual, pickup.gameObject.layer);
            Material acento = pickup.filtro != null && pickup.filtro.canal == FiltroDefinicion.Canal.Hueco
                ? materiales.hueco : materiales.ambar;
            AplicarMateriales(visual, renderer => renderer.name.Contains("Ring") || renderer.name.Contains("Core")
                ? acento
                : renderer.name.Contains("Front") ? materiales.basaltoMedio : materiales.basalto);
            pickup.objetoVisual = visual;
            EditorUtility.SetDirty(pickup);
        }
    }

    static void VestirLinterna(Scene escena, GameObject modelo, Materiales materiales)
    {
        var linterna = Buscar(escena, "LinternaEnPiso");
        if (linterna == null) return;
        BorrarVisualAnterior(linterna.transform);
        OcultarRenderers(linterna);
        var visual = InstanciarModelo(modelo, linterna.transform, VisualMecanica);
        visual.transform.localPosition = new Vector3(0f, -0.12f, 0f);
        visual.transform.localRotation = Quaternion.Euler(0f, 25f, -8f);
        visual.transform.localScale = Vector3.one * 0.24f;
        AplicarMateriales(visual, renderer => renderer.name.Contains("Lens")
            ? materiales.ambar
            : renderer.name.Contains("Bevel") || renderer.name.Contains("Rear")
                ? materiales.metalGastado : materiales.metal);
        var recoger = linterna.GetComponent<RecogerLinterna>();
        if (recoger != null)
        {
            recoger.objetoVisual = visual;
            EditorUtility.SetDirty(recoger);
        }
    }

    static void CrearArquitectura(Transform raiz, GameObject arquitectura, Materiales materiales)
    {
        var contenedor = new GameObject("ArquitecturaModular");
        contenedor.transform.SetParent(raiz, false);

        var modulos = new List<PoseArte>();
        foreach (float z in new[] { -20f, -14f, -8f, -1f, 5f, 11f, 17f, 23f, 29f, 35f, 41f, 47f, 53f, 59f, 65f })
        {
            modulos.Add(new PoseArte(new Vector3(-5.7f, 0f, z), 90f, 0.82f));
            modulos.Add(new PoseArte(new Vector3(5.7f, 0f, z), -90f, 0.82f));
        }

        modulos.Add(new PoseArte(new Vector3(0f, 0f, 137.6f), 0f, 0.76f));
        foreach (float z in new[] { 143f, 149f, 155f, 161f })
        {
            modulos.Add(new PoseArte(new Vector3(-11.7f, 0f, z), 90f, 0.9f));
            modulos.Add(new PoseArte(new Vector3(11.7f, 0f, z), -90f, 0.9f));
        }
        modulos.Add(new PoseArte(new Vector3(0f, 0f, 164.6f), 180f, 0.9f));
        modulos.Add(new PoseArte(new Vector3(296.2f, 0f, 0f), 90f, 0.68f));
        modulos.Add(new PoseArte(new Vector3(303.8f, 0f, 0f), -90f, 0.68f));

        for (int i = 0; i < modulos.Count; i++)
        {
            var pose = modulos[i];
            var modulo = InstanciarModelo(arquitectura, contenedor.transform, $"Modulo_{i + 1:00}");
            modulo.transform.position = pose.posicion;
            modulo.transform.rotation = Quaternion.Euler(0f, pose.rotacionY, 0f);
            modulo.transform.localScale = Vector3.one * pose.escala;
            PonerLayerRecursivo(modulo, LayerMask.NameToLayer("Default"));
            AplicarMateriales(modulo, renderer => renderer.name.Contains("Wall")
                ? materiales.basalto : materiales.piedra);
        }
    }

    static void CrearMotivos(Transform raiz, GameObject motivos, Materiales materiales)
    {
        var contenedor = new GameObject("MotivosTallados");
        contenedor.transform.SetParent(raiz, false);

        var poses = new[]
        {
            new PoseArte(new Vector3(-5.88f, 0.7f, -12f), 90f, 0.72f),
            new PoseArte(new Vector3(5.88f, 0.7f, 12f), -90f, 0.72f),
            new PoseArte(new Vector3(-5.88f, 0.7f, 50f), 90f, 0.72f),
            new PoseArte(new Vector3(5.88f, 0.7f, 61f), -90f, 0.72f),
            new PoseArte(new Vector3(-11.88f, 0.8f, 154f), 90f, 0.88f),
            new PoseArte(new Vector3(11.88f, 0.8f, 154f), -90f, 0.88f),
            new PoseArte(new Vector3(296.18f, 0.7f, 0f), 90f, 0.62f),
            new PoseArte(new Vector3(303.82f, 0.7f, 0f), -90f, 0.62f)
        };

        for (int i = 0; i < poses.Length; i++)
        {
            var pose = poses[i];
            var motivo = InstanciarModelo(motivos, contenedor.transform, $"Mural_{i + 1:00}");
            motivo.transform.position = pose.posicion;
            motivo.transform.rotation = Quaternion.Euler(0f, pose.rotacionY, 0f);
            motivo.transform.localScale = Vector3.one * pose.escala;
            AplicarMateriales(motivo, renderer => materiales.ambar);
        }
    }

    static void AplicarMateriales(GameObject raiz, Func<Renderer, Material> resolver)
    {
        foreach (var renderer in raiz.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = resolver(renderer);
            renderer.shadowCastingMode = renderer.sharedMaterial.renderQueue >= (int)RenderQueue.Transparent
                ? ShadowCastingMode.Off : ShadowCastingMode.On;
            renderer.receiveShadows = true;
            EditorUtility.SetDirty(renderer);
        }
    }

    static void CrearIluminacion(Transform raiz)
    {
        var contenedor = new GameObject("IluminacionAmbiental");
        contenedor.transform.SetParent(raiz, false);
        CrearLuz(contenedor.transform, "Luz_Umbral", new Vector3(0f, 3.8f, -12f),
            new Color(1f, 0.43f, 0.12f), 340f, 17f);
        CrearLuz(contenedor.transform, "Luz_Campo", new Vector3(0f, 4.2f, 18f),
            new Color(0.95f, 0.34f, 0.08f), 280f, 20f);
        CrearLuz(contenedor.transform, "Luz_Hondonada", new Vector3(0f, 4.2f, 53f),
            new Color(0.12f, 0.24f, 1f), 390f, 21f);
        CrearLuz(contenedor.transform, "Luz_Cresta", new Vector3(0f, 4.5f, 153f),
            new Color(0.08f, 0.18f, 1f), 520f, 24f);
    }

    static void CrearLuz(Transform padre, string nombre, Vector3 posicion, Color color, float intensidad, float alcance)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = posicion;
        var luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.intensity = intensidad;
        luz.range = alcance;
        luz.shadows = LightShadows.Soft;
    }

    static GameObject InstanciarModelo(GameObject modelo, Transform padre, string nombre)
    {
        var instancia = PrefabUtility.InstantiatePrefab(modelo, padre) as GameObject;
        if (instancia == null) throw new InvalidOperationException("No se pudo instanciar " + modelo.name);
        instancia.name = nombre;
        instancia.transform.localPosition = Vector3.zero;
        instancia.transform.localRotation = Quaternion.identity;
        instancia.transform.localScale = Vector3.one;
        return instancia;
    }

    static void BorrarVisualAnterior(Transform raiz)
    {
        var anterior = raiz.Cast<Transform>().FirstOrDefault(item => item.name == VisualMecanica);
        if (anterior != null) UnityEngine.Object.DestroyImmediate(anterior.gameObject);
    }

    static void OcultarRenderers(GameObject raiz)
    {
        foreach (var renderer in raiz.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }
    }

    static void PonerLayerRecursivo(GameObject raiz, int layer)
    {
        if (layer < 0) return;
        raiz.layer = layer;
        foreach (Transform hijo in raiz.transform) PonerLayerRecursivo(hijo.gameObject, layer);
    }

    static IEnumerable<T> Todos<T>(Scene escena) where T : Component
    {
        return escena.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
    }

    static GameObject Buscar(Scene escena, string nombre)
    {
        return escena.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(item => item.name == nombre)?.gameObject;
    }

    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace("\\", "/");
        AssetDatabase.CreateFolder(padre, System.IO.Path.GetFileName(ruta));
    }

    sealed class Modelos
    {
        public GameObject ancla;
        public GameObject reja;
        public GameObject puente;
        public GameObject linterna;
        public GameObject arquitectura;
        public GameObject motivos;
    }

    sealed class Materiales
    {
        public Material basalto;
        public Material basaltoMedio;
        public Material piso;
        public Material techo;
        public Material piedra;
        public Material metal;
        public Material metalGastado;
        public Material ambar;
        public Material ambarVidrio;
        public Material hueco;
        public Material cielo;
    }

    readonly struct PoseArte
    {
        public readonly Vector3 posicion;
        public readonly float rotacionY;
        public readonly float escala;

        public PoseArte(Vector3 posicion, float rotacionY, float escala)
        {
            this.posicion = posicion;
            this.rotacionY = rotacionY;
            this.escala = escala;
        }
    }
}
