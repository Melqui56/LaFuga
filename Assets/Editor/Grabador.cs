using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Punto de entrada para construir y para grabar desde la linea de comandos.
public static class Grabador
{
    [MenuItem("La Fuga/Construir y grabar")]
    public static void ConstruirYGrabar()
    {
        ConstructorNivel.Construir();
        Grabar();
    }

    [MenuItem("La Fuga/Grabar recorrido")]
    public static void Grabar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/LaFuga.unity");
        var go = new GameObject("BOT_GRABACION");
        go.AddComponent<BotGrabacion>();
        EditorApplication.isPlaying = true;
    }
}
