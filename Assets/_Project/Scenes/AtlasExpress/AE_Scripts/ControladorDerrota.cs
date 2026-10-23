using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ControladorDerrota : MonoBehaviour
{
    [Header("Textos de Estadísticas")]
    public TextMeshProUGUI textoAciertos;
    public TextMeshProUGUI textoTiempo;

    void Start()
    {
        
        int aciertos = PlayerPrefs.GetInt("AciertosFinales", 0);
        int total = PlayerPrefs.GetInt("TotalDocumentos", 7);
        string tiempo = PlayerPrefs.GetString("TiempoFinal", "00:00");

        
        if (textoAciertos != null)
        {
            
            textoAciertos.text = $"{aciertos} / {total}";
        }

        if (textoTiempo != null)
        {
            textoTiempo.text = tiempo;
        }
    }

    
    public void Reintentar()
    {
        
        SceneManager.LoadScene("AE_Jugar");
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("AE_Menu");
    }
}