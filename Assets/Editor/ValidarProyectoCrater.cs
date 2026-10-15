using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Chequeos no destructivos antes de entregar o compilar: que la escena esté
/// completa, bien conectada y que cada puzzle se pueda resolver desde el piso.
///
/// CÓMO FUNCIONA
/// Abre la escena, junta todos sus objetos y revisa reglas: que existan las piezas
/// clave, que los eventos tengan a quién avisar, que las anclas estén al alcance de
/// su puente, etc. Cada regla que falla suma un mensaje a 'problemas'; al final se
/// escriben en la Consola. No modifica nada. Corre solo después de "Reconstruir todo".
/// </summary>
public static class ValidarProyectoCrater
{
    [MenuItem("Crater/Validar proyecto", priority = 10)]
    public static void ValidarDesdeMenu()
    {
        int errores = Validar(true);
        EditorUtility.DisplayDialog("CRÁTER", errores == 0
            ? "El proyecto superó todos los chequeos automáticos."
            : $"Se encontraron {errores} problemas. Revisá la Consola.", "Aceptar");
    }

    public static void ValidarDesdeLineaDeComandos()
    {
        int errores = Validar(true);
        if (errores > 0) throw new BuildFailedException($"CRÁTER tiene {errores} errores de validación.");
    }

    public static int Validar(bool escribirLog)
    {
        var problemas = new List<string>();

        Comprobar(AssetDatabase.LoadAssetAtPath<SceneAsset>(ConstructorCrater.RutaEscena) != null,
            $"Falta la escena: {ConstructorCrater.RutaEscena}", problemas);
        var escenasBuild = EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
        Comprobar(escenasBuild.Length == 1 && escenasBuild[0] == ConstructorCrater.RutaEscena,
            "Build Settings debe contener únicamente la escena del vertical slice.", problemas);

        var cuerpo = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(ConstructorCrater.RutaFiltroCuerpo);
        var hueco = AssetDatabase.LoadAssetAtPath<FiltroDefinicion>(ConstructorCrater.RutaFiltroHueco);
        Comprobar(cuerpo != null && cuerpo.canal == FiltroDefinicion.Canal.Cuerpo,
            "El filtro SOL falta o está mal configurado.", problemas);
        Comprobar(hueco != null && hueco.canal == FiltroDefinicion.Canal.Hueco,
            "El filtro LUNA falta o está mal configurado.", problemas);

        foreach (var capa in new[] { ConstructorCrater.CapaAncla, ConstructorCrater.CapaReceptor, ConstructorCrater.CapaJugador })
            Comprobar(LayerMask.NameToLayer(capa) >= 0, $"Falta la capa {capa}.", problemas);

        var perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ConstructorCrater.RutaPerfil);
        Comprobar(perfil != null && perfil.Has<ColorAdjustments>(),
            "El perfil de post-procesado no tiene Color Adjustments (lo necesita la adaptación a la oscuridad).", problemas);
        Comprobar(perfil != null && perfil.TryGet<Bloom>(out var bloom) && bloom.threshold.value == 1f,
            "Bloom Threshold debe ser 1,0.", problemas);

        var jugador = AssetDatabase.LoadAssetAtPath<GameObject>(ConstructorCrater.CarpetaPrefabs + "Jugador.prefab");
        Comprobar(jugador != null && jugador.CompareTag("Player") && jugador.GetComponentInChildren<LinternaController>(true) != null,
            "El prefab Jugador falta, no tiene tag Player o no tiene linterna.", problemas);

        Comprobar(jugador != null && jugador.TryGetComponent<JugadorFPS>(out var movimiento) && movimiento.velocidad == 4f && movimiento.multiplicadorCorrer == 1.4f,
            "La velocidad base debe ser 4 m/s y R2 / RT debe multiplicarla por 1,4.", problemas);
        var linternaPrefab = jugador != null ? jugador.GetComponentInChildren<LinternaController>(true) : null;
        Comprobar(linternaPrefab != null && linternaPrefab.duracionFiltro == 2f,
            "Los filtros deben durar 2 segundos efectivos.", problemas);

        Comprobar(linternaPrefab != null && linternaPrefab.anguloBase == 54f && linternaPrefab.anguloVisualFiltro == 44f &&
            linternaPrefab.proporcionNucleo <= .2f && cuerpo != null && hueco != null && cuerpo.anguloCono == 28f && hueco.anguloCono == 28f,
            "La linterna debe suavizar su haz sin ensanchar la activación de los filtros.", problemas);
        if (System.IO.File.Exists(ConstructorCrater.RutaEscena))
            ValidarEscena(cuerpo, hueco, problemas);

        if (escribirLog)
        {
            if (problemas.Count == 0) Debug.Log("[CRÁTER] Validación completa: sin errores.");
            else foreach (string problema in problemas) Debug.LogError("[CRÁTER] " + problema);
        }
        return problemas.Count;
    }

    static void ValidarEscena(FiltroDefinicion cuerpo, FiltroDefinicion hueco, List<string> problemas)
    {
        Scene escena = SceneManager.GetSceneByPath(ConstructorCrater.RutaEscena);
        bool yaCargada = escena.IsValid() && escena.isLoaded;
        if (!yaCargada) escena = EditorSceneManager.OpenScene(ConstructorCrater.RutaEscena, OpenSceneMode.Additive);
        try
        {
            var objetos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            T[] Todos<T>() where T : Component => objetos.Select(go => go.GetComponent<T>()).Where(c => c != null).ToArray();
            ValidarMurales(objetos, problemas);
            ValidarPlaytest(objetos, problemas);

            foreach (GameObject go in objetos)
                Comprobar(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) == 0,
                    $"{go.name} tiene un script faltante.", problemas);

            var jugadores = objetos.Where(go => go.CompareTag("Player")).ToArray();
            Comprobar(jugadores.Length == 1, "Tiene que haber exactamente un objeto con tag Player.", problemas);
            Comprobar(Todos<Camera>().Length == 1 && Todos<AudioListener>().Length == 1,
                "Tiene que haber exactamente una cámara y un AudioListener.", problemas);

            var linternas = Todos<LinternaController>();
            Comprobar(linternas.Length == 1, "Tiene que haber exactamente una linterna.", problemas);
            var linterna = linternas.FirstOrDefault();
            if (linterna != null)
            {
                Comprobar(linterna.filtros.Count == 2 && linterna.filtros[0] == cuerpo && linterna.filtros[1] == hueco,
                    "La linterna tiene que tener SOL (1) y LUNA (2), en ese orden.", problemas);
                Comprobar(linterna.filtrosDesbloqueados.Count == 0, "La linterna no puede empezar con filtros desbloqueados.", problemas);
                Comprobar(linterna.requiereRecogerla, "La linterna tiene que empezar sin recoger.", problemas);
            }

            var colecciones=Todos<ColeccionPiedras>();
            Comprobar(colecciones.Length==1,"Debe haber una colección opcional.",problemas);
            var coleccion=colecciones.FirstOrDefault();var piezas=Todos<PiezaTallada>();
            Comprobar(piezas.Length==6 && piezas.Select(p=>p.identificador).Distinct().Count()==6 && piezas.Select(p=>p.indice).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,6)),"Las seis piedras necesitan IDs e índices únicos.",problemas);
            if(coleccion!=null)
            {
                Comprobar(coleccion.piezas!=null && coleccion.piezas.Length==6 && coleccion.piezas.All(p=>p!=null && p.coleccion==coleccion),"Referencias de piedras incompletas.",problemas);
                Comprobar(coleccion.conjunto!=null && coleccion.conjunto.fragmentos.Length==6 && coleccion.conjunto.fragmentos.All(r=>r!=null) && coleccion.conjunto.proyeccion!=null && coleccion.conjunto.nota!=null && coleccion.conjunto.voces!=null && coleccion.conjunto.voces.Length==6 && coleccion.conjunto.voces.All(v=>v!=null),"Conjunto opcional incompleto.",problemas);
                Comprobar(coleccion.jugador!=null && coleccion.camara!=null && coleccion.interfaz!=null && coleccion.flujo!=null && coleccion.prologo!=null,"La colección debe respetar controles y flujo.",problemas);
            }
            Comprobar(Todos<AguaEnCuenco>().Length==1 && Todos<AguaEnCuenco>().All(a=>a.sonda!=null && a.sonda.resolution==64),"Falta el reflejo limitado a 64px.",problemas);
            Comprobar(Todos<ParticleSystem>().Where(p=>p.name=="Polvo_Abertura").All(p=>p.main.maxParticles<=20),"Polvo por encima del presupuesto.",problemas);
            var recogibles = Todos<Recogible>();
            Comprobar(recogibles.Count(r => r.filtro == null) == 1, "Tiene que haber una única linterna para recoger.", problemas);
            Comprobar(recogibles.Count(r => r.filtro == cuerpo) == 1 && recogibles.Count(r => r.filtro == hueco) == 1,
                "Tiene que haber un filtro SOL y un filtro LUNA para recoger.", problemas);

            foreach (var puente in Todos<PuenteLuz>())
            {
                Comprobar(puente.anclas.Count > 0 && puente.anclas.All(a => a != null),
                    $"{puente.name} no tiene anclas asignadas.", problemas);
                Comprobar(puente.colliders != null && puente.colliders.Length > 0, $"{puente.name} no tiene collider.", problemas);
                if (puente.anclas.Count == 2 && puente.anclas.All(a => a != null))
                    Comprobar(Vector3.Distance(puente.anclas[0].PuntoDeImpacto, puente.anclas[1].PuntoDeImpacto) >= 6f,
                        puente.name + " necesita anclas separadas para exigir apuntar a cada una.", problemas);
                float alcance = cuerpo != null ? cuerpo.alcance : 14f;
                foreach (var ancla in puente.anclas.Where(a => a != null))
                {
                    Comprobar(ancla.canalRequerido == FiltroDefinicion.Canal.Cuerpo,
                        $"{ancla.name} sostiene un puente pero no responde al filtro SOL.", problemas);
                    Comprobar(Vector3.Distance(ancla.PuntoDeImpacto, puente.transform.position) < alcance - 2f,
                        $"{ancla.name} queda demasiado lejos de {puente.name} para alcanzarla con el haz.", problemas);
                }
            }

            foreach (var reja in Todos<MateriaHueca>())
            {
                Comprobar(reja.canalRequerido == FiltroDefinicion.Canal.Hueco, $"{reja.name} no responde al filtro LUNA.", problemas);
                var colliders = reja.GetComponents<Collider>();
                Comprobar(colliders.Any(c => c.isTrigger) && colliders.Any(c => !c.isTrigger),
                    $"{reja.name} necesita un collider sólido y un trigger en el mismo objeto.", problemas);
            }

            foreach (var ancla in Todos<Ancla>())
                Comprobar(LayerMask.LayerToName(ancla.gameObject.layer) == ConstructorCrater.CapaAncla,
                    $"{ancla.name} no está en la capa Ancla.", problemas);

            // todas las compuertas se abren con receptores, salvo el cierre de la Cresta (lo cierra la zona)
            var compuertas = Todos<Compuerta>();
            Compuerta Compuerta(string nombre) => compuertas.FirstOrDefault(c => c.name == nombre);
            Comprobar(compuertas.Where(c => c.name != "Cierre_Cresta" && !c.name.StartsWith("Losa_Red_")).All(c => c.receptores.Count > 0 && c.receptores.All(r => r != null)),
                "Hay una compuerta sin receptores (sólo el cierre de la Cresta puede no tenerlos).", problemas);
            Comprobar(Compuerta("Cierre_Cresta") != null, "Falta el cierre de la Cresta.", problemas);
            foreach (var nombre in new[] { "Compuerta_Umbral", "Puerta_Sellos", "Atajo_Oeste", "Atajo_Este" })
                Comprobar(Compuerta(nombre) != null && TieneOyentes(Compuerta(nombre).alAbrirse), $"Falta {nombre} o no avisa al flujo del juego.", problemas);
            var sellos = Compuerta("Puerta_Sellos");
            Comprobar(sellos != null && sellos.receptores.Count == 2 && sellos.receptores.All(r => r != null && r.permanente),
                "La puerta de los sellos tiene que abrirse con los dos sellos, y los sellos tienen que ser permanentes.", problemas);

            var obeliscos=Todos<ObeliscoDelEclipse>();var obelisco=obeliscos.FirstOrDefault();
            Comprobar(obeliscos.Length==1 && obelisco.jugador!=null && obelisco.puerta==sellos &&
                obelisco.sol!=null && obelisco.luna!=null && obelisco.sol.manual && obelisco.luna.manual &&
                obelisco.sol.sello!=null && obelisco.luna.sello!=null && obelisco.sol.sello!=obelisco.luna.sello &&
                obelisco.eclipse!=null && obelisco.eclipseRegreso!=null && obelisco.luz!=null && obelisco.resonanciaSol!=null && obelisco.resonanciaLuna!=null,
                "El obelisco debe comunicar ambos sellos y abrir el Cruce con referencias completas.",problemas);
            if (obelisco != null)
            {
                foreach (bool luna in new[] { false, true })
                {
                    var columnas = luna ? obelisco.columnasLuna : obelisco.columnasSol;
                    var notas = luna ? obelisco.notasLuna : obelisco.notasSol;
                    var cara = luna ? obelisco.luna : obelisco.sol;
                    Comprobar(columnas != null && columnas.Length == 3 && columnas.All(c => c != null && c.manual &&
                        cara != null && c.sello == cara.sello && c.luz != null && c.renderers != null && c.renderers.Length > 0 && c.renderers.All(r => r != null)) &&
                        notas != null && notas.Length == columnas.Length && notas.All(n => n != null && n.clip != null &&
                        !n.loop && !n.playOnAwake && n.spatialBlend == 1f) &&
                        columnas.All(c => c.luz.type == LightType.Spot && c.luz.transform.position.y < 1f),
                        "Cada sello debe encender tres columnas distintas con una nota localizada por columna.", problemas);
                }
                Comprobar(obelisco.columnasSol != null && obelisco.columnasLuna != null &&
                    obelisco.columnasSol.Concat(obelisco.columnasLuna).Distinct().Count() == 6,
                    "Las secuencias SOL/LUNA no deben compartir ni duplicar columnas.", problemas);
            }
            Comprobar(Todos<CinematicaDeSello>().Length==0 && Todos<HiloDeTallados>().Length==0,
                "La escena vigente no debe tomar la cámara ni encender los hilos antiguos al regresar.",problemas);
            if (obelisco != null)
            {
                var columnas = Todos<Renderer>().Where(r => r.name.StartsWith("Anillo_Columna_") && r.GetComponent<Collider>() != null).ToArray();
                Vector3 centro = obelisco.centro;
                Comprobar(columnas.Length == 6 && columnas.All(r => {
                    Vector3 p = r.bounds.center; p.y = centro.y;
                    Vector3 espejo = new Vector3(-p.x, p.y, p.z);
                    return Mathf.Abs(Vector3.Distance(p, centro) - 6.5f) < .03f && columnas.Any(c => {
                        Vector3 q = c.bounds.center; q.y = centro.y;
                        return Vector3.Distance(q, espejo) < .03f;
                    });
                }), "Las seis columnas deben ser simétricas respecto al eje de la rotonda y estar a 6,5 m del centro.", problemas);
            }
            var atajosInicio = Todos<AtajoOcultoInicio>();
            Comprobar(atajosInicio.Length == 1 && atajosInicio[0].jugador != null && atajosInicio[0].destino != null &&
                atajosInicio[0].flujo != null && TieneOyentes(atajosInicio[0].alLlegar) &&
                atajosInicio[0].GetComponent<Collider>().isTrigger && atajosInicio[0].GetComponentsInChildren<Renderer>().Length == 0,
                "El atajo inicial debe ser invisible, tener destino seguro y conservar el evento del mirador.", problemas);

            // Cresta, mirador, pozo, lugar del cráter y escaleras de recuperación
            var zonas = Todos<ZonaJugador>();
            Comprobar(zonas.Length == 8 && zonas.All(z => TieneOyentes(z.alEntrar) && z.GetComponent<Collider>().isTrigger),
                "Tiene que haber 8 zonas conectadas: Cresta, mirador, pozo, epílogo y cuatro escaleras de recuperación.", problemas);
            foreach (string n in new[] { "O1", "O3", "N1", "N2" })
                Comprobar(Compuerta("Losa_Red_" + n) != null && zonas.Any(z => z.name == "Pie_Escalera_" + n),
                    $"Falta la recuperación de {n}.", problemas);
            Comprobar(Todos<PuenteLuz>().All(p => p.anclas.Count == 2), "Cada puente debe tener dos anclas.", problemas);
            var puertaSol = Compuerta("Puerta_O_Ancla");
            Comprobar(puertaSol != null && !puertaSol.sostenida && puertaSol.receptores.Count == 1,
                "La puerta del ala SOL debe tener una ancla y quedar abierta.", problemas);

            Comprobar(Todos<PrologoCapilla>().Length == 1 && Todos<CieloEclipse>().Length == 1,
                "Falta el prólogo de la capilla o el cielo del eclipse.", problemas);
            Comprobar(Todos<AperturaTecho>().Length == 1, "Falta el techo que se abre en la Cresta.", problemas);

            var adaptacion = Todos<AdaptacionOscuridad>().FirstOrDefault();
            Comprobar(adaptacion != null && adaptacion.GetComponent<Volume>() != null && TieneOyentes(adaptacion.alAdaptarse),
                "La adaptación a la oscuridad falta, no está en el Volume o no dispara el final.", problemas);

            Comprobar(Todos<FlujoJuegoCrater>().Length == 1 && Todos<EclipseFinalController>().Length == 1 &&
                      Todos<InterfazCrater>().Length == 1 && Todos<PausaCrater>().Length == 1,
                "Faltan sistemas (flujo, eclipse, interfaz o pausa).", problemas);
        }
        finally
        {
            if (!yaCargada) EditorSceneManager.CloseScene(escena, true);
        }
    }

    static void ValidarPlaytest(GameObject[] objetos, List<string> problemas)
    {
        var atajo = objetos.Select(o => o.GetComponent<AtajoOcultoInicio>()).FirstOrDefault(c => c != null);
        Comprobar(atajo != null && !atajo.habilitado, "El atajo oculto debe estar desactivado en playtest.", problemas);
        var mirador = objetos.FirstOrDefault(o => o.name == "Zona_Mirador");
        Comprobar(mirador != null && Mathf.Abs(mirador.GetComponent<BoxCollider>().size.x * mirador.transform.lossyScale.x - 88f) < .001f,
            "Zona_Mirador debe medir 88 m.", problemas);
        var apoyo = objetos.FirstOrDefault(o => o.name == "Apoyo_Isla_Cruce");
        Comprobar(apoyo != null && Mathf.Abs(apoyo.transform.lossyScale.y - .30f) < .001f, "El apoyo del Cruce debe medir 0,30 m.", problemas);
        var espejos = objetos.Select(o => o.GetComponent<ParedEspejo>()).Where(c => c != null).ToArray();
        Comprobar(espejos.Length == 6 && espejos.All(p => new SerializedObject(p).FindProperty("intensidadReflejo").floatValue == 4f),
            "Los seis reflejos deben tener intensidad 4.", problemas);
        var encuadre = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Pintura_Encuadre.mat");
        Comprobar(encuadre != null && encuadre.IsKeywordEnabled("_EMISSION") && encuadre.GetColor("_EmissionColor").maxColorComponent == 0f,
            "Pintura_Encuadre requiere _EMISSION y emisión negra en reposo.", problemas);
        foreach (string nombre in new[] { "Rendija_Puerta_Sellos", "Linea_Umbral_Cresta" })
        {
            var guia = objetos.FirstOrDefault(o => o.name == nombre)?.GetComponent<Renderer>();
            Comprobar(guia != null && guia.sharedMaterial != null && guia.sharedMaterial.IsKeywordEnabled("_EMISSION") &&
                guia.sharedMaterial.GetColor("_EmissionColor").maxColorComponent > 0f,
                nombre + " debe conservar emisión porque guía al jugador.", problemas);
        }
        foreach (var go in objetos.Where(o => o.name == "Tallado_Sello_Este" ||
            o.name == "Vela_Entrada" || (o.name == "Tallado" && o.transform.parent?.name == "Compuerta_Umbral")))
            Comprobar(go.GetComponentsInChildren<Renderer>(true).All(r => r.sharedMaterials.All(m => m != null && !m.IsKeywordEnabled("_EMISSION"))),
                go.name + " debe ser mate.", problemas);
        var tono = objetos.SelectMany(o => o.GetComponents<AudioSource>()).Where(a => a.clip != null && a.clip.name == "anillo_diamante").ToArray();
        Comprobar(tono.Length == 1 && Mathf.Approximately(tono[0].volume, .4f), "El anillo de diamante debe sonar a volumen 0,4.", problemas);
        var puerta = objetos.FirstOrDefault(o => o.name == "Puerta_Sellos")?.GetComponent<Compuerta>();
        Comprobar(puerta != null && puerta.sonido != null && (!GeneradorAudioCrater.EsSintetizado(puerta.sonido.clip) ||
            puerta.sonido.GetComponent<GravesCompuerta>() != null), "Falta el cuerpo grave de la compuerta sintetizada del Cruce.", problemas);
        Comprobar(!objetos.SelectMany(o => o.GetComponents<Light>()).Any(l => ConstructorCrater.EsRellenoSinFuente(l.name)),
            "No deben regenerarse luces de relleno flotantes sin fuente visible.", problemas);
        foreach (var nombre in new[] { "Lampara_Ancla_Umbral", "Lampara_Ancla_O_Puerta", "Lampara_Sello_Oeste" })
            Comprobar(objetos.Count(o => o.name == nombre) == 1,
                "Falta o está duplicada la lámpara de puerta: " + nombre, problemas);
        var lampara = objetos.FirstOrDefault(o => o.name == "Lampara_Ancla_Umbral");
        var luzLampara = lampara != null ? lampara.transform.Find("Luz")?.GetComponent<Renderer>() : null;
        Comprobar(luzLampara != null && luzLampara.sharedMaterial != null && luzLampara.sharedMaterial.IsKeywordEnabled("_EMISSION"),
            "La lámpara funcional del ancla del Umbral debe conservar emisión.", problemas);
        var obelisco = objetos.Select(o => o.GetComponent<ObeliscoDelEclipse>()).FirstOrDefault(c => c != null);
        Comprobar(obelisco != null && obelisco.eclipse != null && obelisco.eclipse.sharedMaterial.IsKeywordEnabled("_EMISSION") &&
            obelisco.fundidoResonancia >= .5f && obelisco.fundidoResonancia <= 1f,
            "El obelisco debe conservar emisión y un fundido de resonancia entre 0,5 y 1 s.", problemas);
    }

    static void ValidarMurales(GameObject[] objetos, List<string> problemas)
    {
        for (int i = 1; i <= 8; i++)
        {
            string nombre = "Lore_" + i;
            var losas = objetos.Where(o => o.name == nombre).ToArray();
            Comprobar(losas.Length == 1, $"Debe existir una única losa {nombre}.", problemas);
            if (losas.Length != 1) continue;
            var losa = losas[0].transform;
            var cara = losa.Find("PinturaMural");
            var malla = cara != null ? cara.GetComponent<MeshFilter>()?.sharedMesh : null;
            var material = cara != null ? cara.GetComponent<Renderer>()?.sharedMaterial : null;
            string imagen = $"mural_explanada_{(i <= 4 ? "izq" : "der")}_{(i - 1) % 4 + 1:00}";
            string ruta = $"Assets/Art/Murals/{imagen}.png";
            Comprobar(losa.childCount == 2 && losa.Find("Losa_Piedra") != null && malla != null && malla.vertexCount == 4,
                $"{nombre}: conservar piedra y un único quad pintado.", problemas);
            Comprobar(malla != null && Mathf.Abs(malla.bounds.size.x / malla.bounds.size.y - 1.5f) < .001f,
                $"{nombre}: la cara debe conservar la proporción 3:2.", problemas);
            Comprobar(material != null && material.shader.name == "Universal Render Pipeline/Lit" &&
                AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")) == ruta &&
                material.GetColor("_BaseColor") == Color.white && material.GetFloat("_Smoothness") == .05f &&
                material.GetFloat("_Metallic") == 0f && !material.IsKeywordEnabled("_EMISSION") &&
                material.GetColor("_EmissionColor").maxColorComponent == 0f,
                $"{nombre}: PNG incorrecto o PinturaMural no es mate/sin emisión.", problemas);
            var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            Comprobar(importador != null && importador.sRGBTexture && importador.mipmapEnabled && importador.npotScale == TextureImporterNPOTScale.None &&
                importador.wrapMode == TextureWrapMode.Clamp && importador.filterMode == FilterMode.Bilinear &&
                importador.maxTextureSize == 2048 && importador.textureCompression == TextureImporterCompression.CompressedHQ,
                $"{nombre}: import settings fuera de la especificación.", problemas);
        }
    }

    static bool TieneOyentes(UnityEventBase evento) => evento != null && evento.GetPersistentEventCount() > 0;

    static void Comprobar(bool condicion, string mensaje, ICollection<string> problemas)
    {
        if (!condicion) problemas.Add(mensaje);
    }
}
