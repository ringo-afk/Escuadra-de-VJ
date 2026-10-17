using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ResultadoFinalUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text textoMotivo;
    [SerializeField] private TMP_Text textoAltura;
    [SerializeField] private TMP_Text textoPuntos;
    [SerializeField] private TMP_Text textoAciertos;
    [SerializeField] private GameObject textoRecord;
    [SerializeField] private Button botonJugarDeNuevo;
    [SerializeField] private Button botonVolver;

    [Header("Configuración")]
    [SerializeField] private string modo = "Clásico";
    [SerializeField] private string escenaMenu = "BS_Menu";
    [SerializeField] private MusicaJuego musica;
    [SerializeField] private string escenaMinijuegos = "MenuMinijuegos";

    public event System.Action AlJugarDeNuevo;
    public event System.Action AlMostrar;

    private bool yaGuardado;
    private bool esRecord;

    public bool EsRecord => esRecord;

    private void Awake()
    {
        if (EsUrlDelPortal(Application.absoluteURL)) ConfigurarParaPortal();
        else botonJugarDeNuevo.onClick.AddListener(JugarDeNuevo);

        botonVolver.onClick.AddListener(Volver);
        panel.SetActive(false);
    }

    // el portal abre el juego con ?usuario= en la URL, en el editor la URL viene vacía
    private static bool EsUrlDelPortal(string url)
    {
        return url.Contains("?usuario=") || url.Contains("&usuario=");
    }

    // en el kiosco solo se juega una partida por turno, en vez de jugar de nuevo se regresa a los minijuegos
    private void ConfigurarParaPortal()
    {
        botonJugarDeNuevo.onClick.RemoveListener(JugarDeNuevo);
        botonJugarDeNuevo.onClick.AddListener(IrAMinijuegos);
        TMP_Text texto = botonJugarDeNuevo.GetComponentInChildren<TMP_Text>(true);
        if (texto != null) texto.text = "Volver a minijuegos";
    }

    private void IrAMinijuegos()
    {
        if (musica != null) musica.SalirA(escenaMinijuegos);
        else SceneManager.LoadScene(escenaMinijuegos);
    }

    public void Mostrar(string motivo, int altura, int puntosBase, int bono, int total, float porcentajeAciertos)
    {
        // por si se llama dos veces en la misma partida, solo se guarda la primera
        if (yaGuardado) return;
        yaGuardado = true;

        // el récord se revisa antes de guardar, porque GuardarResultado lo actualiza
        int mejorAnterior = PlayerPrefs.GetInt("MejorPuntaje", 0);
        esRecord = total > mejorAnterior;

        MenuManager.GuardarResultado(modo, altura, total);

        textoMotivo.text = motivo;
        textoAltura.text = $"Altura: {altura} bloques";
        textoPuntos.text = $"Puntos: {puntosBase} + {bono} de bono = {total}";
        textoAciertos.text = $"Aciertos: {Mathf.RoundToInt(porcentajeAciertos * 100f)}%";
        textoRecord.SetActive(esRecord);
        panel.SetActive(true);
        AlMostrar?.Invoke();
    }

    public void Ocultar()
    {
        panel.SetActive(false);
        yaGuardado = false;
    }

    private void JugarDeNuevo()
    {
        AlJugarDeNuevo?.Invoke();
    }

    private void Volver()
    {
        if (musica != null) musica.SalirA(escenaMenu);
        else SceneManager.LoadScene(escenaMenu);
    }
}
