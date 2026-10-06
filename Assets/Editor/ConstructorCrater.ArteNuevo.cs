using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public static partial class ConstructorCrater
{
    static void PisoAnular(Kit k, Transform padre, string nombre, float interior, float exterior,
        float altura, bool bordeCuadrado = false)
    {
        const int segmentos = 64;
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        for (int i = 0; i < segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / segmentos;
            var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            float r = bordeCuadrado ? exterior / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.z)) : exterior;
            vertices.Add(d * interior + Vector3.up * altura);
            vertices.Add(d * r + Vector3.up * altura);
            vertices.Add(d * interior + Vector3.down * 1.5f);
            vertices.Add(d * r + Vector3.down * 1.5f);
        }
        void Tri(int a, int b, int c) { indices.Add(a); indices.Add(b); indices.Add(c); }
        for (int i = 0; i < segmentos; i++)
        {
            int a = i * 4, b = ((i + 1) % segmentos) * 4;
            Tri(a, b + 1, a + 1); Tri(a, b, b + 1);
            Tri(a + 2, a + 3, b + 3); Tri(a + 2, b + 3, b + 2);
            if (interior > 0) { Tri(a, a + 2, b); Tri(a + 2, b + 2, b); }
            Tri(a + 1, b + 1, a + 3); Tri(a + 3, b + 1, b + 3);
        }
        var malla = new Mesh { name = nombre };
        malla.SetVertices(vertices); malla.SetTriangles(indices, 0);
        malla.RecalculateNormals(); malla.RecalculateBounds();
        malla.uv = new Vector2[vertices.Count];
        malla = GuardarMalla(malla);
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = CentroRotonda;
        // La malla física conserva escalones regulares; el acabado usa el mismo
        // ruido, normales planas y tonos por cara que la piedra del resto del nivel.
        var caras = new List<Vector3>();
        var tonos = new List<Vector2>();
        for (int i = 0; i < indices.Count; i += 3)
        {
            Vector3 a = vertices[indices[i]], b = vertices[indices[i + 1]], c = vertices[indices[i + 2]];
            if (a.y == altura && b.y == altura && c.y == altura) continue;
            Triangulo(caras, tonos, a, b, c, PixelDeFaceta(CentroRotonda + (a + b + c) / 3f));
        }
        const int sectores = 64;
        int bandas = Mathf.Max(1, Mathf.CeilToInt((exterior - interior) / PerfilNivel.celda));
        Vector3 Punto(int sector, int banda)
        {
            float angulo = sector * Mathf.PI * 2f / sectores;
            var direccion = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
            float radioExterior = bordeCuadrado
                ? exterior / Mathf.Max(Mathf.Abs(direccion.x), Mathf.Abs(direccion.z)) : exterior;
            float t = (float)banda / bandas;
            var punto = direccion * Mathf.Lerp(interior, radioExterior, t) + Vector3.up * altura;
            // Mantener unidas las aristas del hueco y las contrahuellas.
            float borde = Mathf.Sin(t * Mathf.PI);
            punto += Desplazamiento(CentroRotonda + punto, PerfilNivel.relieve,
                PerfilNivel.onda, PerfilNivel.semilla) * borde;
            return punto;
        }
        for (int i = 0; i < sectores; i++)
            for (int j = 0; j < bandas; j++)
            {
                Vector3 a = Punto(i, j), b = Punto(i + 1, j),
                    c = Punto(i + 1, j + 1), d = Punto(i, j + 1);
                void Cara(Vector3 p, Vector3 q, Vector3 r)
                {
                    if (Vector3.Cross(q - p, r - p).sqrMagnitude < 0.000001f) return;
                    Triangulo(caras, tonos, p, q, r, PixelDeFaceta(CentroRotonda + (p + q + r) / 3f));
                }
                if ((i + j) % 2 == 0) { Cara(a, c, d); Cara(a, b, c); }
                else { Cara(a, b, d); Cara(b, c, d); }
            }
        go.AddComponent<MeshFilter>().sharedMesh = GuardarMalla(CrearMalla(nombre + "_Facetado", caras, tonos));
        go.AddComponent<MeshRenderer>().sharedMaterial = k.piso;
        go.AddComponent<MeshCollider>().sharedMesh = malla;
    }
}
