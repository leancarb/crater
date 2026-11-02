using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlacasInstruccionTests
{
    Scene escena;bool propia;
    Transform[] objetos;
    [SetUp] public void Abrir()
    {
        escena=SceneManager.GetSceneByPath("Assets/Scenes/CraterVerticalSlice.unity");propia=!escena.IsValid()||!escena.isLoaded;
        if(propia)escena=EditorSceneManager.OpenScene("Assets/Scenes/CraterVerticalSlice.unity",OpenSceneMode.Additive);
        objetos=escena.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
    }
    [TearDown] public void Cerrar(){if(propia)EditorSceneManager.CloseScene(escena,true);}
    [Test] public void TodasLasPlacasTienenSoporteBotonYContrasteMate()
    {
        var placas=objetos.Where(t=>t.name=="PlacaInstruccion").ToArray();Assert.That(placas.Length,Is.GreaterThanOrEqualTo(10));
        foreach(var p in placas)
        {
            AssertMate(p.Find("Fondo_Placa").GetComponent<Renderer>().sharedMaterial,"#D1CEC1",.1f);
            Assert.That(p.Find("Poste_Placa"),Is.Not.Null,p.parent.name);
            Assert.That(p.GetComponentInChildren<EtiquetaTutorial>(),Is.Not.Null,p.parent.name);
            foreach(var renderer in p.GetComponentsInChildren<Renderer>())Assert.That(renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.False);
        }
    }
    [Test] public void NarrativaSinPlacaNiEmision()
    {
        var relatos=objetos.Where(t=>t.name=="Narrativa").Select(t=>t.parent).Concat(objetos.Where(t=>t.name.StartsWith("Mural_Runico_"))).Distinct().ToArray();
        Assert.That(relatos.Length,Is.GreaterThanOrEqualTo(17));
        foreach(var relato in relatos)
        {
            Assert.That(relato.GetComponentsInChildren<Transform>().Any(t=>t.name=="Esquina_L"||t.name=="Losa_Piedra"||t.name=="Fondo_Placa"),Is.False,relato.name);
            foreach(var renderer in relato.GetComponentsInChildren<Renderer>())Assert.That(renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.False);
        }
    }
    static void AssertMate(Material m,string hex,float smooth)
    {
        ColorUtility.TryParseHtmlString(hex,out var c);Assert.That(m.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
        Assert.That(Vector4.Distance(m.GetColor("_BaseColor"),c),Is.LessThan(.001));
        Assert.That(m.GetTexture("_BaseMap"),Is.Null);Assert.That(m.GetFloat("_Smoothness"),Is.EqualTo(smooth).Within(.001));
        Assert.That(m.GetFloat("_Metallic"),Is.Zero);Assert.That(m.IsKeywordEnabled("_EMISSION"),Is.False);Assert.That(m.GetColor("_EmissionColor").maxColorComponent,Is.Zero);
    }
}
