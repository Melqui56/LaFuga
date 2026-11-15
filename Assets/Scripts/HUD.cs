using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public SaludJugador salud;
    public GestorNivel gestor;
    public RectTransform barraSalud;
    public Image rellenoSalud;
    public RectTransform barraAgua;
    public Image rellenoAgua;
    public Text textoSalud;
    public Text textoAgua;
    public Text textoValvulas;
    public Text textoObjetivo;
    public Image destelloGolpe;

    static readonly Color SaludAlta = new Color(0.30f, 0.85f, 0.40f);
    static readonly Color SaludBaja = new Color(0.95f, 0.25f, 0.15f);

    void Update()
    {
        if (salud != null)
        {
            float f = salud.Actual / salud.maxima;
            barraSalud.localScale = new Vector3(f, 1f, 1f);
            rellenoSalud.color = Color.Lerp(SaludBaja, SaludAlta, f);
            textoSalud.text = "SALUD " + Mathf.CeilToInt(salud.Actual);
            float desde = Time.time - salud.UltimoGolpe;
            destelloGolpe.color = new Color(0.8f, 0f, 0f, Mathf.Clamp01(0.45f - desde));
        }
        if (gestor != null)
        {
            float margen = Mathf.Clamp(-gestor.AguaSobrePies(), 0f, 2.5f);
            float riesgo = 1f - margen / 2.5f;
            barraAgua.localScale = new Vector3(gestor.CompuertaCerrada ? 0f : riesgo, 1f, 1f);
            rellenoAgua.color = Color.Lerp(new Color(0.2f, 0.6f, 0.95f), new Color(0.95f, 0.25f, 0.15f), riesgo);
            textoAgua.text = gestor.CompuertaCerrada ? "AGUA  CONTROLADA" : "AGUA  " + Mathf.RoundToInt(riesgo * 100f) + "%";
            textoValvulas.text = "VALVULAS " + gestor.Valvulas + "/2   FUGAS " + gestor.FugasSelladas + "/" + gestor.FugasTotales;
            textoObjetivo.text = gestor.Objetivo();
        }
    }
}
