using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Renderiza vistas fijas del recorrido a Assets/Art/Previews/Unity, con la
/// linterna encendida. Sirve para revisar la iluminación sin entrar en Play.
/// </summary>
public static class VistaPreviaCrater
{
    const string Carpeta = "Assets/Art/Previews/Unity";

    static readonly (string nombre, Vector3 ojo, Vector3 objetivo, bool linterna)[] Vistas =
    {
        ("01_Explanada", new Vector3(0f, 5.6f, -40.2f), new Vector3(0f, 3f, -25f), false),
        ("02_Umbral", new Vector3(0f, 1.62f, -21f), new Vector3(-1.5f, 1.2f, -8f), true),
        ("03_Compuerta", new Vector3(-1f, 1.62f, -9f), new Vector3(0f, 2f, -3f), true),
        ("04_Campo", new Vector3(0f, 1.62f, 4.5f), new Vector3(0f, 0.6f, 12f), true),
        ("04b_Campo_Oculo", new Vector3(0f, 1.62f, 10f), new Vector3(0f, 6f, 16f), false),
        ("05_Campo_Torcer", new Vector3(0f, 1.62f, 22.5f), new Vector3(0f, 4f, 26f), true),
        ("06_Hondonada", new Vector3(0f, 1.62f, 36f), new Vector3(0f, 1.5f, 50f), true),
        ("07_Zigzag", new Vector3(-3f, 1.62f, 47.5f), new Vector3(3f, 1.5f, 54f), true),
        ("08_Repisa", new Vector3(0f, 1.62f, 59f), new Vector3(0f, 1f, 67f), true),
        ("09_Cresta", new Vector3(0f, 1.62f, 77f), new Vector3(0f, 3f, 98f), false),
        ("10_Capilla", new Vector3(300f, 1.62f, -4.5f), new Vector3(300f, 1.5f, 10f), false),
    };

    [MenuItem("Crater/Renderizar vistas previas", priority = 30)]
    public static void Renderizar()
    {
        var escena = EditorSceneManager.OpenScene(ConstructorCrater.RutaEscena, OpenSceneMode.Single);
        Directory.CreateDirectory(Carpeta);

        // el jugador de la escena no debe tapar las vistas
        var jugador = GameObject.FindWithTag("Player");
        if (jugador != null) jugador.SetActive(false);

        var camaraGO = new GameObject("CamaraPreviewTemporal");
        var camara = camaraGO.AddComponent<Camera>();
        camara.fieldOfView = 70f;
        camara.nearClipPlane = 0.03f;
        camara.farClipPlane = 250f;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = RenderSettings.fogColor;
        camara.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        var linterna = new GameObject("LinternaTemporal").AddComponent<Light>();
        linterna.transform.SetParent(camaraGO.transform, false);
        linterna.type = LightType.Spot;
        linterna.range = 15f;
        linterna.spotAngle = 30f;
        linterna.innerSpotAngle = 16f;
        linterna.intensity = 420f;
        linterna.color = new Color(1f, 0.95f, 0.86f);
        linterna.shadows = LightShadows.Soft;

        foreach (var v in Vistas)
        {
            camara.transform.position = v.ojo;
            camara.transform.LookAt(v.objetivo);
            linterna.enabled = v.linterna;
            Guardar(camara, Path.Combine(Carpeta, v.nombre + ".png"));
        }

        Object.DestroyImmediate(camaraGO);
        if (jugador != null) jugador.SetActive(true);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.OpenScene(ConstructorCrater.RutaEscena, OpenSceneMode.Single);   // descarta los cambios temporales
        AssetDatabase.Refresh();
        Debug.Log("[CRÁTER] Vistas previas renderizadas en " + Carpeta);
    }

    static void Guardar(Camera camara, string ruta)
    {
        const int ancho = 1280, alto = 720;
        var rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        camara.targetTexture = rt;
        camara.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        camara.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
