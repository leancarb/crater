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
///   06 Capilla     y = 9         el valle, arriba de todo: prólogo y epílogo de día
///   01 Explanada   z -48 … -34   el fondo del pozo que el eclipse abre en el valle; se baja por una escalera
///      Pasaje      z -34 … -23   pasillo plano hasta el Umbral
///   02 Umbral      z -23 … -3    la linterna y la primera ancla (luz blanca) abren la compuerta
///   03 Rotonda     z  -3 …  27   circular, con dos alas en cualquier orden (ver ConstructorCrater.Alas.cs):
///      Ala oeste   x -39 … -15   filtro sol y su sello (entrada a mitad del costado)
///      Ala este    x  15 …  39   filtro luna y su sello
///   04 Cruce       z  27 …  68.5 los dos filtros juntos
///   05 Cresta      z 68.5 … 99   se cierra a la espalda; apagar la linterna, adaptarse y el techo se abre
///
/// CÓMO FUNCIONA
/// Cada sala se arma con cajas (Caja: esquina mínima y máxima en metros), prefabs
/// (anclas, puentes, rejas) y luces. El valle de la capilla está encima del nivel y
/// el cráter es un pozo de verdad (ver ConstructorCrater.Pozo.cs): se baja caminando.
/// Sólo el final es un salto: con la pantalla en blanco, el jugador vuelve a la capilla.
/// Al final, ConstruirSistemas crea los objetos de lógica y conecta referencias
/// (Asignar) y eventos (UnityEventTools) entre ellos.
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
        public GameObject craterValle, huella, guino;
        public Transform[] bordesCrater;
        // los tramos de pared de la escalera: suben con el borde del cráter (si no, asomarían antes del eclipse)
        public List<Transform> paredesEscalera = new List<Transform>();
        public Transform tapaCrater, puertaValle;
        public Renderer[] contornoValle;
        public Renderer destelloValle;
        public ZonaJugador zonaUmbralCapilla, zonaPuertaCrater, zonaMirador;
        public Transform spawnValle;

        // final
        public Compuerta cierreCresta, puertaSellos, atajoOeste, atajoEste;
        public ObeliscoDelEclipse obelisco;
        public HiloDeTallados hiloSoles, hiloLunas, hiloEclipse;
        public ReceptorDeLuz selloOeste, selloEste;
        public AperturaTecho techoCresta;
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
        var rotonda = Grupo(nivel, "03_Rotonda");
        ConstruirRotonda(kit, rotonda, refs);
        // cada ala se arma en su lugar de siempre y después se corre entera: así su entrada
        // queda a mitad del costado de la rotonda circular (ver ConstructorCrater.Alas.cs)
        var alaOeste = Grupo(nivel, "03_Ala_Oeste");
        var selloOeste = ConstruirAlaOeste(kit, alaOeste, refs);
        alaOeste.position = CorrimientoAlaOeste;
        var alaEste = Grupo(nivel, "03_Ala_Este");
        var selloEste = ConstruirAlaEste(kit, alaEste, refs);
        alaEste.position = CorrimientoAlaEste;
        ConstruirPuertaDeLosSellos(kit, rotonda, refs, selloOeste, selloEste);
        ConstruirCruce(kit, Grupo(nivel, "04_Cruce"));
        ConstruirCresta(kit, Grupo(nivel, "05_Cresta"), refs);
        ConstruirCapilla(kit, Grupo(nivel, "06_Capilla"), refs);
        Vestir(kit, arte);
        ConstruirSistemas(kit, refs);
        DespejarTallados(kit);
        FacetarEscena(kit);

        EditorSceneManager.SaveScene(escena, RutaEscena);
    }

    static void ConfigurarAmbiente()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        // la luz de relleno del interior: un poco más alta que la mínima, para que siempre
        // se lean las paredes y el piso aun lejos de las lámparas
        RenderSettings.ambientSkyColor = new Color(0.23f, 0.24f, 0.3f);
        RenderSettings.ambientEquatorColor = new Color(0.14f, 0.135f, 0.15f);
        RenderSettings.ambientGroundColor = new Color(0.06f, 0.05f, 0.045f);
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
        // el piso, las paredes y la escalera que baja desde el valle están en el pozo
        ConstruirPozo(k, g, refs);

        // el pasillo plano del fondo del pozo al Umbral
        Caja(g, "Muro_Pasaje_Izq", -2.05f, -0.3f, -34.3f, -1.75f, 4f, -23, k.basalto);
        Caja(g, "Muro_Pasaje_Der", 1.75f, -0.3f, -34.3f, 2.05f, 4f, -23, k.basalto);

        // para arrancar directo en la Explanada (PrologoCapilla.saltarPrologo)
        var spawn = new GameObject("SpawnInicio").transform;
        spawn.SetParent(g, false);
        spawn.position = new Vector3(-4f, 0.02f, -40f);
        refs.spawnInicio = spawn;
        refs.jugador = (GameObject)PrefabUtility.InstantiatePrefab(k.prefabJugador);
        refs.jugador.transform.SetPositionAndRotation(spawn.position, Quaternion.identity);

        // la luz del cielo sobre el cráter: alcanza el pozo y los óculos
        var lunaGO = new GameObject("Luz_Crater");
        lunaGO.transform.SetParent(g, false);
        lunaGO.transform.rotation = Quaternion.Euler(38f, 160f, 0f);
        refs.luna = lunaGO.AddComponent<Light>();
        refs.luna.type = LightType.Directional;
        refs.luna.color = new Color(0.55f, 0.63f, 0.9f);
        refs.luna.intensity = 1.2f;
        refs.luna.shadows = LightShadows.Soft;
        RenderSettings.sun = refs.luna;

        Luz(g, "Luz_Plaza", new Vector3(0f, 4.5f, -38f), LuzCalida, 90f, 14f, false);
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

        // la linterna, pasando los murales de los controles: así se los mira antes de agarrarla
        Instancia(k.prefabLinterna, g, "Recogible_Linterna", new Vector3(1.5f, 0f, -8.5f), Quaternion.identity);

        var ancla = CrearAnclaEnEscena(k, g, "Ancla_Umbral", new Vector3(-5.1f, 0f, -10f), Quaternion.Euler(0f, 90f, 0f),
            FiltroDefinicion.Canal.Ninguno, 2f, 0);
        AnclaDeLuzBlanca(k, g, ancla);
        BaseDePuerta(k, ancla);

        // compuerta: una losa que se hunde en el piso
        var compuertaGO = new GameObject("Compuerta_Umbral");
        compuertaGO.transform.SetParent(g, false);
        compuertaGO.transform.position = new Vector3(0f, 0f, -3.1f);
        var losa = Bloque(compuertaGO.transform, "Losa", new Vector3(0f, 2f, -3.1f), new Vector3(12f, 4f, 0.4f), k.basaltoMedio, false);
        var motivo = Instanciar(k.modeloMotivos, compuertaGO.transform, "Tallado");
        motivo.transform.localPosition = new Vector3(-0.35f * 1.4f, 0.4f, -0.21f);
        motivo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        motivo.transform.localScale = Vector3.one * 1.4f;
        Pintar(motivo, _ => k.tallaEclipse);   // blanco, como la luz y el ancla que la abren
        var compuerta = compuertaGO.AddComponent<Compuerta>();
        compuerta.receptores.Add(ancla);
        compuerta.desplazamiento = new Vector3(0f, -4.4f, 0f);
        compuerta.duracion = 3f;
        compuerta.acentos = motivo.GetComponentsInChildren<Renderer>();
        compuerta.colorAcento = new Color(0.85f, 0.92f, 1f);
        compuerta.sonido = compuertaGO.AddComponent<AudioSource>();
        ConfigurarAudio(compuerta.sonido, k.audio.compuerta, 1f, false, true);
        compuerta.sonido.maxDistance = 40f;
        refs.compuertaUmbral = compuerta;
        losa.isStatic = false;

        Luz(g, "Luz_Umbral", new Vector3(0f, 3.4f, -17f), LuzCalida, 120f, 14f, true);
        Luz(g, "Luz_Compuerta", new Vector3(0f, 3.2f, -6f), LuzCalida, 45f, 9f, false);
    }

    // ================================================================== 05 Cresta

    static void ConstruirCresta(Kit k, Transform g, Referencias refs)
    {
        Caja(g, "Piso_Corredor", -2, -0.3f, 68.5f, 2, 0, 75.3f, k.piso);
        Caja(g, "Muro_Corredor_Izq", -2.3f, -0.3f, 68.5f, -2, 6, 75, k.basalto);
        Caja(g, "Muro_Corredor_Der", 2, -0.3f, 68.5f, 2.3f, 6, 75, k.basalto);
        Caja(g, "Techo_Corredor", -2.3f, 6, 68.5f, 2.3f, 6.3f, 75, k.techo);
        // dos tabiques en zigzag y sin luz: por un momento no se ve hacia dónde se va (perderse
        // un poco); al salir se abre la Cresta de golpe
        Caja(g, "Tabique_Corredor_A", -2, -0.3f, 70.3f, 0.8f, 6, 70.6f, k.basalto);
        Caja(g, "Tabique_Corredor_B", -0.8f, -0.3f, 72.4f, 2, 6, 72.7f, k.basalto);
        Caja(g, "Cierre_Hondonada_Izq", -6.3f, -0.3f, 68.5f, -2, 6, 68.8f, k.basalto);
        Caja(g, "Cierre_Hondonada_Der", 2, -0.3f, 68.5f, 6.3f, 6, 68.8f, k.basalto);

        Caja(g, "Piso_Cresta", -12, -0.3f, 75, 12, 0, 99, k.piso);
        Caja(g, "Muro_Cresta_Izq", -12.3f, -0.3f, 75, -12, 8, 99, k.basalto);
        Caja(g, "Muro_Cresta_Der", 12, -0.3f, 75, 12.3f, 8, 99, k.basaltoMedio);
        // el fondo es una pared pulida: devuelve el reflejo del propio foco
        Caja(g, "Muro_Cresta_Fondo", -12.3f, -0.3f, 99, 12.3f, 8, 99.3f, k.espejo);
        Caja(g, "Muro_Cresta_Frente_Izq", -12.3f, -0.3f, 74.7f, -2, 8, 75, k.basalto);
        Caja(g, "Muro_Cresta_Frente_Der", 2, -0.3f, 74.7f, 12.3f, 8, 75, k.basalto);
        Caja(g, "Dintel_Corredor", -2, 6, 74.7f, 2, 8, 75, k.basalto);
        // el techo tiene un hueco de 12 × 12 m en el medio, tapado por dos hojas que se abren al final
        Caja(g, "Techo_Cresta_Sur", -12.3f, 8, 74.7f, 12.3f, 8.3f, 81, k.techo);
        Caja(g, "Techo_Cresta_Norte", -12.3f, 8, 93, 12.3f, 8.3f, 99.3f, k.techo);
        Caja(g, "Techo_Cresta_Oeste", -12.3f, 8, 81, -6, 8.3f, 93, k.techo);
        Caja(g, "Techo_Cresta_Este", 6, 8, 81, 12.3f, 8.3f, 93, k.techo);
        ConstruirTechoQueSeAbre(k, g, refs);
        ConstruirCierreCresta(k, g, refs);

        var zona = new GameObject("Zona_Cresta");
        zona.transform.SetParent(g, false);
        zona.transform.position = new Vector3(0f, 1.5f, 84f);
        var caja = zona.AddComponent<BoxCollider>();
        caja.isTrigger = true;
        caja.size = new Vector3(22f, 3f, 14f);
        refs.zonaCresta = zona.AddComponent<ZonaJugador>();

        Luz(g, "Luz_Cresta", new Vector3(0f, 6.8f, 81f), LuzFria, 90f, 20f, false);

        ConstruirParedEspejo(k, g);
    }

    /// <summary>
    /// La pared del fondo: con la linterna prendida devuelve el reflejo encandilante del
    /// foco. Es la pista para apagarla.
    /// </summary>
    static void ConstruirParedEspejo(Kit k, Transform g)
    {
        // todas las paredes de la Cresta devuelven el reflejo (el fondo, los costados y la entrada)
        void Pared(string nombre, Vector3 centro, Vector3 haciaAdentro)
        {
            var espejo = new GameObject(nombre);
            espejo.transform.SetParent(g, false);
            espejo.transform.SetPositionAndRotation(centro, Quaternion.LookRotation(haciaAdentro));
            var paredEspejo = espejo.AddComponent<ParedEspejo>();
            var reflejo = Plano(espejo.transform, "Reflejo", k.resplandorTallado);
            var rebote = Luz(espejo.transform, "LuzRebote", centro - haciaAdentro * 0.6f, Color.white, 0f, 12f, false);
            Asignar(paredEspejo, "reflejo", reflejo);
            Asignar(paredEspejo, "luzRebote", rebote);
        }
        Pared("ParedEspejo", new Vector3(0f, 3.85f, 99f), Vector3.forward);
        Pared("ParedEspejo_Oeste", new Vector3(-12f, 3.85f, 87f), Vector3.left);
        Pared("ParedEspejo_Este", new Vector3(12f, 3.85f, 87f), Vector3.right);
        Pared("ParedEspejo_Entrada", new Vector3(0f, 3.85f, 75f), Vector3.back);
    }

    /// <summary>
    /// Las dos hojas que tapan el hueco del techo de la Cresta. Al adaptarse a la
    /// oscuridad se despegan, se corren y entra la luz blanca del fin del eclipse.
    /// </summary>
    static void ConstruirTechoQueSeAbre(Kit k, Transform g, Referencias refs)
    {
        var raiz = Grupo(g, "TechoQueSeAbre");
        // no estáticas: se mueven
        var izq = Bloque(raiz, "Hoja_Izq", new Vector3(-3f, 8.15f, 87f), new Vector3(6f, 0.3f, 12f), k.techo, false);
        var der = Bloque(raiz, "Hoja_Der", new Vector3(3f, 8.15f, 87f), new Vector3(6f, 0.3f, 12f), k.techo, false);

        // la luz entra desde muy arriba; las hojas y el techo le hacen sombra, así cae por el hueco
        var luz = Luz(raiz, "Luz_Del_Cielo", new Vector3(0f, 16f, 87f), new Color(1f, 0.98f, 0.94f), 0f, 30f, true);
        luz.type = LightType.Spot;
        luz.spotAngle = 70f;
        luz.innerSpotAngle = 40f;
        luz.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var sonido = new GameObject("SonidoTecho").AddComponent<AudioSource>();
        sonido.transform.SetParent(raiz, false);
        sonido.transform.position = new Vector3(0f, 7.5f, 87f);
        ConfigurarAudio(sonido, k.audio.compuerta, 1f, false, true);
        sonido.maxDistance = 50f;

        var techo = raiz.gameObject.AddComponent<AperturaTecho>();
        Asignar(techo, "hojaIzquierda", izq.transform);
        Asignar(techo, "hojaDerecha", der.transform);
        Asignar(techo, "luzDelCielo", luz);
        Asignar(techo, "sonido", sonido);
        refs.techoCresta = techo;
    }

    /// <summary>
    /// Una losa que sube del piso y cierra el corredor a la espalda del jugador apenas
    /// entra a la Cresta: ya no se puede volver. Usa la misma Compuerta que el Umbral,
    /// sin receptores (la abre la zona de la Cresta) y moviéndose hacia arriba.
    /// </summary>
    static void ConstruirCierreCresta(Kit k, Transform g, Referencias refs)
    {
        var raiz = new GameObject("Cierre_Cresta");
        raiz.transform.SetParent(g, false);
        raiz.transform.position = new Vector3(0f, 0f, 74.85f);   // ahí suena
        // escondida bajo el piso; sube 6,3 m hasta meterse en el dintel
        Bloque(raiz.transform, "Losa", new Vector3(0f, -3.45f, 74.85f), new Vector3(4f, 6.3f, 0.3f), k.basaltoMedio, false);
        var sonido = raiz.AddComponent<AudioSource>();
        ConfigurarAudio(sonido, k.audio.compuerta, 0.9f, false, true);
        var cierre = raiz.AddComponent<Compuerta>();
        cierre.desplazamiento = new Vector3(0f, 6.3f, 0f);
        cierre.duracion = 2.2f;
        cierre.sonido = sonido;
        refs.cierreCresta = cierre;
    }

    // ================================================================== 06 Capilla (prólogo y epílogo)

    static void ConstruirCapilla(Kit k, Transform g, Referencias refs)
    {
        // el valle va arriba del nivel, con el cráter del valle justo sobre el pozo de la Explanada
        g.position = new Vector3(CentroPozo.x, AlturaValle, CentroPozo.z) - CraterEnLaCapilla;

        ConstruirEdificioCapilla(k, g);
        ConstruirPaisajeCapilla(k, g);

        // el guiño: adentro, sobre la puerta, la espiral tallada de las anclas. Sólo aparece
        // en el epílogo, frente a donde despierta el jugador: el cráter dejó una marca
        refs.guino = Motivo(k, g, "Guino_Tallado", g.TransformPoint(new Vector3(0f, 3.3f, 6.8f)), 180f, 0.45f, k.ambar);

        refs.spawnCapilla = new GameObject("SpawnCapilla").transform;
        refs.spawnCapilla.SetParent(g, false);
        refs.spawnCapilla.localPosition = new Vector3(0f, 0.05f, -4.5f);

        // el jugador empieza lejos, del otro lado del valle, mirando la capilla: es la meta. El
        // sol está arriba de ella. Caminando hacia ella, la luna tapa el sol; en el mirador
        // (el paso entre los dos cerros) llega la totalidad y la tierra se abre adelante, entre
        // él y la capilla. Sin cinemática: la cámara es siempre suya (PrologoCapilla).
        // En el epílogo despierta adentro de la capilla (SpawnCapilla)
        var spawnValle = new GameObject("SpawnValle").transform;
        spawnValle.SetParent(g, false);
        spawnValle.localPosition = new Vector3(0f, 0.05f, 72f);
        spawnValle.localRotation = Quaternion.LookRotation(Vector3.back);   // hacia la capilla
        refs.jugador.transform.SetPositionAndRotation(spawnValle.position, spawnValle.rotation);
        refs.spawnValle = spawnValle;
        refs.zonaMirador = Zona(g, "Zona_Mirador", g.TransformPoint(new Vector3(0f, 1.5f, 52f)), new Vector3(60f, 3f, 1.2f));
        // el umbral del atrio: en el epílogo, salir por el arco cierra la demo
        refs.zonaUmbralCapilla = Zona(g, "Zona_Umbral_Capilla", g.TransformPoint(new Vector3(0f, 1.5f, 15.9f)), new Vector3(9f, 3f, 0.8f));

        ConstruirCraterDelValle(k, g, refs);

        // epílogo: quedarse en el lugar donde estaba el cráter dispara los créditos
        refs.zonaFinal = Zona(g, "Zona_LugarDelCrater", g.TransformPoint(CraterEnLaCapilla + Vector3.up * 1.5f), new Vector3(16f, 3f, 16f));
        refs.zonaFinal.segundosDePermanencia = 5f;

        var solGO = new GameObject("Sol_Epilogo");
        solGO.transform.SetParent(g, false);
        // de mañana, arriba de la capilla vista desde el camino: delante del jugador
        solGO.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
        refs.sol = solGO.AddComponent<Light>();
        refs.sol.type = LightType.Directional;
        // mañana en el prólogo; en el epílogo CieloEclipse lo baja al atardecer
        refs.sol.color = new Color(1f, 0.95f, 0.86f);
        refs.sol.intensity = 1.7f;
        refs.sol.shadows = LightShadows.Soft;
        refs.sol.enabled = false;

        ConstruirCielo(k, g, refs);
    }

    /// <summary>
    /// El cráter que el eclipse revela en el valle, frente a la capilla: la boca del
    /// pozo de la Explanada. Arranca escondido: el borde bajo tierra y una tapa de
    /// tierra sobre el agujero. La puerta está en el borde, del lado de la capilla.
    /// </summary>
    static void ConstruirCraterDelValle(Kit k, Transform g, Referencias refs)
    {
        var crater = Grupo(g, "Crater_Valle");
        refs.craterValle = crater.gameObject;
        Vector3 centro = g.TransformPoint(CraterEnLaCapilla);
        const float radio = RadioPozo + 0.9f;
        const int piezas = 20;

        var azar = new System.Random(19);
        float Azar(float min, float max) => min + (float)azar.NextDouble() * (max - min);
        var bordes = new List<Transform>();
        for (int i = 0; i < piezas; i++)
        {
            float ang = i * 360f / piezas;
            // hueco hacia la capilla (sur): ahí está la puerta
            if (Mathf.Abs(Mathf.DeltaAngle(ang, 270f)) < 12f) continue;
            var dir = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad), 0f, Mathf.Sin(ang * Mathf.Deg2Rad));
            float alto = Azar(2.2f, 3.6f);
            var roca = Bloque(crater, $"Borde_{i:00}", centro + dir * Azar(radio - 0.2f, radio + 0.4f) + Vector3.up * (alto / 2f - 0.5f),
                new Vector3(Azar(2.6f, 3.4f), alto, Azar(1.8f, 2.6f)), i % 3 == 0 ? k.basaltoMedio : k.basalto, false);
            roca.transform.rotation = Quaternion.LookRotation(-dir) * Quaternion.Euler(Azar(-8f, 8f), Azar(-12f, 12f), Azar(-6f, 6f));
            bordes.Add(roca.transform);
        }
        bordes.AddRange(refs.paredesEscalera);
        refs.bordesCrater = bordes.ToArray();

        // la tapa: tierra sobre el agujero, se abre desde el centro. Está fuera del grupo
        // del cráter porque en el epílogo vuelve a cerrarse (y se puede pisar)
        var tapa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tapa.name = "Tapa_Pozo";
        Object.DestroyImmediate(tapa.GetComponent<Collider>());
        tapa.AddComponent<BoxCollider>();
        tapa.transform.SetParent(g, false);
        tapa.transform.position = centro + Vector3.up * -0.09f;
        tapa.transform.localScale = new Vector3(RadioPozo * 2f + 1.2f, 0.02f, RadioPozo * 2f + 1.2f);
        tapa.GetComponent<Renderer>().sharedMaterial = k.tierra;
        refs.tapaCrater = tapa.transform;

        // la puerta: dos pilares y un dintel de basalto en el borde sur, con el contorno que
        // destella. Del otro lado empieza la escalera que baja al fondo (ConstruirPozo)
        Vector3 umbral = centro + new Vector3(0f, 0f, -RadioPozo);
        var puerta = Grupo(crater, "Puerta");
        puerta.position = umbral;
        refs.puertaValle = puerta;
        Bloque(puerta, "Pilar_Izq", umbral + new Vector3(-0.95f, 1.5f, 0f), new Vector3(0.4f, 3.2f, 0.4f), k.basaltoMedio, false);
        Bloque(puerta, "Pilar_Der", umbral + new Vector3(0.95f, 1.5f, 0f), new Vector3(0.4f, 3.2f, 0.4f), k.basaltoMedio, false);
        Bloque(puerta, "Dintel", umbral + new Vector3(0f, 3.2f, 0f), new Vector3(2.3f, 0.4f, 0.45f), k.basalto, false);
        refs.contornoValle = new[]
        {
            Adorno(puerta, "Contorno_Izq", umbral + new Vector3(-0.72f, 1.45f, -0.05f), new Vector3(0.06f, 2.9f, 0.06f), k.puertaEclipse),
            Adorno(puerta, "Contorno_Der", umbral + new Vector3(0.72f, 1.45f, -0.05f), new Vector3(0.06f, 2.9f, 0.06f), k.puertaEclipse),
            Adorno(puerta, "Contorno_Dintel", umbral + new Vector3(0f, 2.93f, -0.05f), new Vector3(1.5f, 0.06f, 0.06f), k.puertaEclipse),
        };
        var destello = Plano(puerta, "Destello", k.destello);
        destello.transform.position = umbral + new Vector3(0f, 1.45f, -0.25f);
        refs.destelloValle = destello;

        // epílogo: el pasto aplastado donde estuvo
        var huella = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        huella.name = "Huella_Crater";
        Object.DestroyImmediate(huella.GetComponent<Collider>());
        huella.transform.SetParent(g, false);
        huella.transform.position = centro + Vector3.up * -0.065f;   // sobre la tapa cerrada
        huella.transform.localScale = new Vector3(RadioPozo * 2f, 0.01f, RadioPozo * 2f);
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
        // en el Umbral, la mitad sur queda libre para los murales que enseñan los controles
        Par(-7f, 6f, 0.62f, "Umbral");
        ConstruirMurales(k, arte);
        foreach (float z in new[] { 29f, 41f, 56f, 64f }) Par(z, 6f, 0.82f, "Cruce");
        // (la Cresta no lleva módulos: sus siluetas oscuras cortaban el reflejo de la linterna)

        // (en el fondo del pozo ya no hay murales a los lados del túnel: detrás de las paredes
        // de la escalera sólo se veían de canto y distraían entre los dibujos)

        // los tallados de la Cresta: ver ConstructorCrater.Murales.cs (TalladosDeLaCresta)
        TalladosDeLaCresta(k, Grupo(arte, "MotivosLatentes_Cresta"));
        // lo que se ve desde el fondo del pozo es el valle de verdad (ConstructorCrater.Capilla.cs)
    }

    static void Modulo(Kit k, Transform padre, string nombre, Vector3 posicion, float rotY, float escala, Material piedra)
    {
        var m = Instanciar(k.modeloArquitectura, padre, nombre);
        m.transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, rotY, 0f));
        m.transform.localScale = Vector3.one * escala;
        Pintar(m, r => r.name.Contains("Wall") ? k.basaltoMedio : piedra);
        Estatico(m);
    }

    static GameObject Motivo(Kit k, Transform padre, string nombre, Vector3 posicion, float rotY, float escala, Material material)
    {
        var m = Instanciar(k.modeloMotivos, padre, nombre);
        // el tallado está corrido 0,31 m del pivote: se centra
        var rot = Quaternion.Euler(0f, rotY, 0f);
        // separado de la pared: el relieve facetado (hasta 9 cm) lo tapaba en partes
        posicion += rot * Vector3.forward * 0.1f;
        m.transform.SetPositionAndRotation(posicion + rot * new Vector3(0.31f * escala, 0f, 0f), rot);
        m.transform.localScale = Vector3.one * escala;
        Pintar(m, _ => material);
        foreach (var r in m.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        Estatico(m);
        return m;
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
        // los logos de los créditos: si están las imágenes, se usan; si no, se arman en texto
        // (cualquier imagen de Assets/Art/Creditos cuyo nombre diga "fadu" o "uba", y "campos" o "catedra")
        Asignar(interfaz, "logoFacultad", BuscarLogo("fadu", "uba"));
        Asignar(interfaz, "logoCatedra", BuscarLogo("campos", "catedra", "cátedra"));
        Asignar(pausa, "interfaz", interfaz);
        Asignar(flujo, "linterna", linterna);
        Asignar(flujo, "interfaz", interfaz);
        Asignar(flujo, "eclipse", eclipse);
        Asignar(flujo, "compuertaUmbral", refs.compuertaUmbral);
        Asignar(eclipse, "adaptacion", adaptacion);
        Asignar(eclipse, "linterna", linterna);
        Asignar(eclipse, "interfaz", interfaz);
        var latentes = GameObject.Find("MotivosLatentes_Cresta");
        if (latentes != null) AsignarLista(eclipse, "arteCresta", latentes.GetComponentsInChildren<Renderer>());
        Asignar(eclipse, "flujo", flujo);
        Asignar(eclipse, "pausa", pausa);
        Asignar(eclipse, "jugador", refs.jugador.transform);
        Asignar(eclipse, "spawnCapilla", refs.spawnCapilla);
        Asignar(eclipse, "luzDelCrater", refs.luna);
        Asignar(eclipse, "solEpilogo", refs.sol);
        Asignar(eclipse, "ambienteCrater", ambiente);
        Asignar(eclipse, "ambienteExterior", exterior);
        Asignar(eclipse, "techo", refs.techoCresta);
        Asignar(eclipse, "prologo", prologo);
        Asignar(eclipse, "cielo", refs.cielo);
        Asignar(eclipse, "tonoFinal", tono);
        Asignar(eclipse, "campana", campana);
        Asignar(eclipse, "musicaCreditos", musica);
        Asignar(refs.jugador.GetComponent<RespawnPorCaida>(), "interfaz", interfaz);

        Asignar(prologo, "jugador", refs.jugador.GetComponent<JugadorFPS>());
        Asignar(prologo, "cielo", refs.cielo);
        Asignar(prologo, "inicioCamino", refs.spawnValle);
        Asignar(prologo, "mirador", refs.zonaMirador.transform);
        Asignar(prologo, "eclipse", eclipse);
        Asignar(prologo, "flujo", flujo);
        Asignar(prologo, "interfaz", interfaz);
        Asignar(prologo, "spawnCrater", refs.spawnInicio);
        Asignar(prologo, "crater", refs.craterValle);
        AsignarLista(prologo, "bordes", refs.bordesCrater);
        Asignar(prologo, "tapa", refs.tapaCrater);
        Asignar(prologo, "puerta", refs.puertaValle);
        AsignarLista(prologo, "contornoPuerta", refs.contornoValle);
        Asignar(prologo, "destello", refs.destelloValle);
        Asignar(prologo, "huella", refs.huella);
        Asignar(prologo, "zonaEpilogo", refs.zonaFinal.gameObject);
        Asignar(prologo, "guino", refs.guino);
        Asignar(prologo, "pajaros", pajaros);
        Asignar(prologo, "graveEclipse", grave);
        Asignar(prologo, "campana", campana);
        Asignar(prologo, "sonidoRevelado", revelado);
        Asignar(prologo, "sonidoDestello", destelloPuerta);

        UnityEventTools.AddPersistentListener(refs.compuertaUmbral.alAbrirse, new UnityAction(flujo.NotificarUmbralAbierto));
        UnityEventTools.AddPersistentListener(refs.puertaSellos.alAbrirse, new UnityAction(flujo.EntrarCruce));

        // al encender cada sello, la cámara va a la rotonda a ver cómo se prende su hilo
        var cinematica = sistemas.AddComponent<CinematicaDeSello>();
        Asignar(cinematica, "jugador", refs.jugador.GetComponent<JugadorFPS>());
        Asignar(cinematica, "interfaz", interfaz);
        Asignar(cinematica, "pausa", pausa);
        Asignar(cinematica, "linterna", linterna);
        AsignarLista(cinematica, "sellos", new Object[] { refs.selloOeste, refs.selloEste });
        AsignarLista(cinematica, "hilos", new Object[] { refs.hiloSoles, refs.hiloLunas });
        Asignar(cinematica, "eclipse", refs.hiloEclipse);
        Asignar(cinematica, "puerta", refs.puertaSellos);
        Asignar(cinematica, "obelisco", refs.obelisco);
        UnityEventTools.AddPersistentListener(refs.atajoOeste.alAbrirse, new UnityAction(flujo.NotificarSello));
        UnityEventTools.AddPersistentListener(refs.atajoEste.alAbrirse, new UnityAction(flujo.NotificarSello));
        UnityEventTools.AddPersistentListener(refs.zonaCresta.alEntrar, new UnityAction(flujo.EntrarCresta));
        // la Compuerta "se abre" hacia arriba: acá eso es cerrar el corredor
        UnityEventTools.AddPersistentListener(refs.zonaCresta.alEntrar, new UnityAction(refs.cierreCresta.Abrir));
        UnityEventTools.AddPersistentListener(refs.zonaFinal.alEntrar, new UnityAction(eclipse.CerrarDemo));
        UnityEventTools.AddPersistentListener(adaptacion.alAdaptarse, new UnityAction(eclipse.AlCompletarAdaptacion));
        UnityEventTools.AddPersistentListener(refs.zonaMirador.alEntrar, new UnityAction(prologo.IniciarCinematica));
        // en el epílogo, salir del atrio también cierra la demo (en el prólogo CerrarDemo no hace nada)
        UnityEventTools.AddPersistentListener(refs.zonaUmbralCapilla.alEntrar, new UnityAction(eclipse.CerrarDemo));
        refs.zonaUmbralCapilla.unaVez = false;
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

    /// <summary>
    /// Las anclas que abren puertas (no las de los puentes) se paran sobre una base de piedra
    /// clara de dos escalones: se distinguen de un vistazo, "esta abre algo".
    /// </summary>
    static void BaseDePuerta(Kit k, Ancla ancla)
    {
        var t = ancla.transform;
        var base_ = Grupo(t.parent, ancla.name + "_Base");
        var abajo = Bloque(base_, "Escalon_Abajo", t.position + Vector3.up * 0.06f, new Vector3(2.6f, 0.12f, 1.9f), k.piedra);
        var arriba = Bloque(base_, "Escalon_Arriba", t.position + Vector3.up * 0.18f, new Vector3(2.1f, 0.12f, 1.5f), k.piedra);
        abajo.transform.rotation = t.rotation;
        arriba.transform.rotation = t.rotation;
    }

    /// <summary>
    /// El ancla del Umbral es de luz blanca: no se parece a las del filtro sol. Piedra clara,
    /// aros blancos y, arriba, una lámpara colgada del techo que la ilumina y se ve de lejos.
    /// </summary>
    static void AnclaDeLuzBlanca(Kit k, Transform g, Ancla ancla)
    {
        var blanco = new Color(0.85f, 0.92f, 1f);
        ancla.colorApagada = new Color(0.1f, 0.11f, 0.13f);
        ancla.colorEncendida = blanco;
        if (ancla.brillo != null) { ancla.brillo.color = blanco; PrefabUtility.RecordPrefabInstancePropertyModifications(ancla.brillo); }
        PrefabUtility.RecordPrefabInstancePropertyModifications(ancla);
        var acentos = new HashSet<Renderer>(ancla.acentos ?? new Renderer[0]);
        foreach (var r in ancla.GetComponentsInChildren<Renderer>(true))
            if (!acentos.Contains(r)) { r.sharedMaterial = k.piedra; PrefabUtility.RecordPrefabInstancePropertyModifications(r); }

        // la lámpara: una cadena del techo y una piedra blanca que brilla, sobre el ancla
        var arriba = ancla.transform.position + Vector3.up * 3.55f + Vector3.right * 0.3f;
        var lampara = Grupo(g, "Lampara_Ancla_Umbral");
        Bloque(lampara, "Cadena", arriba + Vector3.up * 0.24f, new Vector3(0.03f, 0.42f, 0.03f), k.metal);
        var piedra = Bloque(lampara, "Luz", arriba, Vector3.one * 0.22f, k.luzEclipse);
        piedra.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
        Object.DestroyImmediate(piedra.GetComponent<Collider>());
        var luz = Luz(lampara, "Luz_Lampara", arriba - Vector3.up * 0.3f, blanco, 18f, 6f, true);
        luz.type = LightType.Spot;
        luz.spotAngle = 70f;
        luz.innerSpotAngle = 35f;
        luz.transform.rotation = Quaternion.LookRotation(ancla.PuntoDeImpacto - luz.transform.position);
    }

    /// <summary>
    /// La primera imagen (png o jpg) cuyo nombre contenga alguna de las palabras. Se busca
    /// primero en Assets/Art/Creditos y después en todo Assets. Se usa como textura (RawImage):
    /// no hace falta tocar cómo se importa. Si no hay, avisa en la Consola.
    /// </summary>
    static Texture2D BuscarLogo(params string[] palabras)
    {
        foreach (var carpetas in new[] { new[] { "Assets/Art/Creditos" }, new[] { "Assets" } })
        {
            if (!AssetDatabase.IsValidFolder(carpetas[0])) continue;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", carpetas))
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guid);
                string nombre = System.IO.Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant();
                foreach (var palabra in palabras)
                {
                    if (!nombre.Contains(palabra)) continue;
                    var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
                    if (textura == null) continue;
                    Debug.Log($"[CRÁTER] Logo de los créditos: {ruta}");
                    return textura;
                }
            }
        }
        Debug.LogWarning($"[CRÁTER] No encontré el logo \"{palabras[0]}\" para los créditos: poné la imagen en Assets/Art/Creditos. Por ahora va en texto.");
        return null;
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
