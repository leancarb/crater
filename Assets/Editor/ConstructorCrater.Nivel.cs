using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// El recorrido del vertical slice, en coordenadas de mundo (1 unidad = 1 metro,
/// el jugador avanza hacia +Z). El piso de cada sala está en y = 0 salvo la Explanada.
///
///   01 Explanada   z -41 … -34   inicio al aire libre, en el borde del cráter (y = 4)
///      Rampa       z -34 … -23   baja 4 m hasta el Umbral
///   02 Umbral      z -23 … -3    la linterna y la primera ancla (luz blanca) abren la compuerta
///   03 Campo       z  -3 …  30   filtro CUERPO: tres puentes (enseñar, probar, torcer la mirada)
///   04 Hondonada   z  30 …  68.5 filtro HUECO: rejas, zigzag y la combinación de ambos filtros
///   05 Cresta      z 68.5 … 99   apagar la linterna, adaptarse y cruzar la puerta del eclipse
///   06 Capilla     x = 300       prólogo (el eclipse abre el cráter en el valle) y epílogo de día
///
/// CÓMO FUNCIONA
/// Cada sala se arma con cajas (Caja: esquina mínima y máxima en metros), prefabs
/// (anclas, puentes, rejas) y luces. La capilla está lejos (x = 300) para que nunca se
/// vea desde el cráter: el paso de un lugar al otro es un teletransporte tapado por
/// un fundido. Al final, ConstruirSistemas crea los objetos de lógica y conecta
/// referencias (Asignar) y eventos (UnityEventTools) entre ellos.
/// </summary>
public static partial class ConstructorCrater
{
    static readonly Color ColorNiebla = new Color(0.02f, 0.025f, 0.04f);
    static readonly Color LuzCalida = new Color(1f, 0.72f, 0.48f);
    static readonly Color LuzFria = new Color(0.5f, 0.62f, 1f);

    /// <summary>Lo que los sistemas necesitan conocer del nivel.</summary>
    sealed class Referencias
    {
        public GameObject jugador;
        public Compuerta compuertaUmbral;
        public ZonaJugador zonaCresta, zonaFinal;
        public Transform spawnCapilla, spawnInicio;
        public Light luna, sol;

        // prólogo
        public CieloEclipse cielo;
        public GameObject craterValle, huella;
        public Transform[] bordesCrater;
        public Transform fondoCrater, puertaValle;
        public Renderer[] contornoValle;
        public Renderer destelloValle;
        public ZonaJugador zonaUmbralCapilla, zonaPuertaCrater;

        // final
        public PuertaEclipse puertaEclipse;
        public ZonaJugador zonaCruce;
    }

    static void ConstruirEscena(Kit kit)
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ConfigurarAmbiente();

        var refs = new Referencias();
        var nivel = new GameObject("NIVEL").transform;
        var arte = new GameObject("ARTE").transform;

        ConstruirExplanada(kit, Grupo(nivel, "01_Explanada"), refs);
        ConstruirUmbral(kit, Grupo(nivel, "02_Umbral"), refs);
        ConstruirCampo(kit, Grupo(nivel, "03_Campo"));
        ConstruirHondonada(kit, Grupo(nivel, "04_Hondonada"));
        ConstruirCresta(kit, Grupo(nivel, "05_Cresta"), refs);
        ConstruirCapilla(kit, Grupo(nivel, "06_Capilla"), refs);
        Vestir(kit, arte);
        ConstruirSistemas(kit, refs);

        EditorSceneManager.SaveScene(escena, RutaEscena);
    }

    static void ConfigurarAmbiente()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.16f, 0.17f, 0.22f);
        RenderSettings.ambientEquatorColor = new Color(0.09f, 0.09f, 0.11f);
        RenderSettings.ambientGroundColor = new Color(0.035f, 0.03f, 0.028f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.02f;
        RenderSettings.fogColor = ColorNiebla;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.reflectionIntensity = 0.3f;
    }

    // ================================================================== 01 Explanada

    static void ConstruirExplanada(Kit k, Transform g, Referencias refs)
    {
        Caja(g, "Piso_Plaza", -7, 3.7f, -41, 7, 4, -34, k.piso);
        Caja(g, "Muro_Plaza_Fondo", -7.3f, 3.7f, -41.3f, 7.3f, 7.5f, -41, k.basalto);
        Caja(g, "Muro_Plaza_Izq", -7.3f, 3.7f, -41, -7, 7.5f, -34, k.basaltoMedio);
        Caja(g, "Muro_Plaza_Der", 7, 3.7f, -41, 7.3f, 7.5f, -34, k.basaltoMedio);
        // parapeto bajo: se ve el cráter pero no se puede caer
        Caja(g, "Parapeto_Izq", -7.3f, 3.7f, -34.3f, -2.05f, 5.1f, -34, k.basalto);
        Caja(g, "Parapeto_Der", 2.05f, 3.7f, -34.3f, 7.3f, 5.1f, -34, k.basalto);

        // rampa: de (z -34, y 4) a (z -23, y 0)
        const float largo = 11.9f, grosor = 0.3f;
        float angulo = Mathf.Atan2(4f, 11f);
        var normal = new Vector3(0f, Mathf.Cos(angulo), Mathf.Sin(angulo));
        var rampa = Bloque(g, "Rampa", new Vector3(0f, 2f, -28.5f) - normal * (grosor / 2f),
            new Vector3(3.5f, grosor, largo), k.piso);
        rampa.transform.rotation = Quaternion.Euler(angulo * Mathf.Rad2Deg, 0f, 0f);
        Caja(g, "Muro_Rampa_Izq", -2.05f, -0.3f, -34.3f, -1.75f, 5.8f, -23, k.basalto);
        Caja(g, "Muro_Rampa_Der", 1.75f, -0.3f, -34.3f, 2.05f, 5.8f, -23, k.basalto);

        // el prólogo trae al jugador acá desde la capilla
        var spawn = new GameObject("SpawnInicio").transform;
        spawn.SetParent(g, false);
        spawn.position = new Vector3(0f, 4.02f, -38.8f);
        refs.spawnInicio = spawn;
        refs.jugador = (GameObject)PrefabUtility.InstantiatePrefab(k.prefabJugador);
        refs.jugador.transform.SetPositionAndRotation(spawn.position, Quaternion.identity);

        // la luz del cielo sobre el cráter: sólo alcanza la explanada y los óculos
        var lunaGO = new GameObject("Luz_Crater");
        lunaGO.transform.SetParent(g, false);
        lunaGO.transform.rotation = Quaternion.Euler(38f, 160f, 0f);
        refs.luna = lunaGO.AddComponent<Light>();
        refs.luna.type = LightType.Directional;
        refs.luna.color = new Color(0.55f, 0.63f, 0.9f);
        refs.luna.intensity = 1.2f;
        refs.luna.shadows = LightShadows.Soft;
        RenderSettings.sun = refs.luna;

        Luz(g, "Luz_Plaza", new Vector3(0f, 6.2f, -37.5f), LuzCalida, 70f, 11f, false);
    }

    // ================================================================== 02 Umbral

    static void ConstruirUmbral(Kit k, Transform g, Referencias refs)
    {
        Caja(g, "Piso_Umbral", -6, -0.3f, -23, 6, 0, -3, k.piso);
        Caja(g, "Muro_Umbral_Izq", -6.3f, -0.3f, -23, -6, 4, -3, k.basaltoMedio);
        Caja(g, "Muro_Umbral_Der", 6, -0.3f, -23, 6.3f, 4, -3, k.basalto);
        Caja(g, "Muro_Entrada_Izq", -6.3f, -0.3f, -23.3f, -2.05f, 4, -23, k.basalto);
        Caja(g, "Muro_Entrada_Der", 2.05f, -0.3f, -23.3f, 6.3f, 4, -23, k.basalto);
        Caja(g, "Dintel_Rampa", -2.05f, 4, -23.3f, 2.05f, 5.8f, -23, k.basalto);
        Caja(g, "Techo_Umbral", -6.3f, 4, -23.3f, 6.3f, 4.3f, -3, k.techo);

        Instancia(k.prefabLinterna, g, "Recogible_Linterna", new Vector3(1.2f, 0f, -15.5f), Quaternion.identity);

        var ancla = CrearAnclaEnEscena(k, g, "Ancla_Umbral", new Vector3(-5.1f, 0f, -10f), Quaternion.Euler(0f, 90f, 0f),
            FiltroDefinicion.Canal.Ninguno, 2f, 0);

        // compuerta: una losa que se hunde en el piso
        var compuertaGO = new GameObject("Compuerta_Umbral");
        compuertaGO.transform.SetParent(g, false);
        compuertaGO.transform.position = new Vector3(0f, 0f, -3.1f);
        var losa = Bloque(compuertaGO.transform, "Losa", new Vector3(0f, 2f, -3.1f), new Vector3(12f, 4f, 0.4f), k.basaltoMedio, false);
        var motivo = Instanciar(k.modeloMotivos, compuertaGO.transform, "Tallado");
        motivo.transform.localPosition = new Vector3(-0.35f * 1.4f, 0.4f, -0.21f);
        motivo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        motivo.transform.localScale = Vector3.one * 1.4f;
        Pintar(motivo, _ => k.ambar);
        var compuerta = compuertaGO.AddComponent<Compuerta>();
        compuerta.receptores.Add(ancla);
        compuerta.desplazamiento = new Vector3(0f, -4.4f, 0f);
        compuerta.duracion = 3f;
        compuerta.acentos = motivo.GetComponentsInChildren<Renderer>();
        compuerta.sonido = compuertaGO.AddComponent<AudioSource>();
        ConfigurarAudio(compuerta.sonido, k.audio.compuerta, 1f, false, true);
        compuerta.sonido.maxDistance = 40f;
        refs.compuertaUmbral = compuerta;
        losa.isStatic = false;

        Luz(g, "Luz_Umbral", new Vector3(0f, 3.4f, -17f), LuzCalida, 120f, 14f, true);
        Luz(g, "Luz_Compuerta", new Vector3(0f, 3.2f, -6f), LuzCalida, 45f, 9f, false);
    }

    // ================================================================== 03 Campo

    static void ConstruirCampo(Kit k, Transform g)
    {
        Caja(g, "Piso_Campo_Entrada", -6, -0.3f, -3, 6, 0, 6, k.piso);
        Caja(g, "Piso_Campo_Medio_1", -6, -0.3f, 8.6f, 6, 0, 13, k.piso);
        Caja(g, "Piso_Campo_Medio_2", -6, -0.3f, 18.5f, 6, 0, 24, k.piso);
        Caja(g, "Piso_Campo_Salida", -6, -0.3f, 27.5f, 6, 0, 30, k.piso);
        Caja(g, "Muro_Campo_Izq", -6.3f, -9, -3, -6, 6, 30, k.basaltoMedio);
        Caja(g, "Muro_Campo_Der", 6, -9, -3, 6.3f, 6, 30, k.basalto);
        Caja(g, "Dintel_Umbral", -6.3f, 4, -3.3f, 6.3f, 6, -3, k.basalto);
        Caja(g, "Techo_Campo", -6.3f, 6, -3.3f, 6.3f, 6.3f, 30, k.techo);
        Oculo(k, g, "Oculo_Campo", new Vector3(0f, 5.97f, 16f), 3.2f, 150f, 10f);

        ColocarRecogible(k, g, "Recogible_Cuerpo", k.cuerpo, new Vector3(0f, 0f, 1.5f));

        // Enseñar: las dos anclas entran juntas en el cono (medio ángulo 14°) desde el borde
        var a1 = CrearAnclaEnEscena(k, g, "Ancla_Ensenar_A", new Vector3(-1.15f, 0f, 11.6f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 3f, 0);
        var a2 = CrearAnclaEnEscena(k, g, "Ancla_Ensenar_B", new Vector3(1.15f, 0f, 11.6f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 3f, 2);
        Puente(k, g, "Puente_Ensenar", 6f, 8.6f, 3.5f, a1, a2);

        // Probar: más separadas, hay que barrer de una a la otra y cruzar con la retención
        var b1 = CrearAnclaEnEscena(k, g, "Ancla_Probar_A", new Vector3(-2.6f, 0f, 20.2f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 5.5f, 1);
        var b2 = CrearAnclaEnEscena(k, g, "Ancla_Probar_B", new Vector3(2.6f, 0f, 20.2f), Quaternion.Euler(0f, 180f, 0f), FiltroDefinicion.Canal.Cuerpo, 5.5f, 3);
        Puente(k, g, "Puente_Probar", 13f, 18.5f, 3.6f, b1, b2);

        // Torcer: cuelgan del techo sobre el hueco, hay que levantar la mirada
        var c1 = CrearAnclaEnEscena(k, g, "Ancla_Torcer_A", new Vector3(-1.05f, 6f, 25.75f), Quaternion.Euler(180f, 0f, 0f), FiltroDefinicion.Canal.Cuerpo, 4.5f, 2);
        var c2 = CrearAnclaEnEscena(k, g, "Ancla_Torcer_B", new Vector3(1.05f, 6f, 25.75f), Quaternion.Euler(180f, 0f, 0f), FiltroDefinicion.Canal.Cuerpo, 4.5f, 4);
        Puente(k, g, "Puente_Torcer", 24f, 27.5f, 3.6f, c1, c2);

        Luz(g, "Luz_Campo", new Vector3(0f, 5.3f, 2f), LuzCalida, 130f, 15f, true);
        Luz(g, "Luz_Campo_Fondo", new Vector3(0f, 5.3f, 22f), LuzCalida, 80f, 12f, false);
    }

    // ================================================================== 04 Hondonada

    static void ConstruirHondonada(Kit k, Transform g)
    {
        Caja(g, "Piso_Hondonada", -6, -0.3f, 30, 6, 0, 61, k.piso);
        Caja(g, "Repisa_Hondonada", -1.8f, -0.3f, 64, 1.8f, 0, 65.5f, k.piso);
        Caja(g, "Piso_Hondonada_Salida", -6, -0.3f, 65.5f, 6, 0, 68.5f, k.piso);
        Caja(g, "Muro_Hondonada_Izq", -6.3f, -9, 30, -6, 6, 68.5f, k.basalto);
        Caja(g, "Muro_Hondonada_Der", 6, -9, 30, 6.3f, 6, 68.5f, k.basaltoMedio);
        Caja(g, "Techo_Hondonada", -6.3f, 6, 30, 6.3f, 6.3f, 68.5f, k.techo);

        ColocarRecogible(k, g, "Recogible_Hueco", k.hueco, new Vector3(0f, 0f, 38.5f));

        // Enseñar: una reja de lado a lado
        Reja(k, g, "Reja_Ensenar_A", -3f, 46f);
        Reja(k, g, "Reja_Ensenar_B", 3f, 46f);

        // Probar: zigzag, la mitad de cada paso es roca maciza
        Reja(k, g, "Reja_Zigzag_1", -3f, 50f);
        Caja(g, "Tabique_Zigzag_1", 0, -0.3f, 49.6f, 6, 6, 50.4f, k.basaltoMedio);
        Caja(g, "Tabique_Zigzag_2", -6, -0.3f, 53.6f, 0, 6, 54.4f, k.basaltoMedio);
        Reja(k, g, "Reja_Zigzag_2", 3f, 54f);
        Reja(k, g, "Reja_Zigzag_3", -3f, 58f);
        Caja(g, "Tabique_Zigzag_3", 0, -0.3f, 57.6f, 6, 6, 58.4f, k.basaltoMedio);

        // Torcer: puente con CUERPO, repisa angosta, y reja con HUECO
        var a1 = CrearAnclaEnEscena(k, g, "Ancla_Hondonada_A", new Vector3(-4f, 0f, 60.1f), Quaternion.Euler(0f, 150f, 0f), FiltroDefinicion.Canal.Cuerpo, 5f, 3);
        var a2 = CrearAnclaEnEscena(k, g, "Ancla_Hondonada_B", new Vector3(4f, 0f, 60.1f), Quaternion.Euler(0f, 210f, 0f), FiltroDefinicion.Canal.Cuerpo, 5f, 5);
        Puente(k, g, "Puente_Hondonada", 61f, 64f, 3.6f, a1, a2);
        Reja(k, g, "Reja_Salida_A", -3f, 66.6f);
        Reja(k, g, "Reja_Salida_B", 3f, 66.6f);

        Luz(g, "Luz_Hondonada", new Vector3(0f, 5.3f, 38f), LuzFria, 120f, 15f, true);
        Luz(g, "Luz_Zigzag", new Vector3(0f, 5.3f, 52f), LuzFria, 90f, 12f, false);
        Luz(g, "Luz_Hondonada_Fondo", new Vector3(0f, 5.3f, 64f), LuzFria, 70f, 11f, false);
    }

    // ================================================================== 05 Cresta

    static void ConstruirCresta(Kit k, Transform g, Referencias refs)
    {
        Caja(g, "Piso_Corredor", -2, -0.3f, 68.5f, 2, 0, 75.3f, k.piso);
        Caja(g, "Muro_Corredor_Izq", -2.3f, -0.3f, 68.5f, -2, 6, 75, k.basalto);
        Caja(g, "Muro_Corredor_Der", 2, -0.3f, 68.5f, 2.3f, 6, 75, k.basalto);
        Caja(g, "Techo_Corredor", -2.3f, 6, 68.5f, 2.3f, 6.3f, 75, k.techo);
        Caja(g, "Cierre_Hondonada_Izq", -6.3f, -0.3f, 68.5f, -2, 6, 68.8f, k.basalto);
        Caja(g, "Cierre_Hondonada_Der", 2, -0.3f, 68.5f, 6.3f, 6, 68.8f, k.basalto);

        Caja(g, "Piso_Cresta", -12, -0.3f, 75, 12, 0, 99, k.piso);
        Caja(g, "Muro_Cresta_Izq", -12.3f, -0.3f, 75, -12, 8, 99, k.basalto);
        Caja(g, "Muro_Cresta_Der", 12, -0.3f, 75, 12.3f, 8, 99, k.basaltoMedio);
        // el fondo es una pared pulida con el hueco de la puerta del eclipse (x -0,8 … 0,8)
        Caja(g, "Muro_Cresta_Fondo_Izq", -12.3f, -0.3f, 99, -0.8f, 8, 99.3f, k.espejo);
        Caja(g, "Muro_Cresta_Fondo_Der", 0.8f, -0.3f, 99, 12.3f, 8, 99.3f, k.espejo);
        Caja(g, "Muro_Cresta_Fondo_Dintel", -0.8f, 3f, 99, 0.8f, 8, 99.3f, k.espejo);
        Caja(g, "Muro_Cresta_Frente_Izq", -12.3f, -0.3f, 74.7f, -2, 8, 75, k.basalto);
        Caja(g, "Muro_Cresta_Frente_Der", 2, -0.3f, 74.7f, 12.3f, 8, 75, k.basalto);
        Caja(g, "Dintel_Corredor", -2, 6, 74.7f, 2, 8, 75, k.basalto);
        Caja(g, "Techo_Cresta", -12.3f, 8, 74.7f, 12.3f, 8.3f, 99.3f, k.techo);
        Oculo(k, g, "Oculo_Cresta", new Vector3(0f, 7.97f, 87f), 6.4f, 90f, 14f);

        var zona = new GameObject("Zona_Cresta");
        zona.transform.SetParent(g, false);
        zona.transform.position = new Vector3(0f, 1.5f, 84f);
        var caja = zona.AddComponent<BoxCollider>();
        caja.isTrigger = true;
        caja.size = new Vector3(22f, 3f, 14f);
        refs.zonaCresta = zona.AddComponent<ZonaJugador>();

        Luz(g, "Luz_Cresta", new Vector3(0f, 6.8f, 81f), LuzFria, 90f, 20f, false);

        ConstruirPuertaEclipse(k, g, refs);
    }

    /// <summary>
    /// La puerta que sólo aparece en la oscuridad. Con la linterna prendida, la pared
    /// pulida devuelve el reflejo del foco; adaptado, el contorno se enciende y la hoja
    /// desaparece. Detrás hay un vestíbulo corto que termina en la luz del eclipse.
    /// </summary>
    static void ConstruirPuertaEclipse(Kit k, Transform g, Referencias refs)
    {
        var raiz = Grupo(g, "PuertaEclipse");

        var espejo = new GameObject("ParedEspejo");
        espejo.transform.SetParent(raiz, false);
        espejo.transform.position = new Vector3(0f, 3.85f, 99f);
        var paredEspejo = espejo.AddComponent<ParedEspejo>();
        var reflejo = Plano(espejo.transform, "Reflejo", k.resplandor);
        var rebote = Luz(espejo.transform, "LuzRebote", new Vector3(0f, 1.6f, 98.4f), Color.white, 0f, 8f, false);
        Asignar(paredEspejo, "reflejo", reflejo);
        Asignar(paredEspejo, "luzRebote", rebote);

        var hoja = Caja(raiz, "Hoja", -0.8f, -0.3f, 99, 0.8f, 3, 99.3f, k.espejo);
        hoja.isStatic = false;

        var contorno = new[]
        {
            Adorno(raiz, "Contorno_Izq", new Vector3(-0.84f, 1.52f, 98.97f), new Vector3(0.06f, 3.05f, 0.04f), k.puertaEclipse),
            Adorno(raiz, "Contorno_Der", new Vector3(0.84f, 1.52f, 98.97f), new Vector3(0.06f, 3.05f, 0.04f), k.puertaEclipse),
            Adorno(raiz, "Contorno_Dintel", new Vector3(0f, 3.03f, 98.97f), new Vector3(1.74f, 0.06f, 0.04f), k.puertaEclipse),
        };

        // vestíbulo
        Caja(raiz, "Piso_Vestibulo", -0.95f, -0.3f, 99.3f, 0.95f, 0, 101.9f, k.piso);
        Caja(raiz, "Muro_Vestibulo_Izq", -1.05f, -0.3f, 99.3f, -0.95f, 3.2f, 101.9f, k.basalto);
        Caja(raiz, "Muro_Vestibulo_Der", 0.95f, -0.3f, 99.3f, 1.05f, 3.2f, 101.9f, k.basalto);
        Caja(raiz, "Techo_Vestibulo", -1.05f, 3.1f, 99.3f, 1.05f, 3.3f, 101.9f, k.techo);
        Caja(raiz, "Luz_Del_Eclipse", -0.95f, -0.3f, 101.8f, 0.95f, 3.1f, 101.9f, k.luzEclipse)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Luz(raiz, "Luz_Vestibulo", new Vector3(0f, 1.6f, 101.2f), new Color(0.75f, 0.85f, 1f), 12f, 4f, false);

        var viento = new GameObject("VientoDeLaPuerta").AddComponent<AudioSource>();
        viento.transform.SetParent(raiz, false);
        viento.transform.position = new Vector3(0f, 1.5f, 100.4f);
        ConfigurarAudio(viento, k.audio.vientoOculo, 0f, true, true);
        viento.maxDistance = 30f;
        var abrir = new GameObject("SonidoAbrir").AddComponent<AudioSource>();
        abrir.transform.SetParent(raiz, false);
        abrir.transform.position = new Vector3(0f, 1.5f, 99.2f);
        ConfigurarAudio(abrir, k.audio.compuerta, 0.8f, false, true);
        abrir.maxDistance = 40f;

        var puerta = raiz.gameObject.AddComponent<PuertaEclipse>();
        Asignar(puerta, "hoja", hoja);
        AsignarLista(puerta, "contorno", contorno);
        Asignar(puerta, "viento", viento);
        Asignar(puerta, "sonidoAbrir", abrir);
        refs.puertaEclipse = puerta;

        refs.zonaCruce = Zona(raiz, "Zona_Cruce", new Vector3(0f, 1.4f, 100.8f), new Vector3(1.6f, 2.8f, 1.6f));
    }

    // ================================================================== 06 Capilla (epílogo)

    static void ConstruirCapilla(Kit k, Transform g, Referencias refs)
    {
        g.position = new Vector3(300f, 0f, 0f);

        ConstruirEdificioCapilla(k, g);
        ConstruirPaisajeCapilla(k, g);

        refs.spawnCapilla = new GameObject("SpawnCapilla").transform;
        refs.spawnCapilla.SetParent(g, false);
        refs.spawnCapilla.localPosition = new Vector3(0f, 0.05f, -4.5f);

        // el jugador empieza acá: el prólogo lo lleva a la Explanada
        refs.jugador.transform.SetPositionAndRotation(refs.spawnCapilla.position, refs.spawnCapilla.rotation);
        refs.zonaUmbralCapilla = Zona(g, "Zona_Umbral_Capilla", g.TransformPoint(new Vector3(0f, 1.5f, 7.6f)), new Vector3(2.4f, 3f, 0.8f));

        ConstruirCraterDelValle(k, g, refs);

        // epílogo: quedarse en el lugar donde estaba el cráter dispara los créditos
        refs.zonaFinal = Zona(g, "Zona_LugarDelCrater", g.TransformPoint(new Vector3(0f, 1.5f, 34f)), new Vector3(16f, 3f, 16f));
        refs.zonaFinal.segundosDePermanencia = 5f;

        var solGO = new GameObject("Sol_Epilogo");
        solGO.transform.SetParent(g, false);
        solGO.transform.rotation = Quaternion.Euler(28f, 200f, 0f);
        refs.sol = solGO.AddComponent<Light>();
        refs.sol.type = LightType.Directional;
        refs.sol.color = new Color(1f, 0.88f, 0.72f);
        refs.sol.intensity = 1.6f;
        refs.sol.shadows = LightShadows.Soft;
        refs.sol.enabled = false;

        ConstruirCielo(k, g, refs);
    }

    /// <summary>
    /// El cráter que el eclipse revela en el valle, frente a la capilla. Arranca
    /// escondido: el borde bajo tierra y el fondo cerrado. En el centro, la puerta.
    /// </summary>
    static void ConstruirCraterDelValle(Kit k, Transform g, Referencias refs)
    {
        var crater = Grupo(g, "Crater_Valle");
        refs.craterValle = crater.gameObject;
        Vector3 centro = g.TransformPoint(new Vector3(0f, 0f, 34f));
        const float radio = 9f;
        const int piezas = 20;

        var azar = new System.Random(19);
        float Azar(float min, float max) => min + (float)azar.NextDouble() * (max - min);
        var bordes = new List<Transform>();
        for (int i = 0; i < piezas; i++)
        {
            float ang = i * 360f / piezas;
            // hueco hacia la capilla (sur): por ahí se entra
            if (Mathf.Abs(Mathf.DeltaAngle(ang, 270f)) < 20f) continue;
            var dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0f, Mathf.Sin(ang * Mathf.Deg2Rad));
            float alto = Azar(2.2f, 3.6f);
            var roca = Bloque(crater, $"Borde_{i:00}", centro + dir * Azar(radio - 0.4f, radio + 0.4f) + Vector3.up * (alto / 2f - 0.5f),
                new Vector3(Azar(2.6f, 3.4f), alto, Azar(1.8f, 2.6f)), i % 3 == 0 ? k.basaltoMedio : k.basalto, false);
            roca.transform.rotation = Quaternion.LookRotation(-dir) * Quaternion.Euler(Azar(-8f, 8f), Azar(-12f, 12f), Azar(-6f, 6f));
            bordes.Add(roca.transform);
        }
        refs.bordesCrater = bordes.ToArray();

        var fondo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fondo.name = "Fondo";
        Object.DestroyImmediate(fondo.GetComponent<Collider>());
        fondo.transform.SetParent(crater, false);
        fondo.transform.position = centro + Vector3.up * -0.075f;
        fondo.transform.localScale = new Vector3(radio * 2f - 1f, 0.02f, radio * 2f - 1f);
        fondo.GetComponent<Renderer>().sharedMaterial = k.techo;
        refs.fondoCrater = fondo.transform;

        // la puerta: dos pilares y un dintel de basalto, con el contorno que destella
        var puerta = Grupo(crater, "Puerta");
        puerta.position = centro;
        refs.puertaValle = puerta;
        Bloque(puerta, "Pilar_Izq", centro + new Vector3(-0.95f, 1.5f, 0f), new Vector3(0.4f, 3.2f, 0.4f), k.basaltoMedio, false);
        Bloque(puerta, "Pilar_Der", centro + new Vector3(0.95f, 1.5f, 0f), new Vector3(0.4f, 3.2f, 0.4f), k.basaltoMedio, false);
        Bloque(puerta, "Dintel", centro + new Vector3(0f, 3.2f, 0f), new Vector3(2.3f, 0.4f, 0.45f), k.basalto, false);
        refs.contornoValle = new[]
        {
            Adorno(puerta, "Contorno_Izq", centro + new Vector3(-0.72f, 1.45f, -0.05f), new Vector3(0.06f, 2.9f, 0.06f), k.puertaEclipse),
            Adorno(puerta, "Contorno_Der", centro + new Vector3(0.72f, 1.45f, -0.05f), new Vector3(0.06f, 2.9f, 0.06f), k.puertaEclipse),
            Adorno(puerta, "Contorno_Dintel", centro + new Vector3(0f, 2.93f, -0.05f), new Vector3(1.5f, 0.06f, 0.06f), k.puertaEclipse),
        };
        var destello = Plano(puerta, "Destello", k.resplandor);
        destello.transform.position = centro + new Vector3(0f, 1.45f, -0.25f);
        refs.destelloValle = destello;
        refs.zonaPuertaCrater = Zona(puerta, "Zona_Puerta_Crater", centro + new Vector3(0f, 1.4f, 0f), new Vector3(1.4f, 2.8f, 1.2f));

        // epílogo: el pasto aplastado donde estuvo
        var huella = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        huella.name = "Huella_Crater";
        Object.DestroyImmediate(huella.GetComponent<Collider>());
        huella.transform.SetParent(g, false);
        huella.transform.position = centro + Vector3.up * -0.09f;
        huella.transform.localScale = new Vector3(radio * 2f, 0.01f, radio * 2f);
        huella.GetComponent<Renderer>().sharedMaterial = k.huella;
        refs.huella = huella;
    }

    /// <summary>El sol, la luna y la corona. CieloEclipse los ubica cada frame.</summary>
    static void ConstruirCielo(Kit k, Transform g, Referencias refs)
    {
        var cieloGO = new GameObject("CieloEclipse");
        cieloGO.transform.SetParent(g, false);
        var cielo = cieloGO.AddComponent<CieloEclipse>();

        Transform Disco(string nombre, Material m)
        {
            var disco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            disco.name = nombre;
            Object.DestroyImmediate(disco.GetComponent<Collider>());
            disco.transform.SetParent(cieloGO.transform, false);
            var r = disco.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return disco.transform;
        }

        Asignar(cielo, "sol", refs.sol);
        Asignar(cielo, "discoSol", Disco("DiscoSol", k.discoSol));
        Asignar(cielo, "discoLuna", Disco("DiscoLuna", k.discoLuna));
        Asignar(cielo, "corona", Plano(cieloGO.transform, "Corona", k.corona));
        refs.cielo = cielo;
    }

    // ================================================================== arte

    static void Vestir(Kit k, Transform arte)
    {
        var modulos = Grupo(arte, "ArquitecturaModular");
        void Par(float z, float mitad, float escala, string sala)
        {
            Modulo(k, modulos, $"Modulo_{sala}_Izq_{z:0}", new Vector3(-mitad + 0.45f * escala, 0f, z), 90f, escala, k.piedra);
            Modulo(k, modulos, $"Modulo_{sala}_Der_{z:0}", new Vector3(mitad - 0.45f * escala, 0f, z), -90f, escala, k.piedra);
        }
        foreach (float z in new[] { -19f, -13f, -7f }) Par(z, 6f, 0.62f, "Umbral");
        foreach (float z in new[] { 1f, 10.5f, 16f, 21.5f }) Par(z, 6f, 0.82f, "Campo");
        foreach (float z in new[] { 33f, 40f, 63f }) Par(z, 6f, 0.82f, "Hondonada");
        foreach (float z in new[] { 80f, 87f, 94f }) Par(z, 12f, 1f, "Cresta");
        // el fondo de la Cresta tiene la puerta del eclipse al centro: los módulos la enmarcan
        Modulo(k, modulos, "Modulo_Cresta_Fondo_Izq", new Vector3(-6f, 0f, 98.55f), 180f, 1.05f, k.piedra);
        Modulo(k, modulos, "Modulo_Cresta_Fondo_Der", new Vector3(6f, 0f, 98.55f), 180f, 1.05f, k.piedra);
        Modulo(k, modulos, "Modulo_Plaza_Izq", new Vector3(-7f + 0.45f * 0.6f, 4f, -37.5f), 90f, 0.6f, k.piedra);
        Modulo(k, modulos, "Modulo_Plaza_Der", new Vector3(7f - 0.45f * 0.6f, 4f, -37.5f), -90f, 0.6f, k.piedra);

        var motivos = Grupo(arte, "MotivosTallados");
        Motivo(k, motivos, "Mural_Plaza", new Vector3(0f, 4.4f, -40.95f), 0f, 0.9f, k.ambar);
        Motivo(k, motivos, "Mural_Umbral", new Vector3(5.93f, 0.3f, -16f), -90f, 0.7f, k.ambar);
        Motivo(k, motivos, "Mural_Campo", new Vector3(-5.93f, 0.6f, 4.5f), 90f, 0.8f, k.ambar);
        Motivo(k, motivos, "Mural_Hondonada", new Vector3(5.93f, 0.6f, 36.5f), -90f, 0.8f, k.ambar);

        // tallados latentes: casi invisibles con la linterna, aparecen al adaptarse
        var latentes = Grupo(arte, "MotivosLatentes_Cresta");
        foreach (float z in new[] { 83.5f, 90.5f })
        {
            Motivo(k, latentes, $"Latente_Izq_{z:0}", new Vector3(-11.93f, 1.4f, z), 90f, 1.1f, k.motivoLatente);
            Motivo(k, latentes, $"Latente_Der_{z:0}", new Vector3(11.93f, 1.4f, z), -90f, 1.1f, k.motivoLatente);
        }
        Motivo(k, latentes, "Latente_Fondo", new Vector3(0f, 5.2f, 98.95f), 180f, 1.6f, k.motivoLatente);

        // el borde del cráter: dos cordones de roca a los costados y uno al fondo,
        // siempre fuera del recorrido (|x| > 26) y sin colisión. Se ven desde la explanada.
        var horizonte = Grupo(arte, "Horizonte");
        var azar = new System.Random(7);
        float Azar(float min, float max) => min + (float)azar.NextDouble() * (max - min);
        void Roca(string nombre, Vector3 pie, float ancho, float alto)
        {
            var roca = Bloque(horizonte, nombre, pie + Vector3.up * (alto / 2f - 4f), new Vector3(ancho, alto, Azar(8f, 14f)), k.basalto, false);
            roca.transform.rotation = Quaternion.Euler(Azar(-5f, 5f), Azar(-25f, 25f), Azar(-6f, 6f));
            roca.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(roca.GetComponent<Collider>());
        }
        int n = 0;
        for (float z = -80f; z <= 115f; z += Azar(14f, 20f))
        {
            Roca($"Borde_{n++:00}", new Vector3(-Azar(28f, 40f), 0f, z), Azar(16f, 26f), Azar(14f, 34f));
            Roca($"Borde_{n++:00}", new Vector3(Azar(28f, 40f), 0f, z + Azar(-6f, 6f)), Azar(16f, 26f), Azar(14f, 34f));
        }
        for (float x = -30f; x <= 30f; x += 20f)
            Roca($"Borde_{n++:00}", new Vector3(x, 0f, Azar(-82f, -72f)), Azar(20f, 28f), Azar(18f, 30f));
    }

    static void Modulo(Kit k, Transform padre, string nombre, Vector3 posicion, float rotY, float escala, Material piedra)
    {
        var m = Instanciar(k.modeloArquitectura, padre, nombre);
        m.transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, rotY, 0f));
        m.transform.localScale = Vector3.one * escala;
        Pintar(m, r => r.name.Contains("Wall") ? k.basaltoMedio : piedra);
        Estatico(m);
    }

    static void Motivo(Kit k, Transform padre, string nombre, Vector3 posicion, float rotY, float escala, Material material)
    {
        var m = Instanciar(k.modeloMotivos, padre, nombre);
        // el tallado está corrido 0,31 m del pivote: se centra
        var rot = Quaternion.Euler(0f, rotY, 0f);
        m.transform.SetPositionAndRotation(posicion + rot * new Vector3(0.31f * escala, 0f, 0f), rot);
        m.transform.localScale = Vector3.one * escala;
        Pintar(m, _ => material);
        foreach (var r in m.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        Estatico(m);
    }

    // ================================================================== sistemas

    /// <summary>
    /// Crea los objetos de lógica (interfaz, pausa, flujo, eclipse, prólogo, audio global)
    /// y los conecta entre sí. 'Asignar' escribe un campo privado [SerializeField] por su
    /// nombre, como si se arrastrara la referencia en el Inspector.
    /// </summary>
    static void ConstruirSistemas(Kit k, Referencias refs)
    {
        var linterna = refs.jugador.GetComponentInChildren<LinternaController>();

        var volumenGO = new GameObject("Global Volume");
        var volumen = volumenGO.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.sharedProfile = k.perfil;
        var viento = volumenGO.AddComponent<AudioSource>();
        ConfigurarAudio(viento, k.audio.vientoOculo, 0f, true, false);
        viento.playOnAwake = true;
        var adaptacion = volumenGO.AddComponent<AdaptacionOscuridad>();
        adaptacion.tiempoDeAdaptacion = 10f;
        adaptacion.demoraInicial = 3f;
        adaptacion.exposicionAdaptada = 3.2f;
        adaptacion.ambienteDeAdaptacion = viento;

        var sistemas = new GameObject("SISTEMAS");
        var interfaz = sistemas.AddComponent<InterfazCrater>();
        var pausa = sistemas.AddComponent<PausaCrater>();
        var flujo = sistemas.AddComponent<FlujoJuegoCrater>();
        var eclipse = sistemas.AddComponent<EclipseFinalController>();

        var prologo = sistemas.AddComponent<PrologoCapilla>();

        var ambiente = sistemas.AddComponent<AudioSource>();
        ConfigurarAudio(ambiente, k.audio.ambienteCrater, 0.55f, true, false);
        ambiente.playOnAwake = true;
        var exterior = sistemas.AddComponent<AudioSource>();
        ConfigurarAudio(exterior, k.audio.exteriorCapilla, 0.9f, true, false);
        var pajaros = sistemas.AddComponent<AudioSource>();
        ConfigurarAudio(pajaros, k.audio.pajaros, 0.85f, true, false);
        var campana = sistemas.AddComponent<AudioSource>();
        ConfigurarAudio(campana, k.audio.campana, 0.8f, false, false);
        var revelado = sistemas.AddComponent<AudioSource>();
        ConfigurarAudio(revelado, k.audio.compuerta, 1f, false, false);
        var destelloPuerta = sistemas.AddComponent<AudioSource>();
        ConfigurarAudio(destelloPuerta, k.audio.destelloPuerta, 0.8f, false, false);
        var musica = sistemas.AddComponent<AudioSource>();
        ConfigurarAudio(musica, k.audio.musicaCreditos, 0f, true, false);
        var grave = sistemas.AddComponent<AudioSource>();
        ConfigurarAudio(grave, k.audio.graveEclipse, 0f, true, false);
        var tono = sistemas.AddComponent<AudioSource>();
        ConfigurarAudio(tono, k.audio.anilloDiamante, 0.8f, false, false);
        tono.ignoreListenerPause = true;

        Asignar(interfaz, "linterna", linterna);
        Asignar(pausa, "interfaz", interfaz);
        Asignar(flujo, "linterna", linterna);
        Asignar(flujo, "interfaz", interfaz);
        Asignar(flujo, "eclipse", eclipse);
        Asignar(flujo, "compuertaUmbral", refs.compuertaUmbral);
        Asignar(eclipse, "adaptacion", adaptacion);
        Asignar(eclipse, "linterna", linterna);
        Asignar(eclipse, "interfaz", interfaz);
        Asignar(eclipse, "flujo", flujo);
        Asignar(eclipse, "pausa", pausa);
        Asignar(eclipse, "jugador", refs.jugador.transform);
        Asignar(eclipse, "spawnCapilla", refs.spawnCapilla);
        Asignar(eclipse, "luzDelCrater", refs.luna);
        Asignar(eclipse, "solEpilogo", refs.sol);
        Asignar(eclipse, "ambienteCrater", ambiente);
        Asignar(eclipse, "ambienteExterior", exterior);
        Asignar(eclipse, "puerta", refs.puertaEclipse);
        Asignar(eclipse, "prologo", prologo);
        Asignar(eclipse, "cielo", refs.cielo);
        Asignar(eclipse, "tonoFinal", tono);
        Asignar(eclipse, "campana", campana);
        Asignar(eclipse, "musicaCreditos", musica);
        Asignar(refs.jugador.GetComponent<RespawnPorCaida>(), "interfaz", interfaz);
        Asignar(refs.puertaEclipse, "adaptacion", adaptacion);

        Asignar(prologo, "jugador", refs.jugador.GetComponent<JugadorFPS>());
        Asignar(prologo, "cielo", refs.cielo);
        Asignar(prologo, "eclipse", eclipse);
        Asignar(prologo, "flujo", flujo);
        Asignar(prologo, "interfaz", interfaz);
        Asignar(prologo, "spawnCrater", refs.spawnInicio);
        Asignar(prologo, "crater", refs.craterValle);
        AsignarLista(prologo, "bordes", refs.bordesCrater);
        Asignar(prologo, "fondo", refs.fondoCrater);
        Asignar(prologo, "puerta", refs.puertaValle);
        AsignarLista(prologo, "contornoPuerta", refs.contornoValle);
        Asignar(prologo, "destello", refs.destelloValle);
        Asignar(prologo, "huella", refs.huella);
        Asignar(prologo, "zonaEpilogo", refs.zonaFinal.gameObject);
        Asignar(prologo, "pajaros", pajaros);
        Asignar(prologo, "graveEclipse", grave);
        Asignar(prologo, "campana", campana);
        Asignar(prologo, "sonidoRevelado", revelado);
        Asignar(prologo, "sonidoDestello", destelloPuerta);

        UnityEventTools.AddPersistentListener(refs.compuertaUmbral.alAbrirse, new UnityAction(flujo.NotificarUmbralAbierto));
        UnityEventTools.AddPersistentListener(refs.zonaCresta.alEntrar, new UnityAction(flujo.EntrarCresta));
        UnityEventTools.AddPersistentListener(refs.zonaFinal.alEntrar, new UnityAction(eclipse.CerrarDemo));
        UnityEventTools.AddPersistentListener(adaptacion.alAdaptarse, new UnityAction(eclipse.AlCompletarAdaptacion));
        UnityEventTools.AddPersistentListener(refs.zonaCruce.alEntrar, new UnityAction(eclipse.CruzarPuerta));
        UnityEventTools.AddPersistentListener(refs.zonaUmbralCapilla.alEntrar, new UnityAction(prologo.IniciarCinematica));
        UnityEventTools.AddPersistentListener(refs.zonaPuertaCrater.alEntrar, new UnityAction(prologo.EntrarAlCrater));
    }

    // ================================================================== piezas del nivel

    static Ancla CrearAnclaEnEscena(Kit k, Transform g, string nombre, Vector3 posicion, Quaternion rotacion,
                                    FiltroDefinicion.Canal canal, float retencion, int tono)
    {
        var go = Instancia(k.prefabAncla, g, nombre, posicion, rotacion);
        var ancla = go.GetComponent<Ancla>();
        ancla.canalRequerido = canal;
        ancla.retencion = retencion;
        PrefabUtility.RecordPrefabInstancePropertyModifications(ancla);
        ancla.tono.clip = k.audio.tonosAncla[tono % k.audio.tonosAncla.Length];
        PrefabUtility.RecordPrefabInstancePropertyModifications(ancla.tono);
        return ancla;
    }

    /// <summary>Tiende un puente sobre el hueco [zDesde, zHasta], sostenido por las anclas dadas.</summary>
    static PuenteLuz Puente(Kit k, Transform g, string nombre, float zDesde, float zHasta, float ancho, params Ancla[] anclas)
    {
        const float solape = 0.25f;
        var go = Instancia(k.prefabPuente, g, nombre, new Vector3(0f, 0f, zDesde - solape), Quaternion.identity,
            new Vector3(ancho / 2.2f, 1f, (zHasta - zDesde + solape * 2f) / 8f));
        var puente = go.GetComponent<PuenteLuz>();
        puente.anclas = new List<ReceptorDeLuz>(anclas);
        PrefabUtility.RecordPrefabInstancePropertyModifications(puente);
        return puente;
    }

    static MateriaHueca Reja(Kit k, Transform g, string nombre, float x, float z)
    {
        return Instancia(k.prefabReja, g, nombre, new Vector3(x, 0f, z), Quaternion.identity).GetComponent<MateriaHueca>();
    }

    static Recogible ColocarRecogible(Kit k, Transform g, string nombre, FiltroDefinicion filtro, Vector3 posicion)
    {
        var recogible = Instancia(k.prefabFiltro, g, nombre, posicion, Quaternion.identity).GetComponent<Recogible>();
        recogible.filtro = filtro;
        PrefabUtility.RecordPrefabInstancePropertyModifications(recogible);
        return recogible;
    }

    static void Oculo(Kit k, Transform g, string nombre, Vector3 posicion, float diametro, float intensidad, float alcance)
    {
        var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disco.name = nombre;
        Object.DestroyImmediate(disco.GetComponent<Collider>());
        disco.transform.SetParent(g, false);
        disco.transform.position = posicion;
        disco.transform.localScale = new Vector3(diametro, 0.02f, diametro);
        disco.GetComponent<Renderer>().sharedMaterial = k.cielo;
        disco.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        Estatico(disco);

        var luz = Luz(g, nombre + "_Haz", posicion - Vector3.up * 0.1f, new Color(0.45f, 0.6f, 1f), intensidad, alcance, true);
        luz.type = LightType.Spot;
        luz.spotAngle = 75f;
        luz.innerSpotAngle = 30f;
        luz.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    static GameObject Instancia(GameObject prefab, Transform padre, string nombre, Vector3 posicion, Quaternion rotacion, Vector3? escala = null)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        go.name = nombre;
        go.transform.SetPositionAndRotation(posicion, rotacion);
        if (escala.HasValue) go.transform.localScale = escala.Value;
        return go;
    }

    static ZonaJugador Zona(Transform padre, string nombre, Vector3 posicion, Vector3 tamanio)
    {
        var go = new GameObject(nombre);
        go.layer = LayerMask.NameToLayer("Ignore Raycast");
        go.transform.SetParent(padre, false);
        go.transform.position = posicion;
        var caja = go.AddComponent<BoxCollider>();
        caja.isTrigger = true;
        caja.size = tamanio;
        return go.AddComponent<ZonaJugador>();
    }

    /// <summary>Pieza decorativa sin colisión ni sombra (contornos emisivos).</summary>
    static Renderer Adorno(Transform padre, string nombre, Vector3 centro, Vector3 tamanio, Material m)
    {
        var go = Bloque(padre, nombre, centro, tamanio, m, false);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = ShadowCastingMode.Off;
        return r;
    }

    /// <summary>Un quad sin colisión (destellos, reflejo, corona). Su escala y color se manejan en runtime.</summary>
    static Renderer Plano(Transform padre, string nombre, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return r;
    }

    static void AsignarLista(Object objetivo, string campo, Object[] valores)
    {
        var so = new SerializedObject(objetivo);
        var propiedad = so.FindProperty(campo);
        if (propiedad == null) throw new System.InvalidOperationException($"{objetivo.GetType().Name} no tiene el campo '{campo}'.");
        propiedad.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++) propiedad.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Light Luz(Transform padre, string nombre, Vector3 posicion, Color color, float intensidad, float alcance, bool sombras)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = posicion;
        var luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = color;
        luz.intensity = intensidad;
        luz.range = alcance;
        luz.shadows = sombras ? LightShadows.Soft : LightShadows.None;
        return luz;
    }

    static Transform Grupo(Transform padre, string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    /// <summary>Caja definida por sus esquinas mínima y máxima, en coordenadas de mundo.</summary>
    // ejemplo: Caja(g, "Piso", -6, -0.3f, 0, 6, 0, 10, k.piso) = piso de 12 × 10 m con la cara de arriba en y = 0
    static GameObject Caja(Transform padre, string nombre, float x0, float y0, float z0, float x1, float y1, float z1, Material m)
    {
        return Bloque(padre, nombre, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f),
            new Vector3(x1 - x0, y1 - y0, z1 - z0), m);
    }

    static GameObject CajaLocal(Transform padre, string nombre, Vector3 centroLocal, Vector3 tamanio, Material m)
    {
        var go = Bloque(padre, nombre, padre.TransformPoint(centroLocal), tamanio, m);
        return go;
    }

    static GameObject Bloque(Transform padre, string nombre, Vector3 centro, Vector3 tamanio, Material m, bool estatico = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.position = centro;
        go.transform.localScale = tamanio;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (estatico) Estatico(go);
        return go;
    }

    static void Estatico(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
    }

    static void Asignar(Object objetivo, string campo, Object valor)
    {
        var so = new SerializedObject(objetivo);
        var propiedad = so.FindProperty(campo);
        if (propiedad == null) throw new System.InvalidOperationException($"{objetivo.GetType().Name} no tiene el campo '{campo}'.");
        propiedad.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
