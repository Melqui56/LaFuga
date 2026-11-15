using UnityEngine;

public class SaludJugador : MonoBehaviour
{
    public float maxima = 100f;
    public float invulnerableTras = 0.6f;

    public float Actual { get; private set; }
    public float UltimoGolpe { get; private set; } = -99f;

    GestorNivel gestor;

    void Awake()
    {
        Actual = maxima;
        gestor = Object.FindAnyObjectByType<GestorNivel>();
    }

    public void RecibirDano(float dano)
    {
        if (Time.time - UltimoGolpe < invulnerableTras || Actual <= 0f) return;
        UltimoGolpe = Time.time;
        Actual = Mathf.Max(0f, Actual - dano);
        Debug.Log("GOLPE salud=" + Actual);
        if (Actual <= 0f && gestor != null) gestor.DerrotaPorGuardia();
    }

    public void Restaurar()
    {
        Actual = maxima;
    }
}
