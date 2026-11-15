using System.Collections;
using UnityEngine;

// Prueba de humo del ACA 2 en modo Play: deteccion, persecucion, dano,
// punto de control y escape. Escribe el resultado en consola y cierra el editor.
public class PruebaACA2 : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        var gestor = Object.FindAnyObjectByType<GestorNivel>();
        var jugador = Object.FindAnyObjectByType<Jugador>();
        var salud = jugador.GetComponent<SaludJugador>();
        var guardias = Object.FindObjectsByType<EnemigoIA>(FindObjectsInactive.Exclude);
        Debug.Log("PRUEBA guardias=" + guardias.Length);
        foreach (var g in guardias)
            Debug.Log("PRUEBA " + g.name + " enNavMesh=" + g.GetComponent<UnityEngine.AI.NavMeshAgent>().isOnNavMesh);

        float agua0 = gestor.AguaSobrePies();
        yield return new WaitForSeconds(3f);
        Debug.Log("PRUEBA agua sobre pies: " + agua0.ToString("0.00") + " -> " + gestor.AguaSobrePies().ToString("0.00") + " m en 3 s, fugas visibles=" + Object.FindObjectsByType<Fuga>(FindObjectsInactive.Exclude).Length);
        Object.FindAnyObjectByType<Fuga>().Sellarse();
        Debug.Log("PRUEBA fugas selladas=" + gestor.FugasSelladas + " aviso='" + gestor.Mensaje + "'");
        foreach (var g in guardias)
            Debug.Log("PRUEBA patrulla " + g.name + " pos=" + g.transform.position + " estado=" + g.estadoActual);

        gestor.CerrarCompuerta();
        Mover(jugador, new Vector3(56f, -0.4f, 66f));
        float t0 = Time.time;
        while (Time.time - t0 < 12f)
        {
            jugador.Mover(Vector2.zero, false, false);
            yield return null;
        }
        foreach (var g in guardias)
            Debug.Log("PRUEBA tras 12s " + g.name + " estado=" + g.estadoActual + " pos=" + g.transform.position);
        Debug.Log("PRUEBA salud=" + salud.Actual + " estado=" + gestor.Estado);

        Mover(jugador, new Vector3(56f, -0.4f, 82.5f));
        yield return new WaitForSeconds(1.5f);
        Debug.Log("PRUEBA final estado=" + gestor.Estado);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(0);
#endif
    }

    static void Mover(Jugador j, Vector3 p)
    {
        var cc = j.GetComponent<CharacterController>();
        cc.enabled = false;
        j.transform.position = p;
        cc.enabled = true;
    }
}
