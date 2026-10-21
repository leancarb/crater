using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class ConstructorCrater
{
    static Light[] LucesDelBorde(Transform crater)
    {
        var luces=new System.Collections.Generic.List<Light>();
        // Focos integrados en cuatro piedras del borde: suben con ellas al abrirse.
        foreach(int i in new[]{2,7,12,17})
        {
            var borde=crater.Find($"Borde_{i:00}");
            var viejo=borde.Find("Luz_Borde_Apertura");if(viejo!=null)Object.DestroyImmediate(viejo.gameObject);
            var r=borde.GetComponent<Renderer>();
            var luz=Luz(borde,"Luz_Borde_Apertura",r.bounds.center+Vector3.up*(r.bounds.extents.y+.03f),
                new Color(.65f,.76f,1f),80f,18f,false);
            luz.type=LightType.Spot;luz.spotAngle=105f;luz.innerSpotAngle=45f;
            luz.transform.LookAt(CentroPozo+Vector3.up*4f);
            luces.Add(luz);
        }
        return luces.ToArray();
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
