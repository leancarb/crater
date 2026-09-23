using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Convierte el kit FBX en prefabs editables listos para integrar al graybox.</summary>
public static class CrearPrefabsArteCrater
{
    const string Modelos = "Assets/Models/CraterKit/";
    const string Prefabs = "Assets/Prefabs/Art/";

    [MenuItem("Crater/Arte/Crear prefabs del kit")]
    public static void CrearDesdeMenu()
    {
        CrearTodos();
        EditorUtility.DisplayDialog("CRÁTER", "Prefabs artísticos creados en Assets/Prefabs/Art.", "Aceptar");
    }

    public static void CrearDesdeLineaDeComandos()
    {
        CrearTodos();
    }

    public static void CrearTodos()
    {
        AsegurarCarpeta("Assets/Prefabs/Art");

        Crear("SM_Anchor_Basalto.fbx", "PF_Anchor_Basalto.prefab", ConfigurarAncla);
        Crear("SM_Reja_Hueco.fbx", "PF_Reja_Hueco.prefab", ConfigurarReja);
        Crear("SM_Puente_Cuerpo.fbx", "PF_Puente_Cuerpo.prefab", ConfigurarPuente);
        Crear("SM_Linterna.fbx", "PF_Linterna_Visual.prefab", null);
        Crear("SM_Arquitectura_Modular.fbx", "PF_Arquitectura_Modular.prefab", null);
        Crear("SM_Motivos_Tallados.fbx", "PF_Motivos_Tallados.prefab", null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CRÁTER] Prefabs del kit artístico creados correctamente.");
    }

    static void Crear(string modelo, string prefab, Action<GameObject> configurar)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Modelos + modelo);
        if (asset == null) throw new InvalidOperationException("No se encontró el modelo " + modelo);

        var raiz = new GameObject(System.IO.Path.GetFileNameWithoutExtension(prefab));
        try
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            visual.name = "Visual";
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            configurar?.Invoke(raiz);
            PrefabUtility.SaveAsPrefabAsset(raiz, Prefabs + prefab);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(raiz);
        }
    }

    static void ConfigurarAncla(GameObject raiz)
    {
        int capa = LayerMask.NameToLayer("Ancla");
        PonerLayerRecursivo(raiz, capa);

        var collider = raiz.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.center = new Vector3(0f, 1.35f, 0f);
        collider.radius = 1.15f;

        var ancla = raiz.AddComponent<Ancla>();
        ancla.canalRequerido = FiltroDefinicion.Canal.Cuerpo;
        ancla.retencion = 2.25f;
        ancla.indicador = raiz.GetComponentsInChildren<Renderer>(true)
            .FirstOrDefault(renderer => renderer.name == "Anchor_Core");
    }

    static void ConfigurarReja(GameObject raiz)
    {
        int capa = LayerMask.NameToLayer("Receptor");
        PonerLayerRecursivo(raiz, capa);

        var solido = raiz.AddComponent<BoxCollider>();
        solido.center = new Vector3(0f, 2.1f, 0f);
        solido.size = new Vector3(5.4f, 4.3f, 1.1f);

        var detector = new GameObject("VolumenDetector");
        detector.layer = capa;
        detector.transform.SetParent(raiz.transform, false);
        var trigger = detector.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 2.1f, 0f);
        trigger.size = new Vector3(5.8f, 4.6f, 1.8f);

        var materia = raiz.AddComponent<MateriaHueca>();
        materia.canalRequerido = FiltroDefinicion.Canal.Hueco;
        materia.retencion = 2f;
        materia.opacidadMinima = 0.12f;
    }

    static void ConfigurarPuente(GameObject raiz)
    {
        var collider = raiz.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0f, 4f);
        collider.size = new Vector3(2.2f, 0.18f, 8f);

        var puente = raiz.AddComponent<PuenteLuz>();
        puente.mallas = raiz.GetComponentsInChildren<MeshRenderer>(true);
        puente.colliders = new Collider[] { collider };
    }

    static void PonerLayerRecursivo(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform hijo in go.transform) PonerLayerRecursivo(hijo.gameObject, layer);
    }

    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace("\\", "/");
        AssetDatabase.CreateFolder(padre, System.IO.Path.GetFileName(ruta));
    }
}
