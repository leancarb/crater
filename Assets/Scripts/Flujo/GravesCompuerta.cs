using System;
using UnityEngine;

/// <summary>Presencia en medios para parlantes chicos, sin cambiar el WAV sintetizado.</summary>
[RequireComponent(typeof(AudioSource))]
public class GravesCompuerta : MonoBehaviour
{
    readonly float[] memoria = new float[8];
    readonly float[] mediosLentos = new float[8], mediosRapidos = new float[8];
    float coeficiente, corte400, corte2400;

    void OnEnable() => Reiniciar(AudioSettings.outputSampleRate);

    public void Reiniciar(int frecuencia)
    {
        coeficiente = 1f - Mathf.Exp(-2f * Mathf.PI * 160f / Mathf.Max(1, frecuencia));
        corte400 = 1f - Mathf.Exp(-2f * Mathf.PI * 400f / Mathf.Max(1, frecuencia));
        corte2400 = 1f - Mathf.Exp(-2f * Mathf.PI * 2400f / Mathf.Max(1, frecuencia));
        Array.Clear(memoria, 0, memoria.Length);
        Array.Clear(mediosLentos, 0, mediosLentos.Length);
        Array.Clear(mediosRapidos, 0, mediosRapidos.Length);
    }

    // Sin Unity API, asignaciones ni búsquedas en el hilo de audio.
    void OnAudioFilterRead(float[] datos, int canales) => Procesar(datos, canales);

    public void Procesar(float[] datos, int canales)
    {
        if (canales <= 0 || canales > memoria.Length) return;
        for (int i = 0; i < datos.Length; i++)
        {
            int canal = i % canales;
            memoria[canal] += coeficiente * (datos[i] - memoria[canal]);
            mediosLentos[canal] += corte400 * (datos[i] - mediosLentos[canal]);
            mediosRapidos[canal] += corte2400 * (datos[i] - mediosRapidos[canal]);
            float salida = datos[i] - .25f * memoria[canal] + .8f * (mediosRapidos[canal] - mediosLentos[canal]);
            datos[i] = Math.Max(-.95f, Math.Min(.95f, salida));
        }
    }
}
