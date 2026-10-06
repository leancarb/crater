using System.IO;
using System.Linq;
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
        ("01b_Pasaje_Lore", new Vector3(1.2f, 1.62f, -30.5f), new Vector3(-1.75f, 2f, -28f), false),
        ("02_Umbral", EspacioCrater.Punto(new Vector3(0f, 1.62f, -21f)), EspacioCrater.Punto(new Vector3(-1.5f, 1.2f, -8f)), true),
        ("02b_Umbral_Murales", EspacioCrater.Punto(new Vector3(1.5f, 1.62f, -19.5f)), EspacioCrater.Punto(new Vector3(-6f, 1.8f, -18f)), false),
        ("03_Compuerta", EspacioCrater.Punto(new Vector3(-1f, 1.62f, -9f)), EspacioCrater.Punto(new Vector3(0f, 2f, -3f)), true),
        ("04_Rotonda", EspacioCrater.Punto(new Vector3(0f, 1.62f, 0f)), EspacioCrater.Punto(new Vector3(0f, 2f, 26f)), false),
        ("04b_Rotonda_Ala_Oeste", EspacioCrater.Punto(new Vector3(4f, 1.62f, 8f)), EspacioCrater.Punto(new Vector3(-15f, 2f, 12f)), false),
        ("05_Oeste_Puente", EspacioCrater.Punto(new Vector3(-26f, 1.62f, 12f)), EspacioCrater.Punto(new Vector3(-35f, 1f, 12f)), true),
        ("05b_Oeste_Puerta_Ancla", EspacioCrater.Punto(new Vector3(-24.5f, 1.62f, 23f)), EspacioCrater.Punto(new Vector3(-34f, 1f, 29f)), true),
        ("05c_Oeste_Torcer", EspacioCrater.Punto(new Vector3(-19f, 1.62f, 24.5f)), EspacioCrater.Punto(new Vector3(-19f, 4.5f, 30.3f)), true),
        ("06_Este_Rejas", EspacioCrater.Punto(new Vector3(26f, 1.62f, 12f)), EspacioCrater.Punto(new Vector3(31f, 1.5f, 12f)), true),
        ("06b_Este_Trampilla", EspacioCrater.Punto(new Vector3(27f, 1.62f, 23.5f)), EspacioCrater.Punto(new Vector3(27f, 0f, 28f)), true),
        ("06c_Este_Escotilla", EspacioCrater.Punto(new Vector3(17.5f, -0.4f, 26.5f)), EspacioCrater.Punto(new Vector3(18.5f, 0.45f, 29.5f)), true),
        ("07_Cruce_Reja", EspacioCrater.Punto(new Vector3(0, 1.62f, 34)), EspacioCrater.Punto(new Vector3(3, 1.2f, 42.5f)), true),
        ("08_Cruce_Isla", EspacioCrater.Punto(new Vector3(0, 1.62f, 52)), EspacioCrater.Punto(new Vector3(0, 1.5f, 59)), true),
        ("09_Cresta", EspacioCrater.Punto(new Vector3(0f, 1.62f, 77f)), EspacioCrater.Punto(new Vector3(0f, 3f, 98f)), false),
        ("10_Capilla", new Vector3(0f, 10.62f, -23f), new Vector3(0f, 10.5f, -10.5f), false),
        ("11_Borde_Del_Pozo", new Vector3(0f, 10.6f, -51f), new Vector3(0f, 0.5f, -37f), false),
    };

    public static void ReconstruirYRenderizarCambios()
    {
        ConstructorCrater.ReconstruirDesdeLineaDeComandos();
        RenderizarCambios();
    }

    /// <summary>Capturas de los cambios del valle y sus grutas. No guarda cambios temporales en la escena.</summary>
    [MenuItem("Crater/Renderizar cambios del valle", priority = 31)]
    public static void RenderizarCambios()
    {
        EditorSceneManager.OpenScene(ConstructorCrater.RutaEscena, OpenSceneMode.Single);
        const string salida = "Docs/Previews";
        Directory.CreateDirectory(salida);
        var jugador = GameObject.FindWithTag("Player");
        jugador.SetActive(false);
        var camaraGO = new GameObject("CamaraCambios");
        camaraGO.tag = "MainCamera";
        var cam = camaraGO.AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.03f;
        cam.farClipPlane = 250f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var cielo = Object.FindFirstObjectByType<CieloEclipse>();
        try
        {
            cielo.Mostrar(true);
            cielo.Progreso = 0f;
            Transform valle = GameObject.Find("06_Capilla").transform;
            Transform spawn = valle.Find("SpawnValle");
            void Captura(string nombre, Vector3 ojo, Vector3 objetivo)
            {
                cam.transform.SetPositionAndRotation(ojo, Quaternion.LookRotation(objetivo - ojo));
                cam.backgroundColor = RenderSettings.fogColor;
                cielo.ActualizarVista();
                Guardar(cam, salida + "/" + nombre + ".png");
            }
            Vector3 capilla = valle.TransformPoint(new Vector3(0f, 5f, 7f));
            Captura("01_Inicio_Capilla", spawn.position + Vector3.up * 1.62f, capilla);
            Vector3 Ojo(float z)
            {
                Vector3 punto = valle.TransformPoint(new Vector3(0f, 30f, z));
                Physics.SyncTransforms();
                if (Physics.Raycast(punto, Vector3.down, out var hit, 50f)) return hit.point + Vector3.up * 1.62f;
                return valle.TransformPoint(new Vector3(0f, 1.62f, z));
            }
            Captura("02_Bajada_Oculta", Ojo(137f), capilla);
            Captura("03_Loma_Reaparece", Ojo(123f), capilla);
            cielo.Progreso = 1f;
            GameObject.Find("Tapa_Pozo").SetActive(false);
            Captura("04_Mirador_Totalidad", Ojo(55.5f), new Vector3(0f, 8f, -43f));
            Captura("04b_Bajada_Tallados", new Vector3(0f, 6.3f, -40f), new Vector3(1.58f, 6.3f, -40.5f));
            cielo.Mostrar(false);
            RenderSettings.ambientSkyColor = new Color(0.23f, 0.24f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.14f, 0.135f, 0.15f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.05f, 0.045f);
            RenderSettings.fogColor = new Color(0.02f, 0.025f, 0.04f);
            RenderSettings.fogDensity = 0.02f;
            Captura("05_Cresta_Desde_Antesala", EspacioCrater.Punto(new Vector3(0f, 1.62f, 61f)), EspacioCrater.Punto(new Vector3(0f, 1.4f, 71f)));
            Transform red = GameObject.Find("Red_O1").transform;
            Captura("06_Gruta_Recuperacion", red.TransformPoint(new Vector3(0f, -1.38f, 1f)), red.TransformPoint(new Vector3(3f, -1.6f, -4f)));
            Captura("07_Rotonda_Plaza", EspacioCrater.Punto(new Vector3(0f, 1.62f, 0f)), EspacioCrater.Punto(new Vector3(0f, 0.5f, 12f)));
        }
        finally
        {
            Object.DestroyImmediate(camaraGO);
            EditorSceneManager.OpenScene(ConstructorCrater.RutaEscena, OpenSceneMode.Single);
        }
        Debug.Log("[CRÁTER] Capturas de cambios en " + salida);
    }

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
        linterna.range = 24f;
        linterna.spotAngle = 36f;
        linterna.innerSpotAngle = 16f;
        linterna.intensity = 2000f;
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
        var presupuesto = Object.FindFirstObjectByType<PresupuestoSombras>(FindObjectsInactive.Include);
        if (presupuesto != null) presupuesto.Actualizar(camara.transform.position);
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
