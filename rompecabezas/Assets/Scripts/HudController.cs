using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [Header("Textos del HUD")]
    [Tooltip("Muestra '4 / 9'. Puede quedar vacio.")]
    public TMP_Text progressText;

    [Tooltip("Muestra '01:24'. Puede quedar vacio.")]
    public TMP_Text timerText;

    [Header("Panel de pausa")]
    [Tooltip("Panel que cubre la pantalla al pausar. Debe tener un Image con Raycast Target MARCADO: " +
             "asi bloquea el arrastre de las piezas sin necesidad de tocar PuzzlePiece.")]
    public GameObject pausePanel;

    [Header("Texto del tiempo final")]
    [Tooltip("Opcional: TMP dentro del VictoryPanel donde se escribe el tiempo al ganar.")]
    public TMP_Text victoryTimeText;

    [Tooltip("Texto que envuelve al tiempo final. {0} se reemplaza por 'mm:ss'.")]
    public string victoryTimeFormat = "Tiempo: {0}";

    [Header("Limite de tiempo (turnos)")]
    [Tooltip("Color del reloj cuando queda 1 minuto o menos.")]
    public Color colorPocoTiempo = new Color(1f, 0.35f, 0.3f);

    private Kiosko.HeadBreakerResultados headBreaker;
    private Color colorTiempoNormal = Color.white;

    private float elapsed;
    private bool running = true;
    private bool paused;

    public float Elapsed => elapsed;
    public bool IsPaused => paused;
    public string ElapsedText => Format(elapsed);

    [Header("Botones extra del menu de pausa")]
    [Tooltip("Agrega REINICIAR y SALIR debajo de CONTINUAR (se crean por codigo copiando su estilo).")]
    public bool agregarBotonesPausa = true;

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (agregarBotonesPausa) CrearBotonesPausa();
        headBreaker = FindAnyObjectByType<Kiosko.HeadBreakerResultados>();
        if (timerText != null) colorTiempoNormal = timerText.color;
        Refresh();
    }

    public void ReiniciarDesdePausa()
    {
        Resume();
        if (GameManager.Instance != null) GameManager.Instance.RestartPuzzle();
    }

    public void SalirAlMenu()
    {
        Resume();

        Kiosko.HeadBreakerResultados hb = FindAnyObjectByType<Kiosko.HeadBreakerResultados>();
        if (hb != null) hb.TerminarTurno();

        MenuController menu = FindAnyObjectByType<MenuController>();
        if (menu != null) menu.ShowMenu();
    }

    private void CrearBotonesPausa()
    {
        if (pausePanel == null) return;
        Transform tContinuar = pausePanel.transform.Find("BotonContinuar");
        if (tContinuar == null || tContinuar.GetComponent<Button>() == null)
        {
            Debug.LogWarning("[HudController] No encontre 'BotonContinuar' en el panel de pausa.", this);
            return;
        }

        RectTransform rc = (RectTransform)tContinuar;
        float paso = rc.sizeDelta.y + 20f;

        ClonarBoton(tContinuar.gameObject, "BotonReiniciarPausa", "REINICIAR",
                    rc.anchoredPosition + new Vector2(0f, -paso), 1f, ReiniciarDesdePausa);
        ClonarBoton(tContinuar.gameObject, "BotonSalirPausa", "SALIR",
                    rc.anchoredPosition + new Vector2(0f, -paso * 2f), 0.75f, SalirAlMenu);
    }

    private static void ClonarBoton(GameObject molde, string nombre, string texto, Vector2 pos,
                                    float alfa, UnityAction alClic)
    {
        if (molde.transform.parent.Find(nombre) != null) return;

        GameObject go = Instantiate(molde, molde.transform.parent);
        go.name = nombre;
        go.SetActive(true);
        ((RectTransform)go.transform).anchoredPosition = pos;

        Button b = go.GetComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(alClic);

        Image img = go.GetComponent<Image>();
        if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, img.color.a * alfa);

        TMP_Text t = go.GetComponentInChildren<TMP_Text>(true);
        if (t != null) t.text = texto;
    }

    void Update()
    {
        if (running && !paused)
            elapsed += Time.unscaledDeltaTime;

        Refresh();
    }

    private void Refresh()
    {
        if (timerText != null)
        {
            float restante = headBreaker != null ? headBreaker.SegundosRestantes : -1f;
            if (restante >= 0f)
            {
                timerText.text = Format(Mathf.Ceil(restante));
                timerText.color = restante <= 60f ? colorPocoTiempo : colorTiempoNormal;
            }
            else
            {
                timerText.text = Format(elapsed);
                timerText.color = colorTiempoNormal;
            }
        }

        if (progressText != null)
        {
            GameManager gm = GameManager.Instance;
            int placed = gm != null ? gm.PlacedPieces : 0;
            int total  = gm != null ? gm.TotalPieces  : 0;
            progressText.text = placed + " / " + total;
        }
    }

    private string Format(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    public void OnVictory()
    {
        running = false;
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (victoryTimeText != null)
            victoryTimeText.text = string.Format(victoryTimeFormat, Format(elapsed));
    }

    public void ResetTimer()
    {
        elapsed = 0f;
        running = true;
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Refresh();
    }

    public void TogglePause()
    {
        paused = !paused;
        if (pausePanel != null) pausePanel.SetActive(paused);
    }

    public void Resume()
    {
        paused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
    }
}
