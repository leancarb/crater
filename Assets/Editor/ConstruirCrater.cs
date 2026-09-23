using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ConstruirCrater
{
    [MenuItem("Crater/Construir demo Windows")]
    public static void ConstruirWindows()
    {
        ConfigurarVerticalSlice.Aplicar();
        ValidarProyectoCrater.ValidarDesdeLineaDeComandos();

        const string carpeta = "Builds/Windows";
        Directory.CreateDirectory(carpeta);
        var opciones = new BuildPlayerOptions
        {
            scenes = new[] { ValidarProyectoCrater.Escena },
            locationPathName = carpeta + "/CRATER.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Falló el build de CRÁTER: " + reporte.summary.result);

        Debug.Log($"[CRÁTER] Build listo: {opciones.locationPathName} ({reporte.summary.totalSize} bytes).");
    }
}
