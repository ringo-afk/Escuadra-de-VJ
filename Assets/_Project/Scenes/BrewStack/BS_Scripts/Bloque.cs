using System.Collections;
using UnityEngine;

public class Bloque : MonoBehaviour
{
    public enum Estado { Esperando, Balanceando, Cayendo, Colocado, Suelto }

    [Header("Referencias")]
    [SerializeField] private SpriteRenderer render;
    [SerializeField] private Rigidbody2D rb;

    [Header("Apariencia")]
    [SerializeField] private Color colorAcierto = new Color(0.62f, 0.85f, 1f);
    [SerializeField] private Color colorError = new Color(0.88f, 0.59f, 0.23f);
    [SerializeField] private Sprite spriteAcierto;
    [SerializeField] private Sprite spriteError;
    [SerializeField] private float alto = 0.5f;

    [Header("Caída")]
    [SerializeField] private float gravedad = 25f;
    [SerializeField] private float velocidadMaximaCaida = 20f;

    [Header("Temblor")]
    [SerializeField] private float duracionTemblor = 0.6f;
    [SerializeField] private float anguloTemblor = 8f;
    [SerializeField] private float inclinacionMaxima = 6f;

    [Header("Rebote al aterrizar")]
    [SerializeField] private float cantidadSquash = 0.15f;
    [SerializeField] private float duracionSquash = 0.15f;

    [Header("Al salir volando")]
    [SerializeField] private float tiempoParaDestruir = 3f;

    private Estado estado = Estado.Esperando;
    private float ancho = 1f;
    // la escala real del bloque, el squash siempre regresa a esta
    private Vector3 escalaBase = Vector3.one;
    private Coroutine corrutinaTemblor;

    private float centroBalanceo;
    private float amplitudBalanceo;
    private float velocidadBalanceo;
    private float tiempoBalanceo;

    private float yDestino;
    private float velocidadCaida;
    private System.Action<Bloque> alAterrizar;

    public Estado EstadoActual => estado;
    public float Ancho => ancho;
    public float Alto => alto;

    public void Configurar(float nuevoAncho, bool acierto)
    {
        ancho = nuevoAncho;
        escalaBase = new Vector3(ancho, alto, 1f);
        transform.localScale = escalaBase;

        render.color = acierto ? colorAcierto : colorError;
        Sprite sprite = acierto ? spriteAcierto : spriteError;
        if (sprite != null)
        {
            render.sprite = sprite;
        }
    }

    public void EmpezarBalanceo(float centroX, float y, float amplitud, float velocidad)
    {
        centroBalanceo = centroX;
        amplitudBalanceo = amplitud;
        velocidadBalanceo = velocidad;
        tiempoBalanceo = 0f;

        transform.localPosition = new Vector3(centroX - amplitud, y, 0f);
        transform.localRotation = Quaternion.identity;
        estado = Estado.Balanceando;
    }

    public void Soltar(float yFinal, System.Action<Bloque> callback)
    {
        if (estado != Estado.Balanceando) return;

        yDestino = yFinal;
        velocidadCaida = 0f;
        alAterrizar = callback;
        estado = Estado.Cayendo;
    }

    private void Update()
    {
        if (estado == Estado.Balanceando)
        {
            tiempoBalanceo += Time.deltaTime;
            float x = centroBalanceo - amplitudBalanceo
                + Mathf.PingPong(tiempoBalanceo * velocidadBalanceo, amplitudBalanceo * 2f);
            transform.localPosition = new Vector3(x, transform.localPosition.y, 0f);
        }
        else if (estado == Estado.Cayendo)
        {
            velocidadCaida = Mathf.Min(velocidadCaida + gravedad * Time.deltaTime, velocidadMaximaCaida);
            Vector3 pos = transform.localPosition;
            pos.y -= velocidadCaida * Time.deltaTime;

            if (pos.y <= yDestino)
            {
                pos.y = yDestino;
                transform.localPosition = pos;
                estado = Estado.Colocado;
                StartCoroutine(AnimarSquash());
                alAterrizar?.Invoke(this);
                return;
            }
            transform.localPosition = pos;
        }
    }

    public void MoverX(float x)
    {
        Vector3 pos = transform.localPosition;
        pos.x = x;
        transform.localPosition = pos;
    }

    // intensidad de 0 a 1, el signo dice hacia qué lado se inclina
    public void Temblar(float intensidad)
    {
        // solo se para el temblor anterior, el squash sigue para que la escala regrese bien
        if (corrutinaTemblor != null) StopCoroutine(corrutinaTemblor);
        corrutinaTemblor = StartCoroutine(AnimarTemblor(Mathf.Clamp(intensidad, -1f, 1f)));
    }

    private IEnumerator AnimarSquash()
    {
        float t = 0f;
        while (t < duracionSquash)
        {
            t += Time.deltaTime;
            float fuerza = Mathf.Sin(t / duracionSquash * Mathf.PI) * cantidadSquash;
            transform.localScale = new Vector3(escalaBase.x * (1f + fuerza), escalaBase.y * (1f - fuerza), 1f);
            yield return null;
        }
        transform.localScale = escalaBase;
    }

    private IEnumerator AnimarTemblor(float intensidad)
    {
        float anguloFinal = -intensidad * inclinacionMaxima;
        float t = 0f;

        while (t < duracionTemblor)
        {
            t += Time.deltaTime;
            float apagado = 1f - t / duracionTemblor;
            float sacudida = Mathf.Sin(t * 40f) * anguloTemblor * Mathf.Abs(intensidad) * apagado;
            float angulo = Mathf.Lerp(0f, anguloFinal, t / duracionTemblor) + sacudida;
            transform.localRotation = Quaternion.Euler(0f, 0f, angulo);
            yield return null;
        }

        transform.localRotation = Quaternion.Euler(0f, 0f, anguloFinal);
    }

    public void ActivarFisica(Vector2 empuje, float giro)
    {
        StopAllCoroutines();
        transform.localScale = escalaBase;
        estado = Estado.Suelto;
        transform.SetParent(null, true);

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.AddForce(empuje, ForceMode2D.Impulse);
        rb.AddTorque(giro, ForceMode2D.Impulse);

        Destroy(gameObject, tiempoParaDestruir);
    }

    // para probar en Play sin la escena de juego
    [ContextMenu("Probar balanceo")]
    private void ProbarBalanceo()
    {
        Configurar(2f, true);
        EmpezarBalanceo(0f, transform.localPosition.y, 2f, 3f);
    }

    [ContextMenu("Probar soltar")]
    private void ProbarSoltar()
    {
        Soltar(transform.localPosition.y - 3f, b => b.Temblar(0.8f));
    }

    [ContextMenu("Probar salir volando")]
    private void ProbarSalirVolando()
    {
        ActivarFisica(new Vector2(2f, 1f), 1f);
    }
}
