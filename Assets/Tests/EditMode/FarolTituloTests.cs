using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class FarolTituloTests
{
    [Test]
    public void LaFuenteDelTituloIncluyeLaPalabraConAcento()
    {
        var fuente=Resources.Load<Font>("Fonts/CCElephantmenGreatest Bold");
        Assert.That(fuente,Is.Not.Null);
        foreach(char c in "CRÁTER") Assert.That(fuente.HasCharacter(c),Is.True,c.ToString());
    }

    [TestCase("Jugador")]
    [TestCase("Recogible_Linterna")]
    public void ElFarolConservaSuDifusorYNoAgregaColisiones(string nombre)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+nombre+".prefab");
        var difusor=prefab.GetComponentsInChildren<Renderer>(true).Single(r=>r.name=="Lens_Difusor");
        var modelo=difusor.transform.parent;
        Assert.That(modelo.GetComponentsInChildren<Collider>(true),Is.Empty);
        Assert.That(modelo.GetComponentsInChildren<Light>(true),Is.Empty);
        Assert.That(modelo.Find("JaulaYAsa"),Is.Not.Null);
        Assert.That(difusor.sharedMaterial.IsKeywordEnabled("_EMISSION"),Is.True);
        Assert.That(difusor.sharedMaterial.GetColor("_EmissionColor"),Is.EqualTo(Color.black));
        if(nombre=="Jugador")
        {
            var visual=prefab.GetComponentInChildren<LinternaVisual>(true);
            Assert.That(visual.modelo,Is.EqualTo(modelo));
            Assert.That(visual.lentes,Does.Contain(difusor));
            Assert.That(visual.linterna.duracionFiltro,Is.EqualTo(3f));
        }
    }
}
