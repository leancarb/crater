using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Exporta la escena que está abierta en el editor (por ejemplo el mapa modular final),
/// sin pasar por la validación del vertical slice. Igual que los otros builds,
/// pone el ícono y arma el ZIP con los permisos que necesita macOS.
/// </summary>
public static partial class ConstructorCrater
{
    [MenuItem("Crater/Construir escena abierta/macOS", priority = 30)]
    public static void ConstruirEscenaAbiertaMac() =>
        ConstruirEscenaAbierta(BuildTarget.StandaloneOSX, "Builds/macOS/CRATER.app");

    [MenuItem("Crater/Construir escena abierta/Windows", priority = 31)]
    public static void ConstruirEscenaAbiertaWindows() =>
        ConstruirEscenaAbierta(BuildTarget.StandaloneWindows64, "Builds/Windows/CRATER.exe");

    static void ConstruirEscenaAbierta(BuildTarget destino, string ruta)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, destino))
            throw new BuildFailedException($"Falta el módulo de compilación para {destino}. Instalalo para esta versión de Unity desde Unity Hub.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string escena = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(escena))
            throw new BuildFailedException("Guardá la escena antes de exportarla.");
        if (!EditorUtility.DisplayDialog("CRÁTER", $"Exportar para {(destino == BuildTarget.StandaloneOSX ? "macOS" : "Windows")} la escena:\n\n{escena}", "Exportar", "Cancelar"))
            return;

        ConfigurarIconoAplicacion();
        if (Directory.Exists(ruta)) Directory.Delete(ruta, true);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));

        BuildReport reporte = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { escena },
            locationPathName = ruta,
            target = destino,
            options = BuildOptions.None
        });
        if (reporte.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Falló el build de CRÁTER: " + reporte.summary.result);

        string zip = EmpaquetarDemo(destino, ruta);
        Debug.Log($"[CRÁTER] Build de {escena} listo ({reporte.summary.totalSize / (1024 * 1024)} MB). ZIP: {zip}");
        EditorUtility.RevealInFinder(zip);
    }
}
