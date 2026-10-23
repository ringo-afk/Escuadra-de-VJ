
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public GameObject instructionsPanel;

    public void Jugar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level1");
    }

    public void AbrirInstrucciones()
    {
        instructionsPanel.SetActive(true);
    }

    public void CerrarInstrucciones()
    {
        instructionsPanel.SetActive(false);
    }

    public void Salir()
    {
        Application.Quit();
        Debug.Log("Salir del juego");
    }
}
