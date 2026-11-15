using UnityEngine;

public class SalidaNivel : MonoBehaviour
{
    bool avisado;

    void OnTriggerEnter(Collider otro)
    {
        if (avisado) return;
        if (otro.GetComponent<Jugador>() == null) return;
        Llegar();
    }

    void Llegar()
    {
        var g = Object.FindAnyObjectByType<GestorNivel>();
        if (g == null || !g.CompuertaCerrada) return;
        avisado = true;
        g.Escapar();
    }

    void Start()
    {
        var c = gameObject.AddComponent<SphereCollider>();
        c.isTrigger = true;
        c.radius = 2f;
    }

    void Update()
    {
        var j = Object.FindAnyObjectByType<Jugador>();
        if (j == null || avisado) return;
        if (Vector3.Distance(j.transform.position, transform.position) < 2.5f)
            Llegar();
    }
}
