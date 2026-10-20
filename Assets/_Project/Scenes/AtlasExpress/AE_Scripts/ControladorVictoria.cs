using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; 

public class ControladorVictoria : MonoBehaviour
{
    [Header("Textos de Métricas")]
    public TextMeshProUGUI textoAciertos;
    public TextMeshProUGUI textoTiempo;
    public TextMeshProUGUI textoMonedas;

    void Start()
    {
        
        int aciertos = PlayerPrefs.GetInt("AciertosFinales", 0);
        int total = PlayerPrefs.GetInt("TotalDocumentos", 7);
        string tiempo = PlayerPrefs.GetString("TiempoFinal", "00:00");
        int monedas = PlayerPrefs.GetInt("MonedasGanadas", 0);

        
        if (textoAciertos != null)
            textoAciertos.text = $"Archivos Correctos: {aciertos} / {total}";

        if (textoTiempo != null)
            textoTiempo.text = $"Tiempo Invertido: {tiempo}";

        if (textoMonedas != null)
            textoMonedas.text = $"Monedas Ganadas: +{monedas}";
    }

    
    public void VolverAlJuego()
    {
        
        SceneManager.LoadScene("AE_Menu");
    }
}