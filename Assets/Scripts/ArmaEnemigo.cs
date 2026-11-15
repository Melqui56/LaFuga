using UnityEngine;

public class ArmaEnemigo : MonoBehaviour
{
    public float dano = 20f;
    public bool puedeHacerDano;

    void OnTriggerEnter(Collider otro)
    {
        if (!puedeHacerDano) return;
        var salud = otro.GetComponentInParent<SaludJugador>();
        if (salud == null) return;
        salud.RecibirDano(dano);
        puedeHacerDano = false;
    }

    void OnTriggerStay(Collider otro)
    {
        OnTriggerEnter(otro);
    }
}
