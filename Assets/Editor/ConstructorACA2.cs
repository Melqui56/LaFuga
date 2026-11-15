using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// Evoluciona el nivel del ACA 1 para el ACA 2: colector final, guardias con IA,
// salud, HUD y Blend Tree. Corre despues de ConstructorNivel y es idempotente.
public static class ConstructorACA2
{
    const string ESCENA = "Assets/Scenes/LaFuga.unity";
    const string CARPETA = "Assets/ACA2";
    const float PISO_F = -1.5f;

    static Material mMuro, mPiso, mTecho, mCobertura, mTubo, mBarril, mFranja, mNegro, mCompuerta,
                    mSalida, mPantalla, mGuardia, mVisor, mArma, mLampara;

    [MenuItem("La Fuga/Construir ACA 2")]
    public static void Construir()
    {
        ConstructorNivel.Construir();
        var escena = EditorSceneManager.OpenScene(ESCENA);
        Directory.CreateDirectory(CARPETA);
        Materiales();

        var nivel = GameObject.Find("NIVEL").transform;
        var gestor = Object.FindAnyObjectByType<GestorNivel>();
        var jugador = Object.FindAnyObjectByType<Jugador>();

        QuitarMuroNorteCamara(nivel);
        var colector = new GameObject("COLECTOR").transform;
        colector.SetParent(nivel, false);
        var compuerta = ConstruirColector(colector);
        var punto = new GameObject("PuntoControl").transform;
        punto.SetParent(colector, false);
        punto.position = new Vector3(56f, 1.2f, 50f);

        var salida = Object.FindAnyObjectByType<SalidaNivel>();
        if (salida != null) salida.transform.position = new Vector3(56f, PISO_F + 1f, 83f);

        var salud = jugador.gameObject.AddComponent<SaludJugador>();
        gestor.salud = salud;
        gestor.compuertaColector = compuerta;
        gestor.puntoControl = punto;

        var controlador = CrearAnimator();
        var guardias = new GameObject("GUARDIAS").transform;
        guardias.SetParent(nivel, false);
        CrearGuardias(guardias, controlador);

        HornearNavMesh(nivel, jugador.gameObject, guardias.gameObject);
        CrearHUD(nivel, jugador, salud, gestor);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("ACA 2 CONSTRUIDO");
    }

    // ------------------------------------------------------------ materiales
    static void Materiales()
    {
        mMuro = Mate("F_Muro", new Color(0.36f, 0.34f, 0.31f), 0.1f);
        mPiso = Mate("F_Piso", new Color(0.27f, 0.29f, 0.29f), 0.4f);
        mTecho = Mate("F_Techo", new Color(0.16f, 0.17f, 0.18f), 0.05f);
        mCobertura = Mate("F_Cobertura", new Color(0.52f, 0.50f, 0.46f), 0.1f);
        mTubo = Mate("F_Tubo", new Color(0.30f, 0.42f, 0.38f), 0.5f);
        mBarril = Mate("F_Barril", new Color(0.55f, 0.22f, 0.10f), 0.3f);
        mFranja = Brillo("F_Franja", new Color(1f, 0.78f, 0.1f), 0.8f);
        mNegro = Mate("F_Negro", new Color(0.05f, 0.05f, 0.05f), 0.1f);
        mCompuerta = Mate("F_Compuerta", new Color(0.85f, 0.42f, 0.08f), 0.3f);
        mSalida = Brillo("F_Salida", new Color(0.2f, 1f, 0.45f), 2.2f);
        mPantalla = Brillo("F_Pantalla", new Color(0.25f, 0.75f, 1f), 1.4f);
        mGuardia = Mate("G_Cuerpo", new Color(0.62f, 0.12f, 0.10f), 0.45f);
        mVisor = Brillo("G_Visor", new Color(0.2f, 0.9f, 0.4f), 2f);
        mArma = Mate("G_Arma", new Color(0.25f, 0.25f, 0.27f), 0.6f);
        mLampara = Brillo("F_Lampara", new Color(1f, 0.75f, 0.4f), 1.6f);
    }

    static Material Mate(string nombre, Color c, float brillo)
    {
        var m = new Material(Shader.Find("Standard")) { name = nombre, color = c };
        m.SetFloat("_Glossiness", brillo);
        return Guardar(m);
    }

    static Material Brillo(string nombre, Color c, float intensidad)
    {
        var m = new Material(Shader.Find("Standard")) { name = nombre, color = c };
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensidad);
        return Guardar(m);
    }

    static Material Guardar(Material m)
    {
        string ruta = CARPETA + "/" + m.name + ".mat";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    // ------------------------------------------------------------ geometria
    static void QuitarMuroNorteCamara(Transform nivel)
    {
        foreach (var t in nivel.GetComponentsInChildren<Transform>())
            if (t.name == "Muro" && Vector3.Distance(t.position, new Vector3(56f, 2.1f, 53f)) < 0.1f)
            {
                Object.DestroyImmediate(t.gameObject);
                return;
            }
    }

    static GameObject Caja(Transform padre, string nombre, Vector3 p, Vector3 s, Material m, float giroX = 0f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.position = p;
        go.transform.localScale = s;
        go.transform.rotation = Quaternion.Euler(giroX, 0f, 0f);
        go.GetComponent<Renderer>().sharedMaterial = m;
        go.isStatic = true;
        return go;
    }

    static GameObject Cilindro(Transform padre, string nombre, Vector3 p, Vector3 s, Material m, Vector3 giro)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.position = p;
        go.transform.localScale = s;
        go.transform.rotation = Quaternion.Euler(giro);
        go.GetComponent<Renderer>().sharedMaterial = m;
        go.isStatic = true;
        return go;
    }

    static void Luz(Transform padre, Vector3 p, Color c, float rango, float intensidad)
    {
        var l = new GameObject("Luz").AddComponent<Light>();
        l.transform.SetParent(padre, false);
        l.transform.position = p;
        l.type = LightType.Point;
        l.color = c;
        l.range = rango;
        l.intensity = intensidad;
    }

    static void Letrero(Transform padre, string texto, Vector3 p, float tam, Color c, float giroY = 0f)
    {
        var go = new GameObject("Letrero " + texto);
        go.transform.SetParent(padre, false);
        go.transform.position = p;
        go.transform.rotation = Quaternion.Euler(0f, giroY, 0f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = texto;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 64;
        tm.characterSize = tam;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = c;
        go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
    }

    static Transform ConstruirColector(Transform raiz)
    {
        var estructura = new GameObject("Estructura").transform; estructura.SetParent(raiz, false);
        var cobertura = new GameObject("Cobertura").transform; cobertura.SetParent(raiz, false);
        var validacion = new GameObject("ObjetosValidacion").transform; validacion.SetParent(raiz, false);
        var guia = new GameObject("LineasGuia").transform; guia.SetParent(raiz, false);

        Caja(estructura, "Piso F", new Vector3(56f, PISO_F - 0.15f, 65f), new Vector3(32f, 0.3f, 24f), mPiso);
        Caja(estructura, "Techo F", new Vector3(56f, 5.65f, 65f), new Vector3(32f, 0.3f, 24f), mTecho);
        Caja(estructura, "Muro oeste F", new Vector3(40f, 2f, 65f), new Vector3(0.3f, 7f, 24f), mMuro);
        Caja(estructura, "Muro este F", new Vector3(72f, 2f, 65f), new Vector3(0.3f, 7f, 24f), mMuro);
        Caja(estructura, "Muro sur F a", new Vector3(47.25f, 2f, 53f), new Vector3(14.5f, 7f, 0.3f), mMuro);
        Caja(estructura, "Muro sur F b", new Vector3(64.75f, 2f, 53f), new Vector3(14.5f, 7f, 0.3f), mMuro);
        Caja(estructura, "Dintel compuerta", new Vector3(56f, 4.85f, 53f), new Vector3(3f, 1.3f, 0.3f), mMuro);
        Caja(estructura, "Muro norte F a", new Vector3(46.5f, 2f, 77f), new Vector3(13f, 7f, 0.3f), mMuro);
        Caja(estructura, "Muro norte F b", new Vector3(65.5f, 2f, 77f), new Vector3(13f, 7f, 0.3f), mMuro);
        Caja(estructura, "Dintel salida", new Vector3(56f, 4f, 77f), new Vector3(6f, 3f, 0.3f), mMuro);

        Caja(estructura, "Mirador", new Vector3(56f, -0.75f, 55f), new Vector3(12f, 1.5f, 4f), mPiso);
        Caja(estructura, "Rampa", new Vector3(56f, -0.9f, 60f), new Vector3(4f, 0.3f, 6.2f), mPiso, 14f);
        Caja(estructura, "Baranda a", new Vector3(52f, 0.5f, 57f), new Vector3(4f, 1f, 0.2f), mNegro);
        Caja(estructura, "Baranda b", new Vector3(60f, 0.5f, 57f), new Vector3(4f, 1f, 0.2f), mNegro);
        for (int i = 0; i < 6; i++)
            Caja(guia, "Franja peligro", new Vector3(50.5f + i * 2.2f, 0.01f, 56.8f), new Vector3(1.1f, 0.02f, 0.3f), i % 2 == 0 ? mFranja : mNegro);

        Caja(estructura, "Puente pasarela", new Vector3(64f, -0.75f, 55.5f), new Vector3(4f, 1.5f, 3f), mPiso);
        Caja(estructura, "Pasarela este", new Vector3(69f, -0.75f, 65f), new Vector3(6f, 1.5f, 22f), mCobertura);
        Caja(estructura, "Baranda pasarela", new Vector3(66.1f, 0.5f, 67f), new Vector3(0.2f, 1f, 16f), mNegro);

        Caja(estructura, "Piso salida", new Vector3(56f, PISO_F - 0.15f, 81f), new Vector3(8f, 0.3f, 8f), mPiso);
        Caja(estructura, "Techo salida", new Vector3(56f, 2.85f, 81f), new Vector3(8f, 0.3f, 8f), mTecho);
        Caja(estructura, "Muro salida oeste", new Vector3(52f, 0.6f, 81f), new Vector3(0.3f, 4.2f, 8f), mMuro);
        Caja(estructura, "Muro salida este", new Vector3(60f, 0.6f, 81f), new Vector3(0.3f, 4.2f, 8f), mMuro);
        Caja(estructura, "Muro salida norte", new Vector3(56f, 0.6f, 85f), new Vector3(8f, 4.2f, 0.3f), mMuro);
        var escotilla = Caja(validacion, "Escotilla de salida", new Vector3(56f, 0.1f, 84.8f), new Vector3(2.4f, 2.8f, 0.1f), mSalida);
        Object.DestroyImmediate(escotilla.GetComponent<Collider>());
        Luz(guia, new Vector3(56f, 1.2f, 83.5f), new Color(0.3f, 1f, 0.5f), 14f, 2.6f);
        Letrero(validacion, "SALIDA DE MANTENIMIENTO", new Vector3(56f, 3.2f, 76.8f), 0.07f, new Color(0.3f, 1f, 0.5f));
        Letrero(validacion, "COLECTOR 3", new Vector3(56f, 4.4f, 52.8f), 0.09f, new Color(1f, 0.78f, 0.1f));

        Caja(cobertura, "Muro bajo 1", new Vector3(48f, PISO_F + 0.6f, 64f), new Vector3(4f, 1.2f, 0.6f), mCobertura);
        Caja(cobertura, "Muro bajo 2", new Vector3(62f, PISO_F + 0.6f, 66f), new Vector3(0.6f, 1.2f, 4f), mCobertura);
        Caja(cobertura, "Muro bajo 3", new Vector3(52f, PISO_F + 0.6f, 70f), new Vector3(5f, 1.2f, 0.6f), mCobertura);
        Caja(cobertura, "Muro bajo 4", new Vector3(60f, PISO_F + 0.6f, 73f), new Vector3(4f, 1.2f, 0.6f), mCobertura);
        Caja(cobertura, "Refugio lado sur", new Vector3(42f, 0.5f, 67.5f), new Vector3(4f, 4f, 0.3f), mMuro);
        Caja(cobertura, "Refugio lado norte", new Vector3(42f, 0.5f, 73.5f), new Vector3(4f, 4f, 0.3f), mMuro);
        Caja(cobertura, "Refugio techo", new Vector3(42f, 2.6f, 70.5f), new Vector3(4f, 0.3f, 6.3f), mTecho);

        foreach (float x in new[] { 48f, 64f })
            foreach (float z in new[] { 61f, 68f, 75f })
            {
                Caja(estructura, "Pilar", new Vector3(x, 2f, z), new Vector3(0.8f, 7f, 0.8f), mMuro);
                Caja(guia, "Lampara", new Vector3(x + (x < 56f ? 0.45f : -0.45f), 3.2f, z), new Vector3(0.12f, 0.35f, 0.35f), mLampara);
                Luz(guia, new Vector3(x + (x < 56f ? 1.2f : -1.2f), 3f, z), new Color(1f, 0.72f, 0.42f), 7f, 1.3f);
            }
        Caja(guia, "Franja hacia la salida", new Vector3(56f, PISO_F + 0.01f, 70f), new Vector3(0.35f, 0.02f, 13.5f), mFranja);

        Cilindro(validacion, "Tubo colector oeste", new Vector3(41.2f, 3.6f, 65f), new Vector3(1.6f, 11.5f, 1.6f), mTubo, new Vector3(90f, 0f, 0f));
        Cilindro(validacion, "Tubo colector este", new Vector3(70.9f, 4.3f, 65f), new Vector3(1.1f, 11.5f, 1.1f), mTubo, new Vector3(90f, 0f, 0f));
        foreach (var z in new[] { 58f, 64f, 76f })
            Cilindro(validacion, "Tubo vertical", new Vector3(40.6f, 2f, z), new Vector3(0.5f, 3.5f, 0.5f), mTubo, Vector3.zero);
        var barriles = new[] { new Vector3(45f, 0f, 58f), new Vector3(45.9f, 0f, 58.4f), new Vector3(66f, 0f, 75f), new Vector3(57.5f, 0f, 63f), new Vector3(53f, 0f, 79f) };
        foreach (var b in barriles)
            Cilindro(validacion, "Barril", new Vector3(b.x, PISO_F + 0.6f, b.z), new Vector3(0.8f, 0.6f, 0.8f), mBarril, Vector3.zero);
        Caja(validacion, "Tablero de control", new Vector3(58.6f, PISO_F + 0.7f, 83.5f), new Vector3(1.2f, 1.4f, 0.5f), mNegro);
        var pantalla = Caja(validacion, "Pantalla", new Vector3(58.6f, PISO_F + 1f, 83.24f), new Vector3(0.9f, 0.5f, 0.02f), mPantalla);
        Object.DestroyImmediate(pantalla.GetComponent<Collider>());

        var compuerta = Caja(estructura, "Compuerta del colector", new Vector3(56f, 2.1f, 53f), new Vector3(3f, 4.2f, 0.25f), mCompuerta);
        compuerta.isStatic = false;
        return compuerta.transform;
    }

    // ------------------------------------------------------------ guardias
    struct DatoGuardia
    {
        public string nombre;
        public Vector3[] ruta;
        public float patrulla, persecucion, deteccion;
    }

    static void CrearGuardias(Transform padre, AnimatorController controlador)
    {
        var datos = new[]
        {
            new DatoGuardia { nombre = "Guardia 1 galeria", patrulla = 1.3f, persecucion = 3.0f, deteccion = 6f,
                ruta = new[] { new Vector3(30f, 0f, 3.5f), new Vector3(44f, 0f, 3.5f), new Vector3(44f, 0f, 8.5f), new Vector3(30f, 0f, 8.5f) } },
            new DatoGuardia { nombre = "Guardia 2 compuerta", patrulla = 1.5f, persecucion = 3.4f, deteccion = 7f,
                ruta = new[] { new Vector3(54f, 0f, 18f), new Vector3(54f, 0f, 34f) } },
            new DatoGuardia { nombre = "Guardia 3 colector", patrulla = 1.8f, persecucion = 4.0f, deteccion = 9f,
                ruta = new[] { new Vector3(50f, PISO_F, 62f), new Vector3(63f, PISO_F, 62f), new Vector3(63f, PISO_F, 75f), new Vector3(50f, PISO_F, 75f) } },
            new DatoGuardia { nombre = "Guardia 4 pasarela", patrulla = 1.6f, persecucion = 3.8f, deteccion = 8f,
                ruta = new[] { new Vector3(69f, 0f, 59f), new Vector3(69f, 0f, 75f) } },
        };
        foreach (var d in datos) CrearGuardia(padre, controlador, d);
    }

    static void CrearGuardia(Transform padre, AnimatorController controlador, DatoGuardia d)
    {
        var raiz = new GameObject(d.nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.position = d.ruta[0];

        var agente = raiz.AddComponent<NavMeshAgent>();
        agente.radius = 0.45f;
        agente.height = 1.9f;
        agente.angularSpeed = 360f;
        agente.acceleration = 12f;
        agente.stoppingDistance = 0.2f;

        var cuerpo = new GameObject("Cuerpo");
        cuerpo.transform.SetParent(raiz.transform, false);
        var animador = cuerpo.AddComponent<Animator>();
        animador.runtimeAnimatorController = controlador;

        var torso = Pivote(cuerpo.transform, "Torso", new Vector3(0f, 1.25f, 0f));
        Pieza(torso, "TorsoMalla", PrimitiveType.Cube, Vector3.zero, new Vector3(0.7f, 0.8f, 0.45f), mGuardia, true);
        var cabeza = Pieza(torso, "Cabeza", PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(0.45f, 0.35f, 0.4f), mGuardia, true);
        var visor = Pieza(cabeza, "Visor", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.21f), new Vector3(0.36f, 0.1f, 0.02f), mVisor, true);
        foreach (var lado in new[] { -1f, 1f })
        {
            var cadera = Pivote(cuerpo.transform, lado < 0 ? "PiernaI" : "PiernaD", new Vector3(0.2f * lado, 0.85f, 0f));
            Pieza(cadera, "Pierna", PrimitiveType.Cube, new Vector3(0f, -0.42f, 0f), new Vector3(0.22f, 0.85f, 0.25f), mGuardia, true);
        }
        Pivote(torso, "BrazoI", new Vector3(-0.45f, 0.3f, 0f));
        Pieza(torso.Find("BrazoI"), "Brazo", PrimitiveType.Cube, new Vector3(0f, -0.35f, 0f), new Vector3(0.18f, 0.7f, 0.18f), mGuardia, true);
        var brazoD = Pivote(torso, "BrazoD", new Vector3(0.45f, 0.3f, 0f));
        Pieza(brazoD, "Brazo", PrimitiveType.Cube, new Vector3(0f, -0.35f, 0f), new Vector3(0.18f, 0.7f, 0.18f), mGuardia, true);
        var arma = Pieza(brazoD, "Llave", PrimitiveType.Cube, new Vector3(0f, -0.75f, 0.35f), new Vector3(0.12f, 0.12f, 0.8f), mArma);
        var col = arma.GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3f, 3f, 1.6f);
        var rb = arma.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        var armaScript = arma.gameObject.AddComponent<ArmaEnemigo>();
        cuerpo.AddComponent<EventosArma>().arma = armaScript;

        var ia = raiz.AddComponent<EnemigoIA>();
        ia.visor = visor.GetComponent<Renderer>();
        ia.velocidadPatrulla = d.patrulla;
        ia.velocidadPersecucion = d.persecucion;
        ia.radioDeteccion = d.deteccion;
        var puntos = new List<Transform>();
        for (int i = 0; i < d.ruta.Length; i++)
        {
            var p = new GameObject(d.nombre + " punto " + (i + 1)).transform;
            p.SetParent(padre, false);
            p.position = d.ruta[i];
            puntos.Add(p);
        }
        ia.puntosPatrulla = puntos.ToArray();
    }

    static Transform Pieza(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material m, bool sinColision = false)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.GetComponent<Renderer>().sharedMaterial = m;
        if (sinColision) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go.transform;
    }

    static Transform Pivote(Transform padre, string nombre, Vector3 pos)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = pos;
        return t;
    }

    // ------------------------------------------------------------ animator
    static AnimatorController CrearAnimator()
    {
        var idle = Clip("Guardia_Quieto", 2f, true, new Dictionary<string, float[]> {
            { "Torso/BrazoI", new[] { 0f, 3f, 0f } }, { "Torso/BrazoD", new[] { 0f, -3f, 0f } } },
            new[] { 1.25f, 1.28f, 1.25f });
        var caminar = Clip("Guardia_Caminar", 1f, true, new Dictionary<string, float[]> {
            { "PiernaI", new[] { 25f, 0f, -25f, 0f, 25f } }, { "PiernaD", new[] { -25f, 0f, 25f, 0f, -25f } },
            { "Torso/BrazoI", new[] { -20f, 0f, 20f, 0f, -20f } }, { "Torso/BrazoD", new[] { 20f, 0f, -20f, 0f, 20f } } },
            new[] { 1.25f, 1.20f, 1.25f, 1.20f, 1.25f });
        var correr = Clip("Guardia_Correr", 0.55f, true, new Dictionary<string, float[]> {
            { "PiernaI", new[] { 45f, 0f, -45f, 0f, 45f } }, { "PiernaD", new[] { -45f, 0f, 45f, 0f, -45f } },
            { "Torso/BrazoI", new[] { -40f, 0f, 40f, 0f, -40f } }, { "Torso/BrazoD", new[] { 40f, 0f, -40f, 0f, 40f } } },
            new[] { 1.22f, 1.12f, 1.22f, 1.12f, 1.22f });
        var atacar = Clip("Guardia_Atacar", 0.6f, false, new Dictionary<string, float[]> {
            { "Torso/BrazoD", new[] { 0f, -140f, 40f, 10f, 0f } }, { "Torso/BrazoI", new[] { 0f, 10f, -10f, 0f, 0f } } },
            new[] { 1.25f, 1.27f, 1.18f, 1.22f, 1.25f });
        AnimationUtility.SetAnimationEvents(atacar, new[]
        {
            new AnimationEvent { time = 0.15f, functionName = "ActivarDano" },
            new AnimationEvent { time = 0.42f, functionName = "DesactivarDano" },
        });

        string ruta = CARPETA + "/Guardia.controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        var sm = ctrl.layers[0].stateMachine;
        var loco = ctrl.CreateBlendTreeInController("Locomocion", out BlendTree arbol, 0);
        arbol.blendType = BlendTreeType.Simple1D;
        arbol.blendParameter = "Velocidad";
        arbol.useAutomaticThresholds = false;
        arbol.AddChild(idle, 0f);
        arbol.AddChild(caminar, 1.6f);
        arbol.AddChild(correr, 3.8f);
        sm.defaultState = loco;
        var golpe = sm.AddState("Atacar");
        golpe.motion = atacar;
        var ida = sm.AddAnyStateTransition(golpe);
        ida.AddCondition(AnimatorConditionMode.If, 0f, "Atacar");
        ida.hasExitTime = false;
        ida.duration = 0.05f;
        ida.canTransitionToSelf = false;
        var vuelta = golpe.AddTransition(loco);
        vuelta.hasExitTime = true;
        vuelta.exitTime = 0.95f;
        vuelta.duration = 0.12f;
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    static AnimationClip Clip(string nombre, float duracion, bool loop, Dictionary<string, float[]> giros, float[] alturaTorso)
    {
        var clip = new AnimationClip { name = nombre, frameRate = 30f };
        foreach (var kv in giros)
            clip.SetCurve(kv.Key, typeof(Transform), "localEulerAnglesRaw.x", Curva(kv.Value, duracion));
        clip.SetCurve("Torso", typeof(Transform), "localPosition.y", Curva(alturaTorso, duracion));
        var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);
        string ruta = CARPETA + "/" + nombre + ".anim";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(clip, ruta);
        return clip;
    }

    static AnimationCurve Curva(float[] v, float duracion)
    {
        var c = new AnimationCurve();
        for (int i = 0; i < v.Length; i++) c.AddKey(duracion * i / (v.Length - 1), v[i]);
        for (int i = 0; i < c.keys.Length; i++) c.SmoothTangents(i, 0f);
        return c;
    }

    // ------------------------------------------------------------ navmesh
    static void HornearNavMesh(Transform nivel, GameObject jugador, GameObject guardias)
    {
        var superficie = nivel.gameObject.AddComponent<NavMeshSurface>();
        superficie.collectObjects = CollectObjects.Children;
        superficie.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        jugador.SetActive(false);
        guardias.SetActive(false);
        superficie.BuildNavMesh();
        jugador.SetActive(true);
        guardias.SetActive(true);
        string ruta = CARPETA + "/NavMesh_LaFuga.asset";
        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(superficie.navMeshData, ruta);
        Debug.Log("NAVMESH HORNEADO");
    }

    // ------------------------------------------------------------ HUD
    static void CrearHUD(Transform nivel, Jugador jugador, SaludJugador salud, GestorNivel gestor)
    {
        var go = new GameObject("HUD");
        go.transform.SetParent(nivel, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = jugador.camara;
        canvas.planeDistance = 0.3f;
        var escalador = go.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        var hud = go.AddComponent<HUD>();
        hud.salud = salud;
        hud.gestor = gestor;

        var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var destello = Imagen(go.transform, "Destello golpe", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.8f, 0f, 0f, 0f));
        hud.destelloGolpe = destello;

        var panel = Imagen(go.transform, "Panel estado", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(430f, 170f), new Color(0f, 0f, 0f, 0.45f));
        panel.rectTransform.pivot = new Vector2(0f, 1f);
        hud.textoSalud = Texto(panel.transform, fuente, "SALUD 100", new Vector2(20f, -14f), 26, TextAnchor.UpperLeft);
        Barra(panel.transform, new Vector2(20f, -50f), new Color(0.30f, 0.85f, 0.40f), out hud.barraSalud, out hud.rellenoSalud);
        hud.textoAgua = Texto(panel.transform, fuente, "AGUA 0%", new Vector2(20f, -78f), 22, TextAnchor.UpperLeft);
        Barra(panel.transform, new Vector2(20f, -110f), new Color(0.2f, 0.6f, 0.95f), out hud.barraAgua, out hud.rellenoAgua);
        hud.textoValvulas = Texto(panel.transform, fuente, "VALVULAS 0/2   FUGAS 0/" + gestor.FugasTotales, new Vector2(20f, -132f), 20, TextAnchor.UpperLeft);

        var franja = Imagen(go.transform, "Franja objetivo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(900f, 54f), new Color(0f, 0f, 0f, 0.45f));
        franja.rectTransform.pivot = new Vector2(0.5f, 1f);
        hud.textoObjetivo = Texto(franja.transform, fuente, "Sella las fugas (clic) y gira las dos valvulas (E)", Vector2.zero, 24, TextAnchor.MiddleCenter);
        var rtObj = hud.textoObjetivo.rectTransform;
        rtObj.anchorMin = Vector2.zero; rtObj.anchorMax = Vector2.one; rtObj.offsetMin = Vector2.zero; rtObj.offsetMax = Vector2.zero;

        var mira = Imagen(go.transform, "Mira", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f), new Color(1f, 1f, 1f, 0.8f));
        mira.raycastTarget = false;
    }

    static Image Imagen(Transform padre, string nombre, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 tam, Color c)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        var img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    static Text Texto(Transform padre, Font f, string valor, Vector2 pos, int tam, TextAnchor ancla)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400f, 34f);
        var t = go.GetComponent<Text>();
        t.font = f;
        t.text = valor;
        t.fontSize = tam;
        t.alignment = ancla;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static void Barra(Transform padre, Vector2 pos, Color c, out RectTransform relleno, out Image imagen)
    {
        var fondo = Imagen(padre, "Barra fondo", new Vector2(0f, 1f), new Vector2(0f, 1f), pos, new Vector2(380f, 16f), new Color(1f, 1f, 1f, 0.15f));
        fondo.rectTransform.pivot = new Vector2(0f, 1f);
        imagen = Imagen(fondo.transform, "Relleno", Vector2.zero, new Vector2(1f, 1f), Vector2.zero, Vector2.zero, c);
        relleno = imagen.rectTransform;
        relleno.pivot = new Vector2(0f, 0.5f);
    }
}
