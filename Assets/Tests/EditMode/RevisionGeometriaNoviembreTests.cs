using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public class RevisionGeometriaNoviembreTests
{
    Transform[] objetos;
    [SetUp] public void Preparar()
    {
        var escena=EditorSceneManager.OpenScene("Assets/Scenes/CraterVerticalSlice.unity");
        objetos=escena.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
    }
    Transform Buscar(string n)=>objetos.Single(t=>t.name==n);
    [Test] public void LaGuardaNoReiniciaNiSuperponeUnDibujoPorCadaBloque()
    {
        var grupo=Buscar("Frisos_Lobby_Continuos");var mallas=grupo.GetComponentsInChildren<MeshFilter>();
        Assert.That(mallas.Length,Is.EqualTo(2));
        foreach(var f in mallas)
        {
            var m=f.sharedMesh;Assert.That(m.vertexCount,Is.GreaterThan(1000));
            var material=f.GetComponent<Renderer>().sharedMaterial;Assert.That(material.IsKeywordEnabled("_EMISSION"),Is.False);
            var v=m.vertices;var tr=m.triangles;var caras=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<tr.Length;i+=3)
            {
                var puntos=new[]{v[tr[i]],v[tr[i+1]],v[tr[i+2]]};
                var key=string.Join(";",puntos.Select(p=>Vector3Int.RoundToInt(p*10000).ToString()).OrderBy(s=>s));
                Assert.That(caras.Add(key),Is.True,"No puede haber caras duplicadas en las juntas");
            }
        }
    }
    [Test] public void ElPisoNoTieneTriangulosLargosYLaParedUsaUnTonoPorCara()
    {
        var f=Buscar("Piso_Cresta").GetComponent<MeshFilter>();var v=f.sharedMesh.vertices;var tr=f.sharedMesh.triangles;
        for(int i=0;i<tr.Length;i+=3)
        {
            var a=v[tr[i]];var b=v[tr[i+1]];var c=v[tr[i+2]];
            Assert.That(Mathf.Max(Vector3.Distance(a,b),Vector3.Distance(b,c),Vector3.Distance(c,a)),Is.LessThan(4.3f));
            Assert.That(Vector3.Cross(b-a,c-a).y,Is.GreaterThan(0),"El piso mira hacia arriba");
        }
        var pared=Buscar("Pared_Final_Facetada").GetComponent<MeshFilter>().sharedMesh;var uv=pared.uv;
        for(int i=0;i<uv.Length;i+=3){Assert.That(uv[i],Is.EqualTo(uv[i+1]));Assert.That(uv[i],Is.EqualTo(uv[i+2]));}
        foreach(string nombre in new[]{"Dintel_Corredor","Jamba_Circular_-1","Jamba_Circular_1"})
            foreach(var p in Buscar(nombre).GetComponent<MeshFilter>().sharedMesh.vertices)
                Assert.That(Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.y),Mathf.Abs(p.z)),Is.LessThanOrEqualTo(.5001f),"El perímetro del vano no se deforma ni abre rendijas");
    }
    [Test] public void CartelesDeInicioEstanJuntoAlPrimerTramo()
    {
        var exterior=Buscar("06_Capilla");var spawn=exterior.InverseTransformPoint(Buscar("SpawnValle").position);
        Assert.That(spawn.x,Is.EqualTo(20).Within(.01f));Assert.That(spawn.z,Is.EqualTo(197).Within(.01f));
        foreach(string nombre in new[]{"Cartel_01_Mover","Cartel_02_Saltar"})
        {
            var p=exterior.InverseTransformPoint(Buscar(nombre).position);
            Assert.That(p.x,Is.InRange(9,18));Assert.That(p.z,Is.GreaterThanOrEqualTo(188));
        }
        Assert.That(Buscar("Colision_Lateral_Mirador").GetComponent<BoxCollider>().size.y,Is.GreaterThan(2));
    }
}
