using UnityEngine;
using UnityEngine.SceneManagement;

public class BS_SceneController : MonoBehaviour
{
    public void ToMinigamesMenu()
    {
        SceneManager.LoadScene("MenuMinijuegos");
    }
}
