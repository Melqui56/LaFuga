using UnityEngine;

// Una fuga de agua en la pared. Se sella con el disparo: el chorro desaparece,
// suena el sello y la subida del agua se frena.
public class Fuga : MonoBehaviour
{
    public GestorNivel gestor;
    public GameObject chorro;
    public AudioSource fuente;
    public AudioClip sonido;

    public bool sellada;
    Renderer punto;
    float fase;

    void Start()
    {
        if (chorro != null) punto = chorro.GetComponent<Renderer>();
    }

    void Update()
    {
        // el chorro respira para que se vea de lejos
        if (!sellada && punto != null)
        {
            fase += Time.deltaTime * 6f;
            float s = 1f + Mathf.Sin(fase) * 0.08f;
            punto.material.color = new Color(0.35f, 0.75f, 0.95f, 0.85f);
            if (chorro != null)
                chorro.transform.localScale = new Vector3(0.16f * s, 1.5f * s, 0.16f * s);
        }
    }

    public void Sellarse()
    {
        if (sellada) return;
        sellada = true;
        if (chorro != null) chorro.SetActive(false);
        if (fuente != null && sonido != null) fuente.PlayOneShot(sonido, 1f);
        if (gestor != null) gestor.FugaSellada();
    }
}
