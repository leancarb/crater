using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class ConstructorCrater
{
    static void IteracionDiseno(Kit k,Transform nivel)
    {
        var exterior=nivel.Find("06_Capilla");
        var inicio=exterior.Find("Inicio_Elevado_Kit");
        for(int tramo=1;tramo<CaminoBajadaInicial.Length;tramo++)
        {
            var a=CaminoBajadaInicial[tramo-1];var b=CaminoBajadaInicial[tramo];
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x);
            int piezas=Mathf.CeilToInt(Vector2.Distance(a,b)/1.5f);
            for(int i=0;i<piezas;i++)foreach(float lado in new[]{-1f,1f})
            {
                var p=Vector2.Lerp(a,b,(i+.5f)/piezas)+n*lado*3.4f;
                var baranda=CajaLocal(inicio,"Pretil_Bajada",new Vector3(p.x,AlturaInicioElevado(p.x,p.y)+.65f,p.y),
                    new Vector3(.35f,1.3f,Vector2.Distance(a,b)/piezas+.12f),k.piedra);
                baranda.transform.rotation=exterior.rotation*Quaternion.LookRotation(new Vector3(d.x,0,d.y));
            }
        }
        // Hombros bajos y abiertos al eje: no invaden la silueta solar.
        foreach(float lado in new[]{-1f,1f})for(int i=0;i<7;i++)
        {
            float z=85+i*7;float x=lado*9;
            CajaLocal(inicio,"Lomada_Encuadre_Eclipse",new Vector3(x,AlturaInicioElevado(x,z)+.35f,z),new Vector3(5,.7f,8),k.tierra);
        }
        var spawn=exterior.Find("SpawnValle");spawn.localPosition=new Vector3(4,AlturaInicioElevado(4,184)+.05f,184);
        spawn.localRotation=Quaternion.Euler(0,155,0);
        var jugador=Object.FindFirstObjectByType<JugadorFPS>();jugador.transform.SetPositionAndRotation(spawn.position,spawn.rotation);
        var cielo=new SerializedObject(Object.FindFirstObjectByType<CieloEclipse>());
        cielo.FindProperty("diametroAngular").floatValue=9.5f;cielo.ApplyModifiedPropertiesWithoutUndo();
        var recogible=Object.FindObjectsByType<Recogible>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(r=>r.name=="Recogible_Linterna");
        var entrada=Grupo(nivel,"Cierre_Entrada_Templo");
        entrada.position=EspacioCrater.Punto(new Vector3(0,2,-3.2f));
        var losa=GameObject.CreatePrimitive(PrimitiveType.Cube);losa.transform.SetParent(entrada,false);
        losa.name="Losa_Entrada";losa.transform.localScale=new Vector3(15,4,.45f);losa.GetComponent<Renderer>().sharedMaterial=k.basalto;
        var puerta=entrada.gameObject.AddComponent<Compuerta>();puerta.iniciaAbierta=true;puerta.abrirSoloPorOrden=true;
        puerta.desplazamiento=Vector3.down*4.5f;puerta.duracion=1.2f;
        puerta.sonido=CrearFuenteHija(entrada,"Cierre_Piedra",k.audio.compuerta,.7f);
        var llegada=Grupo(nivel,"Llegada_Rotonda").gameObject.AddComponent<LlegadaRotonda>();
        llegada.farol=Object.FindFirstObjectByType<LinternaController>();llegada.entrada=puerta;
        var rotonda=BuscarObjetoDocente("03_Rotonda");
        llegada.luces=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(l=>l.intensity>0 && (l.transform.IsChildOf(rotonda)||l.name=="Luz_Pasillo_O"||l.name=="Luz_Pasillo_E")&&!l.transform.IsChildOf(recogible.transform)).ToArray();
        var pieza=BuscarObjetoDocente("Pieza_exterior_mirador");
        if(pieza!=null)pieza.position=EspacioCrater.Punto(new Vector3(-4,.3f,-12));
        var atajo=Object.FindFirstObjectByType<AtajoOcultoInicio>();if(atajo!=null)atajo.gameObject.SetActive(false);
        var vis=Grupo(nivel,"Visibilidad_Exterior").gameObject.AddComponent<VisibilidadExterior>();
        vis.paisaje=exterior.GetComponentsInChildren<Renderer>(true).Where(r=>!r.name.Contains("Disco")&&!r.name.Contains("Corona")&&!r.name.Contains("Cielo")).ToArray();
    }
}
