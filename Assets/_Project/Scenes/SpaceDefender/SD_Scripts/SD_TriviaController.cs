using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SD_TriviaController : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        [TextArea]
        public string text;

        public string[] answers = new string[3];

        [Range(0, 2)]
        public int correctIndex;

        [TextArea]
        public string explanation;
    }

    [Header("Jugador")]
    public SD_Player player;

    [Header("Interfaz")]
    public GameObject panel;
    public TMP_Text questionText;
    public Button[] answerButtons;
    public TMP_Text[] answerTexts;
    public TMP_Text feedbackText;
    public Button continueButton;

    [Header("Recompensa")]
    public float healingReward = 20f;

    [Header("Preguntas de ejemplo")]
    public Question[] questions =
    {
        new Question
        {
            text = "¿Dónde puedes consultar tu recibo de nómina?",
            answers = new[]
            {
                "En Nómina > Mis recibos",
                "En Vacaciones > Solicitudes",
                "En Soporte > Reportar problema"
            },
            correctIndex = 0,
            explanation =
                "En este kiosko de ejemplo, los recibos están " +
                "en Nómina > Mis recibos."
        },

        new Question
        {
            text = "¿Dónde solicitas tus días de vacaciones?",
            answers = new[]
            {
                "En Perfil > Datos personales",
                "En Vacaciones > Nueva solicitud",
                "En Nómina > Mis recibos"
            },
            correctIndex = 1,
            explanation =
                "En este kiosko de ejemplo, debes entrar " +
                "a Vacaciones > Nueva solicitud."
        },

        new Question
        {
            text = "¿Dónde revisas si aprobaron tus vacaciones?",
            answers = new[]
            {
                "En Soporte > Preguntas frecuentes",
                "En Nómina > Historial",
                "En Vacaciones > Mis solicitudes"
            },
            correctIndex = 2,
            explanation =
                "En este kiosko de ejemplo, el estado aparece " +
                "en Vacaciones > Mis solicitudes."
        }
    };

    public bool IsOpen { get; private set; }

    public bool HasPendingQuestions => pendingQuestions > 0;

    // Evita que el clic de Continuar también dispare el arma.
    public bool BlocksGameplayInput =>
        IsOpen || Time.frameCount == lastClosedFrame;

    private int pendingQuestions;
    private int lastClosedFrame = -1;
    private float previousTimeScale;
    private bool answered;
    private bool ready;
    private Question currentQuestion;

    void Awake()
    {
        ready = ValidateConfiguration();

        if (panel != null)
            panel.SetActive(false);
    }

    bool ValidateConfiguration()
    {
        if (player == null ||
            panel == null ||
            questionText == null ||
            feedbackText == null ||
            continueButton == null ||
            answerButtons == null ||
            answerTexts == null ||
            answerButtons.Length != 3 ||
            answerTexts.Length != 3 ||
            questions == null ||
            questions.Length == 0)
        {
            Debug.LogError(
                "Completa las referencias de TriviaController."
            );

            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (answerButtons[i] == null || answerTexts[i] == null)
            {
                Debug.LogError("Falta un botón o texto de respuesta.");
                return false;
            }
        }

        foreach (Question question in questions)
        {
            if (question == null ||
                question.answers == null ||
                question.answers.Length != 3 ||
                question.correctIndex < 0 ||
                question.correctIndex > 2)
            {
                Debug.LogError(
                    "Cada pregunta necesita tres respuestas " +
                    "y un índice correcto entre 0 y 2."
                );

                return false;
            }
        }

        return true;
    }

    public void RequestTrivia()
    {
        if (!ready || player == null || !player.alive)
            return;

        pendingQuestions++;

        // Si ya hay una pregunta abierta, guardar la nueva.
        if (IsOpen)
            return;

        previousTimeScale = Time.timeScale;
        IsOpen = true;
        Time.timeScale = 0f;

        panel.SetActive(true);
        ShowQuestion();
    }

    void ShowQuestion()
    {
        currentQuestion =
            questions[Random.Range(0, questions.Length)];

        answered = false;
        questionText.text = currentQuestion.text;
        feedbackText.text = "";

        for (int i = 0; i < 3; i++)
        {
            answerTexts[i].text = currentQuestion.answers[i];
            answerButtons[i].interactable = true;
        }

        continueButton.gameObject.SetActive(false);
    }

    public void Answer(int index)
    {
        if (!IsOpen || answered || index < 0 || index > 2)
            return;

        answered = true;

        foreach (Button button in answerButtons)
        {
            button.interactable = false;
        }

        if (index == currentQuestion.correctIndex)
        {
            float previousHealth = player.energia;
            player.Heal(healingReward);

            float recovered = player.energia - previousHealth;

            feedbackText.text =
                $"¡Correcto! Recuperaste {recovered:0.#} de vida.\n" +
                currentQuestion.explanation;
        }
        else
        {
            feedbackText.text =
                "Respuesta incorrecta.\n" +
                "La respuesta correcta es: " +
                currentQuestion.answers[currentQuestion.correctIndex] +
                "\n" + currentQuestion.explanation;
        }

        continueButton.gameObject.SetActive(true);
    }

    public void Continue()
    {
        if (!IsOpen || !answered)
            return;

        pendingQuestions--;

        if (pendingQuestions > 0)
        {
            ShowQuestion();
            return;
        }

        ClosePanel();
    }

    void Update()
    {
        // Si el jugador murió, cancelar cualquier trivia pendiente.
        if (IsOpen && (player == null || !player.alive))
        {
            pendingQuestions = 0;
            ClosePanel();
        }
    }

    void ClosePanel()
    {
        panel.SetActive(false);
        IsOpen = false;
        lastClosedFrame = Time.frameCount;

        Time.timeScale = previousTimeScale;
    }

    void OnDestroy()
    {
        // Restaurar el tiempo si se cambia de escena con el panel abierto.
        if (IsOpen)
            Time.timeScale = previousTimeScale;
    }
}