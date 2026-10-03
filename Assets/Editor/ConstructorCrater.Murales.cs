using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Murales que enseñan sin texto. Antes de juntar la linterna, el jugador pasa por:
///
///  - El pasaje (pared oeste): cuatro losas que cuentan el lore, de izquierda a derecha
///    caminando hacia el Umbral: el sol y la luna separados; el eclipse sobre la capilla;
///    el cráter que se abre (pozo, escalera, puerta); alguien con la luz que los revela.
///  - El Umbral: una losa por acción, con la tecla tallada como tecla y el ícono de lo que
///    hace. Oeste: mover (WASD + mouse), linterna (F), filtro sol (1: el puente aparece).
///    Este: correr (Shift + W), luz blanca (Q: el ancla), filtro luna (2: la reja se abre).
///    La linterna está más adelante, así se pasa por todas antes de agarrarla.
///
/// Todo es pintura mate sobre piedra clara: no brilla, porque lo que brilla se usa.
///
/// CÓMO FUNCIONA
/// Cada losa es un bloque de piedra y, delante, una malla plana por color. Los dibujos se
/// arman en metros sobre la losa (x a la derecha, y arriba, mirando al jugador) con pocas
/// primitivas: rectángulos, líneas gruesas, discos y las mismas figuras de los tallados.
/// Las letras de las teclas son de una fuente de píxeles de 5 × 7.
/// </summary>
public static partial class ConstructorCrater
{
    /// <summary>Lo que se va pintando en una losa: triángulos por material, en el orden de las capas.</summary>
    sealed class Lienzo
    {
        public readonly List<(Material material, List<Vector3> v)> capas = new List<(Material, List<Vector3>)>();

        public List<Vector3> En(Material m)
        {
            foreach (var c in capas) if (c.material == m) return c.v;
            var v = new List<Vector3>();
            capas.Add((m, v));
            return v;
        }
    }

    static void ConstruirMurales(Kit k, Transform arte)
    {
        var murales = Grupo(arte, "Murales");

        // --- el pasaje: el lore, de sur a norte (de izquierda a derecha mirando la pared)
        var lore = Grupo(murales, "Lore_Pasaje");
        float[] zs = { -32.5f, -30f, -27.5f, -25f };
        System.Action<Kit, Lienzo>[] escenas = { LoreSolYLuna, LoreEclipse, LoreCrater, LoreLuz };
        for (int i = 0; i < zs.Length; i++)
            Losa(k, lore, $"Lore_{i + 1}", new Vector3(-1.75f, 2.1f, zs[i]), Vector3.right, 2f, 1.4f, escenas[i]);
        Luz(lore, "Luz_Pasaje", new Vector3(0.6f, 3.4f, -28.75f), LuzCalida, 30f, 7f, false);

        // --- el Umbral: los controles, una losa por acción
        var controles = Grupo(murales, "Controles_Umbral");
        void Control(string nombre, float lado, float z, System.Action<Kit, Lienzo> dibujo) =>
            Losa(k, controles, nombre, new Vector3(lado * 6f, 1.8f, z), lado < 0f ? Vector3.right : Vector3.left, 2.4f, 1.5f, dibujo);
        Control("Control_Mover", -1f, -21f, PanelMover);
        Control("Control_Correr", 1f, -21f, PanelCorrer);
        Control("Control_Linterna", -1f, -18f, PanelLinterna);
        Control("Control_LuzBlanca", 1f, -18f, PanelLuzBlanca);
        Control("Control_FiltroSol", -1f, -15f, PanelFiltroSol);
        Control("Control_FiltroLuna", 1f, -15f, PanelFiltroLuna);
    }

    /// <summary>Una losa de piedra clara sobre la pared, con un marco y el dibujo pintado delante.</summary>
    static void Losa(Kit k, Transform padre, string nombre, Vector3 enLaPared, Vector3 afuera, float ancho, float alto,
                     System.Action<Kit, Lienzo> dibujo)
    {
        var losa = new GameObject(nombre).transform;
        losa.SetParent(padre, false);
        // el origen queda en la cara de la losa; +Z local entra en la pared
        losa.SetPositionAndRotation(enLaPared + afuera * 0.07f, Quaternion.LookRotation(-afuera));
        Bloque(losa, "Piedra", losa.TransformPoint(new Vector3(0f, 0f, 0.05f)), new Vector3(ancho, alto, 0.12f), k.piedra);

        // el marco va al final: así el orden de las capas es el del dibujo (lo primero, atrás)
        var lienzo = new Lienzo();
        dibujo(k, lienzo);
        float mx = ancho / 2f - 0.06f, my = alto / 2f - 0.06f;
        Marco(lienzo.En(k.pinturaHueso), -mx, -my, mx, my, 0.025f);

        for (int c = 0; c < lienzo.capas.Count; c++)
        {
            var (material, v) = lienzo.capas[c];
            // de los dos lados, como las figuras
            int n = v.Count;
            for (int i = 0; i < n; i += 3) { v.Add(v[i]); v.Add(v[i + 2]); v.Add(v[i + 1]); }
            var uvs = new List<Vector2>();
            foreach (var _ in v) uvs.Add(new Vector2(0.5f, 0.5f));
            var go = new GameObject("Pintura_" + material.name);
            go.transform.SetParent(losa, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.012f - 0.006f * c);
            go.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(CrearMalla($"Mural_{nombre}_{c}", v, uvs));
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            Estatico(go);
        }
    }

    // ================================================================== el lore

    static void LoreSolYLuna(Kit k, Lienzo l)
    {
        // el sol y la luna, cada uno en su lado del cielo; abajo, la gente que los mira
        FiguraEn(l.En(k.pinturaSol), Figura.Sol, -0.5f, 0.22f, 0.6f);
        FiguraEn(l.En(k.pinturaLuna), Figura.LunaLlena, 0.5f, 0.22f, 0.45f);
        var h = l.En(k.pinturaHueso);
        Linea(h, new Vector2(-0.85f, -0.5f), new Vector2(0.85f, -0.5f), 0.03f);
        Persona(h, -0.18f, -0.5f, 0.32f, 0.04f, 0f);
        Persona(h, 0.18f, -0.5f, 0.28f, 0.04f, 0f);
    }

    static void LoreEclipse(Kit k, Lienzo l)
    {
        // se juntan: el eclipse sobre la capilla
        FiguraEn(l.En(k.pinturaSol), Figura.Eclipse, 0f, 0.32f, 0.55f);
        FiguraEn(l.En(k.pinturaLuna), Figura.LunaLlena, 0f, 0.32f, 0.28f);
        var h = l.En(k.pinturaHueso);
        Linea(h, new Vector2(-0.85f, -0.58f), new Vector2(0.85f, -0.58f), 0.03f);
        // la capilla: muros a los lados de la puerta, techo a dos aguas y el campanario con su cruz
        Rect(h, -0.32f, -0.58f, -0.08f, -0.2f);
        Rect(h, 0.08f, -0.58f, 0.32f, -0.2f);
        Rect(h, -0.08f, -0.32f, 0.08f, -0.2f);
        Tri(h, new Vector2(-0.38f, -0.2f), new Vector2(0.38f, -0.2f), new Vector2(0f, 0.0f));
        Rect(h, 0.36f, -0.58f, 0.5f, -0.1f);
        Rect(h, 0.415f, -0.1f, 0.445f, 0.04f);
        Rect(h, 0.38f, -0.03f, 0.48f, -0.005f);
    }

    static void LoreCrater(Kit k, Lienzo l)
    {
        // el cráter se abre: el suelo, la escalera que baja y la puerta del fondo
        FiguraEn(l.En(k.pinturaSol), Figura.Eclipse, 0.45f, 0.42f, 0.38f);
        var h = l.En(k.pinturaHueso);
        Linea(h, new Vector2(-0.9f, 0.02f), new Vector2(-0.4f, 0.02f), 0.03f);
        Vector2 p = new Vector2(-0.4f, 0.02f);
        for (int i = 0; i < 5; i++)
        {
            var q = p + new Vector2(0f, -0.11f);
            Linea(h, p, q, 0.025f);
            p = q + new Vector2(0.1f, 0f);
            Linea(h, q, p, 0.025f);
        }
        Linea(h, p, new Vector2(0.7f, p.y), 0.03f);
        Linea(h, new Vector2(0.7f, p.y), new Vector2(0.72f, 0.02f), 0.03f);
        Linea(h, new Vector2(0.72f, 0.02f), new Vector2(0.9f, 0.02f), 0.03f);
        // la puerta, al pie de la escalera
        Marco(h, 0.38f, p.y, 0.6f, p.y + 0.3f, 0.025f);
        Persona(h, -0.72f, 0.02f, 0.3f, 0.06f, 0.03f);
    }

    static void LoreLuz(Kit k, Lienzo l)
    {
        // alguien levanta la luz y en el haz aparecen el sol y la luna
        var h = l.En(k.pinturaHueso);
        Linea(h, new Vector2(-0.85f, -0.48f), new Vector2(0.85f, -0.48f), 0.03f);
        Persona(h, -0.62f, -0.48f, 0.52f, 0.07f, 0f, true);
        var punta = Linterna(h, new Vector2(-0.53f, -0.08f), 20f, 0.2f);
        Cono(h, punta, 20f, 0.95f, 16f);
        FiguraEn(l.En(k.pinturaSol), Figura.Sol, 0.2f, 0.17f, 0.22f);
        FiguraEn(l.En(k.pinturaLuna), Figura.LunaCreciente, 0.46f, 0.22f, 0.2f);
    }

    // ================================================================== los controles

    static void PanelMover(Kit k, Lienzo l)
    {
        var h = l.En(k.pinturaHueso);
        const float t = 0.26f;
        Tecla(h, -0.66f, 0.17f, t, t, Letra('W'));
        Tecla(h, -0.95f, -0.13f, t, t, Letra('A'));
        Tecla(h, -0.66f, -0.13f, t, t, Letra('S'));
        Tecla(h, -0.37f, -0.13f, t, t, Letra('D'));
        // alguien caminando
        Persona(h, 0.05f, -0.45f, 0.7f, 0.12f, 0.02f);
        // el mouse, con flechas a los lados: mirar
        Marco(h, 0.62f, -0.3f, 0.88f, 0.1f, 0.025f);
        Linea(h, new Vector2(0.62f, -0.06f), new Vector2(0.88f, -0.06f), 0.02f);
        Linea(h, new Vector2(0.75f, -0.06f), new Vector2(0.75f, 0.1f), 0.02f);
        Flecha(h, new Vector2(0.56f, -0.1f), new Vector2(0.38f, -0.1f), 0.025f);
        Flecha(h, new Vector2(0.94f, -0.1f), new Vector2(1.04f, -0.1f), 0.025f);
    }

    static void PanelCorrer(Kit k, Lienzo l)
    {
        var h = l.En(k.pinturaHueso);
        Tecla(h, -0.78f, 0.05f, 0.46f, 0.26f, Letra('^'));
        Mas(h, -0.41f, 0.05f);
        Tecla(h, -0.18f, 0.05f, 0.26f, 0.26f, Letra('W'));
        // alguien corriendo, con las líneas de la velocidad detrás
        Persona(h, 0.6f, -0.45f, 0.7f, 0.22f, 0.12f);
        for (int i = 0; i < 3; i++)
        {
            float y = -0.05f + i * 0.14f;
            Linea(h, new Vector2(0.14f + i * 0.04f, y), new Vector2(0.36f, y), 0.025f);
        }
    }

    static void PanelLinterna(Kit k, Lienzo l)
    {
        var h = l.En(k.pinturaHueso);
        Tecla(h, -0.85f, 0f, 0.3f, 0.3f, Letra('F'));
        var punta = Linterna(h, new Vector2(-0.5f, 0f), 0f, 0.36f);
        Cono(h, punta, 0f, 1.15f, 17f);
    }

    static void PanelLuzBlanca(Kit k, Lienzo l)
    {
        var h = l.En(k.pinturaHueso);
        Tecla(h, -0.85f, 0f, 0.3f, 0.3f, Letra('Q'));
        var punta = Linterna(h, new Vector2(-0.52f, 0f), 0f, 0.3f);
        Cono(h, punta, 0f, 0.72f, 14f);
        // la luz blanca despierta el ancla: un aro con su brillo alrededor
        var ancla = new Vector2(0.82f, 0f);
        Anillo(h, ancla, 0.13f, 0.18f);
        Disco(h, ancla.x, ancla.y, 0.07f, 10);
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * Mathf.PI / 4f;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Linea(h, ancla + d * 0.23f, ancla + d * 0.31f, 0.02f);
        }
    }

    static void PanelFiltroSol(Kit k, Lienzo l)
    {
        var s = l.En(k.pinturaSol);
        var h = l.En(k.pinturaHueso);
        Tecla(h, -0.88f, 0.22f, 0.3f, 0.3f, Letra('1'));
        FiguraEn(s, Figura.Sol, -0.88f, -0.25f, 0.34f);
        var punta = Linterna(h, new Vector2(-0.55f, 0.32f), -28f, 0.28f);
        Cono(s, punta, -28f, 0.95f, 15f);
        // el sol hace sólido el puente sobre el vacío
        Rect(h, -0.32f, -0.7f + 0.06f, -0.08f, -0.38f);
        Rect(h, 0.9f, -0.7f + 0.06f, 1.08f, -0.38f);
        Rect(s, -0.08f, -0.44f, 0.9f, -0.38f);
        for (int i = 0; i < 4; i++) Rect(s, 0.08f + i * 0.24f, -0.38f, 0.11f + i * 0.24f, -0.26f);
    }

    static void PanelFiltroLuna(Kit k, Lienzo l)
    {
        var u = l.En(k.pinturaLuna);
        var h = l.En(k.pinturaHueso);
        Tecla(h, -0.88f, 0.22f, 0.3f, 0.3f, Letra('2'));
        FiguraEn(u, Figura.LunaCreciente, -0.88f, -0.25f, 0.34f);
        var punta = Linterna(h, new Vector2(-0.55f, 0f), 0f, 0.28f);
        Cono(u, punta, 0f, 0.9f, 20f);
        // la luna abre la reja: dentro del haz, las barras se cortan
        Marco(h, 0.25f, -0.55f, 1.1f, 0.55f, 0.03f);
        for (int i = 0; i < 5; i++)
        {
            float x = 0.36f + i * 0.155f;
            bool enElHaz = i < 3;
            if (!enElHaz) { Rect(h, x - 0.015f, -0.55f, x + 0.015f, 0.55f); continue; }
            for (float y = -0.5f; y < 0.5f; y += 0.16f) Rect(h, x - 0.015f, y, x + 0.015f, y + 0.07f);
        }
    }

    // ================================================================== primitivas

    static void Rect(List<Vector3> v, float x0, float y0, float x1, float y1)
    {
        Tri(v, new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1));
        Tri(v, new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(x0, y1));
    }

    static void Marco(List<Vector3> v, float x0, float y0, float x1, float y1, float g)
    {
        Rect(v, x0, y0, x1, y0 + g);
        Rect(v, x0, y1 - g, x1, y1);
        Rect(v, x0, y0 + g, x0 + g, y1 - g);
        Rect(v, x1 - g, y0 + g, x1, y1 - g);
    }

    static void Linea(List<Vector3> v, Vector2 a, Vector2 b, float grosor)
    {
        var d = (b - a).normalized;
        var n = new Vector2(-d.y, d.x) * (grosor / 2f);
        // un poco más larga en las puntas, así las líneas que se tocan no dejan huecos
        a -= d * (grosor / 2f);
        b += d * (grosor / 2f);
        Tri(v, a - n, b - n, b + n);
        Tri(v, a - n, b + n, a + n);
    }

    static void Flecha(List<Vector3> v, Vector2 desde, Vector2 hasta, float grosor)
    {
        var d = (hasta - desde).normalized;
        var n = new Vector2(-d.y, d.x);
        var base_ = hasta - d * grosor * 3f;
        Linea(v, desde, base_, grosor);
        Tri(v, base_ - n * grosor * 2f, hasta, base_ + n * grosor * 2f);
    }

    static void Mas(List<Vector3> v, float x, float y)
    {
        Rect(v, x - 0.06f, y - 0.015f, x + 0.06f, y + 0.015f);
        Rect(v, x - 0.015f, y - 0.06f, x + 0.015f, y + 0.06f);
    }

    static void Anillo(List<Vector3> v, Vector2 c, float r0, float r1)
    {
        const int lados = 16;
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            Tri(v, c + d0 * r0, c + d0 * r1, c + d1 * r1);
            Tri(v, c + d0 * r0, c + d1 * r1, c + d1 * r0);
        }
    }

    /// <summary>Una de las figuras de los tallados (sol, luna, eclipse) en (x, y), de 'tamanio' metros.</summary>
    static void FiguraEn(List<Vector3> v, Figura figura, float x, float y, float tamanio)
    {
        // la malla de la figura ya viene de los dos lados: la primera mitad es el frente
        var vs = MallaDeFigura(figura).vertices;
        for (int i = 0; i < vs.Length / 2; i++) v.Add(new Vector3(x + vs[i].x * tamanio, y + vs[i].y * tamanio, 0f));
    }

    /// <summary>Alguien de 'alto' metros parado en (x, suelo), con las piernas abiertas 'zancada' e inclinado 'inclinacion'.</summary>
    static void Persona(List<Vector3> v, float x, float suelo, float alto, float zancada, float inclinacion, bool brazoArriba = false)
    {
        float g = alto * 0.07f;
        var cadera = new Vector2(x, suelo + alto * 0.45f);
        var cuello = new Vector2(x + inclinacion, suelo + alto * 0.8f);
        var hombro = Vector2.Lerp(cadera, cuello, 0.85f);
        Disco(v, cuello.x + inclinacion * 0.3f, cuello.y + alto * 0.11f, alto * 0.1f, 8);
        Linea(v, cadera, cuello, g);
        Linea(v, cadera, new Vector2(x - zancada, suelo), g);
        Linea(v, cadera, new Vector2(x + zancada, suelo), g);
        Linea(v, hombro, new Vector2(x - zancada * 0.8f - alto * 0.04f, cadera.y + alto * 0.02f), g * 0.85f);
        var mano = brazoArriba ? hombro + new Vector2(alto * 0.18f, alto * 0.05f) : new Vector2(x + zancada * 0.8f + alto * 0.04f, cadera.y + alto * 0.02f);
        Linea(v, hombro, mano, g * 0.85f);
    }

    /// <summary>Una linterna que mira hacia 'grados' (0 = derecha) desde 'mango'. Devuelve la punta.</summary>
    static Vector2 Linterna(List<Vector3> v, Vector2 mango, float grados, float largo)
    {
        var d = new Vector2(Mathf.Cos(grados * Mathf.Deg2Rad), Mathf.Sin(grados * Mathf.Deg2Rad));
        var n = new Vector2(-d.y, d.x);
        var cuello = mango + d * largo * 0.62f;
        var punta = mango + d * largo;
        Linea(v, mango, cuello, largo * 0.22f);
        Tri(v, cuello - n * largo * 0.11f, punta - n * largo * 0.2f, punta + n * largo * 0.2f);
        Tri(v, cuello - n * largo * 0.11f, punta + n * largo * 0.2f, cuello + n * largo * 0.11f);
        return punta + d * 0.03f;
    }

    /// <summary>El haz: un triángulo desde 'punta' hacia 'grados', de 'largo' metros y 'abertura' grados a cada lado.</summary>
    static void Cono(List<Vector3> v, Vector2 punta, float grados, float largo, float abertura)
    {
        var d = new Vector2(Mathf.Cos(grados * Mathf.Deg2Rad), Mathf.Sin(grados * Mathf.Deg2Rad));
        var n = new Vector2(-d.y, d.x);
        float media = largo * Mathf.Tan(abertura * Mathf.Deg2Rad);
        Tri(v, punta + n * 0.02f, punta - n * 0.02f + d * largo - n * media, punta + d * largo + n * media);
        Tri(v, punta + n * 0.02f, punta - n * 0.02f, punta - n * 0.02f + d * largo - n * media);
    }

    /// <summary>Una tecla tallada: el contorno de la tecla y su letra en píxeles.</summary>
    static void Tecla(List<Vector3> v, float cx, float cy, float ancho, float alto, string[] letra)
    {
        Marco(v, cx - ancho / 2f, cy - alto / 2f, cx + ancho / 2f, cy + alto / 2f, alto * 0.08f);
        // la sombra de abajo, que la hace leer como tecla
        Rect(v, cx - ancho / 2f + alto * 0.08f, cy - alto / 2f + alto * 0.08f, cx + ancho / 2f - alto * 0.08f, cy - alto / 2f + alto * 0.16f);
        float p = alto * 0.09f;
        float x0 = cx - 2.5f * p, y0 = cy + 3.5f * p + alto * 0.03f;
        for (int fila = 0; fila < letra.Length; fila++)
            for (int col = 0; col < letra[fila].Length; col++)
                if (letra[fila][col] == '#')
                    Rect(v, x0 + col * p, y0 - (fila + 1) * p, x0 + (col + 1) * p, y0 - fila * p);
    }

    /// <summary>Las letras de las teclas, en 5 × 7 píxeles ('^' es la flecha de Shift).</summary>
    static string[] Letra(char c)
    {
        switch (c)
        {
            case 'W': return new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" };
            case 'A': return new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" };
            case 'S': return new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." };
            case 'D': return new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." };
            case 'F': return new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." };
            case 'Q': return new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" };
            case '1': return new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." };
            case '2': return new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" };
            case '^': return new[] { "..#..", ".###.", "#####", ".###.", ".###.", ".###.", "....." };
            default: return new string[0];
        }
    }
}
