using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Prueba de logica sin render: comprueba que las dos valvulas habilitan la
// puerta y que la rueda cierra el nivel. Sirve para no grabar a ciegas.
public static class PruebaLogica
{
    [MenuItem("La Fuga/Probar logica")]
    public static void Probar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/LaFuga.unity");
        var gestor = Object.FindAnyObjectByType<GestorNivel>();
        var interactivos = Object.FindObjectsByType<Interactivo>(FindObjectsInactive.Exclude);
        Debug.Log("PRUEBA interactivos encontrados: " + interactivos.Length);
        foreach (var it in interactivos)
            Debug.Log("PRUEBA " + it.id + " tipo " + it.tipo + " en " + it.transform.position);

        foreach (var it in interactivos)
            if (it.tipo == TipoInteractivo.Valvula) it.Activar();

        var puerta = System.Array.Find(interactivos, i => i.tipo == TipoInteractivo.Puerta);
        Debug.Log("PRUEBA puede abrir puerta: " + gestor.PuedeAbrirPuerta());
        Debug.Log("PRUEBA puerta disponible: " + (puerta != null && puerta.Disponible()));
        if (puerta != null) puerta.Activar();
        Debug.Log("PRUEBA puerta usada: " + (puerta != null && puerta.usado));
    }
}
