using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Recorre la escena real de punta a punta como lo haría un jugador: camina con
/// el CharacterController, apunta la linterna y cruza cada puzzle. Si alguien
/// mueve una pieza del nivel y un puzzle deja de poder resolverse, falla acá.
///
/// CÓMO FUNCIONA
/// Es un [UnityTest]: una corrutina que corre en Play, frame a frame. Carga la escena
/// real, acelera el tiempo ×2 y "juega": camina moviendo el CharacterController hacia
/// puntos del recorrido, apunta la cámara a las anclas y rejas, equipa filtros y espera.
/// Después de cada tramo comprueba con Assert que la etapa del juego avanzó.
/// Se corre desde Window > General > Test Runner > PlayMode.
/// </summary>
public class RecorridoCraterTests
{
    const float Velocidad = 2.6f;

    GameObject jugador;
    CharacterController cc;
    JugadorFPS fps;
    LinternaController linterna;
    FlujoJuegoCrater flujo;
    PrologoCapilla prologo;

    [UnitySetUp]
    public IEnumerator Cargar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");
        yield return null;
        jugador = GameObject.FindWithTag("Player");
        cc = jugador.GetComponent<CharacterController>();
        fps = jugador.GetComponent<JugadorFPS>();
        linterna = LinternaController.Instancia;
        flujo = Object.FindFirstObjectByType<FlujoJuegoCrater>();
        prologo = Object.FindFirstObjectByType<PrologoCapilla>();
        var pausa = Object.FindFirstObjectByType<PausaCrater>();
        pausa.pausarAlPerderFoco = false;
        if (PausaCrater.EnPausa) pausa.Reanudar();
        Time.timeScale = 2f;
    }

    [UnityTearDown]
    public IEnumerator Restaurar()
    {
        Time.timeScale = 1f;
        yield return null;
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator ElRecorridoCompletoSePuedeJugar() { yield return Recorrido(false); }

    [UnityTest, Timeout(300000)]
    public IEnumerator ElRecorridoConLunaPrimeroSePuedeJugar() { yield return Recorrido(true); }

    int piezasDeseadas;
    [UnityTest, Timeout(420000)]
    public IEnumerator ElFinalFuncionaConColeccionParcial(){yield return Recorrido(false,1);}
    [UnityTest, Timeout(480000)]
    public IEnumerator ElFinalFuncionaConColeccionCompleta(){yield return Recorrido(true,6);}
    IEnumerator RecogerOpcional(int indice)
    {
        var c=Object.FindFirstObjectByType<ColeccionPiedras>();var p=c.piezas.First(x=>x.indice==indice);
        fps.MirarHacia(p.transform.position);yield return Esperar(.2f);
        Assert.That(c.Objetivo,Is.EqualTo(p),"no se alcanza/ve la pieza "+p.identificador);
        Assert.That(p.Recoger(),Is.True);Assert.That(p.Recoger(),Is.False);
    }
    IEnumerator Recorrido(bool lunaPrimero,int cantidad=0)
    {
        piezasDeseadas=cantidad;
        var cuerpo = linterna.filtros[0];
        var hueco = linterna.filtros[1];

        // Prólogo: caminar desde las lomas al mirador; bajar por la boca del cráter
        Vector3 capilla = Buscar<Transform>("SpawnCapilla").position;
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Prologo));
        Vector3 inicio = Buscar<Transform>("SpawnValle").position;
        Assert.That(Vector3.Distance(jugador.transform.position, inicio), Is.LessThan(1f), "el jugador no empieza en la loma exterior");
        Vector3 mirador = Buscar<ZonaJugador>("Zona_Mirador").transform.position;
        for (float t = 0f; t < 90f && !prologo.Reproduciendo; t += Time.deltaTime)
        {
            Vector3 d = mirador - jugador.transform.position;
            d.y = 0f;
            cc.Move(d.normalized * Velocidad * Time.deltaTime);
            yield return null;
        }
        yield return Esperar(0.2f);
        Assert.That(prologo.Reproduciendo, Is.True, "llegar al mirador no abrió el cráter");
        // El jugador observa el eclipse: mirar afuera del encuadre debe pausarlo.
        fps.MirarHacia(fps.camara.position + Object.FindFirstObjectByType<CieloEclipse>().DireccionSol * 100);
        for (float t = 0f; t < 40f && prologo.Reproduciendo; t += Time.deltaTime) yield return null;
        Assert.That(prologo.Reproduciendo, Is.False, "la apertura del cráter no terminó");

        // por la puerta del borde y la escalera, hasta el fondo del pozo (la Explanada)
        yield return Caminar(Buscar<Transform>("Boca_Crater").position + Vector3.forward * 1.2f);
        yield return Caminar(new Vector3(0f, 0f, -33.8f));
        Assert.That(jugador.transform.position.y, Is.LessThan(0.5f), "la escalera del pozo no llega al fondo");
        Assert.That(prologo.EnElCrater, Is.True, "bajar al pozo no avisó que se entró al cráter");
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.BuscarLinterna));

        Capturar("01_Explanada");

        // el pasillo hasta el Umbral
        yield return Caminar(S(new Vector3(0f, 0f, -18f)));

        // Umbral: los murales de los controles y, más adelante, la linterna y la compuerta
        yield return Caminar(S(new Vector3(1.5f, 0f, -8.5f)));
        Assert.That(linterna.Disponible, Is.True, "no se pudo recoger la linterna");
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.EncenderLinterna));
        linterna.Encender(true);
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.AbrirUmbral));
        yield return Iluminar(Buscar<Ancla>("Ancla_Umbral"), 0.8f);
        yield return Esperar(3.5f);
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Explorar), "la compuerta no se abrió");
        yield return Caminar(S(new Vector3(0f, 0f, 1f)));

        fps.MirarHacia(S(new Vector3(0f, 2f, 18f)));
        Capturar("02_Rotonda_Inicial");

        if(piezasDeseadas>0)
        {
            var p=Object.FindFirstObjectByType<ColeccionPiedras>().piezas.First(x=>x.indice==4);
            yield return Caminar(S(new Vector3(-9,0,6)));
            yield return Caminar(p.transform.position+Vector3.right*1.3f);
            yield return RecogerOpcional(4);
            yield return Caminar(S(new Vector3(-9,0,6)));
        }
        if(piezasDeseadas==6)
        {
            yield return Caminar(S(new Vector3(0,0,1)));
            yield return Caminar(S(new Vector3(0,0,-18)));
            yield return Caminar(new Vector3(0,0,-33.8f));
            yield return Caminar(Buscar<Transform>("Boca_Crater").position+Vector3.forward*1.2f);
            var p=Object.FindFirstObjectByType<ColeccionPiedras>().piezas.First(x=>x.indice==0);
            yield return Caminar(new Vector3(0,9,p.transform.position.z));
            yield return Caminar(p.transform.position+Vector3.left*1.2f);
            Assert.That(jugador.transform.position.y,Is.GreaterThan(8.5f),"no se puede volver al exterior antes de Cresta");
            var cieloVista=Object.FindFirstObjectByType<CieloEclipse>();fps.MirarHacia(fps.camara.position+cieloVista.DireccionSol*100);cieloVista.ActualizarVista();
            Capturar("Coleccion_Vista_Exterior");yield return RecogerOpcional(0);
            yield return Caminar(new Vector3(0,9,p.transform.position.z));
            yield return Caminar(Buscar<Transform>("Boca_Crater").position+Vector3.forward*1.2f);
            yield return Caminar(new Vector3(0,0,-33.8f));
            yield return Caminar(S(new Vector3(0,0,-18)));
            yield return Caminar(S(new Vector3(0,0,1)));
        }
        if (lunaPrimero) { yield return CompletarLuna(); yield return CompletarSol(); }
        else { yield return CompletarSol(); yield return CompletarLuna(); }

        // ---- los dos sellos: el eclipse del obelisco, el de la puerta, y la puerta se abre
        Assert.That(Object.FindFirstObjectByType<ObeliscoDelEclipse>().Encendido, Is.True, "el eclipse del obelisco no se encendió");
        yield return Esperar(1f);
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Cruce), "los dos sellos no abrieron la puerta del norte");
        yield return Caminar(S(new Vector3(0f, 0f, 22f)));
        yield return Caminar(S(new Vector3(0f, 0f, 24f)));
        if(piezasDeseadas==6)
        {
            var c=Object.FindFirstObjectByType<ColeccionPiedras>();var p=c.piezas.First(x=>x.indice==5);
            yield return Caminar(S(new Vector3(-4,0,29.2f)));
            yield return RecogerOpcional(5);
            Assert.That(c.Cantidad,Is.EqualTo(6));Assert.That(c.conjunto.Transformado,Is.False);
            yield return Caminar(S(new Vector3(0,0,24)));
            yield return Caminar(S(new Vector3(-6,0,19)));
            yield return Caminar(new Vector3(c.conjunto.transform.position.x,0,c.conjunto.transform.position.z-2));
            fps.MirarHacia(c.conjunto.transform.position);yield return Esperar(6);
            Assert.That(c.conjunto.Transformado,Is.True);
            Capturar("Coleccion_Completa_Recorrido");
            yield return Caminar(S(new Vector3(-6,0,19)));
            yield return Caminar(S(new Vector3(0,0,24)));
        }
        yield return Caminar(S(new Vector3(0, 0, 34)));
        yield return Equipar(hueco);
        yield return Iluminar(Buscar<MateriaHueca>("Reja_N_Tapa"), 0.6f);
        yield return Equipar(cuerpo);
        yield return Iluminar(Buscar<Ancla>("Ancla_N_Oculta"), 0.6f);
        yield return Iluminar(Buscar<Ancla>("Ancla_N_Vista"), 0.6f);
        yield return Esperar(0.3f);
        Assert.That(Buscar<PuenteLuz>("Puente_N_Reja").Solido, Is.True);
        yield return Caminar(S(new Vector3(-2, 0, 34.5f)));
        yield return Caminar(S(new Vector3(-2, 0, 40.8f)));
        yield return Caminar(S(new Vector3(0, 0, 44.5f)));
        yield return CruzarPuente("Puente_N_Borde", S(new Vector3(0, 0, 44.5f)), S(new Vector3(0, 0, 52)), "Ancla_N_Borde_A", "Ancla_N_Borde_B");
        // Se puede esperar sobre la isla sin mantener el primer puente.
        linterna.Encender(false);
        yield return Esperar(12f);
        Assert.That(jugador.transform.position.y, Is.GreaterThan(-0.2f), "la isla de descanso no es segura");
        Assert.That(Buscar<PuenteLuz>("Puente_N_Borde").Solido, Is.False);
        linterna.Encender(true);
        yield return Equipar(hueco);
        yield return Iluminar(Buscar<MateriaHueca>("Reja_N_Salida"), 0.6f);
        yield return Equipar(cuerpo);
        yield return CruzarPuente("Puente_N_Salida", S(new Vector3(0, 0, 52)), S(new Vector3(0, 0, 56.3f)), "Ancla_N_Salida_A", "Ancla_N_Salida_B");
        yield return Equipar(hueco);
        yield return Atravesar("Reja_N_Salida", S(new Vector3(0, 0, 56.3f)), S(new Vector3(0, 0, 58)));
        yield return Caminar(S(new Vector3(0, 0, 62)));

        // Cresta: el corredor se cierra a la espalda
        yield return Caminar(S(new Vector3(0f, 0f, 70f)));
        yield return Caminar(S(new Vector3(0f, 0f, 84f)));
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Cresta));
        Assert.That(Object.FindFirstObjectByType<ColeccionPiedras>().Cantidad,Is.EqualTo(piezasDeseadas));
        yield return Esperar(3f);
        Assert.That(Buscar<Compuerta>("Cierre_Cresta").Abierta, Is.True, "el corredor no se cerró al entrar a la Cresta");

        // apagar y esperar: el techo se abre, todo se vuelve blanco y se vuelve a la capilla
        linterna.Encender(false);
        var techo = Object.FindFirstObjectByType<AperturaTecho>();
        for (float t = 0f; t < 25f && !techo.Abriendo; t += Time.deltaTime) yield return null;
        Assert.That(techo.Abriendo, Is.True, "la adaptación no abrió el techo");
        for (float t = 0f; t < 40f && flujo.EtapaActual != FlujoJuegoCrater.Etapa.Epilogo; t += Time.deltaTime)
            yield return null;
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Epilogo), "la luz del techo no llevó a la capilla");
        Assert.That(Vector3.Distance(jugador.transform.position, capilla), Is.LessThan(1.5f));

        // Epílogo: cruzar el borde de la huella dispara el cierre antes de su centro.
        Vector3 huella = Buscar<ZonaJugador>("Zona_LugarDelCrater").transform.position;
        for (float t = 0; t < 40 && flujo.EtapaActual != FlujoJuegoCrater.Etapa.Finalizado; t += Time.deltaTime)
        {
            Vector3 d = huella - jugador.transform.position; d.y = 0;
            if (fps.enabled) cc.Move(d.normalized * Velocidad * Time.deltaTime);
            yield return null;
        }
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Finalizado), "cruzar la huella no cerró la demo");
        bool tituloVisible = false;
        for (float t = 0; t < 15 && !tituloVisible; t += Time.unscaledDeltaTime)
        {
            tituloVisible = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None)
                .Any(texto => texto.text == "CRÁTER" && texto.enabled && texto.color.a > 0.8f);
            yield return null;
        }
        Assert.That(tituloVisible, Is.True, "los créditos no son visibles");
        Capturar("12_Final_" + (lunaPrimero ? "Luna_SOL" : "SOL_Luna"), true);
    }

    IEnumerator AcercarseAlAla(float lado)
    {
        float actual = Mathf.Sign(jugador.transform.position.x);
        if (Mathf.Abs(jugador.transform.position.x) > 1)
            yield return Caminar(S(new Vector3(actual * 9, 0, 6)));
        yield return Caminar(S(new Vector3(0, 0, 3)));
        yield return Caminar(S(new Vector3(lado * 9, 0, 6)));
        yield return Caminar(S(new Vector3(lado * 13, 0, 12)));
    }

    IEnumerator CompletarSol()
    {
        var cuerpo = linterna.filtros[0];
        // ---- ala oeste: CUERPO
        // la rotonda es circular: el ala se entra por la mitad del costado oeste
        yield return AcercarseAlAla(-1);
        yield return Caminar(O(-21f, 1f));
        fps.MirarHacia(O(-28f, -0.5f) + Vector3.up * 1.55f);
        Capturar("02b_Entrada_Sol");
        yield return Caminar(O(-23f, 1f));
        Assert.That(linterna.EstaDesbloqueado(cuerpo), Is.True, "no se pudo recoger CUERPO");
        yield return Equipar(cuerpo);
        Capturar("03_Sol_Ensenar");
        if(piezasDeseadas==6)
        {
            var red=Buscar<Transform>("Red_O1");
            // El haz apunta a las anclas al entrar: esperar la disolución antes de bajar a la gruta.
            linterna.Encender(false);yield return Esperar(5f);
            Assert.That(Buscar<PuenteLuz>("Puente_O_Ensenar").Solido,Is.False);
            yield return Caminar(red.TransformPoint(new Vector3(0,0,1.25f)));yield return Esperar(1.5f);
            Assert.That(jugador.transform.position.y,Is.LessThan(-2.8f));
            var p=Object.FindFirstObjectByType<ColeccionPiedras>().piezas.First(x=>x.indice==3);
            yield return Caminar(new Vector3(p.transform.position.x+1,-3,p.transform.position.z));
            yield return RecogerOpcional(3);linterna.Encender(true);
            yield return Caminar(red.TransformPoint(new Vector3(5.5f,-3,0)));yield return Esperar(1.3f);
            yield return Caminar(red.TransformPoint(new Vector3(5.5f,0,-5)));
            yield return Caminar(red.TransformPoint(new Vector3(4,0,-5)));
        }
        yield return CruzarPuente("Puente_O_Ensenar", O(-24.8f, 1f), O(-29.6f, 1f), "Ancla_O_Ensenar_A", "Ancla_O_Ensenar_B");
        yield return Caminar(O(-29.6f, 6f));
        yield return Caminar(O(-33f, 8f));
        yield return Caminar(O(-33f, 11.5f));

        // la puerta permanente con una ancla al fondo
        yield return Caminar(O(-28f, 17f));
        yield return Iluminar(Buscar<Ancla>("Ancla_O_Puerta"), 0.6f);
        yield return Esperar(0.7f);
        Assert.That(Buscar<Compuerta>("Puerta_O_Ancla").Abierta, Is.True, "el ancla no abrió la puerta");
        if(piezasDeseadas==6)
        {
            yield return Caminar(O(-33,13.8f));yield return RecogerOpcional(1);
            yield return Caminar(O(-28,17));
        }
        yield return Caminar(O(-21.5f, 12f));
        yield return Caminar(O(-18f, 12f));

        // anclas colgadas del techo, y el sello detrás del tabique
        yield return CruzarPuente("Puente_O_Torcer", O(-16f, 14.3f), O(-16f, 19f), "Ancla_O_Torcer_A", "Ancla_O_Torcer_B");
        yield return Caminar(O(-13.3f, 19.5f));
        yield return Caminar(O(-13.3f, 24.5f));
        yield return Iluminar(Buscar<Ancla>("Sello_Oeste"), 0.6f);
        yield return Esperar(3f);
        Assert.That(Buscar<Compuerta>("Atajo_Oeste").Abierta, Is.True, "el sello oeste no abrió el atajo");
        // por el pasillo de vuelta hasta la rotonda: ahí se ve la cinemática del sello
        yield return Caminar(S(new Vector3(-14f, 0f, 35.75f)));
        yield return Caminar(S(new Vector3(-9.5f, 0f, 35.75f)));
        yield return Caminar(S(new Vector3(-9.5f, 0f, 24f)));
        yield return Caminar(S(new Vector3(-6f, 0f, 19f)));
        yield return EsperarCinematica();

        Capturar("04_Rotonda_Un_Sello");

    }

    IEnumerator CompletarLuna()
    {
        var hueco = linterna.filtros[1];
        // ---- ala este: HUECO (rodeando por el sur el anillo de columnas del centro de la rotonda)
        yield return AcercarseAlAla(1);
        yield return Caminar(E(21f, 1f));
        fps.MirarHacia(E(28f, -0.5f) + Vector3.up * 1.55f);
        Capturar("04b_Entrada_Luna");
        yield return Caminar(E(23.5f, 1f));
        Assert.That(linterna.EstaDesbloqueado(hueco), Is.True, "no se pudo recoger HUECO");
        yield return Equipar(hueco);
        Capturar("05_Luna_Ensenar");
        yield return Atravesar("Reja_E_Ensenar_A", E(25.8f, -2f), E(30f, -2f));
        yield return Caminar(E(29.5f, -6f));
        yield return Caminar(E(33f, -6f));
        yield return Caminar(E(33f, -2f));
        fps.MirarHacia(Buscar<Transform>("Detalle_Luna_Reparo").position);
        Capturar("05b_Luna_Desvio");
        if(piezasDeseadas==6){yield return Caminar(E(33,-3.8f));yield return RecogerOpcional(2);yield return Caminar(E(33,-2));}
        Assert.That(Physics.Raycast(jugador.transform.position + Vector3.up, Vector3.forward, 5f * EspacioCrater.Escala,
            ~LayerMask.GetMask("Jugador"), QueryTriggerInteraction.Ignore), Is.True, "el ramal equivocado no está cerrado");
        yield return Caminar(E(33f, -6f));
        yield return Caminar(E(29.5f, -6f));
        yield return Atravesar("Reja_E_Ensenar_A", E(30f, -2f), E(25.8f, -2f));
        yield return Caminar(E(25.8f, 4f));
        yield return Atravesar("Reja_E_Ensenar_B", E(25.8f, 4f), E(30f, 4f));
        yield return Caminar(E(29.5f, 8f));
        yield return Caminar(E(33f, 8f));
        yield return Caminar(E(33f, 11.5f));

        // la trampilla: el camino sigue abajo
        yield return Caminar(E(24.8f, 13.5f));
        yield return Iluminar(Buscar<MateriaHueca>("Trampilla_E"), 0.6f);
        yield return Caminar(E(24.8f, 20.5f));
        Assert.That(jugador.transform.position.y, Is.LessThan(-3f), "la rampa no llega a la galería");
        yield return Caminar(E(22.6f, 20.5f));
        yield return Atravesar("Reja_E_Galeria", E(22.6f, 17.5f), E(18.5f, 17.5f));
        yield return Atravesar("Reja_E_Trinchera", E(18f, 15.3f), E(18f, 12.5f));

        // la rampa y la escotilla del techo
        yield return Caminar(E(14.5f, 12f));
        yield return Caminar(E(14.5f, 16.2f));
        yield return Iluminar(Buscar<MateriaHueca>("Escotilla_E"), 0.6f);
        yield return Caminar(E(14.5f, 22f));
        Assert.That(jugador.transform.position.y, Is.GreaterThan(-0.5f), "no se pudo cruzar la reja vertical al final de la rampa");
        yield return Caminar(E(18.6f, 22.5f));
        yield return Caminar(E(18.6f, 25.3f));
        yield return Iluminar(Buscar<MateriaHueca>("Sello_Este"), 0.6f);
        yield return Esperar(3f);
        Assert.That(Buscar<Compuerta>("Atajo_Este").Abierta, Is.True, "el sello este no abrió el atajo");
        yield return Caminar(S(new Vector3(14f, 0f, 35.75f)));
        yield return Caminar(S(new Vector3(9.5f, 0f, 35.75f)));
        yield return Caminar(S(new Vector3(9.5f, 0f, 24f)));
        yield return Caminar(S(new Vector3(6f, 0f, 19f)));
        yield return EsperarCinematica();

        Capturar("06_Rotonda_Dos_Sellos");

    }

    [UnityTest, Timeout(120000)]
    public IEnumerator LasCuatroCaidasVuelvenPorEscaleraSinRespawn()
    {
        var opcional=Object.FindFirstObjectByType<ColeccionPiedras>();opcional.piezas[4].Recoger();
        prologo.enabled = false;
        foreach (var caso in new[] { ("O1", 5.5f, 2.5f), ("O3", 2.4f, 2.5f), ("N1", 4.3f, 4f), ("N2", -4.3f, 8f) })
        {
            Transform red = Buscar<Transform>("Red_" + caso.Item1);
            var tapa = Buscar<Compuerta>("Losa_Red_" + caso.Item1);
            Assert.That(tapa.Abierta, Is.False, "la escalera no debe estar abierta desde arriba");
            cc.enabled = false;
            jugador.transform.position = red.TransformPoint(new Vector3(caso.Item1 == "N2" ? -3f : 0f, 0.1f, caso.Item3 / 2f));
            fps.ReiniciarMovimiento();
            cc.enabled = true;
            Physics.SyncTransforms();
            yield return Esperar(1.5f);
            Assert.That(jugador.transform.position.y, Is.EqualTo(-3f).Within(0.15f), "la gruta no atrapó la caída de " + caso.Item1);
            yield return Caminar(red.TransformPoint(new Vector3(caso.Item2, -3f, 0f)));
            yield return Esperar(1.3f);
            Assert.That(tapa.Progreso, Is.EqualTo(1f).Within(0.01f), "no se abrió la losa desde abajo en " + caso.Item1);
            yield return Caminar(red.TransformPoint(new Vector3(caso.Item2, 0f, -5f)));
            // Al llegar arriba se sale de costado al piso firme, antes de la pared de la sala.
            yield return Caminar(red.TransformPoint(new Vector3(caso.Item2 + (caso.Item2 < 0 ? 1.5f : -1.5f), 0f, -5f)));
            yield return Esperar(0.3f);
            Assert.That(jugador.transform.position.y, Is.GreaterThan(-0.2f), "no se pudo salir de " + caso.Item1);
            Assert.That(tapa.Abierta, Is.True, "la losa debe quedar abierta");
            Assert.That(opcional.Cantidad,Is.EqualTo(1),"la recuperación perdió el progreso opcional");
        }
    }

    [UnityTest]
    public IEnumerator ElEclipseAvanzaQuietoYElMiradorConservaLaMirada()
    {
        var cielo = Object.FindFirstObjectByType<CieloEclipse>();
        prologo.AvanzarEclipse(80f);
        Assert.That(cielo.Progreso, Is.LessThan(1f), "la totalidad no debe ocurrir lejos del mirador");
        prologo.IniciarCinematica();
        Assert.That(fps.movimientoBloqueado, Is.True);
        Assert.That(fps.enabled, Is.True, "la mirada debe permanecer disponible");
        fps.MirarHacia(jugador.transform.position + new Vector3(10f, 2f, 0f));
        Quaternion mirada = fps.camara.rotation;
        yield return Esperar(1f);
        Assert.That(Quaternion.Angle(mirada, fps.camara.rotation), Is.LessThan(0.1f), "el prólogo giró la cámara");
        Assert.That(cielo.Progreso, Is.LessThan(1f), "el contacto final avanzó mirando a otro lado");
        fps.MirarHacia(fps.camara.position + cielo.DireccionSol * 100f);
        bool aperturaSonora=false;var sonido=GameObject.Find("Apertura_Tierra").GetComponent<AudioSource>();
        for(float t=0;t<14;t+=Time.deltaTime){aperturaSonora|=sonido.isPlaying;yield return null;}
        Assert.That(aperturaSonora,Is.True,"la apertura no reprodujo su audio de tierra");
        Assert.That(fps.movimientoBloqueado, Is.False);
        Assert.That(prologo.Reproduciendo, Is.False);
    }

    [UnityTest]
    public IEnumerator ElReflejoSeRecortaEnLasCuatroParedes()
    {
        prologo.enabled = false;
        // Mantener el vano abierto durante esta prueba; el recorrido completo verifica su cierre.
        Buscar<ZonaJugador>("Zona_Cresta").gameObject.SetActive(false);
        cc.enabled = false;
        jugador.transform.position = S(new Vector3(0f, 0.05f, 87f));
        fps.ReiniciarMovimiento();
        cc.enabled = true;
        fps.movimientoBloqueado = true;
        linterna.Recoger();
        linterna.Encender(true);
        foreach (var caso in new[] {
            ("ParedEspejo", new Vector3(11.4f, 1.6f, 99f)),
            ("ParedEspejo_Oeste", new Vector3(-12f, 1.6f, 98.4f)),
            ("ParedEspejo_Este", new Vector3(12f, 1.6f, 98.4f)),
            ("ParedEspejo_Entrada", new Vector3(11.4f, 1.6f, 75f)) })
        {
            fps.MirarHacia(S(caso.Item2));
            yield return Esperar(0.2f);
            var pared = Buscar<ParedEspejo>(caso.Item1);
            var reflejo = pared.transform.Find("Reflejo").GetComponent<Renderer>();
            Assert.That(reflejo.enabled, Is.True, "falta el reflejo de " + caso.Item1);
            Bounds b = reflejo.bounds;
            Assert.That(b.min.x, Is.GreaterThanOrEqualTo(-12.02f * EspacioCrater.Escala));
            Assert.That(b.max.x, Is.LessThanOrEqualTo(12.02f * EspacioCrater.Escala));
            Assert.That(b.min.z, Is.GreaterThanOrEqualTo(S(new Vector3(0, 0, 74.98f)).z));
            Assert.That(b.max.z, Is.LessThanOrEqualTo(S(new Vector3(0, 0, 99.02f)).z));
            Assert.That(b.min.y, Is.GreaterThanOrEqualTo(-0.02f));
            Assert.That(b.max.y, Is.LessThanOrEqualTo(8.02f));
            var bloque = new MaterialPropertyBlock();
            reflejo.GetPropertyBlock(bloque);
            Assert.That(bloque.GetVector("_BaseMap_ST").x, Is.LessThan(1f), "el borde debe recortar la textura, sin achicar el círculo entero");
        }
        fps.MirarHacia(S(new Vector3(0f, 1.6f, 75f)));
        yield return Esperar(0.2f);
        Assert.That(Buscar<ParedEspejo>("ParedEspejo_Entrada").Encandilamiento, Is.EqualTo(0f), "el vano de la entrada no debe reflejar en el aire");
    }

    // ------------------------------------------------------------------ acciones

    // cada ala se arma en sus coordenadas de siempre y después se corre entera
    // (ConstructorCrater.CorrimientoAlaOeste / Este): estos pasan de unas a otras
    static Vector3 O(float x, float z) => S(new Vector3(x - 3f, 0f, z + 11f));
    static Vector3 E(float x, float z) => S(new Vector3(x + 3f, 0f, z + 11f));
    static Vector3 S(Vector3 punto) => EspacioCrater.Punto(punto);

    /// <summary>Camina en línea recta hacia 'destino' (sólo x y z). Falla si en 'limite' segundos no llega.</summary>
    IEnumerator Caminar(Vector3 destino, float limite = 30f)
    {
        for (float t = 0f; t < limite; t += Time.deltaTime)
        {
            Vector3 d = destino - jugador.transform.position;
            d.y = 0f;
            if (d.magnitude < 0.12f) yield break;
            if (cc.enabled && fps.enabled && !fps.movimientoBloqueado) cc.Move(d.normalized * Mathf.Min(Velocidad * Time.deltaTime, d.magnitude));
            yield return null;
        }
        Vector3 p = jugador.transform.position;
        var alrededor = Physics.OverlapCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.5f, 0.45f, ~0, QueryTriggerInteraction.Ignore);
        string cerca = string.Join(", ", alrededor.Select(c => c.name + "@" + c.bounds.center));
        Assert.Fail($"no se pudo llegar a {destino}: el jugador quedó en {p}. Colliders cerca: {cerca}");
    }

    /// <summary>Mantiene la mirada (y la linterna) sobre un receptor durante 'segundos'.</summary>
    IEnumerator Iluminar(ReceptorDeLuz receptor, float segundos)
    {
        for (float t = 0f; t < segundos; t += Time.deltaTime)
        {
            fps.MirarHacia(receptor.PuntoDeImpacto);
            yield return null;
        }
        Assert.That(receptor.Activo, Is.True, $"{receptor.name} no se activó con el haz desde {jugador.transform.position}");
    }

    IEnumerator Equipar(FiltroDefinicion filtro)
    {
        while (linterna.CambiandoFiltro) yield return null;
        linterna.EquiparFiltro(filtro);
        while (linterna.CambiandoFiltro) yield return null;
        Assert.That(linterna.FiltroActual, Is.SameAs(filtro));
    }

    /// <summary>Camina hasta 'antes', enciende las anclas, espera el puente y cruza hasta 'despues'.</summary>
    IEnumerator CruzarPuente(string nombre, Vector3 antes, Vector3 despues, params string[] anclas)
    {
        var puente = Buscar<PuenteLuz>(nombre);
        yield return Caminar(antes);
        foreach (var ancla in anclas) yield return Iluminar(Buscar<Ancla>(ancla), 0.6f);
        yield return Esperar(0.3f);
        Assert.That(puente.Solido, Is.True, $"{nombre} no apareció");

        MirarHacia(despues);
        yield return Caminar(despues);
        Assert.That(jugador.transform.position.y, Is.GreaterThan(-0.5f), $"el jugador se cayó cruzando {nombre}");
    }

    /// <summary>Camina hasta 'antes', disuelve la reja y pasa hasta 'despues'.</summary>
    IEnumerator Atravesar(string nombre, Vector3 antes, Vector3 despues)
    {
        var reja = Buscar<MateriaHueca>(nombre);
        yield return Caminar(antes);
        yield return Iluminar(reja, 0.6f);
        yield return Esperar(0.2f);
        Assert.That(reja.Solido, Is.False, $"{nombre} no se disolvió");
        yield return Caminar(despues);
    }

    /// <summary>Mira hacia un punto del recorrido, a la altura de los ojos.</summary>
    void MirarHacia(Vector3 destino)
    {
        Vector3 p = jugador.transform.position;
        Vector3 dir = new Vector3(destino.x - p.x, 0f, destino.z - p.z);
        if (dir.sqrMagnitude < 0.0001f) return;
        fps.MirarHacia(p + Vector3.up * 1.6f + dir.normalized * 10f);
    }

    /// <summary>Espera a que termine la cinemática de un sello (la cámara vuelve al jugador).</summary>
    static IEnumerator EsperarCinematica()
    {
        for (float t = 0f; t < 1f && !CinematicaDeSello.Reproduciendo; t += Time.deltaTime) yield return null;
        for (float t = 0f; t < 40f && CinematicaDeSello.Reproduciendo; t += Time.deltaTime) yield return null;
        Assert.That(CinematicaDeSello.Reproduciendo, Is.False, "la cinemática del sello no terminó");
    }

    static IEnumerator Esperar(float segundos)
    {
        for (float t = 0f; t < segundos; t += Time.deltaTime) yield return null;
    }

    [UnityTest]
    public IEnumerator LaCinematicaMuestraLucesYPuedeInterrumpirseSinBloquear()
    {
        prologo.enabled = false;
        cc.enabled = false;
        jugador.transform.position = S(new Vector3(-6f, 0.05f, 19f));
        cc.enabled = true;
        fps.MirarHacia(S(new Vector3(-12f, 2f, 18f)));
        Quaternion mirada = fps.camara.rotation;
        Buscar<Ancla>("Sello_Oeste").RecibirLuz(linterna.filtros[0], 1f);
        Buscar<MateriaHueca>("Sello_Este").RecibirLuz(linterna.filtros[1], 1f);
        yield return null;
        Assert.That(CinematicaDeSello.Reproduciendo, Is.True);
        Assert.That(fps.enabled, Is.False);
        yield return Esperar(0.8f);
        Assert.That(Quaternion.Angle(mirada, fps.camara.rotation), Is.GreaterThan(2f));
        Assert.That(Object.FindFirstObjectByType<PausaCrater>().enabled, Is.True);
        Object.FindFirstObjectByType<CinematicaDeSello>().enabled = false;
        Assert.That(CinematicaDeSello.Reproduciendo, Is.False);
        Assert.That(fps.enabled, Is.True);
        Assert.That(Buscar<Compuerta>("Puerta_Sellos").Abierta, Is.True);
    }

    [UnityTest]
    public IEnumerator LasRejasNoRepitenElSiseoAlMantenerLaLuz()
    {
        prologo.enabled = false;
        var reja = Buscar<MateriaHueca>("Reja_E_Ensenar_B");
        reja.permanente = true;
        reja.RecibirLuz(linterna.filtros[1], 1f);
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            reja.RecibirLuz(linterna.filtros[1], Time.deltaTime);
            yield return null;
        }
        Assert.That(reja.Activo, Is.True);
        Assert.That(reja.siseo.isPlaying, Is.False, "el siseo no debe repetirse en una reja disuelta");
    }

    [UnityTest]
    public IEnumerator LaPlazaCircularTienePisoEnTodasLasDirecciones()
    {
        yield return null;
        for (int i = 0; i < 16; i++)
            foreach (float radio in new[] { 3f, 4.1f, 5f, 6.3f, 7f })
            {
                float a = i * Mathf.PI / 8;
                Vector3 p = S(new Vector3(Mathf.Cos(a) * radio, 1f, 12f + Mathf.Sin(a) * radio));
                Assert.That(Physics.Raycast(p, Vector3.down, out var hit, 4f,
                    ~LayerMask.GetMask("Jugador"), QueryTriggerInteraction.Ignore), Is.True);
                Assert.That(hit.point.y, Is.InRange(-1.21f, 0.01f), "altura fuera de la escalinata circular");
            }
    }

    [UnityTest]
    public IEnumerator LosPasosCambianConElSueloYLaCarrera()
    {
        prologo.enabled = false;
        var mando = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
        mando.MakeCurrent();
        var suelos = new System.Collections.Generic.List<GameObject>();
        try
        {
            Assert.That(fuentePasosVolumen(), Is.LessThanOrEqualTo(0.21f));
            int indice = 0;
            foreach (var tipo in new[] { SuperficieDePasos.Tipo.Tierra, SuperficieDePasos.Tipo.Piedra, SuperficieDePasos.Tipo.Madera, SuperficieDePasos.Tipo.Metal })
            {
                var suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                suelo.transform.position = new Vector3(300 + indice * 50, -0.1f, 0);
                suelo.transform.localScale = new Vector3(40, 0.2f, 40);
                suelo.AddComponent<SuperficieDePasos>().tipo = tipo;
                suelos.Add(suelo);
                cc.enabled = false;
                jugador.transform.position = suelo.transform.position + new Vector3(0, 0.15f, 0);
                fps.Orientar(Quaternion.identity);
                fps.ReiniciarMovimiento();
                cc.enabled = true;
                int antes = fps.PasosEmitidos;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mando,
                    new UnityEngine.InputSystem.LowLevel.GamepadState { leftStick = Vector2.up });
                yield return Esperar(1.6f);
                Assert.That(fps.PasosEmitidos, Is.GreaterThan(antes));
                Assert.That(fps.UltimaSuperficie, Is.EqualTo(tipo));
                float cadenciaCaminar = fps.CadenciaPasos;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mando,
                    new UnityEngine.InputSystem.LowLevel.GamepadState { leftStick = Vector2.up }.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.LeftStick));
                yield return Esperar(1.2f);
                Assert.That(fps.CadenciaPasos, Is.GreaterThan(cadenciaCaminar * 1.3f));
                Assert.That(fps.UltimoPasoCorriendo, Is.True);
                indice++;
            }
        }
        finally
        {
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mando);
            foreach (var suelo in suelos) Object.Destroy(suelo);
        }
    }

    float fuentePasosVolumen() => fps.fuentePasos.volume;

    [UnityTest]
    public IEnumerator ElExteriorTrasLaEstacionTieneSueloContinuo()
    {
        prologo.enabled = false;
        var valle = Buscar<Transform>("06_Capilla");
        cc.enabled = false;
        jugador.transform.position = valle.TransformPoint(new Vector3(38, 0.05f, -8));
        fps.ReiniciarMovimiento();
        cc.enabled = true;
        var terreno = Buscar<MeshCollider>("Terreno");
        foreach (float x in new[] { -36f, -26f, 0f, 26f, 36f })
            foreach (float z in new[] { -8f, -16f, -22f })
                Assert.That(terreno.Raycast(new Ray(valle.TransformPoint(new Vector3(x, 3, z)), Vector3.down), out _, 4), Is.True,
                    "falta suelo posterior en " + new Vector2(x, z));
        foreach (var p in new[] { new Vector3(38, 0, -21), new Vector3(32, 0, -21), new Vector3(38, 0, -21), new Vector3(38, 0, -8) })
        {
            yield return Caminar(valle.TransformPoint(p));
            Assert.That(jugador.transform.position.y, Is.InRange(8.7f, 9.2f), "se cae detrás de la estación");
        }
    }

    [UnityTest]
    public IEnumerator TecladoMouseYPausaPermitenSaltarLosSellosYRecuperarControl()
    {
        prologo.enabled = false;
        var teclado = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
        var mando = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
        try
        {
            cc.enabled = false;
            jugador.transform.position = S(new Vector3(-6, 0.05f, 19));
            fps.ReiniciarMovimiento(); cc.enabled = true;
            var pausa = Object.FindFirstObjectByType<PausaCrater>();
            void Key(UnityEngine.InputSystem.Key k) => UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado, new UnityEngine.InputSystem.LowLevel.KeyboardState(k));
            void Soltar() => UnityEngine.InputSystem.InputSystem.QueueStateEvent(teclado, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            // Dejar pasar los tres frames iniciales que descartan el salto del cursor.
            for(int i=0;i<4;i++)yield return null;
            var antes = fps.camara.rotation;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { delta = new Vector2(24, 8) });
            yield return null; yield return null;
            Assert.That(Quaternion.Angle(antes, fps.camara.rotation), Is.GreaterThan(0.1f), "el mouse no controla la mirada");
            linterna.Recoger(); linterna.Desbloquear(linterna.filtros[0]);
            Key(UnityEngine.InputSystem.Key.F); yield return null; yield return null;
            Assert.That(linterna.Encendida, Is.True);
            Soltar(); yield return null;
            Key(UnityEngine.InputSystem.Key.Digit1); yield return null; yield return null;
            Assert.That(linterna.FiltroActual, Is.SameAs(linterna.filtros[0]));
            Soltar(); yield return null;
            var sello = Buscar<Ancla>("Sello_Oeste");
            sello.RecibirLuz(linterna.filtros[0], 1); sello.Avanzar(1);
            yield return Esperar(0.2f);
            Assert.That(CinematicaDeSello.Reproduciendo, Is.True);
            Key(UnityEngine.InputSystem.Key.Escape); yield return null; yield return null;
            Assert.That(PausaCrater.EnPausa, Is.True);
            Soltar(); yield return null;
            Key(UnityEngine.InputSystem.Key.Space); yield return null; yield return null;
            Assert.That(CinematicaDeSello.Reproduciendo, Is.True, "saltar en pausa no debe perder el control");
            Soltar(); yield return null;
            Key(UnityEngine.InputSystem.Key.Escape); yield return null; yield return null;
            Soltar(); yield return null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(mando,
                new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South));
            yield return null; yield return Esperar(0.3f);
            Assert.That(CinematicaDeSello.Reproduciendo, Is.False);
            Assert.That(fps.enabled, Is.True);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(mando, new UnityEngine.InputSystem.LowLevel.GamepadState());
            yield return null;
            var luna = Buscar<MateriaHueca>("Sello_Este");
            luna.RecibirLuz(linterna.filtros[1], 1); luna.Avanzar(1);
            yield return Esperar(0.2f);
            Key(UnityEngine.InputSystem.Key.Space); yield return null; yield return Esperar(0.3f);
            Soltar(); yield return null;
            Assert.That(CinematicaDeSello.Reproduciendo, Is.False);
            Assert.That(fps.enabled, Is.True);
            Assert.That(Buscar<Compuerta>("Puerta_Sellos").Abierta, Is.True);
            Vector3 posicion = jugador.transform.position;
            Key(UnityEngine.InputSystem.Key.W); yield return Esperar(0.4f); Soltar();
            Assert.That(Vector3.Distance(posicion, jugador.transform.position), Is.GreaterThan(0.2f), "no devolvió movimiento al teclado");
        }
        finally
        {
            Object.FindFirstObjectByType<PausaCrater>()?.Reanudar();
            UnityEngine.InputSystem.InputSystem.RemoveDevice(teclado);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(mando);
        }
    }

    [UnityTest]
    public IEnumerator ElAnclaYElTalladoRespondenAlHazSinCambiarElProgreso()
    {
        prologo.enabled = false;
        cc.enabled = false;
        jugador.transform.position = O(-24.8f, 1f) + Vector3.up * 0.05f;
        cc.enabled = true;
        fps.enabled = false;
        linterna.Recoger();
        linterna.Desbloquear(linterna.filtros[0]);
        yield return Equipar(linterna.filtros[0]);
        var ancla = Buscar<Ancla>("Ancla_O_Ensenar_A");
        Time.timeScale = 0.5f;
        linterna.Encender(false);
        fps.MirarHacia(ancla.PuntoDeImpacto);
        Capturar("13_Ancla_Reposo");
        linterna.Encender(true);
        yield return Esperar(0.12f);
        Assert.That(ancla.Carga, Is.InRange(0.01f, 0.99f), "no recibe el primer contacto del haz");
        Assert.That(ancla.resonancia.isPlaying, Is.True);
        Capturar("14_Ancla_Carga");
        yield return Esperar(0.8f);
        Assert.That(ancla.Activo, Is.True);
        Capturar("15_Ancla_Activa");
        linterna.Encender(false);
        yield return Esperar(ancla.retencion * 0.8f);
        Assert.That(ancla.Activo, Is.True, "la retención debe dar tiempo para cruzar");
        Capturar("16_Ancla_Retencion");
        yield return Esperar(ancla.retencion * 0.2f + 0.7f);
        Assert.That(ancla.Activo, Is.False);
        Assert.That(ancla.resonancia.isPlaying, Is.False);
        Capturar("17_Ancla_Descargada");

        var red = Buscar<Transform>("Red_O1");
        cc.enabled = false;
        jugador.transform.position = red.TransformPoint(new Vector3(-4.8f, -2.95f, -3.2f));
        cc.enabled = true;
        var tallado = Buscar<TalladoResonante>("Tallado_Resonante_1");
        cc.enabled = false;
        jugador.transform.position = red.TransformPoint(new Vector3(0, -2.95f, 1));
        cc.enabled = true;
        fps.MirarHacia(red.TransformPoint(new Vector3(-4.8f, -2.1f, -4.65f)));
        Capturar("18b_Gruta_Conjunto");
        cc.enabled = false;
        jugador.transform.position = red.TransformPoint(new Vector3(-4.8f, -2.95f, -3.2f));
        cc.enabled = true;
        var etapa = flujo.EtapaActual;
        fps.MirarHacia(tallado.PuntoDeImpacto);
        Capturar("18_Gruta_Reposo");
        linterna.Encender(true);
        yield return Esperar(0.8f);
        Assert.That(tallado.Respuesta, Is.GreaterThan(0.5f), "el tallado queda fuera del alcance o tapado");
        Assert.That(tallado.resonancia.isPlaying, Is.True);
        Capturar("19_Gruta_Resonancia");
        linterna.Encender(false);
        yield return Esperar(1.2f);
        Assert.That(tallado.Respuesta, Is.Zero);
        Assert.That(tallado.resonancia.isPlaying, Is.False);
        Assert.That(flujo.EtapaActual, Is.EqualTo(etapa), "el gesto opcional no debe cambiar el progreso");
    }

    [UnityTest]
    public IEnumerator LaOscuridadRespondeProntoYLaAdaptacionSigueGradual()
    {
        prologo.enabled = false;
        cc.enabled = false;
        jugador.transform.position = S(new Vector3(4, 0.05f, 87));
        cc.enabled = true;
        fps.enabled = false;
        linterna.Recoger();
        linterna.Encender(true);
        var adaptacion = Object.FindFirstObjectByType<AdaptacionOscuridad>();
        adaptacion.Habilitar();
        fps.MirarHacia(S(new Vector3(11.5f, 3, 87)));
        Capturar("20_Cresta_Con_Linterna");
        linterna.Encender(false);
        yield return Esperar(0.8f);
        Assert.That(adaptacion.Progreso, Is.GreaterThan(0));
        Assert.That(adaptacion.ambienteDeAdaptacion.volume, Is.GreaterThan(0.01f));
        Assert.That(adaptacion.ambienteDeAdaptacion.isPlaying, Is.True);
        Capturar("21_Cresta_Primera_Respuesta");
        yield return Esperar(4f);
        Assert.That(adaptacion.Progreso, Is.LessThan(adaptacion.umbralApertura));
        Capturar("22_Cresta_Adaptacion_Parcial");
        linterna.Encender(true);
        yield return Esperar(1.5f);
        Assert.That(adaptacion.Progreso, Is.Zero);
        Assert.That(adaptacion.ambienteDeAdaptacion.volume, Is.Zero);
    }

    [UnityTest]
    public IEnumerator PisoYTechoReflejanYElOculoMuestraElCieloReal()
    {
        prologo.enabled = false;
        cc.enabled = false;
        jugador.transform.position = S(new Vector3(4, 0.05f, 87));
        cc.enabled = true;
        fps.enabled = false;
        linterna.Recoger();
        linterna.Encender(true);
        foreach (string nombre in new[] { "ParedEspejo_Piso", "ParedEspejo_Techo" })
        {
            var plano = Buscar<ParedEspejo>(nombre);
            fps.MirarHacia(plano.transform.position + Vector3.right * 4);
            yield return Esperar(0.15f);
            Assert.That(plano.Encandilamiento, Is.GreaterThan(0.1f), nombre);
            Capturar(nombre == "ParedEspejo_Piso" ? "24_Reflejo_Piso" : "25_Reflejo_Techo");
            linterna.Encender(false);
            yield return null;
            Assert.That(plano.Encandilamiento, Is.Zero);
            linterna.Encender(true);
        }
        foreach (var a in Object.FindObjectsByType<Ancla>(FindObjectsSortMode.None))
        {
            var escala = a.transform.lossyScale;
            Assert.That(Mathf.Abs(escala.x - escala.y), Is.LessThan(0.001f), a.name);
            Assert.That(Mathf.Abs(escala.x - escala.z), Is.LessThan(0.001f), a.name);
        }
        var cielo = Object.FindFirstObjectByType<CieloEclipse>();
        cielo.Progreso = 1;
        cielo.MostrarEnSubsuelo();
        Vector3 observacion = S(new Vector3(-2, 1.62f, 5));
        Physics.SyncTransforms();
        Assert.That(Physics.Raycast(observacion, cielo.DireccionSol, 100,
            ~LayerMask.GetMask("Jugador", "Ignore Raycast"), QueryTriggerInteraction.Ignore), Is.False, "el techo o terreno tapa el eclipse real");
        Assert.That(GameObject.Find("Oculo_Rotonda"), Is.Null, "queda la imagen de eclipse del óculo anterior");
        cc.enabled = false;
        jugador.transform.position = S(new Vector3(-2, 0.05f, 5));
        cc.enabled = true;
        fps.MirarHacia(fps.camara.position + cielo.DireccionSol * 100);
        cielo.ActualizarVista();
        Capturar("23_Rotonda_Eclipse_Real");
    }

    [UnityTest]
    public IEnumerator LaBajadaTieneLateralesCerradosYTalladosPegados()
    {
        prologo.enabled = false;
        yield return null;
        foreach (float x in new[] { -2.5f, 2.5f, -5f, 5f })
            for (float z = -46; z <= -35; z += 1)
                Assert.That(Physics.Raycast(new Vector3(x, 12, z), Vector3.down, out var hit,
                    4f, ~LayerMask.GetMask("Jugador"), QueryTriggerInteraction.Ignore) && hit.point.y > 8.4f,
                    Is.True, "hueco lateral de la escalera en " + new Vector2(x, z));
        var tallados = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r.name.StartsWith("Sol_Escalera_") || r.name.StartsWith("Luna_Escalera_")).ToArray();
        Assert.That(tallados.Length, Is.EqualTo(8));
        foreach (var r in tallados)
            Assert.That(Mathf.Abs(Mathf.Abs(r.bounds.center.x) - 1.6f), Is.LessThan(0.04f));
    }

    [UnityTest]
    public IEnumerator ElJoystickPausaYReanudaSinTeclado()
    {
        prologo.enabled = false;
        var g = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
        try
        {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(g,
                new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.Start));
            yield return null; yield return null;
            Assert.That(PausaCrater.EnPausa, Is.True);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(g, new UnityEngine.InputSystem.LowLevel.GamepadState());
            yield return null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(g,
                new UnityEngine.InputSystem.LowLevel.GamepadState().WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.East));
            yield return null; yield return null;
            Assert.That(PausaCrater.EnPausa, Is.False);
        }
        finally
        {
            UnityEngine.InputSystem.InputSystem.RemoveDevice(g);
            Object.FindFirstObjectByType<PausaCrater>()?.Reanudar();
        }
    }

    void Capturar(string nombre, bool incluirUI = false)
    {
        var cam = fps.camara.GetComponent<Camera>();
        var presupuesto = cam.GetComponent<PresupuestoSombras>();
        presupuesto?.Actualizar(cam.transform.position);
        var rt = new RenderTexture(1280, 720, 24);
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var anterior = RenderTexture.active;
        var objetivo = cam.targetTexture;
        var canvases = incluirUI ? Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray() : new Canvas[0];
        var estadosUI = canvases.Select(c => (c, c.renderMode, c.worldCamera, c.planeDistance)).ToArray();
        try
        {
            cam.targetTexture = rt;
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = 0.2f; }
            if (incluirUI) Canvas.ForceUpdateCanvases();
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            System.IO.Directory.CreateDirectory("Docs/Previews/Recorrido");
            System.IO.File.WriteAllBytes("Docs/Previews/Recorrido/" + nombre + ".png", tex.EncodeToPNG());
        }
        finally
        {
            foreach (var e in estadosUI) { e.c.renderMode = e.renderMode; e.c.worldCamera = e.worldCamera; e.c.planeDistance = e.planeDistance; }
            cam.targetTexture = objetivo;
            RenderTexture.active = anterior;
            Object.Destroy(rt);
            Object.Destroy(tex);
        }
    }

    static T Buscar<T>(string nombre) where T : Component
    {
        var encontrado = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == nombre);
        Assert.That(encontrado, Is.Not.Null, $"no existe {nombre} en la escena");
        return encontrado;
    }
}
