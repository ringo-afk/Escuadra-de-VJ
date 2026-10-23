using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// flujo de la partida: pregunta, bloque, resultado, hasta que la torre cae o se acaba el tiempo
public class JuegoManager : MonoBehaviour
{
    private enum Fase { Pregunta, Bloque, Cayendo, Esperando, Terminada }

    [Header("Referencias")]
    [SerializeField] private Torre torre;
    [SerializeField] private TriviaUI trivia;
    [SerializeField] private HUDJuego hud;
    [SerializeField] private ResultadoFinalUI resultado;
    [SerializeField] private TextosFlotantes textosFlotantes;
    [SerializeField] private PausaJuego pausa;

    [Header("Tiempos")]
    [Range(120f, 300f)]
    [SerializeField] private float tiempoSesion = 180f;
    [SerializeField] private float pausaEntreBloques = 0.4f;
    [SerializeField] private float esperaAntesDelResultado = 1.5f;

    [Header("Puntaje")]
    [SerializeField] private int puntosBienPuesto = 10;
    [SerializeField] private int puntosMalPuesto = 5;
    [SerializeField] private float factorBono = 0.5f;

    // el nivel es la dificultad de las preguntas, solo sube durante la partida
    public event System.Action<int> AlCambiarNivel;

    private Fase fase;
    private float tiempoEspera;
    private float tiempoRestante;
    private int puntos;
    private int preguntasContestadas;
    private int aciertos;
    private string motivoFinal;
    private int nivel;
    private bool preparado;
    private bool completada;

    public int Nivel => nivel;
    public float TiempoRestante => tiempoRestante;
    public bool Terminada => fase == Fase.Terminada;

    private void OnEnable()
    {
        trivia.AlResponder += Respondio;
        torre.AlAterrizar += Aterrizo;
        torre.AlColapsar += Colapso;
        torre.AlLlegarAlturaMaxima += LlegoAlturaMaxima;
        resultado.AlJugarDeNuevo += Reiniciar;
    }

    private void OnDisable()
    {
        trivia.AlResponder -= Respondio;
        torre.AlAterrizar -= Aterrizo;
        torre.AlColapsar -= Colapso;
        torre.AlLlegarAlturaMaxima -= LlegoAlturaMaxima;
        resultado.AlJugarDeNuevo -= Reiniciar;
    }

    private void Start()
    {
        StartCoroutine(Preparar());
    }

    private void Reiniciar()
    {
        StartCoroutine(Preparar());
    }

    private IEnumerator Preparar()
    {
        preparado = false;
        BrewKiosko kiosko = BrewKiosko.Obtener();
        kiosko.IniciarPartida();

        float espera = 0f;
        while (!kiosko.Listo && espera < 8f)
        {
            espera += Time.unscaledDeltaTime;
            yield return null;
        }

        if (kiosko.Bloqueado) yield break;

        EmpezarPartida();
    }

    private void EmpezarPartida()
    {
        CancelInvoke();
        preparado = true;
        completada = false;
        resultado.Ocultar();
        trivia.Cancelar();
        torre.Reiniciar();
        hud.Reiniciar();

        tiempoRestante = tiempoSesion;
        puntos = 0;
        preguntasContestadas = 0;
        aciertos = 0;
        nivel = trivia.DificultadParaAltura(torre.Altura);
        hud.MostrarPuntos(puntos);
        hud.MostrarTiempo(tiempoRestante);
        hud.MostrarNivel(nivel, false);

        SiguientePregunta();
    }

    private void Update()
    {
        if (!preparado || fase == Fase.Terminada) return;
        if (pausa != null && pausa.EnPausa) return;

        tiempoRestante -= Time.deltaTime;
        hud.MostrarTiempo(tiempoRestante);
        if (tiempoRestante <= 0f)
        {
            torre.Detener();
            TerminarPartida("Se acabó el tiempo", 0f);
            return;
        }

        if (fase == Fase.Esperando)
        {
            tiempoEspera -= Time.deltaTime;
            if (tiempoEspera <= 0f) SiguientePregunta();
            return;
        }

        // con la pregunta activa no se puede soltar nada
        if (fase != Fase.Bloque || trivia.Activa) return;

        Keyboard teclado = Keyboard.current;
        Mouse mouse = Mouse.current;
        // el clic sobre un botón, como el de pausa, no suelta el bloque
        bool sobreUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool clic = mouse != null && mouse.leftButton.wasPressedThisFrame && !sobreUI;
        bool espacio = teclado != null && teclado.spaceKey.wasPressedThisFrame;
        if (clic || espacio)
        {
            torre.SoltarBloque();
            fase = Fase.Cayendo;
        }
    }

    private void SiguientePregunta()
    {
        fase = Fase.Pregunta;
        trivia.MostrarPregunta(torre.Altura);
    }

    private void Respondio(bool acierto)
    {
        if (fase == Fase.Terminada) return;

        preguntasContestadas++;
        if (acierto) aciertos++;

        torre.PrepararBloque(acierto);
        fase = Fase.Bloque;
    }

    private void Aterrizo(Torre.Resultado res, Torre.Calidad calidad, Bloque bloque)
    {
        int ganados = 0;
        if (res == Torre.Resultado.BienPuesto) ganados = puntosBienPuesto;
        else if (res == Torre.Resultado.MalPuesto) ganados = puntosMalPuesto;

        puntos += ganados;
        hud.MostrarPuntos(puntos);
        BrewKiosko.Obtener().Puntaje(puntos);
        if (ganados > 0 && textosFlotantes != null)
        {
            textosFlotantes.Mostrar(ganados, bloque.transform.position, res == Torre.Resultado.BienPuesto);
        }

        int nuevoNivel = trivia.DificultadParaAltura(torre.Altura);
        if (nuevoNivel != nivel)
        {
            nivel = nuevoNivel;
            hud.MostrarNivel(nivel, true);
            AlCambiarNivel?.Invoke(nivel);
        }

        fase = Fase.Esperando;
        tiempoEspera = pausaEntreBloques;
    }

    private void Colapso()
    {
        TerminarPartida("La torre colapsó", esperaAntesDelResultado);
    }

    private void LlegoAlturaMaxima()
    {
        completada = true;
        TerminarPartida("Llegaste a la altura máxima", esperaAntesDelResultado);
    }

    private void TerminarPartida(string motivo, float espera)
    {
        if (fase == Fase.Terminada) return;
        fase = Fase.Terminada;
        trivia.Cancelar();

        motivoFinal = motivo;
        Invoke(nameof(MostrarResultado), espera);
    }

    private void MostrarResultado()
    {
        float porcentaje = preguntasContestadas > 0 ? (float)aciertos / preguntasContestadas : 0f;
        int bono = Mathf.RoundToInt(puntos * porcentaje * factorBono);
        BrewKiosko.Obtener().Finalizar(puntos + bono, completada);
        resultado.Mostrar(motivoFinal, torre.AlturaAlcanzada, puntos, bono, puntos + bono, porcentaje);
    }
}
