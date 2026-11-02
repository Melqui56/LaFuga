using UnityEngine;

// Marca el final del recorrido. La victoria real la dispara la rueda de cierre;
// esto solo deja constancia en consola de que el jugador llego a la camara.
public class SalidaNivel : MonoBehaviour
{
    bool avisado;

    void OnTriggerEnter(Collider otro)
    {
        if (avisado) return;
        if (otro.GetComponent<Jugador>() == null) return;
        avisado = true;
        Debug.Log("SALIDA ALCANZADA");
    }

    void Start()
    {
        var c = gameObject.AddComponent<SphereCollider>();
        c.isTrigger = true;
        c.radius = 3f;
    }

    void Update()
    {
        var j = Object.FindAnyObjectByType<Jugador>();
        if (j == null || avisado) return;
        if (Vector3.Distance(j.transform.position, transform.position) < 3.5f)
        {
            avisado = true;
            Debug.Log("SALIDA ALCANZADA");
        }
    }
}
