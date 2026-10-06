using NUnit.Framework;
using UnityEngine;

public class ColeccionCraterTests
{
    GameObject raiz;ColeccionPiedras coleccion;
    PiezaTallada Crear(string id,int indice)
    {
        var go=new GameObject(id);go.transform.SetParent(raiz.transform);var p=go.AddComponent<PiezaTallada>();p.identificador=id;p.indice=indice;p.coleccion=coleccion;return p;
    }
    [SetUp]public void Preparar(){raiz=new GameObject("Prueba_Coleccion");coleccion=raiz.AddComponent<ColeccionPiedras>();}
    [TearDown]public void Limpiar(){Object.DestroyImmediate(raiz);}
    [Test]public void IdentificadorEIndiceSoloSeRegistranUnaVez()
    {
        var a=Crear("piedra_a",0);var b=Crear("piedra_a",1);var c=Crear("piedra_b",0);coleccion.piezas=new[]{a,b,c};
        Assert.That(a.Recoger(),Is.True);Assert.That(a.Recoger(),Is.False);Assert.That(b.Recoger(),Is.False);Assert.That(c.Recoger(),Is.False);
        Assert.That(coleccion.Cantidad,Is.EqualTo(1));Assert.That(coleccion.Completa,Is.False);
    }
    [TestCase(-1)][TestCase(6)]public void RechazaIndicesFueraDelConjunto(int indice)
    {
        var p=Crear("fuera",indice);coleccion.piezas=new[]{p};Assert.That(p.Recoger(),Is.False);Assert.That(coleccion.Cantidad,Is.Zero);
    }
    [Test]public void RechazaPiezasAjenasYSinIdentificador()
    {
        var p=Crear("",0);coleccion.piezas=new[]{p};Assert.That(p.Recoger(),Is.False);
        var otro=Crear("ajeno",1);Assert.That(otro.Recoger(),Is.False);Assert.That(coleccion.Registrar(null),Is.False);
    }
}
