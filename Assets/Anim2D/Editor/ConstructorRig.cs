using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Construye el personaje 2D a partir del arte recortado.
//
// Lee partes.json, que trae las piezas ya ordenadas de atrás hacia adelante,
// el hueso de cada una, su padre y el pivote (la articulación). Con eso arma la
// jerarquía, crea las animaciones y exporta el .unitypackage.
//
// Las curvas están pensadas para que el cuerpo no se rompa: la cadera y el
// hombro giran poco (el torso es la pieza fija), la rodilla y el codo hacen el
// trabajo, y el salto desplaza la raíz para que el personaje despegue.
public static class ConstructorRig
{
    const string ARTE = "/home/melqui/Documents/uni/Cun/Primer Semestre/animacion2d/" +
                        "entregable/ACA_I/fuente_arte/salida";
    const string DESTINO = "Assets/Anim2D";
    const string ESCENA = "Assets/Anim2D/Personaje.unity";

    [Serializable]
    class Pieza
    {
        public string nombre, archivo, hueso, padre;
        public float[] pivote;
    }

    [Serializable]
    class Personaje
    {
        public float[] lienzo;
        public float escala;
        public Pieza[] piezas;
    }

    [MenuItem("Anim2D/Construir personaje y exportar")]
    public static void Construir()
    {
        Directory.CreateDirectory(DESTINO + "/Sprites");
        Directory.CreateDirectory(DESTINO + "/Animaciones");

        var json = File.ReadAllText(Path.Combine(ARTE, "partes.json"));
        var per = JsonUtility.FromJson<Personaje>(json);
        if (per == null || per.piezas == null || per.piezas.Length == 0)
        {
            Debug.LogError("partes.json vacio o con formato distinto");
            return;
        }
        float lado = per.lienzo[0];
        float escala = 1f / 1000f;   // 1000 px por unidad

        // 1. sprites con el pivote en la articulación
        var sprites = new Dictionary<string, Sprite>();
        foreach (var p in per.piezas)
        {
            string origen = Path.Combine(ARTE, p.archivo);
            string destino = DESTINO + "/Sprites/" + p.archivo;
            File.Copy(origen, destino, true);
            AssetDatabase.ImportAsset(destino, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(destino);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 1000f;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;

            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            float px = p.pivote[0], py = p.pivote[1];
            st.spritePivot = new Vector2(px / lado, 1f - py / lado);
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();

            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(destino);
            if (sp == null) { Debug.LogError("Sin sprite: " + destino); continue; }
            sprites[p.nombre] = sp;
        }

        // 2. jerarquía de huesos
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var raiz = new GameObject("Nilo").transform;
        var huesos = new Dictionary<string, Transform>();

        foreach (var p in per.piezas)
        {
            var go = new GameObject(p.hueso);
            float px = p.pivote[0], py = p.pivote[1];
            var pivoteMundo = new Vector3(px * escala, (lado - py) * escala, 0f);

            Transform padre = null;
            if (!string.IsNullOrEmpty(p.padre) && huesos.ContainsKey(p.padre))
                padre = huesos[p.padre];

            if (padre != null)
            {
                go.transform.SetParent(padre, true);
                go.transform.position = pivoteMundo;
            }
            else
            {
                go.transform.SetParent(raiz, false);
                go.transform.position = pivoteMundo;
            }
            huesos[p.hueso] = go.transform;

            if (sprites.ContainsKey(p.nombre))
            {
                var sr = new GameObject("Sprite").AddComponent<SpriteRenderer>();
                sr.transform.SetParent(go.transform, false);
                sr.sprite = sprites[p.nombre];
                sr.sortingOrder = huesos.Count;   // el orden del archivo es el apilado
                // las extremidades de atrás van más oscuras: cuando las piernas se
                // cruzan al caminar, cada una se sigue leyendo por separado
                if (p.nombre.EndsWith("_atras"))
                    sr.color = new Color(0.45f, 0.48f, 0.56f, 1f);
            }
        }

        // 3. animaciones
        var clips = Animaciones(huesos);
        foreach (var c in clips)
        {
            string ruta = DESTINO + "/Animaciones/" + c.Key + ".anim";
            AssetDatabase.DeleteAsset(ruta);
            AssetDatabase.CreateAsset(c.Value, ruta);
        }

        // 4. controlador
        var controlador = AnimatorController.CreateAnimatorControllerAtPath(
            DESTINO + "/Nilo.controller");
        controlador.AddParameter("velocidad", AnimatorControllerParameterType.Float);
        var sm = controlador.layers[0].stateMachine;
        foreach (var nombre in new[] { "Idle", "Walk", "Run", "Salto" })
        {
            var estado = sm.AddState(nombre);
            estado.motion = clips[nombre];
            if (nombre == "Idle") sm.defaultState = estado;
        }
        var anim = raiz.gameObject.AddComponent<Animator>();
        anim.runtimeAnimatorController = controlador;

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(escena, ESCENA);
        AssetDatabase.Refresh();

        foreach (var c in clips)
            Debug.Log("CLIP " + c.Key + " largo " + c.Value.length.ToString("0.00") + " s");

        Exportar();
    }

    // ------------------------------------------------------------------ clips
    // Cada curva es una lista de keyframes repartidos uniformemente en el
    // tiempo. Los ciclos empiezan y terminan en el mismo valor para que el loop
    // no dé un salto.
    static Dictionary<string, AnimationClip> Animaciones(Dictionary<string, Transform> huesos)
    {
        var d = new Dictionary<string, AnimationClip>();

        // respiración: el torso sube y los brazos acompañan
        d["Idle"] = Clip("Idle", 2.4f, true, new Dictionary<string, float[]> {
            {"Torso", new[] { 0f, 2.2f, 0f, -1f, 0f }},
            {"Head", new[] { 0f, -1.6f, 0f, 1f, 0f }},
            {"ArmFrontUpper", new[] { 0f, 2f, 0f, -1.4f, 0f }},
            {"ArmBack", new[] { 0f, -2.4f, 0f, 1.6f, 0f }},
        }, huesos, altura: new[] { 0f, 0.006f, 0f, -0.004f, 0f });

        // ocho poses: contacto, bajada, paso, subida y la otra pierna; la raíz baja
        // en la flexión y sube en el impulso, con el pie de apoyo sobre el piso
        d["Walk"] = Clip("Walk", 0.9f, true, new Dictionary<string, float[]> {
            {"LegFrontUpper", new[] { 8f, 5f, 0f, -4f, -7f, -4f, 1f, 5f, 8f }},
            {"LegFrontLower", new[] { 0f, -2f, -1f, 0f, -2f, -4f, -5f, -2f, 0f }},
            {"LegBackUpper", new[] { -7f, -4f, 1f, 5f, 8f, 5f, 0f, -4f, -7f }},
            {"LegBackLower", new[] { -2f, -4f, -5f, -2f, 0f, -2f, -1f, 0f, -2f }},
            {"ArmBack", new[] { 15f, 10f, 0f, -7f, -14f, -8f, 1f, 10f, 15f }},
            {"ArmFrontUpper", new[] { -8f, -5f, 0f, 4f, 7f, 4f, -1f, -5f, -8f }},
            {"Torso", new[] { 3f, 5f, 3f, 2f, 3f, 5f, 3f, 2f, 3f }},
            {"Head", new[] { -1f, -3f, -1f, 0f, -1f, -3f, -1f, 0f, -1f }},
        }, huesos, altura: new[] { 0f, -0.03f, 0f, 0.02f, 0f, -0.03f, 0f, 0.02f, 0f });

        // igual que caminar, más amplio, con fase de vuelo y torso inclinado
        d["Run"] = Clip("Run", 0.55f, true, new Dictionary<string, float[]> {
            {"LegFrontUpper", new[] { 12f, 7f, -1f, -8f, -11f, -6f, 3f, 9f, 12f }},
            {"LegFrontLower", new[] { -1f, -4f, -2f, -1f, -4f, -7f, -7f, -4f, -1f }},
            {"LegBackUpper", new[] { -11f, -6f, 3f, 9f, 12f, 7f, -1f, -8f, -11f }},
            {"LegBackLower", new[] { -4f, -7f, -7f, -4f, -1f, -4f, -2f, -1f, -4f }},
            {"ArmBack", new[] { 29f, 16f, -4f, -22f, -27f, -13f, 9f, 25f, 29f }},
            {"ArmFrontUpper", new[] { -14f, -8f, 2f, 11f, 14f, 6f, -4f, -13f, -14f }},
            {"ArmFrontLower", new[] { 10f, 12f, 14f, 12f, 10f, 12f, 14f, 12f, 10f }},
            {"Torso", new[] { 9f, 11f, 9f, 8f, 9f, 11f, 9f, 8f, 9f }},
            {"Head", new[] { -4f, -6f, -4f, -3f, -4f, -6f, -4f, -3f, -4f }},
        }, huesos, altura: new[] { 0f, -0.045f, 0f, 0.05f, 0f, -0.045f, 0f, 0.05f, 0f });

        // reposo, anticipación, despegue, apogeo, caída, aterrizaje y recuperación
        d["Salto"] = Clip("Salto", 0.7f, false, new Dictionary<string, float[]> {
            {"LegFrontUpper", new[] { 0f, 8f, -3f, 10f, 4f, 7f, 0f }},
            {"LegFrontLower", new[] { 0f, -8f, 0f, -9f, -3f, -7f, 0f }},
            {"LegBackUpper", new[] { 0f, 6f, -4f, 6f, -2f, 5f, 0f }},
            {"LegBackLower", new[] { 0f, -7f, -1f, -9f, -3f, -6f, 0f }},
            {"ArmBack", new[] { 0f, -24f, 30f, 22f, 10f, -16f, 0f }},
            {"ArmFrontUpper", new[] { 0f, -10f, 14f, 8f, 4f, -8f, 0f }},
            {"Torso", new[] { 0f, 14f, -4f, 6f, 2f, 12f, 0f }},
            {"Head", new[] { 0f, -6f, 4f, -2f, 0f, -4f, 0f }},
        }, huesos, altura: new[] { 0f, -0.06f, 0.13f, 0.32f, 0.13f, -0.05f, 0f });

        return d;
    }

    static AnimationClip Clip(string nombre, float duracion, bool repetir,
                              Dictionary<string, float[]> curvas,
                              Dictionary<string, Transform> huesos,
                              float[] altura = null)
    {
        var clip = new AnimationClip { name = nombre, frameRate = 24f };
        foreach (var kv in curvas)
        {
            if (!huesos.ContainsKey(kv.Key)) { Debug.LogWarning("sin hueso " + kv.Key); continue; }
            clip.SetCurve(Ruta(huesos[kv.Key]), typeof(Transform), "localEulerAnglesRaw.z",
                          Curva(kv.Value, duracion));
        }
        if (altura != null)
            clip.SetCurve("", typeof(Transform), "localPosition.y", Curva(altura, duracion));

        var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = repetir;
        ajustes.startTime = 0f;
        ajustes.stopTime = duracion;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);
        return clip;
    }

    static AnimationCurve Curva(float[] valores, float duracion)
    {
        var curva = new AnimationCurve();
        for (int i = 0; i < valores.Length; i++)
        {
            float t = valores.Length == 1 ? 0f : duracion * i / (valores.Length - 1);
            curva.AddKey(t, valores[i]);
        }
        for (int i = 0; i < curva.keys.Length; i++)
            curva.SmoothTangents(i, 0f);
        return curva;
    }

    static string Ruta(Transform t)
    {
        var partes = new List<string>();
        var actual = t;
        while (actual != null && actual.parent != null &&
               actual.parent.name != "Nilo")
        {
            partes.Insert(0, actual.name);
            actual = actual.parent;
        }
        if (actual != null && actual.parent != null) partes.Insert(0, actual.name);
        return string.Join("/", partes);
    }

    static void Exportar()
    {
        string destino = "/tmp/opencode/Nilo_Rig.unitypackage";
        AssetDatabase.ExportPackage(DESTINO, destino,
            ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
        Debug.Log("PAQUETE " + destino);
    }
}
