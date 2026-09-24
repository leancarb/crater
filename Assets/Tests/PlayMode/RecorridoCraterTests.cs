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

        // Prólogo: salir de la capilla dispara el eclipse; la puerta del cráter lleva a la Explanada
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Prologo));
        Assert.That(jugador.transform.position.x, Is.EqualTo(300f).Within(1f), "el jugador no empieza en la capilla");
        yield return Caminar(new Vector3(300f, 0f, 8.2f));
        yield return Esperar(0.2f);
        Assert.That(prologo.Reproduciendo, Is.True, "salir de la capilla no disparó la cinemática");
        for (float t = 0f; t < 40f && prologo.Reproduciendo; t += Time.deltaTime) yield return null;
        Assert.That(prologo.Reproduciendo, Is.False, "la cinemática del eclipse no terminó");
        yield return Caminar(new Vector3(300f, 0f, 33.6f));
        for (float t = 0f; t < 15f && flujo.EtapaActual == FlujoJuegoCrater.Etapa.Prologo; t += Time.deltaTime) yield return null;
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.BuscarLinterna), "la puerta del cráter no llevó a la Explanada");
        Assert.That(jugador.transform.position.x, Is.EqualTo(0f).Within(1f));
        while (!cc.enabled || !prologo.EnElCrater) yield return null;
        yield return Esperar(3f);   // termina el fundido

        // Explanada y rampa
        yield return Caminar(new Vector3(0f, 0f, -18f));
        Assert.That(jugador.transform.position.y, Is.LessThan(0.5f), "la rampa no llega al Umbral");

        // Umbral: linterna y compuerta
        yield return Caminar(new Vector3(1.2f, 0f, -15.5f));
        Assert.That(linterna.Disponible, Is.True, "no se pudo recoger la linterna");
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.EncenderLinterna));
        linterna.Encender(true);
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.AbrirUmbral));
        yield return Iluminar(Buscar<Ancla>("Ancla_Umbral"), 0.8f);
        yield return Esperar(3.5f);
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.BuscarCuerpo), "la compuerta no se abrió");

        // Campo: CUERPO
        yield return Caminar(new Vector3(0f, 0f, 1.5f));
        Assert.That(linterna.EstaDesbloqueado(cuerpo), Is.True, "no se pudo recoger CUERPO");
        yield return Equipar(cuerpo);
        yield return CruzarPuente("Puente_Ensenar", 5.4f, 9.3f, "Ancla_Ensenar_A", "Ancla_Ensenar_B");
        yield return Caminar(new Vector3(0f, 0f, 12.6f));
        yield return CruzarPuente("Puente_Probar", 12.6f, 19.3f, "Ancla_Probar_A", "Ancla_Probar_B");
        yield return Caminar(new Vector3(0f, 0f, 23.5f));
        yield return CruzarPuente("Puente_Torcer", 23.5f, 28.6f, "Ancla_Torcer_A", "Ancla_Torcer_B");

        // Hondonada: HUECO
        yield return Caminar(new Vector3(0f, 0f, 38.5f));
        Assert.That(linterna.EstaDesbloqueado(hueco), Is.True, "no se pudo recoger HUECO");
        yield return Equipar(hueco);
        yield return Atravesar("Reja_Ensenar_A");
        yield return Atravesar("Reja_Zigzag_1");
        yield return Atravesar("Reja_Zigzag_2");
        yield return Atravesar("Reja_Zigzag_3");

        // Torcer: CUERPO para el puente, HUECO para la reja, desde la repisa
        yield return Caminar(new Vector3(0f, 0f, 59.6f));
        yield return Equipar(cuerpo);
        yield return CruzarPuente("Puente_Hondonada", 59.6f, 64.8f, "Ancla_Hondonada_A", "Ancla_Hondonada_B");
        yield return Equipar(hueco);
        var reja = Buscar<MateriaHueca>("Reja_Salida_A");
        yield return Iluminar(reja, 0.8f);
        Assert.That(reja.Solido, Is.False, "la reja de salida no se disolvió desde la repisa");
        yield return Caminar(new Vector3(-1f, 0f, 68f));
        Assert.That(jugador.transform.position.z, Is.GreaterThan(67.5f), "no se pudo atravesar la reja de salida");

        // Cresta: apagar y esperar
        yield return Caminar(new Vector3(0f, 0f, 70f));
        yield return Caminar(new Vector3(0f, 0f, 84f));
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Cresta));
        linterna.Encender(false);
        var puerta = Object.FindFirstObjectByType<PuertaEclipse>();
        for (float t = 0f; t < 25f && !puerta.Abierta; t += Time.deltaTime) yield return null;
        Assert.That(puerta.Abierta, Is.True, "la adaptación no abrió la puerta del eclipse");

        // cruzar la puerta: anillo de diamante, blanco y capilla
        yield return Caminar(new Vector3(0f, 0f, 100.9f));
        for (float t = 0f; t < 30f && flujo.EtapaActual != FlujoJuegoCrater.Etapa.Epilogo; t += Time.deltaTime)
            yield return null;
        Assert.That(flujo.EtapaActual, Is.EqualTo(FlujoJuegoCrater.Etapa.Epilogo), "cruzar la puerta no llevó a la capilla");
        Assert.That(jugador.transform.position.x, Is.EqualTo(300f).Within(1f));

        // Epílogo: el cráter ya no está; quedarse en su lugar trae los créditos
        yield return Caminar(new Vector3(300f, 0f, 34f));
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

    IEnumerator CruzarPuente(string nombre, float zAntes, float zDespues, params string[] anclas)
    {
        var puente = Buscar<PuenteLuz>(nombre);
        yield return Caminar(new Vector3(0f, 0f, zAntes));
        foreach (var ancla in anclas) yield return Iluminar(Buscar<Ancla>(ancla), 0.6f);
        yield return Esperar(0.3f);
        Assert.That(puente.Solido, Is.True, $"{nombre} no apareció");

        fps.MirarHacia(jugador.transform.position + Vector3.forward * 10f + Vector3.up * 1.6f);
        yield return Caminar(new Vector3(0f, 0f, zDespues));
        Assert.That(jugador.transform.position.y, Is.GreaterThan(-0.5f), $"el jugador se cayó cruzando {nombre}");
    }

    IEnumerator Atravesar(string nombre)
    {
        var reja = Buscar<MateriaHueca>(nombre);
        Vector3 p = reja.transform.position;
        yield return Caminar(new Vector3(p.x, 0f, p.z - 2.2f));
        yield return Iluminar(reja, 0.6f);
        yield return Esperar(0.2f);
        Assert.That(reja.Solido, Is.False, $"{nombre} no se disolvió");
        yield return Caminar(new Vector3(p.x, 0f, p.z + 1.4f));
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
