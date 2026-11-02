using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PlacasLecturaPlayTests
{
    [UnitySetUp] public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        Object.FindFirstObjectByType<PausaCrater>().Reanudar();Object.FindFirstObjectByType<PrologoCapilla>().enabled=false;
    }
    [UnityTest] public IEnumerator LasIndicacionesDeApagadoSonCincoPlacasYNoRecuperanEmisionEnElFinal()
    {
        var placas=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="PlacaInstruccion").ToArray();
        var apagado=placas.Where(t=>t.parent.name.StartsWith("Apagar_")).ToArray();
        Assert.That(apagado.Length,Is.EqualTo(5));
        foreach(var p in apagado)
        {
            var size=p.Find("Fondo_Placa").lossyScale;Assert.That(size.x,Is.GreaterThanOrEqualTo(1.2f));Assert.That(size.y,Is.GreaterThanOrEqualTo(.8f));
            Assert.That(p.GetComponentsInChildren<Renderer>().Any(r=>r.name=="Relieve_Instruccion"),Is.True);
        }
        var eclipse=Object.FindFirstObjectByType<EclipseFinalController>();
        typeof(EclipseFinalController).GetMethod("IluminarArte",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(eclipse,new object[]{1f});yield return null;
        foreach(var p in placas)foreach(var r in p.GetComponentsInChildren<Renderer>())
        {
            Assert.That(r.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.False);
            var bloque=new MaterialPropertyBlock();r.GetPropertyBlock(bloque);Assert.That(bloque.GetColor("_EmissionColor").maxColorComponent,Is.Zero);
        }
        Assert.That(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Any(r=>r.sharedMaterial.name=="TalladoCresta"),Is.False,"Sin glifos flotantes antiguos");
    }
}
