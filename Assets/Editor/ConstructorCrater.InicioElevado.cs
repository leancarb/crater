using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Base de terreno para sustituir después las rocas por piezas del kit modular.</summary>
public static partial class ConstructorCrater
{
    public const float InicioElevadoZ = 184f;
    public const float FinBajadaInicialZ = 150f;
    public const float DesnivelInicio = 5f;
    // Primero se descubre el objetivo; el sendero se busca hacia la derecha de la vista.
    public static readonly Vector2[] CaminoBajadaInicial = {
        new Vector2(0,184), new Vector2(-12,184), new Vector2(-19,177),
        new Vector2(-19,165), new Vector2(-12,154), new Vector2(0,150)
    };

    // Conserva exactamente el relieve anterior al llegar al punto de inicio viejo.
    static float AlturaInicioElevado(float x, float z)
    {
        float anterior = AlturaRelieve(x, z);
        if (z <= FinBajadaInicialZ)
        {
            float centro = 3*Mathf.Sin((z-80)*Mathf.PI/70);
            float banco = Mathf.Exp(-Mathf.Pow((Mathf.Abs(x-centro)-7)/2.5f,2));
            float tramo = Mathf.SmoothStep(0,1,(z-80)/8)*Mathf.SmoothStep(0,1,(150-z)/8);
            return anterior+1.25f*banco*tramo;
        }
        float cota = AlturaRelieve(0,150);
        float fueraX = Mathf.Max(0,Mathf.Abs(x)-9);
        float fueraZ = Mathf.Max(179.7f-z,z-193,0);
        float meseta = 1-Mathf.SmoothStep(0,1,Mathf.Sqrt(fueraX*fueraX+fueraZ*fueraZ)/4);
        float altura = Mathf.Lerp(anterior,cota+DesnivelInicio,meseta);
        float largo=0;
        for(int i=1;i<CaminoBajadaInicial.Length;i++)largo+=Vector2.Distance(CaminoBajadaInicial[i-1],CaminoBajadaInicial[i]);
        float distancia=float.MaxValue,avance=0,acumulado=0;
        var p=new Vector2(x,z);
        for(int i=1;i<CaminoBajadaInicial.Length;i++)
        {
            var a=CaminoBajadaInicial[i-1];var d=CaminoBajadaInicial[i]-a;
            float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);
            float cerca=Vector2.Distance(p,a+d*t);
            if(cerca<distancia){distancia=cerca;avance=(acumulado+d.magnitude*t)/largo;}
            acumulado+=d.magnitude;
        }
        float peso=1-Mathf.SmoothStep(0,1,(distancia-3.2f)/3.5f);
        float cotaCamino=cota+DesnivelInicio*(1-Mathf.SmoothStep(0,1,avance));
        return Mathf.Lerp(altura,cotaCamino,peso);
    }

    [MenuItem("Crater/Actualizar inicio elevado y farol")]
    public static void ActualizarInicioElevado()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Detener Play antes de actualizar el inicio.");
        var nivel = GameObject.Find("NIVEL");
        if (nivel == null || GameObject.Find("06_Capilla") == null)
            throw new System.InvalidOperationException("Abrí la escena CraterVerticalSlice antes de actualizar.");
        contenedorMallas = AssetDatabase.LoadMainAssetAtPath(RutaMallasFacetadas);
        Material M(string nombre) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + nombre + ".mat");
        var k = new Kit { tierra = M("Tierra"), piedra = M("PiedraClara"), pinturaHueso = M("PinturaHueso") };
        // El terreno existente es la referencia de material: no crea ni modifica materiales.
        k.tierra = GameObject.Find("Lomas_Valle").GetComponent<Renderer>().sharedMaterial;
        AplicarInicioElevado(k, nivel.transform);
        PrepararOclusion(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    static void AplicarInicioElevado(Kit k, Transform nivel)
    {
        var exterior = nivel.Find("06_Capilla");
        var paisaje = exterior.Find("Paisaje");
        var viejo = exterior.Find("Inicio_Elevado_Kit");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        var inicio = Grupo(exterior, "Inicio_Elevado_Kit");

        var vertices = new List<Vector3>(); var uv = new List<Vector2>();
        Vector3 P(float x, float z) => new Vector3(x, AlturaInicioElevado(x, z), z);
        for (float z = 80; z < 210; z += 1)
            for (float x = -45; x < 45; x += 1.5f)
            {
                var a = P(x, z); var b = P(x, z + 1); var c = P(x + 1.5f, z + 1); var d = P(x + 1.5f, z);
                Triangulo(vertices, uv, a, b, c, PixelDeFaceta((a+b+c)/3));
                Triangulo(vertices, uv, a, c, d, PixelDeFaceta((a+c+d)/3));
            }
        var relieve = MallaPersistenteInicio("Relieve_Inicio_Elevado", vertices, uv);
        var lomas = paisaje.Find("Lomas_Valle");
        lomas.GetComponent<MeshFilter>().sharedMesh = relieve;
        lomas.GetComponent<MeshCollider>().sharedMesh = relieve;

        // El cierre posterior anterior atravesaría la bajada nueva: pasa detrás del inicio.
        var limite = paisaje.Find("Borde_Valle_Inicio");
        if (limite != null) limite.localPosition = new Vector3(0, 25, 208);
        foreach (var t in paisaje.GetComponentsInChildren<Transform>())
        {
            if (t.name.StartsWith("Cerro_Atras_") && t.localPosition.z < 200)
                t.localPosition += Vector3.forward * 36;
            // Abrir los hombros delante del impacto para leer toda la lomada circular.
            if (!t.name.EndsWith("_Mirador") && (t.name.StartsWith("Cerro_A_") || t.name.StartsWith("Cerro_B_")))
            {
                t.localPosition+=Vector3.right*(t.name.StartsWith("Cerro_A_")?-14:14);
                t.name+="_Mirador";
            }
        }
        foreach (var t in nivel.GetComponentsInChildren<Transform>())
            if (t.name == "Roca_Cierre_Valle")
            {
                var local = exterior.InverseTransformPoint(t.position);
                if (local.z > 165 && local.z < 180)
                { local.z += 41; t.position = exterior.TransformPoint(local); }
            }

        // Una sola malla de roca compartida por todas las piezas de encuadre.
        var referencia = paisaje.GetComponentsInChildren<MeshFilter>().First(m => m.name.StartsWith("Cerro_Inicio_"));
        Mesh roca = referencia.sharedMesh;
        var bRoca = roca.bounds;
        void Roca(string nombre, float x, float z, Vector3 medida)
        {
            var t = Grupo(inicio, nombre);
            t.localScale = new Vector3(medida.x/bRoca.size.x, medida.y/bRoca.size.y, medida.z/bRoca.size.z);
            var pie = new Vector3(x, AlturaInicioElevado(x,z)-.25f, z);
            t.localPosition = pie - Vector3.Scale(new Vector3(bRoca.center.x,bRoca.min.y,bRoca.center.z),t.localScale);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = roca;
            t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = k.tierra;
            var col = t.gameObject.AddComponent<BoxCollider>(); col.center=bRoca.center; col.size=bRoca.size;
            Estatico(t.gameObject);
        }
        // Antepecho bajo: deja ver el objetivo, pero impide bajar de frente.
        for(int i=0;i<6;i++)
            Roca("Borde_Mirador_"+i,-8+i*4,179.5f,new Vector3(4.4f,.95f,2.4f));
        for(int i=1;i<CaminoBajadaInicial.Length;i++)
        {
            var a=CaminoBajadaInicial[i-1];var b=CaminoBajadaInicial[i];
            var normal=new Vector2(-(b-a).y,(b-a).x).normalized;
            int piezas=Mathf.CeilToInt(Vector2.Distance(a,b)/5);
            for(int j=0;j<piezas;j++)foreach(float lado in new[]{-1f,1f})
            {
                var p=Vector2.Lerp(a,b,(j+.5f)/piezas)+normal*lado*4.6f;
                // El frente se reserva a la vista: allí sólo está el borde bajo.
                if(p.x>-11 && p.y>174 && p.y<181)continue;
                Roca("Roca_Sendero_"+i+"_"+j+"_"+lado,p.x,p.y,new Vector3(3.2f,1.15f+(j%3)*.25f,4.2f));
            }
        }
        for(int i=0;i<5;i++) Roca("Roca_Fondo_"+i,-12+i*6,199,new Vector3(7,5,7));
        // El mirador nuevo queda más atrás: extender también los cerros de los costados.
        foreach(float lado in new[]{-1f,1f})for(int i=0;i<3;i++)
            Roca("Cierre_Lateral_Mirador_"+lado+"_"+i,lado*40,165+i*21,new Vector3(18,32,25));

        var spawn = exterior.Find("SpawnValle");
        spawn.localPosition = new Vector3(0,AlturaInicioElevado(0,InicioElevadoZ)+.05f,InicioElevadoZ);
        spawn.localRotation = Quaternion.Euler(0,180,0);
        var jugador = Object.FindFirstObjectByType<JugadorFPS>();
        jugador.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
        PrefabUtility.RecordPrefabInstancePropertyModifications(jugador.transform);
        // El cartel acompaña el comienzo de la bajada, al costado del encuadre.
        var cartel = exterior.Find("Cartel_Controles_Inicio");
        if (cartel != null)
        {
            var panel = cartel.Find("Control_Mover");
            var p = exterior.InverseTransformPoint(panel != null ? panel.position : cartel.position);
            var destino = new Vector3(-8,AlturaInicioElevado(-8,187)+1.6f,187);
            cartel.position += exterior.TransformVector(destino-p);
            if(panel!=null)
            {
                Vector3 normal=spawn.position-panel.position;normal.y=0;
                float giro=Vector3.SignedAngle(panel.forward,-normal,Vector3.up);
                foreach(Transform pieza in cartel)pieza.RotateAround(panel.position,Vector3.up,giro);
            }
        }
        ConstruirRebordeAntiguo(k,exterior,inicio);
        AplicarRevisionCaminoMurales(k,nivel,inicio);
        TrasladarFarolAlCentro(k,nivel);
        AjustarPuertaSolVisible(k,nivel);
        var linterna=Object.FindFirstObjectByType<LinternaController>();
        linterna.duracionFiltro=0;
        PrefabUtility.RecordPrefabInstancePropertyModifications(linterna);
        var prefab=PrefabUtility.LoadPrefabContents(CarpetaPrefabs+"Jugador.prefab");
        try
        {
            prefab.GetComponentInChildren<LinternaController>(true).duracionFiltro=0;
            PrefabUtility.SaveAsPrefabAsset(prefab,CarpetaPrefabs+"Jugador.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
    }

    static Mesh MallaPersistenteInicio(string nombre,List<Vector3> v,List<Vector2> uv)
    {
        AsegurarCarpeta("Assets/Models/Generado");
        string ruta="Assets/Models/Generado/"+nombre+".asset";
        var nueva=CrearMalla(nombre,v,uv);
        var existente=AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if(existente==null){AssetDatabase.CreateAsset(nueva,ruta);return nueva;}
        EditorUtility.CopySerialized(nueva,existente);Object.DestroyImmediate(nueva);EditorUtility.SetDirty(existente);return existente;
    }

    static void ConstruirRebordeAntiguo(Kit k,Transform exterior,Transform padre)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();
        const int segmentos=96, anillos=12;
        Vector3 P(int i,int j)
        {
            float a=i*Mathf.PI*2/segmentos;
            float r=Mathf.Lerp(RadioPozo-.25f,RadioPozo+9,j/(float)anillos);
            float alto=AlturaLomadaCrater(r);
            // Una boca transitable al sur enlaza con el descanso, sin un escalón de tierra.
            if(Mathf.Sin(a)>0)alto*=Mathf.SmoothStep(0,1,(Mathf.Abs(Mathf.Cos(a)*r)-1.9f)/1.2f);
            return CraterEnLaCapilla+new Vector3(Mathf.Cos(a)*r,-.095f+alto,Mathf.Sin(a)*r);
        }
        for(int i=0;i<segmentos;i++)for(int j=0;j<anillos;j++)
        {
            var a=P(i,j);var b=P(i+1,j);var c=P(i+1,j+1);var d=P(i,j+1);
            Triangulo(v,uv,a,b,c,PixelDeFaceta((a+b+c)/3));Triangulo(v,uv,a,c,d,PixelDeFaceta((a+c+d)/3));
        }
        // Faldón interior continuo: tapa las juntas de los muros rectos del pozo.
        for(int i=0;i<segmentos;i++)
        {
            var a=P(i,0);var b=P(i+1,0);var c=b-Vector3.up*.8f;var d=a-Vector3.up*.8f;
            Triangulo(v,uv,a,c,b,PixelDeFaceta(a));Triangulo(v,uv,a,d,c,PixelDeFaceta(d));
        }
        var go=new GameObject("Reborde_Crater_Antiguo");go.transform.SetParent(padre,false);
        var m=MallaPersistenteInicio("Reborde_Crater_Antiguo",v,uv);
        go.AddComponent<MeshFilter>().sharedMesh=m;go.AddComponent<MeshRenderer>().sharedMaterial=k.tierra;
        go.AddComponent<MeshCollider>().sharedMesh=m;Estatico(go);
        // La superficie central acompaña la tapa al abrir: no queda un disco flotando.
        var tapa=exterior.Find("Tapa_Pozo");
        v.Clear();uv.Clear();
        float radioTapa=RadioPozo+.6f;
        Vector3 T(int i,int j)
        {
            float a=i*Mathf.PI*2/segmentos,r=radioTapa*j/anillos;
            return new Vector3(Mathf.Cos(a)*r/tapa.localScale.x,AlturaLomadaCrater(r)/tapa.localScale.y,Mathf.Sin(a)*r/tapa.localScale.z);
        }
        for(int i=0;i<segmentos;i++)for(int j=0;j<anillos;j++)
        {
            var a=T(i,j);var b=T(i+1,j);var c=T(i+1,j+1);var d=T(i,j+1);
            if(j>0)Triangulo(v,uv,a,b,c,PixelDeFaceta(a));
            Triangulo(v,uv,a,c,d,PixelDeFaceta(d));
        }
        var cubierta=MallaPersistenteInicio("Tapa_Crater_Lomada",v,uv);
        tapa.GetComponent<MeshFilter>().sharedMesh=cubierta;
        foreach(var col in tapa.GetComponents<Collider>())Object.DestroyImmediate(col);
        tapa.gameObject.AddComponent<MeshCollider>().sharedMesh=cubierta;
    }

    static float AlturaLomadaCrater(float radio) => .8f*Mathf.Pow(Mathf.Clamp01(1-radio*radio/Mathf.Pow(RadioPozo+9,2)),2);

    static void TrasladarFarolAlCentro(Kit k,Transform nivel)
    {
        // Se retira sólo el mecanismo del Umbral; su arquitectura y murales permanecen.
        foreach(string nombre in new[]{"Compuerta_Umbral","Ancla_Umbral","Ancla_Umbral_Base","Lampara_Ancla_Umbral"})
        {
            var t=BuscarObjetoDocente(nombre);if(t!=null)Object.DestroyImmediate(t.gameObject);
        }
        var flujo=Object.FindFirstObjectByType<FlujoJuegoCrater>();Asignar(flujo,"compuertaUmbral",null);
        var progreso=Object.FindFirstObjectByType<ObeliscoDelEclipse>();
        // Las columnas y el Cruce conservan el controlador de progreso y sus referencias.
        var cuerpo=BuscarObjetoDocente("Anillo_Obelisco");
        if(cuerpo!=null)
        {
            foreach(var col in cuerpo.GetComponents<Collider>())Object.DestroyImmediate(col);
            foreach(var r in cuerpo.GetComponents<Renderer>())Object.DestroyImmediate(r);
            var mf=cuerpo.GetComponent<MeshFilter>();if(mf!=null)Object.DestroyImmediate(mf);
        }
        foreach(var cara in new[]{progreso.sol,progreso.luna})
        {
            foreach(var r in cara.renderers ?? new Renderer[0])if(r!=null)Object.DestroyImmediate(r.gameObject);
            cara.renderers=new Renderer[0];
            if(cara.luz!=null)Object.DestroyImmediate(cara.luz.gameObject);
            cara.luz=null;EditorUtility.SetDirty(cara);
        }
        if(progreso.eclipse!=null)Object.DestroyImmediate(progreso.eclipse.gameObject);
        if(progreso.eclipseRegreso!=null)Object.DestroyImmediate(progreso.eclipseRegreso.gameObject);
        if(progreso.luz!=null)Object.DestroyImmediate(progreso.luz.gameObject);
        progreso.eclipse=null;progreso.eclipseRegreso=null;progreso.luz=null;EditorUtility.SetDirty(progreso);

        Vector3 centro=EspacioCrater.Punto(CentroRotonda);
        var farol=Object.FindObjectsByType<Recogible>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(r=>r.filtro==null);
        farol.transform.SetPositionAndRotation(new Vector3(centro.x,-.35f,centro.z),Quaternion.identity);
        PrefabUtility.RecordPrefabInstancePropertyModifications(farol.transform);
        var existente=BuscarObjetoDocente("Apoyo_Farol_Central");
        if(existente==null)
        {
            var apoyo=GameObject.CreatePrimitive(PrimitiveType.Cylinder);apoyo.name="Apoyo_Farol_Central";
            apoyo.transform.SetParent(nivel,false);apoyo.transform.position=new Vector3(centro.x,-.76f,centro.z);
            apoyo.transform.localScale=new Vector3(.85f,.34f,.85f);apoyo.GetComponent<Renderer>().sharedMaterial=k.piedra;Estatico(apoyo);
        }
    }
}
