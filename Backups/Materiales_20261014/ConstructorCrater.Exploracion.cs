using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class ConstructorCrater
{
    static void ConstruirTechoCircular(Kit k, Transform padre)
    {
        var vertices = new List<Vector3>(); var uv = new List<Vector2>();
        const int lados = 64; const float radio = 6.4f;
        Vector3 P(int i, float r) { float a = i * Mathf.PI * 2 / lados; return CentroRotonda + new Vector3(Mathf.Cos(a)*r, 7, Mathf.Sin(a)*r); }
        Vector3 Exterior(int i) { float a=i*Mathf.PI*2/lados; float r=15.6f/Mathf.Max(Mathf.Abs(Mathf.Cos(a)),Mathf.Abs(Mathf.Sin(a)));return P(i,r); }
        for (int i=0;i<lados;i++)
        {
            var a=P(i,radio);var b=P(i+1,radio);var c=Exterior(i);var d=Exterior(i+1);
            Triangulo(vertices,uv,a,c,b,PixelDeFaceta(a)); Triangulo(vertices,uv,b,c,d,PixelDeFaceta(b));
        }
        var m=GuardarMalla(CrearMalla("Techo_Oculo_Circular",vertices,uv));
        var go=Tronco(padre,"Techo_Rotonda_Circular",Vector3.zero,m,k.techo);
        go.AddComponent<MeshCollider>().sharedMesh=m;
    }

    static readonly string[] IdPiezas = { "exterior_mirador", "sol_veta", "luna_reparo", "gruta_o1", "rotonda_cavidad", "cruce_antesala" };

    static Mesh FragmentoDelHalo(int indice)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();
        Vector3 P(float a,float r)=>new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);
        for(int j=0;j<8;j++)
        {
            float a=(indice*60+2+j*7)*Mathf.Deg2Rad,b=(indice*60+2+(j+1)*7)*Mathf.Deg2Rad;
            var p=P(a,.32f);var q=P(b,.32f);var r=P(a,.55f);var t=P(b,.55f);
            Triangulo(v,uv,p,q,r,PixelDeFaceta(p));Triangulo(v,uv,q,t,r,PixelDeFaceta(q));
        }
        return GuardarMalla(CrearMalla("Fragmento_Halo_"+indice,v,uv));
    }
    static PiezaTallada CrearPiezaOpcional(Kit k,Transform padre,ColeccionPiedras coleccion,int i,Vector3 punto)
    {
        var raiz=Grupo(padre,"Pieza_"+IdPiezas[i]);raiz.position=punto;
        var cuerpo=Tronco(raiz,"Piedra_Tallada",punto,MallaTronco("Piedra_Opcional",6,.21f,.17f,.06f),k.piedra);
        var motivo=Tronco(raiz,"Fragmento_Grabado",punto+Vector3.up*.075f,FragmentoDelHalo(i),k.tallaEclipse);
        motivo.transform.rotation=Quaternion.Euler(90,0,0);motivo.transform.localScale=Vector3.one*.3f;
        raiz.gameObject.layer=2;
        var detector=raiz.gameObject.AddComponent<SphereCollider>();detector.isTrigger=true;detector.radius=.3f;
        var p=raiz.gameObject.AddComponent<PiezaTallada>();p.identificador=IdPiezas[i];p.indice=i;p.coleccion=coleccion;
        return p;
    }
    static void ConstruirExploracion(Kit k,Transform nivel,Referencias refs)
    {
        var g=Grupo(nivel,"EXPLORACION_Opcional");
        var c=g.gameObject.AddComponent<ColeccionPiedras>();
        c.jugador=refs.jugador.GetComponent<JugadorFPS>();c.camara=refs.jugador.GetComponentInChildren<Camera>();
        c.interfaz=Object.FindFirstObjectByType<InterfazCrater>();c.flujo=Object.FindFirstObjectByType<FlujoJuegoCrater>();c.prologo=Object.FindFirstObjectByType<PrologoCapilla>();
        c.recogida=CrearFuenteHija(g,"Nota_Pieza",k.audio.resonanciaSuave,.12f);c.recogida.maxDistance=6;
        c.luzRecogida=Luz(g,"Respuesta_Pieza",Vector3.zero,LuzFria,0,2,false);
        var conjunto=Grupo(g,"Conjunto_Piedras_Talladas");conjunto.position=EspacioCrater.Punto(new Vector3(-11f,1.2f,18));
        Bloque(g,"Apoyo_Conjunto",conjunto.position+Vector3.down*.925f,new Vector3(1.7f,.55f,.6f),k.piedra);
        Bloque(g,"Fondo_Conjunto",conjunto.position+Vector3.forward*.12f,new Vector3(1.6f,1.4f,.25f),k.basaltoMedio);
        var ct=conjunto.gameObject.AddComponent<ConjuntoTallado>();c.conjunto=ct;ct.coleccion=c;
        ct.fragmentos=new Renderer[6];
        for(int i=0;i<6;i++){ct.fragmentos[i]=Tronco(conjunto,"Representacion_"+IdPiezas[i],conjunto.position+Vector3.back*.16f,FragmentoDelHalo(i),k.tallaEclipse).GetComponent<Renderer>();ct.fragmentos[i].enabled=false;}
        var combinadas=new CombineInstance[6];for(int i=0;i<6;i++)combinadas[i]=new CombineInstance{mesh=FragmentoDelHalo(i),transform=Matrix4x4.identity};
        var halo=new Mesh{name="Halo_Conjunto_Completo"};halo.CombineMeshes(combinadas,true,true);
        ct.proyeccion=Tronco(g,"Motivo_Proyectado",conjunto.position+new Vector3(1.65f,.2f,.08f),GuardarMalla(halo),k.tallaEclipse).GetComponent<Renderer>();
        ct.proyeccion.transform.localScale=Vector3.one*1.15f;ct.proyeccion.enabled=false;
        Bloque(g,"Piedra_Proyeccion",ct.proyeccion.transform.position+Vector3.forward*.22f,new Vector3(1.5f,1.5f,.2f),k.piedra);
        Bloque(g,"Apoyo_Proyeccion",ct.proyeccion.transform.position+new Vector3(0,-1.075f,.22f),new Vector3(1.2f,.65f,.45f),k.piedra);
        ct.luz=Luz(g,"Luz_Conjunto",conjunto.position+new Vector3(0,.4f,-.6f),LuzFria,0,4,false);
        ct.voces=new AudioSource[6];float[] tonos={1,1.25f,1.5f,1.125f,1.33f,2};
        for(int i=0;i<6;i++){ct.voces[i]=CrearFuenteHija(conjunto,"Resonancia_Conjunto_"+i,k.audio.resonanciaSuave,.45f);ct.voces[i].pitch=tonos[i];ct.voces[i].maxDistance=10;}
        ct.resonancias=ct.voces[0];ct.nota=k.audio.resonanciaSuave;
        ct.llamada=CrearFuenteHija(conjunto,"Invitacion_Conjunto",k.audio.resonanciaSuave,.035f);ct.llamada.maxDistance=24;
        Luz(g,"Luz_Cuidado_Conjunto",conjunto.position+new Vector3(0,.7f,-1),LuzCalida,4,4,false);
        // La cavidad conserva el prototipo ya probado y ocupa el lateral seguro.
        Vector3 centro=EspacioCrater.Punto(new Vector3(-11.5f,0,6));
        Bloque(g,"Cavidad_Fondo",centro+new Vector3(-1.25f,1,0),new Vector3(.3f,2.4f,2.6f),k.piedra);
        foreach(float z in new[]{-1.25f,1.25f})Bloque(g,"Cavidad_Lateral",centro+new Vector3(0,1,z),new Vector3(2.5f,2.4f,.25f),k.piedra);
        Bloque(g,"Cavidad_Techo",centro+new Vector3(0,2.3f,0),new Vector3(2.7f,.3f,2.7f),k.piedra);
        Luz(g,"Luz_Cavidad",centro+new Vector3(-.7f,.6f,0),LuzCalida,4,3,false);
        var cavidad=Grupo(g,"Cavidad_Para_Escuchar");cavidad.position=centro+Vector3.up;
        var cr=cavidad.gameObject.AddComponent<CavidadResonante>();cr.oyente=c.camara.transform;
        cr.viento=CrearFuenteHija(cavidad,"Viento_Resonante",k.audio.exteriorCapilla,0);cr.viento.loop=true;cr.viento.maxDistance=9;
        cr.tono=CrearFuenteHija(cavidad,"Cuerpo_Resonante",k.audio.resonanciaSuave,0);cr.tono.loop=true;cr.tono.pitch=.5f;cr.tono.maxDistance=6;
        c.piezas=new PiezaTallada[6];
        c.piezas[4]=CrearPiezaOpcional(k,g,c,4,centro+new Vector3(-.65f,.18f,0));
        Bloque(g,"Apoyo_Pieza_Cavidad",centro+new Vector3(-.65f,.09f,0),new Vector3(.55f,.18f,.55f),k.piedra);
        var valle=nivel.Find("06_Capilla");
        Vector3 exterior=valle.TransformPoint(new Vector3(-7.5f,.25f,78));
        c.piezas[0]=CrearPiezaOpcional(k,g,c,0,exterior+Vector3.up*.32f);
        // Un desvío lateral plano, con pircas bajas y vuelta por la misma entrada.
        Bloque(g,"Mirador_Lateral_Piso",exterior+Vector3.down*.16f,new Vector3(3,.18f,3),k.piedra);
        foreach(float z in new[]{-1.45f,1.45f})Bloque(g,"Mirador_Lateral_Pirca",exterior+new Vector3(0,.35f,z),new Vector3(3,1,.25f),k.piedra);
        Bloque(g,"Mirador_Lateral_Fondo",exterior+new Vector3(1.45f,.35f,0),new Vector3(.25f,1,3),k.piedra);
        Luz(g,"Luz_Apoyo_Mirador",exterior+new Vector3(0,.7f,0),LuzCalida,5,3,false);
        Bloque(g,"Apoyo_Pieza_Exterior",exterior+Vector3.up*.09f,new Vector3(.6f,.4f,.6f),k.piedra);
        Vector3 sol=EspacioCrater.Punto(new Vector3(-34.7f,.58f,13.8f)+CorrimientoAlaOeste);
        c.piezas[1]=CrearPiezaOpcional(k,g,c,1,sol);
        Bloque(g,"Apoyo_Pieza_SOL",sol+Vector3.down*.28f,new Vector3(.55f,.55f,.55f),k.piedra);
        Vector3 luna=EspacioCrater.Punto(new Vector3(34,.55f,-3.8f)+CorrimientoAlaEste);
        c.piezas[2]=CrearPiezaOpcional(k,g,c,2,luna);
        Bloque(g,"Apoyo_Pieza_LUNA",luna+Vector3.down*.27f,new Vector3(.55f,.55f,.55f),k.piedra);
        var red=GameObject.Find("Red_O1").transform;
        c.piezas[3]=CrearPiezaOpcional(k,g,c,3,red.TransformPoint(new Vector3(-4.35f,-2.58f,-4.4f)));
        Vector3 cruce=EspacioCrater.Punto(new Vector3(-4.9f,.5f,29.8f));
        c.piezas[5]=CrearPiezaOpcional(k,g,c,5,cruce);
        Bloque(g,"Apoyo_Pieza_Cruce",cruce+Vector3.down*.25f,new Vector3(.65f,.5f,.65f),k.piedra);
        ConstruirAguaYUso(k,g,refs,red);
        ConstruirVetaYEncuadre(k,g,refs);
        var regreso=Grupo(g,"Detalle_Al_Regresar");regreso.position=conjunto.position+new Vector3(-1.5f,0,.02f);
        Bloque(g,"Losa_Trabajo_Incompleto",regreso.position+Vector3.forward*.25f,new Vector3(1.2f,1.2f,.25f),k.piedra);
        Bloque(g,"Apoyo_Trabajo",regreso.position+new Vector3(0,-.82f,.25f),new Vector3(.65f,.76f,.5f),k.piedra);
        var vuelta=regreso.gameObject.AddComponent<RevelacionAlRegresar>();
        vuelta.tallado=Glifo(regreso,"Halo_Incompleto",Figura.Halo,regreso.position,Vector3.back,.85f,k.tallaEclipse,false);
        vuelta.luz=Luz(regreso,"Rebote_Al_Regresar",regreso.position+new Vector3(0,.4f,-.7f),LuzCalida,0,4,false);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(refs.selloOeste.alActivarse,vuelta.Revelar);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(refs.selloEste.alActivarse,vuelta.Revelar);
        var abertura=GameObject.Find("Luz_Abertura_Rotonda").GetComponent<Light>();
        CrearPolvo(k,g,"Polvo_Abertura",abertura.transform.position+Vector3.down*4,abertura);
        ConstruirCierreSinRastro(k,g,nivel,c.prologo);
        Facetar(k,g.gameObject);
    }

    static Mesh MallaTerrenoSinRastro()
    {
        const int nx=24,nz=28;var p=new Vector3[nx+1,nz+1];var azar=new System.Random(417);
        for(int x=0;x<=nx;x++)for(int z=0;z<=nz;z++)
        {
            float px=Mathf.Lerp(-45,45,x/(float)nx),pz=Mathf.Lerp(-24,80,z/(float)nz);
            if(x>0&&x<nx&&z>0&&z<nz){px+=((float)azar.NextDouble()-.5f)*1.4f;pz+=((float)azar.NextDouble()-.5f)*1.4f;}
            p[x,z]=new Vector3(px,-.1f,pz);
        }
        var v=new List<Vector3>();var uv=new List<Vector2>();
        for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
        {
            var a=p[x,z];var b=p[x,z+1];var c=p[x+1,z+1];var d=p[x+1,z];
            Triangulo(v,uv,a,b,c,PixelDeFaceta((a+b+c)/3));Triangulo(v,uv,a,c,d,PixelDeFaceta((a+c+d)/3));
        }
        return GuardarMalla(CrearMalla("Terreno_Epilogo_Completo",v,uv));
    }

    static void ConstruirCierreSinRastro(Kit k, Transform g, Transform nivel, PrologoCapilla prologo)
    {
        var valle=nivel.Find("06_Capilla");
        var terreno=valle.Find("Paisaje/Terreno");
        var cerrado=Grupo(valle,"Terreno_Epilogo_Sin_Rastro");
        var malla=MallaTerrenoSinRastro();
        cerrado.gameObject.AddComponent<MeshFilter>().sharedMesh=malla;
        cerrado.gameObject.AddComponent<MeshRenderer>().sharedMaterial=k.tierra;
        cerrado.gameObject.AddComponent<MeshCollider>().sharedMesh=malla;
        var llano=Bloque(valle,"Cierre_Ranura_Exterior",valle.TransformPoint(new Vector3(0,-.4f,-32)),new Vector3(18,.4f,16),k.tierra,false);
        llano.transform.rotation=valle.rotation;
        var oculo=Tronco(g,"Cierre_Oculo_Epilogo",EspacioCrater.Punto(CentroRotonda+Vector3.up*7),MallaTronco("Cierre_Circular_Oculo",64,8,8,.12f),k.techo);
        oculo.AddComponent<MeshCollider>().sharedMesh=oculo.GetComponent<MeshFilter>().sharedMesh;
        var cierres=new[]{cerrado.gameObject,llano,oculo};foreach(var cierre in cierres)cierre.SetActive(false);
        AsignarLista(prologo,"cierresEpilogo",cierres);
        AsignarLista(prologo,"ocultarEpilogo",new[]{terreno.gameObject,GameObject.Find("Luz_Abertura_Rotonda")});
    }

    static void ConstruirAguaYUso(Kit k,Transform g,Referencias refs,Transform red)
    {
        var cuenco=GameObject.Find("Cuenco_Rotonda").transform;cuenco.localScale=Vector3.one*2.2f;
        Vector3 agua=cuenco.position+Vector3.up*.29f;
        var material=Opaco("Agua_Cuenco",new Color(.18f,.23f,.28f),.98f);material.SetFloat("_Metallic",.85f);
        var superficie=Tronco(g,"Agua_En_Cuenco",agua,MallaTronco("Disco_Agua",24,.36f,.36f,.006f),material);
        superficie.layer=LayerMask.NameToLayer("Water");
        // El cuenco conserva el agua y refleja el entorno existente, sin columna ni tallado añadido.
        var a=superficie.AddComponent<AguaEnCuenco>();a.oyente=refs.jugador.transform;
        var sondaGO=Grupo(g,"Sonda_Cuenco");sondaGO.position=agua+Vector3.up*.05f;
        a.sonda=sondaGO.gameObject.AddComponent<ReflectionProbe>();a.sonda.mode=UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        a.sonda.refreshMode=UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;a.sonda.resolution=64;
        a.sonda.size=new Vector3(4,4,4);a.sonda.boxProjection=false;a.sonda.nearClipPlane=.03f;a.sonda.farClipPlane=10;
        a.sonda.cullingMask=~((1<<LayerMask.NameToLayer(CapaJugador))|(1<<LayerMask.NameToLayer("Water")));
        a.sonda.renderDynamicObjects=true;a.sonda.timeSlicingMode=UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(refs.selloOeste.alActivarse,a.MarcarCambio);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(refs.selloEste.alActivarse,a.MarcarCambio);
        var original=GameObject.Find("Cuenco_Gruta_O1").transform;
        original.localScale=Vector3.one*1.5f;
        var union=Adorno(g,"Union_Cuenco_Reparado",original.position+new Vector3(-.12f,.17f,.24f),new Vector3(.022f,.21f,.03f),k.piedra);union.transform.rotation=Quaternion.Euler(0,0,-20);
        var asiento=GameObject.Find("Asiento_Gruta_O1").transform;
        for(int i=0;i<3;i++)Tronco(g,"Piedra_Ordenada_O1_"+i,asiento.position+asiento.right*(.05f+i*.28f)+Vector3.up*.21f+asiento.forward*.1f,MallaTronco("Piedra_Ordenada",6,.13f,.1f,.14f),k.cal);
    }
    static void ConstruirVetaYEncuadre(Kit k,Transform g,Referencias refs)
    {
        Vector3 pos=EspacioCrater.Punto(new Vector3(-36f,1.8f,15.5f)+CorrimientoAlaOeste);
        var veta=Grupo(g,"Veta_Para_El_Haz");veta.position=pos;
        var v=veta.gameObject.AddComponent<VetaAngular>();v.linterna=refs.jugador.GetComponentInChildren<LinternaController>();v.normal=Vector3.right;v.vetas=new Renderer[4];
        var mat=Opaco("Veta_Plateada",new Color(.1f,.12f,.15f),.7f);mat.shader=Shader.Find("Universal Render Pipeline/Unlit");
        for(int i=0;i<4;i++)
        {
            v.vetas[i]=Adorno(veta,"Veta_"+i,pos+new Vector3(.04f,i*.13f-.2f,i*.13f-.2f),new Vector3(.035f,.08f,1.2f),mat);
            v.vetas[i].transform.rotation=Quaternion.Euler(28+i*4,0,0);
        }
        Vector3 ojo=EspacioCrater.Punto(new Vector3(1,1.62f,-3));
        Vector3 centro=EspacioCrater.Punto(new Vector3(3,1.95f,2.5f));
        var motivo=Grupo(g,"Motivo_Por_Perspectiva");motivo.position=centro;
        Quaternion giro=Quaternion.LookRotation(centro-ojo);float distancia=Vector3.Distance(centro,ojo);
        var pinturaEncuadre=Emisivo(Opaco("Pintura_Encuadre",new Color(.58f,.55f,.48f),.08f),Color.white*.01f);Validar(pinturaEncuadre);
        var fragmentos=new Renderer[6];
        for(int i=0;i<6;i++)
        {
            float fondo=(i%3)*.45f,escala=(distancia+fondo)/distancia;
            Vector3 p=ojo+(centro-ojo).normalized*(distancia+fondo);
            float a=(i*60+30)*Mathf.Deg2Rad;Vector3 apoyo=p+giro*new Vector3(Mathf.Cos(a)*.3f*escala,Mathf.Sin(a)*.3f*escala,.07f);
            // Pequeñas superficies sostenidas por piedra: ningún fragmento flota en el aire.
            Bloque(motivo,"Soporte_Fragmento_"+i,new Vector3(apoyo.x,apoyo.y*.5f,apoyo.z),new Vector3(.13f,apoyo.y,.13f),k.piedra);
            var baseFigura=Tronco(motivo,"Superficie_Separada_"+i,p+giro*Vector3.forward*.025f,FragmentoDelHalo(i),k.piedra);
            baseFigura.transform.rotation=giro;baseFigura.transform.localScale=Vector3.one*escala*.91f;
            var tallado=Tronco(motivo,"Fragmento_En_Perspectiva_"+i,p,FragmentoDelHalo(i),pinturaEncuadre);
            fragmentos[i]=tallado.GetComponent<Renderer>();
            tallado.transform.rotation=giro;tallado.transform.localScale=Vector3.one*escala*.7f;
        }
        var m=motivo.gameObject.AddComponent<MotivoPorPerspectiva>();m.mirada=refs.jugador.GetComponentInChildren<Camera>().transform;m.puntoDeVista=ojo;m.fragmentos=fragmentos;
        m.nota=CrearFuenteHija(motivo,"Respuesta_Encuadre",k.audio.resonanciaSuave,.04f);m.nota.maxDistance=6;
        Luz(motivo,"Luz_Relieve_Perspectiva",centro+Vector3.up*.8f-giro*Vector3.forward,LuzCalida,5,4,false);
    }
    static void CrearPolvo(Kit k,Transform padre,string nombre,Vector3 posicion,Light luz)
    {
        var go=Grupo(padre,nombre);go.position=posicion;
        var ps=go.gameObject.AddComponent<ParticleSystem>();
        var main=ps.main;main.maxParticles=20;main.startLifetime=18;main.startSpeed=.015f;main.startSize=new ParticleSystem.MinMaxCurve(.045f,.08f);main.startColor=new Color(.85f,.9f,1,.45f);
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.useUnscaledTime=false;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(2,2.5f,2);
        var em=ps.emission;em.rateOverTime=.8f;
        var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.name="Polvo_Suave";
        material.SetTexture("_BaseMap",Textura("Polvo_Radial",32,(u,v)=>new Color(1,1,1,Mathf.SmoothStep(1,0,Vector2.Distance(new Vector2(u,v),Vector2.one*.5f)*2))));
        material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_ZWrite",0);
        material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;material.SetColor("_BaseColor",new Color(1,1,1,.65f));
        var r=ps.GetComponent<ParticleSystemRenderer>();var ruta=CarpetaMateriales+"Polvo_Suave.mat";
        var guardado=AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if(guardado==null){AssetDatabase.CreateAsset(material,ruta);guardado=material;}
        else{EditorUtility.CopySerialized(material,guardado);Object.DestroyImmediate(material);EditorUtility.SetDirty(guardado);}
        r.sharedMaterial=guardado;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var control=go.gameObject.AddComponent<PolvoEnLuz>();control.fuente=luz;control.polvo=ps;
    }
}
