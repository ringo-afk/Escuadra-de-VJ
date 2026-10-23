using UnityEngine;
using TMPro;

// tarjetas a la derecha que explican cómo jugar en la primera partida, sin detener el juego
public class TutorialJuego : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TriviaUI trivia;
    [SerializeField] private Torre torre;
    [SerializeField] private ResultadoFinalUI resultado;
    [SerializeField] private PausaJuego pausa;
    [SerializeField] private CanvasGroup tarjeta;
    [SerializeField] private TMP_Text textoTitulo;
    [SerializeField] private TMP_Text textoCuerpo;

    [Header("Tiempos")]
    [SerializeField] private float duracion = 7f;
    [SerializeField] private float velocidadFade = 4f;

    private const string KEY_VISTO = "BS_TutorialVisto";

    private static readonly string[] titulos =
    {
        "Responde para ganar tu bloque",
        "Suelta el bloque",
        "Puntos",
        "Cuida la estabilidad",
        "Pausa y opciones"
    };

    private static readonly string[] cuerpos =
    {
        "Antes de cada bloque sale una pregunta. Si aciertas, el bloque es ancho y lento. Si fallas, es angosto y rápido.",
        "Haz clic o presiona Espacio cuando el bloque esté encima de la torre.",
        "Bien centrado vale 10 puntos. Si queda chueco vale 5 y la torre se inclina.",
        "Mira la barra de estabilidad. Con 3 bloques mal puestos seguidos la torre se cae.",
        "Presiona Esc, P o el botón Pausa para cambiar el volumen o salir."
    };

    private bool activo;
    private bool terminado;
    private int paso = -1;
    private int aterrizajes;
    private int aterrizajesAlPaso;
    private float tiempoVisible;

    private void Awake()
    {
        activo = PlayerPrefs.GetInt(KEY_VISTO, 0) == 0;
        tarjeta.alpha = 0f;
        tarjeta.interactable = false;
        tarjeta.blocksRaycasts = false;
        tarjeta.gameObject.SetActive(activo);
    }

    private void OnEnable()
    {
        trivia.AlMostrarPregunta += MostroPregunta;
        trivia.AlResponder += Respondio;
        torre.AlAterrizar += Aterrizo;
        resultado.AlMostrar += Terminar;
    }

    private void OnDisable()
    {
        trivia.AlMostrarPregunta -= MostroPregunta;
        trivia.AlResponder -= Respondio;
        torre.AlAterrizar -= Aterrizo;
        resultado.AlMostrar -= Terminar;
    }

    private void Update()
    {
        if (!activo) return;

        bool visible = tiempoVisible > 0f && (pausa == null || !pausa.EnPausa);
        if (visible) tiempoVisible -= Time.deltaTime;

        // unscaled para que el fade también termine si se pausa a media tarjeta
        tarjeta.alpha = Mathf.MoveTowards(tarjeta.alpha, visible ? 1f : 0f, velocidadFade * Time.unscaledDeltaTime);
    }

    private void MostroPregunta()
    {
        if (paso < 0) Mostrar(0);
    }

    private void Respondio(bool acierto)
    {
        if (paso == 0) Mostrar(1);
    }

    private void Aterrizo(Torre.Resultado res, Torre.Calidad calidad, Bloque bloque)
    {
        aterrizajes++;
        int desdeElPaso = aterrizajes - aterrizajesAlPaso;

        if (paso == 1) Mostrar(2);
        else if (paso == 2 && (desdeElPaso >= 2 || res == Torre.Resultado.MalPuesto)) Mostrar(3);
        else if (paso == 3 && desdeElPaso >= 2) Mostrar(4);
    }

    private void Mostrar(int nuevoPaso)
    {
        if (!activo || terminado) return;

        paso = nuevoPaso;
        aterrizajesAlPaso = aterrizajes;
        textoTitulo.text = titulos[paso];
        textoCuerpo.text = cuerpos[paso];
        tiempoVisible = duracion;

        if (paso == titulos.Length - 1) MarcarVisto();
    }

    // aunque la partida acabe antes de ver todas, en la siguiente ya no salen
    private void Terminar()
    {
        terminado = true;
        tiempoVisible = 0f;
        MarcarVisto();
    }

    private void MarcarVisto()
    {
        PlayerPrefs.SetInt(KEY_VISTO, 1);
        PlayerPrefs.Save();
    }
}
