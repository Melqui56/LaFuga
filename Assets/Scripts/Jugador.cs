using UnityEngine;

// Movimiento, camara y disparo del jugador. Todo pasa por metodos publicos
// (Mover, Mirar, IntentarDisparar) para que el bot de grabacion use exactamente
// el mismo camino que el teclado y el raton.
public class Jugador : MonoBehaviour
{
    public Camera camara;
    public GestorNivel gestor;

    public float velocidad = 4.2f;
    public float velocidadCorrer = 6.4f;
    public float alturaSalto = 1.15f;
    public float gravedad = -19f;
    public float cooldown = 0.8f;
    public float alcanceDisparo = 14f;

    CharacterController cc;
    Vector3 velocidadVertical;
    float yaw, pitch;
    float ultimoDisparo = -99f;
    bool bloqueado;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (bloqueado) return;
        Mirar(Input.GetAxis("Mouse X") * 2.2f, Input.GetAxis("Mouse Y") * 2.2f);
        Mover(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")),
              Input.GetKey(KeyCode.LeftShift), Input.GetKeyDown(KeyCode.Space));
        if (Input.GetMouseButton(0)) IntentarDisparar();
        if (Input.GetKeyDown(KeyCode.E) && gestor != null) gestor.InteractuarCerca();
        if (Input.GetKeyDown(KeyCode.Escape)) Cursor.lockState = CursorLockMode.None;
    }

    public void Mirar(float dx, float dy)
    {
        yaw += dx;
        pitch = Mathf.Clamp(pitch - dy, -80f, 80f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        camara.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void Mover(Vector2 entrada, bool correr, bool saltar)
    {
        Vector3 dir = transform.right * entrada.x + transform.forward * entrada.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        float v = correr ? velocidadCorrer : velocidad;

        if (cc.isGrounded && velocidadVertical.y < 0f) velocidadVertical.y = -2f;
        if (saltar && cc.isGrounded)
            velocidadVertical.y = Mathf.Sqrt(2f * alturaSalto * -gravedad);
        velocidadVertical.y += gravedad * Time.deltaTime;

        cc.Move((dir * v + velocidadVertical) * Time.deltaTime);
    }

    public void IntentarDisparar()
    {
        if (bloqueado || Time.time < ultimoDisparo + cooldown) return;
        ultimoDisparo = Time.time;

        Ray rayo = new Ray(camara.transform.position, camara.transform.forward);
        RaycastHit golpe;
        Vector3 boca = camara.transform.position + camara.transform.right * 0.25f - camara.transform.up * 0.2f;
        if (!Physics.Raycast(rayo, out golpe, alcanceDisparo, ~0, QueryTriggerInteraction.Ignore))
        {
            Trazo(boca, rayo.GetPoint(alcanceDisparo));
            return;
        }
        Trazo(boca, golpe.point);

        var fuga = golpe.collider.GetComponentInParent<Fuga>();
        if (fuga != null)
        {
            fuga.Sellarse();
            return;
        }
        DejarMarca(golpe.point);
    }

    void Trazo(Vector3 desde, Vector3 hasta)
    {
        var go = new GameObject("Trazo sellador");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, desde);
        lr.SetPosition(1, hasta);
        lr.startWidth = 0.06f;
        lr.endWidth = 0.02f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.9f, 0.4f, 0.9f);
        lr.endColor = new Color(0.4f, 0.8f, 1f, 0.6f);
        Destroy(go, 0.12f);
    }

    void DejarMarca(Vector3 punto)
    {
        var marca = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marca.name = "Marca";
        marca.transform.position = punto;
        marca.transform.localScale = Vector3.one * 0.18f;
        var r = marca.GetComponent<Renderer>();
        r.material.color = new Color(1f, 0.85f, 0.2f);
        Destroy(marca.GetComponent<Collider>());
        Destroy(marca, 1.2f);
    }

    public void Bloquear(bool valor) { bloqueado = valor; }

    public bool PuedeDisparar() { return Time.time >= ultimoDisparo + cooldown; }
}
