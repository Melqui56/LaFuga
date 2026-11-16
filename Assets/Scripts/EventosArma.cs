using UnityEngine;

public class EventosArma : MonoBehaviour
{
    public ArmaEnemigo arma;

    public void ActivarDano()
    {
        if (arma != null) arma.puedeHacerDano = true;
    }

    public void DesactivarDano()
    {
        if (arma != null) arma.puedeHacerDano = false;
    }
}
