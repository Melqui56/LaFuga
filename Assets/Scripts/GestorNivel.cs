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

    readonly List<Interactivo> interactivos = new List<Interactivo>();
    int valvulas;
    int fugasSelladas;
    float nivelAgua;
    float velocidadActual;
    string estado = "jugando";
    float temporizador;
    AudioSource ambiente;

    void Start()
    {
        nivelAgua = aguaBase;
        if (jugador == null) jugador = Object.FindFirstObjectByType<Jugador>();

        // la lista no se serializa: en Play hay que volver a llenarla o las
        // valvulas, la puerta y la rueda quedan muertas
        interactivos.Clear();
        interactivos.AddRange(Object.FindObjectsByType<Interactivo>(FindObjectsSortMode.None));
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

    public void CerrarCompuerta()
    {
        if (estado != "jugando") return;
        estado = "completado";
        temporizador = 0f;
        if (jugador != null) jugador.Bloquear(true);
    }

    void Update()
    {
        if (aguaVisual == null) return;

        if (estado == "jugando")
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
        return 4;                   // camara
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
            Color c = estado == "ahogado" ? new Color(0.45f, 0.02f, 0.02f, 0.55f)
                                          : new Color(0.02f, 0.14f, 0.06f, 0.45f);
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
                      estado == "ahogado" ? "EL AGUA TE ALCANZO" : "COMPUERTA CERRADA",
                      estilo);
        }
    }
}
