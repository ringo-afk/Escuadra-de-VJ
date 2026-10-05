using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TriviaUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private BancoPreguntas banco;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text textoPregunta;
    [SerializeField] private Button[] botones;
    [SerializeField] private TMP_Text[] textosBotones;
    [SerializeField] private RectTransform barraTiempo;
    [SerializeField] private Image imagenBarra;

    [Header("Tiempo")]
    [SerializeField] private float tiempoPregunta = 8f;
    [SerializeField] private float pausaDespuesDeResponder = 0.8f;

    [Header("Dificultad según altura")]
    [SerializeField] private int alturaDificultad2 = 10;
    [SerializeField] private int alturaDificultad3 = 25;

    [Header("Colores")]
    [SerializeField] private Color colorBoton = new Color(0.62f, 0.85f, 1f, 0.35f);
    [SerializeField] private Color colorTextoBoton = new Color(0.81f, 0.9f, 1f);
    [SerializeField] private Color colorCorrecta = new Color(0.62f, 0.85f, 1f);
    [SerializeField] private Color colorIncorrecta = new Color(0.88f, 0.59f, 0.23f);
    [SerializeField] private Color colorTextoMarcado = new Color(0.02f, 0.04f, 0.09f);
    [SerializeField] private Color colorBarra = new Color(0.62f, 0.85f, 1f);
    [SerializeField] private Color colorBarraPocoTiempo = new Color(0.88f, 0.59f, 0.23f);
    [Range(0f, 1f)]
    [SerializeField] private float avisoPocoTiempo = 0.3f;

    public event System.Action<bool> AlResponder;

    private Pregunta preguntaActual;
    private int[] orden;
    private float tiempoRestante;
    private bool esperandoRespuesta;
    private bool activa;

    public bool Activa => activa;

    private void Awake()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            int indice = i;
            botones[i].onClick.AddListener(() => Responder(indice));
        }
        panel.SetActive(false);
    }

    public int DificultadParaAltura(int altura)
    {
        if (altura >= alturaDificultad3) return 3;
        if (altura >= alturaDificultad2) return 2;
        return 1;
    }

    public void MostrarPregunta(int altura)
    {
        preguntaActual = banco.SiguientePregunta(DificultadParaAltura(altura));
        if (preguntaActual == null)
        {
            // sin preguntas no se puede jugar bien, pero que no se trabe
            Debug.LogWarning("TriviaUI: el banco no tiene preguntas, se cuenta como acierto");
            AlResponder?.Invoke(true);
            return;
        }

        textoPregunta.text = preguntaActual.texto;

        int cantidad = preguntaActual.opciones.Length;
        orden = new int[cantidad];
        for (int i = 0; i < cantidad; i++) orden[i] = i;
        Barajar(orden);

        for (int i = 0; i < botones.Length; i++)
        {
            bool usado = i < cantidad;
            botones[i].gameObject.SetActive(usado);
            if (!usado) continue;

            textosBotones[i].text = preguntaActual.opciones[orden[i]];
            PintarBoton(i, colorBoton, colorTextoBoton);
            botones[i].interactable = true;
        }

        tiempoRestante = tiempoPregunta;
        esperandoRespuesta = true;
        activa = true;
        panel.SetActive(true);
        ActualizarBarra();
    }

    private void Update()
    {
        if (!esperandoRespuesta) return;

        tiempoRestante -= Time.deltaTime;
        ActualizarBarra();

        if (tiempoRestante <= 0f)
        {
            Responder(-1);
        }
    }

    private void ActualizarBarra()
    {
        float porcentaje = Mathf.Clamp01(tiempoRestante / tiempoPregunta);
        barraTiempo.anchorMax = new Vector2(porcentaje, 1f);
        imagenBarra.color = porcentaje <= avisoPocoTiempo ? colorBarraPocoTiempo : colorBarra;
    }

    // boton -1 es que se acabó el tiempo
    private void Responder(int boton)
    {
        if (!esperandoRespuesta) return;
        esperandoRespuesta = false;

        bool acierto = boton >= 0 && orden[boton] == preguntaActual.correcta;

        for (int i = 0; i < orden.Length; i++)
        {
            botones[i].interactable = false;
            if (orden[i] == preguntaActual.correcta)
            {
                PintarBoton(i, colorCorrecta, colorTextoMarcado);
            }
            else if (i == boton)
            {
                PintarBoton(i, colorIncorrecta, colorTextoMarcado);
            }
        }

        StartCoroutine(TerminarPregunta(acierto));
    }

    private IEnumerator TerminarPregunta(bool acierto)
    {
        yield return new WaitForSeconds(pausaDespuesDeResponder);
        panel.SetActive(false);
        activa = false;
        AlResponder?.Invoke(acierto);
    }

    public void Cancelar()
    {
        StopAllCoroutines();
        esperandoRespuesta = false;
        activa = false;
        panel.SetActive(false);
    }

    private void PintarBoton(int i, Color fondo, Color texto)
    {
        // el color se pone en la imagen y en el disabledColor para que no se vea gris al desactivarlo
        botones[i].image.color = fondo;
        ColorBlock colores = botones[i].colors;
        colores.disabledColor = Color.white;
        botones[i].colors = colores;
        textosBotones[i].color = texto;
    }

    private void Barajar(int[] arreglo)
    {
        for (int i = arreglo.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = arreglo[i];
            arreglo[i] = arreglo[j];
            arreglo[j] = temp;
        }
    }
}
