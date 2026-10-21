using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class ConstructorCrater
{
    // Farol portátil: depósito, difusor opalino, jaula y asa. Sin colliders ni luces
    // propias: el haz y los filtros siguen perteneciendo a LinternaController.
    static GameObject CrearModeloFarol(Transform padre)
    {
        AsegurarCarpeta("Assets/Models/Farol");
        var raiz = new GameObject("Modelo");
        raiz.transform.SetParent(padre, false);
        var metal = Opaco("Farol_Cuerpo", new Color(.065f, .10f, .095f), .28f, .72f);
        var jaula = Opaco("Farol_Jaula", new Color(.12f, .095f, .065f), .3f, .8f);
        var lente = Emisivo(Opaco("Farol_Difusor", new Color(.85f, .77f, .59f), .16f), Color.black);

        PiezaFarol(raiz.transform, "Deposito", PerfilFarol("Deposito", new[] {
            new Vector2(.066f,-.22f), new Vector2(.085f,-.21f), new Vector2(.094f,-.19f),
            new Vector2(.094f,-.07f), new Vector2(.086f,-.045f), new Vector2(.06f,-.025f)
        }), metal);
        PiezaFarol(raiz.transform, "Lens_Difusor", PerfilFarol("Difusor", new[] {
            new Vector2(.067f,-.015f), new Vector2(.081f,.005f), new Vector2(.086f,.035f),
            new Vector2(.086f,.15f), new Vector2(.077f,.18f), new Vector2(.064f,.19f)
        }), lente);
        PiezaFarol(raiz.transform, "Tapa", PerfilFarol("Tapa", new[] {
            new Vector2(.103f,.19f), new Vector2(.105f,.202f), new Vector2(.083f,.215f),
            new Vector2(.069f,.225f), new Vector2(.066f,.24f)
        }), metal);

        var soportes = new GameObject("PiezasMetalicas");
        soportes.transform.SetParent(raiz.transform, false);
        // Ocho nervios curvos, dejando visible el vidrio entre ellos.
        for (int i = 0; i < 8; i++)
        {
            float a = (i + .5f) * Mathf.PI / 4f;
            Vector3 Punto(float radio, float y) => new Vector3(Mathf.Cos(a)*radio,y,Mathf.Sin(a)*radio);
            var puntos = new[] { Punto(.076f,-.026f), Punto(.098f,.012f),
                Punto(.101f,.15f), Punto(.084f,.197f) };
            for (int j = 1; j < puntos.Length; j++) BarraFarol(soportes.transform,puntos[j-1],puntos[j],.006f);
        }
        // Asa arqueada en el plano vertical; se ve de frente y de perfil.
        Vector3 anterior = new Vector3(-.09f,.225f,0f);
        for (int i = 1; i <= 16; i++)
        {
            float a = Mathf.PI - i * Mathf.PI / 16f;
            var siguiente = new Vector3(Mathf.Cos(a)*.09f,.225f+Mathf.Sin(a)*.115f,0f);
            BarraFarol(soportes.transform, anterior, siguiente, .007f);
            anterior = siguiente;
        }
        var piezas = soportes.GetComponentsInChildren<MeshFilter>();
        var combinada = new Mesh { name = "JaulaYAsa" };
        combinada.CombineMeshes(piezas.Select(f => new CombineInstance {
            mesh=f.sharedMesh, transform=soportes.transform.worldToLocalMatrix*f.transform.localToWorldMatrix
        }).ToArray());
        PiezaFarol(raiz.transform,"JaulaYAsa",combinada,jaula);
        Object.DestroyImmediate(soportes);
        return raiz;
    }

    static void BarraFarol(Transform padre, Vector3 a, Vector3 b, float radio)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre,false);
        go.transform.localPosition=(a+b)*.5f;
        go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
        go.transform.localScale=new Vector3(radio*2f,(b-a).magnitude*.5f,radio*2f);
    }

    static Mesh PerfilFarol(string nombre, Vector2[] perfil)
    {
        const int lados=16;
        var v=new List<Vector3>();
        var t=new List<int>();
        // Vértices separados entre caras para conservar el lenguaje facetado del juego.
        for(int y=1;y<perfil.Length;y++) for(int i=0;i<lados;i++)
        {
            Vector3 Punto(Vector2 p,int j) => new Vector3(Mathf.Cos(j*Mathf.PI*2/lados)*p.x,p.y,Mathf.Sin(j*Mathf.PI*2/lados)*p.x);
            int k=v.Count;
            v.Add(Punto(perfil[y-1],i)); v.Add(Punto(perfil[y],i));
            v.Add(Punto(perfil[y],i+1)); v.Add(Punto(perfil[y-1],i+1));
            t.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
        }
        // Tapas, para que el depósito no se vea hueco al inclinarse.
        for(int extremo=0;extremo<2;extremo++)
        {
            Vector2 p=perfil[extremo==0?0:perfil.Length-1];
            for(int i=0;i<lados;i++)
            {
                int k=v.Count;
                v.Add(new Vector3(0,p.y,0));
                v.Add(new Vector3(Mathf.Cos(i*Mathf.PI*2/lados)*p.x,p.y,Mathf.Sin(i*Mathf.PI*2/lados)*p.x));
                v.Add(new Vector3(Mathf.Cos((i+1)*Mathf.PI*2/lados)*p.x,p.y,Mathf.Sin((i+1)*Mathf.PI*2/lados)*p.x));
                t.AddRange(extremo==0?new[]{k,k+1,k+2}:new[]{k,k+2,k+1});
            }
        }
        var m=new Mesh{name=nombre}; m.SetVertices(v);m.SetTriangles(t,0);
        m.RecalculateNormals();m.RecalculateBounds();return m;
    }

    static void PiezaFarol(Transform padre,string nombre,Mesh malla,Material material)
    {
        string ruta="Assets/Models/Farol/"+malla.name+".asset";
        var existente=AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if(existente==null) AssetDatabase.CreateAsset(malla,ruta);
        else { EditorUtility.CopySerialized(malla,existente);Object.DestroyImmediate(malla);malla=existente; }
        var go=new GameObject(nombre,typeof(MeshFilter),typeof(MeshRenderer));
        go.transform.SetParent(padre,false);
        go.GetComponent<MeshFilter>().sharedMesh=malla;
        go.GetComponent<Renderer>().sharedMaterial=material;
    }

    [MenuItem("Crater/Actualizar título y farol")]
    public static void ActualizarTituloYFarol()
    {
        // Actualización localizada: conserva la escena restaurada, el nivel y los WAV.
        foreach(string nombre in new[]{"Jugador","Recogible_Linterna"})
        {
            string ruta=CarpetaPrefabs+nombre+".prefab";
            var raiz=PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                var visual=raiz.GetComponentInChildren<LinternaVisual>(true);
                Transform padre=visual!=null?visual.transform:raiz.transform.Find("Visual");
                var anterior=padre.Find("Modelo");
                if(anterior!=null) Object.DestroyImmediate(anterior.gameObject);
                var modelo=CrearModeloFarol(padre);
                if(visual!=null)
                {
                    visual.posicionReposo=new Vector3(.26f,-.35f,.66f);
                    modelo.transform.localScale=Vector3.one*.74f;
                    modelo.transform.localPosition=visual.posicionReposo;
                    modelo.transform.localRotation=Quaternion.Euler(0,-18,-8);
                    visual.modelo=modelo.transform;
                    visual.lentes=modelo.GetComponentsInChildren<Renderer>().Where(r=>r.name.Contains("Lens")).ToArray();
                    foreach(var r in modelo.GetComponentsInChildren<Renderer>()) r.shadowCastingMode=ShadowCastingMode.Off;
                    PonerCapa(modelo,LayerMask.NameToLayer(CapaJugador));
                }
                else
                {
                    modelo.transform.localPosition=new Vector3(0,.24f,0);
                    modelo.transform.localRotation=Quaternion.Euler(0,25,0);
                    PonerCapa(modelo,LayerMask.NameToLayer("Ignore Raycast"));
                }
                PrefabUtility.SaveAsPrefabAsset(raiz,ruta);
            }
            finally { PrefabUtility.UnloadPrefabContents(raiz); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[CRÁTER] Título y farol actualizados; nivel y audio conservados.");
    }
}
