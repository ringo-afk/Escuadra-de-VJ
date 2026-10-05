using UnityEngine;
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

    [Header("Tiempos")]
    [Range(120f, 300f)]
    [SerializeField] private float tiempoSesion = 180f;
    [SerializeField] private float pausaEntreBloques = 0.4f;
    [SerializeField] private float esperaAntesDelResultado = 1.5f;

    [Header("Puntaje")]
    [SerializeField] private int puntosBienPuesto = 10;
    [SerializeField] private int puntosMalPuesto = 5;
    [SerializeField] private float factorBono = 0.5f;

    private Fase fase;
    private float tiempoEspera;
    private float tiempoRestante;
    private int puntos;
    private int preguntasContestadas;
    private int aciertos;
    private string motivoFinal;

    private void OnEnable()
    {
        trivia.AlResponder += Respondio;
        torre.AlAterrizar += Aterrizo;
        torre.AlColapsar += Colapso;
        torre.AlLlegarAlturaMaxima += LlegoAlturaMaxima;
        resultado.AlJugarDeNuevo += EmpezarPartida;
    }

    private void OnDisable()
    {
        trivia.AlResponder -= Respondio;
        torre.AlAterrizar -= Aterrizo;
        torre.AlColapsar -= Colapso;
        torre.AlLlegarAlturaMaxima -= LlegoAlturaMaxima;
        resultado.AlJugarDeNuevo -= EmpezarPartida;
    }

    private void Start()
    {
        EmpezarPartida();
    }

    private void EmpezarPartida()
    {
        CancelInvoke();
        resultado.Ocultar();
        trivia.Cancelar();
        torre.Reiniciar();
        hud.Reiniciar();

        tiempoRestante = tiempoSesion;
        puntos = 0;
        preguntasContestadas = 0;
        aciertos = 0;
        hud.MostrarPuntos(puntos);
        hud.MostrarTiempo(tiempoRestante);

        SiguientePregunta();
    }

    private void Update()
    {
        if (fase == Fase.Terminada) return;

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
        bool clic = mouse != null && mouse.leftButton.wasPressedThisFrame;
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
        if (res == Torre.Resultado.BienPuesto) puntos += puntosBienPuesto;
        else if (res == Torre.Resultado.MalPuesto) puntos += puntosMalPuesto;
        hud.MostrarPuntos(puntos);

        fase = Fase.Esperando;
        tiempoEspera = pausaEntreBloques;
    }

    private void Colapso()
    {
        TerminarPartida("La torre colapsó", esperaAntesDelResultado);
    }

    private void LlegoAlturaMaxima()
    {
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
        resultado.Mostrar(motivoFinal, torre.AlturaAlcanzada, puntos, bono, puntos + bono, porcentaje);
    }
}
