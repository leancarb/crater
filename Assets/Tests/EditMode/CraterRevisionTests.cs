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
