using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class FiltroHUDPlayTests
{
    [UnityTest] public IEnumerator ElRelojRespetaCambioTresSegundosPausaYRetornoAutomatico()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice"); yield return null;
        Object.FindFirstObjectByType<PausaCrater>().Reanudar();
        var l = LinternaController.Instancia; l.Recoger(); l.Encender(true);
        var ui = Object.FindFirstObjectByType<InterfazCrater>();
        var barra = ui.GetComponentsInChildren<Image>(true).Single(i => i.name == "Restante");
        var aviso = ui.GetComponentsInChildren<Text>(true).Single(t => t.text == "Filtro agotado · LUZ BLANCA");
        l.EquiparFiltro(l.filtros[0]); yield return null;
        Assert.That(barra.fillAmount, Is.EqualTo(1).Within(.02));
        yield return new WaitForSeconds(l.demoraDeCambio + 1.5f);
        Assert.That(barra.fillAmount, Is.InRange(.4f,.6f));
        float antes = barra.fillAmount; Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(barra.fillAmount, Is.EqualTo(antes).Within(.02)); Time.timeScale = 1;
        yield return new WaitForSeconds(1.6f);
        Assert.That(l.FiltroActual, Is.Null); Assert.That(barra.gameObject.activeInHierarchy, Is.False);
        Assert.That(aviso.gameObject.activeInHierarchy, Is.True);
        yield return new WaitForSeconds(1.9f); Assert.That(aviso.gameObject.activeInHierarchy, Is.False);
        l.EquiparFiltro(l.filtros[1]); yield return null;
        Assert.That(barra.fillAmount, Is.EqualTo(1).Within(.02));
    }
    [UnityTearDown] public IEnumerator Restaurar() { Time.timeScale = 1; yield return null; }
}
