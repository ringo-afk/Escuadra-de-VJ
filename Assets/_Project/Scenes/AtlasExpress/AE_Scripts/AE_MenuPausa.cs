using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPausa : MonoBehaviour
{
    [Header("Paneles de Interfaz")]
    public GameObject panelPausa;
    public GameObject panelInstrucciones;

    void Start()
    {
        
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
        Time.timeScale = 1f;
    }

    public void PausarJuego()
    {
        panelPausa.SetActive(true);
        Time.timeScale = 0f; 
    }

    public void ReanudarJuego()
    {
        panelPausa.SetActive(false);
        if (panelInstrucciones != null) panelInstrucciones.SetActive(false);
        Time.timeScale = 1f; 
    }

    
    public void MostrarInstrucciones()
    {
        
        panelPausa.SetActive(false);
        panelInstrucciones.SetActive(true);
    }

    public void OcultarInstrucciones() 
    {
        
        panelInstrucciones.SetActive(false);
        panelPausa.SetActive(true);
    }
    

    public void Reintentar()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SalirAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("AE_Menu"); 
    }
}