using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SD_Aftermath : MonoBehaviour
{
    [Header("Textos")]
    public TMP_Text finText;
    public TMP_Text estadisticasText;

    void Start()
    {
        Time.timeScale = 1f;

        if (finText == null || estadisticasText == null)
        {
            Debug.LogError(
                "Asigna los textos de la pantalla de resultados."
            );

            return;
        }

        // Permite abrir esta escena directamente para editarla.
        if (!SD_RunResults.HasResults)
        {
            finText.text = "Fin del juego";
            estadisticasText.text =
                "Todavía no hay resultados de una partida.";

            return;
        }
        
        SD_AudioManager.Instance?.Play(
            SD_RunResults.Victory
                ? SD_AudioManager.Sound.Victory
                : SD_AudioManager.Sound.Defeat
        );

        finText.text = SD_RunResults.Victory
            ? "¡Victoria!"
            : "Derrota";

        estadisticasText.text =
            $"Puntos ganados: {SD_RunResults.Points}\n" +
            (SD_RunResults.HasCoins
                ? $"Monedas ganadas: +{SD_RunResults.Coins}\n"
                : "") +
            $"Oleadas superadas: " +
            $"{SD_RunResults.CompletedWaves}/{SD_RunResults.TotalWaves}\n" +
            $"Vida restante: {SD_RunResults.RemainingHealth:0}";
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SD_Gameplay");
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SD_MainMenu");
    }
}