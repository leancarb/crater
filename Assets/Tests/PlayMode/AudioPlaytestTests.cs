using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class AudioPlaytestTests
{
    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");
        yield return null;
        var pausa = Object.FindFirstObjectByType<PausaCrater>();
        pausa.pausarAlPerderFoco = false;
        pausa.Reanudar();
        Object.FindFirstObjectByType<PrologoCapilla>().enabled = false;
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator ElZumbidoSaleSuavementeYSeCruzaAlCambiarDeFiltro()
    {
        var l = LinternaController.Instancia;
        l.Recoger(); l.EquiparFiltro(l.filtros[0]); l.Encender(true);
        yield return new WaitForSeconds(.8f);
        var fuentes = l.GetComponentsInChildren<AudioSource>().Where(a => a.name.StartsWith("Zumbido_")).ToArray();
        Assert.That(fuentes.Length, Is.EqualTo(2));
        var primera = fuentes.Single(a => a.clip == l.filtros[0].zumbido);
        float volumen = primera.volume;
        Assert.That(primera.isPlaying, Is.True); Assert.That(volumen, Is.GreaterThan(0f));
        l.Encender(false);
        Assert.That(primera.isPlaying, Is.True, "No cortar el bucle al apagar.");
        Assert.That(primera.volume, Is.EqualTo(volumen));
        yield return new WaitForSeconds(.07f);
        Assert.That(primera.volume, Is.InRange(.001f, volumen - .001f));
        yield return new WaitForSeconds(.25f);
        Assert.That(primera.isPlaying, Is.False);
        l.Encender(true); yield return new WaitForSeconds(.3f);
        l.EquiparFiltro(l.filtros[1]);
        Assert.That(primera.isPlaying, Is.True, "El filtro anterior conserva su cola.");
        yield return new WaitForSeconds(.07f);
        var segunda = fuentes.Single(a => a.clip == l.filtros[1].zumbido);
        Assert.That(primera.volume, Is.GreaterThan(0f)); Assert.That(segunda.volume, Is.GreaterThan(0f));
        yield return new WaitForSeconds(.25f);
        Assert.That(primera.isPlaying, Is.False); Assert.That(segunda.isPlaying, Is.True);
        l.VolverALuzBlanca(); yield return new WaitForSeconds(.3f);
        Assert.That(fuentes.All(a => !a.isPlaying && a.volume == 0f), Is.True);
        Assert.That(l.GetComponent<AudioSource>().clip, Is.Null, "Los clicks usan una fuente independiente.");
    }

    [UnityTest]
    public IEnumerator LaResonanciaDelObeliscoDesvaneceSuColaYRespetaPausa()
    {
        var obelisco = Object.FindFirstObjectByType<ObeliscoDelEclipse>();
        var fuente = obelisco.resonanciaSol;
        float volumen = fuente.volume;
        fuente.Play(); fuente.time = fuente.clip.length - fuente.pitch * .65f;
        yield return new WaitForSeconds(.1f);
        float cola = fuente.volume;
        Assert.That(cola, Is.InRange(.001f, volumen - .001f));
        yield return new WaitForSeconds(.15f);
        Assert.That(fuente.volume, Is.LessThan(cola));
        var pausa = Object.FindFirstObjectByType<PausaCrater>(); pausa.Pausar();
        float detenido = fuente.volume;
        yield return new WaitForSecondsRealtime(.1f);
        Assert.That(fuente.volume, Is.EqualTo(detenido).Within(.0001f));
        pausa.Reanudar(); yield return new WaitForSeconds(.6f);
        Assert.That(fuente.isPlaying, Is.False);
    }
}
