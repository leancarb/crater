using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
public class BordesYEncuadreTests
{
    InputSettings.BackgroundBehavior comportamientoFondo;
    InputSettings.EditorInputBehaviorInPlayMode rutaEntradaEditor;
    JugadorFPS j;Keyboard k;InputDevice[] reales;
    Transform Buscar(string n)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name==n);
    [UnitySetUp] public IEnumerator Preparar(){rutaEntradaEditor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;comportamientoFondo=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;yield return SceneManager.LoadSceneAsync("CraterVerticalSlice");yield return null;var p=Object.FindFirstObjectByType<PausaCrater>();p.Reanudar();p.enabled=false;var pro=Object.FindFirstObjectByType<PrologoCapilla>();pro.StopAllCoroutines();pro.enabled=false;Object.FindFirstObjectByType<FlujoJuegoCrater>().StopAllCoroutines();j=Object.FindFirstObjectByType<JugadorFPS>();j.enabled=false;reales=InputSystem.devices.Where(d=>d.enabled&&(d is Keyboard||d is Mouse||d is Gamepad)).ToArray();foreach(var d in reales)InputSystem.DisableDevice(d);k=InputSystem.AddDevice<Keyboard>();Time.timeScale=2;}
    [UnityTearDown] public IEnumerator Limpiar(){InputSystem.settings.editorInputBehaviorInPlayMode=rutaEntradaEditor;InputSystem.settings.backgroundBehavior=comportamientoFondo;Time.timeScale=1;InputSystem.RemoveDevice(k);foreach(var d in reales)if(d.added)InputSystem.EnableDevice(d);yield return null;}
    void Situar(Vector3 p){var c=j.GetComponent<CharacterController>();c.enabled=false;j.transform.position=p;j.ReiniciarMovimiento();c.enabled=true;Physics.SyncTransforms();}
    IEnumerator Mover(Key tecla,float segundos){InputSystem.QueueStateEvent(k,new KeyboardState(tecla));yield return null;var mover=typeof(JugadorFPS).GetMethod("Mover",BindingFlags.Instance|BindingFlags.NonPublic);for(float t=0;t<segundos;t+=Time.deltaTime){mover.Invoke(j,new object[]{Time.deltaTime});yield return null;}}
    [UnityTest] public IEnumerator PresionarElBordeNoImpideRetroceder()
    {
        var ext=Buscar("06_Capilla");var spawn=Buscar("SpawnValle");
        foreach(float x in new[]{14f,-6f})
        {
            var local=ext.InverseTransformPoint(spawn.position);local.x=x;local.z=182.2f;Situar(ext.TransformPoint(local));j.Orientar(spawn.rotation);
            yield return Mover(Key.W,5);var contacto=j.transform.position;float caida=(float)typeof(JugadorFPS).GetField("caida",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(j);
            yield return Mover(Key.S,2);Assert.That(Vector3.Dot(j.transform.position-contacto,-spawn.forward),Is.GreaterThan(2),"x="+x+", caida="+caida+", contacto="+contacto+", salida="+j.transform.position);
        }
    }

    [UnityTest] public IEnumerator InicioIzquierdoConTerrazaTransitable()
    {
        var spawn=Buscar("SpawnValle");var ext=Buscar("06_Capilla");Assert.That(spawn.position.x,Is.LessThan(-18));
        foreach(float x in new[]{4f,14f,22f})foreach(float z in new[]{183f,190f,197f})
        {
            var q=ext.TransformPoint(new Vector3(x,ext.InverseTransformPoint(spawn.position).y+4,z));
            Assert.That(Physics.Raycast(q,Vector3.down,out var h,7,1,QueryTriggerInteraction.Ignore),Is.True);
            Assert.That(h.point.y,Is.GreaterThan(spawn.position.y-1.2f),"La bajada suave conserva suelo continuo bajo la terraza");
        }
        yield return null;
    }
    [UnityTest] public IEnumerator BordesCerradosYOrientadosHaciaFuera()
    {
        var mallas=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("Pirca_Natural_")||f.name=="Cierre_Posterior_Mirador"||f.name=="Cresta_Natural_Mirador"||f.name=="Cierre_Lateral_Izquierdo_Mirador").ToArray();Assert.That(mallas.Length,Is.EqualTo(5));
        foreach(var f in mallas)
        {
            var mesh=f.GetComponent<MeshCollider>().sharedMesh;var v=mesh.vertices;var tr=mesh.triangles;var ids=new Dictionary<Vector3Int,int>();var unicos=new int[v.Length];var aristas=new Dictionary<(int,int),int>();double volumen=0;
            for(int a=0;a<v.Length;a++){var q=Vector3Int.RoundToInt(v[a]*1000);if(!ids.TryGetValue(q,out var id)){id=ids.Count;ids.Add(q,id);}unicos[a]=id;}
            void Arista(int a,int b){var key=a<b?(a,b):(b,a);aristas[key]=aristas.TryGetValue(key,out var n)?n+1:1;}
            for(int a=0;a<tr.Length;a+=3){int x=tr[a],y=tr[a+1],z=tr[a+2];Arista(unicos[x],unicos[y]);Arista(unicos[y],unicos[z]);Arista(unicos[z],unicos[x]);volumen+=Vector3.Dot(v[x]-v[0],Vector3.Cross(v[y]-v[0],v[z]-v[0]))/6d;}
            Assert.That(aristas.Values.All(n=>n==2),Is.True,f.name+": no puede haber extremos ni base abiertos");Assert.That(volumen,Is.GreaterThan(0),f.name+": normales hacia afuera");
        }
        yield return null;
    }
    [UnityTest] public IEnumerator NoQuedanFocosDeCartelesNiPuntosFlotantes()
    {
        var luces=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Assert.That(luces.Any(l=>l.name=="Luz_Cartel"||l.name=="Luz_Mural"||l.name=="Luz_Umbral"||l.name.StartsWith("Luz_Vuelta_")||l.name.StartsWith("Luz_Pasillo_")),Is.False);
        yield return null;
    }
    [UnityTest] public IEnumerator ElEncuadreSeBuscaDeCostadoYSePuedeAlinear()
    {
        var m=Object.FindFirstObjectByType<MotivoPorPerspectiva>();var cam=Camera.main;Situar(new Vector3(0,.05f,5.6f));cam.transform.position=new Vector3(0,1.62f,5.6f);cam.transform.LookAt(m.destino.position);yield return new WaitForSeconds(.2f);Assert.That(m.Alineado,Is.False);
        Assert.That(m.puntoDeVista.x,Is.GreaterThan(4),"El punto de vista queda del lado opuesto al cartel de recogida");
        var centros=m.fragmentos.Select(r=>cam.WorldToViewportPoint(r.transform.position)).ToArray();Assert.That(centros.Max(c=>c.x)-centros.Min(c=>c.x),Is.GreaterThan(.012f));
        Situar(m.puntoDeVista-Vector3.up*1.62f);cam.transform.position=m.puntoDeVista;cam.transform.LookAt(m.destino.position);yield return new WaitForSeconds(.3f);Assert.That(m.Alineado,Is.True);
        var dir=m.destino.position-m.puntoDeVista;Assert.That(Physics.Raycast(m.puntoDeVista,dir.normalized,dir.magnitude-.15f,1,QueryTriggerInteraction.Ignore),Is.False);
    }
    [UnityTest] public IEnumerator ElReflejoSigueLaParedCurvaSinSaltarEntreCaras()
    {
        var c=EspacioCrater.Punto(new Vector3(0,0,87));Situar(c+Vector3.up*.05f);var f=Object.FindFirstObjectByType<LinternaController>();f.Recoger();f.Encender(true);var espejo=Buscar("ParedEspejo").GetComponent<ParedEspejo>();
        foreach(float angulo in new[]{-.18f,-.1f,0,.1f,.18f})
        {
            Camera.main.transform.LookAt(c+new Vector3(Mathf.Sin(angulo)*20,1.62f,Mathf.Cos(angulo)*20));yield return new WaitForSeconds(.12f);Assert.That(espejo.Encandilamiento,Is.GreaterThan(.05f));
            var filtro=espejo.GetComponentInChildren<MeshFilter>();Assert.That(filtro.sharedMesh.name,Is.EqualTo("Reflejo_Cilindrico_Runtime"));
            foreach(var vertice in filtro.sharedMesh.vertices){var q=filtro.transform.TransformPoint(vertice);Assert.That(new Vector2(q.x-c.x,q.z-c.z).magnitude,Is.InRange(14.5f,15.5f));Assert.That(q.y,Is.InRange(-.01f,7.86f));}
        }
    }
    [UnityTest] public IEnumerator ElLateralIzquierdoDelNuevoSpawnNoPermiteCaer()
    {
        var spawn=Buscar("SpawnValle");Situar(spawn.position);j.Orientar(spawn.rotation);float y=spawn.position.y;
        yield return Mover(Key.A,5);
        Assert.That(j.transform.position.y,Is.GreaterThan(y-.6f));Assert.That(j.transform.position.x,Is.GreaterThan(-25.4f));
        var borde=j.transform.position;yield return Mover(Key.D,1);
        Assert.That(j.transform.position.x-borde.x,Is.GreaterThan(2),"También se puede retroceder sin quedar trabado");
    }
    [UnityTest] public IEnumerator LaCompuertaFinalReflejaSoloCuandoCierraElPaso()
    {
        var centro=EspacioCrater.Punto(new Vector3(0,0,87));Situar(centro+Vector3.up*.05f);
        var puerta=Buscar("Cierre_Cresta").GetComponent<Compuerta>();puerta.enabled=false;
        var espejo=Buscar("ParedEspejo").GetComponent<ParedEspejo>();Assert.That(espejo.superficiesCirculares,Does.Contain(puerta.transform.Find("Losa").GetComponent<Collider>()));
        var f=Object.FindFirstObjectByType<LinternaController>();f.Recoger();f.Encender(true);
        puerta.Abrir();puerta.Avanzar(3);Physics.SyncTransforms();
        var losa=puerta.transform.Find("Losa").GetComponent<Renderer>();Camera.main.transform.LookAt(losa.bounds.center);
        yield return new WaitForSeconds(.4f);
        Assert.That(espejo.Encandilamiento,Is.GreaterThan(.05f));Assert.That(espejo.GetComponentInChildren<Renderer>().enabled,Is.True);
        puerta.Cerrar();puerta.Avanzar(3);Physics.SyncTransforms();yield return new WaitForSeconds(.1f);
        Assert.That(espejo.Encandilamiento,Is.Zero,"El vano libre no debe reflejar en el aire");
    }
    [UnityTest] public IEnumerator ElExteriorBajoNoDesapareceYLaCubiertaSoloAbreAlEntrar()
    {
        var vis=Object.FindFirstObjectByType<VisibilidadExterior>();var tapa=Buscar("Cubierta_Exterior_Rotonda");
        Situar(new Vector3(0,5,-50));yield return null;vis.SendMessage("LateUpdate");
        Assert.That(vis.paisaje.Where(r=>r!=null&&r.name.StartsWith("Horizonte")).Any(r=>r.enabled),Is.True);
        Assert.That(tapa.GetComponent<Renderer>().enabled,Is.True);Assert.That(tapa.GetComponent<Collider>().enabled,Is.True);
        Object.FindFirstObjectByType<PrologoCapilla>().SendMessage("EntrarSinTransicion");yield return null;
        Assert.That(tapa.GetComponent<Renderer>().enabled,Is.False);Assert.That(tapa.GetComponent<Collider>().enabled,Is.False);
    }
}
