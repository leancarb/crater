using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class ConstructorCrater
{
    // Las barras de una reja comparten material y reciben exactamente la misma
    // disolución. Una malla evita enviar por separado cada barra, sin modificar
    // sus vértices, UV, normales, marco, collider ni punto de impacto del haz.
    static Renderer UnirMallasDeMaterial(Transform padre, Renderer[] barras, string nombre)
    {
        if (barras.Length <= 1) return barras.FirstOrDefault();
        var material = barras[0].sharedMaterial;
        if (barras.Any(r => !(r is MeshRenderer) || r.sharedMaterial != material ||
            r.GetComponent<MeshFilter>() == null || r.GetComponent<MeshFilter>().sharedMesh.subMeshCount != 1))
            throw new InvalidOperationException(nombre + " dejó de tener mallas compatibles para agrupar.");
        var combinaciones = barras.Select(r => new CombineInstance
        {
            mesh = r.GetComponent<MeshFilter>().sharedMesh,
            transform = padre.worldToLocalMatrix * r.transform.localToWorldMatrix
        }).ToArray();
        var malla = new Mesh { name = nombre };
        malla.CombineMeshes(combinaciones, true, true);
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(malla);
        var resultado = go.AddComponent<MeshRenderer>();
        resultado.sharedMaterial = material;
        resultado.shadowCastingMode = barras[0].shadowCastingMode;
        resultado.receiveShadows = barras[0].receiveShadows;
        foreach (var r in barras)
        {
            UnityEngine.Object.DestroyImmediate(r.GetComponent<MeshFilter>());
            UnityEngine.Object.DestroyImmediate(r);
        }
        return resultado;
    }

    static void PrepararOclusion(Scene escena)
    {
        foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            // Nunca hornear mecanismos móviles o translúcidos como paredes.
            flags &= ~StaticEditorFlags.OccluderStatic;
            var c = r.GetComponent<Collider>();
            var s = r.bounds.size;
            bool fijo = (flags & StaticEditorFlags.BatchingStatic) != 0;
            bool opaco = r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m != null && m.renderQueue <= 2500);
            bool mecanismo = r.GetComponentInParent<Compuerta>() != null ||
                r.GetComponentInParent<MateriaHueca>() != null || r.GetComponentInParent<PuenteLuz>() != null;
            if (fijo && opaco && !mecanismo && c != null && !c.isTrigger &&
                Mathf.Max(s.x * s.y, s.x * s.z, s.y * s.z) >= 4f)
                flags |= StaticEditorFlags.OccluderStatic;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
        }
        // Compute necesita la ruta de la escena. Recalcular después de reconstruir
        // mantiene sus datos en sincronía, también al cambiar alas o puertas.
        EditorSceneManager.SaveScene(escena, RutaEscena);
        StaticOcclusionCulling.smallestOccluder = 4f;
        StaticOcclusionCulling.smallestHole = .25f;
        StaticOcclusionCulling.backfaceThreshold = 100f;
        if (!StaticOcclusionCulling.Compute())
            throw new InvalidOperationException("No se pudieron reconstruir los datos de oclusión de CRÁTER.");
    }
}
