using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD mínimo. Se construye en runtime para no depender de referencias frágiles
/// en la escena: indicaciones, estado de la linterna, mira con barra de carga,
/// velos de fundido, título, pausa y cierre.
/// </summary>
public class InterfazCrater : MonoBehaviour
{
    [SerializeField] LinternaController linterna;
    [SerializeField] Font fuente;

    const float DuracionFundidoPrompt = 0.35f;
    static readonly Color ColorApagado = new Color(0.6f, 0.6f, 0.6f);
    static readonly Color ColorBloqueado = new Color(0.8f, 0.2f, 0.15f);

    Text textoPrompt;
    CanvasGroup grupoPrompt;
    Text textoEstado;
    Text textoFiltros;
    Image mira;
    Image fondoCarga;
    Image barraCarga;
    Image velo;
    Text textoTitulo;
    Text textoSubtitulo;
    GameObject panelPausa;

    Coroutine rutinaPrompt;
    Coroutine rutinaDestello;
    string ultimoEstado;
    string ultimosFiltros;

    void Awake()
    {
        if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Construir();
    }

    void Update() => ActualizarLinterna();

    // ---------------------------------------------------------------- indicaciones

    public void MostrarPrompt(string mensaje)
    {
        if (rutinaPrompt != null) StopCoroutine(rutinaPrompt);
        rutinaPrompt = StartCoroutine(CambiarPrompt(mensaje, -1f));
    }

    public void MostrarPromptTemporal(string mensaje, float segundos = 4f)
    {
        if (rutinaPrompt != null) StopCoroutine(rutinaPrompt);
        rutinaPrompt = StartCoroutine(CambiarPrompt(mensaje, segundos));
    }

    public void OcultarPrompt()
    {
        if (rutinaPrompt != null) StopCoroutine(rutinaPrompt);
        rutinaPrompt = isActiveAndEnabled ? StartCoroutine(FundirPrompt(0f)) : null;
    }

    public string PromptActual => grupoPrompt != null && grupoPrompt.alpha > 0f ? textoPrompt.text : "";

    IEnumerator CambiarPrompt(string mensaje, float segundos)
    {
        if (grupoPrompt.alpha > 0f && textoPrompt.text != mensaje) yield return FundirPrompt(0f);
        textoPrompt.text = mensaje;
        if (string.IsNullOrWhiteSpace(mensaje)) yield break;
        yield return FundirPrompt(1f);
        if (segundos <= 0f) yield break;
        yield return new WaitForSeconds(segundos);
        yield return FundirPrompt(0f);
    }

    IEnumerator FundirPrompt(float objetivo)
    {
        while (!Mathf.Approximately(grupoPrompt.alpha, objetivo))
        {
            grupoPrompt.alpha = Mathf.MoveTowards(grupoPrompt.alpha, objetivo, Time.unscaledDeltaTime / DuracionFundidoPrompt);
            yield return null;
        }
    }

    // ---------------------------------------------------------------- velos

    public void MostrarVelo(Color color, float alfa)
    {
        color.a = Mathf.Clamp01(alfa);
        velo.color = color;
        velo.enabled = color.a > 0.001f;
    }

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

    // ---------------------------------------------------------------- pantallas

    public IEnumerator MostrarTitulo()
    {
        MostrarVelo(Color.black, 1f);
        textoTitulo.text = "CRÁTER";
        textoSubtitulo.text = "";
        yield return FundirTexto(textoTitulo, 0f, 1f, 1.6f);
        yield return new WaitForSeconds(1.4f);
        StartCoroutine(FundirTexto(textoTitulo, 1f, 0f, 1.6f));
        yield return Fundir(Color.black, 1f, 0f, 2.5f);
    }

    public IEnumerator MostrarCierre()
    {
        OcultarPrompt();
        yield return Fundir(Color.black, velo.enabled ? velo.color.a : 0f, 1f, 2.5f);
        textoTitulo.text = "CRÁTER";
        textoSubtitulo.text = "Proyecto académico · FADU · 2026\n\nR · Volver a empezar     Esc · Salir";
        StartCoroutine(FundirTexto(textoSubtitulo, 0f, 1f, 2.5f));
        yield return FundirTexto(textoTitulo, 0f, 1f, 2f);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void MostrarPausa(bool visible)
    {
        if (panelPausa != null) panelPausa.SetActive(visible);
    }

    static IEnumerator FundirTexto(Text texto, float desde, float hasta, float duracion)
    {
        texto.enabled = true;
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            var c = texto.color;
            c.a = Mathf.Lerp(desde, hasta, t / duracion);
            texto.color = c;
            yield return null;
        }
        var final = texto.color;
        final.a = hasta;
        texto.color = final;
        texto.enabled = hasta > 0f;
    }

    // ---------------------------------------------------------------- estado de la linterna

    void ActualizarLinterna()
    {
        if (linterna == null) return;

        bool disponible = linterna.Disponible;
        textoEstado.enabled = disponible;
        textoFiltros.enabled = disponible;
        mira.enabled = disponible;
        if (!disponible)
        {
            fondoCarga.gameObject.SetActive(false);
            return;
        }

        string estado;
        Color color;
        if (!linterna.Encendida)
        {
            estado = "LUZ APAGADA";
            color = ColorApagado;
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

        string filtros = DescribirFiltros();
        if (ultimosFiltros != filtros)
        {
            textoFiltros.text = filtros;
            ultimosFiltros = filtros;
        }

        bool apuntando = linterna.Encendida && linterna.HayObjetivo;
        mira.color = apuntando ? Color.Lerp(color, Color.white, 0.2f) : new Color(1f, 1f, 1f, linterna.Encendida ? 0.45f : 0.2f);
        mira.rectTransform.localScale = Vector3.one * (apuntando ? 1.6f : 1f);

        bool mostrarCarga = apuntando && linterna.CargaObjetivo > 0f && linterna.CargaObjetivo < 1f
                            || (apuntando && !linterna.ObjetivoAceptaFiltro);
        fondoCarga.gameObject.SetActive(mostrarCarga);
        if (!mostrarCarga) return;
        barraCarga.fillAmount = linterna.ObjetivoAceptaFiltro ? linterna.CargaObjetivo : 1f;
        barraCarga.color = linterna.ObjetivoAceptaFiltro ? color : ColorBloqueado;
    }

    string DescribirFiltros()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < linterna.filtros.Count; i++)
        {
            var f = linterna.filtros[i];
            if (!linterna.EstaDesbloqueado(f)) continue;
            bool actual = f == linterna.FiltroActual;
            string hex = ColorUtility.ToHtmlStringRGB(actual ? f.color : Color.Lerp(f.color, Color.gray, 0.55f));
            if (sb.Length > 0) sb.Append("     ");
            sb.Append($"<color=#{hex}>{i + 1} {f.nombreVisible}</color>");
        }
        return sb.ToString();
    }

    // ---------------------------------------------------------------- construcción

    void Construir()
    {
        var canvasGO = new GameObject("HUD_CRATER", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var raiz = canvasGO.transform;

        // indicación inferior
        var promptGO = Crear("Prompt", raiz, new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.2f));
        grupoPrompt = promptGO.AddComponent<CanvasGroup>();
        grupoPrompt.alpha = 0f;
        grupoPrompt.blocksRaycasts = false;
        textoPrompt = CrearTexto(promptGO, 32, TextAnchor.MiddleCenter, Color.white);

        // estado de la linterna (arriba a la derecha)
        textoEstado = CrearTexto(Crear("Estado", raiz, new Vector2(0.6f, 0.9f), new Vector2(0.97f, 0.965f)),
            26, TextAnchor.UpperRight, Color.white);
        textoFiltros = CrearTexto(Crear("Filtros", raiz, new Vector2(0.5f, 0.855f), new Vector2(0.97f, 0.9f)),
            20, TextAnchor.UpperRight, Color.white);

        // mira y carga
        var miraGO = Crear("Mira", raiz, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        ((RectTransform)miraGO.transform).sizeDelta = new Vector2(7f, 7f);
        mira = miraGO.AddComponent<Image>();
        mira.sprite = CrearCirculo();
        mira.raycastTarget = false;

        var fondoGO = Crear("FondoCarga", raiz, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var fondoRT = (RectTransform)fondoGO.transform;
        fondoRT.sizeDelta = new Vector2(120f, 6f);
        fondoRT.anchoredPosition = new Vector2(0f, -28f);
        fondoCarga = fondoGO.AddComponent<Image>();
        fondoCarga.color = new Color(0f, 0f, 0f, 0.55f);
        fondoCarga.raycastTarget = false;
        var barraGO = Crear("Carga", fondoGO.transform, Vector2.zero, Vector2.one);
        barraCarga = barraGO.AddComponent<Image>();
        barraCarga.sprite = CrearBlanco();   // Filled necesita un sprite
        barraCarga.type = Image.Type.Filled;
        barraCarga.fillMethod = Image.FillMethod.Horizontal;
        barraCarga.raycastTarget = false;
        fondoGO.SetActive(false);

        // velo de fundidos
        velo = Crear("Velo", raiz, Vector2.zero, Vector2.one).AddComponent<Image>();
        velo.raycastTarget = false;
        MostrarVelo(Color.black, 0f);

        // título / cierre
        textoTitulo = CrearTexto(Crear("Titulo", raiz, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.62f)),
            78, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0f));
        textoSubtitulo = CrearTexto(Crear("Subtitulo", raiz, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.44f)),
            26, TextAnchor.UpperCenter, new Color(0.85f, 0.85f, 0.85f, 0f));
        textoTitulo.enabled = false;
        textoSubtitulo.enabled = false;

        // pausa
        panelPausa = Crear("Pausa", raiz, Vector2.zero, Vector2.one);
        var fondoPausa = panelPausa.AddComponent<Image>();
        fondoPausa.color = new Color(0f, 0f, 0f, 0.78f);
        CrearTexto(Crear("TituloPausa", panelPausa.transform, new Vector2(0.1f, 0.58f), new Vector2(0.9f, 0.72f)),
            56, TextAnchor.MiddleCenter, Color.white).text = "PAUSA";
        CrearTexto(Crear("Controles", panelPausa.transform, new Vector2(0.2f, 0.18f), new Vector2(0.8f, 0.56f)),
            24, TextAnchor.UpperCenter, new Color(0.85f, 0.85f, 0.85f)).text =
            "WASD · Moverse      Mouse · Mirar\n" +
            "F · Linterna      1 / 2 · Filtros      Q · Luz blanca\n\n" +
            "Esc · Seguir      R · Reiniciar      X · Salir";
        panelPausa.SetActive(false);
    }

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

    Text CrearTexto(GameObject go, int tamanio, TextAnchor alineacion, Color color)
    {
        var texto = go.AddComponent<Text>();
        texto.font = fuente;
        texto.fontSize = tamanio;
        texto.alignment = alineacion;
        texto.color = color;
        texto.raycastTarget = false;
        texto.supportRichText = true;
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.verticalOverflow = VerticalWrapMode.Overflow;
        var sombra = go.AddComponent<Shadow>();
        sombra.effectColor = new Color(0f, 0f, 0f, 0.85f);
        sombra.effectDistance = new Vector2(2f, -2f);
        return texto;
    }

    static Sprite circulo;
    static Sprite blanco;

    static Sprite CrearBlanco()
    {
        if (blanco != null) return blanco;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixeles = new Color[16];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = Color.white;
        tex.SetPixels(pixeles);
        tex.Apply();
        blanco = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        return blanco;
    }

    static Sprite CrearCirculo()
    {
        if (circulo != null) return circulo;
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(n / 2f - d)));
        }
        tex.Apply();
        circulo = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return circulo;
    }
}
