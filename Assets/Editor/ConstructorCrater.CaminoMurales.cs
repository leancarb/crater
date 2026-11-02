using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class ConstructorCrater
{
    // Se ejecuta después de ampliar el subsuelo: las pinturas conservan medidas reales.
    static void AplicarRevisionCaminoMurales(Kit k, Transform nivel, Transform inicio)
    {
        var exterior = nivel.Find("06_Capilla");
        var borde = exterior.Find("Crater_Valle");
        if (borde != null)
            foreach (Transform t in borde.Cast<Transform>().ToArray())
                if (t.name.StartsWith("Borde_")) Object.DestroyImmediate(t.gameObject);
        // El prólogo también guarda los bordes animados. Quitar referencias destruidas.
        var prologo = Object.FindFirstObjectByType<PrologoCapilla>();
        var so = new SerializedObject(prologo);
        var bordes = so.FindProperty("bordes");
        if (bordes != null)
            for (int i = bordes.arraySize - 1; i >= 0; i--)
                if (bordes.GetArrayElementAtIndex(i).objectReferenceValue == null)
                { bordes.GetArrayElementAtIndex(i).objectReferenceValue = null; bordes.DeleteArrayElementAtIndex(i); }
        so.ApplyModifiedPropertiesWithoutUndo();
        if (borde != null) AsignarLista(prologo,"lucesApertura",LucesDelBorde(borde));
        var zona = BuscarObjetoDocente("Zona_Puerta_Crater");
        zona.position = new Vector3(CentroPozo.x, 3f, -35.5f);
        zona.GetComponent<BoxCollider>().size = new Vector3(16, 6, 8);
        var sol = BuscarObjetoDocente("Sol_Epilogo");
        sol.rotation = exterior.rotation * Quaternion.Euler(9, 0, 0);
        AjustarRematesDelPozo();
        DibujarSenderoInicial(k, inicio);
        ConstruirMuralesRunicos(nivel);
        var farol = Object.FindFirstObjectByType<LinternaController>();
        farol.demoraDeCambio = .35f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(farol);
        var prefab = PrefabUtility.LoadPrefabContents(CarpetaPrefabs + "Jugador.prefab");
        try
        {
            prefab.GetComponentInChildren<LinternaController>(true).demoraDeCambio = .35f;
            PrefabUtility.SaveAsPrefabAsset(prefab, CarpetaPrefabs + "Jugador.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }

    static void DibujarSenderoInicial(Kit k, Transform padre)
    {
        var puntos = new List<Vector2>(CaminoBajadaInicial);
        for (float z = 145; z >= 80; z -= 5)
            puntos.Add(new Vector2(3 * Mathf.Sin((z - 80) * Mathf.PI / 70), z));
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        Vector3 P(Vector2 p) => new Vector3(p.x, AlturaInicioElevado(p.x, p.y) + .025f, p.y);
        for (int i = 1; i < puntos.Count; i++)
        {
            var a = puntos[i - 1]; var b = puntos[i];
            var normal = new Vector2(-(b-a).y, (b-a).x).normalized;
            int pasos = Mathf.CeilToInt(Vector2.Distance(a, b) / .6f);
            for (int j = 0; j < pasos; j++)
            {
                var p = Vector2.Lerp(a, b, j/(float)pasos);
                var q = Vector2.Lerp(a, b, (j+1)/(float)pasos);
                var aa=P(p-normal*1.8f); var bb=P(q-normal*1.8f);
                var cc=P(q+normal*1.8f); var dd=P(p+normal*1.8f);
                Triangulo(v,uv,aa,cc,bb,PixelDeFaceta((aa+bb+cc)/3));
                Triangulo(v,uv,aa,dd,cc,PixelDeFaceta((aa+cc+dd)/3));
            }
        }
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tierra_Sendero.mat");
        if (material == null)
        { material = new Material(k.tierra); AssetDatabase.CreateAsset(material,"Assets/Materials/Tierra_Sendero.mat"); }
        material.SetColor("_BaseColor", k.tierra.GetColor("_BaseColor") * 1.35f);
        material.DisableKeyword("_EMISSION"); material.SetColor("_EmissionColor",Color.black);
        EditorUtility.SetDirty(material);
        var go = new GameObject("Sendero_Tierra_Clara"); go.transform.SetParent(padre,false);
        go.AddComponent<MeshFilter>().sharedMesh = MallaPersistenteInicio("Sendero_Tierra_Clara",v,uv);
        go.AddComponent<MeshRenderer>().sharedMaterial = material; Estatico(go);
    }

    static void ConstruirMuralesRunicos(Transform nivel)
    {
        var viejo = nivel.Find("Murales_Runicos");
        if(viejo != null) Object.DestroyImmediate(viejo.gameObject);
        var grupo = Grupo(nivel,"Murales_Runicos");
        // Cada imagen ocupa el paño completo. LUNA deja libre el cielo de estrellas del norte.
        Mural("Entrada y descenso al crater","Masa_Norte_Izq",Vector3.back,2.7f,5f);
        Mural("Umbral descubrimiento del farol","Muro_Umbral_Izq",Vector3.right,2.2f,6f);
        Mural("Rotonda dos caminos","Dintel_Umbral",Vector3.forward,5.3f,5f);
        Mural("Ala SOL construir un paso","Muro_O2_Norte",Vector3.back,2.7f,6f);
        Mural("Ala LUNA atravesar lo sólido","Muro_E2_Oeste",Vector3.right,2.7f,6f);
        Mural("Cruce alternar las dos luces","Muro_Cruce_Der",Vector3.left,2.8f,6f);
        Mural("Cresta descubrir en la oscuridad","Muro_Corredor_Der",Vector3.left,4.1f,4f);
        Mural("Mystical Celestial Portal Schematic","Dintel_Sellos",Vector3.back,5.3f,3.5f);
        Mural("Regreso el ciclo transformado","Muro_Corredor_Izq",Vector3.right,4.1f,4f);
        void Mural(string imagen,string pared,Vector3 normal,float altura,float ancho)
        {
            var soporte=BuscarObjetoDocente(pared);
            if(soporte==null)throw new System.InvalidOperationException("Falta pared para mural: "+pared);
            if(normal==Vector3.zero)normal=soporte.forward;
            var bounds=soporte.GetComponent<Renderer>().bounds;
            // El paño visible comienza en el piso, no en el cimiento de la pared.
            float piso=Mathf.Max(0,bounds.min.y),techo=bounds.max.y;
            float margen=.08f;
            ancho=(Mathf.Abs(normal.x)>.5f?bounds.size.z:bounds.size.x)-margen*2;
            float alto=techo-piso-margen*2;
            Vector3 p=bounds.center;
            p.y=(piso+techo)*.5f;
            p+=normal*(Mathf.Abs(normal.x)*bounds.extents.x+Mathf.Abs(normal.z)*bounds.extents.z+.025f);
            DespejarParedDeMural(soporte,normal,p,ancho,alto);
            var material=MaterialMuralRunico(imagen,ref ancho,ref alto,out _);
            // Tallado blanco mate; conserva las normales y las sombras de la incisión.
            material.SetColor("_BaseColor",Color.white);
            EditorUtility.SetDirty(material);
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Mural_Runico_"+imagen;
            go.transform.SetParent(grupo,false);
            go.transform.SetPositionAndRotation(p,Quaternion.LookRotation(-normal));
            go.transform.localScale=new Vector3(ancho,alto,1);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var r=go.GetComponent<Renderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
            Estatico(go);
        }
    }

    static void DespejarParedDeMural(Transform pared,Vector3 normal,Vector3 centro,float ancho,float alto)
    {
        Vector3 horizontal=Vector3.Cross(Vector3.up,normal);
        var retirar=new HashSet<GameObject>();
        bool Decoracion(string n) => n.StartsWith("Friso_") || n.StartsWith("Mural_") ||
            n.StartsWith("Sol_") || n.StartsWith("Luna_") || n.StartsWith("Halo_") ||
            n.StartsWith("Estrella") || n.StartsWith("Control_") || n.StartsWith("Ensenanza_") || n.StartsWith("Apagar_");
        foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(r.transform==pared || r.transform.IsChildOf(pared) || r.name.StartsWith("Mural_Runico_"))continue;
            var b=r.bounds;var d=b.center-centro;
            float espesor=Mathf.Abs(normal.x)*b.size.x+Mathf.Abs(normal.z)*b.size.z;
            if(espesor>1.5f || Mathf.Abs(Vector3.Dot(d,normal))>1.1f ||
                Mathf.Abs(Vector3.Dot(d,horizontal))>ancho*.5f+.1f || Mathf.Abs(d.y)>alto*.5f+.1f)continue;
            Transform candidato=null;
            for(var t=r.transform;t!=null;t=t.parent)
            {
                if(t.name.StartsWith("Mural_Runico_")){candidato=null;break;}
                if(Decoracion(t.name))candidato=t;
            }
            if(candidato==null || candidato.GetComponentInParent<ReceptorDeLuz>()!=null ||
                candidato.GetComponentInParent<TestigoDeSello>()!=null || candidato.GetComponentInParent<Compuerta>()!=null)continue;
            if(candidato.Find("PlacaInstruccion")!=null){candidato.position+=normal*1.3f;continue;}
            retirar.Add(candidato.gameObject);
        }
        foreach(var go in retirar)if(go!=null)
        {
            Debug.Log("[CRATER] Despejar "+pared.name+": "+go.name);
            Object.DestroyImmediate(go);
        }
    }

    static Material MaterialMuralRunico(string nombre,ref float ancho,ref float alto,out float proporcion)
    {
        const string carpeta="Assets/Art/Murals/Runicos";
        AsegurarCarpeta(carpeta);
        string original="images/murals_final/"+nombre+".png";
        string destino=carpeta+"/"+nombre+".png";
        // Derivado de importación: se conserva intacto el PNG del artista.
        var fuente=new Texture2D(2,2); ImageConversion.LoadImage(fuente,File.ReadAllBytes(original));
        proporcion=fuente.width/(float)fuente.height;
        // Encajar la imagen completa sin deformar ni recortar. El relieve usa las medidas finales.
        ancho=Mathf.Min(ancho,alto*proporcion);
        alto=ancho/proporcion;
        if(!File.Exists(destino)||File.GetLastWriteTimeUtc(original)>File.GetLastWriteTimeUtc(destino)||AssetImporter.GetAtPath(destino)?.userData!="CRATER_TINTA_BLANCA_1")
        {
            int w=Mathf.Min(2048,fuente.width),h=Mathf.RoundToInt(w/proporcion);
            var tinta=new Texture2D(w,h,TextureFormat.RGBA32,false);
            var pixels=new Color[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                var c=fuente.GetPixelBilinear((x+.5f)/w,(y+.5f)/h);
                float alpha=Mathf.SmoothStep(0,1,(Mathf.Max(c.r,c.g,c.b)-.08f)/.55f);
                pixels[y*w+x]=new Color(1f,1f,1f,alpha);
            }
            tinta.SetPixels(pixels);tinta.Apply();File.WriteAllBytes(destino,tinta.EncodeToPNG());
            Object.DestroyImmediate(tinta);AssetDatabase.ImportAsset(destino);
        }
        Object.DestroyImmediate(fuente);
        var imp=(TextureImporter)AssetImporter.GetAtPath(destino);
        imp.userData="CRATER_TINTA_BLANCA_1";
        imp.sRGBTexture=true;imp.alphaSource=TextureImporterAlphaSource.FromInput;imp.alphaIsTransparency=true;
        imp.mipmapEnabled=true;imp.mipMapsPreserveCoverage=true;imp.alphaTestReferenceValue=.2f;
        imp.wrapMode=TextureWrapMode.Clamp;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.CompressedHQ;
        imp.SaveAndReimport();
        var m=Opaco("Mural_Runico_"+nombre,Color.white,.05f);
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(destino));
        m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.2f);m.EnableKeyword("_ALPHATEST_ON");
        m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=(int)RenderQueue.AlphaTest;
        m.SetFloat("_Metallic",0);m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);
        AplicarGrabadoMural(m,destino,ancho,alto);
        EditorUtility.SetDirty(m);return m;
    }
}
