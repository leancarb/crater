using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static partial class ConstructorCrater
{
    static void CrearCrestaCerrada(Kit k,Transform padre,string nombre,List<Vector3[]> cortes)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();
        void Cara(Vector3 a,Vector3 b,Vector3 c,Vector3 interior)
        {
            Triangulo(v,uv,a,b,c,PixelDeFaceta(a));
        }
        for(int i=1;i<cortes.Count;i++)
        {
            var a=cortes[i-1];var b=cortes[i];var interior=(a[0]+a[1]+a[2]+b[0]+b[1]+b[2])/6;
            for(int j=0;j<3;j++){int h=(j+1)%3;Cara(a[j],b[j],b[h],interior);Cara(a[j],b[h],a[h],interior);}
        }
        var primero=cortes[0];var segundo=cortes[1];var ultimo=cortes[cortes.Count-1];var previo=cortes[cortes.Count-2];
        Cara(primero[0],primero[1],primero[2],(primero[0]+primero[1]+primero[2]+segundo[0]+segundo[1]+segundo[2])/6);
        Cara(ultimo[0],ultimo[2],ultimo[1],(ultimo[0]+ultimo[1]+ultimo[2]+previo[0]+previo[1]+previo[2])/6);
        // Mantener orientación compartida en las juntas; invertir el sólido completo si hace falta.
        var origen=v[0];double volumen=0;
        for(int i=0;i<v.Count;i+=3)volumen+=Vector3.Dot(v[i]-origen,Vector3.Cross(v[i+1]-origen,v[i+2]-origen))/6d;
        if(volumen<0)for(int i=0;i<v.Count;i+=3){var t=v[i+1];v[i+1]=v[i+2];v[i+2]=t;}
        var go=new GameObject(nombre);go.transform.SetParent(padre,false);var m=MallaPersistenteInicio(nombre,v,uv);
        go.AddComponent<MeshFilter>().sharedMesh=m;go.AddComponent<MeshRenderer>().sharedMaterial=k.tierra;go.AddComponent<MeshCollider>().sharedMesh=m;Estatico(go);
    }
    static void CrearBordesNaturales(Kit k,Transform padre,Transform exterior)
    {
        foreach(float lado in new[]{-1f,1f})
        {
            var cortes=new List<Vector3[]>();
            for(int tramo=1;tramo<CaminoBajadaInicial.Length;tramo++)
            {
                var a=CaminoBajadaInicial[tramo-1];var b=CaminoBajadaInicial[tramo];var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*lado;int pasos=Mathf.CeilToInt(Vector2.Distance(a,b));
                for(int i=tramo==1?0:1;i<=pasos;i++)
                {
                    var baseP=Vector2.Lerp(a,b,i/(float)pasos);var normal=n;float miter=1;
                    if(i==0&&tramo>1){var previo=(a-CaminoBajadaInicial[tramo-2]).normalized;normal=(n+new Vector2(-previo.y,previo.x)*lado).normalized;miter=1/Mathf.Max(.5f,Vector2.Dot(normal,n));}
                    if(i==pasos&&tramo<CaminoBajadaInicial.Length-1){var sig=(CaminoBajadaInicial[tramo+1]-b).normalized;normal=(n+new Vector2(-sig.y,sig.x)*lado).normalized;miter=1/Mathf.Max(.5f,Vector2.Dot(normal,n));}
                    var centro=baseP+normal*miter*(3.5f+.12f*Mathf.PerlinNoise(baseP.x*.4f,baseP.y*.4f));var corte=new Vector3[3];
                    for(int j=0;j<3;j++){var q=centro+normal*miter*(j==0?-.65f:j==2?.85f:.05f);corte[j]=new Vector3(q.x,AlturaInicioElevado(q.x,q.y)+(j==1?.78f+.16f*Mathf.PerlinNoise(q.x*.2f+31,q.y*.2f):-.3f),q.y);}
                    cortes.Add(corte);
                }
            }
            CrearCrestaCerrada(k,padre,"Pirca_Natural_"+lado,cortes);
        }
        void Cierre(string nombre,Vector2 inicio,Vector2 fin,float altura)
        {
            var cortes=new List<Vector3[]>();var d=(fin-inicio).normalized;var n=new Vector2(-d.y,d.x);int pasos=Mathf.CeilToInt(Vector2.Distance(inicio,fin));
            for(int i=0;i<=pasos;i++){var p=Vector2.Lerp(inicio,fin,i/(float)pasos);var corte=new Vector3[3];for(int j=0;j<3;j++){var q=p+n*(j-1)*.8f;corte[j]=new Vector3(q.x,AlturaInicioElevado(q.x,q.y)+(j==1?altura+.18f*Mathf.PerlinNoise(q.x*.3f+19,q.y*.3f):-.3f),q.y);}cortes.Add(corte);}
            CrearCrestaCerrada(k,padre,nombre,cortes);
        }
        Cierre("Cierre_Posterior_Mirador",new Vector2(-14,201),new Vector2(28,201),1);
        Cierre("Cresta_Natural_Mirador",new Vector2(-10,179.5f),new Vector2(28,179.5f),.7f);
        Cierre("Cierre_Lateral_Izquierdo_Mirador",new Vector2(25.5f,179),new Vector2(25.5f,202),1.6f);
        // Colisión vertical dentro de la roca: no permite trepar la cresta y caer del mirador.
        var limite=Grupo(padre,"Colision_Lateral_Mirador");
        limite.localPosition=new Vector3(25.5f,AlturaInicioElevado(25,193)+.8f,190.5f);
        var col=limite.gameObject.AddComponent<BoxCollider>();col.size=new Vector3(.45f,3,24);
    }

    static void CrearSenderoDesgastado(Kit k,Transform padre)
    {
        var puntos=new List<Vector2>(CaminoBajadaInicial);
        for(float z=145;z>=80;z-=4)puntos.Add(new Vector2(3*Mathf.Sin((z-80)*Mathf.PI/70),z));
        var v=new List<Vector3>();var uv=new List<Vector2>();float recorrido=0;
        for(int i=1;i<puntos.Count;i++)
        {
            var a=puntos[i-1];var b=puntos[i];var n=new Vector2(-(b-a).y,(b-a).x).normalized;
            int pasos=Mathf.CeilToInt(Vector2.Distance(a,b)/.7f);
            for(int j=0;j<pasos;j++)
            {
                float t=j/(float)pasos,t2=(j+1)/(float)pasos;var p=Vector2.Lerp(a,b,t);var q=Vector2.Lerp(a,b,t2);
                float w=1.6f+.25f*Mathf.PerlinNoise(p.x*.3f,p.y*.3f),w2=1.6f+.25f*Mathf.PerlinNoise(q.x*.3f,q.y*.3f);
                Vector3 P(Vector2 x)=>new Vector3(x.x,AlturaInicioElevado(x.x,x.y)+.055f,x.y);
                var aa=P(p-n*w);var bb=P(q-n*w2);var cc=P(q+n*w2);var dd=P(p+n*w);
                float y=(recorrido+Vector2.Distance(a,b)*t)/7f,y2=(recorrido+Vector2.Distance(a,b)*t2)/7f;
                v.AddRange(new[]{aa,cc,bb,aa,dd,cc});uv.AddRange(new[]{new Vector2(0,y),new Vector2(1,y2),new Vector2(0,y2),new Vector2(0,y),new Vector2(1,y),new Vector2(1,y2)});
            }
            recorrido+=Vector2.Distance(a,b);
        }
        var tex=Textura("Tierra_Desgaste_Suave",128,(u,w)=>
        {float borde=Mathf.SmoothStep(0,1,Mathf.Clamp01((.5f-Mathf.Abs(u-.5f))/.34f));float ruido=Mathf.PerlinNoise(u*13,w*17);return new Color(.42f+.08f*ruido,.29f+.06f*ruido,.18f+.04f*ruido,borde*(.08f+.15f*ruido));});
        tex.wrapMode=TextureWrapMode.Repeat;EditorUtility.SetDirty(tex);
        var mat=Opaco("Tierra_Sendero_Desgastada",Color.white,.02f);mat.SetTexture("_BaseMap",tex);mat.SetFloat("_Surface",1);mat.SetFloat("_ZWrite",0);
        mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;EditorUtility.SetDirty(mat);
        var go=new GameObject("Sendero_Tierra_Desgastada");go.transform.SetParent(padre,false);go.AddComponent<MeshFilter>().sharedMesh=MallaPersistenteInicio("Sendero_Tierra_Desgastada",v,uv);go.AddComponent<MeshRenderer>().sharedMaterial=mat;Estatico(go);
    }

    static void AplicarRevisionNoviembre(Kit k,Transform nivel)
    {
        var todos=nivel.GetComponentsInChildren<Transform>(true);
        foreach(var t in todos.Where(t=>t.name.StartsWith("Mirador_Lateral_")||t.name=="Apoyo_Pieza_Exterior"||t.name=="Luz_Apoyo_Mirador"||t.name=="Cartel_03_RecogerFarol"||t.name=="Cartel_04_Farol").ToArray())if(t!=null)Object.DestroyImmediate(t.gameObject);
        var prologo=Object.FindFirstObjectByType<PrologoCapilla>();var exterior=nivel.Find("06_Capilla");
        AsignarLista(prologo,"soloEnEpilogo",new[]{exterior.Find("Estacion_Observacion").gameObject,exterior.Find("Patio_Estacion").gameObject});
        var cielo=new SerializedObject(Object.FindFirstObjectByType<CieloEclipse>());cielo.FindProperty("direccionAtardecer").vector2Value=new Vector2(18,14);cielo.ApplyModifiedPropertiesWithoutUndo();
        var progreso=Object.FindFirstObjectByType<ObeliscoDelEclipse>();progreso.radio=20;
        ReubicarPiedraYCartel(k,nivel);
        AdelantarCartelesInicio(nivel);
        AjustarEncuadreFarol(k,nivel);
        RevisarIluminacion(k,nivel);
        RepetirFrisosLobby(k,nivel);
        EnmarcarHistoria(k,nivel);
        RedondearCresta(k,nivel);
        var centroTapa=EspacioCrater.Punto(CentroRotonda);centroTapa.y=8.85f;
        var tapa=Tronco(nivel,"Cubierta_Exterior_Rotonda",centroTapa,MallaTronco("Cubierta_Exterior_Rotonda",64,RadioRotonda*EspacioCrater.Escala+1,RadioRotonda*EspacioCrater.Escala+1,.2f),k.tierra);
        tapa.AddComponent<MeshCollider>().sharedMesh=tapa.GetComponent<MeshFilter>().sharedMesh;
        tapa.AddComponent<CubiertaExteriorRotonda>();
        GameObjectUtility.SetStaticEditorFlags(tapa,0); // Se retira en juego: no hornear como oclusor permanente.
        var e=Object.FindFirstObjectByType<EclipseFinalController>();var so=new SerializedObject(e);
        var ambiente=nivel.gameObject.AddComponent<AmbienteCraterSuave>();
        ambiente.exterior=so.FindProperty("ambienteExterior").objectReferenceValue as AudioSource;
        ambiente.interior=so.FindProperty("ambienteCrater").objectReferenceValue as AudioSource;
        so.FindProperty("volumenCrater").floatValue=.055f;so.ApplyModifiedPropertiesWithoutUndo();
        ambiente.exterior.volume=.20f;
        var cavidad=Object.FindFirstObjectByType<CavidadResonante>();ambiente.cavidad=cavidad!=null?cavidad.viento:null;
        var pp=new SerializedObject(prologo);var pajaros=pp.FindProperty("pajaros").objectReferenceValue as AudioSource;if(pajaros!=null)pajaros.volume=0;
        // Quitar referencias a elementos retirados antes de serializar el nivel.
        var vis=Object.FindFirstObjectByType<VisibilidadExterior>();vis.paisaje=vis.paisaje.Where(r=>r!=null).ToArray();
        var llegada=Object.FindFirstObjectByType<LlegadaRotonda>();llegada.luces=llegada.luces.Where(l=>l!=null).ToArray();
    }

    static void ReubicarPiedraYCartel(Kit k,Transform nivel)
    {
        var banco=BuscarObjetoDocente("Banco_Rotonda").GetComponent<Renderer>().bounds;
        var piedra=BuscarObjetoDocente("Pieza_exterior_mirador");piedra.position=new Vector3(banco.min.x+.5f,banco.max.y+.12f,banco.center.z);
        var cartel=BuscarObjetoDocente("Cartel_03_RecogerPiedra");
        cartel.position=new Vector3(banco.min.x-1.2f,1.5f,banco.center.z+.6f);var normal=new Vector3(0,1.5f,3.5f)-cartel.position;normal.y=0;cartel.rotation=Quaternion.LookRotation(-normal);
        var coleccion=Object.FindFirstObjectByType<ColeccionPiedras>();coleccion.luzRecogida.intensity=0;
    }

    static void AjustarEncuadreFarol(Kit k,Transform nivel)
    {
        var motivo=Object.FindFirstObjectByType<MotivoPorPerspectiva>();var farol=Object.FindObjectsByType<Recogible>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(r=>r.name=="Recogible_Linterna");
        // El origen del modelo está bajo la base: apuntar al difusor visible, no a ese pivote.
        var centroOptico=Grupo(farol.PuntoInteraccion,"Centro_Optico_Farol");
        centroOptico.position=farol.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Lens_Difusor").bounds.center;
        Vector3 ojo=new Vector3(5,1.62f,8),destino=centroOptico.position;
        Vector3 centro=Vector3.Lerp(ojo,destino,.48f);var delta=centro-motivo.transform.position;
        motivo.transform.position+=delta;motivo.puntoDeVista=ojo;motivo.destino=centroOptico;
        // El encuadre está a 6,6 m del ojo: el alcance anterior de 6 m dejaba muda la confirmación.
        if(motivo.nota!=null){motivo.nota.minDistance=3;motivo.nota.maxDistance=14;motivo.nota.volume=.10f;}
        // El centro de cada fragmento se recompone sobre la misma línea de vista al farol.
        var giro=Quaternion.LookRotation(centro-ojo);float dist=Vector3.Distance(centro,ojo);
        for(int i=0;i<6;i++)
        {
            float fondo=i*.65f,escala=(dist+fondo)/dist;
            Vector3 p=ojo+(centro-ojo).normalized*(dist+fondo);float a=(i*60+30)*Mathf.Deg2Rad;
            var soporte=motivo.transform.Find("Soporte_Fragmento_"+i);var baseFigura=motivo.transform.Find("Superficie_Separada_"+i);var tallado=motivo.transform.Find("Fragmento_En_Perspectiva_"+i);
            var radial=giro*new Vector3(Mathf.Cos(a)*.3f*escala,Mathf.Sin(a)*.3f*escala,0);
            var apoyo=p+giro*new Vector3(Mathf.Cos(a)>=0?.48f:-.48f,Mathf.Sin(a)*.3f*escala,.08f);
            var suelo=Physics.RaycastAll(apoyo+Vector3.up*2,Vector3.down,8,1,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.transform.IsChildOf(motivo.transform)).OrderBy(h=>h.distance).FirstOrDefault();
            float piso=suelo.collider!=null?suelo.point.y:-1.2f,alto=Mathf.Max(.2f,apoyo.y-piso);
            soporte.position=new Vector3(apoyo.x,piso+alto*.5f,apoyo.z);soporte.localScale=new Vector3(.065f,alto,.065f);
            var brazo=Bloque(motivo.transform,"Brazo_Fragmento_"+i,(apoyo+p+radial)*.5f,new Vector3(Vector3.Distance(apoyo,p+radial),.055f,.055f),k.piedra);
            brazo.transform.rotation=giro;
            baseFigura.SetPositionAndRotation(p+giro*Vector3.forward*.025f,giro);baseFigura.localScale=Vector3.one*escala*.91f;
            tallado.SetPositionAndRotation(p,giro);tallado.localScale=Vector3.one*escala*.7f;
        }
        var linea=motivo.GetComponent<LineRenderer>();if(linea!=null)Object.DestroyImmediate(linea);motivo.guia=null;
        var luz=Luz(nivel,"Haz_Cenital_Farol",new Vector3(destino.x,6.7f,destino.z),Hex("#DDD6C2"),1.5f,8,false);
        luz.type=LightType.Spot;luz.spotAngle=22;luz.innerSpotAngle=8;luz.transform.LookAt(destino);motivo.luzCenital=luz;
    }

    static void RevisarIluminacion(Kit k,Transform nivel)
    {
        var retirar=new HashSet<string>{"Luz_Rotonda","Luz_Rotonda_Norte","Luz_Murales_Oeste","Luz_Murales_Este","Luz_Pasaje","Luz_Cuidado_Conjunto","Luz_Relieve_Perspectiva"};
        foreach(var l in nivel.GetComponentsInChildren<Light>(true))
        {
            if(retirar.Contains(l.name) || l.name=="Luz_Cartel" || (l.type==LightType.Point && !new[]{"Luz_Vela_Entrada","Brillo","Luz_Lampara","Luz_Columna","Luz_Columna_Techo","Luz_Aro_Obelisco_Piso","Luz_Aro_Obelisco_Techo","Luz_Bajo_Puerta_Sellos","Luz_Conjunto","Respuesta_Pieza","Rebote_Al_Regresar","LuzRebote"}.Contains(l.name))){Object.DestroyImmediate(l.gameObject);continue;}
            if(l.name=="Luz_Vela_Entrada")
            {
                l.shadows=LightShadows.None;l.range=2.5f;l.intensity=3;
                var llama=GameObject.CreatePrimitive(PrimitiveType.Sphere);llama.name="Llama_Vela";llama.transform.SetParent(l.transform,false);llama.transform.localScale=new Vector3(.035f,.075f,.035f);
                Object.DestroyImmediate(llama.GetComponent<Collider>());llama.GetComponent<Renderer>().sharedMaterial=Emisivo(Opaco("Llama_Calida",Hex("#E7BD7C"),0),new Color(1,.64f,.24f)*.8f);
            }
        }
        foreach(string n in new[]{"Faro_Atajo_Oeste","Faro_Atajo_Este","Requisito_SOL","Requisito_LUNA"})
        {
            var t=BuscarObjetoDocente(n);if(t==null)continue;
            var estado=t.GetComponent<TestigoDeSello>();
            if(estado!=null){estado.renderers=new Renderer[0];if(estado.luz!=null)Object.DestroyImmediate(estado.luz.gameObject);estado.luz=null;}
            // Se conserva el estado que referencian los sellos, sin el símbolo suelto.
            var r=t.GetComponent<Renderer>();if(r!=null)r.enabled=false;
        }
    }

    static void RepetirFrisosLobby(Kit k,Transform nivel)
    {
        var grupo=Grupo(nivel,"Frisos_Lobby_Continuos");
        var naranja=MateLiso("Friso_Lobby_Naranja","#C8641A",.05f);var azul=MateLiso("Friso_Lobby_Azul","#2A4FA8",.05f);
        foreach(var t in nivel.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Friso_Umbral_Oeste"||t.name=="Friso_Umbral_Este").ToArray())Object.DestroyImmediate(t.gameObject);
        CrearGuardasContinuas(grupo,naranja,azul);
    }

    static void EnmarcarHistoria(Kit k,Transform nivel)
    {
        var marco=MateLiso("Marco_Historia_Piedra","#514335",.05f);
        var historia=nivel.Find("Historia_Nueva");
        var mural=historia.Find("Historia_09_Farol");var plano=mural.Find("PinturaMural");plano.localScale=new Vector3(4.8f,2.4f,1);
        var direccion=new Vector3(.9f,0,.435f).normalized;
        mural.SetPositionAndRotation(EspacioCrater.Punto(CentroRotonda)+direccion*17.8f+Vector3.up*2.25f,Quaternion.LookRotation(direccion));
        foreach(Transform raiz in historia)
        {
            var imagen=raiz.Find("PinturaMural");if(imagen==null)continue;
            float w=imagen.localScale.x,h=imagen.localScale.y;var borde=Grupo(raiz,"Marco_Narrativa");
            foreach(float lado in new[]{-1f,1f})
            {
                CajaDePlaca(borde,"Moldura_Horizontal",new Vector3(0,lado*(h*.5f+.06f),.015f),new Vector3(w+.16f,.045f,.045f),marco);
                CajaDePlaca(borde,"Moldura_Vertical",new Vector3(lado*(w*.5f+.06f),0,.015f),new Vector3(.045f,h+.16f,.045f),marco);
            }
            if(raiz==mural)CajaDePlaca(borde,"Paño_Farol",new Vector3(0,0,.10f),new Vector3(w+.18f,h+.18f,.14f),k.basalto);
        }
    }

    static void RedondearCresta(Kit k,Transform nivel)
    {
        var g=BuscarObjetoDocente("05_Cresta");Vector3 centro=EspacioCrater.Punto(new Vector3(0,0,87));const float radio=15;
        var plantilla=BuscarObjetoDocente("ParedEspejo").gameObject;
        var antiguo=nivel.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Muro_Cresta_")||t.name.StartsWith("Techo_Cresta_")).ToArray();
        foreach(var t in antiguo)Object.DestroyImmediate(t.gameObject);
        var anillo=Grupo(g,"Sala_Final_Circular");
        var colisiones=new List<Collider>();
        for(int i=0;i<48;i++)
        {
            float a=(i+.5f)*Mathf.PI*2/48;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            if(d.z<-.97f)continue;
            var pared=Bloque(anillo,"Muro_Circular_"+i,centro+d*(radio+.25f)+Vector3.up*4,new Vector3(2.12f,8.6f,.55f),i%4==0?k.basaltoMedio:k.basalto);
            pared.transform.rotation=Quaternion.LookRotation(d);
            colisiones.Add(pared.GetComponent<Collider>());
            pared.GetComponent<Renderer>().enabled=false;
        }
        AfinarSuperficiesSalaFinal(k,anillo,centro,radio,colisiones);
        plantilla.transform.SetParent(anillo,true);plantilla.transform.localScale=Vector3.one;
        var reflejoCurvo=plantilla.GetComponent<ParedEspejo>();reflejoCurvo.superficiesCirculares=colisiones.ToArray();reflejoCurvo.centroCircular=centro;reflejoCurvo.radioCircular=14.8f;
        foreach(string n in new[]{"ParedEspejo_Oeste","ParedEspejo_Este","ParedEspejo_Entrada"}){var t=BuscarObjetoDocente(n);if(t!=null)Object.DestroyImmediate(t.gameObject);}
        // Techo anular: las hojas móviles existentes siguen cubriendo la abertura central.
        var v=new List<Vector3>();var uv=new List<Vector2>();
        for(int i=0;i<96;i++)
        {
            float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96;
            Vector3 P(float t,float r)=>centro+new Vector3(Mathf.Cos(t)*r,8,Mathf.Sin(t)*r);
            // Las hojas de 12 m cubren por completo el óculo circular de 12 m.
            var aa=P(a,6f);var bb=P(b,6f);var cc=P(b,15.6f);var dd=P(a,15.6f);
            Triangulo(v,uv,aa,cc,bb,PixelDeFaceta(aa));Triangulo(v,uv,aa,dd,cc,PixelDeFaceta(dd));
        }
        var escalaMundo=new Vector3(1/anillo.lossyScale.x,1/anillo.lossyScale.y,1/anillo.lossyScale.z);
        var techo=Tronco(anillo,"Techo_Circular",Vector3.zero,GuardarMalla(CrearMalla("Techo_Cresta_Circular",v,uv)),k.techo);
        // Esta malla ya usa coordenadas mundiales: compensar la ampliación del subsuelo.
        techo.transform.localScale=escalaMundo;techo.AddComponent<MeshCollider>().sharedMesh=techo.GetComponent<MeshFilter>().sharedMesh;
        var piso=BuscarObjetoDocente("Piso_Cresta");piso.position=centro-Vector3.up*.3f;piso.localScale=escalaMundo;
        var mp=CrearPisoFinalFacetado();piso.GetComponent<MeshFilter>().sharedMesh=mp;
        foreach(var c in piso.GetComponents<Collider>())Object.DestroyImmediate(c);piso.gameObject.AddComponent<MeshCollider>().sharedMesh=mp;
        var latentes=BuscarObjetoDocente("MotivosLatentes_Cresta");
        foreach(var f in latentes.GetComponentsInChildren<MeshFilter>())
        {
            var m=Object.Instantiate(f.sharedMesh);var vertices=m.vertices;
            for(int i=0;i<vertices.Length;i++){var p=f.transform.TransformPoint(vertices[i]);var d=p-centro;d.y=0;if(d.sqrMagnitude>.001f){var q=centro+d.normalized*14.8f;q.y=p.y;vertices[i]=f.transform.InverseTransformPoint(q);}}
            m.vertices=vertices;m.RecalculateBounds();m.RecalculateNormals();m.name=f.sharedMesh.name+"_Circular";f.sharedMesh=GuardarMalla(m);
        }
        AgregarReflejoCompuerta(k);
    }
}
