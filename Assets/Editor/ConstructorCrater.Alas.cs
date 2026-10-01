using UnityEditor;
using UnityEngine;

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
/// y máxima), prefabs (anclas, puentes, rejas) y luces. Donde hay un abismo, las
/// paredes bajan hasta y = -9 (caerse ahí devuelve al último suelo firme). Las rejas
/// cortan la luz (ver LinternaController): lo que está detrás no se enciende hasta
/// disolverlas. Los sellos son receptores 'permanentes'.
/// </summary>
public static partial class ConstructorCrater
{
    // ================================================================== la rotonda

    static void ConstruirRotonda(Kit k, Transform g, Referencias refs)
    {
        Caja(g, "Piso_Rotonda", -12, -0.3f, -3, 12, 0, 27, k.piso);

        // sur: la boca del Umbral
        Caja(g, "Muro_Rotonda_Sur_Izq", -12.3f, -0.3f, -3.3f, -6, 7, -3, k.basalto);
        Caja(g, "Muro_Rotonda_Sur_Der", 6, -0.3f, -3.3f, 12.3f, 7, -3, k.basalto);
        Caja(g, "Dintel_Umbral", -6, 4, -3.3f, 6, 7, -3, k.basalto);

        // oeste y este: el pasillo a cada ala (z -1 … 3) y el atajo de vuelta (z 23 … 26,5)
        foreach (float lado in new[] { -1f, 1f })
        {
            string n = lado < 0f ? "O" : "E";
            float x0 = lado < 0f ? -12.3f : 12f, x1 = x0 + 0.3f;
            var m = lado < 0f ? k.basaltoMedio : k.basalto;
            Caja(g, $"Muro_Rotonda_{n}_1", x0, -0.3f, -3.3f, x1, 7, -1, m);
            Caja(g, $"Muro_Rotonda_{n}_2", x0, -0.3f, 3, x1, 7, 23, m);
            Caja(g, $"Muro_Rotonda_{n}_3", x0, -0.3f, 26.5f, x1, 7, 27.3f, m);
            Caja(g, $"Dintel_Rotonda_{n}_Ala", x0, 4.5f, -1, x1, 7, 3, m);
            Caja(g, $"Dintel_Rotonda_{n}_Atajo", x0, 4, 23, x1, 7, 26.5f, m);
        }
        // del otro lado de estas paredes hay pozos: bajan hasta el fondo
        Caja(g, "Muro_Rotonda_O_Bajo", -12.3f, -9, 15, -12, -0.3f, 17.5f, k.basaltoMedio);
        Caja(g, "Muro_Rotonda_E_Bajo", 12, -3.8f, 9.3f, 12.3f, -0.3f, 21, k.basalto);

        // norte: la puerta de los sellos
        Caja(g, "Muro_Rotonda_Norte_Izq", -12.3f, -0.3f, 27, -2, 7, 27.3f, k.basalto);
        Caja(g, "Muro_Rotonda_Norte_Der", 2, -0.3f, 27, 12.3f, 7, 27.3f, k.basalto);
        Caja(g, "Dintel_Sellos", -2, 4, 27, 2, 7, 27.3f, k.basalto);

        Caja(g, "Techo_Rotonda", -12.3f, 7, -3.3f, 12.3f, 7.3f, 27.3f, k.techo);
        Oculo(k, g, "Oculo_Rotonda", new Vector3(0f, 6.97f, 12f), 4f, 150f, 12f);
        ConstruirAnilloDelMapa(k, g, new Vector3(0f, 0.12f, 12f), 0.4f);

        Luz(g, "Luz_Rotonda", new Vector3(0f, 5.8f, 5f), LuzCalida, 140f, 16f, true);
        Luz(g, "Luz_Rotonda_Norte", new Vector3(0f, 5.8f, 21f), LuzCalida, 100f, 14f, false);
    }

    /// <summary>
    /// La puerta del norte, los hilos de tallados que llegan a ella (se encienden de a
    /// uno con cada sello) y los faros de los atajos. Va después de las alas: necesita los sellos.
    /// </summary>
    static void ConstruirPuertaDeLosSellos(Kit k, Transform g, Referencias refs, ReceptorDeLuz selloOeste, ReceptorDeLuz selloEste)
    {
        refs.puertaSellos = CompuertaLosa(k, g, "Puerta_Sellos", new Vector3(0f, 2f, 27.15f), new Vector3(4f, 4f, 0.3f),
            new Vector3(0f, -4.4f, 0f), 3f, false, selloOeste, selloEste);

        // la abre la cinemática del segundo sello, a la vista (CinematicaDeSello)
        refs.puertaSellos.abrirSoloPorOrden = true;
        refs.selloOeste = selloOeste;
        refs.selloEste = selloEste;

        // los hilos de soles y lunas que llegan a la puerta (ConstructorCrater.Tallados.cs)
        ConstruirHilosDeLosSellos(k, g, refs, selloOeste, selloEste);

        // sobre cada atajo, del lado de la rotonda, un tallado con luz: se ve de lejos cuál se abrió
        void Faro(string nombre, float lado, ReceptorDeLuz sello)
        {
            var tallado = Motivo(k, g, nombre, new Vector3(lado * 11.93f, 4.5f, 24.75f), lado < 0f ? 90f : -90f, 0.6f, k.ambar);
            var testigo = tallado.AddComponent<TestigoDeSello>();
            testigo.sello = sello;
            testigo.renderers = tallado.GetComponentsInChildren<Renderer>();
            testigo.luz = Luz(g, nombre + "_Luz", new Vector3(lado * 10.8f, 5f, 24.75f), LuzCalida, 0f, 9f, false);
            testigo.intensidadLuz = 45f;
        }
        Faro("Faro_Atajo_Oeste", -1f, selloOeste);
        Faro("Faro_Atajo_Este", 1f, selloEste);
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
        ColocarRecogible(k, g, "Recogible_Cuerpo", k.cuerpo, new Vector3(-16.5f, 0f, 1f));
        Luz(g, "Luz_Pasillo_O", new Vector3(-16f, 3.8f, 1f), LuzCalida, 50f, 8f, false);

        // ---- O1 · enseñar: un abismo y dos anclas juntas del otro lado (entran juntas en el cono)
        Caja(g, "Piso_O1_Este", -26, -0.3f, -7, -20, 0, 9, k.piso);
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
        Luz(g, "Luz_O1", new Vector3(-28f, 5.3f, 1f), LuzCalida, 120f, 15f, true);
        Motivo(k, g, "Mural_O1", new Vector3(-35.93f, 0.6f, -4f), 90f, 0.8f, k.ambar);

        // ---- O2 · probar: la puerta se sostiene abierta mientras las dos anclas estén encendidas.
        // Están lejos una de otra: se enciende una, se barre a la otra y se corre por la retención
        Caja(g, "Piso_O2", -36, -0.3f, 9.3f, -20, 0, 21, k.piso);
        Caja(g, "Muro_O2_Oeste", -36.3f, -0.3f, 9.3f, -36, 6, 21.3f, k.basalto);
        Caja(g, "Muro_O2_Norte", -36.3f, -0.3f, 21, -19.7f, 6, 21.3f, k.basalto);
        Caja(g, "Muro_O2_Este_Sur", -20, -0.3f, 9.3f, -19.7f, 6, 10.5f, k.basaltoMedio);
        Caja(g, "Muro_O2_Este_Norte", -20, -0.3f, 13.5f, -19.7f, 6, 21.3f, k.basaltoMedio);
        Caja(g, "Dintel_O2_Puerta", -20, 3.5f, 10.5f, -19.7f, 6, 13.5f, k.basaltoMedio);
        Caja(g, "Techo_O2", -36.3f, 6, 9.3f, -19.7f, 6.3f, 21.3f, k.techo);
        var p1 = CrearAnclaEnEscena(k, g, "Ancla_O_Puerta_A", new Vector3(-27f, 0f, 10.1f), Quaternion.identity, FiltroDefinicion.Canal.Cuerpo, 4f, 1);
        var p2 = CrearAnclaEnEscena(k, g, "Ancla_O_Puerta_B", new Vector3(-31f, 0f, 20.2f), Quaternion.Euler(0f, 135f, 0f), FiltroDefinicion.Canal.Cuerpo, 4f, 3);
        CompuertaLosa(k, g, "Puerta_O_Dos_Anclas", new Vector3(-19.85f, 1.75f, 12f), new Vector3(0.3f, 3.5f, 3f),
            new Vector3(0f, -3.8f, 0f), 0.7f, true, p1, p2);
        Luz(g, "Luz_O2", new Vector3(-28f, 5.3f, 15f), LuzCalida, 110f, 14f, true);

        // ---- O3 · torcer: otro abismo; las anclas cuelgan del techo, sobre la otra orilla
        Caja(g, "Piso_O3_Cerca", -19.7f, -0.3f, 9.3f, -12.3f, 0, 15, k.piso);
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
        sello.permanente = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sello);
        refs.atajoOeste = CompuertaLosa(k, g, "Atajo_Oeste", new Vector3(-12.15f, 2f, 24.75f), new Vector3(0.3f, 4f, 3.5f),
            new Vector3(0f, -4.4f, 0f), 2.5f, false, sello);
        Luz(g, "Luz_Sello_Oeste", new Vector3(-16f, 5.3f, 24.5f), LuzCalida, 70f, 10f, false);
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
        ColocarRecogible(k, g, "Recogible_Hueco", k.hueco, new Vector3(16.5f, 0f, 1f));
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
        Motivo(k, g, "Mural_E1", new Vector3(35.93f, 0.6f, -4f), -90f, 0.8f, k.ambar);

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
        var tallado = Motivo(k, g, "Tallado_Sello_Este", new Vector3(14.6f, 0.4f, 26.95f), 180f, 0.7f, k.ambar);
        var sello = Reja(k, g, "Sello_Este", new Vector3(14.6f, 0f, 26.55f), 180f, new Vector3(0.5f, 0.5f, 0.5f));
        sello.permanente = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(sello);
        // el tallado de atrás de la placa se enciende cuando la placa queda disuelta
        var testigo = tallado.AddComponent<TestigoDeSello>();
        testigo.sello = sello;
        testigo.renderers = tallado.GetComponentsInChildren<Renderer>();
        refs.atajoEste = CompuertaLosa(k, g, "Atajo_Este", new Vector3(12.15f, 2f, 24.75f), new Vector3(0.3f, 4f, 3.5f),
            new Vector3(0f, -4.4f, 0f), 2.5f, false, sello);
        Luz(g, "Luz_Sello_Este", new Vector3(16f, 5f, 25f), LuzFria, 60f, 9f, false);
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
        Caja(g, "Piso_N1_Cerca", -6, -0.3f, 27.3f, 6, 0, 33, k.piso);
        Caja(g, "Piso_N1_Lejos", -6, -0.3f, 35.5f, 6, 0, 45, k.piso);
        var vista = CrearAnclaEnEscena(k, g, "Ancla_N_Vista", new Vector3(-3.2f, 0f, 38.5f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 6f, 0);
        var oculta = CrearAnclaEnEscena(k, g, "Ancla_N_Oculta", new Vector3(3.2f, 0f, 38.5f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 6f, 2);
        Reja(k, g, "Reja_N_Tapa", new Vector3(3.2f, 0f, 36.8f), 0f, new Vector3(0.9f, 1f, 1f));
        Puente(k, g, "Puente_N_Reja", new Vector3(0f, 0f, 33f), new Vector3(0f, 0f, 35.5f), 3.6f, vista, oculta);
        Luz(g, "Luz_N1", new Vector3(0f, 5.3f, 30.5f), LuzCalida, 110f, 14f, true);
        Luz(g, "Luz_N1_Fondo", new Vector3(0f, 5.3f, 39.5f), LuzFria, 80f, 11f, false);

        // ---- N2 · el puente se sostiene con anclas que quedan a la espalda y termina contra
        // una reja: parado arriba, cambiar a HUECO y pasar antes de que se apague
        Caja(g, "Piso_N2_Lejos", -6, -0.3f, 50.5f, 6, 0, 68.5f, k.piso);
        // miran al sur: se encienden antes de subir al puente y después quedan a la espalda
        var b1 = CrearAnclaEnEscena(k, g, "Ancla_N_Borde_A", new Vector3(-1.3f, 0f, 43.2f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 8f, 1);
        var b2 = CrearAnclaEnEscena(k, g, "Ancla_N_Borde_B", new Vector3(1.3f, 0f, 43.2f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 8f, 3);
        Puente(k, g, "Puente_N_Borde", new Vector3(0f, 0f, 45f), new Vector3(0f, 0f, 50.5f), 3.6f, b1, b2);
        Reja(k, g, "Reja_N_Borde_A", new Vector3(-3f, 0f, 51.2f));
        Reja(k, g, "Reja_N_Borde_B", new Vector3(3f, 0f, 51.2f));
        Caja(g, "Muro_Rejas_N_Arriba", -6, 4.6f, 50.75f, 6, 6, 51.65f, k.basalto);
        Oculo(k, g, "Oculo_Cruce", new Vector3(0f, 5.97f, 47.8f), 3.2f, 120f, 10f);
        Luz(g, "Luz_N2", new Vector3(0f, 5.3f, 44f), LuzCalida, 90f, 12f, false);

        // ---- N3 · la antesala de la Cresta
        Luz(g, "Luz_N3", new Vector3(0f, 5.3f, 61f), LuzFria, 70f, 11f, false);
        Motivo(k, g, "Mural_Antesala", new Vector3(5.93f, 0.6f, 61f), -90f, 0.8f, k.ambar);
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
