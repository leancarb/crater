using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
public class ConfirmacionPiedraPlayTests
{
    [UnityTest] public IEnumerator RecogerConfirmaSinAyudasYUnDuplicadoNoRepiteLaConfirmacion()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice"); yield return null;
        Object.FindFirstObjectByType<PausaCrater>().Reanudar();
        var c = Object.FindFirstObjectByType<ColeccionPiedras>();
        var texto = c.interfaz.GetComponentsInChildren<Text>(true).Single(t => t.text == "Piedra tallada recogida");
        bool ayudas = OpcionesCrater.AyudasEscritas;
        if (ayudas) OpcionesCrater.AlternarAyudas();
        try
        {
            Assert.That(c.Registrar(c.piezas[0]), Is.True); yield return null;
            Assert.That(texto.gameObject.activeInHierarchy, Is.True);
            Assert.That(c.recogida.clip, Is.Not.Null); Assert.That(c.recogida.isPlaying, Is.True);
            yield return new WaitForSeconds(1);
            Assert.That(c.Registrar(c.piezas[0]), Is.False);
            yield return new WaitForSeconds(.9f);
            Assert.That(texto.gameObject.activeInHierarchy, Is.False);
        }
        finally { if (ayudas != OpcionesCrater.AyudasEscritas) OpcionesCrater.AlternarAyudas(); }
    }
}
