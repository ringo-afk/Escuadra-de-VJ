using UnityEngine;
using UnityEngine.SceneManagement;

public class SD_PauseController : MonoBehaviour
{
    [Header("Referencias")]
    public SD_Player player;
    public SD_TriviaController triviaController;

    [Header("Paneles")]
    public GameObject pausePanel;
    public GameObject instructionsPanel;

    [Header("Escena para salir")]
    public string menuSceneName = "SD_MainMenu";

    public bool IsPaused { get; private set; }

    // Evita que el clic de Continuar también dispare el arma.
    public bool BlocksGameplayInput =>
        IsPaused || Time.frameCount == lastResumedFrame;

    private float previousTimeScale;
    private int lastResumedFrame = -1;

    void Start()
    {
        pausePanel.SetActive(false);
        instructionsPanel.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (IsPaused)
        {
            if (instructionsPanel.activeSelf)
            {
                BackToPause();
            }
            else
            {
                Resume();
            }

            return;
        }

        Pause();
    }

    public void Pause()
    {
        if (IsPaused || player == null || !player.alive)
            return;

        // No abrir pausa encima de una trivia.
        if (triviaController != null &&
            triviaController.HasPendingQuestions)
        {
            return;
        }

        // No intervenir si otro sistema ya pausó la partida.
        if (Time.timeScale == 0f)
            return;

        previousTimeScale = Time.timeScale;
        IsPaused = true;

        Time.timeScale = 0f;

        instructionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        pausePanel.SetActive(false);
        instructionsPanel.SetActive(false);

        IsPaused = false;
        lastResumedFrame = Time.frameCount;

        Time.timeScale = previousTimeScale;
    }

    public void ShowInstructions()
    {
        if (!IsPaused)
            return;

        pausePanel.SetActive(false);
        instructionsPanel.SetActive(true);
    }

    public void BackToPause()
    {
        if (!IsPaused)
            return;

        instructionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void Retry()
    {
        PrepareSceneChange();

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void ExitToMenu()
    {
        PrepareSceneChange();

        SceneManager.LoadScene(menuSceneName);
    }

    void PrepareSceneChange()
    {
        IsPaused = false;

        // La nueva escena debe comenzar con el tiempo funcionando.
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (IsPaused)
        {
            Time.timeScale = previousTimeScale;
        }
    }
}