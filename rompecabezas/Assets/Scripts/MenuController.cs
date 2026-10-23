using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Serializable]
    public class NivelDificultad
    {
        [Tooltip("Texto grande del boton.")]
        public string nombre;
        [Tooltip("Lado de la cuadricula: 3 = 3x3 (9 piezas), 4 = 4x4 (16), 5 = 5x5 (25).")]
        [Range(2, 8)] public int tamano = 3;

        public NivelDificultad() { }

        public NivelDificultad(string nombre, int tamano) { this.nombre = nombre; this.tamano = tamano; }
    }

    [Header("Panel")]
    [Tooltip("Panel que contiene toda la pantalla de inicio. Debe ser el ULTIMO hijo del Canvas.")]
    public GameObject menuPanel;

    [Header("Regreso automatico")]
    [Tooltip("Segundos que se queda la pantalla de victoria antes de volver al menu. 0 = no volver.")]
    public float returnToMenuSeconds = 12f;

    [Header("Seleccion de dificultad (aparece despues de 'Jugar')")]
    public NivelDificultad[] niveles =
    {
        new NivelDificultad("FÁCIL", 3),
        new NivelDificultad("MEDIO", 4),
        new NivelDificultad("DIFÍCIL", 5),
    };

    [Tooltip("Titulo de la pantalla de dificultad.")]
    public string tituloSelector = "ELIGE TU DIFICULTAD";

    [Tooltip("Texto del boton para regresar al inicio.")]
    public string textoVolver = "‹  VOLVER";

    [Tooltip("Reescribe el texto del pie del menu con los niveles (3 x 3 · 4 x 4 · 5 x 5). Apagado: el pie queda vacio.")]
    public bool actualizarTextoPie = false;

    [Header("Ranking (boton debajo de JUGAR)")]
    public string textoBotonRanking = "RANKING";
    public string tituloRanking = "RANKING · HEAD BREAKER";

    [Header("Instrucciones (boton debajo de RANKING)")]
    public string textoBotonInstrucciones = "INSTRUCCIONES";
    public string tituloInstrucciones = "CÓMO JUGAR";
    public string textoPieInstrucciones = "10 puntos por pieza  ·  bono por dificultad y por rapidez  ·  3 partidas por día de 10 minutos";

    [Header("Azulejos flotantes (opcional)")]
    [Tooltip("Images decorativos del menu. Se llenan con fotos al azar del banco. " +
             "Deja el arreglo en Size 0 si no los quieres.")]
    public Image[] tiles;

    [Tooltip("Debe coincidir con el PuzzleGenerator.")]
    public string resourcesSubfolder = "Puzzles";
    public string filePrefix = "puzzle_";
    public int imageCount = 9;
    [Range(1, 4)] public int numberPadding = 2;

    private bool menuVisible;
    private float winTimer;
    private readonly List<Sprite> spritesDelMenu = new List<Sprite>();

    private RectTransform selectorRoot;
    private readonly List<GameObject> elementosInicio = new List<GameObject>();
    private readonly List<Button> botonesSelector = new List<Button>();
    private bool cargando;
    private bool pidiendoTurno;
    private TMP_Text txtAvisoSelector;
    private TMP_Text txtEstadoTurno;
    private string textoEstadoOriginal;
    private float proximaConsultaTurnos;
    private float inicioCarga;

    private static readonly string[] OCULTAR_EN_SELECTOR = { "BotonEmpezar", "BotonRanking", "BotonInstrucciones", "TxtSubtitulo", "TxtDuracion", "Filete" };

    private static readonly string[] OCULTAR_EN_RANKING = { "LogoMenu", "TxtEyebrow" };

    private RectTransform instruccionesRoot;
    private RectTransform rankingRoot;
    private TMP_Text txtRankingLista;
    private TMP_Text txtRankingPie;
    private readonly List<GameObject> elementosRanking = new List<GameObject>();
    private string pieMejor = "";
    private string pieSaldo = "";

    public bool IsMenuVisible => menuVisible;

    void Awake()
    {
        if (menuPanel == null)
            Debug.LogError("[MenuController] 'Menu Panel' esta vacio en el Inspector.", this);
    }

    void Start()
    {
        if (actualizarTextoPie) ActualizarTextoPie();
        CrearBotonRanking();
        CrearBotonInstrucciones();
        ReunirElementosInicio();
        ShowMenu();
    }

    void Update()
    {
        if (cargando && !pidiendoTurno && Time.unscaledTime - inicioCarga > 0.25f)
        {
            GameManager g = GameManager.Instance;
            if (g != null && !g.IsLoading) TerminarCarga();
        }

        if (menuVisible && Time.unscaledTime >= proximaConsultaTurnos) ActualizarEstadoTurnos();

        if (menuVisible || returnToMenuSeconds <= 0f) return;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.IsWon)
        {
            winTimer += Time.unscaledDeltaTime;
            if (winTimer >= returnToMenuSeconds) ShowMenu();
        }
        else
        {
            winTimer = 0f;
        }
    }

    public void ShowMenu()
    {
        menuVisible = true;
        winTimer = 0f;

        if (menuPanel != null) menuPanel.SetActive(true);
        MostrarVistaInicio();
        CargarAzulejos();
        ActualizarEstadoTurnos();
    }

    public void AbrirSelectorDificultad()
    {
        if (menuPanel == null) return;
        ConstruirSelector();
        if (selectorRoot == null) return;

        foreach (GameObject go in elementosInicio) if (go != null) go.SetActive(false);
        selectorRoot.gameObject.SetActive(true);
        PonerInteractivos(true);
    }

    public void MostrarVistaInicio()
    {
        if (selectorRoot != null) selectorRoot.gameObject.SetActive(false);
        if (rankingRoot != null) rankingRoot.gameObject.SetActive(false);
        if (instruccionesRoot != null) instruccionesRoot.gameObject.SetActive(false);
        foreach (GameObject go in elementosInicio) if (go != null) go.SetActive(true);
        foreach (GameObject go in elementosRanking) if (go != null) go.SetActive(true);
    }

    public void StartGame()
    {
        AbrirSelectorDificultad();
    }

    public void IniciarJuego(int tamano)
    {
        if (cargando) return;

        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[MenuController] No hay GameManager en la escena.", this);
            return;
        }

        cargando = true;
        inicioCarga = Time.unscaledTime;
        PonerInteractivos(false);

        Kiosko.HeadBreakerResultados hb = FindAnyObjectByType<Kiosko.HeadBreakerResultados>();
        if (hb != null && hb.UsaTurnos)
        {
            pidiendoTurno = true;
            MostrarAvisoSelector("Pidiendo turno…");
            hb.IniciarTurno(
                () => ArrancarPartida(gm, tamano),
                (mensaje, esErrorDeRed) =>
                {
                    if (esErrorDeRed && hb.PermiteSinConexion)
                    {
                        ArrancarPartida(gm, tamano);
                        return;
                    }
                    pidiendoTurno = false;
                    cargando = false;
                    PonerInteractivos(true);
                    MostrarAvisoSelector(mensaje);
                    ActualizarEstadoTurnos();
                });
            return;
        }

        gm.StartNewGame(tamano, TerminarCarga);
    }

    private void ArrancarPartida(GameManager gm, int tamano)
    {
        pidiendoTurno = false;
        inicioCarga = Time.unscaledTime;
        MostrarAvisoSelector("");
        gm.StartNewGame(tamano, TerminarCarga);
    }

    private void MostrarAvisoSelector(string texto)
    {
        if (txtAvisoSelector != null) txtAvisoSelector.text = texto;
    }

    private void ActualizarEstadoTurnos()
    {
        proximaConsultaTurnos = Time.unscaledTime + 30f;

        if (txtEstadoTurno == null && menuPanel != null)
        {
            Transform t = menuPanel.transform.Find("TxtDuracion");
            txtEstadoTurno = t != null ? t.GetComponent<TMP_Text>() : null;
            if (txtEstadoTurno != null) textoEstadoOriginal = txtEstadoTurno.text;
        }
        if (txtEstadoTurno == null) return;

        Kiosko.HeadBreakerResultados hb = FindAnyObjectByType<Kiosko.HeadBreakerResultados>();
        Kiosko.KioskoApi api = Kiosko.KioskoApi.Instancia;
        if (hb == null || !hb.UsaTurnos || api == null || !Kiosko.SesionKiosko.HayUsuario) return;

        api.ObtenerTurnos(Kiosko.SesionKiosko.Usuario, "HEAD_BREAKER",
            r =>
            {
                if (txtEstadoTurno == null || r == null || r.resumen == null) return;
                txtEstadoTurno.text = r.resumen.mensaje;
            },
            _ =>
            {
                if (txtEstadoTurno != null) txtEstadoTurno.text = textoEstadoOriginal;
            });
    }

    private void TerminarCarga()
    {
        if (!cargando) return;
        cargando = false;
        PonerInteractivos(true);
        OcultarMenu();
    }

    private void OcultarMenu()
    {
        menuVisible = false;
        winTimer = 0f;

        LiberarAzulejos();
        MostrarVistaInicio();
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    private void PonerInteractivos(bool valor)
    {
        foreach (Button b in botonesSelector) if (b != null) b.interactable = valor;
    }

    private void ConstruirSelector()
    {
        if (selectorRoot != null || menuPanel == null) return;

        Transform raiz = menuPanel.transform;
        Transform tBoton = raiz.Find("BotonEmpezar");
        Transform tSub = raiz.Find("TxtSubtitulo");

        if (tBoton == null || tBoton.GetComponent<Button>() == null)
        {
            Debug.LogError("[MenuController] No encontre 'BotonEmpezar' dentro de MenuPanel; " +
                           "no puedo armar la seleccion de dificultad.", this);
            return;
        }

        ReunirElementosInicio();

        GameObject goRoot = new GameObject("SelectorDificultad", typeof(RectTransform));
        selectorRoot = goRoot.GetComponent<RectTransform>();
        selectorRoot.SetParent(raiz, false);
        selectorRoot.anchorMin = Vector2.zero;
        selectorRoot.anchorMax = Vector2.one;
        selectorRoot.offsetMin = Vector2.zero;
        selectorRoot.offsetMax = Vector2.zero;

        if (tSub != null)
        {
            GameObject titulo = Instantiate(tSub.gameObject, selectorRoot);
            titulo.name = "TxtTituloSelector";
            titulo.SetActive(true);
            TMP_Text tt = titulo.GetComponent<TMP_Text>();
            if (tt != null) tt.text = tituloSelector;
        }

        RectTransform rtBoton = (RectTransform)tBoton;
        float y = rtBoton.anchoredPosition.y;
        const float ancho = 380f, alto = 124f, separacion = 30f;

        int n = niveles != null ? niveles.Length : 0;
        float total = n * ancho + Mathf.Max(0, n - 1) * separacion;
        float x0 = -total * 0.5f + ancho * 0.5f;

        for (int i = 0; i < n; i++)
        {
            NivelDificultad nivel = niveles[i];
            int lado = nivel.tamano;
            string detalle = lado + " × " + lado + " · " + (lado * lado) + " piezas";
            string texto = nivel.nombre + "\n<size=55%>" + detalle + "</size>";

            int capturado = lado;
            Button b = CrearBoton(tBoton.gameObject, selectorRoot, "Btn_" + nivel.nombre, texto,
                                  new Vector2(x0 + i * (ancho + separacion), y),
                                  new Vector2(ancho, alto), 38f, () => IniciarJuego(capturado));
            botonesSelector.Add(b);
        }

        Button volver = CrearBoton(tBoton.gameObject, selectorRoot, "Btn_Volver", textoVolver,
                                   new Vector2(0f, y - alto - 70f), new Vector2(300f, 80f), 28f,
                                   MostrarVistaInicio);
        Image imgVolver = volver.GetComponent<Image>();
        if (imgVolver != null) imgVolver.color = new Color(1f, 1f, 1f, 0.75f);
        botonesSelector.Add(volver);

        Transform tAviso = raiz.Find("TxtDuracion");
        if (tAviso != null)
        {
            GameObject aviso = Instantiate(tAviso.gameObject, selectorRoot);
            aviso.name = "TxtAvisoTurno";
            aviso.SetActive(true);
            RectTransform ra = (RectTransform)aviso.transform;
            ra.anchoredPosition = new Vector2(0f, y - alto - 190f);
            ra.sizeDelta = new Vector2(1400f, 90f);
            txtAvisoSelector = aviso.GetComponent<TMP_Text>();
            if (txtAvisoSelector != null)
            {
                txtAvisoSelector.text = "";
                txtAvisoSelector.color = new Color(1f, 0.75f, 0.25f);
            }
        }

        selectorRoot.gameObject.SetActive(false);
    }

    private Button CrearBoton(GameObject molde, Transform padre, string nombre, string texto, Vector2 pos, Vector2 tam,
                              float fuente, UnityAction alClic)
    {
        GameObject go = Instantiate(molde, padre);
        go.name = nombre;
        go.SetActive(true);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;

        Button b = go.GetComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();
        b.onClick.AddListener(alClic);

        TMP_Text t = go.GetComponentInChildren<TMP_Text>(true);
        if (t != null)
        {
            t.enableAutoSizing = false;
            t.fontSize = fuente;
            t.text = texto;
        }
        return b;
    }

    private void ReunirElementosInicio()
    {
        if (menuPanel == null) return;
        Transform raiz = menuPanel.transform;

        elementosInicio.Clear();
        foreach (string nombre in OCULTAR_EN_SELECTOR)
        {
            Transform t = raiz.Find(nombre);
            if (t != null) elementosInicio.Add(t.gameObject);
        }

        elementosRanking.Clear();
        foreach (string nombre in OCULTAR_EN_RANKING)
        {
            Transform t = raiz.Find(nombre);
            if (t != null) elementosRanking.Add(t.gameObject);
        }
    }

    private void CrearBotonRanking()
    {
        if (menuPanel == null) return;
        Transform raiz = menuPanel.transform;
        Transform tBoton = raiz.Find("BotonEmpezar");
        if (tBoton == null || raiz.Find("BotonRanking") != null) return;

        RectTransform rtJugar = (RectTransform)tBoton;
        float y = rtJugar.anchoredPosition.y - rtJugar.sizeDelta.y * 0.5f - 62f;

        Button b = CrearBoton(tBoton.gameObject, raiz, "BotonRanking", textoBotonRanking,
                              new Vector2(rtJugar.anchoredPosition.x, y), new Vector2(380f, 84f), 30f, AbrirRanking);
        Image img = b.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 1f, 1f, 0.75f);

        Transform tDur = raiz.Find("TxtDuracion");
        if (tDur != null)
        {
            RectTransform rd = (RectTransform)tDur;
            rd.anchoredPosition = new Vector2(rd.anchoredPosition.x, y - 42f - 40f);
        }
    }

    private void CrearBotonInstrucciones()
    {
        if (menuPanel == null) return;
        Transform raiz = menuPanel.transform;
        Transform tBoton = raiz.Find("BotonEmpezar");
        Transform tRank = raiz.Find("BotonRanking");
        if (tBoton == null || tRank == null || raiz.Find("BotonInstrucciones") != null) return;

        RectTransform rr = (RectTransform)tRank;
        float y = rr.anchoredPosition.y - rr.sizeDelta.y - 20f;

        Button b = CrearBoton(tBoton.gameObject, raiz, "BotonInstrucciones", textoBotonInstrucciones,
                              new Vector2(rr.anchoredPosition.x, y), new Vector2(380f, 84f), 30f, AbrirInstrucciones);
        Image img = b.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 1f, 1f, 0.75f);

        Transform tDur = raiz.Find("TxtDuracion");
        if (tDur != null)
        {
            RectTransform rd = (RectTransform)tDur;
            rd.anchoredPosition = new Vector2(rd.anchoredPosition.x, y - 42f - 40f);
        }
    }

    public void AbrirInstrucciones()
    {
        if (menuPanel == null) return;
        ConstruirInstrucciones();
        if (instruccionesRoot == null) return;

        foreach (GameObject go in elementosInicio) if (go != null) go.SetActive(false);
        foreach (GameObject go in elementosRanking) if (go != null) go.SetActive(false);
        if (selectorRoot != null) selectorRoot.gameObject.SetActive(false);
        if (rankingRoot != null) rankingRoot.gameObject.SetActive(false);
        instruccionesRoot.gameObject.SetActive(true);
    }

    private Sprite spriteTarjeta;
    private Sprite spriteCirculo;
    private Sprite spriteDemoModelo;
    private Sprite demoPieza;
    private Sprite demoHueco;

    private static Sprite CrearSpriteRedondeado(int tam, int radio, bool conBorde)
    {
        Color32[] pix = new Color32[tam * tam];
        float mitad = tam * 0.5f;
        float interior = mitad - radio;
        for (int y = 0; y < tam; y++)
        {
            for (int x = 0; x < tam; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - mitad) - interior;
                float qy = Mathf.Abs(y + 0.5f - mitad) - interior;
                float d = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
                          + Mathf.Min(Mathf.Max(qx, qy), 0f) - radio;
                pix[y * tam + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - d) * 255f));
            }
        }
        Texture2D t = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        t.SetPixels32(pix);
        t.Apply(false, true);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        Vector4 borde = conBorde ? new Vector4(radio, radio, radio, radio) : Vector4.zero;
        return Sprite.Create(t, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borde);
    }

    private static RectTransform CrearImagen(Transform padre, string nombre, Sprite sprite, Color color, Vector2 pos, Vector2 tam, bool cortado)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = cortado ? Image.Type.Sliced : Image.Type.Simple;
        img.preserveAspect = !cortado;
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private static TMP_Text CrearTextoTarjeta(GameObject molde, Transform padre, string nombre, string texto,
                                              Vector2 pos, Vector2 tam, float fuente, Color color, FontStyles estilo)
    {
        GameObject go = Instantiate(molde, padre);
        go.name = nombre;
        go.SetActive(true);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        TMP_Text t = go.GetComponent<TMP_Text>();
        t.enableAutoSizing = false;
        t.fontSize = fuente;
        t.color = color;
        t.fontStyle = estilo;
        t.alignment = TextAlignmentOptions.Center;
        t.richText = true;
        t.text = texto;
        return t;
    }

    private void ConstruirInstrucciones()
    {
        if (instruccionesRoot != null || menuPanel == null) return;

        Transform raiz = menuPanel.transform;
        Transform tBoton = raiz.Find("BotonEmpezar");
        Transform tSub = raiz.Find("TxtSubtitulo");
        if (tBoton == null || tSub == null)
        {
            Debug.LogError("[MenuController] Faltan 'BotonEmpezar' o 'TxtSubtitulo' en MenuPanel; no puedo armar las instrucciones.", this);
            return;
        }

        GameObject goRoot = new GameObject("VistaInstrucciones", typeof(RectTransform));
        instruccionesRoot = goRoot.GetComponent<RectTransform>();
        instruccionesRoot.SetParent(raiz, false);
        instruccionesRoot.anchorMin = Vector2.zero;
        instruccionesRoot.anchorMax = Vector2.one;
        instruccionesRoot.offsetMin = Vector2.zero;
        instruccionesRoot.offsetMax = Vector2.zero;

        spriteTarjeta = CrearSpriteRedondeado(64, 24, true);
        spriteCirculo = CrearSpriteRedondeado(64, 32, false);
        demoPieza = PuzzleSplitter.FormaDemo(160, 1, 0, new Color(0.96f, 0.66f, 0.16f, 1f));
        demoHueco = PuzzleSplitter.FormaDemo(160, 0, -1, new Color(1f, 1f, 1f, 0.28f));

        string carpeta = string.IsNullOrEmpty(resourcesSubfolder) ? "" : resourcesSubfolder.TrimEnd('/') + "/";
        Texture2D texDemo = Resources.Load<Texture2D>(carpeta + filePrefix + 1.ToString().PadLeft(numberPadding, '0'));
        if (texDemo != null)
            spriteDemoModelo = Sprite.Create(texDemo, new Rect(0f, 0f, texDemo.width, texDemo.height),
                                             new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

        Color dorado = new Color(1f, 0.75f, 0.25f);
        Color blanco = new Color(1f, 1f, 1f, 0.92f);
        Color oscuro = new Color(0.08f, 0.1f, 0.22f);

        GameObject titulo = Instantiate(tSub.gameObject, instruccionesRoot);
        titulo.name = "TxtTituloInstrucciones";
        titulo.SetActive(true);
        ((RectTransform)titulo.transform).anchoredPosition = new Vector2(0f, 400f);
        TMP_Text tt = titulo.GetComponent<TMP_Text>();
        if (tt != null) tt.text = tituloInstrucciones;

        string[] cabezas = { "ELIGE", "OBSERVA", "ARRASTRA", "ENCADENA" };
        string[] cuerpos =
        {
            "Fácil, Medio o Difícil: tú decides cuántas piezas armar.",
            "La imagen de referencia te muestra cómo debe quedar.",
            "Lleva cada pieza al hueco con su misma forma. Si queda cerca, encaja sola.",
            "Coloca piezas seguidas y tu multiplicador sube hasta x5. Fallar lo reinicia."
        };

        const float ancho = 400f, alto = 500f, separacion = 28f, yCentro = 75f;
        float x0 = -(4 * ancho + 3 * separacion) * 0.5f + ancho * 0.5f;

        for (int i = 0; i < 4; i++)
        {
            float cx = x0 + i * (ancho + separacion);
            RectTransform tarjeta = CrearImagen(instruccionesRoot, "Tarjeta_" + (i + 1), spriteTarjeta,
                                                new Color(1f, 1f, 1f, 0.09f), new Vector2(cx, yCentro), new Vector2(ancho, alto), true);

            CrearImagen(tarjeta, "Insignia", spriteCirculo, dorado, new Vector2(0f, 185f), new Vector2(72f, 72f), false);
            CrearTextoTarjeta(tSub.gameObject, tarjeta, "Numero", (i + 1).ToString(), new Vector2(0f, 185f),
                              new Vector2(72f, 72f), 40f, oscuro, FontStyles.Bold);
            CrearTextoTarjeta(tSub.gameObject, tarjeta, "Cabeza", cabezas[i], new Vector2(0f, 115f),
                              new Vector2(360f, 50f), 36f, dorado, FontStyles.Bold);
            CrearTextoTarjeta(tSub.gameObject, tarjeta, "Cuerpo", cuerpos[i], new Vector2(0f, -150f),
                              new Vector2(350f, 150f), 28f, blanco, FontStyles.Normal);

            if (i == 0)
            {
                CrearTextoTarjeta(tSub.gameObject, tarjeta, "Niveles", "3×3 · 4×4 · 5×5", new Vector2(0f, 5f),
                                  new Vector2(380f, 80f), 38f, blanco, FontStyles.Bold);
            }
            else if (i == 1 && spriteDemoModelo != null)
            {
                CrearImagen(tarjeta, "Modelo", spriteDemoModelo, Color.white, new Vector2(0f, 5f), new Vector2(190f, 170f), false);
            }
            else if (i == 2)
            {
                CrearImagen(tarjeta, "DemoPieza", demoPieza, Color.white, new Vector2(-105f, 5f), new Vector2(140f, 140f), false);
                CrearTextoTarjeta(tSub.gameObject, tarjeta, "Flecha", "›", new Vector2(0f, 3f),
                                  new Vector2(60f, 80f), 70f, dorado, FontStyles.Bold);
                CrearImagen(tarjeta, "DemoHueco", demoHueco, Color.white, new Vector2(100f, 5f), new Vector2(140f, 140f), false);
            }
            else if (i == 3)
            {
                CrearTextoTarjeta(tSub.gameObject, tarjeta, "Combo", "x5", new Vector2(0f, 5f),
                                  new Vector2(300f, 150f), 100f, dorado, FontStyles.Bold);
            }
        }

        CrearTextoTarjeta(tSub.gameObject, instruccionesRoot, "TxtPieInstrucciones", textoPieInstrucciones, new Vector2(0f, -235f),
                          new Vector2(1600f, 50f), 28f, new Color(0.65f, 0.78f, 1f), FontStyles.Normal);

        Button volver = CrearBoton(tBoton.gameObject, instruccionesRoot, "Btn_VolverInstrucciones", textoVolver,
                                   new Vector2(0f, -430f), new Vector2(300f, 80f), 28f, MostrarVistaInicio);
        Image imgVolver = volver.GetComponent<Image>();
        if (imgVolver != null) imgVolver.color = new Color(1f, 1f, 1f, 0.75f);

        instruccionesRoot.gameObject.SetActive(false);
    }

    public void AbrirRanking()
    {
        if (menuPanel == null) return;
        ConstruirRanking();
        if (rankingRoot == null) return;

        foreach (GameObject go in elementosInicio) if (go != null) go.SetActive(false);
        foreach (GameObject go in elementosRanking) if (go != null) go.SetActive(false);
        if (selectorRoot != null) selectorRoot.gameObject.SetActive(false);
        if (instruccionesRoot != null) instruccionesRoot.gameObject.SetActive(false);
        rankingRoot.gameObject.SetActive(true);

        CargarRanking();
    }

    private void ConstruirRanking()
    {
        if (rankingRoot != null || menuPanel == null) return;

        Transform raiz = menuPanel.transform;
        Transform tBoton = raiz.Find("BotonEmpezar");
        Transform tSub = raiz.Find("TxtSubtitulo");
        Transform tDur = raiz.Find("TxtDuracion");
        if (tBoton == null || tSub == null)
        {
            Debug.LogError("[MenuController] Faltan 'BotonEmpezar' o 'TxtSubtitulo' en MenuPanel; no puedo armar el ranking.", this);
            return;
        }

        GameObject goRoot = new GameObject("VistaRanking", typeof(RectTransform));
        rankingRoot = goRoot.GetComponent<RectTransform>();
        rankingRoot.SetParent(raiz, false);
        rankingRoot.anchorMin = Vector2.zero;
        rankingRoot.anchorMax = Vector2.one;
        rankingRoot.offsetMin = Vector2.zero;
        rankingRoot.offsetMax = Vector2.zero;

        GameObject titulo = Instantiate(tSub.gameObject, rankingRoot);
        titulo.name = "TxtTituloRanking";
        titulo.SetActive(true);
        ((RectTransform)titulo.transform).anchoredPosition = new Vector2(0f, 400f);
        TMP_Text tt = titulo.GetComponent<TMP_Text>();
        if (tt != null) tt.text = tituloRanking;

        GameObject lista = Instantiate(tSub.gameObject, rankingRoot);
        lista.name = "TxtListaRanking";
        lista.SetActive(true);
        RectTransform rl = (RectTransform)lista.transform;
        rl.anchoredPosition = new Vector2(0f, 20f);
        rl.sizeDelta = new Vector2(900f, 600f);
        txtRankingLista = lista.GetComponent<TMP_Text>();
        if (txtRankingLista != null)
        {
            txtRankingLista.enableAutoSizing = false;
            txtRankingLista.fontSize = 34f;
            txtRankingLista.alignment = TextAlignmentOptions.Top;
            txtRankingLista.richText = true;
            txtRankingLista.lineSpacing = 12f;
        }

        GameObject pie = Instantiate((tDur != null ? tDur : tSub).gameObject, rankingRoot);
        pie.name = "TxtPieRanking";
        pie.SetActive(true);
        RectTransform rp = (RectTransform)pie.transform;
        rp.anchoredPosition = new Vector2(0f, -330f);
        rp.sizeDelta = new Vector2(1000f, 50f);
        txtRankingPie = pie.GetComponent<TMP_Text>();

        Button volver = CrearBoton(tBoton.gameObject, rankingRoot, "Btn_VolverRanking", textoVolver,
                                   new Vector2(0f, -430f), new Vector2(300f, 80f), 28f, MostrarVistaInicio);
        Image imgVolver = volver.GetComponent<Image>();
        if (imgVolver != null) imgVolver.color = new Color(1f, 1f, 1f, 0.75f);

        rankingRoot.gameObject.SetActive(false);
    }

    private void CargarRanking()
    {
        pieMejor = "";
        pieSaldo = "";
        if (txtRankingPie != null) txtRankingPie.text = "";
        if (txtRankingLista == null) return;

        Kiosko.KioskoApi api = Kiosko.KioskoApi.Instancia;
        if (api == null)
        {
            txtRankingLista.text = "Sin conexión con el kiosko";
            return;
        }

        txtRankingLista.text = "Cargando…";
        int usuario = Kiosko.SesionKiosko.HayUsuario ? Kiosko.SesionKiosko.Usuario : 0;

        api.ObtenerLeaderboard("HEAD_BREAKER", usuario, MostrarRanking,
                               error => { if (txtRankingLista != null) txtRankingLista.text = error; });

        if (usuario > 0)
            api.ObtenerSaldo(usuario, s => { pieSaldo = "Saldo: " + s.saldo + " monedas"; ActualizarPieRanking(); }, _ => { });
    }

    private void MostrarRanking(Kiosko.LeaderboardRespuesta lb)
    {
        if (txtRankingLista == null) return;

        if (lb == null || lb.top == null || lb.top.Length == 0)
        {
            txtRankingLista.text = "Todavía nadie ha jugado.\n¡Sé el primero!";
        }
        else
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("<alpha=#88><size=75%><align=left><pos=8%>#<pos=18%>JUGADOR<pos=62%>PUNTOS<pos=82%>PARTIDAS</size><alpha=#FF>\n");
            foreach (Kiosko.FilaRanking f in lb.top)
            {
                string linea = $"<align=left><pos=8%>{f.posicion}<pos=18%>{f.jugador}<pos=62%>{f.mejor_puntaje}<pos=82%>{f.partidas}";
                sb.Append(f.es_usuario_actual == 1 ? $"<color=#FFC040>{linea}</color>" : linea);
                sb.Append("\n");
            }
            txtRankingLista.text = sb.ToString();
        }

        Kiosko.MiPosicion mp = lb != null ? lb.miPosicion : null;
        pieMejor = mp != null && mp.mi_posicion > 0
            ? $"Tu mejor: {mp.mi_mejor_puntaje} pts  ·  lugar {mp.mi_posicion}"
            : "Aún no tienes partidas";
        ActualizarPieRanking();
    }

    private void ActualizarPieRanking()
    {
        if (txtRankingPie == null) return;
        txtRankingPie.text = string.IsNullOrEmpty(pieSaldo) ? pieMejor
                           : string.IsNullOrEmpty(pieMejor) ? pieSaldo
                           : pieMejor + "  ·  " + pieSaldo;
    }

    private void ActualizarTextoPie()
    {
        if (menuPanel == null || niveles == null || niveles.Length == 0) return;
        Transform t = menuPanel.transform.Find("TxtPie");
        TMP_Text pie = t != null ? t.GetComponent<TMP_Text>() : null;
        if (pie == null) return;

        List<string> partes = new List<string>();
        foreach (NivelDificultad nv in niveles) partes.Add(nv.tamano + " × " + nv.tamano);
        pie.text = "Elige tu nivel · " + string.Join(" · ", partes);
    }

    private void CargarAzulejos()
    {
        if (tiles == null || tiles.Length == 0) return;

        LiberarAzulejos();

        List<int> bolsa = new List<int>();
        for (int i = 1; i <= imageCount; i++) bolsa.Add(i);
        PuzzleSplitter.Revolver(bolsa);

        string carpeta = string.IsNullOrEmpty(resourcesSubfolder) ? "" : resourcesSubfolder.TrimEnd('/') + "/";

        for (int i = 0; i < tiles.Length && i < bolsa.Count; i++)
        {
            if (tiles[i] == null) continue;

            string ruta = carpeta + filePrefix + bolsa[i].ToString().PadLeft(numberPadding, '0');

            Texture2D tex = Resources.Load<Texture2D>(ruta);
            if (tex == null)
            {
                Debug.LogWarning($"[MenuController] No pude cargar 'Resources/{ruta}'.", this);
                continue;
            }

            Sprite completa = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            completa.name = tex.name + "_menu";

            spritesDelMenu.Add(completa);
            tiles[i].sprite = completa;
            tiles[i].enabled = true;
        }
    }

    private void LiberarAzulejos()
    {
        if (tiles != null)
            foreach (Image img in tiles)
                if (img != null) img.sprite = null;

        foreach (Sprite s in spritesDelMenu)
            if (s != null) Destroy(s);
        spritesDelMenu.Clear();
    }
}
