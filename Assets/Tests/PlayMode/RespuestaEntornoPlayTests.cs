using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class RespuestaEntornoPlayTests
{
    [UnityTest] public IEnumerator HudSinReticulaNiBarraDeCarga()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");
        yield return null;
        var hud = GameObject.Find("HUD_CRATER");
        Assert.That(hud, Is.Not.Null);
        Assert.That(hud.transform.Find("Mira"), Is.Null);
        Assert.That(hud.transform.Find("FondoCarga"), Is.Null);
        Assert.That(hud.transform.Find("Carga"), Is.Null);
    }

    [UnityTest] public IEnumerator PulsoDeAnclaNoCargaNiActiva()
    {
        float escala = Time.timeScale;
        Time.timeScale = 1;
        var objeto = new GameObject("AnclaPruebaVisual");
        var ancla = objeto.AddComponent<Ancla>();
        ancla.brillo = objeto.AddComponent<Light>();
        ancla.retencion = 5;
        yield return null;
        ancla.AvisarFiltroIncompatible();
        yield return new WaitForSeconds(.3f);
        Assert.That(ancla.brillo.intensity, Is.GreaterThan(0));
        Assert.That(ancla.Carga, Is.Zero);
        Assert.That(ancla.Activo, Is.False);
        Assert.That(ancla.retencion, Is.EqualTo(5));
        yield return new WaitForSeconds(.8f);
        Assert.That(ancla.brillo.enabled, Is.False);
        Object.Destroy(objeto);
        Time.timeScale = escala;
    }

    [UnityTest] public IEnumerator PulsoDeRejaConservaColorYColision()
    {
        float escala = Time.timeScale;
        Time.timeScale = 1;
        var objeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var renderer = objeto.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        var reja = objeto.AddComponent<MateriaHueca>();
        reja.sellos = new[] { renderer };
        yield return null;
        var bloque = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(bloque);
        float antes = bloque.GetColor("_EmissionColor").b;
        reja.AvisarFiltroIncompatible();
        yield return new WaitForSeconds(.3f);
        renderer.GetPropertyBlock(bloque);
        Color color = bloque.GetColor("_EmissionColor");
        Assert.That(color.b, Is.GreaterThan(antes));
        Assert.That(color.b, Is.GreaterThan(color.r));
        Assert.That(reja.Carga, Is.Zero);
        Assert.That(reja.Disolucion, Is.Zero);
        Assert.That(reja.Solido, Is.True);
        Assert.That(objeto.GetComponent<Collider>().enabled, Is.True);
        Object.Destroy(objeto);
        Object.Destroy(material);
        Time.timeScale = escala;
    }
}
