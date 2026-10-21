using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class TipografiaCraterTests
{
    GameObject objeto;
    InterfazCrater Crear(bool nueva)
    {
        objeto=new GameObject("PruebaTipografia");
        objeto.SetActive(false);
        var interfaz=objeto.AddComponent<InterfazCrater>();
        interfaz.usarFuenteNuevaEnHUD=nueva;
        objeto.SetActive(true);
        return interfaz;
    }

    [UnityTearDown]
    public IEnumerator Limpiar()
    {
        if(objeto!=null) Object.Destroy(objeto);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ElHudUsaLaFuenteNuevaYConservaLosSimbolosDeControles()
    {
        var interfaz=Crear(true);
        interfaz.MostrarPausa(true);
        yield return null;
        foreach(var texto in objeto.GetComponentsInChildren<Text>(true).Where(t=>new[]{"Estado","Filtros","Prompt","Controles"}.Contains(t.name)))
        {
            Assert.That(texto.font.name,Is.EqualTo("CCElephantmenGreatest Bold"));
            foreach(char c in "↑□○△Á") Assert.That(texto.font.HasCharacter(c),Is.True,c.ToString());
        }
        var controles=objeto.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Controles");
        Assert.That(controles.text,Does.Contain("H / ↑"));
        Assert.That(controles.preferredWidth,Is.LessThan(controles.rectTransform.rect.width));
        Assert.That(controles.preferredHeight,Is.LessThan(controles.rectTransform.rect.height));
    }

    [UnityTest]
    public IEnumerator LaPruebaDelHudSePuedeDesactivarSinCambiarElTitulo()
    {
        var interfaz=Crear(false);
        var transicion=interfaz.MostrarTitulo();transicion.MoveNext();
        yield return null;
        var textos=objeto.GetComponentsInChildren<Text>(true);
        Assert.That(textos.Single(t=>t.name=="Estado").font.name,Is.EqualTo("LegacyRuntime"));
        Assert.That(textos.Single(t=>t.name=="Titulo").font.name,Is.EqualTo("CCElephantmenGreatest Bold"));
    }

    [UnityTest]
    public IEnumerator ElInicioUsaLaFuenteBlancaSobreNegroSinImagen()
    {
        var interfaz=Crear(false);
        interfaz.StartCoroutine(interfaz.MostrarTitulo());
        yield return new WaitForSecondsRealtime(.15f);
        var titulo=objeto.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Titulo");
        var velo=objeto.GetComponentsInChildren<Image>(true).Single(t=>t.name=="Velo");
        Assert.That(titulo.text,Is.EqualTo("CRATER"));
        Assert.That(titulo.font.name,Is.EqualTo("CCElephantmenGreatest Bold"));
        Assert.That(titulo.enabled,Is.True);
        Assert.That(titulo.color.r,Is.EqualTo(1f));
        Assert.That(titulo.color.g,Is.EqualTo(1f));
        Assert.That(titulo.color.b,Is.EqualTo(1f));
        Assert.That(titulo.color.a,Is.GreaterThan(0f).And.LessThan(1f));
        Assert.That(velo.color,Is.EqualTo(Color.black));
        Assert.That(objeto.GetComponentsInChildren<RawImage>(true).Any(r=>r.name=="ImagenTitulo"),Is.False);
    }

    [UnityTest]
    public IEnumerator ElFinalUsaLaFuenteNegraSobreBlancoSinImagen()
    {
        var interfaz=Crear(false);
        interfaz.StartCoroutine(interfaz.MostrarCreditos(new string[0]));
        yield return new WaitForSecondsRealtime(1.7f);
        var titulo=objeto.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Titulo");
        var velo=objeto.GetComponentsInChildren<Image>(true).Single(t=>t.name=="Velo");
        Assert.That(titulo.text,Is.EqualTo("CRATER"));
        Assert.That(titulo.font.name,Is.EqualTo("CCElephantmenGreatest Bold"));
        Assert.That(titulo.enabled,Is.True);
        Assert.That(titulo.color.r,Is.Zero);
        Assert.That(titulo.color.g,Is.Zero);
        Assert.That(titulo.color.b,Is.Zero);
        Assert.That(titulo.color.a,Is.GreaterThan(0f));
        Assert.That(velo.color,Is.EqualTo(Color.white));
        Assert.That(objeto.GetComponentsInChildren<RawImage>(true).Any(r=>r.name=="ImagenTitulo"),Is.False);
    }
}
