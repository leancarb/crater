using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sintetiza todos los sonidos de la demo y los guarda como WAV en Assets/Audio/Generado.
/// Es determinista: correrlo dos veces da los mismos archivos. Cuando haya sonido
/// grabado de verdad, alcanza con reemplazar el WAV manteniendo el nombre.
/// </summary>
public static class GeneradorAudioCrater
{
    public const string Carpeta = "Assets/Audio/Generado";
    const int Muestreo = 44100;

    public sealed class Clips
    {
        public AudioClip[] tonosAncla;
        public AudioClip puenteAparecer, puenteDisolver, siseoReja, compuerta;
        public AudioClip recoger, linternaEncender, linternaApagar, equiparCuerpo, equiparHueco;
        public AudioClip zumbidoCuerpo, zumbidoHueco;
        public AudioClip ambienteCrater, vientoOculo, exteriorCapilla;
        public AudioClip[] pasos;
    }

    public static Clips GenerarTodo()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Audio")) AssetDatabase.CreateFolder("Assets", "Audio");
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/Audio", "Generado");

        // La, Do#, Mi, La: cada par de anclas suena como un intervalo del mismo acorde
        float[] notas = { 220f, 277.18f, 329.63f, 440f, 554.37f, 659.25f };
        var clips = new Clips { tonosAncla = new AudioClip[notas.Length], pasos = new AudioClip[4] };
        for (int i = 0; i < notas.Length; i++)
            clips.tonosAncla[i] = Guardar($"ancla_tono_{i + 1}", Campana(notas[i], 3.2f, 0.55f));

        clips.puenteAparecer = Guardar("puente_aparecer", Barrido(180f, 720f, 0.9f, 0.45f, 11));
        clips.puenteDisolver = Guardar("puente_disolver", Barrido(640f, 140f, 0.7f, 0.4f, 12));
        clips.siseoReja = Guardar("reja_siseo", Bucle(Ruido(2f, 0.25f, 0.18f, 21), 0.25f));
        clips.compuerta = Guardar("compuerta_piedra", Retumbo(3.2f, 31));
        clips.recoger = Guardar("recoger", Arpegio(new[] { 440f, 554.37f, 659.25f, 880f }, 0.11f, 1.6f));
        clips.linternaEncender = Guardar("linterna_encender", Click(2400f, 0.35f, 41));
        clips.linternaApagar = Guardar("linterna_apagar", Click(1700f, 0.3f, 42));
        clips.equiparCuerpo = Guardar("filtro_cuerpo_equipar", Campana(329.63f, 0.9f, 0.4f));
        clips.equiparHueco = Guardar("filtro_hueco_equipar", Campana(493.88f, 0.9f, 0.4f));
        clips.zumbidoCuerpo = Guardar("filtro_cuerpo_zumbido", Dron(4f, new[] { 55f, 110f, 165.25f }, new[] { 0.5f, 0.25f, 0.08f }, 0.12f, 0f, 51));
        clips.zumbidoHueco = Guardar("filtro_hueco_zumbido", Dron(4f, new[] { 73.5f, 147f }, new[] { 0.35f, 0.18f }, 0.1f, 0.05f, 52));
        clips.ambienteCrater = Guardar("ambiente_crater", Dron(12f, new[] { 41.25f, 61.75f, 82.5f }, new[] { 0.4f, 0.22f, 0.12f }, 0.35f, 0.04f, 61));
        clips.vientoOculo = Guardar("viento_oculo", Bucle(Viento(10f, 71), 1f));
        clips.exteriorCapilla = Guardar("exterior_capilla", Bucle(Viento(10f, 81, 0.12f, 1800f), 1f));
        for (int i = 0; i < clips.pasos.Length; i++)
            clips.pasos[i] = Guardar($"paso_{i + 1}", Paso(91 + i));

        return clips;
    }

    // ------------------------------------------------------------------ síntesis

    static float[] Campana(float f, float duracion, float volumen)
    {
        float[] parciales = { 1f, 2f, 2.76f, 5.4f };
        float[] amplitudes = { 1f, 0.45f, 0.22f, 0.1f };
        float[] caidas = { 1f, 0.6f, 0.4f, 0.22f };
        var s = new float[(int)(duracion * Muestreo)];
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Muestreo;
            float ataque = Mathf.Clamp01(t / 0.006f);
            float v = 0f;
            for (int p = 0; p < parciales.Length; p++)
                v += amplitudes[p] * Mathf.Exp(-t / (duracion * caidas[p] * 0.35f)) * Mathf.Sin(2f * Mathf.PI * f * parciales[p] * t);
            s[i] = v * ataque;
        }
        return Normalizar(s, volumen);
    }

    static float[] Barrido(float f0, float f1, float duracion, float volumen, int semilla)
    {
        var azar = new System.Random(semilla);
        var s = new float[(int)(duracion * Muestreo)];
        float fase = 0f, fase2 = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float u = i / (float)s.Length;
            float f = Mathf.Lerp(f0, f1, u * u * (3f - 2f * u));
            fase += 2f * Mathf.PI * f / Muestreo;
            fase2 += 2f * Mathf.PI * f * 1.502f / Muestreo;
            float env = Mathf.Sin(Mathf.PI * u) * (1f - u * 0.4f);
            float brillo = (float)(azar.NextDouble() * 2 - 1) * 0.08f;
            s[i] = env * (Mathf.Sin(fase) + 0.4f * Mathf.Sin(fase2) + brillo);
        }
        return Normalizar(s, volumen);
    }

    static float[] Ruido(float duracion, float corte, float volumen, int semilla)
    {
        var azar = new System.Random(semilla);
        var s = new float[(int)(duracion * Muestreo)];
        float lp = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float blanco = (float)(azar.NextDouble() * 2 - 1);
            lp += corte * (blanco - lp);
            s[i] = blanco - lp;   // pasa-altos suave: siseo
        }
        return Normalizar(s, volumen);
    }

    static float[] Viento(float duracion, int semilla, float volumen = 0.45f, float corteHz = 500f)
    {
        var azar = new System.Random(semilla);
        var s = new float[(int)(duracion * Muestreo)];
        float lp = 0f, lp2 = 0f;
        float a = 1f - Mathf.Exp(-2f * Mathf.PI * corteHz / Muestreo);
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Muestreo;
            float blanco = (float)(azar.NextDouble() * 2 - 1);
            lp += a * (blanco - lp);
            lp2 += a * (lp - lp2);
            // ráfagas: ciclos enteros dentro de la duración para que el bucle no se note
            float rafaga = 0.55f + 0.3f * Mathf.Sin(2f * Mathf.PI * t * 2f / duracion) + 0.15f * Mathf.Sin(2f * Mathf.PI * t * 5f / duracion);
            s[i] = lp2 * rafaga;
        }
        return Normalizar(s, volumen);
    }

    static float[] Dron(float duracion, float[] frecuencias, float[] amplitudes, float volumen, float ruido, int semilla)
    {
        var azar = new System.Random(semilla);
        var s = new float[(int)(duracion * Muestreo)];
        float lp = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Muestreo;
            float v = 0f;
            for (int k = 0; k < frecuencias.Length; k++)
            {
                // las frecuencias elegidas completan ciclos enteros en 'duracion': bucle perfecto
                float tremolo = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * t * (k + 1) / duracion);
                v += amplitudes[k] * tremolo * Mathf.Sin(2f * Mathf.PI * frecuencias[k] * t);
            }
            lp += 0.02f * ((float)(azar.NextDouble() * 2 - 1) - lp);
            s[i] = v + lp * ruido * 20f;
        }
        return Normalizar(s, volumen);
    }

    static float[] Retumbo(float duracion, int semilla)
    {
        var azar = new System.Random(semilla);
        var s = new float[(int)(duracion * Muestreo)];
        float lp = 0f, lp2 = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float u = i / (float)s.Length;
            float t = i / (float)Muestreo;
            float blanco = (float)(azar.NextDouble() * 2 - 1);
            lp += 0.01f * (blanco - lp);
            lp2 += 0.01f * (lp - lp2);
            float env = Mathf.Clamp01(u / 0.05f) * Mathf.Pow(1f - u, 0.6f);
            float crujido = azar.NextDouble() < 0.0006 ? (float)(azar.NextDouble() * 2 - 1) : 0f;
            s[i] = env * (lp2 * 30f + 0.25f * Mathf.Sin(2f * Mathf.PI * 38f * t) + crujido);
        }
        return Normalizar(s, 0.6f);
    }

    static float[] Arpegio(float[] notas, float separacion, float duracion)
    {
        var s = new float[(int)(duracion * Muestreo)];
        for (int n = 0; n < notas.Length; n++)
        {
            var nota = Campana(notas[n], duracion - separacion * n, 1f);
            int inicio = (int)(separacion * n * Muestreo);
            for (int i = 0; i < nota.Length && inicio + i < s.Length; i++) s[inicio + i] += nota[i] * 0.5f;
        }
        return Normalizar(s, 0.5f);
    }

    static float[] Click(float f, float volumen, int semilla)
    {
        var azar = new System.Random(semilla);
        var s = new float[(int)(0.06f * Muestreo)];
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Muestreo;
            float env = Mathf.Exp(-t / 0.008f);
            s[i] = env * (0.6f * Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * (float)(azar.NextDouble() * 2 - 1));
        }
        return Normalizar(s, volumen);
    }

    static float[] Paso(int semilla)
    {
        var azar = new System.Random(semilla);
        var s = new float[(int)(0.16f * Muestreo)];
        float lp = 0f;
        float corte = 0.05f + (float)azar.NextDouble() * 0.04f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = i / (float)Muestreo;
            float blanco = (float)(azar.NextDouble() * 2 - 1);
            lp += corte * (blanco - lp);
            float env = Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-t / 0.035f);
            s[i] = env * (lp * 3f + 0.3f * Mathf.Sin(2f * Mathf.PI * 70f * t));
        }
        return Normalizar(s, 0.35f);
    }

    /// <summary>Funde el final con el principio para que el bucle no haga click.</summary>
    static float[] Bucle(float[] s, float segundosDeFundido)
    {
        int n = Mathf.Min((int)(segundosDeFundido * Muestreo), s.Length / 2);
        var r = new float[s.Length - n];
        Array.Copy(s, r, r.Length);
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)n;
            r[i] = s[i] * u + s[r.Length + i] * (1f - u);
        }
        return r;
    }

    static float[] Normalizar(float[] s, float pico)
    {
        float max = 0.0001f;
        foreach (float v in s) max = Mathf.Max(max, Mathf.Abs(v));
        for (int i = 0; i < s.Length; i++) s[i] = s[i] / max * pico;
        return s;
    }

    // ------------------------------------------------------------------ archivos

    static AudioClip Guardar(string nombre, float[] muestras)
    {
        string ruta = $"{Carpeta}/{nombre}.wav";
        EscribirWav(ruta, muestras);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        var importador = (AudioImporter)AssetImporter.GetAtPath(ruta);
        var ajustes = importador.defaultSampleSettings;
        ajustes.loadType = muestras.Length > Muestreo * 3 ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
        ajustes.quality = 0.7f;
        importador.defaultSampleSettings = ajustes;
        importador.forceToMono = true;
        importador.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
    }

    static void EscribirWav(string ruta, float[] muestras)
    {
        using var flujo = new FileStream(ruta, FileMode.Create);
        using var w = new BinaryWriter(flujo);
        int bytes = muestras.Length * 2;
        w.Write(new[] { 'R', 'I', 'F', 'F' });
        w.Write(36 + bytes);
        w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
        w.Write(16);
        w.Write((short)1);          // PCM
        w.Write((short)1);          // mono
        w.Write(Muestreo);
        w.Write(Muestreo * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write(new[] { 'd', 'a', 't', 'a' });
        w.Write(bytes);
        foreach (float v in muestras)
            w.Write((short)(Mathf.Clamp(v, -1f, 1f) * short.MaxValue));
    }
}
