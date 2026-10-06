using UnityEngine;
public class PolvoEnLuz : MonoBehaviour
{
    public Light fuente;
    public ParticleSystem polvo;
    void LateUpdate(){var e=polvo.emission;e.enabled=fuente!=null && fuente.isActiveAndEnabled && fuente.intensity>1;}
}
