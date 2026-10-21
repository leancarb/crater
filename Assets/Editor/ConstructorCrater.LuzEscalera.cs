using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class ConstructorCrater
{
    static void IluminarTalladoEscalera(Renderer dibujo, bool sol)
    {
        // Separar la luz proyectada de la emisión visible evita quemar el dibujo.
        Color tinte=sol?new Color(1f,.72f,.40f):new Color(.60f,.76f,1f);
        var material=Emisivo(Opaco(sol?"Escalera_Sol_Luminaria":"Escalera_Luna_Luminaria",
            sol?new Color(.32f,.24f,.13f):new Color(.24f,.32f,.42f),.05f),tinte*.45f);
        dibujo.sharedMaterial=material;
        var previo=dibujo.transform.Find("Luz_Del_Tallado");
        if(previo!=null)Object.DestroyImmediate(previo.gameObject);
        Vector3 normal=-dibujo.transform.forward;
        var luz=Luz(dibujo.transform,"Luz_Del_Tallado",dibujo.transform.position+normal*.16f,
            tinte,20f,4.7f,false);
        luz.type=LightType.Spot;luz.spotAngle=110f;luz.innerSpotAngle=45f;
        luz.transform.LookAt(dibujo.transform.position+normal*.9f+Vector3.down*1.7f+Vector3.forward*.4f);
    }

    [MenuItem("Crater/Actualizar luz de escalera")]
    public static void ActualizarLuzEscalera()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Detener Play antes de actualizar.");
        foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(r=>r.name.StartsWith("Sol_Escalera_")||r.name.StartsWith("Luna_Escalera_")))
            IluminarTalladoEscalera(r,r.name.StartsWith("Sol_"));
        foreach(var luz in Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(l=>l.name=="Luz_Plaza"||(l.name=="Luz_Vela_Entrada (1)"&&Mathf.Abs(l.transform.position.x)<.5f&&l.transform.position.z < -34&&l.transform.position.z > -49)).ToArray())
            Object.DestroyImmediate(luz.gameObject);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
