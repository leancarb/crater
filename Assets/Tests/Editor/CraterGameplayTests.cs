using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class CraterGameplayTests
{
    GameObject go;

    [TearDown]
    public void Limpiar()
    {
        if (go != null) UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void SoloExistenLosDosFiltrosDelVerticalSlice()
    {
        CollectionAssert.AreEquivalent(
            new[] { "Ninguno", "Cuerpo", "Hueco" },
            Enum.GetNames(typeof(FiltroDefinicion.Canal)));
    }

    [Test]
    public void ReceptorSoloAceptaElFiltroCorrecto()
    {
        go = new GameObject("AnclaPrueba");
        var ancla = go.AddComponent<Ancla>();
        var cuerpo = ScriptableObject.CreateInstance<FiltroDefinicion>();
        var hueco = ScriptableObject.CreateInstance<FiltroDefinicion>();
        cuerpo.canal = FiltroDefinicion.Canal.Cuerpo;
        hueco.canal = FiltroDefinicion.Canal.Hueco;

        Assert.That(ancla.AceptaFiltro(cuerpo), Is.True);
        Assert.That(ancla.AceptaFiltro(hueco), Is.False);
        Assert.That(ancla.AceptaFiltro(null), Is.False);

        UnityEngine.Object.DestroyImmediate(cuerpo);
        UnityEngine.Object.DestroyImmediate(hueco);
    }

    [Test]
    public void DesbloquearFiltroNoAgregaDuplicados()
    {
        go = new GameObject("LinternaPrueba");
        var linterna = go.AddComponent<LinternaController>();
        var filtro = ScriptableObject.CreateInstance<FiltroDefinicion>();
        int avisos = 0;
        linterna.AlDesbloquearFiltro += _ => avisos++;

        linterna.Desbloquear(filtro);
        linterna.Desbloquear(filtro);

        Assert.That(linterna.filtrosDesbloqueados, Has.Count.EqualTo(1));
        Assert.That(avisos, Is.EqualTo(1));
        UnityEngine.Object.DestroyImmediate(filtro);
    }

    [Test]
    public void RecogerLinternaSoloAvisaUnaVez()
    {
        go = new GameObject("LinternaPrueba");
        var linterna = go.AddComponent<LinternaController>();
        int avisos = 0;
        linterna.AlRecogerLinterna += () => avisos++;

        linterna.Recoger();
        linterna.Recoger();

        Assert.That(linterna.Disponible, Is.True);
        Assert.That(avisos, Is.EqualTo(1));
    }

    [Test]
    public void AdaptacionDebeHabilitarseExplicitamente()
    {
        go = new GameObject("AdaptacionPrueba");
        var adaptacion = go.AddComponent<AdaptacionOscuridad>();

        Assert.That(adaptacion.Habilitada, Is.False);
        adaptacion.Habilitar();
        Assert.That(adaptacion.Habilitada, Is.True);
        adaptacion.Deshabilitar();
        Assert.That(adaptacion.Habilitada, Is.False);
        Assert.That(adaptacion.Progreso, Is.Zero);
    }

    [Test]
    public void PuenteEmpiezaInsolidoYSeActivaConSuAncla()
    {
        go = new GameObject("PuentePrueba");
        var collider = go.AddComponent<BoxCollider>();
        var puente = go.AddComponent<PuenteLuz>();
        Invocar(puente, "Awake");
        Assert.That(collider.enabled, Is.False);

        var anclaGO = new GameObject("AnclaPrueba");
        anclaGO.transform.SetParent(go.transform);
        var ancla = anclaGO.AddComponent<Ancla>();
        var filtro = ScriptableObject.CreateInstance<FiltroDefinicion>();
        filtro.canal = FiltroDefinicion.Canal.Cuerpo;
        filtro.tiempoDeCarga = 0.01f;
        ancla.RecibirLuz(filtro, 1f);
        puente.anclas.Add(ancla);
        Campo(puente, "visibilidad").SetValue(puente, 1f);

        Invocar(puente, "Update");

        Assert.That(puente.Solido, Is.True);
        Assert.That(collider.enabled, Is.True);
        UnityEngine.Object.DestroyImmediate(filtro);
    }

    [Test]
    public void MateriaHuecaNoSeCierraSobreElJugador()
    {
        go = new GameObject("MateriaPrueba");
        var solido = go.AddComponent<BoxCollider>();
        var triggerGO = new GameObject("VolumenSeguro");
        triggerGO.transform.SetParent(go.transform);
        var trigger = triggerGO.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        var materia = go.AddComponent<MateriaHueca>();
        Invocar(materia, "Awake");

        Campo(materia, "disolucion").SetValue(materia, 1f);
        Invocar(materia, "AlActualizar", true);
        Assert.That(solido.enabled, Is.False);

        var jugador = new GameObject("JugadorPrueba") { tag = "Player" };
        var jugadorCollider = jugador.AddComponent<BoxCollider>();
        Invocar(materia, "OnTriggerEnter", jugadorCollider);
        Campo(materia, "disolucion").SetValue(materia, 0f);
        Invocar(materia, "AlActualizar", false);
        Assert.That(solido.enabled, Is.False);

        Invocar(materia, "OnTriggerExit", jugadorCollider);
        Invocar(materia, "AlActualizar", false);
        Assert.That(solido.enabled, Is.True);
        UnityEngine.Object.DestroyImmediate(jugador);
    }

    [Test]
    public void RespawnDevuelveAlUltimoPuntoSeguroYReiniciaMovimiento()
    {
        go = new GameObject("JugadorRespawn");
        var controlador = go.AddComponent<CharacterController>();
        var movimiento = go.AddComponent<JugadorFPS>();
        var respawn = go.AddComponent<RespawnPorCaida>();
        Vector3 seguro = new Vector3(4f, 2f, 8f);
        Quaternion orientacion = Quaternion.Euler(0f, 90f, 0f);
        respawn.RegistrarPuntoSeguro(seguro, orientacion);
        go.transform.position = new Vector3(0f, -30f, 0f);

        respawn.Respawn();

        Assert.That(go.transform.position, Is.EqualTo(seguro + Vector3.up * 0.15f));
        Assert.That(Mathf.DeltaAngle(go.transform.eulerAngles.y, 90f), Is.EqualTo(0f).Within(0.01f));
        Assert.That(controlador.enabled, Is.True);
        Assert.That(movimiento.enabled, Is.True);
    }

    static FieldInfo Campo(object objetivo, string nombre)
    {
        return objetivo.GetType().GetField(nombre, BindingFlags.Instance | BindingFlags.NonPublic);
    }

    static void Invocar(object objetivo, string nombre, params object[] argumentos)
    {
        objetivo.GetType().GetMethod(nombre, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(objetivo, argumentos);
    }
}
