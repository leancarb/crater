// Compilado por el constructor con el csc de Windows; no pertenece al runtime de Unity.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("CRATER")]
[assembly: AssemblyProduct("CRATER portable")]
[assembly: AssemblyVersion("1.0.0.0")]

internal static class LanzadorCrater
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--prepare-only"))
        {
            try { Preparar(null); return 0; }
            catch (Exception e) { GuardarError(e); return 1; }
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var ventana = new Form { Text = "CRATER", ClientSize = new Size(370, 100),
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false,
            MinimizeBox = false, ControlBox = false, StartPosition = FormStartPosition.CenterScreen };
        var texto = new Label { Text = "Preparando CRATER…", AutoSize = true, Location = new Point(22, 18) };
        var progreso = new ProgressBar { Location = new Point(22, 50), Size = new Size(326, 20), Style = ProgressBarStyle.Marquee };
        ventana.Controls.Add(texto); ventana.Controls.Add(progreso);
        var trabajo = new BackgroundWorker { WorkerReportsProgress = true };
        trabajo.DoWork += delegate(object s, DoWorkEventArgs e) { e.Result = Preparar(p => trabajo.ReportProgress(p)); };
        trabajo.ProgressChanged += delegate(object s, ProgressChangedEventArgs e) {
            texto.Text = "Preparando CRATER… " + e.ProgressPercentage + "%";
            progreso.Style = ProgressBarStyle.Continuous; progreso.Value = e.ProgressPercentage;
        };
        trabajo.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs e) {
            try
            {
                if (e.Error != null) throw e.Error;
                string carpeta = (string)e.Result;
                var proceso = Process.Start(new ProcessStartInfo {
                    FileName = Path.Combine(carpeta, "CRATER.exe"), WorkingDirectory = carpeta,
                    Arguments = string.Join(" ", args.Select(Citar)), UseShellExecute = false
                });
                if (proceso == null) throw new IOException("No se pudo abrir el juego.");
            }
            catch (Exception ex)
            {
                GuardarError(ex); Environment.ExitCode = 1;
                MessageBox.Show("No se pudo abrir CRATER.\n\n" + ex.Message,
                    "CRATER", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { ventana.Close(); }
        };
        ventana.Shown += delegate { trabajo.RunWorkerAsync(); };
        Application.Run(ventana);
        return Environment.ExitCode;
    }

    private static string Preparar(Action<int> avance)
    {
        using (var recurso = Assembly.GetExecutingAssembly().GetManifestResourceStream("CRATER.Payload.zip"))
        {
            if (recurso == null) throw new IOException("Faltan los archivos del juego.");
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(recurso)).Replace("-", "");
            recurso.Position = 0;
            string carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CRATER", "Portable", hash.Substring(0, 32));
            string raiz = Path.GetFullPath(carpeta) + Path.DirectorySeparatorChar;
            using (var mutex = new Mutex(false, "Local\\CRATER_Portable_" + hash.Substring(0, 32)))
            {
                bool tomado = false;
                try
                {
                    try { tomado = mutex.WaitOne(TimeSpan.FromMinutes(2)); }
                    catch (AbandonedMutexException) { tomado = true; }
                    if (!tomado) throw new IOException("Otra copia está preparando el juego. Intentá nuevamente en unos segundos.");
                    Directory.CreateDirectory(carpeta);
                    using (var archivo = new ZipArchive(recurso, ZipArchiveMode.Read, true))
                    {
                        Func<ZipArchiveEntry, string> ruta = entrada => {
                            string destino = Path.GetFullPath(Path.Combine(carpeta, entrada.FullName.Replace('/', Path.DirectorySeparatorChar)));
                            if (!destino.StartsWith(raiz, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Ruta inválida en el paquete.");
                            return destino;
                        };
                        var entradas = archivo.Entries.Where(e => !e.FullName.EndsWith("/")).ToArray();
                        string marca = Path.Combine(carpeta, ".completo");
                        bool completo = File.Exists(marca) && File.ReadAllText(marca) == hash && entradas.All(e => {
                            string p = ruta(e); return File.Exists(p) && new FileInfo(p).Length == e.Length;
                        });
                        if (!completo)
                        {
                            // La marca se escribe al final: una extracción interrumpida se repara al reintentar.
                            if (File.Exists(marca)) File.Delete(marca);
                            int i = 0;
                            foreach (var entrada in entradas)
                            {
                                string destino = ruta(entrada); Directory.CreateDirectory(Path.GetDirectoryName(destino));
                                using (var datos = entrada.Open()) using (var salida = File.Create(destino)) datos.CopyTo(salida);
                                if (avance != null) avance(++i * 100 / entradas.Length);
                            }
                            if (!File.Exists(Path.Combine(carpeta, "CRATER.exe")) || !File.Exists(Path.Combine(carpeta, "UnityPlayer.dll")) || !Directory.Exists(Path.Combine(carpeta, "CRATER_Data")))
                                throw new InvalidDataException("El paquete de CRATER está incompleto.");
                            File.WriteAllText(marca, hash);
                        }
                    }
                    string registro = Path.Combine(Path.GetDirectoryName(carpeta), "ultima-carpeta.txt");
                    File.WriteAllText(registro, carpeta);
                    return carpeta;
                }
                finally { if (tomado) mutex.ReleaseMutex(); }
            }
        }
    }

    private static string Citar(string valor)
    {
        // Reglas de CommandLineToArgvW; no se invoca cmd.exe ni otro shell.
        var s = new System.Text.StringBuilder("\""); int barras = 0;
        foreach (char c in valor)
        {
            if (c == '\\') { barras++; continue; }
            if (c == '"') { s.Append('\\', barras * 2 + 1); s.Append(c); barras = 0; continue; }
            s.Append('\\', barras); barras = 0; s.Append(c);
        }
        s.Append('\\', barras * 2); s.Append('"'); return s.ToString();
    }

    private static void GuardarError(Exception error)
    {
        try
        {
            string carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CRATER", "Portable");
            Directory.CreateDirectory(carpeta); File.WriteAllText(Path.Combine(carpeta, "ultimo-error.txt"), error.ToString());
        }
        catch { /* El mensaje de error en pantalla sigue disponible si no se puede escribir el registro. */ }
    }
}
