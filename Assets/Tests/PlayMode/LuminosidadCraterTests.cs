using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LuminosidadCraterTests
{
    bool teniaNueva, teniaAnterior;
    float nueva, anterior;
    PausaCrater pausa;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        teniaNueva=PlayerPrefs.HasKey(OpcionesCrater.ClaveBrillo);
        teniaAnterior=PlayerPrefs.HasKey("crater.brillo");
        nueva=PlayerPrefs.GetFloat(OpcionesCrater.ClaveBrillo);
        anterior=PlayerPrefs.GetFloat("crater.brillo");
        PlayerPrefs.DeleteKey(OpcionesCrater.ClaveBrillo);
        PlayerPrefs.DeleteKey("crater.brillo");
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");
        yield return null;
        pausa=Object.FindFirstObjectByType<PausaCrater>();
        pausa.pausarAlPerderFoco=false;
        pausa.Reanudar();
        OpcionesCrater.Cargar();
    }

    [UnityTearDown]
    public IEnumerator Restaurar()
    {
        if(pausa!=null) pausa.Reanudar();
        if(teniaNueva) PlayerPrefs.SetFloat(OpcionesCrater.ClaveBrillo,nueva);
        else PlayerPrefs.DeleteKey(OpcionesCrater.ClaveBrillo);
        if(teniaAnterior) PlayerPrefs.SetFloat("crater.brillo",anterior);
        else PlayerPrefs.DeleteKey("crater.brillo");
        OpcionesCrater.Guardar(); OpcionesCrater.Cargar();
        Time.timeScale=1f; AudioListener.pause=false;
        yield return null;
    }

    [Test]
    public void ParteMasClaroYMigraSinPerderUnAjusteExplicito()
    {
        Assert.That(OpcionesCrater.Brillo,Is.EqualTo(.75f));
        PlayerPrefs.SetFloat("crater.brillo",0f); OpcionesCrater.Cargar();
        Assert.That(OpcionesCrater.Brillo,Is.EqualTo(.75f));
        PlayerPrefs.SetFloat("crater.brillo",1.25f); OpcionesCrater.Cargar();
        Assert.That(OpcionesCrater.Brillo,Is.EqualTo(1.25f));
        OpcionesCrater.EstablecerBrillo(100f); OpcionesCrater.Guardar(); OpcionesCrater.Cargar();
        Assert.That(OpcionesCrater.Brillo,Is.EqualTo(OpcionesCrater.BrilloMaximo));
        OpcionesCrater.EstablecerBrillo(-100f);
        Assert.That(OpcionesCrater.Brillo,Is.EqualTo(OpcionesCrater.BrilloMinimo));
    }

    [UnityTest]
    public IEnumerator ElMouseCambiaLaExposicionEnPausaYSeGuardaAlSeguir()
    {
        pausa.Pausar(); yield return null;
        var slider=Object.FindObjectsByType<Slider>(FindObjectsSortMode.None).Single(s=>s.name=="Luminosidad");
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)slider.transform;
        var lugar=rect.TransformPoint(new Vector3(rect.rect.xMin+rect.rect.width*.8f,rect.rect.center.y));
        var puntero=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,lugar),button=PointerEventData.InputButton.Left};
        var impactos=new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(puntero,impactos);
        Assert.That(impactos.Any(h=>h.gameObject.transform.IsChildOf(slider.transform)||h.gameObject==slider.gameObject),Is.True,"El slider debe recibir el mouse real");
        slider.OnPointerDown(puntero); slider.OnDrag(puntero);
        yield return null;
        Assert.That(Time.timeScale,Is.Zero);
        Assert.That(OpcionesCrater.Brillo,Is.GreaterThan(1.4f));
        var adaptacion=Object.FindFirstObjectByType<AdaptacionOscuridad>();
        adaptacion.GetComponent<Volume>().profile.TryGet<ColorAdjustments>(out var color);
        Assert.That(color.postExposure.value,Is.EqualTo(OpcionesCrater.Brillo).Within(.01f));
        float elegido=OpcionesCrater.Brillo;
        pausa.Reanudar(); OpcionesCrater.Cargar();
        Assert.That(OpcionesCrater.Brillo,Is.EqualTo(elegido));
    }

    [UnityTest]
    public IEnumerator ElSliderAceptaNavegacionYLosTextosEntran()
    {
        pausa.Pausar(); yield return null; Canvas.ForceUpdateCanvases();
        var slider=EventSystem.current.currentSelectedGameObject.GetComponent<Slider>();
        float antes=OpcionesCrater.Brillo;
        ExecuteEvents.Execute(slider.gameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Right},ExecuteEvents.moveHandler);
        Assert.That(OpcionesCrater.Brillo,Is.GreaterThan(antes));
        foreach(var t in slider.transform.parent.GetComponentsInChildren<Text>().Where(t=>t.name=="Controles"||t.name=="AyudaLuminosidad"))
        {
            Assert.That(t.preferredHeight,Is.LessThanOrEqualTo(t.rectTransform.rect.height),t.name);
            Assert.That(t.font.name,Is.EqualTo("LegacyRuntime"));
        }
    }
}
