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
        Time.timeScale = 2f;
    }

    [UnityTearDown]
    public IEnumerator Restaurar()
    {
        Time.timeScale = 1f;
        yield return null;
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator ElRecorridoCompletoSePuedeJugar()
    {
        var cuerpo = linterna.filtros[0];
        var hueco = linterna.filtros[1];

        // Prólogo: salir de la capilla dispara el eclipse; por la puerta del cráter se baja caminando
        Vector3 capilla = Buscar<Transform>("SpawnCapilla").position;
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Prologo));
        Assert.That(Vector3.Distance(jugador.transform.position, capilla), Is.LessThan(1f), "el jugador no empieza en la capilla");
        yield return Caminar(Buscar<ZonaJugador>("Zona_Umbral_Capilla").transform.position + Vector3.forward * 0.6f);
        yield return Esperar(0.2f);
        Assert.That(prologo.Reproduciendo, Is.True, "salir de la capilla no disparó la cinemática");
        for (float t = 0f; t < 40f && prologo.Reproduciendo; t += Time.deltaTime) yield return null;
        Assert.That(prologo.Reproduciendo, Is.False, "la cinemática del eclipse no terminó");

        // por la puerta del borde y la escalera, hasta el fondo del pozo (la Explanada)
        yield return Caminar(Buscar<Transform>("Puerta").position + Vector3.forward * 1.2f);
        yield return Caminar(new Vector3(0f, 0f, -33.8f));
        Assert.That(jugador.transform.position.y, Is.LessThan(0.5f), "la escalera del pozo no llega al fondo");
        Assert.That(prologo.EnElCrater, Is.True, "bajar al pozo no avisó que se entró al cráter");
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.BuscarLinterna));

        // el pasillo hasta el Umbral
        yield return Caminar(new Vector3(0f, 0f, -18f));

        // Umbral: linterna y compuerta
        yield return Caminar(new Vector3(1.2f, 0f, -15.5f));
        Assert.That(linterna.Disponible, Is.True, "no se pudo recoger la linterna");
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.EncenderLinterna));
        linterna.Encender(true);
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.AbrirUmbral));
        yield return Iluminar(Buscar<Ancla>("Ancla_Umbral"), 0.8f);
        yield return Esperar(3.5f);
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Explorar), "la compuerta no se abrió");
        yield return Caminar(new Vector3(0f, 0f, 1f));

        // ---- ala oeste: CUERPO
        yield return Caminar(new Vector3(-16.5f, 0f, 1f));
        Assert.That(linterna.EstaDesbloqueado(cuerpo), Is.True, "no se pudo recoger CUERPO");
        yield return Equipar(cuerpo);
        yield return CruzarPuente("Puente_O_Ensenar", new Vector3(-24.8f, 0f, 1f), new Vector3(-29.6f, 0f, 1f), "Ancla_O_Ensenar_A", "Ancla_O_Ensenar_B");
        yield return Caminar(new Vector3(-29.6f, 0f, 6f));
        yield return Caminar(new Vector3(-33f, 0f, 8f));
        yield return Caminar(new Vector3(-33f, 0f, 11.5f));

        // la puerta que se sostiene con dos anclas lejanas
        yield return Caminar(new Vector3(-21.5f, 0f, 12f));
        yield return Iluminar(Buscar<Ancla>("Ancla_O_Puerta_A"), 0.6f);
        yield return Iluminar(Buscar<Ancla>("Ancla_O_Puerta_B"), 0.6f);
        yield return Esperar(0.7f);
        Assert.That(Buscar<Compuerta>("Puerta_O_Dos_Anclas").Abierta, Is.True, "las dos anclas no abrieron la puerta");
        yield return Caminar(new Vector3(-18f, 0f, 12f));

        // anclas colgadas del techo, y el sello detrás del tabique
        yield return CruzarPuente("Puente_O_Torcer", new Vector3(-16f, 0f, 14.3f), new Vector3(-16f, 0f, 19f), "Ancla_O_Torcer_A", "Ancla_O_Torcer_B");
        yield return Caminar(new Vector3(-13.3f, 0f, 19.5f));
        yield return Caminar(new Vector3(-13.3f, 0f, 24.5f));
        yield return Iluminar(Buscar<Ancla>("Sello_Oeste"), 0.6f);
        yield return Esperar(3f);
        Assert.That(Buscar<Compuerta>("Atajo_Oeste").Abierta, Is.True, "el sello oeste no abrió el atajo");
        yield return Caminar(new Vector3(-9f, 0f, 24.75f));

        // ---- ala este: HUECO
        yield return Caminar(new Vector3(8f, 0f, 1f));
        yield return Caminar(new Vector3(16.5f, 0f, 1f));
        Assert.That(linterna.EstaDesbloqueado(hueco), Is.True, "no se pudo recoger HUECO");
        yield return Equipar(hueco);
        yield return Atravesar("Reja_E_Ensenar_A", new Vector3(25.8f, 0f, -2f), new Vector3(30f, 0f, -2f));
        yield return Caminar(new Vector3(33f, 0f, -2f));
        yield return Caminar(new Vector3(33f, 0f, 8f));
        yield return Caminar(new Vector3(33f, 0f, 11.5f));

        // la trampilla: el camino sigue abajo
        yield return Caminar(new Vector3(24f, 0f, 13.5f));
        yield return Iluminar(Buscar<MateriaHueca>("Trampilla_E"), 0.6f);
        yield return Caminar(new Vector3(24f, 0f, 17.5f));
        yield return Esperar(1f);
        Assert.That(jugador.transform.position.y, Is.LessThan(-3f), "no se cayó por la trampilla");
        yield return Atravesar("Reja_E_Galeria", new Vector3(23.2f, 0f, 17.5f), new Vector3(18.5f, 0f, 17.5f));
        yield return Atravesar("Reja_E_Trinchera", new Vector3(18f, 0f, 15.3f), new Vector3(18f, 0f, 12.5f));

        // la rampa y la escotilla del techo
        yield return Caminar(new Vector3(14.5f, 0f, 12f));
        yield return Caminar(new Vector3(14.5f, 0f, 16.2f));
        yield return Iluminar(Buscar<MateriaHueca>("Escotilla_E"), 0.6f);
        yield return Caminar(new Vector3(14.5f, 0f, 22f));
        Assert.That(jugador.transform.position.y, Is.GreaterThan(-0.5f), "no se pudo subir por la escotilla");
        yield return Caminar(new Vector3(18.6f, 0f, 22.5f));
        yield return Caminar(new Vector3(18.6f, 0f, 25.3f));
        yield return Iluminar(Buscar<MateriaHueca>("Sello_Este"), 0.6f);
        yield return Esperar(3f);
        Assert.That(Buscar<Compuerta>("Atajo_Este").Abierta, Is.True, "el sello este no abrió el atajo");
        yield return Caminar(new Vector3(9f, 0f, 24.75f));

        // ---- los dos sellos abren el norte: el Cruce
        yield return Esperar(3.5f);
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Cruce), "los dos sellos no abrieron la puerta del norte");
        yield return Caminar(new Vector3(0f, 0f, 24f));
        yield return Caminar(new Vector3(0f, 0f, 32.3f));

        // un ancla detrás de una reja: HUECO, cambiar a CUERPO y encenderla antes de que se cierre
        yield return Iluminar(Buscar<MateriaHueca>("Reja_N_Tapa"), 0.6f);
        yield return Equipar(cuerpo);
        yield return Iluminar(Buscar<Ancla>("Ancla_N_Oculta"), 0.6f);
        yield return Iluminar(Buscar<Ancla>("Ancla_N_Vista"), 0.6f);
        yield return Esperar(0.3f);
        Assert.That(Buscar<PuenteLuz>("Puente_N_Reja").Solido, Is.True, "el puente de la reja no apareció");
        MirarHacia(new Vector3(-0.8f, 0f, 37f));
        yield return Caminar(new Vector3(-0.8f, 0f, 37f));
        yield return Caminar(new Vector3(-0.8f, 0f, 41.5f));
        yield return Caminar(new Vector3(0f, 0f, 42f));

        // el puente con las anclas a la espalda y la reja al final
        yield return Iluminar(Buscar<Ancla>("Ancla_N_Borde_A"), 0.6f);
        yield return Iluminar(Buscar<Ancla>("Ancla_N_Borde_B"), 0.6f);
        yield return Esperar(0.3f);
        Assert.That(Buscar<PuenteLuz>("Puente_N_Borde").Solido, Is.True, "el puente del borde no apareció");
        MirarHacia(new Vector3(0f, 0f, 50f));
        yield return Caminar(new Vector3(0f, 0f, 44.6f));
        yield return Caminar(new Vector3(-1.5f, 0f, 50.1f));
        yield return Equipar(hueco);
        yield return Atravesar("Reja_N_Borde_A", new Vector3(-1.5f, 0f, 50.1f), new Vector3(-1.5f, 0f, 53f));
        yield return Caminar(new Vector3(0f, 0f, 62f));

        // Cresta: el corredor se cierra a la espalda
        yield return Caminar(new Vector3(0f, 0f, 70f));
        yield return Caminar(new Vector3(0f, 0f, 84f));
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Cresta));
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

        // Epílogo: el cráter ya no está (la tierra lo tapó); quedarse en su lugar trae los créditos
        yield return Caminar(Buscar<ZonaJugador>("Zona_LugarDelCrater").transform.position);
        for (float t = 0f; t < 12f && flujo.EtapaActual != FlujoJuegoCrater.Etapa.Finalizado; t += Time.deltaTime)
            yield return null;
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Finalizado), "quedarse en el lugar del cráter no cerró la demo");
    }

    // ------------------------------------------------------------------ acciones

    /// <summary>Camina en línea recta hacia 'destino' (sólo x y z). Falla si en 'limite' segundos no llega.</summary>
    IEnumerator Caminar(Vector3 destino, float limite = 30f)
    {
        for (float t = 0f; t < limite; t += Time.deltaTime)
        {
            Vector3 d = destino - jugador.transform.position;
            d.y = 0f;
            if (d.magnitude < 0.12f) yield break;
            if (cc.enabled) cc.Move(d.normalized * Mathf.Min(Velocidad * Time.deltaTime, d.magnitude));
            yield return null;
        }
        Assert.Fail($"no se pudo llegar a {destino}: el jugador quedó en {jugador.transform.position}");
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

    static IEnumerator Esperar(float segundos)
    {
        for (float t = 0f; t < segundos; t += Time.deltaTime) yield return null;
    }

    static T Buscar<T>(string nombre) where T : Component
    {
        var encontrado = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(c => c.name == nombre);
        Assert.That(encontrado, Is.Not.Null, $"no existe {nombre} en la escena");
        return encontrado;
    }
}
