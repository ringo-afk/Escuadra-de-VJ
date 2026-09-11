using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controla el menú principal de Torre de Café: navegación entre paneles,
/// lectura de estadísticas guardadas, historial y audio.
/// </summary>
public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject comoSeJuegaPanel;
    [SerializeField] private GameObject historialPanel;

    [Header("Stats en Main Menu")]
    [SerializeField] private TMP_Text mejorPuntajeText;
    [SerializeField] private TMP_Text ultimaPartidaText;

    [Header("Historial")]
    [SerializeField] private Transform historialContent;
    [SerializeField] private HistorialRow historialRowPrefab;

    [Header("Escena de juego")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickSound;

    // Claves de PlayerPrefs. Guárdalas iguales en el script del juego
    // al terminar una partida.
    private const string KEY_MEJOR_PUNTAJE = "MejorPuntaje";
    private const string KEY_ULTIMA_PARTIDA = "UltimaPartida";
    private const string KEY_HISTORIAL = "HistorialJson";

    private void Start()
    {
        MostrarMainMenu();
        CargarStats();
    }

    // ---------- Audio ----------

    private void ReproducirClick()
    {
        if (sfxSource != null && clickSound != null)
        {
            sfxSource.PlayOneShot(clickSound);
        }
    }

    // ---------- Navegación ----------

    public void MostrarMainMenu()
    {
        ReproducirClick();
        mainMenuPanel.SetActive(true);
        comoSeJuegaPanel.SetActive(false);
        historialPanel.SetActive(false);
    }

    public void MostrarComoSeJuega()
    {
        ReproducirClick();
        mainMenuPanel.SetActive(false);
        comoSeJuegaPanel.SetActive(true);
        historialPanel.SetActive(false);
    }

    public void MostrarHistorial()
    {
        ReproducirClick();
        mainMenuPanel.SetActive(false);
        comoSeJuegaPanel.SetActive(false);
        historialPanel.SetActive(true);
        PoblarHistorial();
    }

    public void Jugar()
    {
        ReproducirClick();
        SceneManager.LoadScene(gameSceneName);
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ---------- Stats ----------

    private void CargarStats()
    {
        int mejor = PlayerPrefs.GetInt(KEY_MEJOR_PUNTAJE, 0);
        int ultima = PlayerPrefs.GetInt(KEY_ULTIMA_PARTIDA, 0);

        mejorPuntajeText.text = $"{mejor} pts";
        ultimaPartidaText.text = $"{ultima} pts";
    }

    // ---------- Historial ----------

    [System.Serializable]
    public class HistorialEntry
    {
        public string fecha;
        public string modo;
        public int altura;
        public int puntos;
    }

    [System.Serializable]
    private class HistorialWrapper
    {
        public List<HistorialEntry> entries = new List<HistorialEntry>();
    }

    private void PoblarHistorial()
    {
        // Limpia filas anteriores
        foreach (Transform child in historialContent)
        {
            Destroy(child.gameObject);
        }

        List<HistorialEntry> entries = CargarHistorial();

        if (entries.Count == 0)
        {
            return; // Deja el panel vacío, sin partidas registradas todavía
        }

        foreach (HistorialEntry entry in entries)
        {
            HistorialRow row = Instantiate(historialRowPrefab, historialContent);
            row.Configurar(entry.fecha, entry.modo, entry.altura, entry.puntos);
        }
    }

    private List<HistorialEntry> CargarHistorial()
    {
        string json = PlayerPrefs.GetString(KEY_HISTORIAL, "");
        if (string.IsNullOrEmpty(json))
        {
            return new List<HistorialEntry>();
        }

        HistorialWrapper wrapper = JsonUtility.FromJson<HistorialWrapper>(json);
        return wrapper != null ? wrapper.entries : new List<HistorialEntry>();
    }

    /// <summary>
    /// Llama esto desde el script del juego al terminar una partida
    /// para guardar el resultado y actualizar mejor puntaje / última partida.
    /// </summary>
    public static void GuardarResultado(string modo, int altura, int puntos)
    {
        int mejor = PlayerPrefs.GetInt(KEY_MEJOR_PUNTAJE, 0);
        if (puntos > mejor)
        {
            PlayerPrefs.SetInt(KEY_MEJOR_PUNTAJE, puntos);
        }
        PlayerPrefs.SetInt(KEY_ULTIMA_PARTIDA, puntos);

        string json = PlayerPrefs.GetString(KEY_HISTORIAL, "");
        HistorialWrapper wrapper = string.IsNullOrEmpty(json)
            ? new HistorialWrapper()
            : JsonUtility.FromJson<HistorialWrapper>(json);

        wrapper.entries.Insert(0, new HistorialEntry
        {
            fecha = System.DateTime.Now.ToString("dd MMM"),
            modo = modo,
            altura = altura,
            puntos = puntos
        });

        PlayerPrefs.SetString(KEY_HISTORIAL, JsonUtility.ToJson(wrapper));
        PlayerPrefs.Save();
    }
}