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
        SceneManager.LoadScene("MainMenuAE");
    }
    
    public void ToBrewStack()
    {
        // Brew Stack
        SceneManager.LoadScene("MainMenuBS");
    }

    public void ToHeadBreaker()
    {
        // Head Breaker
        SceneManager.LoadScene("MainMenuHB");
    }
    
    public void ToSpaceDefender()
    {
        // Space Defender
        SceneManager.LoadScene("MainMenuSD");
    }
    
    public void ToOrbitPayment()
    {
        // Orbit Payment
        SceneManager.LoadScene("MainMenuOP");
    }




}
