using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;

public static partial class ConstructorCrater
{
    /// <summary>Empaqueta un build terminado; también permite volver a comprimirlo sin recompilar.</summary>
    public static string EmpaquetarDemo(BuildTarget destino, string ruta)
    {
        bool mac = destino == BuildTarget.StandaloneOSX;
        if (!mac && destino != BuildTarget.StandaloneWindows64)
            throw new ArgumentException("Sólo se empaquetan demos de Windows y macOS.", nameof(destino));
        string jugador = Path.GetFullPath(ruta);
        string carpeta = Path.GetDirectoryName(jugador);
        if (mac ? !Directory.Exists(Path.Combine(jugador, "Contents/MacOS")) ||
            !Directory.EnumerateFiles(Path.Combine(jugador, "Contents/MacOS")).Any() ||
            !File.Exists(Path.Combine(jugador, "Contents/Info.plist")) :
            !File.Exists(jugador) || !Directory.Exists(Path.Combine(carpeta, "CRATER_Data")) ||
            !File.Exists(Path.Combine(carpeta, "UnityPlayer.dll")))
            throw new IOException("El build está incompleto; no se puede generar el ZIP: " + ruta);

        string zip = Path.Combine(Path.GetDirectoryName(carpeta), mac ? "CRATER_macOS.zip" : "CRATER_Windows.zip");
        string temporal = zip + ".tmp";
        try
        {
            using (var salida = File.Create(temporal))
            using (var archivo = new ZipArchive(salida, ZipArchiveMode.Create))
            {
                string origen = mac ? jugador : carpeta;
                foreach (string fichero in Directory.EnumerateFiles(origen, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
                {
                    string relativo = fichero.Substring(carpeta.Length + 1).Replace('\\', '/');
                    if (relativo.Split('/').Any(p => p.EndsWith("_DoNotShip", StringComparison.OrdinalIgnoreCase) ||
                        p.IndexOf("DontShipItWithYourGame", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        relativo.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)) continue;
                    var entrada = archivo.CreateEntry(relativo, CompressionLevel.Optimal);
                    if (mac)
                    {
                        bool ejecutable = relativo.Contains("/MacOS/") || relativo.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase);
                        entrada.ExternalAttributes = unchecked((int)((ejecutable ? 0x81EDu : 0x81A4u) << 16));
                    }
                    using (var datos = File.OpenRead(fichero))
                    using (var destinoZip = entrada.Open()) datos.CopyTo(destinoZip);
                }
            }
            if (mac) MarcarZipComoUnix(temporal);
            if (File.Exists(zip)) File.Replace(temporal, zip, null);
            else File.Move(temporal, zip);
            return zip;
        }
        finally { if (File.Exists(temporal)) File.Delete(temporal); }
    }

    // En Windows, ZipArchive escribe origen DOS. macOS necesita origen Unix para respetar los permisos.
    static void MarcarZipComoUnix(string ruta)
    {
        using (var flujo = new FileStream(ruta, FileMode.Open, FileAccess.ReadWrite))
        using (var lector = new BinaryReader(flujo, System.Text.Encoding.UTF8, true))
        using (var escritor = new BinaryWriter(flujo, System.Text.Encoding.UTF8, true))
        {
            // El ZIP recién creado no tiene comentario: su registro final ocupa 22 bytes.
            long fin = flujo.Length - 22;
            flujo.Position = fin;
            if (lector.ReadUInt32() != 0x06054b50) throw new InvalidDataException("Final ZIP inválido.");
            flujo.Position = fin + 16;
            long central = lector.ReadUInt32();
            if (central == uint.MaxValue)
            {
                // ZIP64: localizar el registro extendido y su directorio central.
                flujo.Position = fin - 12;
                long registro64 = lector.ReadInt64();
                flujo.Position = registro64 + 48;
                central = lector.ReadInt64();
            }
            flujo.Position = central;
            while (lector.ReadUInt32() == 0x02014b50)
            {
                long inicio = flujo.Position - 4;
                flujo.Position = inicio + 28;
                int longitud = lector.ReadUInt16() + lector.ReadUInt16() + lector.ReadUInt16();
                flujo.Position = inicio + 5;
                escritor.Write((byte)3); // sistema creador Unix, conserva el resto de la cabecera
                flujo.Position = inicio + 46 + longitud;
            }
        }
    }
}
