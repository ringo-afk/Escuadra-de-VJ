using UnityEngine;
using UnityEngine.SceneManagement;

public class AE_SceneManager : MonoBehaviour
{
    public void ToMinigamesMenu()
    {
        SceneManager.LoadScene("MenuMinijuegos");
    }
}
