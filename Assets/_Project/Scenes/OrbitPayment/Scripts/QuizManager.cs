using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class PasoQuiz
{
    public string texto;
    public int orden;
}

[Serializable]
public class PreguntaQuiz
{
    public int id;
    public string pregunta;
    public PasoQuiz[] pasos;
}

[Serializable]
public class BancoPreguntasOrbit
{
    public PreguntaQuiz[] preguntas;
}

public class QuizManager : MonoBehaviour
{
    public GameObject quizPanel;
    public GameObject feedbackBackground;

    private Coroutine rutinaFeedback;

    public TMP_Text questionText;
    public TMP_Text feedbackText;
    public Transform stepsContainer;
    public Button verifyButton;
    public TextAsset archivoPreguntas;

    private BancoPreguntasOrbit banco;
    private bool abierto = false;
    private QuizColumnTrigger columnaActual;
    public PortalUnlock portalUnlock;

    [Header("Sonidos del Quiz")]
    public AudioSource audioSource;
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoIncorrecto;

    private PreguntaQuiz preguntaActual;
    private bool preguntaDelServidor;

    void Awake()
    {
        quizPanel.SetActive(false);

        if (archivoPreguntas == null)
        {
            Debug.LogWarning("No hay preguntas.json de respaldo en QuizManager.");
            return;
        }

        banco = JsonUtility.FromJson<BancoPreguntasOrbit>(archivoPreguntas.text);

        if (banco == null || banco.preguntas == null)
            Debug.LogWarning("No se pudieron cargar las preguntas de respaldo. Revisa el JSON.");
    }

    void Start()
    {
        OrbitKiosko.Obtener().PrecargarPreguntas();
    }

    public void AbrirQuiz(int id, QuizColumnTrigger columna)
    {
        if (abierto) return;

        PreguntaQuiz pregunta = null;
        bool delServidor = false;

        if (OrbitKiosko.Obtener().SiguientePregunta(out PreguntaQuiz recibida)
            && recibida.pasos != null && recibida.pasos.Length == 4)
        {
            pregunta = recibida;
            delServidor = true;
        }

        if (pregunta == null && banco != null && banco.preguntas != null)
            pregunta = Array.Find(banco.preguntas, p => p.id == id);

        if (pregunta == null || pregunta.pasos == null ||
            pregunta.pasos.Length != 4)
        {
            Debug.LogError("No hay una pregunta válida para mostrar.");
            return;
        }

        abierto = true;
        columnaActual = columna;
        preguntaActual = pregunta;
        preguntaDelServidor = delServidor;

        questionText.text = pregunta.pregunta;
        feedbackText.text = "";
        feedbackBackground.SetActive(false);

        List<PasoQuiz> pasos = new List<PasoQuiz>(pregunta.pasos);
        int intentos = 0;

        do
        {
            for (int i = pasos.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);

                PasoQuiz temp = pasos[i];
                pasos[i] = pasos[j];
                pasos[j] = temp;
            }

            intentos++;
        }
        while (EstaOrdenado(pasos) && intentos < 10);

        for (int i = 0; i < 4; i++)
        {
            Transform tarjeta = stepsContainer.GetChild(i);

            tarjeta.GetComponent<QuizStepDrag>()
                .ordenCorrecto = pasos[i].orden;

            tarjeta.GetComponentInChildren<TMP_Text>()
                .text = pasos[i].texto;
        }

        quizPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private bool EstaOrdenado(List<PasoQuiz> pasos)
    {
        for (int i = 0; i < pasos.Count; i++)
        {
            if (pasos[i].orden != i + 1)
                return false;
        }

        return true;
    }

    public void VerificarRespuesta()
    {
        bool correcto = true;

        for (int i = 0; i < stepsContainer.childCount; i++)
        {
            QuizStepDrag paso = stepsContainer
                .GetChild(i)
                .GetComponent<QuizStepDrag>();

            if (paso.ordenCorrecto != i + 1)
            {
                correcto = false;
                break;
            }
        }

        if (preguntaActual != null)
        {
            OrbitKiosko.Obtener().PreguntaRespondida(
                preguntaActual.id, correcto, preguntaDelServidor
            );
        }

        if (correcto)
        {
            ReproducirCorrecto();

            if (rutinaFeedback != null)
            {
                StopCoroutine(rutinaFeedback);
                rutinaFeedback = null;
            }

            feedbackBackground.SetActive(true);

            feedbackText.text = "¡MISIÓN COMPLETADA!";
            feedbackText.color = new Color(0.4f, 1f, 0.85f);

            verifyButton.interactable = false;

            columnaActual.Completar();

            if (portalUnlock != null)
            {
                portalUnlock.PreguntaCompletada();
            }

            StartCoroutine(CerrarConEspera());
        }
        else
        {
            ReproducirIncorrecto();

            if (rutinaFeedback != null)
            {
                StopCoroutine(rutinaFeedback);
            }

            rutinaFeedback = StartCoroutine(
                MostrarFeedbackTemporal(
                    "¡ORDEN INCORRECTO!",
                    new Color(1f, 0.55f, 0.65f)
                )
            );
        }
    }

    IEnumerator CerrarConEspera()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        CerrarQuiz();
    }

    IEnumerator MostrarFeedbackTemporal(string mensaje, Color color)
    {
        feedbackBackground.SetActive(true);

        feedbackText.text = mensaje;
        feedbackText.color = color;

        yield return new WaitForSecondsRealtime(1.5f);

        feedbackBackground.SetActive(false);
        rutinaFeedback = null;
    }

    void CerrarQuiz()
    {
        quizPanel.SetActive(false);
        Time.timeScale = 1f;
        verifyButton.interactable = true;
        abierto = false;
    }

    void ReproducirCorrecto()
    {
        if (audioSource != null && sonidoCorrecto != null)
        {
            audioSource.PlayOneShot(sonidoCorrecto);
        }
    }

    void ReproducirIncorrecto()
    {
        if (audioSource != null && sonidoIncorrecto != null)
        {
            audioSource.PlayOneShot(sonidoIncorrecto);
        }
    }
}
