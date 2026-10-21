using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MontanasEncuadreTests
{
    [UnitySetUp] public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        Object.FindFirstObjectByType<PausaCrater>().Reanudar();Object.FindFirstObjectByType<PrologoCapilla>().enabled=false;
    }
    [Test] public void LasMontanasQuedanSobreElTechoYConBaseCerrada()
    {
        var rocas=GameObject.Find("Cierre_Rocoso_Valle").GetComponentsInChildren<MeshCollider>();
        Assert.That(rocas.Length,Is.EqualTo(42));
        foreach(var roca in rocas)
        {
            Assert.That(roca.GetComponent<Renderer>().bounds.min.y,Is.GreaterThan(8.8f),"Una montaña invade el subsuelo");
            var mesh=roca.sharedMesh;
            Assert.That(mesh.triangles.Length,Is.EqualTo(72),"La base debe cerrar los seis lados");
            Assert.That(mesh.normals.Count(n=>n.y<-.99f),Is.EqualTo(18));
        }
    }
    [Test] public void ElCentroDelFondoEsBajoYNoTapaElDiscoSolarDesdeElMirador()
    {
        var cierre=GameObject.Find("Cierre_Rocoso_Valle").transform;
        var exterior=GameObject.Find("06_Capilla").transform;
        var fondo=cierre.GetComponentsInChildren<Renderer>().Where(r=>exterior.InverseTransformPoint(r.transform.position).z< -40).ToArray();
        var central=fondo.Single(r=>Mathf.Abs(exterior.InverseTransformPoint(r.transform.position).x)<1);
        Assert.That(central.bounds.max.y,Is.LessThan(19));
        foreach(var r in fondo.Where(r=>Mathf.Abs(exterior.InverseTransformPoint(r.transform.position).x)>40))
            Assert.That(r.bounds.max.y,Is.GreaterThan(40));
        var ojo=GameObject.Find("Zona_Mirador").transform.position+Vector3.up*1.6f;
        // El sol exterior se encuentra a 12° de elevación y tiene un diámetro de 7°.
        foreach(float elevacion in new[]{8.5f,12f,15.5f})foreach(float azimut in new[]{-3.5f,0,3.5f})
        {
            var direccion=Quaternion.Euler(-elevacion,azimut,0)*Vector3.forward;
            var impactos=Physics.RaycastAll(ojo,direccion,200,~0,QueryTriggerInteraction.Ignore);
            Assert.That(impactos.Any(h=>h.transform.IsChildOf(cierre)),Is.False,"Una cumbre tapa el disco solar");
        }
    }
}
