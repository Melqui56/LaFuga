using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Saca el animatic del propio rig: muestrea cada clip en sus poses clave y
// renderiza un PNG por pose. El animatic no se dibuja aparte, sale del rig.
public static class Animatico
{
    const int ANCHO = 1080, ALTO = 1215;

    [MenuItem("Anim2D/Renderizar animatic")]
    public static void Renderizar()
    {
        var escena = EditorSceneManager.OpenScene("Assets/Anim2D/Personaje.unity");
        var raiz = GameObject.Find("Nilo");
        if (raiz == null) { Debug.LogError("No esta Nilo"); return; }
        var anim = raiz.GetComponent<Animator>();

        string salida = "/tmp/opencode/animatico";
        Directory.CreateDirectory(salida);

        Piso();
        var camara = new GameObject("CamaraAnimatico").AddComponent<Camera>();
        camara.orthographic = true;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.97f, 0.97f, 0.97f, 1f);
        camara.transform.position = new Vector3(1.0f, 1.0f, -6f);
        camara.orthographicSize = 0.98f;

        var clips = new[] { "Idle", "Walk", "Run", "Salto" };
        var poses = new[] { 2, 6, 5, 4 };
        var lienzo = new Texture2D(ANCHO, ALTO, TextureFormat.RGB24, false);

        AnimationMode.StartAnimationMode();
        foreach (var nombre in clips)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Anim2D/Animaciones/" + nombre + ".anim");
            if (clip == null) { Debug.LogWarning("falta clip " + nombre); continue; }
            int n = poses[System.Array.IndexOf(clips, nombre)];
            for (int i = 0; i < n; i++)
            {
                float t = clip.length * i / Mathf.Max(1, n - 1);
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(raiz, clip, Mathf.Min(t, clip.length - 0.01f));
                AnimationMode.EndSampling();
                Render(camara, lienzo, Path.Combine(salida, nombre + "_" + i + ".png"));
            }
            Debug.Log("ANIMATIC " + nombre + " poses " + n);
        }
        AnimationMode.StopAnimationMode();
        EditorSceneManager.SaveScene(escena);
        Debug.Log("ANIMATIC LISTO " + salida);
    }

    [MenuItem("Anim2D/Renderizar video del animatic")]
    public static void Video()
    {
        EditorSceneManager.OpenScene("Assets/Anim2D/Personaje.unity");
        var raiz = GameObject.Find("Nilo");
        if (raiz == null) { Debug.LogError("No esta Nilo"); return; }

        string salida = "/tmp/opencode/anim_video";
        if (Directory.Exists(salida)) Directory.Delete(salida, true);
        Directory.CreateDirectory(salida);

        Piso();
        var camara = new GameObject("CamaraVideo").AddComponent<Camera>();
        camara.orthographic = true;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.96f, 0.96f, 0.95f, 1f);
        camara.transform.position = new Vector3(1.0f, 1.0f, -6f);
        camara.orthographicSize = 0.98f;

        var lienzo = new Texture2D(ANCHO, ALTO, TextureFormat.RGB24, false);
        int n = 0;
        AnimationMode.StartAnimationMode();
        foreach (var nombre in new[] { "Idle", "Walk", "Run", "Salto" })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                "Assets/Anim2D/Animaciones/" + nombre + ".anim");
            if (clip == null) continue;
            int cuadros = Mathf.Max(2, Mathf.RoundToInt(clip.length * 24f));
            for (int i = 0; i < cuadros; i++)
            {
                float t = clip.length * i / cuadros;
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(raiz, clip, t);
                AnimationMode.EndSampling();
                Render(camara, lienzo, Path.Combine(salida, "v" + n.ToString("D4") + ".png"));
                n++;
            }
            Debug.Log("VIDEO " + nombre + " " + cuadros + " cuadros");
        }
        AnimationMode.StopAnimationMode();
        Debug.Log("VIDEO LISTO " + n + " cuadros en " + salida);
    }

    // línea de piso: sin ella no se ve si el personaje despega al saltar
    static void Piso()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Piso";
        go.transform.position = new Vector3(1f, 0.148f, 1.2f);
        go.transform.localScale = new Vector3(3.2f, 0.006f, 0.05f);
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.62f, 0.62f, 0.60f, 1f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
    }

    static void Render(Camera camara, Texture2D lienzo, string ruta)
    {
        var rt = RenderTexture.GetTemporary(ANCHO, ALTO, 24);
        camara.targetTexture = rt;
        camara.Render();
        RenderTexture.active = rt;
        lienzo.ReadPixels(new Rect(0, 0, ANCHO, ALTO), 0, 0);
        lienzo.Apply();
        RenderTexture.active = null;
        camara.targetTexture = null;
        File.WriteAllBytes(ruta, lienzo.EncodeToPNG());
        RenderTexture.ReleaseTemporary(rt);
    }
}
