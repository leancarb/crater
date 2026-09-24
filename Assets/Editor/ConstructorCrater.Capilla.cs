using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// La capilla y su valle. Coordenadas locales al grupo 06_Capilla (x = 300):
/// la nave va de z -7 a 7, la puerta mira a +z y el cráter del valle está en z 34.
///
/// Capilla andina de adobe encalado: zócalo de piedra, contrafuertes, techo de paja
/// a dos aguas, espadaña con campana y cruz, óculo sobre la puerta. Adelante, el
/// atrio con pirca, arco de ingreso y cruz atrial. Alrededor, cardones, paja brava
/// y cerros.
///
/// CÓMO FUNCIONA
/// Todo se arma con piezas simples: cajas (CajaLocal), cilindros y prismas triangulares
/// (MallaPrisma, una malla creada por código para hastiales y remates). Las medidas
/// están en metros, en coordenadas locales de la capilla. El paisaje usa un generador
/// de números al azar con semilla fija: cada reconstrucción da exactamente el mismo valle.
/// </summary>
public static partial class ConstructorCrater
{
    const float AltoMuro = 4.45f;
    const float MitadNave = 4f;          // eje de los muros laterales
    const float PendienteTecho = 18f;    // grados

    static void ConstruirEdificioCapilla(Kit k, Transform g)
    {
        var edificio = Grupo(g, "Edificio");

        // ---------------------------------------------------------------- nave
        CajaLocal(edificio, "Piso_Capilla", new Vector3(0f, -0.1f, 0f), new Vector3(8f, 0.2f, 14f), k.piedraCapilla);
        CajaLocal(edificio, "Muro_Izq", new Vector3(-MitadNave, AltoMuro / 2f, 0f), new Vector3(0.35f, AltoMuro, 14.35f), k.cal);
        CajaLocal(edificio, "Muro_Der", new Vector3(MitadNave, AltoMuro / 2f, 0f), new Vector3(0.35f, AltoMuro, 14.35f), k.cal);
        CajaLocal(edificio, "Muro_Fondo", new Vector3(0f, AltoMuro / 2f, -7f), new Vector3(8f, AltoMuro, 0.35f), k.cal);
        // fachada con la puerta (x -1,2 … 1,2, hasta y 3,15)
        CajaLocal(edificio, "Fachada_Izq", new Vector3(-2.6f, AltoMuro / 2f, 7f), new Vector3(2.8f, AltoMuro, 0.35f), k.cal);
        CajaLocal(edificio, "Fachada_Der", new Vector3(2.6f, AltoMuro / 2f, 7f), new Vector3(2.8f, AltoMuro, 0.35f), k.cal);
        CajaLocal(edificio, "Fachada_Dintel", new Vector3(0f, 3.8f, 7f), new Vector3(2.4f, 1.3f, 0.35f), k.cal);

        // zócalo de piedra, un poco más grueso que el muro
        CajaLocal(edificio, "Zocalo_Izq", new Vector3(-MitadNave, 0.3f, 0f), new Vector3(0.5f, 0.6f, 14.5f), k.piedraCapilla);
        CajaLocal(edificio, "Zocalo_Der", new Vector3(MitadNave, 0.3f, 0f), new Vector3(0.5f, 0.6f, 14.5f), k.piedraCapilla);
        CajaLocal(edificio, "Zocalo_Fondo", new Vector3(0f, 0.3f, -7f), new Vector3(8.5f, 0.6f, 0.5f), k.piedraCapilla);
        CajaLocal(edificio, "Zocalo_Fachada_Izq", new Vector3(-2.65f, 0.3f, 7f), new Vector3(2.9f, 0.6f, 0.5f), k.piedraCapilla);
        CajaLocal(edificio, "Zocalo_Fachada_Der", new Vector3(2.65f, 0.3f, 7f), new Vector3(2.9f, 0.6f, 0.5f), k.piedraCapilla);

        // contrafuertes: dos cuerpos, el de abajo más ancho
        foreach (float lado in new[] { -1f, 1f })
            foreach (float z in new[] { -6.6f, -2.2f, 2.2f, 6.6f })
            {
                float x = lado * (MitadNave + 0.45f);
                CajaLocal(edificio, "Contrafuerte", new Vector3(x, 1.2f, z), new Vector3(0.6f, 2.4f, 0.75f), k.cal);
                CajaLocal(edificio, "Contrafuerte_Remate", new Vector3(lado * (MitadNave + 0.33f), 2.9f, z), new Vector3(0.36f, 1f, 0.6f), k.cal);
            }

        // hastiales: los triángulos de adelante y atrás que cierran el techo
        float altoHastial = MitadNave * Mathf.Tan(PendienteTecho * Mathf.Deg2Rad);
        Prisma(edificio, "Hastial_Fachada", new Vector3(0f, AltoMuro, 7f), new Vector3(8.35f, altoHastial, 0.35f), k.cal);
        Prisma(edificio, "Hastial_Fondo", new Vector3(0f, AltoMuro, -7f), new Vector3(8.35f, altoHastial, 0.35f), k.cal);

        // ---------------------------------------------------------------- techo de paja
        // cada faldón es una caja inclinada 'PendienteTecho' grados; el alero sobresale del muro
        const float alero = 0.6f;
        float mitad = MitadNave + alero;
        float largoFaldon = mitad / Mathf.Cos(PendienteTecho * Mathf.Deg2Rad);
        float cumbrera = AltoMuro + MitadNave * Mathf.Tan(PendienteTecho * Mathf.Deg2Rad);
        float yAlero = cumbrera - mitad * Mathf.Tan(PendienteTecho * Mathf.Deg2Rad);
        foreach (float lado in new[] { -1f, 1f })
        {
            var faldon = CajaLocal(edificio, lado < 0 ? "Techo_Izq" : "Techo_Der",
                new Vector3(lado * mitad / 2f, (cumbrera + yAlero) / 2f + 0.18f, 0f), new Vector3(largoFaldon, 0.35f, 14.8f), k.paja);
            faldon.transform.localRotation = Quaternion.Euler(0f, 0f, -lado * PendienteTecho);
        }
        CajaLocal(edificio, "Cumbrera", new Vector3(0f, cumbrera + 0.36f, 0f), new Vector3(0.45f, 0.3f, 15f), k.paja);

        // ---------------------------------------------------------------- espadaña
        float baseEsp = cumbrera - 0.2f;
        CajaLocal(edificio, "Espadana_Base", new Vector3(0f, baseEsp + 0.55f, 7.2f), new Vector3(3.2f, 1.5f, 0.6f), k.cal);
        CajaLocal(edificio, "Espadana_Pilar_Izq", new Vector3(-1.25f, baseEsp + 2f, 7.2f), new Vector3(0.7f, 1.4f, 0.6f), k.cal);
        CajaLocal(edificio, "Espadana_Pilar_Der", new Vector3(1.25f, baseEsp + 2f, 7.2f), new Vector3(0.7f, 1.4f, 0.6f), k.cal);
        CajaLocal(edificio, "Espadana_Dintel", new Vector3(0f, baseEsp + 2.95f, 7.2f), new Vector3(3.2f, 0.5f, 0.6f), k.cal);
        Prisma(edificio, "Espadana_Remate", new Vector3(0f, baseEsp + 3.2f, 7.2f), new Vector3(3.2f, 0.8f, 0.6f), k.cal);
        CajaLocal(edificio, "Cruz_V", new Vector3(0f, baseEsp + 4.45f, 7.2f), new Vector3(0.14f, 1f, 0.14f), k.madera);
        CajaLocal(edificio, "Cruz_H", new Vector3(0f, baseEsp + 4.65f, 7.2f), new Vector3(0.6f, 0.14f, 0.14f), k.madera);
        CajaLocal(edificio, "Yugo", new Vector3(0f, baseEsp + 2.55f, 7.2f), new Vector3(1.9f, 0.16f, 0.16f), k.madera);
        Cilindro(edificio, "Campana", new Vector3(0f, baseEsp + 2.1f, 7.2f), new Vector3(0.55f, 0.3f, 0.55f), k.metalGastado);
        Cilindro(edificio, "Campana_Boca", new Vector3(0f, baseEsp + 1.82f, 7.2f), new Vector3(0.72f, 0.05f, 0.72f), k.metalGastado);

        // ---------------------------------------------------------------- puerta y óculo
        CajaLocal(edificio, "Marco_Izq", new Vector3(-1.25f, 1.55f, 7.2f), new Vector3(0.12f, 3.2f, 0.1f), k.madera);
        CajaLocal(edificio, "Marco_Der", new Vector3(1.25f, 1.55f, 7.2f), new Vector3(0.12f, 3.2f, 0.1f), k.madera);
        CajaLocal(edificio, "Marco_Dintel", new Vector3(0f, 3.2f, 7.2f), new Vector3(2.62f, 0.14f, 0.1f), k.madera);
        // hojas abiertas hacia adentro, contra el vano
        CajaLocal(edificio, "Hoja_Izq", new Vector3(-1.12f, 1.5f, 6.25f), new Vector3(0.08f, 3f, 1.15f), k.madera);
        CajaLocal(edificio, "Hoja_Der", new Vector3(1.12f, 1.5f, 6.25f), new Vector3(0.08f, 3f, 1.15f), k.madera);
        var oculo = Cilindro(edificio, "Oculo_Fachada", new Vector3(0f, 3.85f, 7.19f), new Vector3(0.7f, 0.02f, 0.7f), k.techo);
        oculo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var aro = Cilindro(edificio, "Oculo_Aro", new Vector3(0f, 3.85f, 7.18f), new Vector3(0.86f, 0.015f, 0.86f), k.madera);
        aro.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // ventanas altas: nichos oscuros con alféizar de madera
        foreach (float lado in new[] { -1f, 1f })
            foreach (float z in new[] { -4.4f, 0f, 4.4f })
            {
                float x = lado * (MitadNave + 0.18f);
                CajaLocal(edificio, "Ventana", new Vector3(x, 3.1f, z), new Vector3(0.04f, 1f, 0.6f), k.techo);
                CajaLocal(edificio, "Alfeizar", new Vector3(x, 2.55f, z), new Vector3(0.14f, 0.08f, 0.75f), k.madera);
            }

        // ---------------------------------------------------------------- interior
        foreach (float z in new[] { -5.5f, -3f, -0.5f, 2f, 4.5f })
            CajaLocal(edificio, "Viga", new Vector3(0f, AltoMuro - 0.1f, z), new Vector3(7.7f, 0.2f, 0.2f), k.madera);

        // retablo: marco de madera, fondo oscuro, cruz
        CajaLocal(edificio, "Retablo_Fondo", new Vector3(0f, 2.3f, -6.78f), new Vector3(2.4f, 2.8f, 0.06f), k.techo);
        CajaLocal(edificio, "Retablo_Marco_Izq", new Vector3(-1.25f, 2.3f, -6.74f), new Vector3(0.14f, 3f, 0.14f), k.madera);
        CajaLocal(edificio, "Retablo_Marco_Der", new Vector3(1.25f, 2.3f, -6.74f), new Vector3(0.14f, 3f, 0.14f), k.madera);
        CajaLocal(edificio, "Retablo_Marco_Sup", new Vector3(0f, 3.8f, -6.74f), new Vector3(2.64f, 0.16f, 0.14f), k.madera);
        Prisma(edificio, "Retablo_Remate", new Vector3(0f, 3.88f, -6.74f), new Vector3(2.64f, 0.5f, 0.14f), k.madera);
        CajaLocal(edificio, "Retablo_Cruz_V", new Vector3(0f, 2.55f, -6.7f), new Vector3(0.1f, 1.1f, 0.06f), k.metalGastado);
        CajaLocal(edificio, "Retablo_Cruz_H", new Vector3(0f, 2.8f, -6.7f), new Vector3(0.55f, 0.1f, 0.06f), k.metalGastado);

        CajaLocal(edificio, "Altar", new Vector3(0f, 0.5f, -6f), new Vector3(1.9f, 1f, 0.8f), k.cal);
        CajaLocal(edificio, "Altar_Tapa", new Vector3(0f, 1.03f, -6f), new Vector3(2.05f, 0.08f, 0.95f), k.piedraCapilla);
        foreach (float x in new[] { -0.7f, 0.7f })
        {
            CajaLocal(edificio, "Vela", new Vector3(x, 1.22f, -6.1f), new Vector3(0.07f, 0.3f, 0.07f), k.vela);
            var llama = Luz(edificio, "Luz_Vela", Vector3.zero, new Color(1f, 0.62f, 0.3f), 4f, 3.5f, false);
            llama.transform.localPosition = new Vector3(x, 1.5f, -5.95f);
        }

        // bancos a los dos lados del pasillo central
        foreach (float z in new[] { -3.6f, -2.2f, -0.8f, 0.6f, 2f, 3.4f })
            foreach (float lado in new[] { -1f, 1f })
            {
                float x = lado * 2.15f;
                CajaLocal(edificio, "Banco_Asiento", new Vector3(x, 0.45f, z), new Vector3(2.5f, 0.08f, 0.42f), k.madera);
                CajaLocal(edificio, "Banco_Respaldo", new Vector3(x, 0.8f, z + 0.2f), new Vector3(2.5f, 0.5f, 0.06f), k.madera);
                CajaLocal(edificio, "Banco_Pata_A", new Vector3(x - 1.1f, 0.22f, z), new Vector3(0.08f, 0.45f, 0.38f), k.madera);
                CajaLocal(edificio, "Banco_Pata_B", new Vector3(x + 1.1f, 0.22f, z), new Vector3(0.08f, 0.45f, 0.38f), k.madera);
            }

        var interior = Luz(edificio, "Luz_Interior", Vector3.zero, new Color(1f, 0.8f, 0.6f), 45f, 12f, false);
        interior.transform.localPosition = new Vector3(0f, 3.5f, -1f);

        // ---------------------------------------------------------------- atrio
        var atrio = Grupo(g, "Atrio");
        CajaLocal(atrio, "Piso_Atrio", new Vector3(0f, -0.1f, 11.1f), new Vector3(7.8f, 0.2f, 8f), k.piedraCapilla);
        CajaLocal(atrio, "Pirca_Izq", new Vector3(-3.8f, 0.45f, 11.1f), new Vector3(0.4f, 1.1f, 8f), k.cal);
        CajaLocal(atrio, "Pirca_Der", new Vector3(3.8f, 0.45f, 11.1f), new Vector3(0.4f, 1.1f, 8f), k.cal);
        CajaLocal(atrio, "Pirca_Frente_Izq", new Vector3(-2.65f, 0.45f, 15f), new Vector3(2.7f, 1.1f, 0.4f), k.cal);
        CajaLocal(atrio, "Pirca_Frente_Der", new Vector3(2.65f, 0.45f, 15f), new Vector3(2.7f, 1.1f, 0.4f), k.cal);
        foreach (float x in new[] { -3.8f, 3.8f, -1.45f, 1.45f })
            CajaLocal(atrio, "Pirca_Remate", new Vector3(x, 1.08f, 15f), new Vector3(0.5f, 0.18f, 0.5f), k.piedraCapilla);

        // arco de ingreso
        CajaLocal(atrio, "Arco_Pilar_Izq", new Vector3(-1.45f, 1.3f, 15f), new Vector3(0.5f, 2.8f, 0.5f), k.cal);
        CajaLocal(atrio, "Arco_Pilar_Der", new Vector3(1.45f, 1.3f, 15f), new Vector3(0.5f, 2.8f, 0.5f), k.cal);
        CajaLocal(atrio, "Arco_Dintel", new Vector3(0f, 2.95f, 15f), new Vector3(3.4f, 0.5f, 0.5f), k.cal);
        Prisma(atrio, "Arco_Remate", new Vector3(0f, 3.2f, 15f), new Vector3(3.4f, 0.6f, 0.5f), k.cal);
        CajaLocal(atrio, "Arco_Cruz_V", new Vector3(0f, 4.15f, 15f), new Vector3(0.1f, 0.7f, 0.1f), k.madera);
        CajaLocal(atrio, "Arco_Cruz_H", new Vector3(0f, 4.3f, 15f), new Vector3(0.4f, 0.1f, 0.1f), k.madera);

        // cruz atrial sobre gradas, a un costado del camino
        CajaLocal(atrio, "Grada_1", new Vector3(-2.3f, 0.12f, 12.2f), new Vector3(1.5f, 0.24f, 1.5f), k.piedraCapilla);
        CajaLocal(atrio, "Grada_2", new Vector3(-2.3f, 0.36f, 12.2f), new Vector3(1.05f, 0.24f, 1.05f), k.piedraCapilla);
        CajaLocal(atrio, "Cruz_Atrial_V", new Vector3(-2.3f, 1.6f, 12.2f), new Vector3(0.16f, 2.3f, 0.16f), k.madera);
        CajaLocal(atrio, "Cruz_Atrial_H", new Vector3(-2.3f, 2.2f, 12.2f), new Vector3(0.9f, 0.16f, 0.16f), k.madera);

        // senda hasta el valle
        var senda = CajaLocal(atrio, "Senda", new Vector3(0f, -0.09f, 20f), new Vector3(1.8f, 0.02f, 10f), k.piedraCapilla);
        Object.DestroyImmediate(senda.GetComponent<Collider>());
    }

    static void ConstruirPaisajeCapilla(Kit k, Transform g)
    {
        var paisaje = Grupo(g, "Paisaje");
        CajaLocal(paisaje, "Terreno", new Vector3(0f, -0.35f, 30f), new Vector3(60f, 0.5f, 50f), k.tierra);
        // el piso sigue más allá del terreno jugable, sin colisión
        var llano = CajaLocal(paisaje, "Llano", new Vector3(0f, -0.4f, 30f), new Vector3(420f, 0.4f, 420f), k.tierra);
        Object.DestroyImmediate(llano.GetComponent<Collider>());

        var azar = new System.Random(23);
        float Azar(float min, float max) => min + (float)azar.NextDouble() * (max - min);

        // cerros facetados que cierran el valle (con colisión: son el borde jugable)
        void Cerro(string nombre, Vector3 centro, float ancho, float alto, float fondo, bool colision)
        {
            for (int i = 0; i < 3; i++)
            {
                var pieza = CajaLocal(paisaje, $"{nombre}_{i}",
                    centro + new Vector3(Azar(-ancho, ancho) * 0.3f, alto / 2f - 1.5f - i * 0.8f, Azar(-fondo, fondo) * 0.3f),
                    new Vector3(ancho * Azar(0.6f, 1f), alto * Azar(0.55f, 1f), fondo * Azar(0.6f, 1f)),
                    i == 1 ? k.huella : k.tierra);
                pieza.transform.localRotation = Quaternion.Euler(Azar(-6f, 6f), Azar(-30f, 30f), Azar(-8f, 8f));
                if (!colision) Object.DestroyImmediate(pieza.GetComponent<Collider>());
            }
        }
        Cerro("Cerro_A", new Vector3(-16f, 0f, 50f), 22f, 8f, 7f, true);
        Cerro("Cerro_B", new Vector3(14f, 0f, 53f), 20f, 6f, 7f, true);
        Cerro("Cerro_C", new Vector3(-31f, 0f, 30f), 7f, 9f, 40f, true);
        Cerro("Cerro_D", new Vector3(31f, 0f, 28f), 7f, 7f, 40f, true);
        Cerro("Cerro_E", new Vector3(0f, 0f, -15f), 34f, 5f, 6f, true);

        // horizonte: cordones lejanos, sin colisión
        for (float ang = 0f; ang < 360f; ang += Azar(14f, 22f))
        {
            float r = Azar(95f, 150f);
            var pos = new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad) * r, 0f, 25f + Mathf.Cos(ang * Mathf.Deg2Rad) * r);
            Cerro($"Horizonte_{ang:000}", pos, Azar(30f, 55f), Azar(18f, 42f), Azar(14f, 24f), false);
        }

        // cardones y paja brava, fuera del camino y del cráter
        bool Libre(Vector3 p) =>
            Mathf.Abs(p.x) > 5.5f &&                                        // camino y capilla
            Vector3.Distance(p, new Vector3(0f, 0f, 34f)) > 11f &&          // cráter
            !(Mathf.Abs(p.x) < 6f && p.z < 17f);                            // atrio
        int cardones = 0, intentos = 0;
        while (cardones < 16 && intentos++ < 200)
        {
            var p = new Vector3(Azar(-26f, 26f), 0f, Azar(-6f, 50f));
            if (!Libre(p)) continue;
            Cardon(k, paisaje, $"Cardon_{cardones++:00}", p, Azar(2.2f, 4.2f), azar);
        }
        int matas = 0;
        intentos = 0;
        while (matas < 60 && intentos++ < 400)
        {
            var p = new Vector3(Azar(-28f, 28f), 0f, Azar(-8f, 54f));
            if (!Libre(p)) continue;
            PajaBrava(k, paisaje, $"Paja_{matas++:00}", p, azar);
        }
    }

    static void Cardon(Kit k, Transform padre, string nombre, Vector3 pie, float alto, System.Random azar)
    {
        var raiz = Grupo(padre, nombre);
        raiz.position = padre.TransformPoint(pie);
        var tronco = Cilindro(raiz, "Tronco", Vector3.up * (alto / 2f - 0.1f), new Vector3(0.42f, alto / 2f, 0.42f), k.cardon);
        tronco.AddComponent<CapsuleCollider>();
        int brazos = azar.Next(0, 3);
        for (int i = 0; i < brazos; i++)
        {
            float lado = i == 0 ? 1f : -1f;
            float h = alto * (0.35f + (float)azar.NextDouble() * 0.25f);
            float largoBrazo = alto * (0.3f + (float)azar.NextDouble() * 0.25f);
            var codo = Cilindro(raiz, "Codo", new Vector3(lado * 0.35f, h, 0f), new Vector3(0.3f, 0.3f, 0.3f), k.cardon);
            codo.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Cilindro(raiz, "Brazo", new Vector3(lado * 0.6f, h + largoBrazo / 2f, 0f), new Vector3(0.3f, largoBrazo / 2f, 0.3f), k.cardon);
        }
        raiz.localRotation = Quaternion.Euler(0f, (float)azar.NextDouble() * 360f, 0f);
    }

    static void PajaBrava(Kit k, Transform padre, string nombre, Vector3 pie, System.Random azar)
    {
        var raiz = Grupo(padre, nombre);
        raiz.position = padre.TransformPoint(pie);
        for (int i = 0; i < 4; i++)
        {
            var hoja = CajaLocal(raiz, "Hoja", Vector3.up * 0.25f, new Vector3(0.05f, 0.6f + (float)azar.NextDouble() * 0.3f, 0.3f), k.pajaBrava);
            hoja.transform.localRotation = Quaternion.Euler((float)azar.NextDouble() * 30f - 15f, i * 45f + (float)azar.NextDouble() * 20f, (float)azar.NextDouble() * 30f - 15f);
            Object.DestroyImmediate(hoja.GetComponent<Collider>());
            hoja.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    // ---------------------------------------------------------------- piezas

    static GameObject Cilindro(Transform padre, string nombre, Vector3 centroLocal, Vector3 escala, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = nombre;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.position = padre.TransformPoint(centroLocal);
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = m;
        Estatico(go);
        return go;
    }

    /// <summary>Prisma triangular (hastiales, remates). 'baseLocal' es el centro de la base.</summary>
    static GameObject Prisma(Transform padre, string nombre, Vector3 baseLocal, Vector3 tamanio, Material m)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = padre.TransformPoint(baseLocal);
        go.transform.localScale = tamanio;
        go.AddComponent<MeshFilter>().sharedMesh = MallaPrisma();
        go.AddComponent<MeshRenderer>().sharedMaterial = m;
        Estatico(go);
        return go;
    }

    /// <summary>
    /// Prisma de base 1×1 y alto 1, con la base en y = 0 y centrado en x y z.
    /// Caras planas (vértices propios por cara) para que se lea facetado. Se guarda
    /// como asset para que la escena lo referencie.
    /// </summary>
    static Mesh MallaPrisma()
    {
        const string ruta = "Assets/Models/Generado/Prisma.asset";
        var malla = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if (malla != null) return malla;
        AsegurarCarpeta("Assets/Models");
        AsegurarCarpeta("Assets/Models/Generado");

        Vector3 a0 = new Vector3(-0.5f, 0f, -0.5f), b0 = new Vector3(0.5f, 0f, -0.5f), c0 = new Vector3(0f, 1f, -0.5f);
        Vector3 a1 = new Vector3(-0.5f, 0f, 0.5f), b1 = new Vector3(0.5f, 0f, 0.5f), c1 = new Vector3(0f, 1f, 0.5f);
        var centro = new Vector3(0f, 1f / 3f, 0f);
        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangulos = new System.Collections.Generic.List<int>();

        void Cara(params Vector3[] v)
        {
            // orientar hacia afuera: Unity dibuja las caras en sentido horario
            Vector3 normal = Vector3.Cross(v[1] - v[0], v[2] - v[0]);
            Vector3 medio = Vector3.zero;
            foreach (var p in v) medio += p;
            medio /= v.Length;
            if (Vector3.Dot(normal, medio - centro) < 0f) System.Array.Reverse(v);
            int i0 = vertices.Count;
            vertices.AddRange(v);
            for (int i = 1; i < v.Length - 1; i++) triangulos.AddRange(new[] { i0, i0 + i, i0 + i + 1 });
        }

        Cara(a0, b0, c0);          // frente
        Cara(a1, b1, c1);          // atrás
        Cara(a0, a1, b1, b0);      // base
        Cara(a0, c0, c1, a1);      // faldón izquierdo
        Cara(b0, b1, c1, c0);      // faldón derecho

        malla = new Mesh { name = "Prisma" };
        malla.SetVertices(vertices);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        AssetDatabase.CreateAsset(malla, ruta);
        return malla;
    }
}
