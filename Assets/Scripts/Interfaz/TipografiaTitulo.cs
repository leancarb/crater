using UnityEngine;
using UnityEngine.UI;

/// <summary>Espaciado del logotipo sin cambiar el texto ni su acento.</summary>
[RequireComponent(typeof(Text))]
public class TipografiaTitulo : BaseMeshEffect
{
    public float espaciado = 100f;

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive()) return;
        int letras = vertices.currentVertCount / 4;
        var vertice = new UIVertex();
        for (int i = 0; i < letras; i++)
        {
            float desplazamiento = (i - (letras - 1) * .5f) * espaciado;
            for (int j = 0; j < 4; j++)
            {
                int indice = i * 4 + j;
                vertices.PopulateUIVertex(ref vertice, indice);
                vertice.position.x += desplazamiento;
                vertices.SetUIVertex(vertice, indice);
            }
        }
    }
}
