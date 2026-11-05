using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class FiltroHUDPlayTests
{
    [UnityTest] public IEnumerator TextoCuentaTresSegundosRespetaPausaYVuelveABlanco()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        var p=Object.FindFirstObjectByType<PausaCrater>();p.Reanudar();p.enabled=false;
        Object.FindFirstObjectByType<FlujoJuegoCrater>().StopAllCoroutines();
        Object.FindFirstObjectByType<PrologoCapilla>().StopAllCoroutines();
        var l=LinternaController.Instancia;l.Recoger();l.Encender(true);
        var texto=Object.FindFirstObjectByType<InterfazCrater>().GetComponentsInChildren<Text>(true).Single(t=>t.name=="Estado");
        l.EquiparFiltro(l.filtros[0]);yield return null;yield return null;Assert.That(texto.text,Does.Contain("3 s"));
        yield return new WaitForSeconds(1.2f);float antes=l.TiempoFiltroRestante;Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);Assert.That(l.TiempoFiltroRestante,Is.EqualTo(antes));
        Time.timeScale=5;yield return new WaitForSeconds(2.2f);Assert.That(l.FiltroActual,Is.Null);Assert.That(texto.text,Is.EqualTo("LUZ BLANCA"));
        l.EquiparFiltro(l.filtros[1]);yield return null;yield return null;Assert.That(texto.text,Does.Contain("3 s"));
    }
    [UnityTearDown] public IEnumerator Limpiar(){Time.timeScale=1;yield return null;}
}
