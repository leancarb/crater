using System;
using UnityEngine;

/// <summary>Refuerzo suave bajo 160 Hz al reproducir la compuerta sintetizada, sin cambiar el WAV.</summary>
[RequireComponent(typeof(AudioSource))]
public class GravesCompuerta : MonoBehaviour
{
    readonly float[] memoria = new float[8];
    float coeficiente;

    void OnEnable() => Reiniciar(AudioSettings.outputSampleRate);

    public void Reiniciar(int frecuencia)
    {
        coeficiente = 1f - Mathf.Exp(-2f * Mathf.PI * 160f / Mathf.Max(1, frecuencia));
        Array.Clear(memoria, 0, memoria.Length);
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
            datos[i] += .55f * memoria[canal];
        }
    }
}
