using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD minimo del vertical slice. Construye una interfaz responsiva en runtime
/// para no depender de referencias fragiles en la escena.
/// </summary>
public class InterfazCrater : MonoBehaviour
{
    Text textoPrompt;
    Text textoFinal;
    Text textoEstado;
    Image velo;
    Image fondoCarga;
    Image barraCarga;
    [SerializeField] LinternaController linterna;
    Coroutine ocultarPrompt;
    string ultimoEstado;

    public void ConfigurarLinterna(LinternaController nuevaLinterna)
    {
        linterna = nuevaLinterna;
    }

    void Awake()
    {
        ConstruirInterfaz();
    }

    void Update()
    {
        ActualizarEstadoLinterna();
    }

    public void MostrarPrompt(string mensaje)
    {
        if (ocultarPrompt != null) StopCoroutine(ocultarPrompt);
        textoPrompt.text = mensaje;
        textoPrompt.gameObject.SetActive(!string.IsNullOrWhiteSpace(mensaje));
    }

    public void MostrarPromptTemporal(string mensaje, float segundos = 4f)
    {
        MostrarPrompt(mensaje);
        ocultarPrompt = StartCoroutine(OcultarPromptLuego(segundos));
    }

    public void OcultarPrompt()
    {
        if (ocultarPrompt != null) StopCoroutine(ocultarPrompt);
        ocultarPrompt = null;
        textoPrompt.gameObject.SetActive(false);
    }

    public void MostrarBlanco(float alpha)
    {
        velo.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        velo.gameObject.SetActive(alpha > 0.001f);
    }

    public IEnumerator FundirDesdeBlanco(float duracion)
    {
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            MostrarBlanco(1f - t / duracion);
            yield return null;
        }
        MostrarBlanco(0f);
    }

    public IEnumerator MostrarCierre()
    {
        OcultarPrompt();
        velo.gameObject.SetActive(true);
        for (float t = 0f; t < 2f; t += Time.unscaledDeltaTime)
        {
            float a = Mathf.Clamp01(t / 2f);
            velo.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }

        velo.color = Color.black;
        textoFinal.gameObject.SetActive(true);
        textoFinal.text = "CRÁTER\n\nProyecto académico · FADU · 2026";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    IEnumerator OcultarPromptLuego(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        OcultarPrompt();
    }

    void ConstruirInterfaz()
    {
        var canvasGO = new GameObject("HUD_CRATER", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var promptGO = new GameObject("Prompt", typeof(RectTransform), typeof(Text));
        promptGO.transform.SetParent(canvasGO.transform, false);
        textoPrompt = promptGO.GetComponent<Text>();
        textoPrompt.font = fuente;
        textoPrompt.fontSize = 30;
        textoPrompt.alignment = TextAnchor.MiddleCenter;
        textoPrompt.color = Color.white;
        textoPrompt.raycastTarget = false;
        textoPrompt.horizontalOverflow = HorizontalWrapMode.Wrap;
        textoPrompt.verticalOverflow = VerticalWrapMode.Overflow;
        var promptRT = promptGO.GetComponent<RectTransform>();
        promptRT.anchorMin = new Vector2(0.2f, 0.06f);
        promptRT.anchorMax = new Vector2(0.8f, 0.18f);
        promptRT.offsetMin = Vector2.zero;
        promptRT.offsetMax = Vector2.zero;

        var sombra = promptGO.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sombra.effectDistance = new Vector2(2f, -2f);

        var estadoGO = new GameObject("EstadoLinterna", typeof(RectTransform), typeof(Text));
        estadoGO.transform.SetParent(canvasGO.transform, false);
        textoEstado = estadoGO.GetComponent<Text>();
        textoEstado.font = fuente;
        textoEstado.fontSize = 22;
        textoEstado.alignment = TextAnchor.UpperRight;
        textoEstado.raycastTarget = false;
        var estadoRT = estadoGO.GetComponent<RectTransform>();
        estadoRT.anchorMin = new Vector2(0.72f, 0.9f);
        estadoRT.anchorMax = new Vector2(0.97f, 0.97f);
        estadoRT.offsetMin = Vector2.zero;
        estadoRT.offsetMax = Vector2.zero;
        var sombraEstado = estadoGO.AddComponent<Shadow>();
        sombraEstado.effectColor = new Color(0f, 0f, 0f, 0.9f);
        sombraEstado.effectDistance = new Vector2(2f, -2f);
        estadoGO.SetActive(false);

        var fondoCargaGO = new GameObject("FondoCarga", typeof(RectTransform), typeof(Image));
        fondoCargaGO.transform.SetParent(canvasGO.transform, false);
        fondoCarga = fondoCargaGO.GetComponent<Image>();
        fondoCarga.color = new Color(0f, 0f, 0f, 0.65f);
        fondoCarga.raycastTarget = false;
        var fondoCargaRT = fondoCargaGO.GetComponent<RectTransform>();
        fondoCargaRT.anchorMin = new Vector2(0.4f, 0.205f);
        fondoCargaRT.anchorMax = new Vector2(0.6f, 0.22f);
        fondoCargaRT.offsetMin = Vector2.zero;
        fondoCargaRT.offsetMax = Vector2.zero;

        var barraGO = new GameObject("Carga", typeof(RectTransform), typeof(Image));
        barraGO.transform.SetParent(fondoCargaGO.transform, false);
        barraCarga = barraGO.GetComponent<Image>();
        barraCarga.type = Image.Type.Filled;
        barraCarga.fillMethod = Image.FillMethod.Horizontal;
        barraCarga.fillOrigin = 0;
        barraCarga.raycastTarget = false;
        var barraRT = barraGO.GetComponent<RectTransform>();
        barraRT.anchorMin = new Vector2(0.02f, 0.2f);
        barraRT.anchorMax = new Vector2(0.98f, 0.8f);
        barraRT.offsetMin = Vector2.zero;
        barraRT.offsetMax = Vector2.zero;
        fondoCargaGO.SetActive(false);

        var veloGO = new GameObject("Velo", typeof(RectTransform), typeof(Image));
        veloGO.transform.SetParent(canvasGO.transform, false);
        velo = veloGO.GetComponent<Image>();
        velo.raycastTarget = false;
        var veloRT = veloGO.GetComponent<RectTransform>();
        veloRT.anchorMin = Vector2.zero;
        veloRT.anchorMax = Vector2.one;
        veloRT.offsetMin = Vector2.zero;
        veloRT.offsetMax = Vector2.zero;
        MostrarBlanco(0f);

        var finalGO = new GameObject("TituloFinal", typeof(RectTransform), typeof(Text));
        finalGO.transform.SetParent(canvasGO.transform, false);
        textoFinal = finalGO.GetComponent<Text>();
        textoFinal.font = fuente;
        textoFinal.fontSize = 52;
        textoFinal.alignment = TextAnchor.MiddleCenter;
        textoFinal.color = Color.white;
        textoFinal.raycastTarget = false;
        var finalRT = finalGO.GetComponent<RectTransform>();
        finalRT.anchorMin = new Vector2(0.1f, 0.1f);
        finalRT.anchorMax = new Vector2(0.9f, 0.9f);
        finalRT.offsetMin = Vector2.zero;
        finalRT.offsetMax = Vector2.zero;
        finalGO.SetActive(false);
    }

    void ActualizarEstadoLinterna()
    {
        if (linterna == null || textoEstado == null || fondoCarga == null || barraCarga == null) return;

        bool mostrarEstado = linterna.Disponible;
        textoEstado.gameObject.SetActive(mostrarEstado);
        if (!mostrarEstado)
        {
            fondoCarga.gameObject.SetActive(false);
            return;
        }

        string estado;
        Color color;
        if (!linterna.Encendida)
        {
            estado = "LUZ APAGADA";
            color = new Color(0.65f, 0.65f, 0.65f);
        }
        else if (linterna.FiltroActual == null)
        {
            estado = "LUZ BLANCA";
            color = linterna.colorBase;
        }
        else
        {
            estado = linterna.FiltroActual.nombreVisible;
            color = linterna.FiltroActual.color;
        }

        if (ultimoEstado != estado)
        {
            textoEstado.text = estado;
            ultimoEstado = estado;
        }
        textoEstado.color = color;

        bool mostrarCarga = linterna.Encendida && linterna.HayObjetivo && linterna.FiltroActual != null;
        fondoCarga.gameObject.SetActive(mostrarCarga);
        if (!mostrarCarga) return;

        barraCarga.fillAmount = linterna.CargaObjetivo;
        barraCarga.color = linterna.ObjetivoAceptaFiltro
            ? linterna.FiltroActual.color
            : new Color(0.8f, 0.18f, 0.14f);
    }
}
