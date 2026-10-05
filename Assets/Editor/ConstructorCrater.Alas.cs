using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// El corazón del recorrido: una rotonda con dos alas que se hacen en cualquier
/// orden, y el Cruce, donde hay que usar los dos filtros juntos.
///
///                         CRESTA
///                           |
///                    N3  Antesala         z 51 … 68,5
///                    N2  Puente al borde  z 42 … 51   (anclas a la espalda, reja al final)
///                    N1  Ancla tapada     z 27 … 42   (una de las anclas detrás de una reja)
///                           |  puerta de los sellos
///   O3 Pozo alto ─atajo─ ROTONDA ─atajo─ E3 Rampa y escotilla
///   O2 Dos anclas  |     (z -3 … 27)     |  E2 Trampilla / galería de abajo
///   O1 Puente ─pasillo─┘   UMBRAL   └─pasillo─ E1 Muro de rejas
///
/// Ala oeste (CUERPO): enseñar (dos anclas juntas), probar (una puerta que se sostiene
/// con dos anclas lejanas), torcer (anclas colgadas del techo). Al final, un sello
/// escondido detrás de un tabique, que abre un atajo a la rotonda.
/// Ala este (HUECO): enseñar (muro de rejas), probar (la salida es una trampilla en
/// el piso), torcer (una escotilla en el techo de la rampa). Al final, el sello: una
/// placa de materia hueca que queda disuelta, y su atajo.
/// Los dos sellos abren la puerta del norte.
///
/// CÓMO FUNCIONA
/// Igual que el resto del nivel: cajas en coordenadas de mundo (Caja: esquina mínima
/// y máxima), prefabs (anclas, puentes, rejas) y luces. Debajo de cada abismo hay una
/// red de seguridad (una gruta con escalera, ver RedDeSeguridad): no hay reaparición. Las rejas
/// cortan la luz (ver LinternaController): lo que está detrás no se enciende hasta
/// disolverlas. Los sellos son receptores 'permanentes'.
/// </summary>
public static partial class ConstructorCrater
{
    // ================================================================== la rotonda

    /// <summary>Centro (a nivel del piso) y radio de la rotonda circular.</summary>
    static readonly Vector3 CentroRotonda = new Vector3(0f, 0f, 12f);
    const float RadioRotonda = 15f;
    /// <summary>
    /// Un óculo en el techo (se ve el eclipse) con su haz de luz que baja hasta 'piso':
    /// vuelve a mostrar la meta y lleva la mirada a lo que hay debajo.
    /// </summary>
    static void OculoConHaz(Kit k, Transform g, string nombre, Vector3 techo, float diametro, float piso)
    {
        Oculo(k, g, nombre, techo, diametro, 70f, 9f);
        var haz = Grupo(g, nombre + "_Haz_Visible");
        float alto = techo.y - piso;
        for (int i = 0; i < 2; i++)
        {
            var plano = Plano(haz, $"Haz_{i}", k.hazDeLuz);
            plano.transform.SetPositionAndRotation(new Vector3(techo.x, piso + alto / 2f, techo.z), Quaternion.Euler(0f, 45f + i * 90f, 0f));
            plano.transform.localScale = new Vector3(diametro * 0.95f, alto, 1f);
        }
    }

    /// <summary>
    /// Red de seguridad debajo de un abismo (como en "Spatial Communication in Level Design"):
    /// caerse no reinicia ni corta. Se cae 3 m a una gruta que sigue por debajo del piso de la
    /// orilla de donde se viene, con una luz cálida y un mural que sólo se ve desde ahí (lo
    /// que se gana por caerse). Una escalera de verdad sube por un hueco del piso y deja al
    /// jugador antes del desafío, de cara a intentarlo otra vez; arriba de la escalera, una
    /// luz marca la salida desde abajo. Desde arriba el hueco no se ve: lo tapa una losa al ras
    /// que sólo se corre cuando se llega al pie de la escalera desde la gruta.
    /// 'fondo': la planta de la gruta (x0, z0, x1, z1). La escalera arranca en 'pie' (al nivel
    /// del fondo) y sube hacia 'haciaArriba' (x o z, horizontal). El hueco que deja en el piso
    /// de arriba lo devuelve 'HuecoDeEscalera' (el piso se arma con PisoConHueco).
    /// </summary>
    static void RedDeSeguridad(Kit k, Transform g, string nombre, UnityEngine.Rect fondo, Vector3 pie, Vector3 haciaArriba,
                               Vector3 mural, float giroMural, Material pintura)
    {
        var red = Grupo(g, $"Red_{nombre}");
        Caja(red, $"Red_{nombre}_Fondo", fondo.xMin, FondoRed - 0.3f, fondo.yMin, fondo.xMax, FondoRed, fondo.yMax, k.piso);

        // la escalera: escalones de piedra macizos (sin collider) y una rampa invisible que
        // pasa por el medio de cada uno, como la del cráter: se sube sin saltitos
        var escalera = Grupo(red, $"Red_{nombre}_Escalera");
        Vector3 lateral = Vector3.Cross(Vector3.up, haciaArriba);
        float alzada = -FondoRed / EscalonesRed, pisada = LargoEscaleraRed / EscalonesRed;
        for (int i = 0; i < EscalonesRed; i++)
        {
            float tope = FondoRed + (i + 1) * alzada;
            Vector3 centro = pie + haciaArriba * ((i + 0.5f) * pisada);
            var escalon = Bloque(escalera, $"Escalon_{i:00}",
                new Vector3(centro.x, (FondoRed - 0.3f + tope) / 2f, centro.z),
                Abs(haciaArriba * (pisada + 0.02f) + lateral * AnchoEscaleraRed) + Vector3.up * (tope - FondoRed + 0.3f),
                i % 2 == 0 ? k.piedra : k.basaltoMedio);
            Object.DestroyImmediate(escalon.GetComponent<Collider>());
        }
        Vector3 abajo = new Vector3(pie.x, FondoRed, pie.z), arriba = abajo + haciaArriba * LargoEscaleraRed + Vector3.up * -FondoRed;
        var rampa = Rampa(escalera, $"Red_{nombre}_Rampa_Invisible", abajo, arriba, AnchoEscaleraRed, k.piedra);
        Object.DestroyImmediate(rampa.GetComponent<MeshRenderer>());

        // el hueco del piso lo tapa una losa al ras: desde arriba no se ve ninguna escalera (si
        // no, parecía el camino). Al llegar al pie de la escalera desde la gruta, la losa se
        // corre de costado, por debajo del piso de la sala, y queda abierta
        Vector3 haciaLaSala = lateral;
        Vector3 medio = (abajo + arriba) / 2f;
        if (!fondo.Contains(new Vector2(medio.x + lateral.x * 1.3f, medio.z + lateral.z * 1.3f))) haciaLaSala = -lateral;
        var raiz = new GameObject($"Red_{nombre}_Tapa");
        raiz.transform.SetParent(red, false);
        raiz.transform.position = new Vector3(medio.x, -0.17f, medio.z);
        Bloque(raiz.transform, "Losa", raiz.transform.position,
            Abs(haciaArriba * (LargoEscaleraRed + 0.04f) + lateral * (AnchoEscaleraRed + 0.04f)) + Vector3.up * 0.3f, k.piso, false);
        var sonido = raiz.AddComponent<AudioSource>();
        ConfigurarAudio(sonido, k.audio.compuerta, 0.7f, false, true);
        var tapa = raiz.AddComponent<Compuerta>();
        tapa.desplazamiento = haciaLaSala * (AnchoEscaleraRed + 0.15f) + Vector3.down * 0.14f;
        tapa.duracion = 1.2f;
        tapa.sonido = sonido;
        var zona = Zona(red, $"Zona_Red_{nombre}_Escalera", new Vector3(pie.x, FondoRed + 1f, pie.z) - haciaArriba * 0.6f,
            Abs(haciaArriba * 1.6f + lateral * 2.4f) + Vector3.up * 2f);
        UnityEventTools.AddPersistentListener(zona.alEntrar, new UnityAction(tapa.Abrir));

        // la luz que marca la salida (se ve desde el fondo, arriba de la escalera) y la del mural
        // (abajo de la losa: desde la sala no se ve)
        Luz(red, $"Luz_Red_{nombre}_Salida", arriba - haciaArriba * 1.1f + Vector3.down * 0.9f, LuzCalida, 14f, 5f, false);
        Luz(red, $"Luz_Red_{nombre}_Gruta", new Vector3(mural.x, FondoRed + 1.9f, mural.z) + Quaternion.Euler(0f, giroMural, 0f) * Vector3.forward * 1.6f,
            LuzCalida, 10f, 5.5f, false);
        Motivo(k, red, $"Mural_Red_{nombre}", mural, giroMural, 0.7f, pintura);
    }

    /// <summary>El hueco que la escalera de la red deja en el piso de arriba (x0, z0, x1, z1).</summary>
    static UnityEngine.Rect HuecoDeEscalera(Vector3 pie, Vector3 haciaArriba)
    {
        Vector3 lateral = Vector3.Cross(Vector3.up, haciaArriba) * (AnchoEscaleraRed / 2f);
        Vector3 a = pie + lateral, b = pie - lateral + haciaArriba * LargoEscaleraRed;
        return UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
    }

    /// <summary>Un piso (x0 … x1, z0 … z1, de -0,3 a 0) con un hueco rectangular: hasta cuatro losas.</summary>
    static void PisoConHueco(Kit k, Transform g, string nombre, float x0, float z0, float x1, float z1, UnityEngine.Rect hueco)
    {
        void Losa(string sufijo, float a0, float b0, float a1, float b1)
        {
            if (a1 - a0 > 0.01f && b1 - b0 > 0.01f) Caja(g, nombre + sufijo, a0, -0.3f, b0, a1, 0, b1, k.piso);
        }
        Losa("", x0, z0, hueco.xMin, z1);
        Losa("_B", hueco.xMax, z0, x1, z1);
        Losa("_C", hueco.xMin, z0, hueco.xMax, hueco.yMin);
        Losa("_D", hueco.xMin, hueco.yMax, hueco.xMax, z1);
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    const float FondoRed = -3f, LargoEscaleraRed = 3.6f, AnchoEscaleraRed = 1.2f;
    const int EscalonesRed = 12;

    /// <summary>Lo que baja la plaza del centro de la rotonda.</summary>
    const float ProfundidadPlaza = -1.2f;
    /// <summary>Radio de la plaza hundida (adentro de los escalones).</summary>
    const float RadioPlaza = 8f;

    /// <summary>
    /// El piso de la rotonda: un anillo de bloques a nivel (r 9,5 … 15,4), cuatro escalones de
    /// 30 cm que bajan hacia el centro y el fondo de la plaza, 1,2 m más abajo.
    /// </summary>
    static void PisoHundido(Kit k, Transform g)
    {
        Vector3 c = new Vector3(CentroRotonda.x, 0f, CentroRotonda.z);
        void Anillo(string nombre, float r0, float r1, float tope, float fondo, int bloques)
        {
            float rm = (r0 + r1) / 2f;
            float ancho = 2f * Mathf.PI * r1 / bloques * 1.2f;
            for (int i = 0; i < bloques; i++)
            {
                float ang = (i + 0.5f) * 360f / bloques;
                var dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0f, Mathf.Sin(ang * Mathf.Deg2Rad));
                var b = Bloque(g, $"{nombre}_{i:00}", c + dir * rm + Vector3.up * ((tope + fondo) / 2f),
                    new Vector3(ancho, tope - fondo, r1 - r0), k.piso);
                b.transform.rotation = Quaternion.LookRotation(dir);
            }
        }
        Anillo("Piso_Rotonda_Anillo", 9.5f, 15.6f, 0f, -0.3f, 40);
        for (int e = 0; e < 3; e++)
            Anillo($"Escalon_Plaza_{e}", 9.5f - (e + 1) * 0.5f, 9.5f - e * 0.5f, -0.3f * (e + 1), ProfundidadPlaza - 0.3f, 36);
        Caja(g, "Piso_Plaza", c.x - RadioPlaza - 0.4f, ProfundidadPlaza - 0.3f, c.z - RadioPlaza - 0.4f,
            c.x + RadioPlaza + 0.4f, ProfundidadPlaza, c.z + RadioPlaza + 0.4f, k.piso);
        // lo que queda fuera del anillo de bloques en las esquinas (debajo de la pared) se rellena
        Caja(g, "Piso_Rotonda_Base", -RadioRotonda - 0.6f, -1.8f, -3.3f, RadioRotonda + 0.6f, ProfundidadPlaza - 0.3f, 27.6f, k.piso);
    }

    /// <summary>Cuánto se corre cada ala desde donde la arma su código: hacia afuera y al norte.</summary>
    static readonly Vector3 CorrimientoAlaOeste = new Vector3(-3f, 0f, 11f), CorrimientoAlaEste = new Vector3(3f, 0f, 11f);

    /// <summary>
    /// Sala circular de 30 m. Las alas se entran por la mitad de cada costado (z 10 … 14),
    /// lejos de la boca del Umbral; los atajos vuelven por dos pasillos que entran en
    /// diagonal por el noroeste y el noreste. Al norte, la puerta de los sellos.
    /// </summary>
    static void ConstruirRotonda(Kit k, Transform g, Referencias refs)
    {
        float cz = CentroRotonda.z;
        // el piso es una plaza hundida (como un templete semisubterráneo): un anillo a nivel por
        // donde se entra y se sale, y en el centro, 1,2 m más abajo y con cuatro escalones
        // alrededor, el anillo de columnas y el obelisco. Al entrar desde el Umbral se ve todo
        // de arriba: las dos alas, el obelisco y la puerta del norte (vista privilegiada)
        PisoHundido(k, g);
        Caja(g, "Techo_Rotonda", -RadioRotonda - 0.6f, 7, -3.3f, RadioRotonda + 0.6f, 7.3f, 27.6f, k.techo);

        // la pared: un anillo de bloques con aberturas (ángulos desde +x, en sentido antihorario)
        var aberturas = new[]
        {
            new Vector2(246.4f, 293.6f),   // sur: la boca del Umbral (x ±6)
            new Vector2(172.4f, 187.6f),   // oeste: el pasillo al ala (z 10 … 14)
            new Vector2(-7.6f, 7.6f),      // este: el pasillo al ala
            new Vector2(122.2f, 137.2f),   // noroeste: vuelve el atajo oeste (x -11 … -8)
            new Vector2(42.8f, 57.8f),     // noreste: vuelve el atajo este (x 8 … 11)
            new Vector2(82.4f, 97.6f),     // norte: la puerta de los sellos (x ±2)
        };
        const int bloques = 96;
        float anchoBloque = 2f * Mathf.PI * (RadioRotonda + 0.3f) / bloques * 1.2f;
        for (int i = 0; i < bloques; i++)
        {
            float ang = i * 360f / bloques;
            bool abierto = false;
            foreach (var ab in aberturas) abierto |= Mathf.DeltaAngle(ab.x, ang) >= 0f && Mathf.DeltaAngle(ang, ab.y) >= 0f;
            if (abierto) continue;
            var dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0f, Mathf.Sin(ang * Mathf.Deg2Rad));
            var b = Bloque(g, $"Pared_Rotonda_{i:00}", CentroRotonda + dir * (RadioRotonda + 0.3f) + Vector3.up * 3.35f,
                new Vector3(anchoBloque, 7.3f, 0.6f), i % 4 == 0 ? k.basaltoMedio : k.basalto);
            b.transform.rotation = Quaternion.LookRotation(-dir);
        }

        // jambas que cierran lo que el anillo de bloques deja entre cada abertura y su pasillo
        Caja(g, "Jamba_Umbral_Izq", -6.6f, -0.3f, -3.3f, -5.9f, 7, -1.2f, k.basalto);
        Caja(g, "Jamba_Umbral_Der", 5.9f, -0.3f, -3.3f, 6.6f, 7, -1.2f, k.basalto);
        Caja(g, "Dintel_Umbral", -6, 4, -3.3f, 6, 7, -3, k.basalto);
        foreach (float lado in new[] { -1f, 1f })
        {
            string n = lado < 0f ? "O" : "E";
            float x0 = lado < 0f ? -15.7f : 14.6f, x1 = x0 + 1.1f;
            Caja(g, $"Jamba_Ala_{n}_Sur", x0, -0.3f, 9.4f, x1, 7, 10, k.basalto);
            Caja(g, $"Jamba_Ala_{n}_Norte", x0, -0.3f, 14, x1, 7, 14.6f, k.basalto);
            Caja(g, $"Dintel_Ala_{n}", x0, 4.5f, 10, x1, 7, 14, k.basalto);
            // el piso cruza el umbral del anillo hasta el pasillo del ala
            Caja(g, $"Piso_Umbral_Ala_{n}", lado < 0f ? -15.4f : 14.9f, -0.3f, 10, lado < 0f ? -14.9f : 15.4f, 0, 14, k.piso);
        }
        Caja(g, "Jamba_Sellos_Izq", -2.7f, -0.3f, 26.3f, -2, 7, 27.3f, k.basalto);
        Caja(g, "Jamba_Sellos_Der", 2, -0.3f, 26.3f, 2.7f, 7, 27.3f, k.basalto);
        Caja(g, "Dintel_Sellos", -2, 4, 27, 2, 7, 27.3f, k.basalto);

        // los pasillos de vuelta de los atajos: del atajo (en el ala) hasta el anillo
        foreach (float lado in new[] { -1f, 1f })
        {
            string n = lado < 0f ? "O" : "E";
            float X(float x) => lado * x;   // se arma del lado oeste y se espeja
            void C(string nombre, float xa, float y0, float za, float xb, float y1, float zb, Material m) =>
                Caja(g, nombre, Mathf.Min(X(xa), X(xb)), y0, za, Mathf.Max(X(xa), X(xb)), y1, zb, m);
            // arranca adentro del anillo del piso: entre el anillo y el pasillo quedaba un pozo
            C($"Piso_Vuelta_{n}", 11, -0.3f, 21.6f, 8, 0, 33.7f, k.piso);
            C($"Piso_Vuelta_{n}_Atajo", 15, -0.3f, 33.7f, 8, 0, 37.8f, k.piso);
            C($"Muro_Vuelta_{n}_Interior", 11.3f, -0.3f, 21.6f, 11, 7, 33.7f, k.basalto);
            C($"Muro_Vuelta_{n}_Atajo_Sur", 15, -0.3f, 33.4f, 11, 7, 33.7f, k.basalto);
            // arranca en el anillo: más al sur se metía en la rotonda y tapaba el primer tallado
            C($"Muro_Vuelta_{n}_Exterior", 8, -0.3f, 24.8f, 7.7f, 7, 38.1f, k.basaltoMedio);
            C($"Muro_Vuelta_{n}_Fondo", 15.3f, -0.3f, 37.8f, 7.7f, 7, 38.1f, k.basalto);
            C($"Techo_Vuelta_{n}", 15.3f, 7, 27.6f, 7.7f, 7.3f, 38.1f, k.techo);
            // con sombras: sin ellas atravesaba la pared y se veía una franja cálida en el ala
            Luz(g, $"Luz_Vuelta_{n}", new Vector3(X(9.5f), 5.5f, 33f), LuzCalida, 40f, 9f, true);
        }

        // (el óculo de la rotonda va sobre el obelisco: ver MapaYObelisco)
        ConstruirAnilloDelMapa(k, g, new Vector3(0f, ProfundidadPlaza + 0.12f, cz), 0.4f);

        Luz(g, "Luz_Rotonda", new Vector3(0f, 5.8f, 4f), LuzCalida, 150f, 17f, true);
        Luz(g, "Luz_Rotonda_Norte", new Vector3(0f, 5.8f, 21f), LuzCalida, 110f, 15f, false);
    }

    /// <summary>
    /// La pared interior que cada ala necesita del lado de la rotonda (antes la ponía la
    /// rotonda cuadrada): de la última sala hasta la puerta del atajo, con el pozo
    /// bajando hasta el fondo. Coordenadas del ala SIN correr (se corren con el ala).
    /// </summary>
    static void ParedInteriorDelAla(Kit k, Transform g, float lado, float pisoBajo, float inicioAtajo = 23f, float finAtajo = 26.5f)
    {
        float x0 = lado < 0f ? -12.3f : 12f, x1 = x0 + 0.3f;
        string n = lado < 0f ? "O" : "E";
        Caja(g, $"Muro_Interior_{n}", x0, pisoBajo, 9.3f, x1, 6.3f, inicioAtajo, k.basaltoMedio);
        Caja(g, $"Muro_Interior_{n}_Fin", x0, -0.3f, finAtajo, x1, 6.3f, 27.3f, k.basaltoMedio);
        Caja(g, $"Dintel_Atajo_{n}", x0, 4, inicioAtajo, x1, 6.3f, finAtajo, k.basaltoMedio);
    }

    /// <summary>
    /// La puerta del norte, los hilos de tallados que llegan a ella (se encienden de a
    /// uno con cada sello) y los faros de los atajos. Va después de las alas: necesita los sellos.
    /// </summary>
    static void ConstruirPuertaDeLosSellos(Kit k, Transform g, Referencias refs, ReceptorDeLuz selloOeste, ReceptorDeLuz selloEste)
    {
        // la losa no toca el piso: por la rendija de abajo se escapa luz de lo que hay del otro
        // lado. Se sabe que hay algo detrás, pero no qué (misterio)
        refs.puertaSellos = CompuertaLosa(k, g, "Puerta_Sellos", new Vector3(0f, 2.06f, 27.15f), new Vector3(4f, 3.88f, 0.3f),
            new Vector3(0f, -4.4f, 0f), 3f, false, selloOeste, selloEste);
        var rendija = Bloque(g, "Rendija_Sellos", new Vector3(0f, 0.01f, 27.15f), new Vector3(3.9f, 0.02f, 0.3f), k.luzEclipse);
        Object.DestroyImmediate(rendija.GetComponent<Collider>());
        Luz(g, "Luz_Detras_Sellos", new Vector3(0f, 0.5f, 28.6f), new Color(0.85f, 0.92f, 1f), 22f, 6f, true);

        // la abre la cinemática del segundo sello, a la vista (CinematicaDeSello)
        refs.puertaSellos.abrirSoloPorOrden = true;
        refs.selloOeste = selloOeste;
        refs.selloEste = selloEste;

        // los hilos de soles y lunas que llegan a la puerta (ConstructorCrater.Tallados.cs)
        ConstruirHilosDeLosSellos(k, g, refs, selloOeste, selloEste);
        ArteDeLosPasillosDeVuelta(k, g, selloOeste, selloEste);
        refs.obelisco = MapaYObelisco(k, g, selloOeste, selloEste);

        // sobre la boca de cada pasillo de vuelta, en el anillo, un tallado con luz: se ve de lejos cuál ala está hecha
        void Faro(string nombre, float lado, ReceptorDeLuz sello)
        {
            float ang = (lado < 0f ? 141f : 39f) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            var tallado = Glifo(g, nombre, lado < 0f ? Figura.Sol : Figura.LunaLlena,
                CentroRotonda + dir * (RadioRotonda - 0.05f) + Vector3.up * 4.6f, -dir, 0.9f, lado < 0f ? k.ambar : k.tallaLuna);
            var testigo = tallado.gameObject.AddComponent<TestigoDeSello>();
            testigo.sello = sello;
            testigo.renderers = new[] { tallado };
            testigo.colorEncendido = lado < 0f ? new Color(1f, 0.42f, 0.1f) : ColorLuna;
            testigo.emisionApagado = 0.02f;
            testigo.luz = Luz(g, nombre + "_Luz", CentroRotonda + dir * (RadioRotonda - 1.6f) + Vector3.up * 5f, lado < 0f ? LuzCalida : LuzFria, 0f, 9f, true);
            testigo.intensidadLuz = 45f;
        }
        Faro("Faro_Atajo_Oeste", -1f, selloOeste);
        Faro("Faro_Atajo_Este", 1f, selloEste);
    }

    /// <summary>Las cuatro caras verticales de una pieza del FBX (una columna, el obelisco):
    /// hacia dónde mira cada una y su centro a la altura 'y'.</summary>
    static List<(Vector3 normal, Vector3 centro)> CarasVerticales(Renderer r, float y)
    {
        var caras = new List<(Vector3, Vector3)>();
        var malla = r.GetComponent<MeshFilter>().sharedMesh;
        var t = r.transform;
        Vector3 centro = t.TransformPoint(malla.bounds.center);
        for (int eje = 0; eje < 3; eje++)
        {
            var local = Vector3.zero;
            local[eje] = 1f;
            Vector3 mundo = t.TransformVector(local * malla.bounds.extents[eje]);
            if (Mathf.Abs(mundo.y) > 0.5f * mundo.magnitude) continue;   // el eje vertical (la altura de la pieza)
            foreach (float signo in new[] { -1f, 1f })
            {
                var n = new Vector3(mundo.x, 0f, mundo.z) * signo;
                var c = centro + n;
                caras.Add((n.normalized, new Vector3(c.x, y, c.z)));
            }
        }
        return caras;
    }

    /// <summary>
    /// El centro de la rotonda deja de ser decorado:
    ///  - Mapa del progreso: las columnas del lado oeste llevan un sol en sus caras y las del
    ///    este una luna; se encienden con el sello de su ala. De un vistazo se ve qué falta.
    ///  - El obelisco, el reloj del eclipse: en su cara oeste un sol y en la este una luna, que se
    ///    encienden con el sello de su ala. Con los dos, en la cinemática, se enciende el eclipse
    ///    de su cara sur (mira a la puerta) y desde ahí la mirada va a la puerta, que se abre.
    /// </summary>
    static ObeliscoDelEclipse MapaYObelisco(Kit k, Transform g, ReceptorDeLuz selloOeste, ReceptorDeLuz selloEste)
    {
        var mapa = g.Find("Anillo_Del_Mapa");
        if (mapa == null) throw new System.InvalidOperationException("Falta el anillo del mapa en la rotonda");
        Vector3 centro = new Vector3(CentroRotonda.x, 0f, CentroRotonda.z);
        var marcas = Grupo(g, "Mapa_Del_Progreso");

        // el obelisco: se gira en su lugar para que sus caras miren a los cuatro puntos cardinales
        var obeliscoR = System.Array.Find(mapa.GetComponentsInChildren<Renderer>(true), r => r.name == "Anillo_Obelisco");
        if (obeliscoR == null) throw new System.InvalidOperationException("SM_Mapa01_Recorrido.fbx no tiene Anillo_Obelisco");
        var caraUno = CarasVerticales(obeliscoR, 0f)[0].normal;
        float giro = Mathf.Atan2(caraUno.z, caraUno.x) * Mathf.Rad2Deg;   // de esa cara a +x
        giro = Mathf.Repeat(giro + 45f, 90f) - 45f;                        // el giro más chico
        obeliscoR.transform.RotateAround(obeliscoR.bounds.center, Vector3.up, giro);

        // columnas: un sol (oeste) o una luna (este) en cada cara
        var soles = new List<Renderer>();
        var lunas = new List<Renderer>();
        foreach (var r in mapa.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.name.StartsWith("Anillo_Columna")) continue;
            float dx = r.bounds.center.x - centro.x;
            if (Mathf.Abs(dx) < 0.5f) continue;   // las del eje norte-sur quedan neutras
            bool oeste = dx < 0f;
            int i = 0;
            foreach (var (normal, cara) in CarasVerticales(r, r.bounds.min.y + 2.3f))
                (oeste ? soles : lunas).Add(Glifo(marcas, $"{r.name}_{i++}", oeste ? Figura.Sol : Figura.LunaCreciente,
                    cara + normal * 0.02f, normal, 0.3f, oeste ? k.ambar : k.tallaLuna, separar: false));
        }
        foreach (bool oeste in new[] { true, false })
        {
            var lado = Grupo(marcas, oeste ? "Columnas_Sol" : "Columnas_Luna");
            var testigo = lado.gameObject.AddComponent<TestigoDeSello>();
            testigo.sello = oeste ? selloOeste : selloEste;
            testigo.renderers = (oeste ? soles : lunas).ToArray();
            if (!oeste) testigo.colorEncendido = ColorLuna;
            testigo.emisionApagado = 0.02f;
            testigo.luz = Luz(lado, "Luz", centro + new Vector3(oeste ? -3.5f : 3.5f, 3f, 0f), oeste ? LuzCalida : LuzFria, 0f, 7f, false);
            testigo.intensidadLuz = 14f;
        }

        // las caras del obelisco: el sol al oeste y la luna al este, que se encienden con el sello
        // de su ala; al sur, el eclipse, que se enciende en la cinemática del segundo sello
        var raiz = Grupo(g, "Obelisco_Del_Eclipse");
        var obelisco = raiz.gameObject.AddComponent<ObeliscoDelEclipse>();
        (Vector3 normal, Vector3 centro) Cara(Vector3 hacia, float y)
        {
            var mejor = CarasVerticales(obeliscoR, y)[0];
            foreach (var c in CarasVerticales(obeliscoR, y)) if (Vector3.Dot(c.normal, hacia) > Vector3.Dot(mejor.normal, hacia)) mejor = c;
            return mejor;
        }
        void CaraDelSello(string nombre, Vector3 hacia, Figura figura, Material m, ReceptorDeLuz sello, bool luna)
        {
            var (normal, punto) = Cara(hacia, obeliscoR.bounds.min.y + 2.6f);
            var cara = Grupo(raiz, nombre);
            var testigo = cara.gameObject.AddComponent<TestigoDeSello>();
            testigo.sello = sello;
            testigo.renderers = new[] { Glifo(cara, "Tallado", figura, punto, normal, 0.95f, m) };
            if (luna) testigo.colorEncendido = ColorLuna;
            testigo.emisionApagado = 0.03f;
            testigo.luz = Luz(cara, "Brillo", punto + normal * 0.9f, luna ? ColorLuna : new Color(1f, 0.55f, 0.2f), 0f, 5f, false);
            testigo.intensidadLuz = 10f;
        }
        CaraDelSello("Obelisco_Sol", Vector3.left, Figura.Sol, k.ambar, selloOeste, false);
        CaraDelSello("Obelisco_Luna", Vector3.right, Figura.LunaCreciente, k.tallaLuna, selloEste, true);
        var (normalSur, puntoSur) = Cara(Vector3.back, obeliscoR.bounds.max.y - 1.3f);
        obelisco.eclipse = Glifo(raiz, "Obelisco_Eclipse", Figura.Eclipse, puntoSur, normalSur, 1.1f, k.tallaEclipse);
        obelisco.luz = Luz(raiz, "Luz_Eclipse", puntoSur + normalSur * 1.2f, new Color(0.85f, 0.92f, 1f), 0f, 12f, false);

        // arriba del obelisco, un círculo en el techo: si se mira para arriba se ve el eclipse,
        // y entra un haz de luz que cae sobre el obelisco e invita a mirar por ahí
        var arriba = new Vector3(obeliscoR.bounds.center.x, 6.97f, obeliscoR.bounds.center.z);
        Oculo(k, g, "Oculo_Rotonda", arriba, 2.8f, 120f, 12f);
        var haz = Grupo(g, "Haz_Oculo");
        for (int i = 0; i < 2; i++)
        {
            var plano = Plano(haz, $"Haz_{i}", k.hazDeLuz);
            plano.transform.SetPositionAndRotation(new Vector3(arriba.x, (7f + ProfundidadPlaza) / 2f, arriba.z), Quaternion.Euler(0f, 45f + i * 90f, 0f));
            plano.transform.localScale = new Vector3(2.6f, 7f - ProfundidadPlaza, 1f);
        }
        return obelisco;
    }

    /// <summary>
    /// El anillo del mapa de Blender (Assets/Models/CraterMapa): seis columnas y el
    /// obelisco sobre un piso redondo. Del FBX se usan sólo esas piezas, escaladas y
    /// centradas en 'centro' (la cara de arriba del piso queda a esa altura), con el
    /// obelisco mirando al norte, hacia la puerta de los sellos.
    /// </summary>
    static void ConstruirAnilloDelMapa(Kit k, Transform g, Vector3 centro, float escala)
    {
        var mapa = Instanciar(k.modeloMapaRecorrido, g, "Anillo_Del_Mapa");
        foreach (var r in mapa.GetComponentsInChildren<Renderer>(true))
            if (!r.name.StartsWith("Anillo")) Object.DestroyImmediate(r.gameObject);
        mapa.transform.localScale = Vector3.one * escala;

        Renderer Pieza(string nombre) => System.Array.Find(mapa.GetComponentsInChildren<Renderer>(true), r => r.name == nombre);
        var piso = Pieza("Anillo_Piso");
        var obelisco = Pieza("Anillo_Obelisco");
        if (piso == null) throw new System.InvalidOperationException("SM_Mapa01_Recorrido.fbx no tiene Anillo_Piso");

        // centrar: el medio del piso en 'centro', su cara de arriba a la altura de 'centro'
        Bounds b = piso.bounds;
        mapa.transform.position += centro - new Vector3(b.center.x, b.max.y, b.center.z);
        // girar alrededor del centro hasta que el obelisco quede al norte (+z)
        if (obelisco != null)
        {
            Vector3 hacia = obelisco.bounds.center - centro;
            mapa.transform.RotateAround(centro, Vector3.up, -Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg);
        }

        Pintar(mapa, r => r.name.Contains("Obelisco") ? k.piedra : r.name.Contains("Piso") ? k.piso : k.basaltoMedio);
        // columnas y obelisco sólidos; el piso es apenas un escalón, sin collider
        foreach (var r in mapa.GetComponentsInChildren<Renderer>(true))
            if (!r.name.Contains("Piso") && r.GetComponent<Collider>() == null) r.gameObject.AddComponent<BoxCollider>();
        Estatico(mapa);
    }

    // ================================================================== ala oeste: CUERPO

    static ReceptorDeLuz ConstruirAlaOeste(Kit k, Transform g, Referencias refs)
    {
        // pasillo, con el filtro
        Caja(g, "Piso_Pasillo_O", -20, -0.3f, -1, -12.3f, 0, 3, k.piso);
        Caja(g, "Muro_Pasillo_O_Sur", -20, -0.3f, -1.3f, -12.3f, 4.5f, -1, k.basalto);
        Caja(g, "Muro_Pasillo_O_Norte", -20, -0.3f, 3, -12.3f, 4.5f, 3.3f, k.basalto);
        Caja(g, "Techo_Pasillo_O", -20, 4.5f, -1.3f, -12.3f, 4.8f, 3.3f, k.techo);
        // el filtro no está en el pasillo sino adentro de la primera sala, a la vista del abismo:
        // primero se ve el problema (no se puede cruzar), después se encuentra la solución
        ColocarRecogible(k, g, "Recogible_Cuerpo", k.cuerpo, new Vector3(-23f, 0f, 6.5f));
        MuralesDelFiltro(k, g, -1f);
        Luz(g, "Luz_Pasillo_O", new Vector3(-16f, 3.8f, 1f), LuzCalida, 50f, 8f, false);

        // ---- O1 · enseñar: un abismo y dos anclas juntas del otro lado (entran juntas en el cono)
        // la red de seguridad del abismo: la escalera sube pegada a la pared de la entrada
        var pieO1 = new Vector3(-20.6f, 0f, -6f);
        PisoConHueco(k, g, "Piso_O1_Este", -26, -7, -20, 9, HuecoDeEscalera(pieO1, Vector3.forward));
        Caja(g, "Piso_O1_Oeste", -36, -0.3f, -7, -28.5f, 0, 9, k.piso);
        Caja(g, "Muro_O1_Este_Sur", -20, -9, -7.3f, -19.7f, 6, -1, k.basaltoMedio);
        Caja(g, "Muro_O1_Este_Norte", -20, -9, 3, -19.7f, 6, 9.3f, k.basaltoMedio);
        Caja(g, "Muro_O1_Este_Bajo", -20, -9, -1, -19.7f, -0.3f, 3, k.basaltoMedio);
        Caja(g, "Dintel_O1_Este", -20, 4.5f, -1, -19.7f, 6, 3, k.basaltoMedio);
        Caja(g, "Muro_O1_Sur", -36.3f, -9, -7.3f, -19.7f, 6, -7, k.basalto);
        Caja(g, "Muro_O1_Oeste", -36.3f, -9, -7, -36, 6, 9.3f, k.basalto);
        Caja(g, "Muro_O1_Norte_1", -36.3f, -9, 9, -35, 6, 9.3f, k.basalto);
        Caja(g, "Muro_O1_Norte_2", -31, -9, 9, -19.7f, 6, 9.3f, k.basalto);
        Caja(g, "Muro_O1_Norte_Bajo", -35, -9, 9, -31, -0.3f, 9.3f, k.basalto);
        Caja(g, "Dintel_O1_Norte", -35, 4, 9, -31, 6, 9.3f, k.basalto);
        Caja(g, "Techo_O1", -36.3f, 6, -7.3f, -19.7f, 6.3f, 9.3f, k.techo);
        var e1 = CrearAnclaEnEscena(k, g, "Ancla_O_Ensenar_A", new Vector3(-32f, 0f, -0.15f), Quaternion.Euler(0f, 90f, 0f), FiltroDefinicion.Canal.Cuerpo, 3f, 0);
        var e2 = CrearAnclaEnEscena(k, g, "Ancla_O_Ensenar_B", new Vector3(-32f, 0f, 2.15f), Quaternion.Euler(0f, 90f, 0f), FiltroDefinicion.Canal.Cuerpo, 3f, 2);
        Puente(k, g, "Puente_O_Ensenar", new Vector3(-26f, 0f, 1f), new Vector3(-28.5f, 0f, 1f), 3.6f, e1, e2);
        RedDeSeguridad(k, g, "O1", UnityEngine.Rect.MinMaxRect(-28.5f, -7f, -20f, 9f), pieO1, Vector3.forward,
            new Vector3(-28.5f, -2.4f, 3.5f), 90f, k.pinturaSol);
        Caja(g, "Red_O1_Pared_Lejos", -28.8f, FondoRed - 0.3f, -7, -28.5f, -0.3f, 9, k.basalto);
        Luz(g, "Luz_O1", new Vector3(-28f, 5.3f, 1f), LuzCalida, 120f, 15f, true);
        Motivo(k, g, "Mural_O1", new Vector3(-35.93f, 0.6f, -4f), 90f, 0.8f, k.pinturaSol);

        // ---- O2 · probar: la puerta tiene una sola ancla, lejos, en el rincón del fondo. Las
        // puertas se distinguen de los puentes: un ancla, y una vez abiertas quedan abiertas
        Caja(g, "Piso_O2", -36, -0.3f, 9.3f, -20, 0, 21, k.piso);
        Caja(g, "Muro_O2_Oeste", -36.3f, -0.3f, 9.3f, -36, 6, 21.3f, k.basalto);
        Caja(g, "Muro_O2_Norte", -36.3f, -0.3f, 21, -19.7f, 6, 21.3f, k.basalto);
        Caja(g, "Muro_O2_Este_Sur", -20, -0.3f, 9.3f, -19.7f, 6, 10.5f, k.basaltoMedio);
        Caja(g, "Muro_O2_Este_Norte", -20, -0.3f, 13.5f, -19.7f, 6, 21.3f, k.basaltoMedio);
        Caja(g, "Dintel_O2_Puerta", -20, 3.5f, 10.5f, -19.7f, 6, 13.5f, k.basaltoMedio);
        Caja(g, "Techo_O2", -36.3f, 6, 9.3f, -19.7f, 6.3f, 21.3f, k.techo);
        var p1 = CrearAnclaEnEscena(k, g, "Ancla_O_Puerta", new Vector3(-31f, 0f, 20.2f), Quaternion.Euler(0f, 135f, 0f), FiltroDefinicion.Canal.Cuerpo, 4f, 3);
        BaseDePuerta(k, p1);
        CompuertaLosa(k, g, "Puerta_O_Ancla", new Vector3(-19.85f, 1.75f, 12f), new Vector3(0.3f, 3.5f, 3f),
            new Vector3(0f, -3.8f, 0f), 1.5f, false, p1);
        Luz(g, "Luz_O2", new Vector3(-28f, 5.3f, 15f), LuzCalida, 110f, 14f, true);

        // ---- O3 · torcer: otro abismo; las anclas cuelgan del techo, sobre la otra orilla
        // la red: la gruta sigue por debajo de la orilla de acá; la escalera sube pegada a la
        // pared sur y sale junto a la puerta de la sala de antes
        var pieO3 = new Vector3(-13.5f, 0f, 9.9f);
        PisoConHueco(k, g, "Piso_O3_Cerca", -19.7f, 9.3f, -12.3f, 15, HuecoDeEscalera(pieO3, Vector3.left));
        RedDeSeguridad(k, g, "O3", UnityEngine.Rect.MinMaxRect(-19.7f, 9.3f, -12.3f, 17.5f), pieO3, Vector3.left,
            new Vector3(-16f, -2.4f, 17.5f), 180f, k.pinturaSol);
        Caja(g, "Red_O3_Pared_Sur", -19.7f, FondoRed - 0.3f, 9, -12.3f, -0.3f, 9.3f, k.basalto);
        // (corrida hacia adentro: la puerta de la sala de antes se hunde justo del otro lado)
        Caja(g, "Red_O3_Pared_Oeste", -19.7f, FondoRed - 0.3f, 9.3f, -19.4f, -0.3f, 15, k.basalto);
        Caja(g, "Red_O3_Pared_Lejos", -19.7f, FondoRed - 0.3f, 17.5f, -12.3f, -0.3f, 17.8f, k.basalto);
        Caja(g, "Piso_O3_Lejos", -19.7f, -0.3f, 17.5f, -12.3f, 0, 27, k.piso);
        Caja(g, "Muro_O3_Sur", -19.7f, -0.3f, 9, -12.3f, 6, 9.3f, k.basalto);
        Caja(g, "Muro_O3_Oeste_Bajo", -20, -9, 15, -19.7f, -0.3f, 17.5f, k.basalto);
        Caja(g, "Muro_O3_Oeste_Norte", -20, -0.3f, 21.3f, -19.7f, 6, 27.3f, k.basalto);
        Caja(g, "Muro_O3_Norte", -20, -0.3f, 27, -12.3f, 6, 27.3f, k.basalto);
        Caja(g, "Techo_O3", -20, 6, 9.3f, -12.3f, 6.3f, 27.3f, k.techo);
        var t1 = CrearAnclaEnEscena(k, g, "Ancla_O_Torcer_A", new Vector3(-16.6f, 6f, 19.3f), Quaternion.Euler(180f, 0f, 0f), FiltroDefinicion.Canal.Cuerpo, 4.5f, 2);
        var t2 = CrearAnclaEnEscena(k, g, "Ancla_O_Torcer_B", new Vector3(-15.4f, 6f, 19.3f), Quaternion.Euler(180f, 0f, 0f), FiltroDefinicion.Canal.Cuerpo, 4.5f, 4);
        Puente(k, g, "Puente_O_Torcer", new Vector3(-16f, 0f, 15f), new Vector3(-16f, 0f, 17.5f), 3.4f, t1, t2);
        Luz(g, "Luz_O3", new Vector3(-16f, 5.3f, 12f), LuzCalida, 90f, 12f, true);

        // el sello, detrás de un tabique: desde la otra orilla no se lo ve
        Caja(g, "Tabique_O3", -19.7f, -0.3f, 21.5f, -14.3f, 6, 21.8f, k.basaltoMedio);
        var sello = CrearAnclaEnEscena(k, g, "Sello_Oeste", new Vector3(-18.6f, 0f, 24.5f), Quaternion.Euler(0f, 90f, 0f), FiltroDefinicion.Canal.Cuerpo, 0f, 5);
        BaseDePuerta(k, sello);
        sello.permanente = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sello);
        refs.atajoOeste = CompuertaLosa(k, g, "Atajo_Oeste", new Vector3(-12.15f, 2f, 24.75f), new Vector3(0.3f, 4f, 3.5f),
            new Vector3(0f, -4.4f, 0f), 2.5f, false, sello);
        Luz(g, "Luz_Sello_Oeste", new Vector3(-16f, 5.3f, 24.5f), LuzCalida, 70f, 10f, false);
        // arriba del sello, un óculo: se vuelve a ver el eclipse (la meta) y su haz lleva al sello
        OculoConHaz(k, g, "Oculo_Sello_Oeste", new Vector3(-17.4f, 5.97f, 24.5f), 1.8f, 0f);
        ParedInteriorDelAla(k, g, -1f, -9f);
        MuralesAlaOeste(k, g);
        DecorarAlaOeste(k, g, sello);
        return sello;
    }

    // ================================================================== ala este: HUECO

    static ReceptorDeLuz ConstruirAlaEste(Kit k, Transform g, Referencias refs)
    {
        // pasillo, con el filtro
        Caja(g, "Piso_Pasillo_E", 12.3f, -0.3f, -1, 20, 0, 3, k.piso);
        Caja(g, "Muro_Pasillo_E_Sur", 12.3f, -0.3f, -1.3f, 20, 4.5f, -1, k.basalto);
        Caja(g, "Muro_Pasillo_E_Norte", 12.3f, -0.3f, 3, 20, 4.5f, 3.3f, k.basalto);
        Caja(g, "Techo_Pasillo_E", 12.3f, 4.5f, -1.3f, 20, 4.8f, 3.3f, k.techo);
        // (como en el ala sol: el filtro está en la sala, a la vista del muro de rejas)
        ColocarRecogible(k, g, "Recogible_Hueco", k.hueco, new Vector3(23f, 0f, 6.5f));
        MuralesDelFiltro(k, g, 1f);
        Luz(g, "Luz_Pasillo_E", new Vector3(16f, 3.8f, 1f), LuzFria, 50f, 8f, false);

        // ---- E1 · enseñar: un muro de rejas de lado a lado
        Caja(g, "Piso_E1", 20, -0.3f, -7, 36, 0, 9, k.piso);
        Caja(g, "Muro_E1_Oeste_Sur", 19.7f, -0.3f, -7.3f, 20, 6, -1, k.basalto);
        Caja(g, "Muro_E1_Oeste_Norte", 19.7f, -0.3f, 3, 20, 6, 9.3f, k.basalto);
        Caja(g, "Dintel_E1_Oeste", 19.7f, 4.5f, -1, 20, 6, 3, k.basalto);
        Caja(g, "Muro_E1_Sur", 19.7f, -0.3f, -7.3f, 36.3f, 6, -7, k.basalto);
        Caja(g, "Muro_E1_Este", 36, -0.3f, -7, 36.3f, 6, 9.3f, k.basalto);
        Caja(g, "Muro_E1_Norte_1", 19.7f, -0.3f, 9, 31, 6, 9.3f, k.basalto);
        Caja(g, "Muro_E1_Norte_2", 35, -0.3f, 9, 36.3f, 6, 9.3f, k.basalto);
        Caja(g, "Dintel_E1_Norte", 31, 4, 9, 35, 6, 9.3f, k.basalto);
        Caja(g, "Techo_E1", 19.7f, 6, -7.3f, 36.3f, 6.3f, 9.3f, k.techo);
        Caja(g, "Muro_Rejas_E1_Sur", 27.6f, -0.3f, -7, 28.4f, 6, -5, k.basaltoMedio);
        Caja(g, "Muro_Rejas_E1_Norte", 27.6f, -0.3f, 7, 28.4f, 6, 9, k.basaltoMedio);
        Caja(g, "Muro_Rejas_E1_Arriba", 27.6f, 4.6f, -5, 28.4f, 6, 7, k.basaltoMedio);
        Reja(k, g, "Reja_E_Ensenar_A", new Vector3(28f, 0f, -2f), 90f);
        Reja(k, g, "Reja_E_Ensenar_B", new Vector3(28f, 0f, 4f), 90f);
        Luz(g, "Luz_E1", new Vector3(24f, 5.3f, 1f), LuzFria, 110f, 14f, true);
        Luz(g, "Luz_E1_Fondo", new Vector3(32f, 5.3f, 1f), LuzFria, 70f, 10f, false);
        Motivo(k, g, "Mural_E1", new Vector3(35.93f, 0.6f, -4f), -90f, 0.8f, k.pinturaLuna);

        // ---- E2 · probar: la sala no tiene salida. El camino es la trampilla del piso,
        // que da a una galería 3,5 m más abajo; ahí otra reja tapa el túnel
        Caja(g, "Piso_E2_Sur", 20, -0.3f, 9.3f, 36, 0, 15.4f, k.piso);
        Caja(g, "Piso_E2_Norte", 20, -0.3f, 20, 36, 0, 21, k.piso);
        Caja(g, "Piso_E2_Este", 27, -0.3f, 15.4f, 36, 0, 20, k.piso);
        Caja(g, "Piso_E2_Oeste", 20, -0.3f, 15.4f, 21, 0, 20, k.piso);
        Caja(g, "Muro_E2_Oeste", 19.7f, -0.3f, 9.3f, 20, 6, 21.3f, k.basaltoMedio);
        Caja(g, "Muro_E2_Oeste_Bajo_Sur", 19.7f, -3.8f, 9.3f, 20, -0.3f, 16, k.basaltoMedio);
        Caja(g, "Muro_E2_Oeste_Bajo_Norte", 19.7f, -3.8f, 19, 20, -0.3f, 21.3f, k.basaltoMedio);
        Caja(g, "Dintel_Tunel_E", 19.7f, -1, 16, 20, -0.3f, 19, k.basaltoMedio);
        Caja(g, "Muro_E2_Norte", 19.7f, -3.8f, 21, 36.3f, 6, 21.3f, k.basalto);
        Caja(g, "Muro_E2_Este", 36, -0.3f, 9.3f, 36.3f, 6, 21.3f, k.basalto);
        Caja(g, "Techo_E2", 19.7f, 6, 9.3f, 36.3f, 6.3f, 21.3f, k.techo);
        Trampilla(k, g, "Trampilla_E", new Vector3(24f, -0.45f, 15.4f));
        Luz(g, "Luz_E2", new Vector3(28f, 5.3f, 15f), LuzFria, 90f, 14f, true);

        Caja(g, "Piso_Galeria_E", 20, -3.8f, 12, 28, -3.5f, 21, k.piso);
        Caja(g, "Muro_Galeria_E_Sur", 20, -3.8f, 11.7f, 28.3f, -0.3f, 12, k.basalto);
        Caja(g, "Muro_Galeria_E_Este", 28, -3.8f, 12, 28.3f, -0.3f, 21, k.basalto);
        Reja(k, g, "Reja_E_Galeria", new Vector3(21.5f, -3.5f, 17.5f), 90f, new Vector3(1f, 0.7f, 1f));
        Caja(g, "Relleno_Galeria_E_Sur", 21.2f, -3.8f, 12, 21.8f, -0.3f, 14.5f, k.basalto);
        Caja(g, "Relleno_Galeria_E_Norte", 21.2f, -3.8f, 20.5f, 21.8f, -0.3f, 21, k.basalto);
        Luz(g, "Luz_Galeria_E", new Vector3(24.5f, -1.2f, 13.5f), LuzFria, 40f, 8f, false);

        // ---- E3 · torcer: una trinchera, otra reja, y una rampa que sube a la plataforma;
        // arriba de la rampa, acostada como un techo, la escotilla: hay que mirar para arriba
        Caja(g, "Piso_Trinchera_E", 12.3f, -3.8f, 9.3f, 19.7f, -3.5f, 21, k.piso);
        Caja(g, "Piso_E3_Alto", 12.3f, -0.3f, 21, 19.7f, 0, 27, k.piso);
        Caja(g, "Frente_E3_Alto", 12.3f, -3.5f, 20.7f, 19.7f, -0.3f, 21, k.basalto);
        Caja(g, "Muro_E3_Sur", 12.3f, -3.8f, 9, 19.7f, 6, 9.3f, k.basalto);
        Caja(g, "Muro_E3_Norte", 12.3f, -0.3f, 27, 20, 6, 27.3f, k.basalto);
        Caja(g, "Muro_E3_Este_Norte", 19.7f, -0.3f, 21.3f, 20, 6, 27.3f, k.basalto);
        Caja(g, "Techo_E3", 12.3f, 6, 9.3f, 20, 6.3f, 27.3f, k.techo);
        Reja(k, g, "Reja_E_Trinchera", new Vector3(18.05f, -3.5f, 14f), 0f, new Vector3(0.55f, 1f, 1f));
        Rampa(g, "Rampa_E3", new Vector3(14.5f, -3.5f, 13f), new Vector3(14.5f, 0f, 21f), 3.8f, k.piso);
        Trampilla(k, g, "Escotilla_E", new Vector3(15.5f, 0.45f, 16.4f));
        Luz(g, "Luz_Trinchera_E", new Vector3(16f, 2.5f, 12f), LuzFria, 90f, 13f, true);

        // el sello, detrás de un tabique: una placa de materia hueca que queda disuelta
        Caja(g, "Tabique_E3", 12.3f, -0.3f, 23.5f, 17.5f, 6, 23.8f, k.basaltoMedio);
        // detrás de la placa, lunas y estrellas (antes era un tallado ámbar del kit, que no era del ala)
        var tallado = Grupo(g, "Tallado_Sello_Este").gameObject;
        Glifo(tallado.transform, "Luna", Figura.LunaCreciente, new Vector3(14.6f, 0.85f, 26.95f), Vector3.back, 1f, k.tallaLuna);
        Glifo(tallado.transform, "Estrella_A", Figura.Estrella, new Vector3(13.95f, 1.35f, 26.95f), Vector3.back, 0.28f, k.tallaLuna);
        Glifo(tallado.transform, "Estrella_B", Figura.Estrella, new Vector3(15.25f, 0.4f, 26.95f), Vector3.back, 0.22f, k.tallaLuna);
        var sello = Reja(k, g, "Sello_Este", new Vector3(14.6f, 0f, 26.55f), 180f, new Vector3(0.5f, 0.5f, 0.5f));
        sello.permanente = true;
        // no es una reja (parecía que se pasaba por ahí): es una placa redonda de materia hueca,
        // apoyada contra la pared sobre la luna tallada. Se usa la mecánica de la reja, sin sus barras
        foreach (var r in sello.GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
        var placa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        placa.name = "Placa_Sello";
        Object.DestroyImmediate(placa.GetComponent<Collider>());
        placa.transform.SetParent(sello.transform, false);
        placa.transform.SetPositionAndRotation(new Vector3(14.6f, 0.9f, 26.78f), Quaternion.Euler(90f, 0f, 0f));
        // 1,7 m de diámetro en el mundo (el prefab de la reja está a media escala)
        Vector3 escalaSello = sello.transform.lossyScale;
        placa.transform.localScale = new Vector3(1.7f / escalaSello.x, 0.05f / escalaSello.y, 1.7f / escalaSello.z);
        placa.GetComponent<Renderer>().sharedMaterial = k.rejaSello;
        sello.renderers = new[] { placa.GetComponent<Renderer>() };
        sello.sellos = sello.renderers;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sello);
        // el tallado de atrás de la placa se enciende cuando la placa queda disuelta
        var testigo = tallado.AddComponent<TestigoDeSello>();
        testigo.sello = sello;
        testigo.renderers = tallado.GetComponentsInChildren<Renderer>();
        testigo.colorEncendido = ColorLuna;
        testigo.emisionApagado = 0.04f;
        // el atajo arranca detrás del tabique: si no, al abrirse dejaba ver por una rendija la sala de antes
        refs.atajoEste = CompuertaLosa(k, g, "Atajo_Este", new Vector3(12.15f, 2f, 25.15f), new Vector3(0.3f, 4f, 2.7f),
            new Vector3(0f, -4.4f, 0f), 2.5f, false, sello);
        Luz(g, "Luz_Sello_Este", new Vector3(16f, 5f, 25f), LuzFria, 60f, 9f, false);
        OculoConHaz(k, g, "Oculo_Sello_Este", new Vector3(14.6f, 5.97f, 25.3f), 1.8f, 0f);
        ParedInteriorDelAla(k, g, 1f, -3.8f, 23.8f);
        MuralesAlaEste(k, g);
        DecorarAlaEste(k, g, sello);
        return sello;
    }

    // ================================================================== el Cruce: los dos filtros

    static void ConstruirCruce(Kit k, Transform g)
    {
        Caja(g, "Muro_Cruce_Izq", -6.3f, -9, 27.3f, -6, 6, 68.5f, k.basaltoMedio);
        Caja(g, "Muro_Cruce_Der", 6, -9, 27.3f, 6.3f, 6, 68.5f, k.basalto);
        Caja(g, "Techo_Cruce", -6.3f, 6, 27.3f, 6.3f, 6.3f, 68.5f, k.techo);

        // ---- N1 · una de las anclas del puente está detrás de una reja: HUECO para
        // destaparla, cambiar a CUERPO y encenderla antes de que la reja se cierre
        // las redes de los dos abismos: grutas debajo de cada orilla de acá, con su escalera
        var pieN1 = new Vector3(5.4f, 0f, 32.4f);
        var pieN2 = new Vector3(-5.4f, 0f, 39.4f);
        PisoConHueco(k, g, "Piso_N1_Cerca", -6, 27.3f, 6, 33, HuecoDeEscalera(pieN1, Vector3.back));
        PisoConHueco(k, g, "Piso_N1_Lejos", -6, 35.5f, 6, 45, HuecoDeEscalera(pieN2, Vector3.forward));
        var vista = CrearAnclaEnEscena(k, g, "Ancla_N_Vista", new Vector3(-3.2f, 0f, 38.5f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 6f, 0);
        var oculta = CrearAnclaEnEscena(k, g, "Ancla_N_Oculta", new Vector3(3.2f, 0f, 38.5f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 6f, 2);
        Reja(k, g, "Reja_N_Tapa", new Vector3(3.2f, 0f, 36.8f), 0f, new Vector3(0.9f, 1f, 1f));
        Puente(k, g, "Puente_N_Reja", new Vector3(0f, 0f, 33f), new Vector3(0f, 0f, 35.5f), 3.6f, vista, oculta);
        RedDeSeguridad(k, g, "N1", UnityEngine.Rect.MinMaxRect(-6f, 27.9f, 6f, 35.5f), pieN1, Vector3.back,
            new Vector3(-2.5f, -2.4f, 35.5f), 180f, k.pinturaLuna);
        Caja(g, "Red_N1_Pared_Sur", -6, FondoRed - 0.3f, 27.6f, 6, -0.3f, 27.9f, k.basalto);
        Caja(g, "Red_N_Pared_Medio", -6, FondoRed - 0.3f, 35.5f, 6, -0.3f, 35.8f, k.basalto);
        Luz(g, "Luz_N1", new Vector3(0f, 5.3f, 30.5f), LuzCalida, 110f, 14f, true);
        Luz(g, "Luz_N1_Fondo", new Vector3(0f, 5.3f, 39.5f), LuzFria, 80f, 11f, false);

        // ---- N2 · el puente se sostiene con anclas que quedan a la espalda y termina contra
        // una reja: parado arriba, cambiar a HUECO y pasar antes de que se apague
        Caja(g, "Piso_N2_Lejos", -6, -0.3f, 50.5f, 6, 0, 68.5f, k.piso);
        // miran al sur: se encienden antes de subir al puente y después quedan a la espalda
        var b1 = CrearAnclaEnEscena(k, g, "Ancla_N_Borde_A", new Vector3(-1.3f, 0f, 43.2f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 8f, 1);
        var b2 = CrearAnclaEnEscena(k, g, "Ancla_N_Borde_B", new Vector3(1.3f, 0f, 43.2f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 8f, 3);
        Puente(k, g, "Puente_N_Borde", new Vector3(0f, 0f, 45f), new Vector3(0f, 0f, 50.5f), 3.6f, b1, b2);
        RedDeSeguridad(k, g, "N2", UnityEngine.Rect.MinMaxRect(-6f, 35.8f, 6f, 50.5f), pieN2, Vector3.forward,
            new Vector3(2.5f, -2.4f, 50.5f), 180f, k.pinturaSol);
        Caja(g, "Red_N2_Pared_Lejos", -6, FondoRed - 0.3f, 50.5f, 6, -0.3f, 50.8f, k.basalto);
        Reja(k, g, "Reja_N_Borde_A", new Vector3(-3f, 0f, 51.2f));
        Reja(k, g, "Reja_N_Borde_B", new Vector3(3f, 0f, 51.2f));
        Caja(g, "Muro_Rejas_N_Arriba", -6, 4.6f, 50.75f, 6, 6, 51.65f, k.basalto);
        Oculo(k, g, "Oculo_Cruce", new Vector3(0f, 5.97f, 47.8f), 3.2f, 120f, 10f);
        Luz(g, "Luz_N2", new Vector3(0f, 5.3f, 44f), LuzCalida, 90f, 12f, false);

        // ---- N3 · la antesala de la Cresta
        Luz(g, "Luz_N3", new Vector3(0f, 5.3f, 61f), LuzFria, 70f, 11f, false);
    }

    // ================================================================== piezas

    /// <summary>
    /// Un puente de luz sobre el hueco que va de 'desde' a 'hasta' (en el piso, y = 0),
    /// en cualquier dirección horizontal, sostenido por las anclas dadas.
    /// </summary>
    static PuenteLuz Puente(Kit k, Transform g, string nombre, Vector3 desde, Vector3 hasta, float ancho, params Ancla[] anclas)
    {
        const float solape = 0.25f;
        Vector3 dir = hasta - desde;
        dir.y = 0f;
        float largo = dir.magnitude;
        dir /= largo;
        var go = Instancia(k.prefabPuente, g, nombre, desde - dir * solape, Quaternion.LookRotation(dir),
            new Vector3(ancho / 2.2f, 1f, (largo + solape * 2f) / 8f));
        var puente = go.GetComponent<PuenteLuz>();
        puente.anclas = new System.Collections.Generic.List<ReceptorDeLuz>(anclas);
        PrefabUtility.RecordPrefabInstancePropertyModifications(puente);
        return puente;
    }

    /// <summary>Una reja parada, con el pie en 'pie'. rotY 90 = de lado a lado en z.</summary>
    static MateriaHueca Reja(Kit k, Transform g, string nombre, Vector3 pie, float rotY = 0f, Vector3? escala = null)
    {
        return Instancia(k.prefabReja, g, nombre, pie, Quaternion.Euler(0f, rotY, 0f), escala).GetComponent<MateriaHueca>();
    }

    /// <summary>
    /// Una reja acostada (piso o techo): 6 m en x centrada en 'borde', 4,6 m hacia +z desde
    /// 'borde', y 0,9 m de espesor centrado en borde.y.
    /// </summary>
    static MateriaHueca Trampilla(Kit k, Transform g, string nombre, Vector3 borde)
    {
        return Instancia(k.prefabReja, g, nombre, borde, Quaternion.Euler(90f, 0f, 0f)).GetComponent<MateriaHueca>();
    }

    /// <summary>Una losa inclinada para caminar de 'desde' a 'hasta' (las caras de arriba).</summary>
    static GameObject Rampa(Transform g, string nombre, Vector3 desde, Vector3 hasta, float ancho, Material m)
    {
        const float grosor = 0.4f;
        var giro = Quaternion.LookRotation(hasta - desde);
        var rampa = Bloque(g, nombre, (desde + hasta) / 2f - giro * Vector3.up * (grosor / 2f),
            new Vector3(ancho, grosor, Vector3.Distance(desde, hasta) + 0.3f), m);
        rampa.transform.rotation = giro;
        return rampa;
    }

    /// <summary>
    /// Una losa que se hunde (o sube) para abrir un paso cuando todos sus receptores
    /// están activos. 'sostenida': se vuelve a cerrar cuando se apagan.
    /// </summary>
    static Compuerta CompuertaLosa(Kit k, Transform g, string nombre, Vector3 centro, Vector3 tamanio, Vector3 desplazamiento,
                                   float duracion, bool sostenida, params ReceptorDeLuz[] receptores)
    {
        var raiz = new GameObject(nombre);
        raiz.transform.SetParent(g, false);
        raiz.transform.position = centro;
        Bloque(raiz.transform, "Losa", centro, tamanio, k.basaltoMedio, false);
        var sonido = raiz.AddComponent<AudioSource>();
        ConfigurarAudio(sonido, k.audio.compuerta, sostenida ? 0.6f : 1f, false, true);
        sonido.maxDistance = 40f;
        var compuerta = raiz.AddComponent<Compuerta>();
        compuerta.receptores.AddRange(receptores);
        compuerta.desplazamiento = desplazamiento;
        compuerta.duracion = duracion;
        compuerta.sostenida = sostenida;
        compuerta.capaJugador = LayerMask.GetMask(CapaJugador);
        compuerta.sonido = sonido;
        return compuerta;
    }
}
