using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HerramientasACA2
{
    const string ESCENA = "Assets/Scenes/LaFuga.unity";
    const string CAPTURAS = "Builds/capturas";

    [MenuItem("La Fuga/Capturas ACA 2")]
    public static void Capturas()
    {
        EditorSceneManager.OpenScene(ESCENA);
        Directory.CreateDirectory(CAPTURAS);
        var cam = new GameObject("CamCaptura").AddComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.farClipPlane = 200f;

        Foto(cam, "01_vista_colector", new Vector3(56f, 1.9f, 54.2f), new Vector3(56f, -1.2f, 72f));
        Foto(cam, "02_salida_dominante", new Vector3(56f, 0.4f, 66f), new Vector3(56f, 0.3f, 84f));
        Foto(cam, "03_refugio", new Vector3(50f, 0.2f, 66f), new Vector3(42f, -0.6f, 71f));
        Foto(cam, "04_guardia", new Vector3(52.5f, -0.2f, 59.5f), new Vector3(50f, -0.6f, 62f));
        Foto(cam, "05_pasarela", new Vector3(69f, 1.6f, 56.5f), new Vector3(66f, -0.5f, 76f));
        Foto(cam, "06_galeria_guardia", new Vector3(25f, 1.6f, 6f), new Vector3(31f, 0.8f, 4f));

        cam.orthographic = true;
        cam.orthographicSize = 46f;
        var techos = new System.Collections.Generic.List<GameObject>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            if (r.name.StartsWith("Techo") || r.name.StartsWith("Refugio techo")) { techos.Add(r.gameObject); r.gameObject.SetActive(false); }
        Foto(cam, "07_planta", new Vector3(38f, 60f, 42f), new Vector3(38f, 0f, 42.01f), 1600, 1600);
        foreach (var t in techos) t.SetActive(true);
        Object.DestroyImmediate(cam.gameObject);

        var jugador = Object.FindAnyObjectByType<Jugador>();
        jugador.transform.position = new Vector3(56f, 1.2f, 55f);
        jugador.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        Foto(jugador.camara, "08_hud", jugador.camara.transform.position, jugador.camara.transform.position + new Vector3(0f, -0.25f, 1f));
        Debug.Log("CAPTURAS LISTAS");
    }

    static void Foto(Camera cam, string nombre, Vector3 desde, Vector3 hacia, int ancho = 1920, int alto = 1080)
    {
        var anterior = cam.transform.position;
        var rotAnterior = cam.transform.rotation;
        cam.transform.position = desde;
        cam.transform.LookAt(hacia);
        var rt = new RenderTexture(ancho, alto, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(CAPTURAS, nombre + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        cam.transform.SetPositionAndRotation(anterior, rotAnterior);
    }

    [MenuItem("La Fuga/Prueba ACA 2")]
    public static void Prueba()
    {
        EditorSceneManager.OpenScene(ESCENA);
        new GameObject("PRUEBA_ACA2").AddComponent<PruebaACA2>();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("La Fuga/Build Linux")]
    public static void BuildLinux()
    {
        var opciones = new BuildPlayerOptions
        {
            scenes = new[] { ESCENA },
            locationPathName = "Builds/Linux/LaFuga.x86_64",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None,
        };
        var r = BuildPipeline.BuildPlayer(opciones);
        Debug.Log("BUILD " + r.summary.result + " " + r.summary.totalSize + " bytes, errores " + r.summary.totalErrors);
    }
}
