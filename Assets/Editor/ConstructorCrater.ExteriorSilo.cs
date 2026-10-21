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
    [MenuItem("Crater/Actualizar exterior referencia Silo")]
    public static void ActualizarExteriorSilo()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Salir de Play antes de actualizar el exterior.");
        AplicarExteriorSilo();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    // También se llama desde ConstruirEscena, después del facetado. Sólo modifica
    // arte exterior y parámetros diurnos; no mueve suelo, zonas, eclipse ni colliders.
    static void AplicarExteriorSilo()
    {
        var valle=GameObject.Find("06_Capilla");
        if(valle==null) throw new InvalidOperationException("Abrir la escena CraterVerticalSlice.");
        var paisaje=valle.transform.Find("Paisaje");
        AsegurarCarpeta("Assets/Models/ExteriorSilo");
        var tierra=Opaco("CapillaTierra",new Color(.61f,.58f,.47f),.03f);
        tierra.shader=Shader.Find("Crater/SueloExterior");
        EditorUtility.SetDirty(tierra);
        Opaco("CapillaHuella",new Color(.50f,.49f,.40f),.03f);
        Opaco("CapillaAdobe",new Color(.47f,.45f,.35f),.06f);
        Opaco("CapillaTechoPaja",new Color(.25f,.27f,.23f),.04f);
        Opaco("CapillaPiedra",new Color(.43f,.43f,.37f),.06f);
        Opaco("CapillaCal",new Color(.66f,.66f,.55f),.05f);
        Opaco("Cardon",new Color(.26f,.25f,.20f),.03f);
        Opaco("PajaBrava",new Color(.37f,.35f,.26f),.03f);
        var hormigon=Opaco("Exterior_Hormigon",new Color(.44f,.44f,.38f),.04f);
        hormigon.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Texturas/Facetas.png"));
        foreach(var r in valle.transform.Find("Crater_Valle").GetComponentsInChildren<Renderer>(true))
            if(r.name.StartsWith("Borde_")||r.name.StartsWith("Cordon_")||r.name=="Umbral") r.sharedMaterial=hormigon;
        var roca=Opaco("Exterior_GravaOscura",new Color(.23f,.25f,.23f),.02f);
        var clara=Opaco("Exterior_GravaPolvo",new Color(.39f,.38f,.31f),.02f);
        var ramas=Opaco("Exterior_RamasSecas",new Color(.31f,.30f,.24f),.03f);

        var cielo=valle.GetComponentInChildren<CieloEclipse>(true);
        var ajustes = new SerializedObject(cielo);
        void ColorDia(string campo, Color valor) => ajustes.FindProperty(campo).colorValue = valor;
        void NumeroDia(string campo, float valor) => ajustes.FindProperty(campo).floatValue = valor;
        ColorDia("solDia",new Color(.98f,.96f,.83f));
        NumeroDia("intensidadDia",2f);
        ColorDia("cieloDia",new Color(.87f,.90f,.79f));
        ColorDia("ambienteCieloDia",new Color(1f,1f,.94f));
        ColorDia("ambienteHorizonteDia",new Color(.85f,.83f,.70f));
        ColorDia("ambienteSueloDia",new Color(.50f,.49f,.40f));
        NumeroDia("densidadNiebla",.005f);

        ajustes.ApplyModifiedPropertiesWithoutUndo();
        var fondo=cielo.transform.Find("Fondo_Brumoso");
        if(fondo==null)
        {
            var esfera=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            esfera.name="Fondo_Brumoso";Object.DestroyImmediate(esfera.GetComponent<Collider>());
            esfera.transform.SetParent(cielo.transform,false);fondo=esfera.transform;
        }
        fondo.localScale=Vector3.one*380f;
        var rendererFondo=fondo.GetComponent<Renderer>();
        var materialFondo=SinLuz("Exterior_CieloBrumoso",Color.white);
        materialFondo.shader=Shader.Find("Crater/CieloExteriorBrumoso");EditorUtility.SetDirty(materialFondo);
        rendererFondo.sharedMaterial=materialFondo;rendererFondo.shadowCastingMode=ShadowCastingMode.Off;
        rendererFondo.receiveShadows=false;rendererFondo.allowOcclusionWhenDynamic=false;
        Asignar(cielo,"fondoBrumoso",rendererFondo);
        fondo.gameObject.SetActive(false);
        var anterior=paisaje.Find("Exterior_Silo");
        if(anterior!=null) Object.DestroyImmediate(anterior.gameObject);
        var grupo=Grupo(paisaje,"Exterior_Silo");
        var oscuros=new GeometriaExterior();var claros=new GeometriaExterior();var secos=new GeometriaExterior();
        var azar=new System.Random(151026);
        float Azar(float a,float b)=>a+(float)azar.NextDouble()*(b-a);

        // Muestrear las mallas visibles evita que las piedras floten sobre las facetas.
        var suelos=new List<MeshCollider>();
        foreach(string nombre in new[]{"Terreno","Lomas_Valle"})
        {
            var suelo=paisaje.Find(nombre);
            var temporal=new GameObject("MuestreoTemporal");temporal.hideFlags=HideFlags.HideAndDontSave;
            temporal.transform.SetPositionAndRotation(suelo.position,suelo.rotation);
            temporal.transform.localScale=suelo.lossyScale;
            var col=temporal.AddComponent<MeshCollider>();col.sharedMesh=suelo.GetComponent<MeshFilter>().sharedMesh;suelos.Add(col);
        }
        bool Altura(float x,float z,out float y)
        {
            var rayo=new Ray(paisaje.TransformPoint(new Vector3(x,40,z)),Vector3.down);
            float altura=float.NegativeInfinity;
            foreach(var col in suelos) if(col.Raycast(rayo,out var hit,70)) altura=Mathf.Max(altura,paisaje.InverseTransformPoint(hit.point).y);
            y=altura;return !float.IsNegativeInfinity(altura);
        }
        try
        {
            for(int i=0;i<11000;i++)
            {
                float x=Azar(-40,40),z=Azar(-15,162);
                if((Mathf.Abs(x)<6&&z<18)||Vector2.Distance(new Vector2(x,z),new Vector2(CraterEnLaCapilla.x,CraterEnLaCapilla.z))<RadioPozo+2.2f) continue;
                if(Mathf.Abs(x)<2.8f&&azar.NextDouble()>.22) continue;
                if(Mathf.PerlinNoise(x*.13f+43,z*.13f)<.38f||!Altura(x,z,out float y)) continue;
                float radio=Azar(.065f,.28f);
                if(i%41==0)radio*=2.4f;
                (i%4==0?claros:oscuros).Piedra(new Vector3(x,y-.025f,z),radio,Azar(.45f,.85f),Azar(0,360),azar);
            }
            int indice=0;
            foreach(var planta in paisaje.Cast<Transform>().Where(t=>t.name.StartsWith("Cardon_")).ToArray())
            {
                foreach(var r in planta.GetComponentsInChildren<Renderer>())r.enabled=false;
                Vector3 pie=planta.localPosition;
                float h=Azar(1.8f,3.6f);
                var punta=pie+new Vector3(Azar(-.25f,.25f),h,Azar(-.2f,.2f));
                secos.Rama(pie,punta,.11f,.025f);
                for(int j=0;j<4;j++)
                {
                    float a=Azar(0,Mathf.PI*2),altura=h*Azar(.32f,.7f);
                    Vector3 inicio=Vector3.Lerp(pie,punta,altura/h);
                    Vector3 codo=inicio+new Vector3(Mathf.Cos(a)*h*.28f,h*.16f,Mathf.Sin(a)*h*.28f);
                    Vector3 fin=codo+new Vector3(Mathf.Cos(a)*.15f,h*Azar(.18f,.32f),Mathf.Sin(a)*.15f);
                    secos.Rama(inicio,codo,.04f,.022f);secos.Rama(codo,fin,.022f,.005f);
                }
                indice++;
            }
            indice=0;
            foreach(var mata in paisaje.Cast<Transform>().Where(t=>t.name.StartsWith("Paja_")))
                foreach(var r in mata.GetComponentsInChildren<Renderer>())r.enabled=indice++%3==0;
            oscuros.Guardar(grupo,"Grava_Oscura",roca,false);
            claros.Guardar(grupo,"Grava_Clara",clara,false);
            secos.Guardar(grupo,"Arbustos_Secos",ramas,true);
        }
        finally { foreach(var col in suelos) Object.DestroyImmediate(col.gameObject); }
        Debug.Log("[CRÁTER] Exterior Silo: paleta gris ocre, grava agrupada y arbustos secos. Recorrido y colisiones conservados.");
    }

    sealed class GeometriaExterior
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<int> indices=new List<int>();
        void Tri(Vector3 a,Vector3 b,Vector3 c)
        {
            int i=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.Add(i);indices.Add(i+1);indices.Add(i+2);
        }
        public void Piedra(Vector3 pie,float radio,float achatado,float giro,System.Random azar)
        {
            var borde=new Vector3[7];var q=Quaternion.Euler(0,giro,0);
            for(int j=0;j<7;j++)
            {
                float a=j*Mathf.PI*2/7,r=radio*(.75f+(float)azar.NextDouble()*.35f);
                borde[j]=pie+q*new Vector3(Mathf.Cos(a)*r,radio*.18f,Mathf.Sin(a)*r);
            }
            Vector3 cima=pie+q*new Vector3(radio*.12f,radio*achatado,-radio*.1f);
            for(int j=0;j<7;j++){Tri(borde[j],cima,borde[(j+1)%7]);Tri(borde[(j+1)%7],pie-Vector3.up*radio*.22f,borde[j]);}
        }
        public void Rama(Vector3 a,Vector3 b,float ra,float rb)
        {
            var giro=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
            for(int j=0;j<6;j++)
            {
                float t=j*Mathf.PI/3,u=(j+1)*Mathf.PI/3;
                var d=giro*new Vector3(Mathf.Cos(t),0,Mathf.Sin(t));var e=giro*new Vector3(Mathf.Cos(u),0,Mathf.Sin(u));
                Tri(a+d*ra,b+d*rb,b+e*rb);Tri(a+d*ra,b+e*rb,a+e*ra);
            }
        }
        public void Guardar(Transform padre,string nombre,Material material,bool sombras)
        {
            var mesh=new Mesh{name=nombre,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string ruta="Assets/Models/ExteriorSilo/"+nombre+".asset";
            var anterior=AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if(anterior==null)AssetDatabase.CreateAsset(mesh,ruta);
            else{EditorUtility.CopySerialized(mesh,anterior);Object.DestroyImmediate(mesh);mesh=anterior;}
            var go=new GameObject(nombre,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(padre,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=sombras?ShadowCastingMode.On:ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
        }
    }
}
