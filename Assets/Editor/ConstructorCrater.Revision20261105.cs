using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static partial class ConstructorCrater
{
    static void AdelantarCartelesInicio(Transform nivel)
    {
        var exterior = nivel.Find("06_Capilla");
        var spawn = exterior.Find("SpawnValle");
        void Situar(string nombre, Vector2 punto)
        {
            var cartel = BuscarObjetoDocente(nombre);
            var p = exterior.TransformPoint(new Vector3(punto.x, AlturaInicioElevado(punto.x, punto.y) + 1.5f, punto.y));
            var hacia = spawn.position - p; hacia.y = 0;
            cartel.SetPositionAndRotation(p, Quaternion.LookRotation(-hacia));
        }
        Situar("Cartel_01_Mover", new Vector2(17, 191.5f));
        Situar("Cartel_02_Saltar", new Vector2(10, 188));
    }

    // Una fase para todo el anillo: las juntas entre bloques no reinician el motivo.
    // Cada trazo se proyecta sobre piedra real y se interrumpe en las puertas.
    static void CrearGuardasContinuas(Transform grupo, Material naranja, Material azul)
    {
        Physics.SyncTransforms();
        var centro = EspacioCrater.Punto(CentroRotonda); centro.y = 3.9f;
        var vertices = new[] { new List<Vector3>(), new List<Vector3>() };
        var uvs = new[] { new List<Vector2>(), new List<Vector2>() };
        bool Proyectar(Vector3 origen, Vector3 direccion, string prefijo, out Vector3 punto)
        {
            if (Physics.Raycast(origen, direccion, out var h, 30, 1, QueryTriggerInteraction.Ignore) && h.collider.name.StartsWith(prefijo))
            { punto = h.point - direccion * .10f; return true; }
            punto = default; return false;
        }
        void Trazo(Vector3 a, Vector3 b, Vector3 normal)
        {
            if ((b-a).sqrMagnitude < .000001f || Vector3.Distance(a,b) > .65f) return;
            int lado = (a.x+b.x) < 0 ? 0 : 1;
            var ancho = Vector3.Cross(normal, b-a).normalized * .0225f;
            var aa = a-ancho; var bb = b-ancho; var cc = b+ancho; var dd = a+ancho;
            if (Vector3.Dot(Vector3.Cross(bb-aa,cc-aa),normal)<0) { var q=aa;aa=dd;dd=q;q=bb;bb=cc;cc=q; }
            Triangulo(vertices[lado],uvs[lado],aa,bb,cc,Vector2.zero);
            Triangulo(vertices[lado],uvs[lado],aa,cc,dd,Vector2.zero);
        }
        const float paso = .14f, amplitud = .132f;
        float[] alturas = { -amplitud, 0, amplitud, 0 };
        float radio = RadioRotonda * EspacioCrater.Escala;
        int pasos = Mathf.RoundToInt(2*Mathf.PI*radio/(paso*4))*4;
        for(int i=0;i<pasos;i++)
        {
            float a=i*2*Mathf.PI/pasos,b=(i+1)*2*Mathf.PI/pasos;
            Vector3 D(float t) => new Vector3(Mathf.Cos(t),0,Mathf.Sin(t));
            if (!Proyectar(centro,D(a),"Pared_Rotonda_",out var p) || !Proyectar(centro,D(b),"Pared_Rotonda_",out var q) ||
                !Proyectar(centro,D((a+b)*.5f),"Pared_Rotonda_",out _)) continue;
            float y=alturas[i%4],sig=alturas[(i+1)%4];
            Trazo(p+Vector3.up*y,q+Vector3.up*y,-D((a+b)*.5f));
            Trazo(q+Vector3.up*y,q+Vector3.up*sig,-D(b));
        }
        foreach(float lado in new[]{-1f,1f})
        {
            var pared = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Single(c=>c.name==(lado<0?"Muro_Umbral_Izq":"Muro_Umbral_Der"));
            var b=pared.bounds;int inicio=Mathf.CeilToInt(b.min.z/paso),fin=Mathf.FloorToInt(b.max.z/paso);
            for(int i=inicio;i<fin;i++)
            {
                var origen=new Vector3(0,3.9f,i*paso);var siguiente=origen+Vector3.forward*paso;var d=Vector3.right*lado;
                if(!Proyectar(origen,d,"Muro_Umbral_",out var p)||!Proyectar(siguiente,d,"Muro_Umbral_",out var q))continue;
                int fase=((i%4)+4)%4;float y=alturas[fase],sig=alturas[(fase+1)%4];
                Trazo(p+Vector3.up*y,q+Vector3.up*y,-d);Trazo(q+Vector3.up*y,q+Vector3.up*sig,-d);
            }
        }
        for(int lado=0;lado<2;lado++)
            SuperficieMundial(grupo,"Friso_Lobby_Continuo_"+(lado==0?"Sol":"Luna"),vertices[lado],uvs[lado],lado==0?naranja:azul);
    }

    static GameObject SuperficieMundial(Transform padre,string nombre,List<Vector3> v,List<Vector2> uv,Material material)
    {
        var go=Tronco(padre,nombre,Vector3.zero,GuardarMalla(CrearMalla(nombre,v,uv)),material);
        var escala=padre.lossyScale;go.transform.localScale=new Vector3(1/escala.x,1/escala.y,1/escala.z);
        return go;
    }

    static void AfinarSuperficiesSalaFinal(Kit k,Transform anillo,Vector3 centro,float radio,List<Collider> colisiones)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();
        void Cara(Vector3 a,Vector3 b,Vector3 c,Vector3 normal)
        {
            if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var t=b;b=c;c=t;}
            Triangulo(v,uv,a,b,c,PixelDeFaceta((a+b+c)/3));
        }
        // Triángulos grandes y un único tono por cara; sin UV de cubo estiradas en bandas.
        for(int i=0;i<48;i++)
        {
            float a=i*2*Mathf.PI/48,b=(i+1)*2*Mathf.PI/48;
            if(Mathf.Sin((a+b)*.5f)<-.97f)continue;
            Vector3 P(float t,int fila) => centro+new Vector3(Mathf.Cos(t)*radio,-.3f+fila*2.15f,Mathf.Sin(t)*radio);
            var normal=new Vector3(-Mathf.Cos((a+b)*.5f),0,-Mathf.Sin((a+b)*.5f));
            for(int fila=0;fila<4;fila++)
            {
                var aa=P(a,fila);var bb=P(b,fila);var cc=P(b,fila+1);var dd=P(a,fila+1);
                if((i+fila)%2==0){Cara(aa,bb,cc,normal);Cara(aa,cc,dd,normal);}
                else{Cara(aa,bb,dd,normal);Cara(bb,cc,dd,normal);}
            }
        }
        SuperficieMundial(anillo,"Pared_Final_Facetada",v,uv,k.basalto);
        // Transición entre el anillo y el vano rectangular: cerrar los huecos laterales.
        var cierre=BuscarObjetoDocente("Cierre_Cresta");var losa=cierre.Find("Losa").GetComponent<Renderer>().bounds;
        var perfilPortal=new Perfil(1.3f,Vector3.zero,2.6f,1);
        var dintel=BuscarObjetoDocente("Dintel_Corredor");
        dintel.GetComponent<MeshFilter>().sharedMesh=CajaFacetada(dintel,perfilPortal);
        float z=losa.max.z+.03f;
        foreach(float lado in new[]{-1f,1f})
        {
            float x0=lado*losa.extents.x,x1=lado*radio*Mathf.Cos(255*Mathf.Deg2Rad);
            // La apertura empieza a ±3,88 m: el hueco útil de la compuerta mide 5 m.
            x1=lado*Mathf.Abs(x1);
            float borde=centro.z+radio*Mathf.Sin(255*Mathf.Deg2Rad);
            var jamba=Bloque(anillo,"Jamba_Circular_"+lado,new Vector3((x0+x1)*.5f,3.85f,(z+borde)*.5f),
                new Vector3(Mathf.Abs(x1-x0)+.12f,8.3f,Mathf.Abs(borde-z)+.35f),k.basalto);
            // Dimensiones mundiales, independientemente de la escala del subsuelo.
            jamba.transform.localScale=Vector3.Scale(jamba.transform.localScale,new Vector3(1/anillo.lossyScale.x,1/anillo.lossyScale.y,1/anillo.lossyScale.z));
            // Perímetro recto en el vano: conserva los tonos por faceta sin abrir rendijas.
            var mesh=CajaFacetada(jamba.transform,perfilPortal);jamba.GetComponent<MeshFilter>().sharedMesh=mesh;
        }
    }

    static Mesh CrearPisoFinalFacetado()
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();const int lados=64;
        // Recortar celdas contra el perímetro; evita el abanico de triángulos de 15 metros.
        for(float x=-16;x<16;x+=2.4f)for(float z=-16;z<16;z+=2.4f)
        {
            Vector3 P(float a,float b)=>new Vector3(a+.28f*Mathf.Sin(a*1.7f+b),.3f,b+.28f*Mathf.Sin(b*1.3f-a));
            var poligono=new List<Vector3>{P(x,z),P(x,z+2.4f),P(x+2.4f,z+2.4f),P(x+2.4f,z)};
            for(int i=0;i<lados&&poligono.Count>0;i++)
            {
                float a=(i+.5f)*Mathf.PI*2/lados;var n=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                float limite=15.4f*Mathf.Cos(Mathf.PI/lados);var salida=new List<Vector3>();
                for(int j=0;j<poligono.Count;j++)
                {
                    var p=poligono[j];var q=poligono[(j+1)%poligono.Count];float dp=Vector3.Dot(p,n)-limite,dq=Vector3.Dot(q,n)-limite;
                    if(dp<=0)salida.Add(p);if((dp<=0)!=(dq<=0))salida.Add(Vector3.Lerp(p,q,dp/(dp-dq)));
                }
                poligono=salida;
            }
            for(int i=1;i<poligono.Count-1;i++)
            {
                var a=poligono[0];var b=poligono[i];var c=poligono[i+1];
                if(Vector3.Cross(b-a,c-a).sqrMagnitude>.000001f)Triangulo(v,uv,a,b,c,PixelDeFaceta((a+b+c)/3));
            }
        }
        return GuardarMalla(CrearMalla("Piso_Cresta_Circular",v,uv));
    }

    static void AgregarReflejoCompuerta(Kit k)
    {
        var puerta=BuscarObjetoDocente("Cierre_Cresta");var b=puerta.Find("Losa").GetComponent<Renderer>().bounds;
        var raiz=Grupo(puerta,"ParedEspejo_Compuerta");
        raiz.SetPositionAndRotation(new Vector3(b.center.x,b.center.y,b.max.z+.01f),Quaternion.LookRotation(Vector3.back));
        var escala=puerta.lossyScale;raiz.localScale=new Vector3(1/escala.x,1/escala.y,1/escala.z);
        var espejo=raiz.gameObject.AddComponent<ParedEspejo>();var so=new SerializedObject(espejo);
        so.FindProperty("tamanio").vector2Value=new Vector2(b.size.x,b.size.y);so.ApplyModifiedPropertiesWithoutUndo();
        Asignar(espejo,"reflejo",Plano(raiz,"Reflejo",k.resplandorTallado));
        Asignar(espejo,"luzRebote",Luz(raiz,"LuzRebote",raiz.position+Vector3.forward*.6f,Color.white,0,12,false));
        espejo.enabled=false;
        var continuo=BuscarObjetoDocente("ParedEspejo").GetComponent<ParedEspejo>();
        continuo.superficiesCirculares=continuo.superficiesCirculares.Concat(new[]{puerta.Find("Losa").GetComponent<Collider>(),BuscarObjetoDocente("Jamba_Circular_-1").GetComponent<Collider>(),BuscarObjetoDocente("Jamba_Circular_1").GetComponent<Collider>(),BuscarObjetoDocente("Dintel_Corredor").GetComponent<Collider>()}).ToArray();
    }
}
