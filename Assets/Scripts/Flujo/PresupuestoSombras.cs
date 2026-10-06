using UnityEngine;

/// <summary>Reserva sombras para las luces locales que iluminan la zona del jugador.
/// Cada point light ocupa seis mapas; la linterna y el sol conservan sus sombras.</summary>
public class PresupuestoSombras : MonoBehaviour
{
    public Light[] luces;
    [Range(1, 4)] public int maximo = 3;
    public Light[] imprescindibles;
    float siguiente;
    float[] puntuaciones;

    void LateUpdate()
    {
        if (Time.unscaledTime < siguiente) return;
        siguiente = Time.unscaledTime + 0.25f;
        Actualizar(transform.position);
    }

    public void Actualizar(Vector3 ojo)
    {
        if (imprescindibles != null)
            foreach (var luz in imprescindibles) if (luz != null) luz.shadows = LightShadows.Soft;
        if (luces == null) return;
        if (puntuaciones == null || puntuaciones.Length != luces.Length) puntuaciones = new float[luces.Length];
        // Captura el estado anterior antes de seleccionar, para evitar que una luz
        // recién elegida cambie el orden de las siguientes en el mismo frame.
        for (int i = 0; i < luces.Length; i++)
        {
            var luz = luces[i];
            float d = luz != null ? Vector3.Distance(ojo, luz.transform.position) : float.PositiveInfinity;
            puntuaciones[i] = luz != null && luz.isActiveAndEnabled && luz.intensity > 0.01f && d <= luz.range + 2f
                ? d - (luz.shadows != LightShadows.None ? 0.6f : 0f) : float.PositiveInfinity;
        }
        for (int i = 0; i < luces.Length; i++)
        {
            if (luces[i] == null) continue;
            int delante = 0;
            for (int j = 0; j < luces.Length; j++)
                if (puntuaciones[j] < puntuaciones[i] || (puntuaciones[j] == puntuaciones[i] && j < i)) delante++;
            luces[i].shadows = !float.IsPositiveInfinity(puntuaciones[i]) && delante < maximo
                ? LightShadows.Soft : LightShadows.None;
        }
    }
}
