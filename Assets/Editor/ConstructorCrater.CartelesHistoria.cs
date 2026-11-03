using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static partial class ConstructorCrater
{
    static readonly string[] ImagenesTutorial={
        "Directional Figure Icon Sign","Jump Over the Obstacle Icon","Ancient Artifact Pickup Icon",
        "Lantern Bearer with Light Beam","Lantern Light Builds a Bridge","Lantern Beam Breaking Through Barriers",
        "Lantern Beam Keycap Journey","Lantern Light Builds a Bridge","Lantern Runner Approaches the Bridge",
        "Lantern Puzzle Bridge Diagram","Lantern Off, Eye Watching"};
    static readonly string[] ImagenesHistoria={
        "Andean Traveler at the Sun Station","Andean Observatory Under Sun and Moon","Solar Eclipse Observation Station",
        "Lantern Light Reveals Ancient Crater Glyphs","Eclipse Gateway and Lantern Traveler",
        "Ancient Eclipse Doorway with Lantern Bearer","Andean Canyon Bridge Traveler","Andean Eclipse Observation Station",
        "Andean Temple Lantern and Eclipse","Sun and Moon Stepped Emblem","Ancient Portal Beneath Sun and Moon",
        "Celestial Cycle Over Observation Station"};

    static Rect MedirRecuadroTecla(string archivo)
    {
        var t=new Texture2D(2,2);ImageConversion.LoadImage(t,File.ReadAllBytes(archivo));
        System.Collections.Generic.List<Vector2Int> Tramos(bool horizontal,int fijo,int desde,int hasta)
        {
            var tramos=new System.Collections.Generic.List<Vector2Int>();int inicio=-1;
            for(int i=desde;i<hasta;i++)
            {
                var c=horizontal?t.GetPixel(i,fijo):t.GetPixel(fijo,i);
                bool oscuro=(c.r+c.g+c.b)/3<.31f;
                if(oscuro&&inicio<0)inicio=i;
                if(!oscuro&&inicio>=0){tramos.Add(new Vector2Int(inicio,i-1));inicio=-1;}
            }
            return tramos;
        }
        var x=Tramos(true,t.height/2,(int)(t.width*.04f),(int)(t.width*.32f));
        if(x.Count<2)throw new InvalidOperationException("No se detectó el recuadro vacío: "+archivo);
        int izquierda=x[0].y+4,derecha=x[1].x-4;
        var y=Tramos(false,(izquierda+derecha)/2,(int)(t.height*.15f),(int)(t.height*.85f));
        if(y.Count<2)throw new InvalidOperationException("No se detectó la altura del recuadro: "+archivo);
        var rect=UnityEngine.Rect.MinMaxRect(izquierda/(float)t.width,(y[0].y+4)/(float)t.height,derecha/(float)t.width,(y[1].x-4)/(float)t.height);
        Object.DestroyImmediate(t);return rect;
    }

    static Texture2D ImportarArteNuevo(string original,string destino,bool historia)
    {
        AsegurarCarpeta(Path.GetDirectoryName(destino).Replace('\\','/'));
        if(!File.Exists(original))throw new FileNotFoundException("Falta imagen de arte",original);
        bool convertir=historia;
        var importador=AssetImporter.GetAtPath(destino) as TextureImporter;
        bool generar=!File.Exists(destino)||File.GetLastWriteTimeUtc(original)>File.GetLastWriteTimeUtc(destino)||
            (historia && (importador==null||importador.userData!="CRATER_HISTORIA_1"));
        if(generar)
        {
            if(!convertir)File.Copy(original,destino,true);
            else
            {
                var fuente=new Texture2D(2,2);ImageConversion.LoadImage(fuente,File.ReadAllBytes(original));
                var pixeles=fuente.GetPixels();
                for(int i=0;i<pixeles.Length;i++)
                {
                    var c=pixeles[i];
                    // El fondo casi neutro #1C1A1D se separa de la tinta cálida con borde suave.
                    float fondo=Mathf.SmoothStep(0,1,(Mathf.Max(c.r,c.g,c.b)-.16f)/.13f);
                    float tinta=Mathf.SmoothStep(0,1,(c.r-c.b-.01f)/.06f);
                    c.a*=fondo*tinta;pixeles[i]=c;
                }
                fuente.SetPixels(pixeles);fuente.Apply();File.WriteAllBytes(destino,fuente.EncodeToPNG());Object.DestroyImmediate(fuente);
            }
            AssetDatabase.ImportAsset(destino);
        }
        importador=(TextureImporter)AssetImporter.GetAtPath(destino);
        importador.sRGBTexture=true;importador.mipmapEnabled=true;importador.npotScale=TextureImporterNPOTScale.None;
        importador.wrapMode=TextureWrapMode.Clamp;importador.filterMode=FilterMode.Trilinear;importador.maxTextureSize=2048;
        importador.textureCompression=TextureImporterCompression.CompressedHQ;
        importador.alphaSource=historia?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
        importador.alphaIsTransparency=historia;importador.mipmapEnabled=true;
        if(historia){importador.userData="CRATER_HISTORIA_1";importador.mipMapsPreserveCoverage=true;importador.alphaTestReferenceValue=.15f;}
        importador.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(destino);
    }
    static Transform ImagenEnPared(Transform padre,string nombre,Vector3 pared,Vector3 normal,Vector2 medida,Material material)
    {
        var raiz=Grupo(padre,nombre);raiz.SetPositionAndRotation(pared+normal*.12f,Quaternion.LookRotation(-normal));
        Grupo(raiz,"Narrativa");
        var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="PinturaMural";go.transform.SetParent(raiz,false);
        go.transform.localScale=new Vector3(medida.x,medida.y,1);Object.DestroyImmediate(go.GetComponent<Collider>());
        var r=go.GetComponent<Renderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return raiz;
    }
    static Transform Cartel(Kit k,Transform padre,string nombre,int accion,Vector3 posicion,Vector3 normal,float piso)
    {
        const float ancho=1.8f,alto=1.35f;
        var textura=ImportarArteNuevo("images/tutorial/"+ImagenesTutorial[accion-1]+".png",$"Assets/Art/Carteles/{accion:00}.png",false);
        var material=Opaco($"Cartel_{accion:00}",Color.white,.05f);material.SetTexture("_BaseMap",textura);EditorUtility.SetDirty(material);
        var marco=MateLiso("Marco_Cartel","#332B22",.05f);
        var raiz=Grupo(padre,nombre);raiz.SetPositionAndRotation(posicion,Quaternion.LookRotation(-normal));
        CajaDePlaca(raiz,"Losa_Cartel",new Vector3(0,0,.02f),new Vector3(ancho+.08f,alto+.08f,.06f),marco,true);
        var imagen=GameObject.CreatePrimitive(PrimitiveType.Quad);imagen.name="Imagen_Cartel";imagen.transform.SetParent(raiz,false);
        imagen.transform.localPosition=new Vector3(0,0,-.016f);imagen.transform.localScale=new Vector3(ancho,alto,1);
        Object.DestroyImmediate(imagen.GetComponent<Collider>());imagen.GetComponent<Renderer>().sharedMaterial=material;
        foreach(float lado in new[]{-1f,1f})
        {
            CajaDePlaca(raiz,"Marco_Vertical",new Vector3(lado*(ancho*.5f+.02f),0,-.02f),new Vector3(.04f,alto+.08f,.05f),marco);
            CajaDePlaca(raiz,"Marco_Horizontal",new Vector3(0,lado*(alto*.5f+.02f),-.02f),new Vector3(ancho+.08f,.04f,.05f),marco);
        }
        float pie=Mathf.Max(.05f,posicion.y-piso-alto*.5f);
        CajaDePlaca(raiz,"Soporte_Cartel",new Vector3(0,-alto*.5f-pie*.5f,.04f),new Vector3(.12f,pie,.12f),k.piedra,true);
        var foco=Grupo(raiz,"Luz_Cartel");foco.localPosition=new Vector3(0,1,-1.6f);foco.LookAt(raiz.position);
        var luz=foco.gameObject.AddComponent<Light>();luz.type=LightType.Spot;luz.color=Hex("#F4F1EA");
        luz.intensity=6;luz.range=2.4f;luz.spotAngle=54;luz.innerSpotAngle=38;luz.shadows=LightShadows.None;
        var celda=MedirRecuadroTecla("images/tutorial/"+ImagenesTutorial[accion-1]+".png");
        var glifo=Grupo(raiz,"Glifo_Tecla");glifo.localPosition=new Vector3((celda.center.x-.5f)*ancho,(celda.center.y-.5f)*alto,-.025f);
        var texto=glifo.gameObject.AddComponent<TextMesh>();var fuente=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fuente.RequestCharactersInTexture("WASD ESPACIO X F E B SHIFT SOLTAR CLIC IZQ. DER. RT/L1R2→×○□",64);
        texto.font=fuente;texto.fontSize=64;texto.characterSize=.04f;texto.anchor=TextAnchor.MiddleCenter;
        texto.alignment=TextAlignment.Center;texto.color=Hex("#292520");texto.lineSpacing=.85f;
        var tinta=new Material(Shader.Find("Crater/TextoMundo"));tinta.name="Glifo_Cartel";tinta.mainTexture=fuente.material.mainTexture;
        tinta.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        // Asset compartido: no se crean materiales por frame ni texto que atraviese paredes.
        const string ruta="Assets/Materials/Glifo_Cartel.mat";
        var existente=AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if(existente==null){AssetDatabase.CreateAsset(tinta,ruta);existente=tinta;}else{EditorUtility.CopySerialized(tinta,existente);Object.DestroyImmediate(tinta);}
        glifo.GetComponent<Renderer>().sharedMaterial=existente;
        float anchoCelda=ancho*celda.width*.90f,altoCelda=alto*celda.height*.90f;
        var icono=Grupo(raiz,"Icono_Xbox");icono.localPosition=glifo.localPosition+Vector3.forward*.002f;
        var vertices=new System.Collections.Generic.List<Vector3>();
        if(accion==2||accion==3||accion==4||accion==11)Disco(vertices,0,0,Mathf.Min(anchoCelda,altoCelda)*.49f,32);
        else
        {
            float w=anchoCelda*.49f,h=Mathf.Min(altoCelda*.49f,anchoCelda*(accion==10?.48f:.31f)),r=.025f;
            var puntos=new System.Collections.Generic.List<Vector2>();
            for(int esquina=0;esquina<4;esquina++)for(int i=0;i<=8;i++)
            {
                float a=(esquina*90+i*90f/8)*Mathf.Deg2Rad;
                var centro=new Vector2(esquina==0||esquina==3?w-r:-w+r,esquina<2?h-r:-h+r);
                puntos.Add(centro+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);
            }
            for(int i=0;i<puntos.Count;i++)Tri(vertices,Vector2.zero,puntos[i],puntos[(i+1)%puntos.Count]);
        }
        int caras=vertices.Count;for(int i=0;i<caras;i+=3){vertices.Add(vertices[i]);vertices.Add(vertices[i+2]);vertices.Add(vertices[i+1]);}
        icono.gameObject.AddComponent<MeshFilter>().sharedMesh=GuardarMalla(CrearMalla("Icono_Xbox_"+accion,vertices,vertices.Select(_=>Vector2.zero).ToList()));
        var renderer=icono.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=MateLiso("Fondo_Boton_Xbox","#20242A",.05f);renderer.shadowCastingMode=ShadowCastingMode.Off;
        icono.gameObject.SetActive(false);
        var etiqueta=glifo.gameObject.AddComponent<CartelTutorial>();etiqueta.accion=accion;etiqueta.anchoCelda=anchoCelda;etiqueta.altoCelda=altoCelda;etiqueta.iconoXbox=icono.gameObject;
        texto.text=accion==1?"WASD":accion==3?"E":"";
        return raiz;
    }
    static void AplicarCartelesHistoria(Kit k,Transform nivel)
    {
        var viejos=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(t=>t.name.StartsWith("Control_")||t.name.StartsWith("Ensenanza_")||t.name=="Mural_Ancla_Puerta"||t.name=="Mural_Anclas_Del_Techo").ToArray();
        var posiciones=viejos.ToDictionary(t=>t.name,t=>(t.position,-t.forward));
        foreach(var t in viejos)if(t!=null)Object.DestroyImmediate(t.gameObject);
        foreach(string n in new[]{"Murales_Runicos","Carteles_Tutorial","Historia_Nueva"})
        {var anterior=nivel.Find(n);if(anterior!=null)Object.DestroyImmediate(anterior.gameObject);}
        var carteles=Grupo(nivel,"Carteles_Tutorial");var historia=Grupo(nivel,"Historia_Nueva");
        var exterior=nivel.Find("06_Capilla");
        void EnPose(int accion,string viejo,string nuevo)
        {
            var pose=posiciones[viejo];var p=pose.Item1;p.y=1.5f;
            if(accion==1){var local=exterior.InverseTransformPoint(p);p.y=exterior.TransformPoint(new Vector3(local.x,AlturaInicioElevado(local.x,local.z),local.z)).y+1.5f;}
            Cartel(k,carteles,nuevo,accion,p,pose.Item2,p.y-1.5f);
        }
        EnPose(1,"Control_Mover","Cartel_01_Mover");
        var salto=new Vector3(-8,AlturaInicioElevado(-8,181.7f)+1.5f,181.7f);
        Cartel(k,carteles,"Cartel_02_Saltar",2,exterior.TransformPoint(salto),exterior.TransformDirection(Vector3.right),exterior.TransformPoint(salto).y-1.5f);
        var centro=EspacioCrater.Punto(CentroRotonda);
        Cartel(k,carteles,"Cartel_03_RecogerFarol",3,new Vector3(-1.7f,.3f,centro.z-.6f),Vector3.back,-1.2f);
        Cartel(k,carteles,"Cartel_04_Farol",4,new Vector3(1.7f,.3f,centro.z-.6f),Vector3.back,-1.2f);
        var piedra=BuscarObjetoDocente("Pieza_exterior_mirador");
        Cartel(k,carteles,"Cartel_03_RecogerPiedra",3,new Vector3(piedra.position.x-2.2f,1.5f,piedra.position.z-.5f),Vector3.back,0);
        EnPose(5,"Control_FiltroSol","Cartel_05_SOL");EnPose(6,"Control_FiltroLuna","Cartel_06_LUNA");
        var luna=posiciones["Control_FiltroLuna"];
        var normal=luna.Item2;
        var blanca=luna.Item1+Vector3.Cross(Vector3.up,normal)*2.4f;blanca.y=1.5f;
        Cartel(k,carteles,"Cartel_07_Blanca",7,blanca,normal,0);
        EnPose(8,"Ensenanza_Dos_Anclas","Cartel_08_DosAnclas");
        Cartel(k,carteles,"Cartel_09_Correr",9,EspacioCrater.Punto(new Vector3(-2.2f,1.5f,44.5f)),Vector3.back,0);
        EnPose(10,"Ensenanza_Cruce_Combinacion","Cartel_10_Combinar");
        EnPose(11,"Ensenanza_Cresta_Reflejo","Cartel_11_Cresta");
        for(int i=1;i<=12;i++)
        {
            var textura=ImportarArteNuevo("images/historia/"+ImagenesHistoria[i-1]+".png",$"Assets/Art/Murals/Historia/{i:00}.png",true);
            var m=Opaco($"Historia_{i:00}",Color.white,.05f);m.SetTexture("_BaseMap",textura);
            m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.15f);m.EnableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=(int)RenderQueue.AlphaTest;EditorUtility.SetDirty(m);
            if(i<=8)
            {
                var viejo=BuscarObjetoDocente("Lore_"+i);var n=-viejo.forward;
                var plano=viejo.Find("Plano_Pared");var p=plano!=null?plano.position:viejo.position-n*.12f;
                Object.DestroyImmediate(viejo.gameObject);ImagenEnPared(historia,"Lore_"+i,p,n,new Vector2(2.2f,1.1f),m);
            }
            else if(i==9)
            {
                var pared=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Pared_Rotonda_"))
                    .OrderBy(t=>(t.position-EspacioCrater.Punto(new Vector3(9,3.5f,23.5f))).sqrMagnitude).First();
                var n=(centro-pared.position);n.y=0;n.Normalize();var p=pared.position+n*Mathf.Abs(pared.lossyScale.z)*.5f;p.y=3.5f;
                float w=Mathf.Min(3.4f,pared.GetComponent<MeshFilter>().sharedMesh.bounds.size.x*Mathf.Abs(pared.lossyScale.x)-.1f);
                ImagenEnPared(historia,"Historia_09_Farol",p,n,new Vector2(w,w*.5f),m);
            }
            else
            {
                string n=i==10?"Dintel_Umbral":i==11?"Dintel_Sellos":"Muro_Corredor_Izq";
                var pared=BuscarObjetoDocente(n);var b=pared.GetComponent<Renderer>().bounds;
                var dir=i==10?Vector3.forward:i==11?Vector3.back:Vector3.right;
                var p=b.center+dir*(Mathf.Abs(dir.x)*b.extents.x+Mathf.Abs(dir.z)*b.extents.z);
                float w=i==12?5:Mathf.Min(b.size.x-.15f,(b.size.y-.15f)*2);
                if(i==12)p.y=3.9f;
                ImagenEnPared(historia,$"Historia_{i:00}",p,dir,new Vector2(w,w*.5f),m);
            }
        }
        var vis=Object.FindFirstObjectByType<VisibilidadExterior>();
        var placasExteriores=carteles.Cast<Transform>().Where(t=>t.name=="Cartel_01_Mover"||t.name=="Cartel_02_Saltar")
            .SelectMany(t=>t.GetComponentsInChildren<Renderer>(true));
        vis.paisaje=vis.paisaje.Where(r=>r!=null).Concat(placasExteriores).Distinct().ToArray();
        var llegada=Object.FindFirstObjectByType<LlegadaRotonda>();llegada.luces=llegada.luces.Where(l=>l!=null).ToArray();
        // Retirar sólo buffers de dibujos reemplazados que ya no están referenciados por la escena.
        var usadas=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(f=>f.sharedMesh).ToHashSet();
        foreach(var m in AssetDatabase.LoadAllAssetRepresentationsAtPath(RutaMallasFacetadas).OfType<Mesh>())
            if(!usadas.Contains(m)&&(m.name.StartsWith("Relieve_Instruccion_")||m.name.StartsWith("NarrativaPNG_")))
            {AssetDatabase.RemoveObjectFromAsset(m);Object.DestroyImmediate(m,true);}
    }
}
