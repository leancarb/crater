using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class ConstructorCrater
{
    static void AjustarRematesDelPozo()
    {
        foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            bool pared=r.name.StartsWith("Pared_Pozo_") ||
                (r.name=="Muro" && r.transform.parent!=null && r.transform.parent.name.StartsWith("Pared_Escalera_"));
            if(!pared || r.bounds.max.y<=AlturaValle-.18f+.01f)continue;
            var antes=r.bounds;var escala=r.transform.localScale;
            escala.y*=(AlturaValle-.18f-antes.min.y)/antes.size.y;
            r.transform.localScale=escala;
            r.transform.position+=Vector3.up*(antes.min.y-r.bounds.min.y);
        }
        foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(r=>r.name.StartsWith("Sol_Escalera_")||r.name.StartsWith("Luna_Escalera_")))
        {
            int n=int.Parse(r.name.Substring(r.name.LastIndexOf('_')+1));
            float centro=(AlturaValle-.1f)*(1-(2*n+1.5f)/8)+1.7f;
            var p=r.transform.position;p.y=Mathf.Min(centro-.25f,AlturaValle-.95f);
            r.transform.position=p;
        }
    }

    // Mapas calculados una vez al construir. URP/Lit responde a las luces reales;
    // no se agrega geometría, emisión ni trabajo de CPU durante la partida.
    static void AplicarGrabadoMural(Material material,string pintura,float ancho,float alto)
    {
        string normal=pintura.Replace(".png","_Normal.png");
        string relieve=pintura.Replace(".png","_Relieve.png");
        var tinta=new Texture2D(2,2);ImageConversion.LoadImage(tinta,File.ReadAllBytes(pintura));
        var origen=tinta.GetPixels32();int w=tinta.width,h=tinta.height;
        var normales=new Color32[w*h];var alturas=new Color32[w*h];
        float A(int x,int y)=>origen[Mathf.Clamp(y,0,h-1)*w+Mathf.Clamp(x,0,w-1)].a/255f;
        const float profundidad=.015f;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        {
            float dx=(A(x+2,y)-A(x-2,y))*profundidad/(4*ancho/w);
            float dy=(A(x,y+2)-A(x,y-2))*profundidad/(4*alto/h);
            var n=new Vector3(dx,dy,1).normalized;
            normales[y*w+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
            // Interior del trazo hundido; borde y piedra exterior permanecen altos.
            float hundido=1-.65f*A(x,y);
            alturas[y*w+x]=new Color(hundido,hundido,hundido,1);
        }
        void Guardar(string ruta,Color32[] pixels,bool esNormal)
        {
            var textura=new Texture2D(w,h,TextureFormat.RGBA32,false);
            textura.SetPixels32(pixels);textura.Apply();
            var bytes=textura.EncodeToPNG();
            if(!File.Exists(ruta)||!File.ReadAllBytes(ruta).SequenceEqual(bytes))File.WriteAllBytes(ruta,bytes);
            Object.DestroyImmediate(textura);AssetDatabase.ImportAsset(ruta);
            var imp=(TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.textureType=esNormal?TextureImporterType.NormalMap:TextureImporterType.Default;
            imp.sRGBTexture=false;imp.mipmapEnabled=true;imp.wrapMode=TextureWrapMode.Clamp;
            imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
        }
        Guardar(normal,normales,true);Guardar(relieve,alturas,false);Object.DestroyImmediate(tinta);
        material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
        material.SetFloat("_BumpScale",1);material.EnableKeyword("_NORMALMAP");
        var mapa=AssetDatabase.LoadAssetAtPath<Texture2D>(relieve);
        material.SetTexture("_ParallaxMap",mapa);material.SetFloat("_Parallax",.008f);material.EnableKeyword("_PARALLAXMAP");
        material.SetTexture("_OcclusionMap",mapa);material.SetFloat("_OcclusionStrength",.3f);material.EnableKeyword("_OCCLUSIONMAP");
        material.SetColor("_BaseColor",Color.white);
        material.SetFloat("_Smoothness",.04f);material.SetFloat("_Metallic",0);
        material.DisableKeyword("_EMISSION");material.SetColor("_EmissionColor",Color.black);
    }
}
