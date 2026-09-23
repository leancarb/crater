using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>Reglas de las mecánicas, sin escena: rápidas y deterministas.</summary>
public class CraterGameplayTests
{
    readonly List<UnityEngine.Object> creados = new List<UnityEngine.Object>();

    [TearDown]
    public void Limpiar()
    {
        foreach (var o in creados) if (o != null) UnityEngine.Object.DestroyImmediate(o);
        creados.Clear();
    }

    GameObject Crear(string nombre)
    {
        var go = new GameObject(nombre);
        creados.Add(go);
        return go;
    }

    FiltroDefinicion Filtro(FiltroDefinicion.Canal canal, float tiempoDeCarga = 0.4f)
    {
        var f = ScriptableObject.CreateInstance<FiltroDefinicion>();
        f.canal = canal;
        f.tiempoDeCarga = tiempoDeCarga;
        creados.Add(f);
        return f;
    }

    [Test]
    public void SoloExistenLosDosFiltrosDelVerticalSlice()
    {
        CollectionAssert.AreEquivalent(new[] { "Ninguno", "Cuerpo", "Hueco" },
            Enum.GetNames(typeof(FiltroDefinicion.Canal)));
    }

    [Test]
    public void ReceptorSoloAceptaElFiltroCorrecto()
    {
        var ancla = Crear("Ancla").AddComponent<Ancla>();
        ancla.canalRequerido = FiltroDefinicion.Canal.Cuerpo;

        Assert.That(ancla.AceptaFiltro(Filtro(FiltroDefinicion.Canal.Cuerpo)), Is.True);
        Assert.That(ancla.AceptaFiltro(Filtro(FiltroDefinicion.Canal.Hueco)), Is.False);
        Assert.That(ancla.AceptaFiltro(null), Is.False);
    }

    [Test]
    public void ReceptorSinCanalAceptaLaLuzBlanca()
    {
        var ancla = Crear("Ancla").AddComponent<Ancla>();
        ancla.canalRequerido = FiltroDefinicion.Canal.Ninguno;

        ancla.RecibirLuz(null, 1f);

        Assert.That(ancla.Activo, Is.True);
    }

    [Test]
    public void ReceptorRetieneLaActivacionYDespuesSeApaga()
    {
        var ancla = Crear("Ancla").AddComponent<Ancla>();
        ancla.retencion = 1f;
        var cuerpo = Filtro(FiltroDefinicion.Canal.Cuerpo, 0.4f);

        ancla.RecibirLuz(cuerpo, 0.2f);
        ancla.Avanzar(0.2f);
        Assert.That(ancla.Activo, Is.False, "a mitad de carga todavía no se activa");

        ancla.RecibirLuz(cuerpo, 0.2f);
        ancla.Avanzar(0.2f);
        Assert.That(ancla.Activo, Is.True);

        ancla.Avanzar(0.6f);
        Assert.That(ancla.Activo, Is.True, "dentro de la retención sigue activa");
        Assert.That(ancla.RetencionRestante, Is.EqualTo(0.4f).Within(0.01f));

        ancla.Avanzar(0.5f);   // se agota la retención y empieza a descargarse
        ancla.Avanzar(0.5f);
        Assert.That(ancla.Activo, Is.False);
        Assert.That(ancla.Carga, Is.Zero);
    }

    [Test]
    public void LinternaNoSeEnciendeSinRecogerla()
    {
        var linterna = Crear("Linterna").AddComponent<LinternaController>();
        linterna.requiereRecogerla = true;

        linterna.Encender(true);
        Assert.That(linterna.Encendida, Is.False);

        linterna.Recoger();
        linterna.Encender(true);
        Assert.That(linterna.Encendida, Is.True);
    }

    [Test]
    public void RecogerLinternaSoloAvisaUnaVez()
    {
        var linterna = Crear("Linterna").AddComponent<LinternaController>();
        int avisos = 0;
        linterna.AlRecogerLinterna += () => avisos++;

        linterna.Recoger();
        linterna.Recoger();

        Assert.That(linterna.Disponible, Is.True);
        Assert.That(avisos, Is.EqualTo(1));
    }

    [Test]
    public void DesbloquearFiltroNoAgregaDuplicados()
    {
        var linterna = Crear("Linterna").AddComponent<LinternaController>();
        var filtro = Filtro(FiltroDefinicion.Canal.Cuerpo);
        int avisos = 0;
        linterna.AlDesbloquearFiltro += _ => avisos++;

        linterna.Desbloquear(filtro);
        linterna.Desbloquear(filtro);

        Assert.That(linterna.filtrosDesbloqueados, Has.Count.EqualTo(1));
        Assert.That(avisos, Is.EqualTo(1));
    }

    [Test]
    public void CambiarDeFiltroTieneDemora()
    {
        var linterna = Crear("Linterna").AddComponent<LinternaController>();
        linterna.demoraDeCambio = 10f;
        var cuerpo = Filtro(FiltroDefinicion.Canal.Cuerpo);
        var hueco = Filtro(FiltroDefinicion.Canal.Hueco);

        linterna.EquiparFiltro(cuerpo);
        linterna.EquiparFiltro(hueco);

        Assert.That(linterna.FiltroActual, Is.SameAs(cuerpo), "durante el cambio no se puede volver a cambiar");
        Assert.That(linterna.CambiandoFiltro, Is.True);
    }

    [Test]
    public void RecogibleDesbloqueaSuFiltroYSeDesactiva()
    {
        var linterna = Crear("Linterna").AddComponent<LinternaController>();
        var filtro = Filtro(FiltroDefinicion.Canal.Hueco);
        var go = Crear("Recogible");
        var collider = go.AddComponent<SphereCollider>();
        var recogible = go.AddComponent<Recogible>();
        recogible.filtro = filtro;

        Assert.That(recogible.Recoger(linterna), Is.True);
        Assert.That(recogible.Recoger(linterna), Is.False);
        Assert.That(linterna.EstaDesbloqueado(filtro), Is.True);
        Assert.That(collider.enabled, Is.False);
    }

    [Test]
    public void AdaptacionDebeHabilitarseExplicitamente()
    {
        var adaptacion = Crear("Adaptacion").AddComponent<AdaptacionOscuridad>();

        Assert.That(adaptacion.Habilitada, Is.False);
        adaptacion.Habilitar();
        Assert.That(adaptacion.Habilitada, Is.True);
        adaptacion.Deshabilitar();
        Assert.That(adaptacion.Habilitada, Is.False);
        Assert.That(adaptacion.Progreso, Is.Zero);
    }

    [Test]
    public void PuenteEmpiezaInsolidoYAparecePorCompletoConSusAnclas()
    {
        var go = Crear("Puente");
        var collider = go.AddComponent<BoxCollider>();
        var puente = go.AddComponent<PuenteLuz>();
        var ancla = Crear("Ancla").AddComponent<Ancla>();
        ancla.retencion = 5f;
        puente.anclas.Add(ancla);

        puente.Avanzar(0f);
        Assert.That(collider.enabled, Is.False);
        Assert.That(puente.Solido, Is.False);

        ancla.RecibirLuz(Filtro(FiltroDefinicion.Canal.Cuerpo, 0.01f), 1f);
        for (int i = 0; i < 10; i++) puente.Avanzar(0.1f);

        Assert.That(puente.Visibilidad, Is.EqualTo(1f));
        Assert.That(puente.Solido, Is.True);
        Assert.That(collider.enabled, Is.True);
    }

    [Test]
    public void PuenteNecesitaTodasSusAnclas()
    {
        var puente = Crear("Puente").AddComponent<PuenteLuz>();
        var a = Crear("A").AddComponent<Ancla>();
        var b = Crear("B").AddComponent<Ancla>();
        puente.anclas.Add(a);
        puente.anclas.Add(b);

        a.RecibirLuz(Filtro(FiltroDefinicion.Canal.Cuerpo, 0.01f), 1f);
        for (int i = 0; i < 10; i++) puente.Avanzar(0.1f);

        Assert.That(puente.Solido, Is.False);
    }

    [Test]
    public void MateriaHuecaNoSeCierraSobreElJugador()
    {
        var go = Crear("Materia");
        var solido = go.AddComponent<BoxCollider>();
        var trigger = go.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        var materia = go.AddComponent<MateriaHueca>();
        materia.canalRequerido = FiltroDefinicion.Canal.Hueco;
        materia.retencion = 0f;
        var hueco = Filtro(FiltroDefinicion.Canal.Hueco, 0.01f);

        materia.RecibirLuz(hueco, 1f);
        materia.Avanzar(1f);
        Assert.That(solido.enabled, Is.False, "iluminada se vuelve atravesable");

        var jugador = Crear("Jugador");
        jugador.tag = "Player";
        var colliderJugador = jugador.AddComponent<CapsuleCollider>();
        Invocar(materia, "OnTriggerEnter", colliderJugador);

        for (int i = 0; i < 5; i++) materia.Avanzar(0.5f);   // pierde la luz con el jugador adentro
        Assert.That(materia.Activo, Is.False);
        Assert.That(solido.enabled, Is.False, "no se reconstruye con el jugador adentro");

        Invocar(materia, "OnTriggerExit", colliderJugador);
        materia.Avanzar(0.1f);
        Assert.That(solido.enabled, Is.True);
    }

    [Test]
    public void CompuertaSeAbreCuandoSusReceptoresEstanActivos()
    {
        var compuerta = Crear("Compuerta").AddComponent<Compuerta>();
        var ancla = Crear("Ancla").AddComponent<Ancla>();
        ancla.canalRequerido = FiltroDefinicion.Canal.Ninguno;
        compuerta.receptores.Add(ancla);
        compuerta.desplazamiento = Vector3.down * 4f;
        compuerta.duracion = 1f;
        int avisos = 0;
        compuerta.alAbrirse = new UnityEngine.Events.UnityEvent();
        compuerta.alAbrirse.AddListener(() => avisos++);

        compuerta.Avanzar(0.1f);
        Assert.That(compuerta.Abierta, Is.False);

        ancla.RecibirLuz(null, 1f);
        for (int i = 0; i < 15; i++) compuerta.Avanzar(0.1f);

        Assert.That(compuerta.Abierta, Is.True);
        Assert.That(avisos, Is.EqualTo(1));
        Assert.That(compuerta.transform.localPosition.y, Is.EqualTo(-4f).Within(0.01f));
    }

    [Test]
    public void RespawnDevuelveAlUltimoPuntoSeguro()
    {
        var go = Crear("Jugador");
        var controlador = go.AddComponent<CharacterController>();
        go.AddComponent<JugadorFPS>();
        var respawn = go.AddComponent<RespawnPorCaida>();
        var seguro = new Vector3(4f, 2f, 8f);
        respawn.RegistrarPuntoSeguro(seguro, Quaternion.Euler(0f, 90f, 0f));
        go.transform.position = new Vector3(0f, -30f, 0f);

        respawn.Respawn();

        Assert.That(go.transform.position, Is.EqualTo(seguro + Vector3.up * 0.15f));
        Assert.That(Mathf.DeltaAngle(go.transform.eulerAngles.y, 90f), Is.EqualTo(0f).Within(0.01f));
        Assert.That(controlador.enabled, Is.True);
    }

    static void Invocar(object objetivo, string metodo, params object[] argumentos)
    {
        objetivo.GetType().GetMethod(metodo, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Invoke(objetivo, argumentos);
    }
}
