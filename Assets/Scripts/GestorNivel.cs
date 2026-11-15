using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Estado del nivel: el agua que sube, las valvulas, la puerta, la victoria y
// la derrota. Sin este script no hay forma de perder ni de terminar.
public class GestorNivel : MonoBehaviour
{
    public Transform aguaVisual;
    public float aguaBase;
    public float aguaRitmo;
    public float aguaPorFuga;
    public float aguaMaxima;
    public Transform agua;

    public Jugador jugador;
    public Fuga[] fugas;
    public Transform compuertaColector;
    public Transform puntoControl;
    public SaludJugador salud;

    readonly List<Interactivo> interactivos = new List<Interactivo>();
    int valvulas;
    int fugasSelladas;
    float nivelAgua;
    float velocidadActual;
    string estado = "jugando";
    bool compuertaCerrada;
    float alturaCompuerta;
    float temporizador;
    AudioSource ambiente;

    void Start()
    {
        nivelAgua = aguaBase;
        if (jugador == null) jugador = Object.FindAnyObjectByType<Jugador>();

        // la lista no se serializa: en Play hay que volver a llenarla o las
        // valvulas, la puerta y la rueda quedan muertas
        interactivos.Clear();
        interactivos.AddRange(Object.FindObjectsByType<Interactivo>(FindObjectsInactive.Exclude));
        var clip = Resources.Load<AudioClip>("agua_ambiente");
        if (clip == null)
        {
            var todos = Resources.FindObjectsOfTypeAll<AudioClip>();
            foreach (var c in todos) if (c.name == "agua_ambiente") clip = c;
        }
        if (clip != null)
        {
            ambiente = gameObject.AddComponent<AudioSource>();
            ambiente.clip = clip;
            ambiente.loop = true;
            ambiente.volume = 0.22f;
            ambiente.spatialBlend = 0f;
            ambiente.Play();
        }
    }

    public void RegistrarInteractivo(Interactivo it) { interactivos.Add(it); }

    public bool PuedeAbrirPuerta() { return valvulas >= 2; }

    public void InteractivoUsado(Interactivo it)
    {
        if (it.tipo == TipoInteractivo.Valvula) valvulas++;
        Debug.Log("USADO " + it.id + " valvulas=" + valvulas);
    }

    public void FugaSellada() { fugasSelladas++; }

    public void InteractuarCerca()
    {
        if (estado != "jugando" || jugador == null) return;
        Interactivo mejor = null;
        float dist = 3.4f;
        foreach (var it in interactivos)
        {
            if (!it.Disponible()) continue;
            float d = Vector3.Distance(it.transform.position, jugador.transform.position);
            if (d < dist) { dist = d; mejor = it; }
        }
        if (mejor != null) mejor.Activar();
    }

    public int Valvulas => valvulas;
    public int FugasSelladas => fugasSelladas;
    public int FugasTotales => fugas == null ? 0 : fugas.Length;
    public bool CompuertaCerrada => compuertaCerrada;
    public string Estado => estado;

    public float AguaSobrePies()
    {
        if (jugador == null) return 0f;
        return nivelAgua - (jugador.transform.position.y - 0.85f);
    }

    public string Objetivo()
    {
        if (compuertaCerrada) return "Cruza el colector sin que te atrapen y sal por la escotilla verde";
        if (valvulas < 2) return "Sella las fugas (clic) y gira las dos valvulas (E)";
        return "Abre la puerta de la compuerta y gira la rueda de cierre";
    }

    public void CerrarCompuerta()
    {
        if (estado != "jugando" || compuertaCerrada) return;
        compuertaCerrada = true;
        if (compuertaColector != null) alturaCompuerta = compuertaColector.position.y;
        Debug.Log("COMPUERTA CERRADA: se abre el colector");
    }

    public void Escapar()
    {
        if (estado != "jugando" || !compuertaCerrada) return;
        estado = "completado";
        temporizador = 0f;
        if (jugador != null) jugador.Bloquear(true);
        Debug.Log("ESCAPE COMPLETADO");
    }

    public void DerrotaPorGuardia()
    {
        if (estado != "jugando") return;
        if (compuertaCerrada && puntoControl != null && jugador != null)
        {
            var cc = jugador.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            jugador.transform.position = puntoControl.position;
            if (cc != null) cc.enabled = true;
            if (salud != null) salud.Restaurar();
            foreach (var g in Object.FindObjectsByType<EnemigoIA>(FindObjectsInactive.Exclude))
                g.Reiniciar();
            Debug.Log("REAPARECE EN EL PUNTO DE CONTROL");
            return;
        }
        estado = "atrapado";
        temporizador = 0f;
        if (jugador != null) jugador.Bloquear(true);
    }

    void Update()
    {
        if (aguaVisual == null) return;

        if (estado == "jugando" && compuertaCerrada)
        {
            nivelAgua = Mathf.MoveTowards(nivelAgua, aguaBase - 0.5f, Time.deltaTime * 0.4f);
            if (compuertaColector != null && compuertaColector.position.y < alturaCompuerta + 4.2f)
                compuertaColector.position += Vector3.up * Time.deltaTime * 1.4f;
            if (jugador != null && jugador.transform.position.y < -6f) DerrotaPorGuardia();
        }
        else if (estado == "jugando")
        {
            int abiertas = 0;
            foreach (var f in fugas) if (f != null && !f.sellada) abiertas++;

            // la dificultad crece por tramo: el mismo numero de fugas pesa mas
            // cuanto mas adentro esta el jugador
            float factor = 1f + 0.22f * Tramo();
            velocidadActual = Mathf.Min(aguaRitmo * factor + aguaPorFuga * abiertas * factor, aguaMaxima);
            nivelAgua += velocidadActual * Time.deltaTime;

            if (jugador != null)
            {
                float pies = jugador.transform.position.y - 0.85f;
                if (nivelAgua > pies + 1.15f || jugador.transform.position.y < -2.2f)
                    Derrota();
            }
        }
        else if (estado == "completado")
        {
            nivelAgua = Mathf.MoveTowards(nivelAgua, aguaBase - 0.5f, Time.deltaTime * 0.6f);
            temporizador += Time.deltaTime;
            if (temporizador > 4f) Reiniciar();
        }
        else
        {
            temporizador += Time.deltaTime;
            if (temporizador > 1.2f) Reiniciar();
        }

        aguaVisual.position = new Vector3(aguaVisual.position.x, nivelAgua, aguaVisual.position.z);
    }

    int Tramo()
    {
        if (jugador == null) return 0;
        float x = jugador.transform.position.x;
        float z = jugador.transform.position.z;
        if (x < 22f) return 0;      // boca del tunel
        if (x < 50f) return 1;      // galeria
        if (x < 60f && z < 11f) return 2;  // pozo
        if (z < 39f) return 3;      // compuerta
        if (z < 53f) return 4;      // camara
        return 5;                   // colector
    }

    void Derrota()
    {
        if (estado != "jugando") return;
        estado = "ahogado";
        temporizador = 0f;
        if (jugador != null) jugador.Bloquear(true);
    }

    void Reiniciar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnGUI()
    {
        if (estado != "jugando")
        {
            Color c = estado == "completado" ? new Color(0.02f, 0.14f, 0.06f, 0.45f)
                                             : new Color(0.45f, 0.02f, 0.02f, 0.55f);
            GUI.color = c;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var estilo = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(Screen.height * 0.05f),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, Screen.height * 0.16f),
                      estado == "ahogado" ? "EL AGUA TE ALCANZO"
                      : estado == "atrapado" ? "TE ATRAPO UN GUARDIA" : "ESCAPASTE DEL COLECTOR",
                      estilo);
        }
    }
}
