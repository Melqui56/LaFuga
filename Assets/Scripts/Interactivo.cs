using UnityEngine;

public enum TipoInteractivo { Valvula, Puerta, Rueda }

// Valvulas, puerta de compuerta y rueda de cierre. La animacion es movimiento
// de la pieza (giro o desplazamiento) y siempre va con sonido.
public class Interactivo : MonoBehaviour
{
    public TipoInteractivo tipo;
    public string id;
    public GestorNivel gestor;
    public Transform visual;
    public AudioSource fuente;
    public AudioClip sonido;

    public bool usado;
    public float avance;          // 0 a 1, para animar
    float objetivo;
    Vector3 posicionInicial;
    bool animando;

    void Start()
    {
        if (visual != null) posicionInicial = visual.localPosition;
    }

    public void Activar()
    {
        if (usado) return;

        if (tipo == TipoInteractivo.Puerta)
        {
            if (gestor != null && !gestor.PuedeAbrirPuerta())
            {
                if (fuente != null && sonido != null) fuente.PlayOneShot(sonido, 0.4f);
                return;
            }
        }

        usado = true;
        animando = true;
        objetivo = 1f;
        if (fuente != null && sonido != null) fuente.PlayOneShot(sonido, 1f);
        if (gestor != null) gestor.InteractivoUsado(this);
    }

    public bool Disponible()
    {
        if (usado) return false;
        if (tipo == TipoInteractivo.Puerta && gestor != null && !gestor.PuedeAbrirPuerta())
            return false;
        return true;
    }

    void Update()
    {
        if (!animando) return;
        avance = Mathf.MoveTowards(avance, objetivo, Time.deltaTime * (tipo == TipoInteractivo.Rueda ? 0.9f : 0.55f));

        if (tipo == TipoInteractivo.Valvula)
            visual.localRotation = Quaternion.Euler(0f, 0f, 90f * avance);
        else if (tipo == TipoInteractivo.Puerta)
            visual.localPosition = posicionInicial + new Vector3(0f, 2.6f * avance, 0f);
        else
            visual.localRotation = Quaternion.Euler(0f, 0f, -720f * avance);

        if (avance >= 1f && tipo == TipoInteractivo.Rueda && gestor != null)
            gestor.CerrarCompuerta();
    }
}
