using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fundidos de pantalla: a negro (al reaparecer después de una caída) y a blanco
/// (el anillo de diamante del final). No muestra textos ni indicadores: en esta
/// versión de demostración no hay HUD.
///
/// CÓMO FUNCIONA
/// En Awake crea un Canvas con dos imágenes que cubren la pantalla:
///  - el velo: un color plano; cambiando su alfa se funde a negro o a blanco.
///  - el anillo: un punto de luz en el centro que crece hasta tapar todo.
/// Los métodos que devuelven IEnumerator son corrutinas: duran varios frames y se
/// usan con 'yield return' (o StartCoroutine). Usan tiempo "unscaled" para seguir
/// funcionando aunque el juego esté en pausa.
/// </summary>
public class Fundidos : MonoBehaviour
{
    Image velo;
    Image anillo;
    Coroutine rutinaDestello;

    void Awake() => Construir();

    // ---------------------------------------------------------------- velo

    /// <summary>Cubre la pantalla con un color. alfa 0 = nada, 1 = pantalla llena.</summary>
    public void MostrarVelo(Color color, float alfa)
    {
        color.a = Mathf.Clamp01(alfa);
        velo.color = color;
        velo.enabled = color.a > 0.001f;
    }

    /// <summary>Anima el velo de 'desde' a 'hasta' en 'duracion' segundos.</summary>
    public IEnumerator Fundir(Color color, float desde, float hasta, float duracion)
    {
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            MostrarVelo(color, Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / duracion)));
            yield return null;
        }
        MostrarVelo(color, hasta);
    }

    /// <summary>Un golpe de color que se desvanece solo (por ejemplo al reaparecer).</summary>
    public void Destello(Color color, float duracion)
    {
        if (rutinaDestello != null) StopCoroutine(rutinaDestello);
        rutinaDestello = StartCoroutine(Fundir(color, 1f, 0f, duracion));
    }

    // ---------------------------------------------------------------- anillo de diamante

    /// <summary>
    /// El final del eclipse: un punto de luz que crece desde el centro hasta tapar
    /// toda la pantalla. Termina con el velo blanco opaco.
    /// </summary>
    public IEnumerator AnilloDeDiamante(float duracion)
    {
        var rt = anillo.rectTransform;
        anillo.enabled = true;
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            float k = t / duracion;
            // empieza como un punto que casi no crece y después explota (k elevado a 2,6)
            float tam = Mathf.Lerp(14f, 6000f, Mathf.Pow(k, 2.6f));
            rt.sizeDelta = new Vector2(tam, tam);
            anillo.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k * 6f));
            MostrarVelo(Color.white, Mathf.Pow(k, 3f));
            yield return null;
        }
        MostrarVelo(Color.white, 1f);
        anillo.enabled = false;
    }

    // ---------------------------------------------------------------- construcción

    void Construir()
    {
        var canvasGO = new GameObject("Fundidos", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;   // dibujado encima de todo
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // velo: estirado a toda la pantalla (anclas de 0 a 1)
        velo = Crear("Velo", canvasGO.transform, Vector2.zero, Vector2.one).AddComponent<Image>();
        velo.raycastTarget = false;
        MostrarVelo(Color.black, 0f);

        // anillo: anclado al centro, su tamaño lo anima AnilloDeDiamante
        anillo = Crear("AnilloDeDiamante", canvasGO.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)).AddComponent<Image>();
        anillo.sprite = CrearResplandor();
        anillo.raycastTarget = false;
        anillo.enabled = false;
    }

    /// <summary>Un objeto de UI que ocupa el rectángulo entre anclaMin y anclaMax (fracciones de la pantalla).</summary>
    static GameObject Crear(string nombre, Transform padre, Vector2 anclaMin, Vector2 anclaMax)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    static Sprite resplandor;

    /// <summary>Textura generada por código: núcleo duro y halo largo, se lee como una estrella.</summary>
    static Sprite CrearResplandor()
    {
        if (resplandor != null) return resplandor;
        const int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        float centro = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float r = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro)) / centro;
            float a = Mathf.Clamp01(Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f) + Mathf.Clamp01(0.35f - r) * 3f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        resplandor = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return resplandor;
    }
}
