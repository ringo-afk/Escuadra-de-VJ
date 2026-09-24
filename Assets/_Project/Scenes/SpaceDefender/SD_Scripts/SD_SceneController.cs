using UnityEngine;
using UnityEngine.SceneManagement;

public class SD_SceneController : MonoBehaviour
{
    public void ToMinigamesMenu()
    {
        SceneManager.LoadScene("MenuMinijuegos");
    }

}
