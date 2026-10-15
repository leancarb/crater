using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using Unity.Profiling;
using UnityEngine;

/// <summary>Sonda de vistas fijas en Play. No se incluye en el ejecutable.</summary>
public static class MedidorRendimientoCrater
{
    static readonly string[] nombres = { "PlayerLoop", "Main Thread", "EditorLoop", "CPU Render Thread Frame Time", "GPU Frame Time", "Draw Calls Count", "SetPass Calls Count", "Triangles Count", "GC Allocated In Frame" };
    static ProfilerRecorder[] registros;
    static readonly List<double>[] muestras = new List<double>[9];
    static string destino;
    static int anterior, calentamiento, restantes;
    public static bool Midiendo => registros != null;

    [MenuItem("Crater/Medir rendimiento de la vista actual (Play)")]
    static void DesdeMenu() => Medir("Logs/rendimiento_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt", 1200);
    [MenuItem("Crater/Medir rendimiento de la vista actual (Play)", true)]
    static bool PuedeMedir() => EditorApplication.isPlaying && !Midiendo && !PausaCrater.EnPausa;

    public static void Medir(string archivo, int fotogramas = 360)
    {
        if (!EditorApplication.isPlaying || Midiendo) throw new InvalidOperationException("Entrar en Play y terminar la medición anterior.");
        destino = archivo; calentamiento = 120; restantes = fotogramas; anterior = Time.frameCount;
        registros = new ProfilerRecorder[nombres.Length];
        var disponibles = new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();
        Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(disponibles);
        for (int i = 0; i < registros.Length; i++)
        {
            muestras[i] = new List<double>(fotogramas);
            // La categoría del marcador PlayerLoop es propia; resolver por nombre evita asumir Scripts.
            foreach (var h in disponibles)
            {
                var d = Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription(h);
                if (d.Name == nombres[i]) { registros[i] = ProfilerRecorder.StartNew(d.Category, d.Name, 1); break; }
            }
        }
        EditorApplication.update += Muestrear;
        EditorApplication.playModeStateChanged += AlCambiarPlay;
    }

    static void Muestrear()
    {
        if (Time.frameCount == anterior) return;
        anterior = Time.frameCount;
        if (calentamiento-- > 0) return;
        for (int i = 0; i < registros.Length; i++) muestras[i].Add(registros[i].Valid ? registros[i].LastValue : -1);
        if (--restantes == 0) Terminar(true);
    }
    static void AlCambiarPlay(PlayModeStateChange e) { if (e == PlayModeStateChange.ExitingPlayMode) Terminar(false); }
    static void Terminar(bool guardar)
    {
        EditorApplication.update -= Muestrear;
        EditorApplication.playModeStateChanged -= AlCambiarPlay;
        if (guardar)
        {
            var s = new System.Text.StringBuilder();
            s.AppendLine($"Editor Play, vista fija; {Screen.width}x{Screen.height}; {SystemInfo.graphicsDeviceName}; {SystemInfo.processorType}; vSync={QualitySettings.vSyncCount}; objetivo={Application.targetFrameRate}");
            if (Camera.main != null) s.AppendLine($"Cámara: {Camera.main.transform.position}; giro: {Camera.main.transform.eulerAngles}; oclusión: {Camera.main.useOcclusionCulling}");
            s.AppendLine("Los contadores incluyen trabajo del Editor; cero o -1 no prueban coste nulo. No equivalen a FPS del EXE.");
            for (int i = 0; i < muestras.Length; i++)
            {
                var a = muestras[i]; a.Sort(); double factor = i < 5 ? .000001 : 1;
                double suma = 0; foreach (var v in a) suma += v;
                s.AppendLine($"{nombres[i]}: mediana={a[a.Count/2]*factor:F4}; p90={a[(int)(a.Count*.9)]*factor:F4}; media={suma/a.Count*factor:F4}; unidad={(i<5 ? "ms" : i==8 ? "bytes" : "cantidad")}; n={a.Count}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(destino)); File.WriteAllText(destino, s.ToString());
        }
        foreach (var r in registros) r.Dispose(); registros = null;
    }

    public static void Capturar(string nombre)
    {
        var camara = Camera.main;
        if (camara == null || Midiendo) throw new InvalidOperationException("Capturar en Play, fuera de una medición.");
        var destinoAnterior = camara.targetTexture;
        var activaAnterior = RenderTexture.active;
        var textura = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var rt = new RenderTexture(1280, 720, 24);
        try
        {
            camara.targetTexture = rt; camara.Render(); RenderTexture.active = rt;
            textura.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); textura.Apply();
            Directory.CreateDirectory("Docs/Previews/Rendimiento");
            File.WriteAllBytes("Docs/Previews/Rendimiento/" + nombre + ".png", textura.EncodeToPNG());
        }
        finally
        {
            camara.targetTexture = destinoAnterior; RenderTexture.active = activaAnterior;
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(textura);
        }
    }
}
