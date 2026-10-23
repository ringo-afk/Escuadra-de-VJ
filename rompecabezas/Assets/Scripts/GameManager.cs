using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias de UI")]
    [Tooltip("Panel que se activa al ganar. Debe estar DESACTIVADO en la escena.")]
    public GameObject victoryPanel;

    [Tooltip("Opcional: HUD con cronometro y progreso.")]
    public HudController hud;

    [Header("Banco de imagenes")]
    [Tooltip("Opcional: si se asigna, cada reinicio libera la imagen actual y sortea otra.")]
    public PuzzleGenerator puzzleGenerator;

    [Header("Audio")]
    [Tooltip("AudioSource de este mismo objeto. Play On Awake DESMARCADO.")]
    public AudioSource audioSource;

    [Tooltip("Sonido corto al encajar una pieza.")]
    public AudioClip sfxPlace;

    [Tooltip("Sonido al completar el rompecabezas.")]
    public AudioClip sfxVictory;

    [Tooltip("Variacion aleatoria de tono al encajar, para que no suene repetitivo. 0 = sin variacion.")]
    [Range(0f, 0.2f)] public float placePitchJitter = 0.06f;

    [Header("Dificultad")]
    [Tooltip("Tamano de cuadricula con el que arranca (3 = facil, 4 = medio, 5 = dificil). " +
             "Lo normal es que lo ponga el menu de dificultad.")]
    [Range(2, 8)] public int gridSize = 3;

    [Tooltip("Solo para el modo antiguo (piezas hechas a mano en la escena, sin PuzzleGenerator). " +
             "Revuelve las posiciones iniciales de las piezas para que no queden en el mismo " +
             "orden que los huecos. Desmarcalo solo para depurar.")]
    public bool shuffleStartPositions = true;

    [Header("Puntos y racha")]
    [Tooltip("Puntos por cada pieza colocada. Se multiplican por la racha (x1, x2, x3...).")]
    public int puntosPorPieza = 10;

    [Tooltip("Segundos maximos entre dos piezas para que la racha siga. Si pasa mas tiempo, vuelve a x1.")]
    public float ventanaRacha = 2f;

    [Tooltip("Multiplicador maximo de la racha.")]
    [Range(1, 10)] public int multiplicadorMaximo = 5;

    [Tooltip("Bono por completar el rompecabezas en Facil (3x3).")]
    public int bonoFacil = 200;
    [Tooltip("Bono por completar el rompecabezas en Medio (4x4).")]
    public int bonoMedio = 450;
    [Tooltip("Bono por completar el rompecabezas en Dificil (5x5).")]
    public int bonoDificil = 800;

    [Tooltip("Tiempo meta = piezas x este valor (9 piezas = 54 s). Lo que sobre se paga como bono de rapidez.")]
    public float segundosMetaPorPieza = 6f;
    [Tooltip("Puntos por cada segundo que sobre respecto al tiempo meta.")]
    public int puntosPorSegundoSobrante = 3;

    [Tooltip("Muestra el +N con fuego donde se coloca cada pieza.")]
    public bool mostrarFuego = true;
    [Tooltip("Dibuja el circulo donde puede salir el +N (para ajustar; normalmente apagado).")]
    public bool mostrarCirculoFuego = false;

    [Header("Kiosko")]
    [Tooltip("Guarda la partida en el kiosko (puntaje, monedas y ranking) al ganar. Si lo dejas vacio se busca solo.")]
    public Kiosko.HeadBreakerResultados headBreaker;

    [Header("Ajustes")]
    [Tooltip("Segundos de espera antes de mostrar el panel de victoria.")]
    public float victoryDelay = 0.4f;

    [Tooltip("Segundos tras la victoria para reiniciar solo. 0 = no reiniciar.")]
    public float autoResetSeconds = 20f;

    private bool tiempoAgotado;
    private int racha;
    private int mejorRacha;
    private float ultimoAcierto = -99f;
    private int puntosPiezas;

    public int Racha => racha;
    public int PuntosPiezas => puntosPiezas;

    private readonly List<PuzzlePiece> pieces = new List<PuzzlePiece>();
    private int placedCount;
    private bool gameWon;

    public int TotalPieces => pieces.Count;
    public int PlacedPieces => placedCount;
    public bool IsWon => gameWon;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (victoryPanel != null) victoryPanel.SetActive(false);
        else Debug.LogError("[GameManager] 'Victory Panel' esta vacio en el Inspector. " +
                            "Al ganar no aparecera nada.", this);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (headBreaker == null) headBreaker = FindAnyObjectByType<Kiosko.HeadBreakerResultados>();

        if (hud != null && hud.timerText != null) EfectoFuego.fuente = hud.timerText.font;
    }

    void Start()
    {
        if (headBreaker != null && headBreaker.UsaTurnos && victoryPanel != null)
        {
            Transform boton = victoryPanel.transform.Find("TarjetaVictoria/BotonReiniciar");
            TMPro.TMP_Text texto = boton != null ? boton.GetComponentInChildren<TMPro.TMP_Text>(true) : null;
            if (texto != null) texto.text = "VOLVER AL INICIO";
        }

        if (shuffleStartPositions && puzzleGenerator == null) StartCoroutine(ShuffleNextFrame());
    }

    void Update()
    {
        if (headBreaker != null && headBreaker.TurnoActivo && !gameWon && !IsLoading && !tiempoAgotado
            && headBreaker.SegundosRestantes <= 0f)
        {
            StartCoroutine(TiempoAgotadoRoutine());
        }
    }

    private IEnumerator TiempoAgotadoRoutine()
    {
        tiempoAgotado = true;
        gameWon = true;
        racha = 0;

        if (hud != null) hud.OnVictory();

        if (headBreaker != null)
            headBreaker.TerminarRompecabezas(false, hud != null ? Mathf.RoundToInt(hud.Elapsed) : -1, 0);

        yield return new WaitForSecondsRealtime(3f);

        MenuController menu = FindAnyObjectByType<MenuController>();
        if (menu != null) menu.ShowMenu();
    }

    private IEnumerator ShuffleNextFrame()
    {
        yield return null;
        ShuffleStartPositions();
    }

    public void RegisterPiece(PuzzlePiece piece)
    {
        if (piece == null || pieces.Contains(piece)) return;
        pieces.Add(piece);
    }

  
    public void ShuffleStartPositions()
    {
        int n = pieces.Count;
        if (n < 2) return;

        Vector2[] pos = new Vector2[n];
        for (int i = 0; i < n; i++) pos[i] = pieces[i].StartPosition;

        Vector2[] original = (Vector2[])pos.Clone();

        int intentos = 0;
        do
        {
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Vector2 tmp = pos[i];
                pos[i] = pos[j];
                pos[j] = tmp;
            }
            intentos++;
        }
        while (intentos < 12 && SameOrder(pos, original));   

        for (int i = 0; i < n; i++) pieces[i].SetStartPosition(pos[i]);
    }

    private bool SameOrder(Vector2[] a, Vector2[] b)
    {
        for (int i = 0; i < a.Length; i++)
            if ((a[i] - b[i]).sqrMagnitude > 0.01f) return false;
        return true;
    }

   
    public void OnPiecePlaced(PuzzlePiece piece)
    {
        if (gameWon) return;

        placedCount++;
        PlaySfx(sfxPlace, placePitchJitter);

        float ahora = Time.unscaledTime;
        racha = (ahora - ultimoAcierto <= ventanaRacha) ? racha + 1 : 1;
        ultimoAcierto = ahora;
        mejorRacha = Mathf.Max(mejorRacha, racha);

        int puntos = puntosPorPieza * Mathf.Min(racha, multiplicadorMaximo);
        puntosPiezas += puntos;

        if (mostrarFuego && piece != null && piece.targetSlot != null)
        {
            EfectoFuego.mostrarCirculo = mostrarCirculoFuego;
            EfectoFuego.Mostrar(piece.targetSlot, "+" + puntos, racha);
        }

        if (placedCount >= pieces.Count && pieces.Count > 0)
            StartCoroutine(WinRoutine());
    }

    public void OnPieceMissed(PuzzlePiece piece)
    {
        racha = 0;
        ultimoAcierto = -99f;
    }

    public int CalcularPuntajeFinal(float segundos, out int bonoNivel, out int bonoRapidez)
    {
        bonoNivel = gridSize <= 3 ? bonoFacil : gridSize == 4 ? bonoMedio : bonoDificil;
        float meta = TotalPieces * segundosMetaPorPieza;
        bonoRapidez = Mathf.Max(0, Mathf.RoundToInt((meta - segundos) * puntosPorSegundoSobrante));
        return puntosPiezas + bonoNivel + bonoRapidez;
    }

    private IEnumerator WinRoutine()
    {
        gameWon = true;
        if (hud != null) hud.OnVictory();

        float segundos = hud != null ? hud.Elapsed : 0f;
        int puntaje = CalcularPuntajeFinal(segundos, out int bonoNivel, out int bonoRapidez);
        Debug.Log($"[GameManager] Puntaje {puntaje} = piezas {puntosPiezas} + nivel {bonoNivel} + rapidez {bonoRapidez} " +
                  $"(racha maxima x{mejorRacha})");

        if (hud != null && hud.victoryTimeText != null)
            hud.victoryTimeText.text += $"  ·  racha x{mejorRacha}";

        if (headBreaker != null)
            headBreaker.TerminarRompecabezas(true, Mathf.RoundToInt(segundos), puntaje);

        yield return new WaitForSecondsRealtime(victoryDelay);

        PlaySfx(sfxVictory, 0f);
        if (victoryPanel != null) victoryPanel.SetActive(true);

        if (autoResetSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(autoResetSeconds);
            RestartPuzzle();
        }
    }

    private void PlaySfx(AudioClip clip, float jitter)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = jitter > 0f ? 1f + Random.Range(-jitter, jitter) : 1f;
        audioSource.PlayOneShot(clip);
    }

  
    public int GridSize => gridSize;
    public bool IsLoading { get; private set; }

    public void StartNewGame(int size, System.Action onReady = null)
    {
        gridSize = Mathf.Clamp(size, 2, 8);
        StopAllCoroutines();
        StartCoroutine(RestartRoutine(onReady));
    }

    public void RestartPuzzle()
    {
        if (gameWon && headBreaker != null && headBreaker.UsaTurnos && !headBreaker.TurnoActivo)
        {
            MenuController menu = FindAnyObjectByType<MenuController>();
            if (menu != null) { menu.ShowMenu(); return; }
        }
        StartNewGame(gridSize);
    }

    private IEnumerator RestartRoutine(System.Action onReady)
    {
        IsLoading = true;
        if (victoryPanel != null) victoryPanel.SetActive(false);

        placedCount = 0;
        gameWon = false;
        tiempoAgotado = false;
        racha = 0;
        mejorRacha = 0;
        ultimoAcierto = -99f;
        puntosPiezas = 0;

        if (puzzleGenerator != null)
        {
            pieces.Clear();
            yield return puzzleGenerator.BuildPuzzle(gridSize);
        }
        else
        {
            foreach (PuzzlePiece p in pieces) if (p != null) p.ResetPiece();
            yield return null;
            if (shuffleStartPositions) ShuffleStartPositions();
        }

        if (hud != null) hud.ResetTimer();
        IsLoading = false;
        onReady?.Invoke();
    }
}
