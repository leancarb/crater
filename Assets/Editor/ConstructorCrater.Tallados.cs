using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Tallados de sol y luna: figuras planas y facetadas que brillan sobre las paredes.
/// El sol (naranja) es CUERPO y la luna (azul) es HUECO; juntos, el eclipse.
///
///  - En la escalera del pozo: a la izquierda, en azul, la luna pasa por delante del
///    sol; a la derecha, en naranja, el sol por delante de la luna. Bajando, el eclipse
///    avanza hasta la totalidad mientras se entra al cráter.
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
    enum Figura
    {
        LunaLlena, LunaGibosa, LunaCuarto, LunaCreciente, Sol, SolMordido, SolFino, Eclipse,
        // la luna pasa por delante del sol (0: recién llega … 3: total) y al revés
        LunaSobreSol0, LunaSobreSol1, LunaSobreSol2, LunaSobreSol3,
        SolSobreLuna0, SolSobreLuna1, SolSobreLuna2, SolSobreLuna3,
        Estrella, Halo
    }

    // cuánto se superponen los dos discos en cada paso del eclipse (distancia entre centros)
    static readonly float[] PasosDelEclipse = { 0.5f, 0.33f, 0.17f, 0f };

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
            case Figura.Estrella: Estrella(v, 0.5f, 0.14f, 4); Estrella(v, 0.3f, 0.1f, 4, 45f); break;
            case Figura.Halo: Rayos(v, 0.36f, 0.5f, 16, 0.05f); Anillo(v, 0.31f, 0.33f, 24); break;
            case Figura.LunaSobreSol0: case Figura.LunaSobreSol1: case Figura.LunaSobreSol2: case Figura.LunaSobreSol3:
            {
                // el sol con rayos, y la luna (sólo su borde) tapándolo desde la derecha
                float d = PasosDelEclipse[figura - Figura.LunaSobreSol0];
                Mordida(v, 0.24f, d, 0.25f);
                Anillo(v, 0.25f, 0.29f, 18, d);
                Rayos(v, 0.34f, 0.48f, 10, 0.08f);
                break;
            }
            case Figura.SolSobreLuna0: case Figura.SolSobreLuna1: case Figura.SolSobreLuna2: case Figura.SolSobreLuna3:
            {
                // la luna llena, y el sol (borde y rayos) pasándole por delante
                float d = PasosDelEclipse[figura - Figura.SolSobreLuna0];
                Mordida(v, 0.26f, d, 0.23f);
                Anillo(v, 0.23f, 0.27f, 18, d);
                Rayos(v, 0.31f, 0.43f, 8, 0.08f, d);
                break;
            }
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

    static void Anillo(List<Vector3> v, float r0, float r1, int lados, float cx = 0f)
    {
        var c = new Vector2(cx, 0f);
        for (int i = 0; i < lados; i++)
        {
            float a0 = i * Mathf.PI * 2f / lados, a1 = (i + 1) * Mathf.PI * 2f / lados;
            Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
            Tri(v, c + d0 * r0, c + d0 * r1, c + d1 * r1);
            Tri(v, c + d0 * r0, c + d1 * r1, c + d1 * r0);
        }
    }

    static void Rayos(List<Vector3> v, float r0, float r1, int cantidad, float ancho, float cx = 0f)
    {
        var c = new Vector2(cx, 0f);
        for (int i = 0; i < cantidad; i++)
        {
            float a = (i + 0.5f) * Mathf.PI * 2f / cantidad;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var lado = new Vector2(-d.y, d.x) * (ancho / 2f);
            Tri(v, c + d * r0 - lado, c + d * r1, c + d * r0 + lado);
        }
    }

    /// <summary>Una estrella de 'puntas' puntas: abanico alternando radio largo y corto.</summary>
    static void Estrella(List<Vector3> v, float largo, float corto, int puntas, float giro = 0f)
    {
        int n = puntas * 2;
        for (int i = 0; i < n; i++)
        {
            float a0 = (i * 360f / n + giro) * Mathf.Deg2Rad, a1 = ((i + 1) * 360f / n + giro) * Mathf.Deg2Rad;
            float r0 = i % 2 == 0 ? largo : corto, r1 = i % 2 == 0 ? corto : largo;
            Tri(v, Vector2.zero, r0 * new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), r1 * new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)));
        }
    }

    /// <summary>
    /// Un disco de radio r al que otro disco igual, corrido 'd' hacia la derecha, le
    /// muerde un costado: d chico = creciente fino; d = r, cuarto; d grande, casi llena.
    /// Se arma en franjas verticales: en cada x, lo que queda arriba y abajo del mordisco.
    /// </summary>
    static void Mordida(List<Vector3> v, float r, float d) => Mordida(v, r, d, r);

    /// <summary>Lo mismo con un mordisco de otro radio ('rb'): un disco tapando a otro.</summary>
    static void Mordida(List<Vector3> v, float r, float d, float rb)
    {
        const int franjas = 14;
        float x0 = -r, x1 = r;
        float AltoFuera(float x) => Mathf.Sqrt(Mathf.Max(0f, r * r - x * x));
        // borde del mordisco en x: 0 si no muerde (la franja va entera), hasta AltoFuera si se come todo
        float AltoMordida(float x)
        {
            float dentro = rb * rb - (x - d) * (x - d);
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
            hilo.manual = true;   // los enciende la cinemática del sello
            return hilo;
        }

        var soles = new[]
        {
            Glifo(hilos, "Sol_1", Figura.Sol, new Vector3(-10.2f, y, z), afuera, 0.9f, k.ambar),
            Glifo(hilos, "Sol_2", Figura.SolMordido, new Vector3(-8f, y, z), afuera, 0.9f, k.ambar),
            Glifo(hilos, "Sol_3", Figura.SolMordido, new Vector3(-5.8f, y, z), afuera, 0.9f, k.ambar),
            Glifo(hilos, "Sol_4", Figura.SolFino, new Vector3(-3.6f, y, z), afuera, 0.9f, k.ambar),
        };
        refs.hiloSoles = Hilo("Hilo_Soles", new[] { selloOeste }, refs.atajoOeste, new Color(1f, 0.42f, 0.1f), soles);

        var lunas = new[]
        {
            Glifo(hilos, "Luna_1", Figura.LunaLlena, new Vector3(10.2f, y, z), afuera, 0.9f, k.tallaLuna),
            Glifo(hilos, "Luna_2", Figura.LunaGibosa, new Vector3(8f, y, z), afuera, 0.9f, k.tallaLuna),
            Glifo(hilos, "Luna_3", Figura.LunaCuarto, new Vector3(5.8f, y, z), afuera, 0.9f, k.tallaLuna),
            Glifo(hilos, "Luna_4", Figura.LunaCreciente, new Vector3(3.6f, y, z), afuera, 0.9f, k.tallaLuna),
        };
        refs.hiloLunas = Hilo("Hilo_Lunas", new[] { selloEste }, refs.atajoEste, ColorLuna, lunas);

        var eclipse = Glifo(hilos, "Eclipse", Figura.Eclipse, new Vector3(0f, 5.4f, z), afuera, 1.8f, k.tallaEclipse);
        var hiloEclipse = Hilo("Hilo_Eclipse", new[] { selloOeste, selloEste }, null, new Color(0.85f, 0.9f, 1f), new[] { eclipse });
        hiloEclipse.demoraInicial = 0.4f;
        hiloEclipse.emisionEncendido = 4f;
        refs.hiloEclipse = hiloEclipse;

        // desde dónde mira la cámara en cada cinemática (el obelisco del centro queda atrás)
        Transform Vista(string nombre, Vector3 ojo, Vector3 objetivo)
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(hilos, false);
            t.SetPositionAndRotation(ojo, Quaternion.LookRotation(objetivo - ojo));
            return t;
        }
        refs.vistaSoles = Vista("Vista_Soles", new Vector3(-3.5f, 2.3f, 20.5f), new Vector3(-6.9f, 2.4f, z));
        refs.vistaLunas = Vista("Vista_Lunas", new Vector3(3.5f, 2.3f, 20.5f), new Vector3(6.9f, 2.4f, z));
        refs.vistaPuerta = Vista("Vista_Puerta", new Vector3(0f, 2.6f, 20f), new Vector3(0f, 3.6f, z));
    }

    /// <summary>Lunas a la izquierda y soles a la derecha de la escalera (hijos de cada tramo de pared).</summary>
    static void TallarEscalera(Kit k, Transform paredIzq, Transform paredDer, Vector3 centroIzq, Vector3 centroDer, int i)
    {
        // izquierda: la luna pasa por delante del sol, en azul; derecha: el sol por delante de la luna, en naranja
        Figura[] lunas = { Figura.LunaSobreSol0, Figura.LunaSobreSol1, Figura.LunaSobreSol2, Figura.LunaSobreSol3 };
        Figura[] soles = { Figura.SolSobreLuna0, Figura.SolSobreLuna1, Figura.SolSobreLuna2, Figura.SolSobreLuna3 };
        int n = i / 2;
        if (i % 2 == 0 || n >= lunas.Length) return;
        Glifo(paredIzq, $"Luna_Escalera_{n}", lunas[n], centroIzq, Vector3.right, 1.2f, k.tallaLuna);
        Glifo(paredDer, $"Sol_Escalera_{n}", soles[n], centroDer, Vector3.left, 1.2f, k.ambar);
    }
}

public static partial class ConstructorCrater
{
    // ================================================================== el arte de las alas

    /// <summary>
    /// Un tronco de pirámide de 'lados' caras (de radio r0 abajo a r1 arriba), parado sobre
    /// y = 0. Caras planas con un tono cada una: piedra tallada low-poly.
    /// </summary>
    static Mesh MallaTronco(string nombre, int lados, float r0, float r1, float alto)
    {
        var v = new List<Vector3>();
        var uvs = new List<Vector2>();
        Vector3 P(int i, float r, float y)
        {
            float a = (i + 0.5f) * Mathf.PI * 2f / lados;
            return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
        }
        for (int i = 0; i < lados; i++)
        {
            Vector3 a = P(i, r0, 0f), b = P(i + 1, r0, 0f), c = P(i + 1, r1, alto), d = P(i, r1, alto);
            var uv = PixelDeFaceta((a + c) * 3.1f + Vector3.one * lados);
            Triangulo(v, uvs, a, c, b, uv);
            Triangulo(v, uvs, a, d, c, uv);
            // tapa de arriba
            Triangulo(v, uvs, new Vector3(0f, alto, 0f), c, d, PixelDeFaceta(d * 5.3f));
        }
        return GuardarMalla(CrearMalla(nombre, v, uvs));
    }

    static GameObject Tronco(Transform padre, string nombre, Vector3 pie, Mesh malla, Material m, float giroY = 0f)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.SetPositionAndRotation(pie, Quaternion.Euler(0f, giroY, 0f));
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        Estatico(go);
        return go;
    }

    /// <summary>
    /// Pebetero del sol (ala oeste): pie y cuenco de piedra facetada y, encima, un sol
    /// tallado visto desde los cuatro lados. Brilla apenas hasta que se enciende el sello
    /// del ala; entonces se prende con su luz.
    /// </summary>
    static void Pebetero(Kit k, Transform padre, string nombre, Vector3 pie, ReceptorDeLuz sello)
    {
        var raiz = Grupo(padre, nombre);
        raiz.position = pie;
        Tronco(raiz, "Pie", pie, MallaTronco("Pebetero_Pie", 8, 0.42f, 0.24f, 0.9f), k.piedra);
        Tronco(raiz, "Cuenco", pie + Vector3.up * 0.9f, MallaTronco("Pebetero_Cuenco", 8, 0.26f, 0.55f, 0.35f), k.basaltoMedio);
        var collider = raiz.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.65f, 0f);
        collider.size = new Vector3(1f, 1.3f, 1f);

        var soles = new List<Renderer>();
        for (int i = 0; i < 4; i++)
        {
            var dir = Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward;
            soles.Add(Glifo(raiz, "Sol", i % 2 == 0 ? Figura.Sol : Figura.Halo, pie + Vector3.up * 1.75f + dir * 0.02f, dir, 0.8f, k.ambar));
        }
        var testigo = raiz.gameObject.AddComponent<TestigoDeSello>();
        testigo.sello = sello;
        testigo.renderers = soles.ToArray();
        testigo.emisionApagado = 0.25f;
        testigo.luz = Luz(raiz, "Fuego", pie + Vector3.up * 1.8f, LuzCalida, 0f, 7f, false);
        testigo.intensidadLuz = 25f;
    }

    /// <summary>
    /// Menhir de la luna (ala este): una piedra alta de cinco caras con una luna y una
    /// estrella talladas del lado de la sala. Se enciende con el sello del ala.
    /// </summary>
    static void Menhir(Kit k, Transform padre, string nombre, Vector3 pie, Vector3 haciaLaSala, ReceptorDeLuz sello)
    {
        var raiz = Grupo(padre, nombre);
        raiz.position = pie;
        float giro = Mathf.Atan2(haciaLaSala.x, haciaLaSala.z) * Mathf.Rad2Deg;
        Tronco(raiz, "Piedra", pie, MallaTronco("Menhir", 5, 0.6f, 0.32f, 3.3f), k.basalto, giro + 18f);
        var collider = raiz.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.65f, 0f);
        collider.size = new Vector3(1.1f, 3.3f, 1.1f);

        var tallados = new[]
        {
            Glifo(raiz, "Luna", Figura.LunaCreciente, pie + Vector3.up * 1.9f + haciaLaSala * 0.62f, haciaLaSala, 0.62f, k.tallaLuna),
            Glifo(raiz, "Estrella", Figura.Estrella, pie + Vector3.up * 2.75f + haciaLaSala * 0.5f, haciaLaSala, 0.38f, k.tallaLuna),
        };
        var testigo = raiz.gameObject.AddComponent<TestigoDeSello>();
        testigo.sello = sello;
        testigo.renderers = tallados;
        testigo.colorEncendido = ColorLuna;
        testigo.emisionApagado = 0.3f;
        testigo.luz = Luz(raiz, "Brillo", pie + Vector3.up * 2f + haciaLaSala * 1.2f, LuzFria, 0f, 6f, false);
        testigo.intensidadLuz = 18f;
    }

    /// <summary>Soles en las paredes del ala oeste y pebeteros en sus rincones.</summary>
    static void DecorarAlaOeste(Kit k, Transform g, ReceptorDeLuz sello)
    {
        var arte = Grupo(g, "Arte_Sol");
        // O1: un friso de soles en la pared sur y un halo grande en la oeste
        Figura[] friso = { Figura.Sol, Figura.Halo, Figura.SolMordido, Figura.Halo, Figura.Sol };
        float[] xs = { -34.5f, -32.5f, -30.5f, -24.5f, -22.5f };
        for (int i = 0; i < xs.Length; i++)
            Glifo(arte, $"Friso_O1_{i}", friso[i], new Vector3(xs[i], 3.4f, -6.92f), Vector3.forward, 0.85f, k.ambar);
        Glifo(arte, "Halo_O1", Figura.Halo, new Vector3(-35.92f, 3.6f, 4f), Vector3.right, 2.2f, k.ambar);
        Glifo(arte, "Sol_O1", Figura.Sol, new Vector3(-35.9f, 3.6f, 4f), Vector3.right, 1.1f, k.ambar);
        // O2: el sol que se va comiendo, en la pared norte, y otro halo
        Figura[] fases = { Figura.Sol, Figura.SolMordido, Figura.SolFino, Figura.Eclipse };
        for (int i = 0; i < fases.Length; i++)
            Glifo(arte, $"Fases_O2_{i}", fases[i], new Vector3(-29f + i * 2.2f, 3.6f, 20.92f), Vector3.back, 1f, k.ambar);
        Glifo(arte, "Halo_O2", Figura.Halo, new Vector3(-35.92f, 3.4f, 15f), Vector3.right, 1.8f, k.ambar);

        Pebetero(k, arte, "Pebetero_O1_A", new Vector3(-34.6f, 0f, -5.6f), sello);
        Pebetero(k, arte, "Pebetero_O1_B", new Vector3(-21.4f, 0f, -5.6f), sello);
        Pebetero(k, arte, "Pebetero_O2", new Vector3(-35f, 0f, 16f), sello);
        Pebetero(k, arte, "Pebetero_O3", new Vector3(-13.4f, 0f, 10.4f), sello);
    }

    /// <summary>Lunas y estrellas en las paredes del ala este y menhires en sus rincones.</summary>
    static void DecorarAlaEste(Kit k, Transform g, ReceptorDeLuz sello)
    {
        var arte = Grupo(g, "Arte_Luna");
        // E1: las fases de la luna en la pared sur (salteando el muro de rejas) y una estrella en la este
        Figura[] fases = { Figura.LunaCreciente, Figura.LunaCuarto, Figura.LunaGibosa, Figura.LunaLlena };
        float[] xs = { 22f, 24.6f, 31.4f, 34f };
        for (int i = 0; i < fases.Length; i++)
            Glifo(arte, $"Fases_E1_{i}", fases[i], new Vector3(xs[i], 3.4f, -6.92f), Vector3.forward, 0.85f, k.tallaLuna);
        Glifo(arte, "Estrella_E1", Figura.Estrella, new Vector3(35.92f, 3.6f, 4f), Vector3.left, 1.8f, k.tallaLuna);
        // E2: un cielo de estrellas y una luna grande
        for (int i = 0; i < 6; i++)
            Glifo(arte, $"Estrellas_E2_{i}", Figura.Estrella, new Vector3(22.5f + i * 2.2f, 3.2f + (i % 2) * 1.1f, 20.92f), Vector3.back, 0.4f + (i % 3) * 0.12f, k.tallaLuna);
        Glifo(arte, "Luna_E2", Figura.LunaLlena, new Vector3(35.92f, 3.6f, 15f), Vector3.left, 1.5f, k.tallaLuna);
        // la galería de abajo: estrellas en la pared sur, para que se vea al caer
        Glifo(arte, "Estrella_Galeria_A", Figura.Estrella, new Vector3(23.5f, -1.6f, 12.08f), Vector3.forward, 0.5f, k.tallaLuna);
        Glifo(arte, "Estrella_Galeria_B", Figura.Estrella, new Vector3(26.2f, -1.2f, 12.08f), Vector3.forward, 0.35f, k.tallaLuna);

        Menhir(k, arte, "Menhir_E1_A", new Vector3(34.4f, 0f, -5.4f), new Vector3(-0.7f, 0f, 0.7f).normalized, sello);
        Menhir(k, arte, "Menhir_E1_B", new Vector3(21.6f, 0f, -5.4f), new Vector3(0.7f, 0f, 0.7f).normalized, sello);
        Menhir(k, arte, "Menhir_E2", new Vector3(34.8f, 0f, 19.8f), new Vector3(-0.7f, 0f, -0.7f).normalized, sello);
    }
}
