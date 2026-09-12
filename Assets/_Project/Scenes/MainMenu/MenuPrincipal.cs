using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    public void ToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void ToMinigamesMenu()
    {
        SceneManager.LoadScene("MenuMinijuegos");
    }

    public void ToAtlasExpress()
    {
        // Atlas Express
        SceneManager.LoadScene("MenuEXPRESS");
    }
    
    public void ToBrewStack()
    {
        // Brew Stack
        SceneManager.LoadScene("BS_Menu");
    }

    public void ToHeadBreaker()
    {
        // Head Breaker
        SceneManager.LoadScene("MainMenuHB");
    }
    
    public void ToSpaceDefender()
    {
        // Space Defender
        SceneManager.LoadScene("SD_MainMenu");
    }
    
    public void ToOrbitPayment()
    {
        // Orbit Payment
        SceneManager.LoadScene("MainMenuOP");
    }




}
