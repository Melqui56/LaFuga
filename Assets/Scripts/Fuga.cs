using UnityEngine;

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
        if (!sellada && punto != null)
        {
            fase += Time.deltaTime * 6f;
            float s = 1f + Mathf.Sin(fase) * 0.08f;
            punto.material.color = new Color(0.35f, 0.75f, 0.95f, 0.85f);
            if (chorro != null)
                chorro.transform.localScale = new Vector3(0.2f * s, 1.6f * s, 0.2f * s);
        }
    }

    public void Sellarse()
    {
        if (sellada) return;
        sellada = true;
        if (chorro != null) chorro.SetActive(false);
        var charco = transform.Find("Charco");
        if (charco != null) charco.gameObject.SetActive(false);
        if (fuente != null && sonido != null) fuente.PlayOneShot(sonido, 1f);
        if (gestor != null) gestor.FugaSellada();
    }
}
