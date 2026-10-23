using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;

    [Header("Sonido Game Over")]
    public AudioSource audioSource;
    public AudioClip sonidoGameOver;

    private bool gameOver = false;

    public void MostrarGameOver()
    {
        if (gameOver) return;

        gameOver = true;

        // Reproducir sonido una sola vez
        if (audioSource != null && sonidoGameOver != null)
        {
            audioSource.PlayOneShot(sonidoGameOver);
        }

        gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void Reintentar()
    {
        // Penalizacion de puntos por reiniciar el nivel (ver OrbitKiosko.ReinicioNivel)
        OrbitKiosko.Obtener().ReinicioNivel();
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void IrMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenuOP");
    }
}