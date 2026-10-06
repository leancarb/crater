using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// El cráter como lugar físico: un pozo en el valle de la capilla. La Explanada es
/// su fondo, así que se entra caminando, sin cortes ni teletransporte.
///
///   valle (piso de tierra)         y = 9   (la capilla y el cráter del valle)
///   fondo del pozo = Explanada     y = 0   (a la altura del resto del nivel)
///
/// Desde la abertura en el suelo, en el borde sur del pozo, una escalera recta baja hasta el
/// fondo. Del lado norte, una masa de roca tapa el resto del nivel y deja la boca
/// de un pasillo plano que sigue al Umbral.
///
/// CÓMO FUNCIONA
/// El valle (el grupo 06_Capilla) se ubica de modo que el cráter del valle quede
/// justo encima de la Explanada. La tierra del valle es una malla con un agujero
/// redondo (MallaSueloConHueco); antes del eclipse una tapa de tierra cubre el
/// agujero y el prólogo la abre. Una zona que cubre todo el pozo avisa al prólogo
/// que el jugador entró, para que el ambiente pase del valle al cráter.
/// </summary>
public static partial class ConstructorCrater
{
    /// <summary>Altura (y) del piso del valle: el nivel entero queda debajo.</summary>
    const float AlturaValle = 9f;
    const float RadioPozo = 10.5f;
    /// <summary>Centro del pozo, al nivel del piso de la Explanada.</summary>
    static readonly Vector3 CentroPozo = new Vector3(0f, 0f, -37.5f);
    /// <summary>Dónde está el cráter en las coordenadas locales de la capilla.</summary>
    static readonly Vector3 CraterEnLaCapilla = new Vector3(0f, 0f, 34f);

    // la escalera: baja derecho hacia el norte, del descanso de la puerta al fondo
    const float InicioEscalera = -46.3f, FinEscalera = -34.6f, MitadEscalera = 1.6f;
    const int Escalones = 40;

    /// <summary>Un punto de la circunferencia del pozo, en el plano horizontal.</summary>
    static Vector3 EnElPozo(float angulo, float radio, float y) =>
        new Vector3(CentroPozo.x + Mathf.Cos(angulo * Mathf.Deg2Rad) * radio, y,
                    CentroPozo.z + Mathf.Sin(angulo * Mathf.Deg2Rad) * radio);

    // ================================================================== el pozo

    static void ConstruirPozo(Kit k, Transform g, Referencias refs)
    {
        var pozo = Grupo(g, "Pozo");
        float suelo = AlturaValle - 0.1f;           // la cara de arriba de la tierra del valle
        float borde = AlturaValle + 0.2f;           // las paredes asoman un poco: el labio del cráter
        float piso = CentroPozo.y;

        // el fondo: la Explanada. Del lado norte sigue el pasillo plano hasta el Umbral
        Caja(pozo, "Piso_Pozo", -11.5f, piso - 0.3f, -49f, 11.5f, piso, -34.3f, k.piso);
        Caja(pozo, "Piso_Pasaje", -2.05f, piso - 0.3f, -34.3f, 2.05f, piso, -23f, k.piso);

        // la pared: un anillo de bloques. Al sur queda el hueco de la puerta; al norte está la masa
        const int bloques = 24;
        for (int i = 0; i < bloques; i++)
        {
            float ang = i * 360f / bloques;
            if (Mathf.Abs(Mathf.DeltaAngle(ang, 270f)) < 8f) continue;     // la puerta
            Vector3 centro = EnElPozo(ang, RadioPozo + 0.7f, (piso - 0.3f + borde) / 2f);
            if (centro.z > CentroPozo.z + 5f) continue;                    // adentro de la masa (las de la orilla se solapan con ella)
            float ancho = 2f * Mathf.PI * (RadioPozo + 0.7f) / bloques * 1.25f;
            var b = Bloque(pozo, $"Pared_Pozo_{i:00}", centro, new Vector3(ancho, borde - (piso - 0.3f), 1.4f),
                i % 3 == 0 ? k.basaltoMedio : k.basalto);
            b.transform.rotation = Quaternion.LookRotation(new Vector3(CentroPozo.x - centro.x, 0f, CentroPozo.z - centro.z));
        }

        // la puerta: un descanso al nivel del valle y la pared rellena debajo
        // (empieza justo en el borde del agujero de la tierra: si se superpusieran, parpadearían)
        Caja(pozo, "Descanso_Puerta", -1.9f, suelo - 0.3f, CentroPozo.z - RadioPozo - 0.4f, 1.9f, suelo, InicioEscalera, k.piso);
        Caja(pozo, "Pared_Puerta", -1.9f, piso - 0.3f, -50.2f, 1.9f, suelo - 0.3f, -48.4f, k.basalto);
        ConstruirEscalera(k, pozo, suelo, piso, refs);

        // al norte, la masa de roca: tapa el nivel y deja la boca del pasillo (x ±2,05, 4 m de
        // alto, como el dintel del Umbral). Queda un poco por debajo de la tierra: afuera
        // del agujero no se ve
        float masa = suelo - 0.15f;
        Caja(pozo, "Masa_Norte_Izq", -12.6f, piso - 0.3f, -34.3f, -2.05f, masa, -23.3f, k.basalto);
        Caja(pozo, "Masa_Norte_Der", 2.05f, piso - 0.3f, -34.3f, 12.6f, masa, -23.3f, k.basaltoMedio);
        Caja(pozo, "Masa_Norte_Pasaje", -2.05f, piso + 4f, -34.3f, 2.05f, masa, -23.3f, k.basalto);

        // la zona de entrada: todo el pozo. Pasar la puerta (o caerse adentro) cambia el ambiente
        refs.zonaPuertaCrater = Zona(pozo, "Zona_Puerta_Crater", new Vector3(CentroPozo.x, (piso + AlturaValle + 0.6f) / 2f, CentroPozo.z),
            new Vector3(16f, AlturaValle + 0.6f - piso, 16f));
    }

    /// <summary>
    /// La escalera de la puerta al fondo: escalones macizos (de piedra hasta el piso,
    /// sin huecos abajo) entre dos paredes altas talladas. Para caminarla sin saltitos, los
    /// escalones no tienen collider: se pisa una rampa invisible que pasa por el medio
    /// de cada escalón (a lo sumo a unos centímetros de la piedra que se ve).
    /// </summary>
    static void ConstruirEscalera(Kit k, Transform pozo, float arriba, float abajo, Referencias refs)
    {
        var escalera = Grupo(pozo, "Escalera");
        float largo = FinEscalera - InicioEscalera;
        float pisada = largo / Escalones, alzada = (arriba - abajo) / Escalones;

        for (int i = 0; i < Escalones; i++)
        {
            float z0 = InicioEscalera + i * pisada;
            float tope = arriba - (i + 0.5f) * alzada;
            var escalon = Caja(escalera, $"Escalon_{i:00}", -MitadEscalera, abajo - 0.3f, z0, MitadEscalera, tope, z0 + pisada + 0.02f,
                i % 2 == 0 ? k.piso : k.basaltoMedio);
            Object.DestroyImmediate(escalon.GetComponent<Collider>());
        }

        // la rampa invisible: de (InicioEscalera, arriba) a (FinEscalera, abajo)
        float caida = arriba - abajo;
        float angulo = Mathf.Atan2(caida, largo);
        float hipotenusa = Mathf.Sqrt(largo * largo + caida * caida);
        var normal = new Vector3(0f, Mathf.Cos(angulo), Mathf.Sin(angulo));
        var medio = new Vector3(0f, (arriba + abajo) / 2f, (InicioEscalera + FinEscalera) / 2f);
        var rampa = new GameObject("Rampa_Escalera");
        rampa.transform.SetParent(escalera, false);
        rampa.transform.SetPositionAndRotation(medio - normal * 0.2f, Quaternion.Euler(angulo * Mathf.Rad2Deg, 0f, 0f));
        rampa.AddComponent<BoxCollider>().size = new Vector3(MitadEscalera * 2f, 0.4f, hipotenusa + 0.4f);
        Estatico(rampa);

        // Sellar los laterales hasta el terreno exterior. No queda una cavidad
        // accesible entre la escalera y la masa de roca del pozo.
        foreach (float lado in new[] { -1f, 1f })
        {
            float xa = lado < 0 ? -12.6f : MitadEscalera + 0.45f;
            float xb = lado < 0 ? -MitadEscalera - 0.45f : 12.6f;
            Caja(pozo, lado < 0 ? "Cierre_Tierra_Escalera_Izq" : "Cierre_Tierra_Escalera_Der",
                xa, arriba - 0.4f, CentroPozo.z - RadioPozo - 0.5f,
                xb, arriba - 0.04f, -23.3f, k.tierra);
        }

        // Las paredes llegan hasta el terreno exterior y guardan un borde continuo,
        // con lunas a la izquierda y soles a la derecha. Cada tramo es un grupo (pared y
        // tallado) que sube con el borde del cráter en el prólogo
        const int tramos = 8;
        for (int i = 0; i < tramos; i++)
        {
            float z0 = InicioEscalera + i * largo / tramos, z1 = z0 + largo / tramos;
            float lineaArriba = arriba - (z0 - InicioEscalera) / largo * caida;
            float lineaMedio = arriba - ((z0 + z1) / 2f - InicioEscalera) / largo * caida;
            float tope = arriba + 0.45f;
            Transform Tramo(float lado)
            {
                float x0 = lado < 0f ? -MitadEscalera - 0.6f : MitadEscalera, x1 = x0 + 0.6f;
                var tramo = new GameObject($"Pared_Escalera_{(lado < 0f ? "Izq" : "Der")}_{i}").transform;
                tramo.SetParent(escalera, false);
                tramo.position = new Vector3((x0 + x1) / 2f, 0f, (z0 + z1) / 2f);
                Bloque(tramo, "Muro", new Vector3((x0 + x1) / 2f, (abajo - 0.3f + tope) / 2f, (z0 + z1) / 2f),
                    new Vector3(0.6f, tope - (abajo - 0.3f), z1 - z0 + 0.02f), lado < 0f ? k.basalto : k.basaltoMedio, false);
                refs.paredesEscalera.Add(tramo);
                return tramo;
            }
            var izq = Tramo(-1f);
            var der = Tramo(1f);
            float zMedio = (z0 + z1) / 2f;
            TallarEscalera(k, izq, der, new Vector3(-MitadEscalera + 0.02f, lineaMedio + 1.7f, zMedio),
                new Vector3(MitadEscalera - 0.02f, lineaMedio + 1.7f, zMedio), i);
        }
    }

    // ================================================================== la tierra del valle

    /// <summary>
    /// Un rectángulo de tierra (local x0..x1, z0..z1, a la altura y) con un agujero
    /// redondo. Se arma en anillos alrededor del agujero, del borde del agujero al del
    /// rectángulo, con los vértices del medio corridos al azar: triángulos grandes e
    /// irregulares, planos y con un tono cada uno. Sirve también de collider.
    /// </summary>
    static Mesh MallaSueloConHueco(string nombre, float x0, float x1, float z0, float z1, float y, Vector3 centroHueco, float radioHueco)
    {
        const int sectores = 48;
        var azar = new System.Random(41);
        float Azar() => (float)azar.NextDouble() - 0.5f;

        // distancia del centro del agujero al borde del rectángulo, en la dirección 'd'
        float HastaElBorde(Vector2 d)
        {
            float t = float.MaxValue;
            if (d.x > 1e-4f) t = Mathf.Min(t, (x1 - centroHueco.x) / d.x);
            if (d.x < -1e-4f) t = Mathf.Min(t, (x0 - centroHueco.x) / d.x);
            if (d.y > 1e-4f) t = Mathf.Min(t, (z1 - centroHueco.z) / d.y);
            if (d.y < -1e-4f) t = Mathf.Min(t, (z0 - centroHueco.z) / d.y);
            return t;
        }

        // anillos: tantos como haga falta para que cada cara mida ~4 m en el sentido radial
        float maximo = 0f;
        for (int s = 0; s < sectores; s++)
        {
            float a = s * Mathf.PI * 2f / sectores;
            maximo = Mathf.Max(maximo, HastaElBorde(new Vector2(Mathf.Cos(a), Mathf.Sin(a))));
        }
        int anillos = Mathf.Max(2, Mathf.CeilToInt((maximo - radioHueco) / 4f));

        var p = new Vector3[sectores, anillos + 1];
        for (int s = 0; s < sectores; s++)
        {
            float a = s * Mathf.PI * 2f / sectores;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float hasta = HastaElBorde(d);
            for (int j = 0; j <= anillos; j++)
            {
                float r = Mathf.Lerp(radioHueco, hasta, (float)j / anillos);
                Vector2 q = new Vector2(centroHueco.x, centroHueco.z) + d * r;
                // el borde del agujero y el del rectángulo quedan fijos; el resto se corre
                if (j > 0 && j < anillos)
                {
                    float paso = (hasta - radioHueco) / anillos;
                    q += d * Azar() * paso * 0.45f + new Vector2(-d.y, d.x) * Azar() * r * (Mathf.PI * 2f / sectores) * 0.45f;
                }
                p[s, j] = new Vector3(q.x, y, q.y);
            }
        }

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (int s = 0; s < sectores; s++)
        {
            int s1 = (s + 1) % sectores;
            for (int j = 0; j < anillos; j++)
            {
                // en este orden la cara queda boca arriba (normal hacia +y)
                Vector3 a = p[s, j], b = p[s1, j], c = p[s1, j + 1], d = p[s, j + 1];
                Vector2 uv1 = PixelDeFaceta((a + b + c) / 3f), uv2 = PixelDeFaceta((a + c + d) / 3f);
                Triangulo(vertices, uvs, a, b, c, uv1);
                Triangulo(vertices, uvs, a, c, d, uv2);
            }
        }
        return GuardarMalla(CrearMalla(nombre, vertices, uvs));
    }
}
