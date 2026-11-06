using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public static partial class ConstructorCrater
{
    [MenuItem("Crater/Empaquetar Windows en un solo EXE", priority = 22)]
    public static void EmpaquetarWindowsPortable()
    {
        string zip = EmpaquetarDemo(BuildTarget.StandaloneWindows64, "Builds/Windows/CRATER.exe");
        string exe = CrearWindowsPortable(zip);
        UnityEngine.Debug.Log("[CRÁTER] EXE único listo para compartir: " + exe);
    }

    static string CrearWindowsPortable(string zip)
    {
        if (Application.platform != RuntimePlatform.WindowsEditor)
            throw new PlatformNotSupportedException("El empaquetado de EXE portable se ejecuta desde Windows.");
        string windows = Environment.GetEnvironmentVariable("WINDIR");
        string framework = Path.Combine(windows, "Microsoft.NET/Framework64/v4.0.30319");
        string compilador = Path.Combine(framework, "csc.exe");
        string fuente = Path.GetFullPath("Tools/WindowsPortable/LanzadorCrater.cs");
        if (!File.Exists(compilador) || !File.Exists(fuente)) throw new FileNotFoundException("Falta el compilador de Windows o el código del lanzador portable.");
        string destino = Path.GetFullPath("Builds/CRATER_Windows.exe");
        string temporal = Path.ChangeExtension(destino, ".packing.exe");
        string icono = CrearIconoWindows();
        string Q(string p) => "\"" + p + "\"";
        var inicio = new ProcessStartInfo(compilador) {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            Arguments = "/nologo /target:winexe /platform:x64 /optimize+ /out:" + Q(temporal) +
                " /win32icon:" + Q(icono) +
                " /reference:" + Q(Path.Combine(framework, "System.IO.Compression.dll")) +
                " /reference:" + Q(Path.Combine(framework, "System.Windows.Forms.dll")) +
                " /reference:" + Q(Path.Combine(framework, "System.Drawing.dll")) +
                " /resource:" + Q(Path.GetFullPath(zip)) + ",CRATER.Payload.zip " + Q(fuente)
        };
        try
        {
            using (var proceso = Process.Start(inicio))
            {
                var salida = proceso.StandardOutput.ReadToEndAsync(); var error = proceso.StandardError.ReadToEndAsync();
                proceso.WaitForExit();
                if (proceso.ExitCode != 0) throw new InvalidOperationException("No se pudo crear el EXE portable:\n" + salida.Result + error.Result);
            }
            if (!File.Exists(temporal) || new FileInfo(temporal).Length < new FileInfo(zip).Length)
                throw new IOException("El EXE portable no contiene el paquete completo.");
            if (File.Exists(destino)) File.Replace(temporal, destino, null); else File.Move(temporal, destino);
            return destino;
        }
        finally { if (File.Exists(temporal)) File.Delete(temporal); }
    }
}
