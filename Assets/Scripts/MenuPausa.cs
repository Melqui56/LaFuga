using UnityEngine;

public class MenuPausa : MonoBehaviour
{
    public static bool Abierto { get; private set; }

    GestorNivel gestor;

    public static void Crear(GestorNivel gestor)
    {
        Abierto = false;
        Time.timeScale = 1f;
        var menu = new GameObject("MenuPausa").AddComponent<MenuPausa>();
        menu.gestor = gestor;
    }

    void Update()
    {
        bool fin = gestor != null && gestor.Completado;
        if (fin && !Abierto) Abrir(true);
        else if (!fin && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))) Abrir(!Abierto);
    }

    void Abrir(bool valor)
    {
        Abierto = valor;
        bool fin = gestor != null && gestor.Completado;
        Time.timeScale = valor && !fin ? 0f : 1f;
        Jugador.BloquearCursor(!valor);
    }

    void OnDestroy()
    {
        Abierto = false;
        Time.timeScale = 1f;
    }

    void OnGUI()
    {
        if (!Abierto) return;
        bool fin = gestor != null && gestor.Completado;
        float w = Mathf.Max(320f, Screen.width * 0.24f), h = Screen.height * 0.07f;
        float x = (Screen.width - w) / 2f, y = Screen.height * (fin ? 0.58f : 0.36f);

        if (!fin)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var titulo = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(Screen.height * 0.06f),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(0, Screen.height * 0.2f, Screen.width, Screen.height * 0.12f), "PAUSA", titulo);
        }

        var boton = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(h * 0.4f) };
        if (!fin)
        {
            if (GUI.Button(new Rect(x, y, w, h), "Continuar", boton)) Abrir(false);
            y += h * 1.3f;
        }
        if (GUI.Button(new Rect(x, y, w, h), "Reiniciar nivel", boton))
        {
            Abrir(false);
            if (gestor != null) gestor.Reiniciar();
        }
        y += h * 1.3f;
        if (GUI.Button(new Rect(x, y, w, h), "Salir del juego", boton)) Application.Quit();
    }
}
