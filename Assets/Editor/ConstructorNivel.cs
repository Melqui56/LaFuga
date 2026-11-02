using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Construye la escena completa desde nivel.json. Idempotente: se puede correr
// las veces que haga falta y siempre produce el mismo nivel.
public static class ConstructorNivel
{
    const string RUTA_ESCENA = "Assets/Scenes/LaFuga.unity";

    [MenuItem("La Fuga/Construir nivel")]
    public static void Construir()
    {
        Nivel n = Nivel.Cargar();
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory("Assets/Scenes");

        var mats = Materiales();
        var raiz = new GameObject("NIVEL").transform;

        var entorno = Grupo("ENTORNO", raiz);
        Cajas(n.pisos, "Piso", entorno, mats["piso"], true);
        Cajas(n.muros, "Muro", entorno, mats["muro"], true);
        Cajas(n.techos, "Techo", entorno, mats["techo"], true);
        Cajas(n.plataformas, "Plataforma", entorno, mats["plataforma"], true);

        var props = Grupo("INTERACTIVOS", raiz);
        var juego = new GameObject("JUEGO").AddComponent<GestorNivel>();
        juego.transform.SetParent(raiz);

        var agua = CrearAgua(n, raiz, mats["agua"], juego);
        juego.agua = agua;

        foreach (var d in n.interactivos)
            CrearInteractivo(d, props, juego, mats);

        var fugas = Grupo("FUGAS", raiz);
        List<Fuga> listaFugas = new List<Fuga>();
        foreach (var f in n.fugas)
            listaFugas.Add(CrearFuga(f, fugas, juego, mats["fuga"]));
        juego.fugas = listaFugas.ToArray();

        var luces = Grupo("LUCES", raiz);
        CrearLuces(n, luces);
        RenderSettings.ambientLight = new Color(0.24f, 0.25f, 0.28f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.07f);
        RenderSettings.fogStartDistance = 12f;
        RenderSettings.fogEndDistance = 46f;

        var jugador = CrearJugador(n, raiz, juego);
        juego.jugador = jugador;

        AgregarSalida(raiz);
        EditorSceneManager.SaveScene(escena, RUTA_ESCENA);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RUTA_ESCENA, true) };
        Debug.Log("NIVEL CONSTRUIDO: " + n.pisos.Length + " pisos, " + n.muros.Length +
                  " muros, " + n.techos.Length + " techos, " + n.interactivos.Length +
                  " interactivos, " + n.fugas.Length + " fugas.");
    }

    // ------------------------------------------------------------------ piezas
    static Dictionary<string, Material> Materiales()
    {
        var d = new Dictionary<string, Material>();
        d["piso"] = Mate("Piso", new Color(0.52f, 0.52f, 0.50f), 0.06f);
        d["muro"] = Mate("Muro", new Color(0.31f, 0.32f, 0.33f), 0.05f);
        d["techo"] = Mate("Techo", new Color(0.20f, 0.21f, 0.22f), 0.03f);
        d["plataforma"] = Mate("Plataforma", new Color(0.62f, 0.62f, 0.58f), 0.08f);
        d["valvula"] = Mate("Valvula", new Color(0.92f, 0.68f, 0.10f), 0.35f);
        d["puerta"] = Mate("Puerta", new Color(0.85f, 0.42f, 0.08f), 0.30f);
        d["fuga"] = Mate("Fuga", new Color(0.20f, 0.65f, 0.95f), 0.55f);
        d["agua"] = Transparente("Agua", new Color(0.16f, 0.55f, 0.72f, 0.55f));
        d["lampara"] = Emisivo("Lampara", new Color(1f, 0.85f, 0.55f));
        return d;
    }

    static Material Mate(string nombre, Color c, float brillo)
    {
        var m = new Material(Shader.Find("Standard")) { name = nombre, color = c };
        m.SetFloat("_Glossiness", brillo);
        m.SetFloat("_Metallic", 0.0f);
        return m;
    }

    static Material Transparente(string nombre, Color c)
    {
        var m = new Material(Shader.Find("Standard")) { name = nombre, color = c };
        m.SetFloat("_Mode", 3f);
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
        return m;
    }

    static Material Emisivo(string nombre, Color c)
    {
        var m = new Material(Shader.Find("Standard")) { name = nombre, color = c };
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * 1.6f);
        return m;
    }

    static Transform Grupo(string nombre, Transform padre)
    {
        var g = new GameObject(nombre).transform;
        g.SetParent(padre, false);
        return g;
    }

    static void Cajas(Caja[] cajas, string nombre, Transform padre, Material m, bool estatico)
    {
        var grupo = Grupo(nombre, padre);
        foreach (var c in cajas)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nombre;
            go.transform.SetParent(grupo, false);
            go.transform.position = new Vector3(c.p[0], c.p[1], c.p[2]);
            go.transform.localScale = new Vector3(c.s[0], c.s[1], c.s[2]);
            go.GetComponent<Renderer>().sharedMaterial = m;
            if (estatico) go.isStatic = true;
        }
    }

    // ------------------------------------------------------------------ agua
    static Transform CrearAgua(Nivel n, Transform padre, Material m, GestorNivel juego)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "AGUA";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(32f, n.agua.inicio, 27f);
        go.transform.localScale = new Vector3(7.2f, 1f, 5.8f);
        go.GetComponent<Renderer>().sharedMaterial = m;
        juego.aguaVisual = go.transform;
        juego.aguaBase = n.agua.inicio;
        juego.aguaRitmo = n.agua.ritmo;
        juego.aguaPorFuga = n.agua.por_fuga;
        juego.aguaMaxima = n.agua.maximo;
        return go.transform;
    }

    // ------------------------------------------------------------------ props
    static void CrearInteractivo(InteractivoDato d, Transform padre, GestorNivel juego,
                                 Dictionary<string, Material> mats)
    {
        var raiz = new GameObject(d.tipo.ToUpper() + "_" + d.id).transform;
        raiz.SetParent(padre, false);
        raiz.position = new Vector3(d.x, d.y, d.z);

        var visual = new GameObject("Visual").transform;
        visual.SetParent(raiz, false);

        if (d.tipo == "valvula")
        {
            var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cuerpo.name = "Cuerpo";
            cuerpo.transform.SetParent(visual, false);
            cuerpo.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
            cuerpo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cuerpo.GetComponent<Renderer>().sharedMaterial = mats["valvula"];

            for (int i = 0; i < 2; i++)
            {
                var asa = GameObject.CreatePrimitive(PrimitiveType.Cube);
                asa.name = "Asa";
                asa.transform.SetParent(visual, false);
                asa.transform.localScale = new Vector3(0.9f, 0.07f, 0.07f);
                asa.transform.localRotation = Quaternion.Euler(0f, 0f, 45f + i * 90f);
                asa.transform.localPosition = Vector3.zero;
                asa.GetComponent<Renderer>().sharedMaterial = mats["valvula"];
            }
        }
        else if (d.tipo == "puerta")
        {
            var hoja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hoja.name = "Hoja";
            hoja.transform.SetParent(visual, false);
            hoja.transform.localScale = new Vector3(2.6f, 3.0f, 0.22f);
            hoja.transform.localPosition = new Vector3(0f, 0f, 0f);
            hoja.GetComponent<Renderer>().sharedMaterial = mats["puerta"];
        }
        else
        {
            var aro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aro.name = "Aro";
            aro.transform.SetParent(visual, false);
            aro.transform.localScale = new Vector3(0.85f, 0.09f, 0.85f);
            aro.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            aro.GetComponent<Renderer>().sharedMaterial = mats["valvula"];
            for (int i = 0; i < 3; i++)
            {
                var radio = GameObject.CreatePrimitive(PrimitiveType.Cube);
                radio.name = "Radio";
                radio.transform.SetParent(visual, false);
                radio.transform.localScale = new Vector3(1.7f, 0.08f, 0.08f);
                radio.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
                radio.GetComponent<Renderer>().sharedMaterial = mats["valvula"];
            }
        }

        var col = raiz.gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2.2f, 2.6f, 2.2f);

        var fuente = raiz.gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0.75f;
        fuente.minDistance = 2f;
        fuente.maxDistance = 22f;

        var it = raiz.gameObject.AddComponent<Interactivo>();
        it.tipo = d.tipo == "valvula" ? TipoInteractivo.Valvula :
                  d.tipo == "puerta" ? TipoInteractivo.Puerta : TipoInteractivo.Rueda;
        it.id = d.id;
        it.gestor = juego;
        it.visual = visual;
        it.fuente = fuente;
        it.sonido = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/" + (d.tipo == "valvula" ? "valvula" :
                               d.tipo == "puerta" ? "puerta" : "rueda") + ".wav");
        juego.RegistrarInteractivo(it);
    }

    static Fuga CrearFuga(FugaDato d, Transform padre, GestorNivel juego, Material m)
    {
        var raiz = new GameObject("FUGA").transform;
        raiz.SetParent(padre, false);
        raiz.position = new Vector3(d.x, d.y, d.z);

        var chorro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chorro.name = "Chorro";
        chorro.transform.SetParent(raiz, false);
        chorro.transform.localScale = new Vector3(0.16f, 1.5f, 0.16f);
        chorro.transform.localPosition = new Vector3(0f, -0.75f, 0f);
        chorro.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(chorro.GetComponent<Collider>());

        var punto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        punto.name = "Punto";
        punto.transform.SetParent(raiz, false);
        punto.transform.localScale = Vector3.one * 0.28f;
        punto.GetComponent<Renderer>().sharedMaterial = m;

        var t = raiz.gameObject.AddComponent<Fuga>();
        t.gestor = juego;
        t.chorro = chorro;
        t.fuente = raiz.gameObject.AddComponent<AudioSource>();
        t.fuente.playOnAwake = false;
        t.fuente.spatialBlend = 0.8f;
        t.sonido = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sello.wav");
        return t;
    }

    static void CrearLuces(Nivel n, Transform padre)
    {
        var sol = new GameObject("Luz direccional");
        sol.transform.SetParent(padre, false);
        var l = sol.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.62f, 0.66f, 0.72f);
        l.intensity = 0.62f;
        l.shadows = LightShadows.Soft;
        sol.transform.rotation = Quaternion.Euler(52f, -34f, 0f);

        foreach (var d in n.luces)
        {
            var go = new GameObject("Luz " + d.color);
            go.transform.SetParent(padre, false);
            go.transform.position = new Vector3(d.x, d.y, d.z);
            var luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.range = d.rango;
            luz.intensity = d.intensidad;
            luz.color = d.color == "rojo" ? new Color(1f, 0.32f, 0.24f) :
                        d.color == "verde" ? new Color(0.42f, 1f, 0.52f) :
                        new Color(1f, 0.70f, 0.35f);
            luz.shadows = LightShadows.None;

            var bombilla = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bombilla.name = "Bombilla";
            bombilla.transform.SetParent(go.transform, false);
            bombilla.transform.localScale = Vector3.one * 0.22f;
            Object.DestroyImmediate(bombilla.GetComponent<Collider>());
            var mr = bombilla.GetComponent<Renderer>();
            mr.sharedMaterial = Mate("Luz_" + d.color, luz.color, 0.2f);
            mr.material.EnableKeyword("_EMISSION");
            mr.material.SetColor("_EmissionColor", luz.color * 2f);
        }
    }

    // ---------------------------------------------------------------- jugador
    static Jugador CrearJugador(Nivel n, Transform padre, GestorNivel juego)
    {
        var go = new GameObject("Jugador");
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(n.entrada.x, 1.15f, n.entrada.z);

        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.05f, 0f);
        cc.stepOffset = 0.45f;
        cc.slopeLimit = 50f;

        var cam = new GameObject("Camara").AddComponent<Camera>();
        cam.transform.SetParent(go.transform, false);
        cam.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 120f;
        cam.fieldOfView = 68f;
        cam.tag = "MainCamera";
        cam.gameObject.AddComponent<AudioListener>();

        var linterna = new GameObject("Linterna").AddComponent<Light>();
        linterna.transform.SetParent(cam.transform, false);
        linterna.type = LightType.Spot;
        linterna.range = 15f;
        linterna.intensity = 1.7f;
        linterna.spotAngle = 48f;
        linterna.color = new Color(1f, 0.94f, 0.82f);

        var j = go.AddComponent<Jugador>();
        j.camara = cam;
        j.gestor = juego;
        juego.jugador = j;

        var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cuerpo.name = "Cuerpo";
        cuerpo.transform.SetParent(go.transform, false);
        cuerpo.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
        Object.DestroyImmediate(cuerpo.GetComponent<Renderer>());
        return j;
    }

    static void AgregarSalida(Transform padre)
    {
        var go = new GameObject("Salida");
        go.transform.SetParent(padre, false);
        go.AddComponent<SalidaNivel>();
    }
}
