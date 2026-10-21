using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class ConstructorCrater
{
    public const float RadioColumnasRotonda = 8.8f;
    const float HombroObelisco = 5.88f, PuntaObelisco = 6.83f;
    static float SemianchoObelisco(float y) => Mathf.Lerp(.88f,.66f,y/HombroObelisco);

    static void EsculpirObelisco(Kit k, Transform padre, Vector3 pie)
    {
        var v=new List<Vector3>(); var uv=new List<Vector2>();
        void Tri(Vector3 a,Vector3 b,Vector3 c) => Triangulo(v,uv,a,b,c,PixelDeFaceta((a+b+c)/3f));
        void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d) { Tri(a,b,c); Tri(a,c,d); }
        foreach(var n in new[]{Vector3.left,Vector3.right,Vector3.back,Vector3.forward})
        {
            Vector3 derecha=Vector3.Cross(Vector3.up,n);
            float centro=Mathf.Abs(n.x)>.5f?3.23f:4.93f;
            float y0=centro-.64f,y1=centro+.64f;
            Vector3 P(float x,float y,float fondo=0) => derecha*x+Vector3.up*y+n*(SemianchoObelisco(y)-fondo);
            // Paño inferior, superior y laterales rodean una cavidad, sin caras superpuestas.
            Quad(P(-SemianchoObelisco(0),0),P(SemianchoObelisco(0),0),P(SemianchoObelisco(y0),y0),P(-SemianchoObelisco(y0),y0));
            Quad(P(-SemianchoObelisco(y1),y1),P(SemianchoObelisco(y1),y1),P(SemianchoObelisco(HombroObelisco),HombroObelisco),P(-SemianchoObelisco(HombroObelisco),HombroObelisco));
            Quad(P(-SemianchoObelisco(y0),y0),P(-.59f,y0),P(-.59f,y1),P(-SemianchoObelisco(y1),y1));
            Quad(P(.59f,y0),P(SemianchoObelisco(y0),y0),P(SemianchoObelisco(y1),y1),P(.59f,y1));
            Quad(P(-.59f,y0,.075f),P(.59f,y0,.075f),P(.59f,y1,.075f),P(-.59f,y1,.075f));
            Quad(P(-.59f,y0),P(.59f,y0),P(.59f,y0,.075f),P(-.59f,y0,.075f));
            Quad(P(.59f,y1),P(-.59f,y1),P(-.59f,y1,.075f),P(.59f,y1,.075f));
            Quad(P(-.59f,y1),P(-.59f,y0),P(-.59f,y0,.075f),P(-.59f,y1,.075f));
            Quad(P(.59f,y0),P(.59f,y1),P(.59f,y1,.075f),P(.59f,y0,.075f));
            Tri(P(-.66f,HombroObelisco),P(.66f,HombroObelisco),Vector3.up*PuntaObelisco);
        }
        var go=new GameObject("Anillo_Obelisco");go.transform.SetParent(padre,false);
        go.transform.SetPositionAndRotation(pie,Quaternion.identity);go.transform.localScale=Vector3.one;
        // Neutralizar la escala del FBX: las dimensiones de la malla están en metros.
        go.transform.localScale=Vector3.one/padre.lossyScale.x;
        go.AddComponent<MeshFilter>().sharedMesh=GuardarMalla(CrearMalla("Obelisco_Incrustado",v,uv));
        go.AddComponent<MeshRenderer>().sharedMaterial=k.piedra;
        var c=go.AddComponent<BoxCollider>();c.center=Vector3.up*(PuntaObelisco*.5f);c.size=new Vector3(1.76f,PuntaObelisco,1.76f);
        Estatico(go);
    }

    static Renderer AroDelPiso(Kit k, Transform padre, Vector3 centro, bool sol)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();
        for(int i=0;i<48;i++)
        {
            Vector3 P(int j,float r) => new Vector3(Mathf.Cos(j*Mathf.PI/24)*r,0,Mathf.Sin(j*Mathf.PI/24)*r);
            var a=P(i,.61f);var b=P(i+1,.61f);var c=P(i+1,.72f);var d=P(i,.72f);
            Triangulo(v,uv,a,b,c,Vector2.zero);Triangulo(v,uv,a,c,d,Vector2.zero);
        }
        var go=new GameObject("Aro_Luminoso_Piso");go.transform.SetParent(padre,false);
        go.transform.position=new Vector3(centro.x,.025f,centro.z);go.transform.rotation=Quaternion.identity;
        go.AddComponent<MeshFilter>().sharedMesh=GuardarMalla(CrearMalla("Aro_Columna_Piso",v,uv));
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=sol?k.ambar:k.tallaLuna;
        r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        return r;
    }

    static void CartelDeMovimientoInicial(Kit k, Transform padre, Transform inicio)
    {
        var cartel=Grupo(padre,"Cartel_Controles_Inicio");
        Vector3 frente=inicio.forward;frente.y=0;frente.Normalize();
        Vector3 posicion=inicio.position+frente*6f+inicio.right*2.8f+Vector3.up*1.55f;
        Vector3 local=padre.InverseTransformPoint(posicion);local.y=AlturaRelieve(local.x,local.z)+1.6f;posicion=padre.TransformPoint(local);posicion.y=Mathf.Max(posicion.y,inicio.position.y+1.2f);
        Vector3 normal=(inicio.position-posicion);normal.y=0;normal.Normalize();
        Vector3 derecha=Vector3.Cross(Vector3.up,normal);
        Losa(k,cartel,"Control_Mover",posicion,normal,3f,1.9f,Escalado(1.22f,PanelMover));
        foreach(var r in cartel.GetComponentsInChildren<Renderer>())
            if(r.sharedMaterial==k.pinturaHueso)r.sharedMaterial=TintaCartelInicial();
        foreach(float x in new[]{-.95f,.95f})
        {
            var pie=posicion+derecha*x-Vector3.up*.95f;
            Vector3 localPie=padre.InverseTransformPoint(pie);localPie.y=AlturaRelieve(localPie.x,localPie.z);
            float suelo=padre.TransformPoint(localPie).y-.15f;
            Bloque(cartel,"Soporte_Cartel",new Vector3(pie.x,(pie.y+suelo)*.5f,pie.z),new Vector3(.10f,pie.y-suelo,.1f),k.piedra);
        }
    }

    [MenuItem("Crater/Actualizar cartel inicial")]
    public static void ActualizarCartelInicial()
    {
        contenedorMallas=AssetDatabase.LoadMainAssetAtPath(RutaMallasFacetadas);
        var viejo=GameObject.Find("Cartel_Controles_Inicio");if(viejo!=null)Object.DestroyImmediate(viejo);
        var k=new Kit{piedra=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PiedraClara.mat"),pinturaHueso=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PinturaHueso.mat")};
        var exterior=GameObject.Find("06_Capilla").transform;
        CartelDeMovimientoInicial(k,exterior,exterior.Find("SpawnValle"));
        PrepararOclusion(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
    static Material TintaCartelInicial()
    {
        const string ruta="Assets/Materials/CartelInicio_Tinta.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,ruta);}
        m.SetColor("_BaseColor",new Color(.80f,.78f,.69f));EditorUtility.SetDirty(m);return m;
    }
    // Actualizar solamente estas piezas conserva el nivel restaurado y todos sus ajustes.
    [MenuItem("Crater/Actualizar rotonda y carteles")]
    public static void ActualizarRotondaYCarteles()
    {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Detener Play antes de actualizar.");
        contenedorMallas=AssetDatabase.LoadMainAssetAtPath(RutaMallasFacetadas);
        Material M(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/"+n+".mat");
        var k=new Kit {piedra=M("PiedraClara"),piso=M("PisoBasalto"),basaltoMedio=M("BasaltoMedio"),
            ambar=M("Ambar"),tallaLuna=M("TalladoLuna"),tallaEclipse=M("TalladoEclipse"),
            pinturaHueso=M("PinturaHueso"),pinturaSol=M("PinturaSol"),pinturaLuna=M("PinturaLuna")};
        CargarModelos(k);
        var anterior=Object.FindFirstObjectByType<ObeliscoDelEclipse>();
        var jugador=anterior.jugador;var puerta=anterior.puerta;var sol=anterior.sol.sello;var luna=anterior.luna.sello;
        k.audio=new GeneradorAudioCrater.Clips{resonanciaSuave=anterior.resonanciaSol.clip,
            tonosAncla=anterior.notasSol.Select(n=>n.clip).ToArray()};
        var objetos=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var mapa=objetos.Single(t=>t.name=="Anillo_Del_Mapa");
        var senales=anterior.columnasSol.Concat(anterior.columnasLuna).SelectMany(t=>t.renderers).Concat(anterior.sol.renderers).Concat(anterior.luna.renderers).Concat(new[]{anterior.eclipse,anterior.eclipseRegreso}).ToArray();
        foreach(var r in senales)if(r!=null)Object.DestroyImmediate(r.gameObject);
        Object.DestroyImmediate(mapa.gameObject);
        foreach(var t in objetos.Where(t=>t!=null&&((t.name.StartsWith("Anillo_Columna_")&&t.GetComponent<Collider>()==null)||t.name.StartsWith("Obelisco_Eclipse")||(t.name=="Tallado"&&t.GetComponent<Renderer>()!=null&&Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(0,EspacioCrater.Punto(CentroRotonda).z))<3f)||t.name=="Mapa_Del_Progreso"||t.name=="Luz_Abertura_Rotonda"||t.name=="ArquitecturaModular"||t.name=="Control_LuzBlanca"||t.name=="Control_FiltroSol"||t.name=="Control_FiltroLuna"||t.name=="Control_Mover"||t.name=="Cartel_Controles_Inicio"||t.name.StartsWith("RevisionRotonda_"))).ToArray())
            if(t!=null) Object.DestroyImmediate(t.gameObject);
        // Reutilizar exactamente la conversión de coordenadas del constructor completo.
        var temporal=new GameObject("RevisionTemporal").transform;
        foreach(var n in new[]{"01_Explanada","02_Umbral","03_Rotonda","03_Ala_Oeste","03_Ala_Este","04_Cruce","05_Cresta"})Grupo(temporal,n);
        var rotonda=temporal.Find("03_Rotonda");var arte=Grupo(temporal,"ArteTemporal");
        ConstruirAnilloDelMapa(k,rotonda,new Vector3(0,-1.08f,12),.4f);
        var nuevo=MapaYObelisco(k,rotonda,sol,luna);nuevo.jugador=jugador;nuevo.puerta=puerta;nuevo.centro=EspacioCrater.Punto(CentroRotonda);
        AmpliarSubsuelo(temporal,arte);
        var rotondaReal=objetos.First(t=>t!=null&&t.name=="03_Rotonda"&&t.parent.name=="SUBSUELO_Ampliado");
        foreach(var t in rotonda.Cast<Transform>().ToArray())t.SetParent(rotondaReal,true);
        var neutro=GameObject.Find("NIVEL").transform.Find("Objetos_Sin_Distorsion");
        foreach(var t in temporal.Find("Objetos_Sin_Distorsion").Cast<Transform>().ToArray())t.SetParent(neutro,true);
        Object.DestroyImmediate(temporal.gameObject);
        // Volver a dibujar sólo los controles de filtros con las teclas 2 y 3.
        foreach(var lado in new[]{-1f,1f})
        {
            var ala=objetos.First(t=>t!=null&&t.name==(lado<0?"03_Ala_Oeste":"03_Ala_Este"));
            var viejo=ala.Find("Murales_Filtro");if(viejo!=null)Object.DestroyImmediate(viejo.gameObject);
            var grupo=new GameObject("FiltroTemporal").transform;MuralesDelFiltro(k,grupo,lado);
            grupo.position=lado<0?CorrimientoAlaOeste:CorrimientoAlaEste;
            // Las losas conservan proporciones y el resto sigue la ampliación del ala.
            foreach(var losa in grupo.GetComponentsInChildren<Transform>().Where(t=>t.Find("Losa_Piedra")!=null).ToArray())
            {var punto=EspacioCrater.Punto(losa.position);losa.SetParent(null,true);losa.position=punto;losa.SetParent(ala,true);}
            foreach(var hijo in grupo.Cast<Transform>().ToArray())hijo.SetParent(ala,false);
            Object.DestroyImmediate(grupo.gameObject);
        }
        var exterior=GameObject.Find("06_Capilla").transform;var inicio=exterior.Find("SpawnValle");
        CartelDeMovimientoInicial(k,exterior,inicio);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        PrepararOclusion(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
