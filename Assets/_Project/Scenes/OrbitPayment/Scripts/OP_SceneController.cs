using UnityEngine;
using UnityEngine.SceneManagement;

public class OP_SceneController : MonoBehaviour
{
    public void ToMinigamesMenu()
    {
        SceneManager.LoadScene("MenuMinijuegos");
    }
}
