using System.Collections.Generic;
using UnityEngine;

public static partial class ConstructorCrater
{
    // Rellenos puntuales sin lámpara, llama, abertura ni respuesta funcional que los motive.
    static readonly HashSet<string> RellenosSinFuente = new HashSet<string> {
        "Luz_Plaza", "Luz_Umbral", "Luz_Compuerta", "Luz_Cresta", "Luz_Umbral_Cresta",
        "Luz_Vuelta_O", "Luz_Vuelta_E", "Luz_Rotonda", "Luz_Rotonda_Norte", "Luz_Bajo_Puerta_Sellos",
        "Luz_Pasillo_O", "Luz_Pasillo_E", "Luz_O1", "Luz_O2", "Luz_O3", "Luz_Sello_Oeste",
        "Luz_Giro_Luna_Sur", "Luz_Giro_Luna_Norte", "Luz_E1", "Luz_E1_Fondo", "Luz_E2",
        "Luz_Galeria_E", "Luz_Trinchera_E", "Luz_Sello_Este", "Luz_N_Lectura", "Luz_N_Isla", "Luz_N_Salida",
        "Luz_Gruta", "Luz_Pie_Escalera", "Luz_Rincon_O1", "Rebote_Sello_SOL", "Luz_Desvio_Luna", "Luz_Regreso_Luna",
        "Luz_Cuidado_Conjunto", "Luz_Cavidad", "Luz_Relieve_Perspectiva", "Luz_Apoyo_Mirador", "Luz_Interior",
        "Luz_Mural", "Luz_Murales_Este", "Luz_Murales_Oeste", "Luz_Pasaje"
    };

    public static bool EsRellenoSinFuente(string nombre) => RellenosSinFuente.Contains(nombre);

    static void AjustarAtmosfera()
    {
        foreach (var luz in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (EsRellenoSinFuente(luz.name))
            {
                Object.DestroyImmediate(luz.gameObject);
                continue;
            }
            // Los haces restantes nacen en óculos o lámparas visibles, con transición amplia.
            if (luz.type == LightType.Spot && luz.GetComponent<LinternaController>() == null)
                luz.innerSpotAngle = luz.spotAngle * .2f;
        }
    }
}
