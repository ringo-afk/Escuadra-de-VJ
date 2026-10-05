using UnityEngine;
using UnityEngine.InputSystem;

// script temporal para probar el ciclo pregunta, bloque, resultado. Se reemplaza por JuegoManager en el paso 8
public class PruebaFisica : MonoBehaviour
{
    private enum Fase { Pregunta, Bloque, Cayendo, Esperando }

    [SerializeField] private Torre torre;
    [SerializeField] private TriviaUI trivia;
    [SerializeField] private float pausaEntreBloques = 0.4f;

    private Fase fase;
    private float tiempoEspera;

    private void OnEnable()
    {
        trivia.AlResponder += Respondio;
        torre.AlAterrizar += Aterrizo;
    }

    private void OnDisable()
    {
        trivia.AlResponder -= Respondio;
        torre.AlAterrizar -= Aterrizo;
    }

    private void Start()
    {
        SiguientePregunta();
    }

    private void Update()
    {
        Keyboard teclado = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (teclado != null && teclado.rKey.wasPressedThisFrame)
        {
            trivia.Cancelar();
            torre.Reiniciar();
            SiguientePregunta();
            return;
        }

        if (torre.Terminada) return;

        if (fase == Fase.Esperando)
        {
            tiempoEspera -= Time.deltaTime;
            if (tiempoEspera <= 0f) SiguientePregunta();
            return;
        }

        // con la pregunta activa no se puede soltar nada
        if (fase != Fase.Bloque || trivia.Activa) return;

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
        Debug.Log(acierto ? "Prueba: respuesta correcta" : "Prueba: respuesta incorrecta");
        torre.PrepararBloque(acierto);
        fase = Fase.Bloque;
    }

    private void Aterrizo(Torre.Resultado resultado)
    {
        // se espera un momento, y si en ese tiempo la torre colapsa ya no sale otra pregunta
        fase = Fase.Esperando;
        tiempoEspera = pausaEntreBloques;
    }
}
