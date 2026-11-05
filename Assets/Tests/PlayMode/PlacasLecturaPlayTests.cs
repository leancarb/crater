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
    [UnityTest] public IEnumerator LosTalladosLatentesSeConservanSinPlacasNiEmision()
    {
        var placas=Object.FindObjectsByType<CartelTutorial>(FindObjectsSortMode.None).Select(c=>c.transform.parent).ToArray();
        Assert.That(placas.Length,Is.EqualTo(10));
        var latentes=GameObject.Find("MotivosLatentes_Cresta").transform;
        Assert.That(latentes.childCount,Is.EqualTo(5));
        Assert.That(latentes.GetComponentsInChildren<TextMesh>(),Is.Empty);
        var eclipse=Object.FindFirstObjectByType<EclipseFinalController>();
        typeof(EclipseFinalController).GetMethod("IluminarArte",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(eclipse,new object[]{1f});yield return null;
        foreach(var p in placas)foreach(var r in p.GetComponentsInChildren<Renderer>())
        {
            Assert.That(r.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.False);
            var bloque=new MaterialPropertyBlock();r.GetPropertyBlock(bloque);Assert.That(bloque.GetColor("_EmissionColor").maxColorComponent,Is.Zero);
        }
        Assert.That(Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Any(r=>r.sharedMaterial.name=="TalladoCresta"),Is.True,"Se conservan los tallados latentes de historia, sin convertirlos en tutorial");
    }
}
