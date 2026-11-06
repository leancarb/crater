using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static partial class ConstructorCrater
{
    static Texture2D ConfigurarIconoAplicacion()
    {
        const string origen="images/icon.jpg", destino="Assets/Art/Iconos/CRATER.jpg";
        if(!File.Exists(origen))throw new FileNotFoundException("Falta el ícono del juego: "+origen);
        AsegurarCarpeta("Assets/Art/Iconos");
        var datos=File.ReadAllBytes(origen);
        if(!File.Exists(destino)||!datos.SequenceEqual(File.ReadAllBytes(destino)))File.WriteAllBytes(destino,datos);
        AssetDatabase.ImportAsset(destino,ImportAssetOptions.ForceSynchronousImport);
        var importador=(TextureImporter)AssetImporter.GetAtPath(destino);
        importador.textureType=TextureImporterType.Default;importador.mipmapEnabled=false;
        importador.textureCompression=TextureImporterCompression.Uncompressed;
        importador.npotScale=TextureImporterNPOTScale.None;importador.maxTextureSize=2048;importador.isReadable=true;
        importador.SaveAndReimport();
        var textura=AssetDatabase.LoadAssetAtPath<Texture2D>(destino);
        int[] tamanios=PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone,IconKind.Any);
        if(tamanios.Length==0)throw new InvalidOperationException("Unity no informó tamaños de ícono para Standalone.");
        PlayerSettings.SetIcons(NamedBuildTarget.Standalone,tamanios.Select(_=>textura).ToArray(),IconKind.Any);
        AssetDatabase.SaveAssets();
        return textura;
    }

    static string CrearIconoWindows()
    {
        var original=ConfigurarIconoAplicacion();
        int[] tamanios={16,24,32,48,64,128,256};var imagenes=new byte[tamanios.Length][];
        for(int i=0;i<tamanios.Length;i++)
        {
            int n=tamanios[i];var textura=new Texture2D(n,n,TextureFormat.RGBA32,false);
            try
            {
                var colores=new Color[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)colores[y*n+x]=original.GetPixelBilinear((x+.5f)/n,(y+.5f)/n);
                textura.SetPixels(colores);textura.Apply();imagenes[i]=textura.EncodeToPNG();
            }
            finally{UnityEngine.Object.DestroyImmediate(textura);}
        }
        string ruta=Path.GetFullPath("Builds/CRATER_Icono.ico");Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        using(var archivo=new BinaryWriter(File.Create(ruta)))
        {
            archivo.Write((ushort)0);archivo.Write((ushort)1);archivo.Write((ushort)tamanios.Length);
            uint offset=(uint)(6+16*tamanios.Length);
            for(int i=0;i<tamanios.Length;i++)
            {
                archivo.Write((byte)(tamanios[i]==256?0:tamanios[i]));archivo.Write((byte)(tamanios[i]==256?0:tamanios[i]));
                archivo.Write((byte)0);archivo.Write((byte)0);archivo.Write((ushort)1);archivo.Write((ushort)32);
                archivo.Write((uint)imagenes[i].Length);archivo.Write(offset);offset+=(uint)imagenes[i].Length;
            }
            foreach(var imagen in imagenes)archivo.Write(imagen);
        }
        return ruta;
    }
}
