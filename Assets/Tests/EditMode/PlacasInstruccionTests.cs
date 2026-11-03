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
    [Test] public void CartelesSonFisicosMatesYLosGlifosRespetanProfundidad()
    {
        var glifos=objetos.Select(t=>t.GetComponent<CartelTutorial>()).Where(t=>t!=null).ToArray();
        Assert.That(glifos.Length,Is.EqualTo(12));
        for(int i=1;i<=11;i++)Assert.That(glifos.Any(c=>c.accion==i),Is.True,"Acción "+i);
        foreach(var c in glifos)
        {
            var raiz=c.transform.parent;Assert.That(raiz.Find("Soporte_Cartel"),Is.Not.Null);
            var m=raiz.Find("Imagen_Cartel").GetComponent<Renderer>().sharedMaterial;
            Assert.That(m.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(m.IsKeywordEnabled("_EMISSION"),Is.False);
            var textura=m.GetTexture("_BaseMap");Assert.That(textura.width/(float)textura.height,Is.EqualTo(4f/3).Within(.001));
            Assert.That(c.GetComponent<Renderer>().sharedMaterial.shader.name,Is.EqualTo("Crater/TextoMundo"));
        }
        Assert.That(objetos.Any(t=>t.name.StartsWith("Control_")||t.name.StartsWith("Ensenanza_")),Is.False);
    }
    [Test] public void HistoriaSinPlacasYLatentesConservados()
    {
        var murales=objetos.Select(t=>t.GetComponent<Renderer>()).Where(r=>r!=null&&r.sharedMaterial!=null&&r.sharedMaterial.name.StartsWith("Historia_")).ToArray();
        Assert.That(murales.Length,Is.EqualTo(12));
        foreach(var r in murales)
        {
            Assert.That(r.transform.lossyScale.x/r.transform.lossyScale.y,Is.EqualTo(2).Within(.001));
            Assert.That(r.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.False);
            Assert.That(r.transform.parent.GetComponentsInChildren<Transform>().Any(t=>t.name=="Esquina_L"||t.name=="Losa_Piedra"||t.name=="Fondo_Placa"),Is.False);
        }
        var latentes=objetos.Single(t=>t.name=="MotivosLatentes_Cresta");
        Assert.That(latentes.childCount,Is.EqualTo(5));
        Assert.That(latentes.GetComponentsInChildren<TextMesh>(),Is.Empty);
    }
    static void AssertMate(Material m,string hex,float smooth)
    {
        ColorUtility.TryParseHtmlString(hex,out var c);Assert.That(m.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
        Assert.That(Vector4.Distance(m.GetColor("_BaseColor"),c),Is.LessThan(.001));
        Assert.That(m.GetTexture("_BaseMap"),Is.Null);Assert.That(m.GetFloat("_Smoothness"),Is.EqualTo(smooth).Within(.001));
        Assert.That(m.GetFloat("_Metallic"),Is.Zero);Assert.That(m.IsKeywordEnabled("_EMISSION"),Is.False);Assert.That(m.GetColor("_EmissionColor").maxColorComponent,Is.Zero);
    }
}
