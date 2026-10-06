using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class ConstructorCrater
{
    static void AmpliarSubsuelo(Transform nivel, Transform arte)
    {
        var espacio = Grupo(nivel, "SUBSUELO_Ampliado");
        espacio.position = EspacioCrater.Origen;
        foreach (string nombre in new[] { "02_Umbral", "03_Rotonda", "03_Ala_Oeste", "03_Ala_Este", "04_Cruce", "05_Cresta" })
            nivel.Find(nombre).SetParent(espacio, true);
        var lore = arte.Find("Murales/Lore_Pasaje");
        if (lore != null) lore.SetParent(nivel.Find("01_Explanada"), true);
        arte.SetParent(espacio, true);

        // Las salas se ensanchan. Los objetos conservan proporciones, incluso
        // cuando están inclinados: salen de la jerarquía con escala no uniforme.
        var conservar = new List<(Transform t, Vector3 p, Quaternion q, Vector3 s)>();
        var planos = new List<(ParedEspejo plano, Vector2 dimensiones)>();
        var rampas = new List<(Transform t, Vector3 a, Vector3 b, float ancho, float grosor)>();
        foreach (var t in espacio.GetComponentsInChildren<Transform>(true))
        {
            bool hijo = false;
            foreach (var c in conservar) if (t.IsChildOf(c.t)) { hijo = true; break; }
            if (hijo) continue;
            if (t.name.StartsWith("Rampa_") && t.GetComponent<BoxCollider>() != null)
            {
                // PhysX no representa el sesgo de una caja inclinada bajo escala no uniforme.
                // Reconstruir desde los extremos transitables evita labios de colisión.
                Vector3 escala = t.lossyScale;
                Vector3 cara = t.position + t.up * escala.y * 0.5f;
                Vector3 semilargo = t.forward * (escala.z - 0.3f) * 0.5f;
                rampas.Add((t, EspacioCrater.Punto(cara - semilargo), EspacioCrater.Punto(cara + semilargo),
                    escala.x * EspacioCrater.Escala, escala.y));
                continue;
            }
            bool detalle = t.name.StartsWith("Cuenco_") || t.name == "Asiento_Gruta_O1" ||
                t.name == "Banco_Rotonda" || t.name == "Apoyo_Isla_Cruce" || t.name == "Piedras_Reparacion_Luna";
            bool mural = t.Find("Losa_Piedra") != null;
            var mf = t.GetComponent<MeshFilter>();
            bool figura = mf != null && mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Figura_");
            bool ancla = t.GetComponent<Ancla>() != null;
            bool recogible = t.GetComponent<Recogible>() != null;
            bool reja = t.GetComponent<MateriaHueca>() != null;
            var plano = t.GetComponent<ParedEspejo>();
            bool modelo = PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject) &&
                PrefabUtility.GetPrefabAssetType(t.gameObject) == PrefabAssetType.Model &&
                t.GetComponentInParent<Ancla>() == null && t.GetComponentInParent<Recogible>() == null &&
                t.GetComponentInParent<MateriaHueca>() == null;
            if (!ancla && !recogible && !reja && plano == null && !modelo && !mural && !figura && !detalle) continue;
            Vector3 pivote = t.position;
            if (modelo)
            {
                var rs = t.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    Bounds b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    pivote = new Vector3(b.center.x, t.position.y, b.center.z);
                }
                if (t.name == "Anillo_Del_Mapa") pivote = new Vector3(0, t.position.y, 12);
            }
            conservar.Add((t, EspacioCrater.Punto(pivote) + t.position - pivote, t.rotation,
                t.lossyScale * (reja ? EspacioCrater.Escala : 1f)));
            if (plano != null)
            {
                var so = new SerializedObject(plano);
                Vector2 tam = so.FindProperty("tamanio").vector2Value;
                Vector3 esc = new Vector3(EspacioCrater.Escala, 1, EspacioCrater.Escala);
                planos.Add((plano, new Vector2(tam.x * Vector3.Scale(t.right, esc).magnitude,
                    tam.y * Vector3.Scale(t.up, esc).magnitude)));
            }
        }
        espacio.localScale = new Vector3(EspacioCrater.Escala, 1, EspacioCrater.Escala);
        foreach (var luz in espacio.GetComponentsInChildren<Light>(true))
        {
            luz.range *= EspacioCrater.Escala;
            if (luz.type != LightType.Directional) luz.intensity *= EspacioCrater.Escala * EspacioCrater.Escala;
        }
        foreach (var fuente in espacio.GetComponentsInChildren<AudioSource>(true))
        {
            fuente.minDistance *= EspacioCrater.Escala;
            fuente.maxDistance *= EspacioCrater.Escala;
        }
        foreach (var receptor in espacio.GetComponentsInChildren<ReceptorDeLuz>(true))
            if (!receptor.permanente) receptor.retencion *= EspacioCrater.Escala;
        var objetos = Grupo(nivel, "Objetos_Sin_Distorsion");
        foreach (var c in conservar)
        {
            c.t.SetParent(objetos, true);
            c.t.SetPositionAndRotation(c.p, c.q);
            c.t.localScale = c.s;
        }
        foreach (var r in rampas)
        {
            Quaternion giro = Quaternion.LookRotation(r.b - r.a);
            r.t.SetParent(objetos, true);
            r.t.SetPositionAndRotation((r.a + r.b) * 0.5f - giro * Vector3.up * r.grosor * 0.5f, giro);
            r.t.localScale = new Vector3(r.ancho, r.grosor, Vector3.Distance(r.a, r.b) + 0.3f);
        }
        foreach (var p in planos)
        {
            var so = new SerializedObject(p.plano);
            so.FindProperty("tamanio").vector2Value = p.dimensiones;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
