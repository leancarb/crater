using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class ConstructorCrater
{
    static void ConfigurarCreditosEquipo(EclipseFinalController controlador)
    {
        var serializado=new SerializedObject(controlador);
        var propiedad=serializado.FindProperty("creditos");
        var lineas=EclipseFinalController.CreditosDelEquipo();
        propiedad.arraySize=lineas.Length;
        for(int i=0;i<lineas.Length;i++)propiedad.GetArrayElementAtIndex(i).stringValue=lineas[i];
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Crater/Actualizar créditos del equipo")]
    public static void ActualizarCreditosEquipo()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Detener Play antes de actualizar créditos.");
        var controlador=Object.FindFirstObjectByType<EclipseFinalController>();
        if(controlador==null)throw new System.InvalidOperationException("Abrí la escena de Crater.");
        ConfigurarCreditosEquipo(controlador);
        EditorSceneManager.MarkSceneDirty(controlador.gameObject.scene);
        EditorSceneManager.SaveScene(controlador.gameObject.scene);
    }
}
