using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class AtmosferaCraterTests
{
    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");
        yield return null;
        var pausa = Object.FindFirstObjectByType<PausaCrater>();
        pausa.pausarAlPerderFoco = false; pausa.Reanudar();
    }

    [UnityTest]
    public IEnumerator LasBasesDeLosPilaresSeEnciendenSinIconosNiRellenosElevados()
    {
        var prologo = Object.FindFirstObjectByType<PrologoCapilla>();
        prologo.SendMessage("EntrarSinTransicion"); prologo.enabled = false;
        var o = Object.FindFirstObjectByType<ObeliscoDelEclipse>();
        var columnas = o.columnasSol.Concat(o.columnasLuna).ToArray();
        Assert.That(columnas.Length, Is.EqualTo(6));
        Assert.That(o.luzColumnasSol, Is.Null);
        Assert.That(o.luzColumnasLuna, Is.Null);
        foreach (var c in columnas)
        {
            Assert.That(c.luz.type, Is.EqualTo(LightType.Spot));
            Assert.That(c.luz.transform.position.y, Is.LessThan(.5f));
            Assert.That(c.luz.transform.forward.y, Is.GreaterThan(.8f));
            Assert.That(c.renderers.Select(r => r.name), Is.EqualTo(new[] { "Difusor_Base" }));
            Assert.That(c.luz.enabled, Is.False);
        }
        columnas[0].Encender();
        yield return new WaitForSeconds(.6f);
        Assert.That(columnas[0].luz.enabled, Is.True);
        Assert.That(columnas[0].luz.intensity, Is.GreaterThan(15f));
        Assert.That(columnas.Skip(1).All(c => !c.luz.enabled), Is.True);

    }

    [UnityTest]
    public IEnumerator LaExposicionInteriorNoAclaraElExteriorNiModificaElPerfilCompartido()
    {
        var a = Object.FindFirstObjectByType<AdaptacionOscuridad>();
        var v = a.GetComponent<Volume>();
        v.sharedProfile.TryGet<ColorAdjustments>(out var compartido);
        float original = compartido.postExposure.value;
        var go = new GameObject("ObservadorDePrueba");
        try
        {
            a.observador = go.transform;
            go.transform.position = Vector3.up * 10f;
            Assert.That(a.ExposicionBase, Is.Zero);
            go.transform.position = Vector3.up * 5f;
            Assert.That(a.ExposicionBase, Is.InRange(.01f, .64f));
            go.transform.position = Vector3.up * 1.7f;
            a.Deshabilitar();
            yield return null;
            v.profile.TryGet<ColorAdjustments>(out var instancia);
            Assert.That(a.ExposicionBase, Is.EqualTo(.65f));
            Assert.That(instancia.postExposure.value, Is.EqualTo(.65f + OpcionesCrater.Brillo).Within(.001f));
            Assert.That(compartido.postExposure.value, Is.EqualTo(original));
        }
        finally { Object.Destroy(go); }
    }

    [Test]
    public void SoloLasAnclasDePuertaTienenLamparaSuperior()
    {
        var lamparas = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name.StartsWith("Lampara_Ancla_") || t.name == "Lampara_Sello_Oeste")
            .Select(t => t.name).ToArray();
        Assert.That(lamparas, Is.EquivalentTo(new[] { "Lampara_Ancla_Umbral", "Lampara_Ancla_O_Puerta", "Lampara_Sello_Oeste" }));
    }
}
