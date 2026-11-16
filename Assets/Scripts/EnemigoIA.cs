using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemigoIA : MonoBehaviour
{
    public enum Estado { Patrulla, Persecucion, Ataque }

    public Transform[] puntosPatrulla;
    public float radioDeteccion = 8f;
    public float radioAtaque = 1.8f;
    public float velocidadPatrulla = 1.5f;
    public float velocidadPersecucion = 3.5f;
    public float esperaEntreGolpes = 1.1f;
    public Renderer visor;

    public Estado estadoActual = Estado.Patrulla;

    NavMeshAgent agente;
    Animator animador;
    Transform jugador;
    int puntoActual;
    bool atacando;
    Vector3 posicionInicial;
    Material materialVisor;

    static readonly Color ColorPatrulla = new Color(0.2f, 0.9f, 0.4f);
    static readonly Color ColorPersecucion = new Color(1f, 0.6f, 0.05f);
    static readonly Color ColorAtaque = new Color(1f, 0.1f, 0.05f);

    void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        animador = GetComponentInChildren<Animator>();
        posicionInicial = transform.position;
        if (visor != null) materialVisor = visor.material;
    }

    void Start()
    {
        var j = Object.FindAnyObjectByType<Jugador>();
        if (j != null) jugador = j.transform;
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        atacando = false;
        agente.Warp(posicionInicial);
        estadoActual = Estado.Patrulla;
        puntoActual = 0;
    }

    void Update()
    {
        if (jugador == null || !agente.isOnNavMesh) return;
        float distancia = Vector3.Distance(transform.position, jugador.position);

        switch (estadoActual)
        {
            case Estado.Patrulla:
                agente.speed = velocidadPatrulla;
                Patrullar();
                if (distancia < radioDeteccion && LoVe()) estadoActual = Estado.Persecucion;
                break;

            case Estado.Persecucion:
                agente.speed = velocidadPersecucion;
                agente.isStopped = false;
                agente.SetDestination(jugador.position);
                if (distancia < radioAtaque) estadoActual = Estado.Ataque;
                else if (distancia > radioDeteccion * 1.5f) estadoActual = Estado.Patrulla;
                break;

            case Estado.Ataque:
                agente.isStopped = true;
                MirarA(jugador.position);
                if (!atacando) StartCoroutine(Golpear());
                if (distancia > radioAtaque * 1.3f) estadoActual = Estado.Persecucion;
                break;
        }

        if (animador != null) animador.SetFloat("Velocidad", agente.velocity.magnitude);
        PintarVisor();
    }

    void Patrullar()
    {
        agente.isStopped = false;
        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;
        if (!agente.pathPending && agente.remainingDistance < 0.5f)
        {
            puntoActual = (puntoActual + 1) % puntosPatrulla.Length;
            agente.SetDestination(puntosPatrulla[puntoActual].position);
        }
    }

    bool LoVe()
    {
        Vector3 ojos = transform.position + Vector3.up * 1.4f;
        Vector3 objetivo = jugador.position + Vector3.up * 0.6f;
        if (Physics.Linecast(ojos, objetivo, out RaycastHit golpe, ~0, QueryTriggerInteraction.Ignore))
            return golpe.transform == jugador || golpe.transform.IsChildOf(jugador);
        return true;
    }

    void MirarA(Vector3 punto)
    {
        Vector3 dir = punto - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
    }

    IEnumerator Golpear()
    {
        atacando = true;
        if (animador != null) animador.SetTrigger("Atacar");
        yield return new WaitForSeconds(esperaEntreGolpes);
        atacando = false;
    }

    void PintarVisor()
    {
        if (materialVisor == null) return;
        Color c = estadoActual == Estado.Patrulla ? ColorPatrulla
                : estadoActual == Estado.Persecucion ? ColorPersecucion : ColorAtaque;
        materialVisor.color = c;
        materialVisor.SetColor("_EmissionColor", c * 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioAtaque);
    }
}
