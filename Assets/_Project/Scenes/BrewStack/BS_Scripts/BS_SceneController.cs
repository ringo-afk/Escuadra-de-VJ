using UnityEngine;
using UnityEngine.SceneManagement;

public class BS_SceneController : MonoBehaviour
{
    public void ToMinigamesMenu()
    {
        // en el portal no hay menú de minijuegos de Unity, se le pide a la página que cierre el juego
        if (BrewKiosko.EnPortal)
        {
            BrewKiosko.Obtener().Cerrar();
            return;
        }
        SceneManager.LoadScene("MenuMinijuegos");
    }
}
