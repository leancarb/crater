using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class LegibilidadEpilogoTests
{
    JugadorFPS jugador;PrologoCapilla prologo;
    IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;
        var pausa=Object.FindFirstObjectByType<PausaCrater>();pausa.pausarAlPerderFoco=false;pausa.Reanudar();
        yield return new WaitForSeconds(6);
        jugador=Object.FindFirstObjectByType<JugadorFPS>();prologo=Object.FindFirstObjectByType<PrologoCapilla>();
    }
    void Situar(Vector3 piso,Vector3 mirar)
    {
        var cc=jugador.GetComponent<CharacterController>();cc.enabled=false;jugador.transform.position=piso;jugador.ReiniciarMovimiento();cc.enabled=true;jugador.MirarHacia(mirar);Physics.SyncTransforms();
    }
    Transform Buscar(string nombre)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(t=>t.name==nombre);
    void Captura(string nombre)=>ExploracionCraterTests.Capturar(jugador.GetComponentInChildren<Camera>(),nombre);

    [UnityTest]
    public IEnumerator RegresoCierraLasAberturasSinMarcasYCambiaElSol()
    {
        yield return Preparar();var cielo=Object.FindFirstObjectByType<CieloEclipse>();var inicial=cielo.DireccionSol;
        prologo.SendMessage("EntrarSinTransicion");
        var techo=Object.FindFirstObjectByType<AperturaTecho>();techo.Abrir();yield return new WaitForSeconds(.5f);techo.Cerrar();
        Assert.That(techo.Abriendo,Is.False);Assert.That(Buscar("Luz_Del_Cielo").GetComponent<Light>().enabled,Is.False);
        prologo.PrepararEpilogo();yield return null;Physics.SyncTransforms();
        Assert.That(Buscar("Huella_Crater").gameObject.activeSelf,Is.False);
        Assert.That(Buscar("Guino_Tallado").gameObject.activeSelf,Is.False);
        Assert.That(Buscar("Tapa_Pozo").gameObject.activeSelf,Is.False,"no debe quedar una tapa circular visible sobre el terreno");
        Assert.That(Buscar("Terreno").gameObject.activeSelf,Is.False);
        Assert.That(Buscar("Terreno_Epilogo_Sin_Rastro").gameObject.activeSelf,Is.True);
        Assert.That(Buscar("Cierre_Oculo_Epilogo").gameObject.activeSelf,Is.True);
        Assert.That(Buscar("Cierre_Ranura_Exterior").gameObject.activeSelf,Is.True);
        Assert.That(Vector3.Angle(inicial,cielo.DireccionSol),Is.GreaterThan(60));
        Assert.That(Mathf.Asin(cielo.DireccionSol.y)*Mathf.Rad2Deg,Is.EqualTo(18).Within(.1f));
        var centro=Buscar("Zona_LugarDelCrater").position;centro.y=30;
        foreach(var offset in new[]{Vector3.zero,Vector3.right*5,Vector3.left*5,Vector3.forward*5,Vector3.back*5})
        {
            Assert.That(Physics.Raycast(centro+offset,Vector3.down,out var hit,40,~LayerMask.GetMask("Jugador"),QueryTriggerInteraction.Ignore),Is.True);
            Assert.That(hit.collider.name,Is.EqualTo("Terreno_Epilogo_Sin_Rastro"),"el lugar del cráter tiene un hueco o un rastro físico");
        }
        var spawn=Buscar("SpawnCapilla");Situar(spawn.position,spawn.position+spawn.forward*20);Captura("28_Epilogo_Casa");
        Situar(new Vector3(centro.x,9.05f,centro.z-12),new Vector3(centro.x,8.9f,centro.z));Captura("29_Epilogo_Sin_Rastro");
        jugador.MirarHacia(jugador.camara.position+cielo.DireccionSol*100);cielo.ActualizarVista();Captura("30_Epilogo_Sol_Desplazado");
    }
    [UnityTest]
    public IEnumerator DetallesTienenRespuestaYSeVenDesdeSueloSeguro()
    {
        yield return Preparar();prologo.SendMessage("EntrarSinTransicion");
        var agua=Object.FindFirstObjectByType<AguaEnCuenco>();
        Situar(new Vector3(agua.transform.position.x,.05f,agua.transform.position.z-1.6f),agua.transform.position+Vector3.up*.4f);
        for(int i=0;i<120&&!agua.Lista;i++)yield return null;Assert.That(agua.Lista,Is.True);Captura("21_Cuenco_Origen_Visible");
        var motivo=Object.FindFirstObjectByType<MotivoPorPerspectiva>();
        Situar(new Vector3(motivo.puntoDeVista.x,.05f,motivo.puntoDeVista.z),motivo.transform.position);yield return new WaitForSeconds(.7f);
        Assert.That(motivo.Alineado,Is.True);Assert.That(motivo.fragmentos.Length,Is.EqualTo(6));Assert.That(motivo.fragmentos.All(r=>r.sharedMaterial.IsKeywordEnabled("_EMISSION")),Is.True);Captura("22_Motivo_Completo_Entrada");
        var veta=Object.FindFirstObjectByType<VetaAngular>();var l=veta.linterna;l.Recoger();l.Encender(true);
        Situar(new Vector3(veta.transform.position.x+2,.05f,veta.transform.position.z),veta.transform.position);yield return new WaitForSeconds(1);
        float frontal=veta.Respuesta;Assert.That(frontal,Is.GreaterThan(.4));Captura("23_Veta_Respuesta_Frontal");
        Situar(new Vector3(veta.transform.position.x+1.8f,.05f,veta.transform.position.z-3.5f),veta.transform.position);yield return new WaitForSeconds(1);
        Assert.That(veta.Respuesta,Is.GreaterThan(frontal+.4f));Captura("24_Veta_Respuesta_Oblicua");l.Encender(false);
        var asiento=Buscar("Asiento_Gruta_O1");Situar(new Vector3(asiento.position.x-2,asiento.position.y-.2f,asiento.position.z),asiento.position+Vector3.up*.35f);yield return new WaitForSeconds(.3f);Captura("25_Gruta_Reparacion");
        var regreso=Object.FindFirstObjectByType<RevelacionAlRegresar>();Situar(new Vector3(regreso.transform.position.x,.05f,regreso.transform.position.z-3),regreso.transform.position);
        yield return null;Assert.That(regreso.tallado.enabled,Is.False);Captura("26_Losa_Antes");regreso.Revelar();yield return new WaitForSeconds(4.1f);
        Assert.That(regreso.tallado.enabled,Is.True);Captura("27_Losa_Despues");
        var polvo=Object.FindFirstObjectByType<PolvoEnLuz>();Assert.That(polvo.polvo.main.maxParticles,Is.EqualTo(20));Assert.That(polvo.polvo.particleCount,Is.GreaterThan(0));
        Situar(EspacioCrater.Punto(new Vector3(-2,.05f,8)),polvo.transform.position);yield return new WaitForSeconds(.3f);Captura("31_Polvo_En_Haz");
        var vista=Buscar("Mirador_Lateral_Piso");var valle=Buscar("06_Capilla");
        // Approach from the path-facing open side, walking with the real CharacterController.
        var llegada=vista.position+valle.right*1.8f; llegada.y=vista.position.y+.14f;
        Situar(llegada,vista.position+Vector3.up*.5f);var cc=jugador.GetComponent<CharacterController>();
        for(float t=0;t<1;t+=Time.deltaTime){cc.Move(-valle.right*1.8f*Time.deltaTime);yield return null;}
        Assert.That(Vector3.Distance(new Vector2(jugador.transform.position.x,jugador.transform.position.z),new Vector2(vista.position.x,vista.position.z)),Is.LessThan(.8f));
        var cielo=Object.FindFirstObjectByType<CieloEclipse>();jugador.MirarHacia(jugador.camara.position+cielo.DireccionSol*100);yield return new WaitForSeconds(.3f);cielo.ActualizarVista();
        Assert.That(RenderSettings.fogDensity,Is.EqualTo(.003f).Within(.0001f),"el retorno al mirador conserva la niebla del subsuelo");
        var obstaculos=Physics.RaycastAll(jugador.camara.position,cielo.DireccionSol,150,~LayerMask.GetMask("Jugador"),QueryTriggerInteraction.Ignore).Where(h=>h.collider.GetComponent<Renderer>() is Renderer r&&r!=null&&r.enabled).Select(h=>h.collider.name).ToArray();
        Assert.That(obstaculos,Is.Empty,"geometría visible tapa el eclipse desde el mirador opcional");
        Captura("32_Mirador_Accesible");
    }
}
