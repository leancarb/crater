using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static partial class ConstructorCrater
{
    static void AplicarAnclasCarreraYAros(Kit k,Transform raiz)
    {
        Vector3 P(float x,float y,float z)=>EspacioCrater.Punto(new Vector3(x,y,z));
        Vector3 O(float x,float y,float z)=>EspacioCrater.Punto(new Vector3(x,y,z)+CorrimientoAlaOeste);
        var casos=new[]{("Puente_O_Ensenar",O(-24.8f,1.4f,1)),("Puente_O_Torcer",O(-16,1.4f,14.3f)),
            ("Puente_N_Reja",P(-2,1.4f,34.5f)),("Puente_N_Borde",P(0,1.4f,44.5f)),("Puente_N_Salida",P(0,1.4f,52))};
        foreach(var caso in casos)
        {
            var puente=BuscarObjetoDocente(caso.Item1)?.GetComponent<PuenteLuz>();if(puente==null)continue;
            bool techo=caso.Item1=="Puente_N_Borde"||caso.Item1=="Puente_O_Torcer";
            if(caso.Item1=="Puente_N_Borde")
                for(int i=0;i<puente.anclas.Count;i++)
                {
                    var a=puente.anclas[i];a.transform.position=P(i==0?-3.5f:3.5f,5.95f,46);
                    a.retencion=1.9f;EditorUtility.SetDirty(a);
                }
            Vector3 centro=(puente.anclas[0].transform.position+puente.anclas[1].transform.position)*.5f;
            Vector3 frente=caso.Item2-centro;frente.y=0;frente.Normalize();
            for(int i=0;i<puente.anclas.Count;i++)
            {
                var a=puente.anclas[i];var normal=Quaternion.AngleAxis(i==0?-45:45,Vector3.up)*frente;
                if(techo)normal=(normal+Vector3.down).normalized;
                a.transform.rotation=Quaternion.LookRotation(normal,techo?Vector3.down:Vector3.up);
                PrefabUtility.RecordPrefabInstancePropertyModifications(a.transform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(a);
            }
        }
        var obelisco=Object.FindFirstObjectByType<ObeliscoDelEclipse>();
        if(obelisco!=null)
        {
            var g=Grupo(raiz,"Aros_Completos_Obelisco");
            var estado=g.gameObject.AddComponent<TestigoDeSello>();estado.manual=true;estado.transicion=1;
            estado.colorEncendido=obelisco.colorEclipse;estado.emisionApagado=.01f;estado.emisionEncendido=2;
            // Borde del sexto y último escalón: plaza hundida a -1,20 m.
            var centro=P(0,0,12);float radio=3.8f*EspacioCrater.Escala;
            var piso=AroCentral(k,g,"Aro_Obelisco_Piso",new Vector3(centro.x,-1.055f,centro.z),radio-.06f,radio+.06f,false,k.tallaEclipse);
            AjustarAberturaDelObelisco(k,g,centro);
            float abertura=RadioAberturaRotonda*EspacioCrater.Escala;
            var techo=AroCentral(k,g,"Aro_Obelisco_Techo",new Vector3(centro.x,6.835f,centro.z),abertura,abertura+.12f,true,k.tallaEclipse);
            // Tapa anular opaca de dos caras: no obstruye el óculo central.
            AroCentral(k,g,"Carcasa_Aro_Obelisco",new Vector3(centro.x,6.90f,centro.z),abertura,abertura+.18f,false,k.techo,true);
            estado.renderers=new[]{piso,techo};
            estado.luz=Luz(g,"Luz_Aro_Obelisco_Piso",new Vector3(centro.x,-.75f,centro.z),estado.colorEncendido,0,8,false);
            estado.luzSuperior=Luz(g,"Luz_Aro_Obelisco_Techo",new Vector3(centro.x,6.7f,centro.z),estado.colorEncendido,0,8,false);
            estado.intensidadLuz=18;estado.intensidadLuzSuperior=18;
            obelisco.arosCompletos=estado;EditorUtility.SetDirty(obelisco);
        }
        CerrarBordesDelValle(k,raiz);
    }

    static void AjustarAberturaDelObelisco(Kit k,Transform padre,Vector3 centro)
    {
        var techo=BuscarObjetoDocente("Techo_Rotonda_Circular");if(techo==null)return;
        float radio=RadioAberturaRotonda*EspacioCrater.Escala;
        var v=new List<Vector3>();var uv=new List<Vector2>();
        Vector3 P(int i,float r,float y){float a=i*Mathf.PI/48;return new Vector3(centro.x+Mathf.Cos(a)*r,y,centro.z+Mathf.Sin(a)*r);}
        Vector3 Exterior(int i){float a=i*Mathf.PI/48;return P(i,15.6f*EspacioCrater.Escala/Mathf.Max(Mathf.Abs(Mathf.Cos(a)),Mathf.Abs(Mathf.Sin(a))),7);}
        for(int i=0;i<96;i++)
        {
            var a=techo.InverseTransformPoint(P(i,radio,7));var b=techo.InverseTransformPoint(P(i+1,radio,7));
            var c=techo.InverseTransformPoint(Exterior(i));var d=techo.InverseTransformPoint(Exterior(i+1));
            Triangulo(v,uv,a,c,b,PixelDeFaceta(a));Triangulo(v,uv,b,c,d,PixelDeFaceta(b));
        }
        var m=GuardarMalla(CrearMalla("Techo_Oculo_Borde_Obelisco",v,uv));
        techo.GetComponent<MeshFilter>().sharedMesh=m;techo.GetComponent<MeshCollider>().sharedMesh=m;
        EditorUtility.SetDirty(techo.GetComponent<MeshFilter>());EditorUtility.SetDirty(techo.GetComponent<MeshCollider>());
        // Canto interior continuo desde la abertura hasta el aro, todo por debajo de y=7.
        v.Clear();uv.Clear();
        for(int i=0;i<96;i++)
        {
            var a=P(i,radio,6.835f);var b=P(i+1,radio,6.835f);var c=P(i+1,radio,7);var d=P(i,radio,7);
            Triangulo(v,uv,a,b,c,Vector2.zero);Triangulo(v,uv,a,c,d,Vector2.zero);
            Triangulo(v,uv,a,c,b,Vector2.zero);Triangulo(v,uv,a,d,c,Vector2.zero);
        }
        var canto=new GameObject("Canto_Abertura_Obelisco");canto.transform.SetParent(padre,false);
        canto.AddComponent<MeshFilter>().sharedMesh=GuardarMalla(CrearMalla("Canto_Abertura_Obelisco",v,uv));
        canto.AddComponent<MeshRenderer>().sharedMaterial=k.techo;
    }

    static Renderer AroCentral(Kit k,Transform padre,string nombre,Vector3 centro,float interior,float exterior,bool abajo,Material material,bool doble=false)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();
        for(int i=0;i<96;i++)
        {
            Vector3 P(int j,float r)=>new Vector3(Mathf.Cos(j*Mathf.PI/48)*r,0,Mathf.Sin(j*Mathf.PI/48)*r);
            var a=P(i,interior);var b=P(i+1,interior);var c=P(i+1,exterior);var d=P(i,exterior);
            Triangulo(v,uv,a,b,c,Vector2.zero);Triangulo(v,uv,a,c,d,Vector2.zero);
            if(doble){Triangulo(v,uv,a,c,b,Vector2.zero);Triangulo(v,uv,a,d,c,Vector2.zero);}
        }
        var go=new GameObject(nombre);go.transform.SetParent(padre,false);
        go.transform.SetPositionAndRotation(centro,abajo?Quaternion.Euler(180,0,0):Quaternion.identity);
        go.AddComponent<MeshFilter>().sharedMesh=GuardarMalla(CrearMalla(nombre,v,uv));
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;
        r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return r;
    }

    static void CerrarBordesDelValle(Kit k,Transform raiz)
    {
        var exterior=BuscarObjetoDocente("06_Capilla");if(exterior==null)return;
        var g=Grupo(raiz,"Cierre_Rocoso_Valle");var azar=new System.Random(1945);
        void Roca(float x,float z,float ancho,float fondo)
        {
            // No bajar de la superficie exterior: el techo del subsuelo está a y=7.
            var local=new Vector3(x,-.12f,z);
            // Un valle visual hacia el sol: centro bajo y bordes altos, sin huecos en el perímetro.
            float lateral=Mathf.SmoothStep(0,1,Mathf.Abs(x)/42f);
            float minimo=z>160?16f:8f;
            float alto=Mathf.Lerp(minimo,32f,lateral)+(float)azar.NextDouble()*(.4f+1.6f*lateral);
            RocaGuiaDocente(k,g,"Roca_Cierre_Valle",exterior.TransformPoint(local),new Vector3(ancho,alto,fondo),azar,true);
            var roca=g.GetChild(g.childCount-1);roca.GetComponent<Renderer>().sharedMaterial=k.tierra;
            // La misma malla cierra visualmente el paisaje y bloquea el paso.
            roca.gameObject.AddComponent<MeshCollider>().sharedMesh=roca.GetComponent<MeshFilter>().sharedMesh;
        }
        foreach(float lado in new[]{-1f,1f})for(int i=0;i<14;i++)Roca(lado*44,-19+i*14,13,17);
        foreach(float z in new[]{-48f,169f})for(int i=0;i<7;i++)Roca(-42+i*14,z,17,13);
    }
}
