using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static partial class ConstructorCrater
{
    static bool EsMuralDeInstruccion(string nombre) => nombre.StartsWith("Control_") ||
        nombre.StartsWith("Ensenanza_") || nombre=="Mural_Ancla_Puerta" ||
        nombre=="Mural_Anclas_Del_Techo" || nombre.StartsWith("Mural_Cresta_");

    static void LuzDeTechoColumna(Kit k,TestigoDeSello testigo,Vector3 centro)
    {
        var anterior=testigo.transform.Find("Techo_Columna");
        if(anterior!=null)Object.DestroyImmediate(anterior.gameObject);
        var g=Grupo(testigo.transform,"Techo_Columna");
        var piso=testigo.renderers.First(r=>r!=null&&r.name=="Aro_Luminoso_Piso");
        var aro=new GameObject("Aro_Luminoso_Techo");aro.transform.SetParent(g,false);
        aro.transform.SetPositionAndRotation(new Vector3(centro.x,6.835f,centro.z),Quaternion.Euler(180,0,0));
        aro.AddComponent<MeshFilter>().sharedMesh=piso.GetComponent<MeshFilter>().sharedMesh;
        var renderer=aro.AddComponent<MeshRenderer>();renderer.sharedMaterial=piso.sharedMaterial;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        testigo.renderers=testigo.renderers.Where(r=>r!=null&&r.name!="Aro_Luminoso_Techo").Concat(new[]{renderer}).ToArray();
        // Carcasa opaca bajo el techo: oculta la emisión vista desde el exterior.
        Cilindro(g,"Carcasa_Aro_Techo",g.InverseTransformPoint(new Vector3(centro.x,6.90f,centro.z)),
            new Vector3(1.55f,.055f,1.55f),k.techo??k.piedra);
        var interior=(new Vector3(0,0,EspacioCrater.Punto(CentroRotonda).z)-new Vector3(centro.x,0,centro.z)).normalized;
        // Antes de ampliar se usa el centro de diseño; después, el de la escena.
        if(Mathf.Abs(centro.z-CentroRotonda.z)<.01f)interior=new Vector3(-Mathf.Sign(centro.x),0,0);
        testigo.luzSuperior=Luz(g,"Luz_Columna_Techo",new Vector3(centro.x,6.72f,centro.z)+interior*.9f,
            testigo.colorEncendido,0,8,false);
        testigo.luzSuperior.type=LightType.Spot;testigo.luzSuperior.spotAngle=64;testigo.luzSuperior.innerSpotAngle=28;
        testigo.luzSuperior.transform.LookAt(new Vector3(centro.x,3.6f,centro.z));
        testigo.intensidadLuzSuperior=45;
    }

    static void AplicarCorreccionDocente(Kit k,Transform nivel)
    {
        var viejo=nivel.Find("Correccion_Docente");if(viejo!=null)Object.DestroyImmediate(viejo.gameObject);
        var g=Grupo(nivel,"Correccion_Docente");
        Vector3 O(float x,float y,float z)=>EspacioCrater.Punto(new Vector3(x,y,z)+CorrimientoAlaOeste);
        Vector3 P(float x,float y,float z)=>EspacioCrater.Punto(new Vector3(x,y,z));

        // Atril sobre el borde seguro: lectura antes de cruzar, apoyos fuera del paso.
        var punto=O(-26.2f,2.65f,1);
        Losa(k,g,"Ensenanza_Dos_Anclas",punto,Vector3.right,3.2f,1.15f,Escalado(.64f,PanelDosAnclas));
        foreach(float z in new[]{-.2f,2.2f})
        {
            var pie=O(-26.2f,4.7f,z);
            Bloque(g,"Soporte_Ensenanza_Sol",pie,new Vector3(.12f,2.7f,.12f),k.piedra,false);
        }
        LuzDeMural(g,punto,Vector3.right,4.4f);
        foreach(string nombre in new[]{"Ancla_O_Ensenar_A","Ancla_O_Ensenar_B"})OrientarAncla(nombre,O(-24.8f,1.4f,1));

        // Combinación antes del puente: LUNA abre la reja, SOL conserva las dos anclas.
        punto=P(-2,2.75f,33.5f);
        Losa(k,g,"Ensenanza_Cruce_Combinacion",punto,Vector3.back,4.2f,1.25f,Escalado(.7f,PanelCombinacion));
        foreach(float x in new[]{-3.7f,-.3f})
            Bloque(g,"Soporte_Combinacion",P(x,4.7f,33.5f),new Vector3(.12f,2.7f,.12f),k.piedra,false);
        LuzDeMural(g,punto,Vector3.back,5.5f);
        foreach(string nombre in new[]{"Ancla_N_Vista","Ancla_N_Oculta"})OrientarAncla(nombre,P(-2,1.4f,34.5f));
        foreach(string nombre in new[]{"Ancla_N_Salida_A","Ancla_N_Salida_B"})OrientarAncla(nombre,P(0,1.4f,52));

        // La entrada anticipa el lenguaje del final; no añade una solución automática.
        foreach(string nombre in new[]{"Mural_Cresta_Encandilado","Mural_Cresta_Oscuridad"})
        {var t=BuscarObjetoDocente(nombre);if(t!=null)Object.DestroyImmediate(t.gameObject);}
        punto=P(-1.6f,1.7f,72.5f);
        Losa(k,g,"Ensenanza_Cresta_Reflejo",punto,new Vector3(1,0,-.3f).normalized,2.8f,1.8f,PanelEncandilado);
        LuzDeMural(g,punto,Vector3.right,2.8f);
        punto=P(1.6f,1.7f,74f);
        Losa(k,g,"Ensenanza_Cresta_Espera",punto,new Vector3(-1,0,-.3f).normalized,2.8f,1.8f,PanelEnLaOscuridad);
        LuzDeMural(g,punto,Vector3.left,2.8f);
        var permanentes=nivel.Find("Murales_Docentes");if(permanentes==null)permanentes=Grupo(nivel,"Murales_Docentes");
        RedibujarMuralesDocentes(k,permanentes);
        var obelisco=Object.FindFirstObjectByType<ObeliscoDelEclipse>();
        if(obelisco!=null)
            foreach(var t in obelisco.columnasSol.Concat(obelisco.columnasLuna))
            {LuzDeTechoColumna(k,t,t.renderers.First(r=>r.name=="Aro_Luminoso_Piso").bounds.center);EditorUtility.SetDirty(t);}
        GuiaExteriorDocente(k,g);
        AplicarAnclasCarreraYAros(k,g);
    }

    static Transform BuscarObjetoDocente(string nombre)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==nombre);
    static void OrientarAncla(string nombre,Vector3 observador)
    {
        var t=BuscarObjetoDocente(nombre);if(t==null)return;
        var d=observador-t.position;d.y=0;t.rotation=Quaternion.LookRotation(d);
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);
    }
    static void PanelDosAnclas(Kit k,Lienzo l)
    {
        var h=l.En(k.pinturaHueso);var s=l.En(k.pinturaSol);
        for(int i=0;i<2;i++)
        {
            float x=-1.45f+i*1.3f;
            FiguraEn(s,Figura.Sol,x-.25f,.45f,.26f);
            Ancla(h,s,x+.2f,-.5f,.65f);
            var punta=Linterna(h,new Vector2(x-.45f,.12f),-12,.18f);
            Cono(s,punta,-12,.4f,12);
        }
        Flecha(h,new Vector2(-.85f,0),new Vector2(-.6f,0),.035f);
        Flecha(h,new Vector2(.4f,0),new Vector2(.7f,0),.035f);
        Rect(s,.8f,-.4f,1.85f,-.34f);Rect(h,.75f,-.65f,.85f,-.34f);Rect(h,1.8f,-.65f,1.9f,-.34f);
        Persona(h,1.2f,-.32f,.56f,.07f,0);
        Reloj(h,new Vector2(1.8f,.48f),.18f);
    }
    static void PanelCombinacion(Kit k,Lienzo l)
    {
        var h=l.En(k.pinturaHueso);var u=l.En(k.pinturaLuna);var s=l.En(k.pinturaSol);
        FiguraEn(u,Figura.LunaCreciente,-2,.5f,.32f);
        var punta=Linterna(h,new Vector2(-2.25f,-.15f),0,.2f);Cono(u,punta,0,.6f,14);
        Marco(h,-1.5f,-.6f,-1.1f,.45f,.035f);
        for(float y=-.5f;y<.4f;y+=.2f)Rect(u,-1.35f,y,-1.3f,y+.08f);
        Flecha(h,new Vector2(-.9f,0),new Vector2(-.6f,0),.035f);
        FiguraEn(s,Figura.Sol,-.1f,.5f,.3f);
        Ancla(h,s,-.4f,-.6f,.6f);Ancla(h,s,.35f,-.6f,.6f);
        Flecha(h,new Vector2(.85f,0),new Vector2(1.15f,0),.035f);
        Rect(s,1.25f,-.4f,2.35f,-.34f);Persona(h,1.65f,-.32f,.6f,.07f,0);
        Reloj(h,new Vector2(2.35f,.5f),.18f);
    }
    static void Reloj(List<Vector3> v,Vector2 c,float radio)
    {
        Anillo(v,c,radio*.83f,radio);
        Linea(v,c,c+Vector2.up*radio*.65f,.025f);
        Linea(v,c,c+new Vector2(.5f,-.25f)*radio,.025f);
    }

    static void RedibujarMuralesDocentes(Kit k,Transform grupo)
    {
        var dibujos=new (string n,float ancho,float alto,Action<Kit,Lienzo> dibujo)[]{
            ("Control_Linterna",2.8f,1.75f,Escalado(1.15f,PanelLinterna)),
            ("Control_FiltroSol",2.2f,1.5f,PanelFiltroSol),("Control_FiltroLuna",2.2f,1.5f,PanelFiltroLuna),
            ("Mural_Ancla_Puerta",3,1.9f,PanelAnclaYPuerta),("Mural_Anclas_Del_Techo",3.6f,2.2f,PanelAnclasDelTecho),
            ("Mural_Cruce_Valle",3,1.9f,PanelValleEclipse),("Mural_Cruce_Chakana",3,1.9f,PanelChakanaGrande),
            ("Mural_Tabique_E3",3.6f,2,PanelNocheDeLuna)};
        foreach(var d in dibujos)
        {
            var viejo=BuscarObjetoDocente(d.n);if(viejo==null)continue;
            var normal=-viejo.forward;var punto=viejo.position-normal*.15f;
            Object.DestroyImmediate(viejo.gameObject);
            Losa(k,grupo,d.n,punto,normal,d.ancho,d.alto,d.dibujo);
        }
    }

    static void GuiaExteriorDocente(Kit k,Transform raiz)
    {
        var exterior=BuscarObjetoDocente("06_Capilla");var inicio=BuscarObjetoDocente("SpawnValle");
        if(exterior==null||inicio==null)return;
        var guia=Grupo(raiz,"Guia_Exterior_Eclipse");
        float Suelo(Vector3 p){var local=exterior.InverseTransformPoint(p);return exterior.TransformPoint(new Vector3(local.x,AlturaRelieve(local.x,local.z),local.z)).y;}
        var azar=new System.Random(19026);
        for(int i=0;i<7;i++)
            foreach(float lado in new[]{-1f,1f})
            {
                var p=inicio.position+inicio.forward*(12+i*11+(float)azar.NextDouble()*2)
                    +inicio.right*(lado*(3.5f+(float)azar.NextDouble()*.8f));
                p.y=Suelo(p)-.05f;
                RocaGuiaDocente(k,guia,"Piedra_Ruta",p,new Vector3(.6f,.3f,.8f)*(.8f+(float)azar.NextDouble()*.6f),azar);
            }
        // Afloramientos bajos e irregulares a los lados del recorrido, sin marco artificial.
        foreach(float lado in new[]{-1f,1f})
        {
            var p=inicio.position+inicio.forward*(lado<0?28:32)+inicio.right*(lado*5.5f);
            p.y=Suelo(p)-.08f;
            RocaGuiaDocente(k,guia,"Afloramiento_Guia",p,new Vector3(1.5f,lado<0?1.4f:2.1f,1.8f),azar);
        }
    }

    static void RocaGuiaDocente(Kit k,Transform padre,string nombre,Vector3 punto,Vector3 escala,System.Random azar,bool cerrarBase=false)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var inferior=new Vector3[6];var superior=new Vector3[6];
        for(int i=0;i<6;i++)
        {
            float angulo=i*Mathf.PI/3;float radio=.8f+(float)azar.NextDouble()*.2f;
            inferior[i]=new Vector3(Mathf.Cos(angulo)*radio,0,Mathf.Sin(angulo)*radio);
            superior[i]=new Vector3(inferior[i].x*.65f,.5f+(float)azar.NextDouble()*.25f,inferior[i].z*.65f);
        }
        var cima=new Vector3(.08f,1,-.12f);
        for(int i=0;i<6;i++)
        {
            int j=(i+1)%6;
            Triangulo(v,uv,inferior[i],superior[i],superior[j],Vector2.zero);
            Triangulo(v,uv,inferior[i],superior[j],inferior[j],Vector2.zero);
            Triangulo(v,uv,superior[i],cima,superior[j],Vector2.zero);
            if(cerrarBase)Triangulo(v,uv,Vector3.zero,inferior[i],inferior[j],Vector2.zero);
        }
        var roca=new GameObject(nombre);roca.transform.SetParent(padre,false);
        roca.transform.SetPositionAndRotation(punto,Quaternion.Euler(0,(float)azar.NextDouble()*360,0));
        roca.transform.localScale=escala;
        roca.AddComponent<MeshFilter>().sharedMesh=GuardarMalla(CrearMalla(nombre,v,uv));
        roca.AddComponent<MeshRenderer>().sharedMaterial=k.piedraCapilla??k.piedra;Estatico(roca);
    }

    [MenuItem("Crater/Actualizar corrección docente")]
    public static void ActualizarCorreccionDocente()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Detener Play antes de actualizar.");
        contenedorMallas=AssetDatabase.LoadMainAssetAtPath(RutaMallasFacetadas);
        Material M(string n)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/"+n+".mat");
        var k=new Kit{tallaEclipse=M("TalladoEclipse"),tierra=M("CapillaTierra"),techo=M("TechoProfundo"),piedraCapilla=M("CapillaPiedra"),piedra=M("PiedraClara"),pinturaHueso=M("PinturaHueso"),pinturaSol=M("PinturaSol"),pinturaLuna=M("PinturaLuna")};
        AplicarCorreccionDocente(k,GameObject.Find("NIVEL").transform);
        foreach(var l in Object.FindObjectsByType<LinternaController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {l.duracionFiltro=3;PrefabUtility.RecordPrefabInstancePropertyModifications(l);EditorUtility.SetDirty(l);}
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaPrefabs+"Jugador.prefab");
        var linterna=prefab.GetComponentInChildren<LinternaController>(true);linterna.duracionFiltro=3;EditorUtility.SetDirty(linterna);
        foreach(var pausa in Object.FindObjectsByType<PausaCrater>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {pausa.pausarAlPerderFoco=false;EditorUtility.SetDirty(pausa);PrefabUtility.RecordPrefabInstancePropertyModifications(pausa);}
        AssetDatabase.SaveAssets();PrepararOclusion(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}



