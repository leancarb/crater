using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class ConstructorCrater
{
    static Light[] LucesDelBorde(Transform crater)
    {
        // Sin focos exteriores: la apertura se lee desde la luz de los tallados interiores.
        foreach(var t in crater.GetComponentsInChildren<Transform>(true).ToArray())
            if(t!=null && (t.name.StartsWith("Foco_Integrado_Tierra_") || t.name=="Luz_Borde_Apertura"))
                Object.DestroyImmediate(t.gameObject);
        return System.Array.Empty<Light>();
    }

    [MenuItem("Crater/Actualizar iluminación de apertura")]
    public static void ActualizarIluminacionApertura()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Detener Play antes de actualizar.");
        var luces=LucesDelBorde(GameObject.Find("Crater_Valle").transform);
        var cielo=new SerializedObject(Object.FindFirstObjectByType<CieloEclipse>());
        cielo.FindProperty("ambienteCieloTotalidad").colorValue=new Color(.60f,.65f,.78f);
        cielo.FindProperty("ambienteHorizonteTotalidad").colorValue=new Color(.45f,.42f,.42f);
        cielo.FindProperty("ambienteSueloTotalidad").colorValue=new Color(.22f,.20f,.18f);
        cielo.ApplyModifiedPropertiesWithoutUndo();
        var p=Object.FindFirstObjectByType<PrologoCapilla>();
        AsignarLista(p,"lucesApertura",luces);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
