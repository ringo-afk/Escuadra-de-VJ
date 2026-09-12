using UnityEngine;
using UnityEngine.SceneManagement;

public class HB_SceneController : MonoBehaviour
{
    public void ToMinigamesMenu()
    {
        SceneManager.LoadScene("MenuMinijuegos");
    }
}
