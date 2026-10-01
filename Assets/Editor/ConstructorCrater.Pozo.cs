using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// El cráter como lugar físico: un pozo en el valle de la capilla. La Explanada es
/// su fondo, así que se entra caminando, sin cortes ni teletransporte.
///
///   valle (piso de tierra)         y = 9   (la capilla y el cráter del valle)
///   fondo del pozo = Explanada     y = 4
///   resto del nivel                y = 0   (todo bajo tierra, con sus techos)
///
/// Desde la puerta, en el borde sur del pozo, una rampa baja pegada a la pared
/// hasta la Explanada. Del lado norte, una masa de roca tapa el resto del nivel y
/// deja la boca del túnel de la rampa que sigue al Umbral.
///
/// CÓMO FUNCIONA
/// El valle (el grupo 06_Capilla) se ubica de modo que el cráter del valle quede
/// justo encima de la Explanada. La tierra del valle es una malla con un agujero
/// redondo (MallaSueloConHueco); antes del eclipse una tapa de tierra cubre el
/// agujero y el prólogo la abre. Una zona que cubre todo el pozo avisa al prólogo
/// que el jugador entró, para que el ambiente pase del valle al cráter.
/// Acá también está el cielo estrellado (CieloEstrellado), que comparten el valle
/// en la totalidad, el pozo y el techo abierto de la Cresta.
/// </summary>
public static partial class ConstructorCrater
{
    /// <summary>Altura (y) del piso del valle: el nivel entero queda debajo.</summary>
    const float AlturaValle = 9f;
    const float RadioPozo = 10.5f;
    /// <summary>Centro del pozo, al nivel del piso de la Explanada.</summary>
    static readonly Vector3 CentroPozo = new Vector3(0f, 4f, -37.5f);
    /// <summary>Dónde está el cráter en las coordenadas locales de la capilla.</summary>
    static readonly Vector3 CraterEnLaCapilla = new Vector3(0f, 0f, 34f);

    // la rampa que baja pegada a la pared: ángulos medidos desde +x hacia +z
    const float RadioRampa = 9.35f, AnchoRampa = 2.2f;
    const float AnguloInicioRampa = 266f, AnguloFinRampa = 185f;

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

        // el fondo: la Explanada. Llega hasta la boca del túnel (z -34)
        Caja(pozo, "Piso_Pozo", -11.5f, piso - 0.3f, -49f, 11.5f, piso, -34f, k.piso);

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
        Caja(pozo, "Descanso_Puerta", -1.9f, suelo - 0.3f, CentroPozo.z - RadioPozo - 0.4f, 1.9f, suelo, -46.3f, k.piso);
        Caja(pozo, "Pared_Puerta", -1.9f, piso - 0.3f, -50.2f, 1.9f, suelo - 0.3f, -48.4f, k.basalto);

        // la rampa: segmentos inclinados de AnguloInicioRampa a AnguloFinRampa, de y 'suelo' a 'piso'
        const int segmentos = 12;
        float paso = (AnguloInicioRampa - AnguloFinRampa) / segmentos;
        for (int i = 0; i < segmentos; i++)
        {
            float a0 = AnguloInicioRampa - i * paso, a1 = a0 - paso;
            Vector3 p0 = EnElPozo(a0, RadioRampa, Mathf.Lerp(suelo, piso, (float)i / segmentos));
            Vector3 p1 = EnElPozo(a1, RadioRampa, Mathf.Lerp(suelo, piso, (float)(i + 1) / segmentos));
            Vector3 adelante = (p1 - p0).normalized;
            var giro = Quaternion.LookRotation(adelante, Vector3.up);
            Vector3 arriba = giro * Vector3.up;
            float largo = Vector3.Distance(p0, p1) + 0.35f;   // se solapan: sin escalones ni rendijas
            var tramo = Bloque(pozo, $"Rampa_Pozo_{i:00}", (p0 + p1) / 2f - arriba * 0.15f,
                new Vector3(AnchoRampa, 0.3f, largo), k.piso);
            tramo.transform.rotation = giro;

            // pretil bajo del lado del vacío
            Vector3 adentro = Vector3.Cross(Vector3.up, adelante).normalized;   // hacia el centro del pozo
            if (Vector3.Dot(adentro, new Vector3(CentroPozo.x - p0.x, 0f, CentroPozo.z - p0.z)) < 0f) adentro = -adentro;
            var pretil = Bloque(pozo, $"Pretil_Pozo_{i:00}", (p0 + p1) / 2f + adentro * (AnchoRampa / 2f - 0.12f) + arriba * 0.35f,
                new Vector3(0.25f, 0.7f, largo), k.basalto);
            pretil.transform.rotation = giro;
        }

        // al norte, la masa de roca: tapa el nivel y deja la boca del túnel (x ±2,05).
        // El techo del túnel va 3,2 m sobre el piso del pozo: la rampa arranca a esa altura
        // y el jugador tiene que pasar parado. Queda un poco por debajo de la tierra:
        // afuera del agujero no se ve
        float masa = suelo - 0.15f;
        float techoTunel = piso + 3.2f;
        Caja(pozo, "Masa_Norte_Izq", -12.6f, piso - 0.3f, -34.3f, -2.05f, masa, -23.3f, k.basalto);
        Caja(pozo, "Masa_Norte_Der", 2.05f, piso - 0.3f, -34.3f, 12.6f, masa, -23.3f, k.basaltoMedio);
        Caja(pozo, "Masa_Norte_Tunel", -2.05f, techoTunel, -34.3f, 2.05f, masa, -23.3f, k.basalto);
        // del lado del Umbral, el dintel llega a 5,8: se cierra lo que queda hasta el techo del túnel
        Caja(pozo, "Cierre_Tunel", -2.05f, 5.8f, -23.6f, 2.05f, techoTunel, -23.3f, k.basalto);

        // la zona de entrada: todo el pozo. Pasar la puerta (o caerse adentro) cambia el ambiente
        refs.zonaPuertaCrater = Zona(pozo, "Zona_Puerta_Crater", new Vector3(CentroPozo.x, (piso + AlturaValle + 0.6f) / 2f, CentroPozo.z),
            new Vector3(16f, AlturaValle + 0.6f - piso, 16f));
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

    // ================================================================== el cielo estrellado

    static void ConstruirCieloEstrellado(Kit k, Referencias refs)
    {
        var raiz = new GameObject("CieloEstrellado");
        var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        esfera.name = "Esfera";
        Object.DestroyImmediate(esfera.GetComponent<Collider>());
        esfera.transform.SetParent(raiz.transform, false);
        var r = esfera.GetComponent<Renderer>();
        r.sharedMaterial = k.estrellas;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        var cielo = raiz.AddComponent<CieloEstrellado>();
        Asignar(cielo, "esfera", r);
        refs.estrellas = cielo;
    }

    /// <summary>
    /// Panorámica (equirectangular) del cielo de la totalidad: fondo negro (el material
    /// suma luz), Vía Láctea con polvo oscuro, nebulosas tenues, estrellas de distintos
    /// brillos y colores, y el resplandor anaranjado del horizonte.
    /// </summary>
    static Texture2D TexturaEstrellas()
    {
        const int ancho = 2048, alto = 1024;
        string ruta = CarpetaTexturas + "CieloEstrellado.png";
        var px = new Color[ancho * alto];
        Vector3 Direccion(float u, float v)
        {
            float lon = u * Mathf.PI * 2f, lat = (v - 0.5f) * Mathf.PI;
            return new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
        }
        var planoVia = new Vector3(0.35f, 0.6f, 0.72f).normalized;   // la Vía Láctea cruza el cielo en diagonal

        for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
            {
                Vector3 d = Direccion((x + 0.5f) / ancho, (y + 0.5f) / alto);
                float lat = Mathf.Asin(d.y);

                float fbm = Ruido(d * 3f, 21) * 0.55f + Ruido(d * 7f, 22) * 0.3f + Ruido(d * 15f, 23) * 0.15f;
                float distVia = Vector3.Dot(d, planoVia);
                float via = Mathf.Exp(-(distVia / 0.2f) * (distVia / 0.2f)) * Mathf.Clamp01(0.5f + 0.7f * fbm);
                float polvo = 1f - 0.65f * Mathf.Exp(-(distVia / 0.035f) * (distVia / 0.035f)) * Mathf.Clamp01(0.6f + Ruido(d * 9f, 24));
                via *= polvo;
                Color c = new Color(0.07f, 0.065f, 0.11f) * via + new Color(0.06f, 0.05f, 0.035f) * via * via;

                float nebulosa = Mathf.Clamp01(Ruido(d * 2.2f, 31) * 1.3f - 0.45f);
                c += new Color(0.045f, 0.015f, 0.06f) * nebulosa + new Color(0.01f, 0.03f, 0.045f) * Mathf.Clamp01(Ruido(d * 1.6f, 32) - 0.3f);

                // el horizonte: un atardecer en todas direcciones, que se apaga hacia arriba
                float sobre = Mathf.SmoothStep(0f, 1f, (lat + 0.05f) / 0.05f);
                float h = Mathf.Max(lat, 0f);
                c += (new Color(0.5f, 0.22f, 0.07f) * Mathf.Exp(-h / 0.09f) + new Color(0.05f, 0.07f, 0.14f) * Mathf.Exp(-h / 0.45f)) * sobre;

                px[y * ancho + x] = c;
            }

        // estrellas: direcciones al azar repartidas parejo en la esfera, más algunas en la Vía Láctea
        var azar = new System.Random(5);
        float Azar() => (float)azar.NextDouble();
        void Sumar(int x, int y, Color c)
        {
            x = ((x % ancho) + ancho) % ancho;
            if (y < 0 || y >= alto) return;
            px[y * ancho + x] += c;
        }
        Color[] tonos = { new Color(0.75f, 0.85f, 1f), Color.white, new Color(1f, 0.92f, 0.78f), new Color(1f, 0.8f, 0.65f) };
        for (int i = 0; i < 9000; i++)
        {
            float zz = Azar() * 2f - 1f, fi = Azar() * Mathf.PI * 2f;
            var d = new Vector3(Mathf.Sqrt(1f - zz * zz) * Mathf.Cos(fi), zz, Mathf.Sqrt(1f - zz * zz) * Mathf.Sin(fi));
            float enVia = Mathf.Exp(-Mathf.Pow(Vector3.Dot(d, planoVia) / 0.2f, 2f));
            if (i % 3 == 0 && Azar() > enVia) continue;   // un tercio sólo vale si cae en la Vía Láctea
            float lon = Mathf.Atan2(d.z, d.x);
            if (lon < 0f) lon += Mathf.PI * 2f;
            int x = Mathf.FloorToInt(lon / (Mathf.PI * 2f) * ancho);
            int y = Mathf.FloorToInt((Mathf.Asin(d.y) / Mathf.PI + 0.5f) * alto);
            float brillo = 0.18f + 2.6f * Mathf.Pow(Azar(), 9f);
            Color c = tonos[azar.Next(tonos.Length)] * brillo;
            Sumar(x, y, c);
            if (brillo > 0.9f)
            {
                // las más brillantes, con un halo en cruz
                Sumar(x + 1, y, c * 0.3f); Sumar(x - 1, y, c * 0.3f);
                Sumar(x, y + 1, c * 0.3f); Sumar(x, y - 1, c * 0.3f);
            }
        }

        var tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        tex.SetPixels(px);
        tex.Apply();
        System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importador.mipmapEnabled = false;
        importador.wrapModeU = TextureWrapMode.Repeat;
        importador.wrapModeV = TextureWrapMode.Clamp;
        importador.maxTextureSize = 2048;
        importador.textureCompression = TextureImporterCompression.CompressedHQ;
        importador.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }
}
