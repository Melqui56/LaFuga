using System.Collections;
using System.IO;
using UnityEngine;

// Recorre el nivel con el mismo Jugador que usa el teclado y guarda un PNG por
// cuadro. Se usa para el video de evidencia: no hay teclado ni raton inyectado.
public class BotGrabacion : MonoBehaviour
{
    public Jugador jugador;
    public GestorNivel gestor;
    public Camera camara;
    public int ancho = 1280, alto = 720;
    public string carpeta = "/tmp/opencode/frames";
    public int maximoCuadros = 2400;

    int cuadro;
    Texture2D lienzo;

    void Start()
    {
        jugador = Object.FindAnyObjectByType<Jugador>();
        gestor = Object.FindAnyObjectByType<GestorNivel>();
        camara = jugador != null ? jugador.camara : Camera.main;
        Directory.CreateDirectory(carpeta);
        lienzo = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        Time.captureFramerate = 24;
        StartCoroutine(Guion());
    }

    // ------------------------------------------------------------------ guion
    IEnumerator Guion()
    {
        yield return Esperar(0.8f);

        // 1. entrada: ver el agua y el tunel
        yield return Ir(new Vector3(8f, 1f, 6f), false);
        yield return Mirar(new Vector3(20f, 1.4f, 6f), 0.6f);

        // 2. primera fuga: se sella con el disparo
        yield return Mirar(new Vector3(12f, 1.6f, 2f), 0.5f);
        yield return Disparar();
        yield return Esperar(0.9f);

        // 3. galeria: segunda y tercera fuga
        yield return Ir(new Vector3(30f, 1f, 6f), true);
        yield return Mirar(new Vector3(26f, 1.9f, 1f), 0.4f);
        yield return Disparar();
        yield return Ir(new Vector3(38f, 1f, 6f), true);
        yield return Mirar(new Vector3(34f, 1.3f, 11f), 0.4f);
        yield return Disparar();
        yield return Esperar(0.7f);

        // 4. valvula 1
        yield return Ir(new Vector3(47.5f, 1f, 8f), true);
        yield return Mirar(new Vector3(49.4f, 1.15f, 8f), 0.5f);
        yield return Interactuar();

        // 5. pozo: tres saltos
        yield return Mirar(new Vector3(56f, 1f, 6f), 0.4f);
        yield return Ir(new Vector3(51.2f, 1f, 6f), false);
        yield return Ir(new Vector3(53.5f, 1f, 5f), false, true);
        yield return Ir(new Vector3(56.5f, 1f, 7f), false, true);
        yield return Ir(new Vector3(59.3f, 1f, 6f), false, true);
        yield return Esperar(0.5f);

        // 6. compuerta: valvula 2
        yield return Ir(new Vector3(56.5f, 1f, 16f), false);
        yield return Ir(new Vector3(57.5f, 1f, 24f), false);
        yield return Mirar(new Vector3(51f, 1.8f, 22f), 0.4f);
        yield return Disparar();
        yield return Ir(new Vector3(58.6f, 1f, 26f), false);
        yield return Mirar(new Vector3(60.4f, 1.15f, 26f), 0.5f);
        yield return Interactuar();

        // 7. la puerta se abre con las dos valvulas
        yield return Ir(new Vector3(56.5f, 1f, 37.3f), false);
        yield return Mirar(new Vector3(56.5f, 1.5f, 39f), 0.6f);
        yield return Interactuar();
        yield return Esperar(2.2f);

        // 8. camara de la compuerta: rueda de cierre
        yield return Ir(new Vector3(56.5f, 1f, 42f), false);
        yield return Ir(new Vector3(60.0f, 1f, 45f), false);
        yield return Mirar(new Vector3(62.4f, 1.3f, 45f), 0.6f);
        yield return Interactuar();
        yield return Esperar(2.6f);

        yield return Esperar(0.6f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(0);
#else
        Application.Quit();
#endif
    }

    IEnumerator Ir(Vector3 destino, bool correr, bool saltar = false)
    {
        float limite = Time.time + 14f;
        while (Time.time < limite)
        {
            Vector3 plano = new Vector3(destino.x, jugador.transform.position.y, destino.z);
            Vector3 dif = plano - jugador.transform.position;
            if (dif.magnitude < 0.7f) break;
            Apuntar(plano, 7f);
            bool brincar = saltar && dif.magnitude < 1.8f;
            jugador.Mover(new Vector2(0f, 1f), correr || saltar, brincar);
            yield return null;
        }
        yield return null;
    }

    IEnumerator Mirar(Vector3 objetivo, float segundos)
    {
        float fin = Time.time + segundos;
        while (Time.time < fin)
        {
            Apuntar(objetivo, 6f);
            jugador.Mover(Vector2.zero, false, false);
            yield return null;
        }
    }

    void Apuntar(Vector3 objetivo, float gradosPorCuadro)
    {
        Vector3 origen = camara.transform.position;
        Vector3 dir = objetivo - origen;
        if (dir.sqrMagnitude < 0.001f) return;
        float yawObj = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float pitchObj = -Mathf.Asin(dir.normalized.y) * Mathf.Rad2Deg;
        float yawAct = jugador.transform.eulerAngles.y;
        float pitchAct = camara.transform.localEulerAngles.x;
        if (pitchAct > 180f) pitchAct -= 360f;
        float dy = Mathf.Clamp(Mathf.DeltaAngle(yawAct, yawObj), -gradosPorCuadro, gradosPorCuadro);
        float dp = Mathf.Clamp(pitchObj - pitchAct, -gradosPorCuadro, gradosPorCuadro);
        jugador.Mirar(dy, -dp);
    }

    IEnumerator Disparar()
    {
        Debug.Log("ACCION disparo " + cuadro);
        jugador.IntentarDisparar();
        yield return null;
        yield return null;
    }

    IEnumerator Interactuar()
    {
        Debug.Log("ACCION interactuar " + cuadro);
        gestor.InteractuarCerca();
        yield return null;
        yield return null;
    }

    IEnumerator Esperar(float segundos)
    {
        float fin = Time.time + segundos;
        while (Time.time < fin)
        {
            jugador.Mover(Vector2.zero, false, false);
            yield return null;
        }
    }

    void LateUpdate()
    {
        if (cuadro >= maximoCuadros) return;
        var rt = RenderTexture.GetTemporary(ancho, alto, 24);
        camara.targetTexture = rt;
        camara.Render();
        RenderTexture.active = rt;
        lienzo.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        lienzo.Apply();
        RenderTexture.active = null;
        camara.targetTexture = null;
        File.WriteAllBytes(Path.Combine(carpeta, "f" + cuadro.ToString("D5") + ".png"),
                           lienzo.EncodeToPNG());
        RenderTexture.ReleaseTemporary(rt);
        cuadro++;
    }
}
