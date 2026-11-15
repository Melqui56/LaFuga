using UnityEngine;

// Los animation events solo llaman metodos del objeto que tiene el Animator;
// este puente los reenvia al arma, que esta mas abajo en la jerarquia.
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
