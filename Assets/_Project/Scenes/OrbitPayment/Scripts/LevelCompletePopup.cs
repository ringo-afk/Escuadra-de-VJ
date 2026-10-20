
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelCompletePopup : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text coinsResultText;
    public TMP_Text documentsResultText;

    [Header("Configuración de escenas")]
    public string siguienteNivel = "Level2";
    public string escenaMenu = "MainMenuOP";

    [Header("Sonido de victoria")]
    public AudioSource audioSource;
    public AudioClip sonidoVictoria;

    private bool completado = false;

    public void MostrarVictoria()
    {
        if (completado)
            return;

        completado = true;

        int monedas = CoinManager.instance.TotalMonedas;
        int documentos = DocumentManager.instance.TotalDocumentos;

        OrbitKiosko.Obtener().NivelCompletado(monedas, documentos);

        coinsResultText.text =
            "Monedas Recolectadas: " + monedas;

        documentsResultText.text =
            "Documentos Recolectados: " + documentos;

        if (audioSource != null && sonidoVictoria != null)
        {
            audioSource.PlayOneShot(sonidoVictoria);
        }

        panel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void IrNivel2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(siguienteNivel);
    }

    public void IrMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }
}
