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
    [Test] public void TodasLasPlacasUsanPizarraYTrazoMateConCuatroEsquinasYSpotPropio()
    {
        var placas=objetos.Where(t=>t.name=="PlacaInstruccion").ToArray();Assert.That(placas.Length,Is.EqualTo(16));
        foreach(var p in placas)
        {
            var fondo=p.Find("Fondo_Placa");var m=fondo.GetComponent<Renderer>().sharedMaterial;
            Assert.That(m.name,Is.EqualTo("Placa_Pizarra"),p.parent.name);AssertMate(m,"#2E3540",.1f);
            var tam=fondo.lossyScale;Assert.That(tam.x,Is.GreaterThanOrEqualTo(1.2f-.001f));Assert.That(tam.y,Is.GreaterThanOrEqualTo(.8f-.001f));Assert.That(tam.z,Is.EqualTo(.06f).Within(.001));
            Assert.That(p.Cast<Transform>().Count(t=>t.name=="Esquina_L"),Is.EqualTo(4));
            foreach(var t in p.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Brazo_Horizontal"||r.name=="Brazo_Vertical"))
            {
                Assert.That(t.sharedMaterial.name,Is.EqualTo("Trazo_Instruccion"));AssertMate(t.sharedMaterial,"#F4F1EA",.05f);
                var s=t.transform.lossyScale;Assert.That(Mathf.Min(s.x,s.y),Is.EqualTo(.03f).Within(.001));Assert.That(Mathf.Max(s.x,s.y),Is.EqualTo(.15f).Within(.001));
            }
            foreach(var r in p.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Relieve_Instruccion"))
            {
                Assert.That(r.sharedMaterial.name,Is.EqualTo("Trazo_Instruccion"));AssertMate(r.sharedMaterial,"#F4F1EA",.05f);
                Assert.That(r.GetComponent<MeshFilter>().sharedMesh.bounds.size.z,Is.EqualTo(.01f).Within(.0001));
                var malla=r.GetComponent<MeshFilter>().sharedMesh;
                for(int i=0;i<malla.vertexCount;i+=24)
                    Assert.That(malla.normals[i].z,Is.LessThan(-.99f),"La cara en relieve debe mirar al jugador: "+p.parent.name);
            }
            var spot=p.GetComponentInChildren<Light>();Assert.That(spot.type,Is.EqualTo(LightType.Spot));
            Assert.That(spot.spotAngle,Is.EqualTo(35));Assert.That(spot.range,Is.EqualTo(3));Assert.That(spot.shadows,Is.EqualTo(LightShadows.None));
            Assert.That(spot.transform.position.y,Is.GreaterThan(p.position.y));
            Assert.That(Vector3.Angle(spot.transform.forward,p.position-spot.transform.position),Is.LessThan(.1));
            if(p.parent.name!="Control_Mover")Assert.That(p.position.y,Is.EqualTo(p.parent.name=="Pista_Trampilla_E2"?1.8f:1.6f).Within(.01));
        }
    }
    [Test] public void LaNarrativaEstaPintadaSinLosaNiEsquinasEnL()
    {
        var relatos=objetos.Where(t=>t.name=="Narrativa").Select(t=>t.parent).ToArray();Assert.That(relatos.Length,Is.GreaterThanOrEqualTo(24));
        foreach(var relato in relatos)
        {
            Assert.That(relato.GetComponentsInChildren<Transform>().Any(t=>t.name=="Esquina_L"||t.name=="Losa_Piedra"||t.name=="Fondo_Placa"),Is.False,relato.name);
            foreach(var r in relato.GetComponentsInChildren<Renderer>())Assert.That(r.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.False,relato.name);
        }
        for(int i=1;i<=8;i++)
        {
            var r=objetos.Single(t=>t.name=="Lore_"+i).GetComponentInChildren<Renderer>();
            Assert.That(r.sharedMaterial.name,Is.EqualTo("PinturaHueso"));AssertMate(r.sharedMaterial,"#948C7A",.08f);
        }
        var tipo=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ValidarProyectoCrater")).First(t=>t!=null);
        var problemas=new List<string>();tipo.GetMethod("ValidarEsteticaLectura",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{objetos.Select(t=>t.gameObject).ToArray(),problemas});
        Assert.That(problemas,Is.Empty,string.Join("\n",problemas));
    }
    static void AssertMate(Material m,string hex,float smooth)
    {
        ColorUtility.TryParseHtmlString(hex,out var c);Assert.That(m.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
        Assert.That(Vector4.Distance(m.GetColor("_BaseColor"),c),Is.LessThan(.001));
        Assert.That(m.GetTexture("_BaseMap"),Is.Null);Assert.That(m.GetFloat("_Smoothness"),Is.EqualTo(smooth).Within(.001));
        Assert.That(m.GetFloat("_Metallic"),Is.Zero);Assert.That(m.IsKeywordEnabled("_EMISSION"),Is.False);Assert.That(m.GetColor("_EmissionColor").maxColorComponent,Is.Zero);
    }
}
