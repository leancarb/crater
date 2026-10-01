using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// La estética low-poly: superficies hechas de caras planas que se distinguen
/// una de otra, como en los videos del tráiler.
///
/// CÓMO FUNCIONA
/// Son tres trucos, todos aplicados al construir (en el juego no corre nada):
///  1. Caras planas. Cada triángulo tiene sus propios vértices, así la normal es la
///     de la cara y la luz cambia de golpe de una cara a la otra (no se suaviza).
///     A los FBX del kit se les pide lo mismo al importarlos (ángulo de suavizado 0).
///  2. Caras irregulares. Las cajas del nivel (muros, pisos, techos, rocas) se
///     subdividen en una grilla (ver los Perfil) y cada vértice se corre un poco según un
///     ruido 3D continuo del mundo. Como el ruido depende sólo de la posición, dos
///     piezas que se tocan se deforman igual y no se abren grietas entre ellas.
///     Los colliders siguen siendo las cajas originales: el juego no cambia.
///  3. Un tono por cara. URP/Lit no usa colores de vértice, así que cada cara apunta
///     sus UV a un único píxel de una textura chica de grises ('Facetas', filtro
///     Point). Así cada cara sale un poco más clara u oscura que su vecina.
/// Las mallas generadas se guardan juntas en Assets/Models/Facetado/MallasFacetadas.asset.
/// </summary>
public static partial class ConstructorCrater
{
    const string RutaMallasFacetadas = "Assets/Models/Facetado/MallasFacetadas.asset";
    const int LadoTexturaFacetas = 32;

    /// <summary>Cómo se faceta una caja: tamaño de las caras, cuánto se deforma y qué tan seguido.</summary>
    readonly struct Perfil
    {
        public readonly float celda;        // lado aproximado de cada cara, en metros
        public readonly Vector3 relieve;    // cuánto se corre como máximo cada vértice, por eje
        public readonly float onda;         // metros entre "bultos" del ruido
        public readonly int semilla, tope;  // tope: máximo de caras por lado
        public readonly bool sinBase;       // va enterrada: la cara de abajo no se genera
        public Perfil(float celda, Vector3 relieve, float onda, int semilla, int tope = 48, bool sinBase = false)
        {
            this.celda = celda; this.relieve = relieve; this.onda = onda;
            this.semilla = semilla; this.tope = tope; this.sinBase = sinBase;
        }
    }

    // muros, pisos y techos del cráter: piedra apenas irregular
    static readonly Perfil PerfilNivel = new Perfil(1.3f, new Vector3(0.09f, 0.06f, 0.09f), 2.6f, 1);
    // rocas del horizonte y cerros lejanos: enormes y sueltas, mucho relieve
    static readonly Perfil PerfilRoca = new Perfil(6f, Vector3.one * 2.2f, 9f, 3, sinBase: true);
    // cerros que cierran el valle (con colisión: relieve moderado)
    static readonly Perfil PerfilCerro = new Perfil(3f, new Vector3(1f, 0.8f, 1f), 6f, 4, sinBase: true);
    // suelo del valle: triángulos grandes, planos (sólo se mueven de costado) y de distinto tono
    static readonly Perfil PerfilSuelo = new Perfil(4f, new Vector3(0.8f, 0f, 0.8f), 8f, 5, 64, sinBase: true);
    // borde del cráter del valle: rocas quebradas
    static readonly Perfil PerfilCrater = new Perfil(1.2f, new Vector3(0.35f, 0.3f, 0.35f), 2.5f, 6);
    // capilla: adobe y cal casi rectos, sólo un poco a mano
    static readonly Perfil PerfilCapilla = new Perfil(1f, new Vector3(0.04f, 0.03f, 0.04f), 2f, 7);

    static Object contenedorMallas;
    static readonly Dictionary<Mesh, Mesh> mallasPlanas = new Dictionary<Mesh, Mesh>();

    // ================================================================== preparación

    /// <summary>Que los FBX del kit se importen con caras planas.</summary>
    static void ImportarConCarasPlanas(string ruta)
    {
        if (!(AssetImporter.GetAtPath(ruta) is ModelImporter importador)) return;
        if (importador.importNormals == ModelImporterNormals.Calculate
            && importador.normalSmoothingSource == ModelImporterNormalSmoothingSource.FromAngle
            && importador.normalSmoothingAngle <= 0.01f) return;
        importador.importNormals = ModelImporterNormals.Calculate;
        importador.normalSmoothingSource = ModelImporterNormalSmoothingSource.FromAngle;
        importador.normalSmoothingAngle = 0f;
        importador.SaveAndReimport();
    }

    /// <summary>
    /// Vacía el archivo de mallas (conserva su GUID) y le pone la textura de facetas a
    /// los materiales de piedra. Va después de crear los materiales y antes de los prefabs.
    /// </summary>
    static void PrepararFacetado(Kit kit)
    {
        AsegurarCarpeta("Assets/Models/Facetado");
        contenedorMallas = AssetDatabase.LoadMainAssetAtPath(RutaMallasFacetadas);
        if (contenedorMallas == null)
        {
            contenedorMallas = new Mesh { name = "MallasFacetadas" };
            AssetDatabase.CreateAsset(contenedorMallas, RutaMallasFacetadas);
        }
        foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(RutaMallasFacetadas))
        {
            AssetDatabase.RemoveObjectFromAsset(sub);
            Object.DestroyImmediate(sub, true);
        }
        mallasPlanas.Clear();

        var facetas = TexturaFacetas();
        // piedra (y tierra, adobe...): caras planas y un tono por cara
        kit.piedraFacetada = new HashSet<Material>(MaterialesDePiedra(kit));
        foreach (var m in kit.piedraFacetada)
        {
            m.SetTexture("_BaseMap", facetas);
            m.SetTexture("_MainTex", facetas);
            EditorUtility.SetDirty(m);
        }
        // tallados: sólo caras planas (no tienen textura)
        kit.facetables = new HashSet<Material>(kit.piedraFacetada) { kit.ambar, kit.motivoLatente };
    }

    /// <summary>Grises entre 0,78 y 1 (con un leve corrimiento cálido o frío), un píxel por tono.</summary>
    static Texture2D TexturaFacetas()
    {
        string ruta = CarpetaTexturas + "Facetas.png";
        var tex = new Texture2D(LadoTexturaFacetas, LadoTexturaFacetas, TextureFormat.RGBA32, false);
        for (int y = 0; y < LadoTexturaFacetas; y++)
            for (int x = 0; x < LadoTexturaFacetas; x++)
            {
                float v = Mathf.Lerp(0.78f, 1f, Hash(x, y, 0, 11));
                float tinte = (Hash(x, y, 0, 12) - 0.5f) * 0.04f;
                tex.SetPixel(x, y, new Color(v * (1f + tinte), v, v * (1f - tinte), 1f));
            }
        tex.Apply();
        System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importador.filterMode = FilterMode.Point;     // sin mezclar píxeles: un tono limpio por cara
        importador.mipmapEnabled = false;
        importador.wrapMode = TextureWrapMode.Repeat;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    // ================================================================== aplicar

    /// <summary>Faceta todo lo que en la escena abierta use materiales de piedra o tallados.</summary>
    static void FacetarEscena(Kit kit)
    {
        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var raiz in escena.GetRootGameObjects())
            Facetar(kit, raiz);
    }

    /// <summary>Faceta un objeto y sus hijos (también sirve para piezas de prefabs).</summary>
    static void Facetar(Kit kit, GameObject raiz)
    {
        foreach (var filtro in raiz.GetComponentsInChildren<MeshFilter>(true))
        {
            var r = filtro.GetComponent<MeshRenderer>();
            var malla = filtro.sharedMesh;
            if (r == null || malla == null) continue;
            if (r.sharedMaterials.Length == 0 || !r.sharedMaterials.All(m => m != null && kit.facetables.Contains(m))) continue;
            // las mallas que ya salieron facetadas de acá (la tierra con el agujero) quedan como están
            if (AssetDatabase.GetAssetPath(malla) == RutaMallasFacetadas) continue;

            filtro.sharedMesh = EsCuboPrimitivo(malla)
                ? CajaFacetada(filtro.transform, PerfilDe(filtro.transform))
                : MallaPlana(malla);
        }
    }

    /// <summary>Elige el perfil por el nombre del objeto o del grupo en el que está.</summary>
    static Perfil PerfilDe(Transform t)
    {
        string nombre = t.name;
        if (nombre.StartsWith("Horizonte") || (t.parent != null && t.parent.name == "Horizonte")) return PerfilRoca;
        if (nombre.StartsWith("Cerro_")) return PerfilCerro;
        if (nombre == "Terreno" || nombre.StartsWith("Llano")) return PerfilSuelo;
        for (var p = t.parent; p != null; p = p.parent)
        {
            if (p.name == "Crater_Valle") return PerfilCrater;
            if (p.name.EndsWith("_Capilla")) return PerfilCapilla;
        }
        return PerfilNivel;
    }

    static bool EsCuboPrimitivo(Mesh m) => m.name == "Cube" && m.vertexCount == 24;

    // ================================================================== mallas

    /// <summary>
    /// Una caja de 1 × 1 × 1 (como el cubo de Unity, para que la escala del objeto la siga
    /// estirando) subdividida según su tamaño real y deformada por el ruido del mundo.
    /// </summary>
    static Mesh CajaFacetada(Transform t, Perfil perfil)
    {
        Vector3 escala = t.lossyScale;
        // subdivisiones por eje: las caras que comparten una arista usan el mismo número
        var n = new int[3];
        for (int eje = 0; eje < 3; eje++)
            n[eje] = Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(escala[eje]) / perfil.celda), 1, perfil.tope);

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        for (int eje = 0; eje < 3; eje++)
            for (int signo = -1; signo <= 1; signo += 2)
            {
                if (perfil.sinBase && eje == 1 && signo < 0) continue;
                // u × v = normal hacia afuera (así los triángulos quedan del lado de afuera)
                int a = (eje + 1) % 3, b = (eje + 2) % 3;
                if (signo < 0) { int c = a; a = b; b = c; }
                Vector3 normal = Vector3.zero; normal[eje] = signo * 0.5f;
                Vector3 u = Vector3.zero; u[a] = 1f;
                Vector3 v = Vector3.zero; v[b] = 1f;
                Vector3 origen = normal - u * 0.5f - v * 0.5f;
                int nu = n[a], nv = n[b];

                // vértices de la grilla, ya deformados (en coordenadas locales)
                var p = new Vector3[nu + 1, nv + 1];
                for (int i = 0; i <= nu; i++)
                    for (int j = 0; j <= nv; j++)
                    {
                        Vector3 local = origen + u * ((float)i / nu) + v * ((float)j / nv);
                        Vector3 mundo = t.TransformPoint(local);
                        p[i, j] = t.InverseTransformPoint(mundo + Desplazamiento(mundo, perfil.relieve, perfil.onda, perfil.semilla));
                    }

                for (int i = 0; i < nu; i++)
                    for (int j = 0; j < nv; j++)
                    {
                        Vector3 p00 = p[i, j], p10 = p[i + 1, j], p11 = p[i + 1, j + 1], p01 = p[i, j + 1];
                        Vector3 mundo = t.TransformPoint((p00 + p11) * 0.5f);
                        Vector2 uv = PixelDeFaceta(mundo);
                        // la diagonal se alterna al azar para que no se lea una grilla
                        if (Hash(Mathf.RoundToInt(mundo.x * 10f), Mathf.RoundToInt(mundo.y * 10f), Mathf.RoundToInt(mundo.z * 10f), 5) < 0.5f)
                        {
                            Triangulo(vertices, uvs, p00, p10, p11, uv);
                            Triangulo(vertices, uvs, p00, p11, p01, uv);
                        }
                        else
                        {
                            Triangulo(vertices, uvs, p00, p10, p01, uv);
                            Triangulo(vertices, uvs, p10, p11, p01, uv);
                        }
                    }
            }

        return GuardarMalla(CrearMalla(t.name + "_Facetado", vertices, uvs));
    }

    /// <summary>
    /// La misma malla con caras planas: cada triángulo con sus propios vértices y un tono.
    /// Se cachea por malla de origen (los módulos del kit se repiten mucho).
    /// </summary>
    static Mesh MallaPlana(Mesh origen)
    {
        if (mallasPlanas.TryGetValue(origen, out var hecha)) return hecha;

        var fuente = origen.vertices;
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var submallas = new List<int[]>();
        for (int sub = 0; sub < origen.subMeshCount; sub++)
        {
            var tri = origen.GetTriangles(sub);
            var propios = new int[tri.Length];
            for (int k = 0; k < tri.Length; k += 3)
            {
                Vector3 a = fuente[tri[k]], b = fuente[tri[k + 1]], c = fuente[tri[k + 2]];
                for (int i = 0; i < 3; i++) propios[k + i] = vertices.Count + i;
                Triangulo(vertices, uvs, a, b, c, PixelDeFaceta((a + b + c) / 3f * 7.3f));
            }
            submallas.Add(propios);
        }

        var malla = new Mesh { name = origen.name + "_Plano" };
        malla.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        malla.SetVertices(vertices);
        malla.SetUVs(0, uvs);
        malla.subMeshCount = submallas.Count;
        for (int sub = 0; sub < submallas.Count; sub++) malla.SetTriangles(submallas[sub], sub);
        malla.RecalculateNormals();   // sin vértices compartidos: normal de cara
        malla.RecalculateBounds();

        hecha = GuardarMalla(malla);
        mallasPlanas[origen] = hecha;
        return hecha;
    }

    static void Triangulo(List<Vector3> vertices, List<Vector2> uvs, Vector3 a, Vector3 b, Vector3 c, Vector2 uv)
    {
        vertices.Add(a); vertices.Add(b); vertices.Add(c);
        uvs.Add(uv); uvs.Add(uv); uvs.Add(uv);
    }

    static Mesh CrearMalla(string nombre, List<Vector3> vertices, List<Vector2> uvs)
    {
        var malla = new Mesh { name = nombre };
        malla.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        malla.SetVertices(vertices);
        malla.SetUVs(0, uvs);
        // cada triángulo usa tres vértices seguidos
        var indices = new int[vertices.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        malla.SetTriangles(indices, 0);
        malla.RecalculateNormals();   // sin vértices compartidos: normal de cara
        malla.RecalculateBounds();
        return malla;
    }

    static Mesh GuardarMalla(Mesh malla)
    {
        AssetDatabase.AddObjectToAsset(malla, contenedorMallas);
        return malla;
    }

    // ================================================================== ruido

    /// <summary>El centro de un píxel al azar de la textura de facetas, elegido por la posición.</summary>
    static Vector2 PixelDeFaceta(Vector3 p)
    {
        int x = Mathf.FloorToInt(Hash(Mathf.RoundToInt(p.x * 20f), Mathf.RoundToInt(p.y * 20f), Mathf.RoundToInt(p.z * 20f), 1) * LadoTexturaFacetas);
        int y = Mathf.FloorToInt(Hash(Mathf.RoundToInt(p.x * 20f), Mathf.RoundToInt(p.y * 20f), Mathf.RoundToInt(p.z * 20f), 2) * LadoTexturaFacetas);
        x = Mathf.Min(x, LadoTexturaFacetas - 1);
        y = Mathf.Min(y, LadoTexturaFacetas - 1);
        return new Vector2((x + 0.5f) / LadoTexturaFacetas, (y + 0.5f) / LadoTexturaFacetas);
    }

    /// <summary>Cuánto se corre un punto del mundo: dos capas de ruido, una amplia y una fina.</summary>
    static Vector3 Desplazamiento(Vector3 p, Vector3 relieve, float onda, int semilla)
    {
        Vector3 d = Vector3.zero;
        for (int eje = 0; eje < 3; eje++)
        {
            int s = semilla * 10 + eje;
            float amplio = Ruido(p / onda, s) * 0.7f;
            float fino = Ruido(p / (onda * 0.45f), s + 3) * 0.3f;
            d[eje] = (amplio + fino) * relieve[eje];
        }
        return d;
    }

    /// <summary>Ruido de valor 3D, continuo, entre -1 y 1.</summary>
    static float Ruido(Vector3 p, int semilla)
    {
        int x0 = Mathf.FloorToInt(p.x), y0 = Mathf.FloorToInt(p.y), z0 = Mathf.FloorToInt(p.z);
        float fx = Suave(p.x - x0), fy = Suave(p.y - y0), fz = Suave(p.z - z0);
        float Esquina(int dx, int dy, int dz) => Hash(x0 + dx, y0 + dy, z0 + dz, semilla);
        float x00 = Mathf.Lerp(Esquina(0, 0, 0), Esquina(1, 0, 0), fx);
        float x10 = Mathf.Lerp(Esquina(0, 1, 0), Esquina(1, 1, 0), fx);
        float x01 = Mathf.Lerp(Esquina(0, 0, 1), Esquina(1, 0, 1), fx);
        float x11 = Mathf.Lerp(Esquina(0, 1, 1), Esquina(1, 1, 1), fx);
        float v = Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
        return v * 2f - 1f;
    }

    static float Suave(float t) => t * t * (3f - 2f * t);

    /// <summary>Un número entre 0 y 1 que depende sólo de los enteros recibidos.</summary>
    static float Hash(int x, int y, int z, int semilla)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)z * 2246822519u + (uint)semilla * 3266489917u;
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777215f;
        }
    }
}
