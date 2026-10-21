using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public class CompuertaFeedbackPlayTests
{
    [UnityTest] public IEnumerator AbrirConfirmaConTemblorCortoSinCambiarLaMirada()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice"); yield return null;
        Object.FindFirstObjectByType<PausaCrater>().Reanudar();
        Object.FindFirstObjectByType<PrologoCapilla>().enabled = false;
        var j = Object.FindFirstObjectByType<JugadorFPS>(); j.movimientoBloqueado = true;
        yield return null;
        var baseLocal = j.camara.localPosition; var mirada = j.camara.localRotation;
        var puerta = new GameObject("Compuerta de prueba"); puerta.transform.position = j.camara.position + Vector3.forward * 2;
        var c = puerta.AddComponent<Compuerta>(); c.abrirSoloPorOrden = true; c.Abrir();
        float maximo = 0;
        for (float t=0; t<.35f; t+=Time.deltaTime)
        { yield return new WaitForFixedUpdate(); maximo = Mathf.Max(maximo, Vector3.Distance(baseLocal,j.camara.localPosition)); }
        yield return null; // El Update siguiente restablece la posición al terminar el pulso.
        Assert.That(maximo, Is.InRange(.0001f,.012f));
        Assert.That(Vector3.Distance(baseLocal,j.camara.localPosition), Is.LessThan(.0001f));
        Assert.That(Quaternion.Angle(mirada,j.camara.localRotation), Is.LessThan(.001f));
        Object.Destroy(puerta);
    }
}
