using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static partial class ConstructorCrater
{
    static Material placaPizarra,trazoInstruccion;
    static float minimoTrazoInstruccion;
    static Color Hex(string valor){ColorUtility.TryParseHtmlString(valor,out var c);return c;}

    static Material MateLiso(string nombre,string color,float suavidad)
    {
        var m=Opaco(nombre,Hex(color),suavidad);
        foreach(string propiedad in new[]{"_BaseMap","_MainTex","_BumpMap","_MetallicGlossMap","_EmissionMap"})
            if(m.HasProperty(propiedad))m.SetTexture(propiedad,null);
        m.DisableKeyword("_NORMALMAP");m.DisableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(m);return m;
    }
    static void PrepararMaterialesLectura()
    {
        placaPizarra=MateLiso("Placa_Pizarra","#2E3540",.1f);
        trazoInstruccion=MateLiso("Trazo_Instruccion","#F4F1EA",.05f);
        MateLiso("PinturaHueso","#948C7A",.08f);
    }
    static float PisoDePlaca(Transform padre,string nombre,Vector3 punto)
    {
        for(var t=padre;t!=null;t=t.parent)
            if(t.name=="06_Capilla")
            {
                var p=t.InverseTransformPoint(punto);p.y=AlturaRelieve(p.x,p.z);
                return t.TransformPoint(p).y;
            }
        return nombre.StartsWith("Mural_Gruta_")?-3f:0f;
    }
    static Transform RaizLectura(Transform padre,string nombre,Vector3 pared,Vector3 afuera,float separacion)
    {
        var t=new GameObject(nombre).transform;t.SetParent(padre,false);
        t.SetPositionAndRotation(pared+afuera*separacion,Quaternion.LookRotation(-afuera));
        var s=padre.lossyScale;t.localScale=new Vector3(1/s.x,1/s.y,1/s.z);
        var ancla=new GameObject("Plano_Pared").transform;ancla.SetParent(t,false);ancla.position=pared;
        return t;
    }
    static GameObject CajaDePlaca(Transform padre,string nombre,Vector3 posicion,Vector3 escala,Material material,bool colision=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=nombre;
        go.transform.SetParent(padre,false);go.transform.localPosition=posicion;go.transform.localScale=escala;
        go.GetComponent<Renderer>().sharedMaterial=material;
        if(!colision)Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    // Único soporte para todos los tutoriales. Las dimensiones son metros reales, aun bajo el subsuelo ampliado.
    static Transform PlacaInstruccion(Kit k,Transform padre,string nombre,Vector3 posicion,Vector3 afuera,
        Vector2 tamano,Action<Kit,Lienzo> dibujo)
    {
        if(placaPizarra==null||trazoInstruccion==null)PrepararMaterialesLectura();
        float piso=PisoDePlaca(padre,nombre,posicion);posicion.y=piso+(nombre=="Pista_Trampilla_E2"?1.8f:1.6f);
        bool apagar=nombre.StartsWith("Apagar_")||nombre.Contains("Oscuridad");
        if(apagar){tamano=new Vector2(1.6f,.8f);dibujo=PanelApagarLaLuz;}
        var tinta=new Kit{pinturaHueso=trazoInstruccion,pinturaSol=trazoInstruccion,pinturaLuna=trazoInstruccion};
        var lienzo=new Lienzo();float anterior=minimoTrazoInstruccion;
        try{minimoTrazoInstruccion=.04f;dibujo(tinta,lienzo);}finally{minimoTrazoInstruccion=anterior;}
        tamano=new Vector2(Mathf.Max(1.2f,tamano.x),Mathf.Max(.8f,tamano.y));
        if(!apagar && lienzo.capas.Any(c=>c.v.Count>0))
        {
            var puntos=lienzo.capas.SelectMany(c=>c.v).ToArray();
            tamano.x=Mathf.Max(tamano.x,2*puntos.Max(v=>Mathf.Abs(v.x))+.24f);
            tamano.y=Mathf.Max(tamano.y,2*puntos.Max(v=>Mathf.Abs(v.y))+.24f);
        }
        var raiz=RaizLectura(padre,nombre,posicion,afuera,.15f);
        var placa=Grupo(raiz,"PlacaInstruccion");
        CajaDePlaca(placa,"Fondo_Placa",new Vector3(0,0,.03f),new Vector3(tamano.x,tamano.y,.06f),placaPizarra,nombre!="Pista_Trampilla_E2");
        // Restaurar el dibujo amplio del corredor sin ampliar su colisión existente.
        if(nombre=="Ensenanza_Cresta_Espera")
            placa.Find("Fondo_Placa").GetComponent<BoxCollider>().size=new Vector3(1.6f/tamano.x,.8f/tamano.y,1);

        foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})
        {
            var esquina=Grupo(placa,"Esquina_L");
            float px=x*(tamano.x*.5f-.05f),py=y*(tamano.y*.5f-.05f);
            CajaDePlaca(esquina,"Brazo_Horizontal",new Vector3(px-x*.075f,py-y*.015f,-.005f),new Vector3(.15f,.03f,.01f),trazoInstruccion);
            CajaDePlaca(esquina,"Brazo_Vertical",new Vector3(px-x*.015f,py-y*.075f,-.005f),new Vector3(.03f,.15f,.01f),trazoInstruccion);
        }
        if(nombre=="Pista_Trampilla_E2") foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})
        {
            var esquina=Grupo(placa,"Esquina_Interior");
            float px=x*(tamano.x*.5f-.13f),py=y*(tamano.y*.5f-.13f);
            CajaDePlaca(esquina,"Brazo_Horizontal",new Vector3(px-x*.075f,py-y*.015f,-.005f),new Vector3(.15f,.03f,.01f),trazoInstruccion);
            CajaDePlaca(esquina,"Brazo_Vertical",new Vector3(px-x*.015f,py-y*.075f,-.005f),new Vector3(.03f,.15f,.01f),trazoInstruccion);
        }
        foreach(var capa in lienzo.capas)
            if(capa.v.Count>0)CrearPintura(placa,"Relieve_Instruccion",RelieveDeDibujo(capa.v),capa.material,.0f);
        var foco=new GameObject("Spot_Placa");foco.transform.SetParent(placa,false);
        foco.transform.localPosition=new Vector3(0,1.05f,-2.4f);foco.transform.LookAt(raiz.position);
        var luz=foco.AddComponent<Light>();luz.type=LightType.Spot;luz.color=Hex("#F4F1EA");
        luz.intensity=14;luz.spotAngle=35;luz.innerSpotAngle=24;luz.range=3;luz.shadows=LightShadows.None;
        if(nombre=="Control_Mover"||nombre=="Ensenanza_Dos_Anclas")
        {
            float alto=Mathf.Max(.05f,1.6f-tamano.y*.5f);
            CajaDePlaca(placa,"Poste_Placa",new Vector3(0,-tamano.y*.5f-alto*.5f,.04f),new Vector3(.08f,alto,.08f),placaPizarra,true);
        }
        return raiz;
    }
    static List<Vector3> RelieveDeDibujo(List<Vector3> original)
    {
        var v=new List<Vector3>();
        void Tri(Vector3 a,Vector3 b,Vector3 c){v.Add(a);v.Add(b);v.Add(c);}
        for(int i=0;i<original.Count;i+=3)
        {
            var a=original[i];var b=original[i+1];var c=original[i+2];
            if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-12f)continue;
            var d=a+Vector3.back*.01f;var e=b+Vector3.back*.01f;var f=c+Vector3.back*.01f;
            if(Vector3.Cross(b-a,c-a).z<0){Tri(d,e,f);Tri(a,c,b);}
            else{Tri(d,f,e);Tri(a,b,c);}
            foreach(var arista in new[]{(a,b,d,e),(b,c,e,f),(c,a,f,d)})
            {Tri(arista.Item1,arista.Item3,arista.Item4);Tri(arista.Item1,arista.Item4,arista.Item2);}
        }
        return v;
    }
    static Renderer CrearPintura(Transform padre,string nombre,List<Vector3> v,Material material,float z)
    {
        var go=new GameObject(nombre);go.transform.SetParent(padre,false);go.transform.localPosition=Vector3.forward*z;
        go.AddComponent<MeshFilter>().sharedMesh=GuardarMalla(CrearMalla(nombre+"_"+padre.parent.name,v,v.Select(_=>Vector2.zero).ToList()));
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return r;
    }
    static Transform PinturaNarrativa(Kit k,Transform padre,string nombre,Vector3 pared,Vector3 afuera,Action<Kit,Lienzo> dibujo)
    {
        var raiz=RaizLectura(padre,nombre,pared,afuera,.12f);var g=Grupo(raiz,"Narrativa");
        var lienzo=new Lienzo();dibujo(k,lienzo);
        foreach(var capa in lienzo.capas)
        {
            if(capa.v.Count==0)continue;
            int n=capa.v.Count;for(int i=0;i<n;i+=3){capa.v.Add(capa.v[i]);capa.v.Add(capa.v[i+2]);capa.v.Add(capa.v[i+1]);}
            CrearPintura(g,"Pintura_"+capa.material.name,capa.v,capa.material,0);
        }
        return raiz;
    }
    // Extraer la tinta del PNG en geometría: conserva el dibujo sin fondo rectangular ni marco horneado.
    static void NarrativaDesdePNG(Kit k,Transform padre,string nombre,Vector3 pared,Vector3 afuera,string imagen)
    {
        var textura=new Texture2D(2,2);ImageConversion.LoadImage(textura,System.IO.File.ReadAllBytes("Assets/Art/Murals/"+imagen+".png"));
        var pixeles=textura.GetPixels32();int w=textura.width,h=textura.height;var v=new List<Vector3>();
        const int paso=3;
        bool Tinta(int x,int y){var c=pixeles[y*w+x];return c.r>56&&c.r>c.g*1.15f&&c.g>c.b*1.12f;}
        int margenX=Mathf.CeilToInt(w*.045f),margenY=Mathf.CeilToInt(h*.055f);
        for(int y=margenY;y<h-margenY;y+=paso)
            for(int x=margenX;x<w-margenX;)
            {
                if(!Tinta(x,y)){x+=paso;continue;}
                int desde=x;while(x<w-margenX&&Tinta(x,y))x+=paso;
                Rect(v,(float)desde/w*2.1f-1.05f,(float)y/h*1.4f-.7f,(float)x/w*2.1f-1.05f,(float)(y+paso)/h*1.4f-.7f);
            }
        Object.DestroyImmediate(textura);
        var raiz=RaizLectura(padre,nombre,pared,afuera,.12f);Grupo(raiz,"Narrativa");
        int cuenta=v.Count;for(int i=0;i<cuenta;i+=3){v.Add(v[i]);v.Add(v[i+2]);v.Add(v[i+1]);}
        var r=CrearPintura(raiz,"PinturaMural",v,k.pinturaHueso,0);
        r.GetComponent<MeshFilter>().sharedMesh.name="NarrativaPNG_"+imagen;
    }
    static void IconoPower(List<Vector3> v,Vector2 centro,float radio)
    {
        for(int i=0;i<20;i++)
        {
            float a=(130+i*14)*Mathf.Deg2Rad,b=(144+i*14)*Mathf.Deg2Rad;
            Linea(v,centro+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radio,centro+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radio,.04f);
        }
        Linea(v,centro+Vector2.up*radio*.25f,centro+Vector2.up*radio*1.35f,.04f);
    }
    static void PanelApagarLaLuz(Kit k,Lienzo l)
    {
        var v=l.En(k.pinturaHueso);
        Persona(v,-.60f,-.23f,.42f,.06f,0,true);
        var punta=Linterna(v,new Vector2(-.54f,.05f),0,.10f);Cono(v,punta,0,.06f,25);
        Flecha(v,new Vector2(-.33f,0),new Vector2(-.19f,0),.04f);
        IconoPower(v,Vector2.zero,.115f);
        Flecha(v,new Vector2(.20f,0),new Vector2(.34f,0),.04f);
        OjoCerrado(v,.53f,-.01f,.30f);
        foreach(var e in new[]{new Vector2(.40f,.18f),new Vector2(.57f,.23f),new Vector2(.69f,.14f)})
            FiguraEn(v,Figura.Estrella,e.x,e.y,.15f);
    }
    static void PlacasApagarCresta(Kit k,Transform padre)
    {
        Losa(k,padre,"Apagar_Fondo",new Vector3(0,1.6f,99),Vector3.back,1.6f,.8f,PanelApagarLaLuz);
        foreach(float z in new[]{83.5f,90.5f})foreach(float lado in new[]{-1f,1f})
            Losa(k,padre,$"Apagar_{(lado<0?"Izq":"Der")}_{z:0}",new Vector3(lado*12,1.6f,z),lado<0?Vector3.right:Vector3.left,1.6f,.8f,PanelApagarLaLuz);
    }

    static void AplicarEsteticaLectura(Kit k)
    {
        PrepararMaterialesLectura();k.pinturaHueso=AssetDatabase.LoadAssetAtPath<Material>(CarpetaMateriales+"PinturaHueso.mat");
        var instrucciones=new (string n,Vector2 tam,Action<Kit,Lienzo> pintar)[]{
            ("Control_Mover",new Vector2(3,1.9f),Escalado(1.22f,PanelMover)),
            ("Control_Linterna",new Vector2(2.8f,1.75f),Escalado(1.15f,PanelLinterna)),
            ("Control_FiltroSol",new Vector2(2.2f,1.5f),PanelFiltroSol),("Control_FiltroLuna",new Vector2(2.2f,1.5f),PanelFiltroLuna),
            ("Mural_Ancla_Puerta",new Vector2(3,1.9f),PanelAnclaYPuerta),
            ("Mural_Anclas_Del_Techo",new Vector2(3.6f,2.2f),PanelAnclasDelTecho),
            ("Ensenanza_Dos_Anclas",new Vector2(3.2f,1.15f),Escalado(.64f,PanelDosAnclas)),
            ("Ensenanza_Cruce_Combinacion",new Vector2(4.2f,1.25f),Escalado(.7f,PanelCombinacion)),
            ("Ensenanza_Cresta_Reflejo",new Vector2(2.8f,1.8f),PanelEncandilado),
            ("Ensenanza_Cresta_Espera",new Vector2(2.8f,1.8f),PanelEnLaOscuridad)};
        foreach(var d in instrucciones)
        {
            var t=BuscarObjetoDocente(d.n);if(t==null)continue;
            var padre=t.parent;var normal=-t.forward;
            var punto=t.Find("Plano_Pared")!=null?t.Find("Plano_Pared").position:t.position-normal*.15f;
            if(d.n=="Ensenanza_Dos_Anclas")punto=EspacioCrater.Punto(new Vector3(-26.2f,1.6f,-2.2f)+CorrimientoAlaOeste);
            if(d.n=="Ensenanza_Cruce_Combinacion"){punto=EspacioCrater.Punto(new Vector3(-5.9f,1.6f,32.5f));normal=Vector3.right;}
            Object.DestroyImmediate(t.gameObject);PlacaInstruccion(k,padre,d.n,punto,normal,d.tam,d.pintar);
        }
        var dibujos=new (string n,Action<Kit,Lienzo> pintar)[]{
            ("Mural_Cruce_Valle",PanelValleEclipse),("Mural_Cruce_Chakana",PanelChakanaGrande),("Mural_Tabique_E3",PanelNocheDeLuna),
            ("Detalle_Luna_Reparo",PanelReparoLuna),
            ("Mural_Gruta_O1",PanelValleEclipse),("Mural_Gruta_O3",PanelValleEclipse),
            ("Mural_Gruta_N1",PanelValleEclipse),("Mural_Gruta_N2",PanelValleEclipse),
            ("Friso_Umbral_Oeste",(kk,l)=>FrisoEscalonado(l.En(kk.pinturaSol),12,.6f)),
            ("Friso_Umbral_Este",(kk,l)=>FrisoEscalonado(l.En(kk.pinturaLuna),12,.6f)),
            ("Friso_O3_Chakanas",(kk,l)=>{for(int i=0;i<6;i++)Chakana(l.En(kk.pinturaSol),-2.5f+i,0,.62f);}),
            ("Friso_E3_Espirales",(kk,l)=>{for(int i=0;i<6;i++)Espiral(l.En(kk.pinturaLuna),-2.5f+i,0,.3f);})};
        foreach(var d in dibujos)RedibujarNarrativa(k,d.n,d.pintar);
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Friso_Cruce_")).ToArray())
            RedibujarNarrativa(k,t.name,(kk,l)=>FrisoCerros(l.En(kk.pinturaHueso),4,.8f));
        for(int i=1;i<=8;i++)
        {
            var t=BuscarObjetoDocente("Lore_"+i);if(t==null)continue;
            var padre=t.parent;var normal=-t.forward;var p=t.Find("Plano_Pared")!=null?t.Find("Plano_Pared").position:t.position-normal*.15f;
            Object.DestroyImmediate(t.gameObject);
            NarrativaDesdePNG(k,padre,"Lore_"+i,p,normal,$"mural_explanada_{(i<=4?"izq":"der")}_{(i-1)%4+1:00}");
        }
        var latentes=BuscarObjetoDocente("MotivosLatentes_Cresta");
        if(latentes!=null)
        {
            foreach(var t in latentes.Cast<Transform>().Where(t=>t.name.StartsWith("Apagar_")||t.name=="Fondo"||t.name.StartsWith("Izq_")||t.name.StartsWith("Der_")).ToArray())
            {
                var n=t.name.StartsWith("Apagar_")?t.name:"Apagar_"+t.name;var normal=-t.forward;
                var punto=t.Find("Plano_Pared")!=null?t.Find("Plano_Pared").position:t.position-normal*.14f;
                Object.DestroyImmediate(t.gameObject);PlacaInstruccion(k,latentes,n,punto,normal,new Vector2(1.6f,.8f),PanelApagarLaLuz);
            }
        }
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name=="Soporte_Ensenanza_Sol"||t.name=="Soporte_Combinacion"||t.name=="Soporte_Cartel").ToArray())
            Object.DestroyImmediate(t.gameObject);
        var eclipse=Object.FindFirstObjectByType<EclipseFinalController>();
        if(eclipse!=null){var so=new SerializedObject(eclipse);so.FindProperty("arteCresta").ClearArray();so.ApplyModifiedPropertiesWithoutUndo();}
        // Las actualizaciones locales reemplazan sus mallas; no acumular copias huérfanas en el contenedor.
        var usadas=new HashSet<Mesh>(Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(f=>f.sharedMesh));
        foreach(var m in AssetDatabase.LoadAllAssetRepresentationsAtPath(RutaMallasFacetadas).OfType<Mesh>())
            if(!usadas.Contains(m)&&(m.name.StartsWith("Relieve_Instruccion_")||m.name.StartsWith("NarrativaPNG_")||m.name.StartsWith("Pintura_Pintura")))
            {AssetDatabase.RemoveObjectFromAsset(m);Object.DestroyImmediate(m,true);}
    }
    static void RedibujarNarrativa(Kit k,string nombre,Action<Kit,Lienzo> dibujo)
    {
        var t=BuscarObjetoDocente(nombre);if(t==null)return;var padre=t.parent;var normal=-t.forward;
        var p=t.Find("Plano_Pared")!=null?t.Find("Plano_Pared").position:t.position-normal*.15f;
        if(nombre.StartsWith("Mural_Gruta_"))p.y=-1.5f;
        Object.DestroyImmediate(t.gameObject);PinturaNarrativa(k,padre,nombre,p,normal,dibujo);
    }
    [MenuItem("Crater/Actualizar placas de instrucción y narrativa")]
    public static void ActualizarPlacasYNarrativa()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Detener Play antes de actualizar.");
        contenedorMallas=AssetDatabase.LoadMainAssetAtPath(RutaMallasFacetadas);
        Material M(string n)=>AssetDatabase.LoadAssetAtPath<Material>(CarpetaMateriales+n+".mat");
        var k=new Kit{piedra=M("PiedraClara"),pinturaHueso=M("PinturaHueso"),pinturaSol=M("PinturaSol"),pinturaLuna=M("PinturaLuna")};
        AplicarEsteticaLectura(k);AssetDatabase.SaveAssets();
        var escena=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);PrepararOclusion(escena);EditorSceneManager.SaveScene(escena);
        var placas=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name=="PlacaInstruccion")
            .Select(t=>new {nombre=t.parent.name,ubicacion=t.parent.position.ToString("F2"),zona=t.parent.parent.name}).OrderBy(p=>p.nombre).ToArray();
        System.IO.Directory.CreateDirectory("Logs/PlacasInstruccion");
        System.IO.File.WriteAllLines("Logs/PlacasInstruccion/placas-migradas.tsv",placas.Select(p=>$"{p.nombre}\t{p.zona}\t{p.ubicacion}"));
    }
}
