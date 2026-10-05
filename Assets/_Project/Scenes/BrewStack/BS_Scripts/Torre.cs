using System.Collections.Generic;
using UnityEngine;

public class Torre : MonoBehaviour
{
    public enum Resultado { BienPuesto, MalPuesto, SeCayo }

    [Header("Referencias")]
    [SerializeField] private Bloque prefabBloque;

    [Header("Bloque según la respuesta")]
    [SerializeField] private float anchoAcierto = 2f;
    [SerializeField] private float anchoError = 1.4f;
    [SerializeField] private float velocidadAcierto = 2.5f;
    [SerializeField] private float velocidadError = 6f;

    [Header("Balanceo")]
    [SerializeField] private float amplitudBalanceo = 2.5f;
    [SerializeField] private float alturaBalanceo = 3f;

    [Header("Base")]
    [SerializeField] private float anchoBase = 3f;

    [Header("Colocación (en anchos del bloque de abajo)")]
    [SerializeField] private float toleranciaBienPuesto = 0.15f;
    [SerializeField] private float desfaseSinApoyo = 0.5f;

    [Header("Estabilidad")]
    [SerializeField] private float umbralDesfase = 1f;
    [SerializeField] private int maxMalPuestosSeguidos = 3;
    [SerializeField] private float recuperacionPorBienPuesto = 0f;
    [SerializeField] private float inclinacionMaximaTorre = 5f;
    [SerializeField] private float velocidadInclinacion = 3f;

    [Header("Partida")]
    [SerializeField] private int alturaMaxima = 75;

    [Header("Colapso")]
    [SerializeField] private float fuerzaColapso = 3f;

    [Header("Debug")]
    [SerializeField] private bool mostrarLogs = true;

    public event System.Action<Resultado> AlAterrizar;
    public event System.Action AlColapsar;
    public event System.Action AlLlegarAlturaMaxima;

    private readonly List<Bloque> bloques = new List<Bloque>();
    private Transform pivote;
    private Bloque bloqueActual;
    private float xArriba;
    private float anchoArriba;
    private float desfaseAcumulado;
    private int malPuestosSeguidos;
    private bool terminada;
    private int alturaAlcanzada;

    public int Altura => bloques.Count;
    public float Estabilidad => Mathf.Clamp01(1f - Mathf.Abs(desfaseAcumulado) / umbralDesfase);
    public bool Terminada => terminada;
    // la altura que tenía antes de colapsar, para el resultado final
    public int AlturaAlcanzada => alturaAlcanzada;
    public float YBloqueBalanceo => transform.position.y + bloques.Count * prefabBloque.Alto + alturaBalanceo;
    public bool HayBloqueBalanceando => bloqueActual != null && bloqueActual.EstadoActual == Bloque.Estado.Balanceando;

    private void Awake()
    {
        // los bloques van dentro del pivote para que se inclinen junto con la torre
        pivote = new GameObject("Pivote").transform;
        pivote.SetParent(transform, false);
        Reiniciar();
    }

    private void Update()
    {
        float anguloObjetivo = -(desfaseAcumulado / umbralDesfase) * inclinacionMaximaTorre;
        anguloObjetivo = Mathf.Clamp(anguloObjetivo, -inclinacionMaximaTorre, inclinacionMaximaTorre);
        Quaternion objetivo = Quaternion.Euler(0f, 0f, anguloObjetivo);
        pivote.localRotation = Quaternion.Lerp(pivote.localRotation, objetivo, velocidadInclinacion * Time.deltaTime);
    }

    public void Reiniciar()
    {
        foreach (Bloque b in bloques)
        {
            if (b != null) Destroy(b.gameObject);
        }
        bloques.Clear();

        if (bloqueActual != null) Destroy(bloqueActual.gameObject);
        bloqueActual = null;

        pivote.localRotation = Quaternion.identity;
        xArriba = 0f;
        anchoArriba = anchoBase;
        desfaseAcumulado = 0f;
        malPuestosSeguidos = 0;
        terminada = false;
        alturaAlcanzada = 0;
    }

    public void PrepararBloque(bool acierto)
    {
        if (terminada || bloqueActual != null) return;

        bloqueActual = Instantiate(prefabBloque, pivote);
        bloqueActual.gameObject.SetActive(true);
        bloqueActual.Configurar(acierto ? anchoAcierto : anchoError, acierto);

        float y = bloques.Count * bloqueActual.Alto + alturaBalanceo;
        float velocidad = acierto ? velocidadAcierto : velocidadError;
        bloqueActual.EmpezarBalanceo(xArriba, y, amplitudBalanceo, velocidad);
    }

    public void SoltarBloque()
    {
        if (!HayBloqueBalanceando) return;

        float yFinal = bloques.Count * bloqueActual.Alto + bloqueActual.Alto / 2f;
        bloqueActual.Soltar(yFinal, Aterrizo);
    }

    private void Aterrizo(Bloque b)
    {
        bloqueActual = null;

        float desfase = (b.transform.localPosition.x - xArriba) / anchoArriba;
        float distancia = Mathf.Abs(desfase);
        float lado = Mathf.Sign(desfase);
        Resultado resultado;

        if (distancia >= desfaseSinApoyo)
        {
            resultado = Resultado.SeCayo;
            malPuestosSeguidos++;
            b.ActivarFisica(new Vector2(lado * fuerzaColapso * 0.5f, 0f), -lado * 0.5f);
        }
        else if (distancia <= toleranciaBienPuesto)
        {
            resultado = Resultado.BienPuesto;
            malPuestosSeguidos = 0;
            desfaseAcumulado = Mathf.MoveTowards(desfaseAcumulado, 0f, recuperacionPorBienPuesto);
            Agregar(b);
        }
        else
        {
            resultado = Resultado.MalPuesto;
            malPuestosSeguidos++;
            desfaseAcumulado += desfase;

            // entre más cerca del borde, más tiembla
            float intensidad = (distancia - toleranciaBienPuesto) / (desfaseSinApoyo - toleranciaBienPuesto);
            b.Temblar(intensidad * lado);
            Agregar(b);
        }

        if (mostrarLogs)
        {
            Debug.Log($"Torre: {resultado}, desfase {desfase:F2}, acumulado {desfaseAcumulado:F2}, " +
                      $"estabilidad {Estabilidad:F2}, mal puestos seguidos {malPuestosSeguidos}, altura {Altura}");
        }

        AlAterrizar?.Invoke(resultado);

        if (Mathf.Abs(desfaseAcumulado) >= umbralDesfase || malPuestosSeguidos >= maxMalPuestosSeguidos)
        {
            Colapsar();
        }
        else if (bloques.Count >= alturaMaxima)
        {
            terminada = true;
            AlLlegarAlturaMaxima?.Invoke();
        }
    }

    private void Agregar(Bloque b)
    {
        bloques.Add(b);
        xArriba = b.transform.localPosition.x;
        anchoArriba = b.Ancho;
        alturaAlcanzada = bloques.Count;
    }

    // para cuando se acaba el tiempo: la torre se queda parada y ya no salen bloques
    public void Detener()
    {
        terminada = true;
        if (bloqueActual != null)
        {
            Destroy(bloqueActual.gameObject);
            bloqueActual = null;
        }
    }

    public void Colapsar()
    {
        if (terminada) return;
        terminada = true;

        float lado = desfaseAcumulado != 0f ? Mathf.Sign(desfaseAcumulado) : (Random.value < 0.5f ? -1f : 1f);

        if (bloqueActual != null)
        {
            Destroy(bloqueActual.gameObject);
            bloqueActual = null;
        }

        // los de arriba salen con más fuerza
        for (int i = 0; i < bloques.Count; i++)
        {
            float altura01 = (i + 1f) / bloques.Count;
            Vector2 empuje = new Vector2(lado * fuerzaColapso * altura01 * Random.Range(0.7f, 1.2f), Random.Range(0f, 1f));
            bloques[i].ActivarFisica(empuje, -lado * Random.Range(0.2f, 0.6f));
        }
        bloques.Clear();

        if (mostrarLogs) Debug.Log("Torre: colapsó");
        AlColapsar?.Invoke();
    }

    // pruebas en Play sin la escena completa, el bloque se suelta con el desfase indicado
    private void ProbarDesfase(float desfase, bool acierto)
    {
        if (terminada) Reiniciar();
        if (bloqueActual != null) Destroy(bloqueActual.gameObject);
        bloqueActual = null;

        PrepararBloque(acierto);
        bloqueActual.MoverX(xArriba + desfase * anchoArriba);
        SoltarBloque();
    }

    [ContextMenu("Probar bien puesto (0)")]
    private void ProbarBienPuesto() => ProbarDesfase(0f, true);

    [ContextMenu("Probar mal puesto a la derecha (0.3)")]
    private void ProbarMalPuestoDerecha() => ProbarDesfase(0.3f, true);

    [ContextMenu("Probar mal puesto a la izquierda (-0.3)")]
    private void ProbarMalPuestoIzquierda() => ProbarDesfase(-0.3f, false);

    [ContextMenu("Probar sin apoyo (0.6)")]
    private void ProbarSinApoyo() => ProbarDesfase(0.6f, true);

    [ContextMenu("Probar colapso")]
    private void ProbarColapso() => Colapsar();

    [ContextMenu("Reiniciar torre")]
    private void ProbarReiniciar() => Reiniciar();
}
