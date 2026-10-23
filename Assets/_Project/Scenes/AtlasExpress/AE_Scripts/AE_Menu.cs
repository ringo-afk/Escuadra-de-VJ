using UnityEngine;
using UnityEngine.SceneManagement;

public class AE_Menu : MonoBehaviour
{
    [Header("Paneles Emergentes")]
    
    public GameObject panelSonido;

    void Start()
    {
        
        Time.timeScale = 1f;
        if (panelSonido != null)
        {
            panelSonido.SetActive(false);
        }
    }

    
    public void Jugar()
    {
        SceneManager.LoadScene("AE_Jugar");
    }

    public void IrAInstrucciones()
    {
        SceneManager.LoadScene("AE_Instrucciones");
    }

    public void VolverAlMenu()
    {
        
        SceneManager.LoadScene("AE_Menu");
    }

    
    public void AbrirPanelSonido()
    {
        if (panelSonido != null)
        {
            panelSonido.SetActive(true);
        }
    }

    public void CerrarPanelSonido()
    {
        if (panelSonido != null)
        {
            panelSonido.SetActive(false);
        }
    }

    
    public void SalirJuego()
    {
        Debug.Log("Cerrando ATLAS Express...");
        Application.Quit();
    }
}