using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Tallados de sol y luna: figuras planas y facetadas que brillan sobre las paredes.
/// El sol (naranja) es CUERPO y la luna (azul) es HUECO; juntos, el eclipse.
///
///  - En la escalera del pozo: lunas a la izquierda, soles a la derecha. Bajando, la
///    luna crece y el sol se achica: el eclipse avanza mientras se entra al cráter.
///  - En la rotonda: un hilo de soles desde el ala oeste y uno de lunas desde el ala
///    este hasta la puerta de los sellos. Cada hilo se enciende de a uno al encender su
///    sello (HiloDeTallados); sobre la puerta, el eclipse se enciende con los dos.
///
/// CÓMO FUNCIONA
/// Cada figura es una malla hecha acá con pocos triángulos (discos de 12 lados, una luna
/// cortada por otro disco, rayos en triángulo), en el plano XY y mirando hacia -Z.
/// Glifo() la pone sobre una pared mirando hacia 'afuera'.
/// </summary>
public static partial class ConstructorCrater
{
    enum Figura { LunaLlena, LunaGibosa, LunaCuarto, LunaCreciente, Sol, SolMordido, SolFino, Eclipse }

    static readonly Dictionary<Figura, Mesh> mallasDeFiguras = new Dictionary<Figura, Mesh>();

    /// <summary>El azul de HUECO: frío y pálido, de luna.</summary>
    static readonly Color ColorLuna = new Color(0.6f, 0.74f, 1f);

    /// <summary>Pone una figura de 'tamanio' metros sobre una pared, mirando hacia 'afuera'.</summary>
    static Renderer Glifo(Transform padre, string nombre, Figura figura, Vector3 posicion, Vector3 afuera, float tamanio, Material m)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.SetPositionAndRotation(posicion, Quaternion.LookRotation(-afuera));
        go.transform.localScale = Vector3.one * tamanio;
        go.AddComponent<MeshFilter>().sharedMesh = MallaDeFigura(figura);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return r;
    }

    static Mesh MallaDeFigura(Figura figura)
    {
        if (mallasDeFiguras.TryGetValue(figura, out var hecha) && hecha != null) return hecha;
        var v = new List<Vector3>();
        switch (figura)
        {
            case Figura.LunaLlena: Disco(v, 0f, 0f, 0.5f, 12); break;
            case Figura.LunaGibosa: Mordida(v, 0.5f, 0.82f); break;
            case Figura.LunaCuarto: Mordida(v, 0.5f, 0.5f); break;
            case Figura.LunaCreciente: Mordida(v, 0.5f, 0.24f); break;
            case Figura.Sol: Disco(v, 0f, 0f, 0.27f, 10); Rayos(v, 0.33f, 0.5f, 8, 0.11f); break;
            case Figura.SolMordido: Mordida(v, 0.27f, 0.36f); Rayos(v, 0.33f, 0.48f, 8, 0.1f); break;
            case Figura.SolFino: Mordida(v, 0.27f, 0.16f); Rayos(v, 0.33f, 0.44f, 8, 0.08f); break;
            default: Anillo(v, 0.25f, 0.31f, 16); Rayos(v, 0.35f, 0.5f, 12, 0.05f); break;
        }
        var uvs = new List<Vector2>();
        foreach (var _ in v) uvs.Add(new Vector2(0.5f, 0.5f));
        hecha = GuardarMalla(CrearMalla("Figura_" + figura, v, uvs));
        mallasDeFiguras[figura] = hecha;
        return hecha;
    }

    // un triángulo dado en sentido antihorario en XY; se guarda al revés para que mire a -Z
    static void Tri(List<Vector3> v, Vector2 a, Vector2 b, Vector2 c)
    {
        v.Add(a); v.Add(c); v.Add(b);
    }

    static void Disco(List<Vector3> v, float cx, float cy, float r, int lados)
    {
        var c = new Vector2(cx, cy);
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
            Tri(v, c, c + r * new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), c + r * new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)));
        }
    }

    static void Anillo(List<Vector3> v, float r0, float r1, int lados)
    {
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            Tri(v, d0 * r0, d0 * r1, d1 * r1);
            Tri(v, d0 * r0, d1 * r1, d1 * r0);
        }
    }

    static void Rayos(List<Vector3> v, float r0, float r1, int cantidad, float ancho)
    {
        for (int i = 0; i < cantidad; i++)
        {
            float a = (i + 0.5f) * Mathf.PI * 2f / cantidad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var lado = new Vector2(-d.y, d.x) * (ancho / 2f);
            Tri(v, d * r0 - lado, d * r1, d * r0 + lado);
        }
    }

    /// <summary>
    /// Un disco de radio r al que otro disco igual, corrido 'd' hacia la derecha, le
    /// muerde un costado: d chico = creciente fino; d = r, cuarto; d grande, casi llena.
    /// Se arma en franjas verticales: en cada x, lo que queda arriba y abajo del mordisco.
    /// </summary>
    static void Mordida(List<Vector3> v, float r, float d)
    {
        const int franjas = 14;
        float x0 = -r, x1 = r;
        float AltoFuera(float x) => Mathf.Sqrt(Mathf.Max(0f, r * r - x * x));
        // borde del mordisco en x: 0 si no muerde (la franja va entera), hasta AltoFuera si se come todo
        float AltoMordida(float x)
        {
            float dentro = r * r - (x - d) * (x - d);
            return dentro <= 0f ? 0f : Mathf.Min(Mathf.Sqrt(dentro), AltoFuera(x));
        }
        for (int i = 0; i < franjas; i++)
        {
            float xa = Mathf.Lerp(x0, x1, (float)i / franjas), xb = Mathf.Lerp(x0, x1, (float)(i + 1) / franjas);
            float ha = AltoFuera(xa), hb = AltoFuera(xb), ma = AltoMordida(xa), mb = AltoMordida(xb);
            // arriba: de la mordida al borde; abajo, igual pero espejado
            Tri(v, new Vector2(xa, ma), new Vector2(xb, mb), new Vector2(xb, hb));
            Tri(v, new Vector2(xa, ma), new Vector2(xb, hb), new Vector2(xa, ha));
            Tri(v, new Vector2(xa, -ha), new Vector2(xb, -hb), new Vector2(xb, -mb));
            Tri(v, new Vector2(xa, -ha), new Vector2(xb, -mb), new Vector2(xa, -ma));
        }
    }

    // ================================================================== dónde van

    /// <summary>Los hilos de la rotonda: soles desde el oeste, lunas desde el este, el eclipse en la puerta.</summary>
    static void ConstruirHilosDeLosSellos(Kit k, Transform g, Referencias refs, ReceptorDeLuz selloOeste, ReceptorDeLuz selloEste)
    {
        var hilos = Grupo(g, "Hilos_De_Los_Sellos");
        var afuera = Vector3.back;   // la pared norte mira al sur
        const float z = 26.82f, y = 2.4f;

        HiloDeTallados Hilo(string nombre, ReceptorDeLuz[] sellos, Compuerta esperar, Color color, Renderer[] tallados)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(hilos, false);
            go.transform.position = tallados[tallados.Length - 1].transform.position;
            var hilo = go.AddComponent<HiloDeTallados>();
            hilo.sellos = sellos;
            hilo.esperarA = esperar;
            hilo.tallados = tallados;
            hilo.color = color;
            hilo.fuente = go.AddComponent<AudioSource>();
            ConfigurarAudio(hilo.fuente, null, 0.7f, false, false);
            hilo.tonos = k.audio.tonosAncla;
            return hilo;
        }

        var soles = new[]
        {
            Glifo(hilos, "Sol_1", Figura.Sol, new Vector3(-10.2f, y, z), afuera, 0.9f, k.ambar),
            Glifo(hilos, "Sol_2", Figura.SolMordido, new Vector3(-8f, y, z), afuera, 0.9f, k.ambar),
            Glifo(hilos, "Sol_3", Figura.SolMordido, new Vector3(-5.8f, y, z), afuera, 0.9f, k.ambar),
            Glifo(hilos, "Sol_4", Figura.SolFino, new Vector3(-3.6f, y, z), afuera, 0.9f, k.ambar),
        };
        Hilo("Hilo_Soles", new[] { selloOeste }, refs.atajoOeste, new Color(1f, 0.42f, 0.1f), soles);

        var lunas = new[]
        {
            Glifo(hilos, "Luna_1", Figura.LunaLlena, new Vector3(10.2f, y, z), afuera, 0.9f, k.tallaLuna),
            Glifo(hilos, "Luna_2", Figura.LunaGibosa, new Vector3(8f, y, z), afuera, 0.9f, k.tallaLuna),
            Glifo(hilos, "Luna_3", Figura.LunaCuarto, new Vector3(5.8f, y, z), afuera, 0.9f, k.tallaLuna),
            Glifo(hilos, "Luna_4", Figura.LunaCreciente, new Vector3(3.6f, y, z), afuera, 0.9f, k.tallaLuna),
        };
        Hilo("Hilo_Lunas", new[] { selloEste }, refs.atajoEste, ColorLuna, lunas);

        var eclipse = Glifo(hilos, "Eclipse", Figura.Eclipse, new Vector3(0f, 5.4f, z), afuera, 1.8f, k.tallaEclipse);
        var hiloEclipse = Hilo("Hilo_Eclipse", new[] { selloOeste, selloEste }, null, new Color(0.85f, 0.9f, 1f), new[] { eclipse });
        hiloEclipse.demoraInicial = 2.2f;   // después de que terminen los dos hilos
        hiloEclipse.emisionEncendido = 4f;
    }

    /// <summary>Lunas a la izquierda y soles a la derecha de la escalera (hijos de cada tramo de pared).</summary>
    static void TallarEscalera(Kit k, Transform paredIzq, Transform paredDer, Vector3 centroIzq, Vector3 centroDer, int i)
    {
        Figura[] lunas = { Figura.LunaCreciente, Figura.LunaCuarto, Figura.LunaGibosa, Figura.LunaLlena };
        Figura[] soles = { Figura.Sol, Figura.SolMordido, Figura.SolMordido, Figura.SolFino };
        int n = i / 2;
        if (i % 2 == 0 || n >= lunas.Length) return;
        Glifo(paredIzq, $"Luna_Escalera_{n}", lunas[n], centroIzq, Vector3.right, 0.9f, k.tallaLuna);
        Glifo(paredDer, $"Sol_Escalera_{n}", soles[n], centroDer, Vector3.left, 0.9f, k.ambar);
    }
}
