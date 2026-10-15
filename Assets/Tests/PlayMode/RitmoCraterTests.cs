using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RitmoCraterTests
{
    JugadorFPS jugador;
    LinternaController linterna;
    CharacterController cc;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");
        yield return null;
        var pausa = Object.FindFirstObjectByType<PausaCrater>();
        pausa.pausarAlPerderFoco = false; pausa.Reanudar();
        Time.timeScale = 1f;
        Object.FindFirstObjectByType<PrologoCapilla>().enabled = false;
        jugador = Object.FindFirstObjectByType<JugadorFPS>();
        cc = jugador.GetComponent<CharacterController>();
        linterna = LinternaController.Instancia;
        linterna.Recoger(); linterna.Encender(true);
    }

    [UnityTearDown]
    public IEnumerator Restaurar()
    {
        Object.FindFirstObjectByType<PausaCrater>()?.Reanudar();
        Time.timeScale = 1f;
        yield return null;
    }

    IEnumerator Equipar(FiltroDefinicion filtro)
    {
        while (linterna.CambiandoFiltro) yield return null;
        linterna.EquiparFiltro(filtro);
        while (linterna.CambiandoFiltro) yield return null;
    }

    [UnityTest]
    public IEnumerator SolYLunaDuranDosSegundosRespetanPausaYVuelvenALuzBlanca()
    {
        jugador.enabled = false;
        foreach (var filtro in linterna.filtros)
        {
            yield return Equipar(filtro);
            Assert.That(linterna.TiempoFiltroRestante, Is.EqualTo(2f).Within(.1f));
            yield return new WaitForSeconds(1.25f);
            Assert.That(linterna.FiltroActual, Is.SameAs(filtro));
            var pausa = Object.FindFirstObjectByType<PausaCrater>();
            pausa.Pausar(); yield return null;
            var controles = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(t => t.name == "Controles");
            Assert.That(controles.text, Does.Contain("H / ↑").And.Contain("R2 / RT").And.Not.Contain("Cruceta"));
            Assert.That(controles.font.HasCharacter('↑'), Is.True, "La fuente debe poder dibujar la flecha arriba.");
            float restante = linterna.TiempoFiltroRestante;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(linterna.TiempoFiltroRestante, Is.EqualTo(restante).Within(.001f));
            pausa.Reanudar();
            yield return new WaitForSeconds(.85f);
            Assert.That(linterna.FiltroActual, Is.Null);
            Assert.That(linterna.Encendida, Is.True);
            Assert.That(linterna.spot.color, Is.EqualTo(linterna.colorBase));
        }
        yield return Equipar(linterna.filtros[0]);
        yield return new WaitForSeconds(.3f);
        linterna.Encender(false);
        yield return new WaitForSeconds(1.85f);
        Assert.That(linterna.FiltroActual, Is.Null, "Apagar no debe congelar el filtro ni renovar sus dos segundos.");
        linterna.Encender(true);
        Assert.That(linterna.FiltroActual, Is.Null);
        yield return Equipar(linterna.filtros[0]);
        Assert.That(linterna.TiempoFiltroRestante, Is.GreaterThan(1.9f), "Puede activarse de nuevo después de volver a luz blanca.");
    }

    [UnityTest]
    public IEnumerator ElJugadorCaminaACuatroSinSprintConShiftNiL3YSoloCorreConR2()
    {
        var teclado = InputSystem.AddDevice<Keyboard>(); teclado.MakeCurrent();
        var mando = InputSystem.AddDevice<Gamepad>(); mando.MakeCurrent();
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        suelo.transform.position = new Vector3(400, -.1f, 0);
        suelo.transform.localScale = new Vector3(60, .2f, 60);
        try
        {
            Situar(new Vector3(400, .05f, 0));
            jugador.Orientar(Quaternion.identity);
            Assert.That(jugador.velocidad, Is.EqualTo(4f));
            foreach (bool shift in new[] { false, true })
            {
                InputSystem.QueueStateEvent(teclado, shift ? new KeyboardState(Key.W, Key.LeftShift) : new KeyboardState(Key.W));
                yield return new WaitForSeconds(.6f);
                Assert.That(new Vector2(cc.velocity.x, cc.velocity.z).magnitude, Is.EqualTo(4f).Within(.15f));
            }
            InputSystem.QueueStateEvent(teclado, new KeyboardState());
            InputSystem.QueueStateEvent(mando, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.LeftStick));
            yield return new WaitForSeconds(.6f);
            Assert.That(new Vector2(cc.velocity.x, cc.velocity.z).magnitude, Is.EqualTo(4f).Within(.15f));
            InputSystem.QueueStateEvent(mando, new GamepadState { leftStick = Vector2.up, rightTrigger = 1f });
            yield return new WaitForSeconds(.6f);
            Assert.That(new Vector2(cc.velocity.x, cc.velocity.z).magnitude, Is.EqualTo(5.6f).Within(.15f));
        }
        finally
        {
            InputSystem.RemoveDevice(teclado); InputSystem.RemoveDevice(mando);
            Object.Destroy(suelo);
        }
    }

    static Vector3 S(float x, float z) => EspacioCrater.Punto(new Vector3(x, .05f, z));
    static Vector3 O(float x, float z) => S(x - 3, z + 11);
    T Buscar<T>(string nombre) where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(c => c.name == nombre);
    void Situar(Vector3 posicion)
    {
        cc.enabled = false; jugador.transform.position = posicion;
        jugador.ReiniciarMovimiento(); cc.enabled = true; Physics.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator CadaParejaExigeApuntarPorSeparadoYPermiteCruzarConUnPulsoSolar()
    {
        jugador.enabled = false;
        var casos = new[] {
            ("Puente_O_Ensenar", O(-24.8f, 1), O(-29.6f, 1), ""),
            ("Puente_O_Torcer", O(-16, 14.3f), O(-16, 19), ""),
            ("Puente_N_Reja", S(-2, 34.5f), S(-2, 40.8f), "Reja_N_Tapa"),
            ("Puente_N_Borde", S(0, 44.5f), S(0, 52), ""),
            ("Puente_N_Salida", S(0, 52), S(0, 56.3f), "Reja_N_Salida")
        };
        foreach (var caso in casos)
        {
            linterna.VolverALuzBlanca();
            Situar(caso.Item2);
            var puente = Buscar<PuenteLuz>(caso.Item1);
            if (caso.Item4 != "")
            {
                yield return Equipar(linterna.filtros[1]);
                var reja = Buscar<MateriaHueca>(caso.Item4);
                jugador.MirarHacia(reja.PuntoDeImpacto);
                yield return new WaitForSeconds(.55f);
                Assert.That(reja.Solido, Is.False, caso.Item4);
            }
            yield return Equipar(linterna.filtros[0]);
            var a = puente.anclas[0]; var b = puente.anclas[1];
            Vector3 ojo = jugador.camara.position;
            Assert.That(Vector3.Angle(a.PuntoDeImpacto - ojo, b.PuntoDeImpacto - ojo),
                Is.GreaterThan(linterna.AnguloActual), caso.Item1 + ": las dos entran en un solo cono");
            jugador.MirarHacia((a.PuntoDeImpacto + b.PuntoDeImpacto) * .5f);
            yield return new WaitForSeconds(.5f);
            Assert.That(puente.anclas.Count(r => r.Activo), Is.LessThan(2), caso.Item1 + ": apuntar al centro enciende ambas");
            jugador.MirarHacia(a.PuntoDeImpacto); yield return new WaitForSeconds(.5f);
            Assert.That(a.Activo, Is.True, a.name);
            jugador.MirarHacia(b.PuntoDeImpacto); yield return new WaitForSeconds(.5f);
            Assert.That(b.Activo && a.Activo, Is.True, caso.Item1 + ": no se encendieron dentro de un pulso");
            yield return new WaitForSeconds(.2f);
            Assert.That(puente.Solido, Is.True, caso.Item1);
            for (float t = 0; t < 8; t += Time.deltaTime)
            {
                var direccion = caso.Item3 - jugador.transform.position; direccion.y = 0;
                if (direccion.magnitude < .12f) break;
                cc.Move(direccion.normalized * Mathf.Min(jugador.velocidad * Time.deltaTime, direccion.magnitude) + Vector3.down * 2 * Time.deltaTime);
                yield return null;
                Assert.That(jugador.transform.position.y, Is.GreaterThan(-.5f), caso.Item1 + ": se cayó cruzando");
            }
            Assert.That(Vector2.Distance(new Vector2(jugador.transform.position.x, jugador.transform.position.z),
                new Vector2(caso.Item3.x, caso.Item3.z)), Is.LessThan(.2f), caso.Item1 + ": no llegó a la otra orilla");
            Assert.That(linterna.FiltroActual, Is.Null, "El filtro debe agotarse sin mantenerlo activo artificialmente.");
        }
    }
}
