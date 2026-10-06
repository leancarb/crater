using UnityEngine;

public static partial class ConstructorCrater
{
    // Prototipo ambiental: al costado del recorrido O1, sin ocupar la escalera.
    static void ConstruirRinconGruta(Kit k, Transform red, float ancho)
    {
        Vector3 P(float x, float y, float z) => red.TransformPoint(new Vector3(x, y, z));
        float x = -ancho * 0.3f;
        var asiento = Bloque(red, "Asiento_Gruta_O1", P(x, -2.8f, -4.65f), new Vector3(1.6f, 0.4f, 0.75f), k.piedra);
        asiento.transform.rotation = red.rotation;
        Tronco(red, "Cuenco_Gruta_O1", P(x - 0.4f, -2.6f, -4.65f), MallaCuencoUsado(), k.cal);
        for (int i = 0; i < 3; i++)
        {
            var t = Grupo(red, "Tallado_Resonante_" + i);
            t.position = P(x + (i - 1) * 0.65f, -1.35f, -5.66f);
            t.rotation = red.rotation;
            var r = Glifo(t, "Sector", i == 1 ? Figura.Halo : Figura.LunaCreciente, t.position, red.forward, 0.42f, k.tallaLuna, false);
            var detector = t.gameObject.AddComponent<SphereCollider>(); detector.isTrigger = true; detector.radius = 0.23f;
            t.gameObject.layer = LayerMask.NameToLayer(CapaReceptor);
            var reaccion = t.gameObject.AddComponent<TalladoResonante>();
            reaccion.canalRequerido = FiltroDefinicion.Canal.Ninguno;
            reaccion.figura = r;
            reaccion.retencion = 0.5f;
            reaccion.resonancia = CrearFuenteHija(t, "Resonancia", k.audio.resonanciaSuave, 0);
            reaccion.resonancia.loop = true;
            reaccion.resonancia.pitch = new[] { 1f, 1.25f, 1.5f }[i];
            reaccion.resonancia.minDistance = 1;
            reaccion.resonancia.maxDistance = 5;
        }
        Luz(red, "Luz_Rincon_O1", P(x, -1.8f, -4.2f), LuzCalida, 6, 3, false);
    }

    static void DetallesDelRecorrido(Kit k, Transform nivel)
    {
        var oeste = nivel.Find("03_Ala_Oeste");
        // El mural existente y el borde del techo componen la revelación de los aros.
        // No se añade el foco sobre anclas colgantes que se había descartado.
        // Un rebote tenue del lado oculto aparece al rodear el tabique del sello.
        Luz(oeste, "Rebote_Sello_SOL", new Vector3(-15, 2.7f, 24) + CorrimientoAlaOeste, LuzCalida, 7, 4, true);
        var este = nivel.Find("03_Ala_Este");
        Losa(k, este, "Detalle_Luna_Reparo", new Vector3(34f, 1.8f, 0.84f) + CorrimientoAlaEste,
            Vector3.back, 2.4f, 1.6f, PanelReparoLuna);
        Bloque(este, "Piedras_Reparacion_Luna", new Vector3(34f, 0.17f, 0.15f) + CorrimientoAlaEste,
            new Vector3(0.6f, 0.34f, 0.7f), k.piedra);
        Luz(este, "Luz_Desvio_Luna", new Vector3(34f, 2.5f, -1.5f) + CorrimientoAlaEste, LuzCalida, 5, 3, true);
        // El espacio de entrada y sus dos rejas conserva contraste frente al fondo ciego.
        Luz(este, "Luz_Regreso_Luna", new Vector3(25.5f, 3.6f, 1) + CorrimientoAlaEste, LuzFria, 9, 4, true);
        var rotonda = nivel.Find("03_Rotonda");
        Bloque(rotonda, "Banco_Rotonda", new Vector3(-2.8f, 0.2f, 0.5f), new Vector3(2, 0.4f, 0.65f), k.piedra);
        // Desde el sector sur se puede detener y observar el eclipse, fuera del eje de paso.
        Tronco(rotonda, "Cuenco_Rotonda", new Vector3(-2.15f, 0.4f, 0.5f), MallaCuencoUsado(), k.cal);
        var cruce = nivel.Find("04_Cruce");
        Bloque(cruce, "Apoyo_Isla_Cruce", new Vector3(1.35f, 0.18f, 52.2f), new Vector3(0.55f, 0.36f, 0.65f), k.piedra);
        Tronco(cruce, "Cuenco_Isla", new Vector3(1.35f, 0.36f, 52.2f), MallaCuencoUsado(), k.cal);
    }

    static Mesh MallaCuencoUsado()
    {
        var v = new System.Collections.Generic.List<Vector3>();
        var uv = new System.Collections.Generic.List<Vector2>();
        Vector3 P(int i, float r, float y) => new Vector3(Mathf.Cos(i * Mathf.PI / 4) * r, y, Mathf.Sin(i * Mathf.PI / 4) * r);
        void T(Vector3 a, Vector3 b, Vector3 c) => Triangulo(v, uv, a, b, c, PixelDeFaceta((a + b + c) * 7));
        for (int i = 0; i < 8; i++)
        {
            var a = P(i, 0.13f, 0); var b = P(i + 1, 0.13f, 0);
            var c = P(i, 0.22f, 0.16f); var d = P(i + 1, 0.22f, 0.16f);
            var e = P(i, 0.18f, 0.15f); var f = P(i + 1, 0.18f, 0.15f);
            T(a, c, b); T(b, c, d); T(c, e, d); T(d, e, f);
            T(e, new Vector3(0, 0.045f, 0), f);
            T(a, b, Vector3.zero);
        }
        return GuardarMalla(CrearMalla("Cuenco_Usado", v, uv));
    }

    static void PanelReparoLuna(Kit k, Lienzo l)
    {
        var h = l.En(k.pinturaHueso);
        var azul = l.En(k.pinturaLuna);
        Persona(h, -0.72f, -0.55f, 0.7f, 0.06f, 0.2f, true);
        Rect(h, -0.4f, -0.58f, 0.85f, -0.52f);
        // La luna ilumina una junta del piso; se ven escalones detrás, sin mapa de ruta.
        Anillo(azul, new Vector2(0.15f, 0.35f), 0.19f, 0.22f);
        for (int i = 0; i < 3; i++) Rect(azul, 0.05f + i * 0.2f, -0.5f - i * 0.12f, 0.23f + i * 0.2f, -0.46f - i * 0.12f);
    }
}
