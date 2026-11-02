using UnityEditor;
using UnityEngine;

public static partial class ConstructorCrater
{
    static void AjustarPuertaSolVisible(Kit k,Transform nivel)
    {
        var ancla=BuscarObjetoDocente("Ancla_O_Puerta").GetComponent<Ancla>();
        var puerta=BuscarObjetoDocente("Puerta_O_Ancla").GetComponent<Compuerta>();
        Vector3 P(Vector3 p)=>EspacioCrater.Punto(p+CorrimientoAlaOeste);
        var nuevo=P(new Vector3(-22f,0,19f));
        var mirar=P(new Vector3(-28,0,14));
        Vector3 delta=nuevo-ancla.transform.position;
        var rotacion=Quaternion.LookRotation((mirar-nuevo).normalized);
        float giro=Vector3.SignedAngle(ancla.transform.forward,rotacion*Vector3.forward,Vector3.up);
        ancla.transform.SetPositionAndRotation(nuevo,rotacion);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ancla.transform);
        var apoyo=BuscarObjetoDocente("Ancla_O_Puerta_Base");
        if(apoyo!=null)
        {
            apoyo.position+=delta;
            foreach(Transform pieza in apoyo)pieza.RotateAround(nuevo,Vector3.up,giro);
        }
        var anterior=BuscarObjetoDocente("Senal_Puerta_SOL");
        if(anterior!=null)Object.DestroyImmediate(anterior.gameObject);
        var g=Grupo(nivel,"Senal_Puerta_SOL");
        var material=k.ambar!=null?k.ambar:AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Ambar.mat");
        var centro=puerta.transform.position;
        var franja=GameObject.CreatePrimitive(PrimitiveType.Cube);franja.name="Luz_Bajo_Puerta_SOL";
        franja.transform.SetParent(g,false);
        franja.transform.position=new Vector3(centro.x-.24f,.07f,centro.z);
        franja.transform.localScale=new Vector3(.035f,.055f,3.1f*EspacioCrater.Escala);
        Object.DestroyImmediate(franja.GetComponent<Collider>());
        franja.GetComponent<Renderer>().sharedMaterial=material;Estatico(franja);
        var foco=Luz(g,"Resplandor_Umbral_SOL",new Vector3(centro.x-.6f,.10f,centro.z),LuzCalida,45,5,false);
        foco.type=LightType.Spot;foco.spotAngle=100;foco.innerSpotAngle=55;
        foco.transform.LookAt(centro);
        foco.shadows=LightShadows.None;
        ActualizarDibujosDeFiltros(k);
    }

    static void ActualizarDibujosDeFiltros(Kit k)
    {
        k.pinturaSol ??= AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PinturaSol.mat");
        k.pinturaLuna ??= AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PinturaLuna.mat");
        for(int i=0;i<2;i++)
        {
            string nombre=i==0?"Control_FiltroSol":"Control_FiltroLuna";
            var anterior=BuscarObjetoDocente(nombre);if(anterior==null)continue;
            var padre=anterior.parent;var normal=-anterior.forward;var punto=anterior.position-normal*.15f;
            Object.DestroyImmediate(anterior.gameObject);
            Losa(k,padre,nombre,punto,normal,2.2f,1.5f,i==0?(System.Action<Kit,Lienzo>)PanelFiltroSol:PanelFiltroLuna);
        }
    }
}
