using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CraterRevisionTests
{
    [Test]
    public void ElProyectoDePlaytestSuperaElValidador()
    {
        var validador = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ValidarProyectoCrater")).First(t => t != null);
        Assert.That((int)validador.GetMethod("Validar").Invoke(null, new object[] { false }), Is.Zero);
    }

    [Test]
    public void ElRefuerzoDeCompuertaFavoreceLosGravesSinRecortarLaSenal()
    {
        var go = new GameObject("PruebaGraves");
        try
        {
            var filtro = go.AddComponent<GravesCompuerta>();
            double Energia(float frecuencia)
            {
                filtro.Reiniciar(44100);
                var muestras = new float[44100];
                for (int i = 0; i < muestras.Length; i++) muestras[i] = .1f * Mathf.Sin(2f * Mathf.PI * frecuencia * i / 44100f);
                filtro.Procesar(muestras, 1);
                Assert.That(muestras.Max(v => Mathf.Abs(v)), Is.LessThan(.2f));
                return muestras.Skip(4410).Average(v => (double)v * v);
            }
            Assert.That(Energia(50f), Is.GreaterThan(Energia(2000f) * 1.8));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void RegenerarAudioConservaTodosLosWavExistentes()
    {
        string Hash(string ruta)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(ruta)));
        }
        var archivos = Directory.GetFiles("Assets/Audio", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) || p.Replace('\\','/').StartsWith("Assets/Audio/Grabado/"))
            .ToDictionary(p => p, Hash);
        var generador = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("GeneradorAudioCrater")).First(t => t != null);
        generador.GetMethod("GenerarTodo").Invoke(null, null);
        foreach (var par in archivos) Assert.That(Hash(par.Key), Is.EqualTo(par.Value), par.Key);
    }

    [Test]
    public void LosMaterialesGeneradosRespetanLaEspecificacion()
    {
        var azul = new Color(78f / 255f, 140f / 255f, 1f);
        var profundo = new Color(42f / 255f, 79f / 255f, 168f / 255f);
        var reja = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RejaBasalto.mat");
        var tallado = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TalladoLuna.mat");
        Assert.That(reja, Is.Not.Null);
        Assert.That(tallado, Is.Not.Null);
        Assert.That(Vector4.Distance(reja.GetColor("_BaseColor"), new Color(azul.r, azul.g, azul.b, .55f)), Is.LessThan(.0001f));
        Assert.That(Vector4.Distance(reja.GetColor("_EmissionColor"), profundo * 1.5f), Is.LessThan(.0001f));
        Assert.That(Vector4.Distance(tallado.GetColor("_BaseColor"), profundo), Is.LessThan(.0001f));
        Assert.That(Vector4.Distance(tallado.GetColor("_EmissionColor"), azul * 1.5f), Is.LessThan(.0001f));
        var encuadre = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Pintura_Encuadre.mat");
        Assert.That(encuadre.IsKeywordEnabled("_EMISSION"), Is.True);
        Assert.That(encuadre.GetColor("_EmissionColor").maxColorComponent, Is.Zero);
    }

    [Test]
    public void LosOchoMuralesGuardadosTienenPNGQuadYUVSinDeformar()
    {
        var escena = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/CraterVerticalSlice.unity");
        bool cargada = escena.IsValid() && escena.isLoaded;
        if (!cargada) escena = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/CraterVerticalSlice.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var objetos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
            var validador = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ValidarProyectoCrater")).First(t => t != null);
            var problemas = new System.Collections.Generic.List<string>();
            validador.GetMethod("ValidarMurales", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { objetos, problemas });
            Assert.That(problemas, Is.Empty, string.Join("\n", problemas));
            for (int i = 1; i <= 8; i++)
            {
                var losa = objetos.Single(o => o.name == "Lore_" + i).transform;
                var cara = losa.Find("PinturaMural");
                var malla = cara.GetComponent<MeshFilter>().sharedMesh;
                var textura = cara.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap");
                Assert.That(textura.width, Is.EqualTo(1728));
                Assert.That(textura.height, Is.EqualTo(1152));
                Assert.That(malla.bounds.size.x, Is.EqualTo(2.1f).Within(.0001f));
                Assert.That(malla.bounds.size.y, Is.EqualTo(1.4f).Within(.0001f));
                CollectionAssert.AreEqual(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right }, malla.uv);
                Assert.That(malla.normals.All(n => Vector3.Dot(n, Vector3.back) > .999f), Is.True);
                Assert.That(cara.GetComponent<Collider>(), Is.Null);
                Assert.That(Vector3.Dot(losa.forward, i <= 4 ? Vector3.left : Vector3.right), Is.GreaterThan(.999f));
            }
            Assert.That(objetos.Single(o => o.name == "Luz_Pasaje").GetComponent<Light>().intensity, Is.EqualTo(14f));
            // AmpliarSubsuelo compensa la distancia multiplicando intensidad por escala².
            float compensacion = EspacioCrater.Escala * EspacioCrater.Escala;
            Assert.That(objetos.Single(o => o.name == "Luz_Murales_Oeste").GetComponent<Light>().intensity, Is.EqualTo(10f * compensacion));
            Assert.That(objetos.Single(o => o.name == "Luz_Murales_Este").GetComponent<Light>().intensity, Is.EqualTo(12f * compensacion));
        }
        finally { if (!cargada) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(escena, true); }
    }

    [Test]
    public void LasSombrasSeLimitanAlEntornoActivo()
    {
        var raiz = new GameObject("PruebaSombras");
        try
        {
            var presupuesto = raiz.AddComponent<PresupuestoSombras>();
            presupuesto.luces = new Light[8];
            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject("Luz" + i);
                go.transform.SetParent(raiz.transform);
                go.transform.position = Vector3.right * (i + 1);
                var luz = go.AddComponent<Light>();
                luz.type = LightType.Point;
                luz.range = 20;
                luz.intensity = 1;
                luz.shadows = LightShadows.Soft;
                presupuesto.luces[i] = luz;
            }
            presupuesto.luces[0].enabled = false;
            presupuesto.Actualizar(Vector3.zero);
            Assert.That(presupuesto.luces.Count(l => l.shadows != LightShadows.None), Is.EqualTo(3));
            Assert.That(presupuesto.luces[0].shadows, Is.EqualTo(LightShadows.None));
            presupuesto.Actualizar(Vector3.right * 10);
            Assert.That(presupuesto.luces[7].shadows, Is.EqualTo(LightShadows.Soft));
            Assert.That(presupuesto.luces.Count(l => l.shadows != LightShadows.None), Is.EqualTo(3));
            presupuesto.Actualizar(Vector3.right * 100);
            Assert.That(presupuesto.luces.All(l => l.shadows == LightShadows.None), Is.True);
        }
        finally { UnityEngine.Object.DestroyImmediate(raiz); }
    }

    [Test]
    public void ReconstruirNoSobrescribeUnaGrabacionPersonalizada()
    {
        string nombre = "__prueba_audio_" + Guid.NewGuid().ToString("N");
        string ruta = "Assets/Audio/Generado/" + nombre + ".wav";
        byte[] grabacion = File.ReadAllBytes("Assets/Audio/Generado/paso_1.wav");
        try
        {
            File.WriteAllBytes(ruta, grabacion);
            var generador = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("GeneradorAudioCrater"))
                .First(t => t != null);
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            generador.GetMethod("CargarHuellas", flags).Invoke(null, null);
            var clip = generador.GetMethod("Guardar", flags).Invoke(null, new object[] { nombre, new float[8820] });
            Assert.That(clip, Is.Not.Null);
            CollectionAssert.AreEqual(grabacion, File.ReadAllBytes(ruta));
        }
        finally { AssetDatabase.DeleteAsset(ruta); }
    }
}
