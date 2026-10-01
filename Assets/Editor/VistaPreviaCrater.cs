using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Renderiza vistas fijas del recorrido a Assets/Art/Previews/Unity, con la
/// linterna encendida. Sirve para revisar la iluminación sin entrar en Play.
///
/// CÓMO FUNCIONA
/// Para cada vista pone una cámara temporal en 'ojo' mirando a 'objetivo', la renderiza
/// a una textura y la guarda como PNG. Al terminar borra la cámara y vuelve a abrir la
/// escena, así se descartan los cambios temporales (jugador escondido, cámara extra).
/// </summary>
public static class VistaPreviaCrater
{
    const string Carpeta = "Assets/Art/Previews/Unity";

    static readonly (string nombre, Vector3 ojo, Vector3 objetivo, bool linterna)[] Vistas =
    {
        ("01_Explanada", new Vector3(-4f, 1.62f, -44f), new Vector3(0f, 1f, -25f), false),
        ("02_Umbral", new Vector3(0f, 1.62f, -21f), new Vector3(-1.5f, 1.2f, -8f), true),
        ("03_Compuerta", new Vector3(-1f, 1.62f, -9f), new Vector3(0f, 2f, -3f), true),
        ("04_Rotonda", new Vector3(0f, 1.62f, 0f), new Vector3(0f, 2f, 26f), false),
        ("05_Oeste_Puente", new Vector3(-23f, 1.62f, 1f), new Vector3(-32f, 1f, 1f), true),
        ("05b_Oeste_Dos_Anclas", new Vector3(-21.5f, 1.62f, 12f), new Vector3(-31f, 1f, 18f), true),
        ("05c_Oeste_Torcer", new Vector3(-16f, 1.62f, 13.5f), new Vector3(-16f, 4.5f, 19.3f), true),
        ("06_Este_Rejas", new Vector3(23f, 1.62f, 1f), new Vector3(28f, 1.5f, 1f), true),
        ("06b_Este_Trampilla", new Vector3(24f, 1.62f, 12.5f), new Vector3(24f, 0f, 17f), true),
        ("06c_Este_Escotilla", new Vector3(14.5f, -0.4f, 15.5f), new Vector3(15.5f, 0.45f, 18.5f), true),
        ("07_Cruce_Reja", new Vector3(0f, 1.62f, 30f), new Vector3(2f, 1f, 38f), true),
        ("08_Cruce_Borde", new Vector3(0f, 1.62f, 49f), new Vector3(0f, 1.5f, 52f), true),
        ("09_Cresta", new Vector3(0f, 1.62f, 77f), new Vector3(0f, 3f, 98f), false),
        ("10_Capilla", new Vector3(0f, 10.62f, -76f), new Vector3(0f, 10.5f, -61.5f), false),
        ("11_Borde_Del_Pozo", new Vector3(0f, 10.6f, -51f), new Vector3(0f, 0.5f, -37f), false),
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
