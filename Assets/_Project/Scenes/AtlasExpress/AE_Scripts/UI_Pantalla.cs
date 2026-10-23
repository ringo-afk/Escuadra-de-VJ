using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UI_Pantallas : MonoBehaviour
{
    [Header("Textos de las Pantallas")]
    public TextMeshProUGUI textoPuntuacion;
    public TextMeshProUGUI textoTiempo;

    [Header("Pantalla de Victoria")]
    public GameObject panelVictoria;
    public TextMeshProUGUI textoResultados;

    private int aciertos = 0;
    private int documentosProcesados = 0;
    private int totalDocumentos = 7;
    private float tiempoJugado = 0f;
    private bool juegoActivo = false;

    void Start()
    {
        if (panelVictoria != null) panelVictoria.SetActive(false);

        ActualizarPuntuacion();
        juegoActivo = true;
    }

    void Update()
    {
        if (juegoActivo)
        {
            tiempoJugado += Time.deltaTime;
            ActualizarCronometro();
        }
    }

    public void DocumentoEvaluado(bool esCorrecto)
    {
        documentosProcesados++;

        if (esCorrecto)
        {
            aciertos++;
        }

        AtlasKiosko.Obtener().DocumentoEvaluado(esCorrecto);

        ActualizarPuntuacion();

        if (documentosProcesados >= totalDocumentos)
        {
            TerminarJuego();
        }
    }

    private void ActualizarPuntuacion()
    {
        textoPuntuacion.text = aciertos + "/" + totalDocumentos;
    }

    private void ActualizarCronometro()
    {
        int minutos = Mathf.FloorToInt(tiempoJugado / 60);
        int segundos = Mathf.FloorToInt(tiempoJugado % 60);
        textoTiempo.text = string.Format("{0:00}:{1:00}", minutos, segundos);
    }

    private void TerminarJuego()
    {
        juegoActivo = false;

        int minutos = Mathf.FloorToInt(tiempoJugado / 60);
        int segundos = Mathf.FloorToInt(tiempoJugado % 60);
        string tiempoFinalStr = string.Format("{0:00}:{1:00}", minutos, segundos);

        
        bool gano = aciertos >= 4;
        int monedasGanadas = 0;

        PlayerPrefs.SetInt("AciertosFinales", aciertos);
        PlayerPrefs.SetInt("TotalDocumentos", totalDocumentos);
        PlayerPrefs.SetString("TiempoFinal", tiempoFinalStr);
        PlayerPrefs.SetInt("MonedasGanadas", monedasGanadas);
        PlayerPrefs.Save();

        string destino = gano ? "AE_Pantalla_Victoria" : "AE_Pantalla_Derrota";
        AtlasKiosko.Obtener().Terminar(aciertos, totalDocumentos, tiempoJugado, gano, destino);
    }

    public void ReiniciarTurno()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}