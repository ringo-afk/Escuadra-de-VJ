using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace Headbreaker
{
    public class PuzzleGenerator : MonoBehaviour
    {
        [Header("Banco local (Resources) - imagenes dummy")]
        [Tooltip("Subcarpeta dentro de Assets/Resources. SIN 'Resources/' y SIN barra final.")]
        public string resourcesSubfolder = "Puzzles";

        [Tooltip("Prefijo del nombre de archivo. Ej: 'puzzle_' para puzzle_01.png")]
        public string filePrefix = "puzzle_";

        [Tooltip("Cuantas imagenes hay en el banco local (numeradas desde 1).")]
        public int imageCount = 9;

        [Tooltip("Digitos del numero. 2 -> puzzle_01 ; 3 -> puzzle_001")]
        [Range(1, 4)] public int numberPadding = 2;

        [Tooltip("Cuantas imagenes recientes se evitan al sortear. 0 = permitir repetir de inmediato.")]
        public int avoidRepeatLast = 3;

        [Header("Imagenes del kiosko (base de datos / web)")]
        [Tooltip("Marcalo cuando el endpoint del kiosko ya exista. Mientras tanto se usan las imagenes dummy.")]
        public bool usarRemoto = false;

        [Tooltip("URL que devuelve la lista de imagenes publicadas por el admin. Respuesta esperada: " +
                 "[{\"id\":1,\"url\":\"https://.../foto.jpg\"}, ...]  (tambien vale {\"items\":[...]}). " +
                 "'url' puede ser relativa a esta misma URL.")]
        public string urlListaImagenes = "";

        [Tooltip("Segundos maximos de espera por cada peticion.")]
        public int timeoutSegundos = 8;

        [Tooltip("Si falla el remoto, jugar con las imagenes locales en vez de quedarse sin juego.")]
        public bool fallbackALocal = true;

        [Header("Kiosko (API Commit Crew)")]
        [Tooltip("Usa las imagenes que sube el admin desde la pagina del kiosko (via HeadBreakerResultados). " +
                 "Si el API no responde, se juega con el banco local y esa partida no se guarda.")]
        public bool usarKiosko = true;

        [Tooltip("Objeto HeadBreakerResultados de la escena. Si lo dejas vacio se busca solo.")]
        public Kiosko.HeadBreakerResultados headBreaker;

        [Header("Escena")]
        [Tooltip("Miniatura con la imagen completa de referencia (en la escena: HUD/Modelo).")]
        public Image backgroundImage;

        [Tooltip("Contenedor donde se crean los slots. Si lo dejas vacio busca el objeto 'Slots'.")]
        public RectTransform slotsRoot;

        [Tooltip("Contenedor donde se crean las piezas. Si lo dejas vacio busca el objeto 'Piezas'.")]
        public RectTransform piecesRoot;

        [Tooltip("Sprite 9-slice de los huecos (Assets/UI/slot_9s). Si esta vacio se copia del primer " +
                 "Slot que exista en la escena; si tampoco hay, los huecos salen como un cuadro tenue.")]
        public Sprite slotSprite;

        [Tooltip("Al iniciar, borra los Slot_x / Pieza_x que sobren de la escena (el prototipo de 3x3 hecho a mano).")]
        public bool limpiarEscenaAlIniciar = true;

        [Header("Geometria (unidades del Canvas)")]
        [Tooltip("Centro del tablero (el marco BoardFrame).")]
        public Vector2 boardCenter = new Vector2(-300f, 80f);

        [Tooltip("Lado del cuadrado donde caben todos los huecos.")]
        public float boardSize = 612f;

        [Tooltip("Separacion entre huecos.")]
        public float slotGap = 6f;

        [Tooltip("Centro de la zona donde arrancan las piezas revueltas.")]
        public Vector2 trayCenter = new Vector2(530f, 0f);

        [Tooltip("Lado del cuadrado de esa zona.")]
        public float traySize = 690f;

        [Tooltip("La pieza encaja si se suelta a menos de (tamano de pieza x este valor) de su hueco. " +
                 "0.6 equivale a los 120 px que tenia el 3x3.")]
        [Range(0.2f, 1.5f)] public float snapFraction = 0.6f;

        [Header("Opciones")]
        public bool mostrarContornoHuecos = true;
        public bool verboseLog = true;

        [Serializable] private class ItemRemoto { public int id; public string url; }
        [Serializable] private class ListaRemota { public ItemRemoto[] items; }

        private Texture2D texturaActual;
        private bool texturaEsRuntime;
        private readonly List<Sprite> spritesActuales = new List<Sprite>();
        private Sprite spriteModelo;

        private string etiquetaActual = "";
        private readonly Queue<string> recientes = new Queue<string>();
        private List<string> urlsRemotas;

        private Texture2D adquirida;
        private bool adquiridaEsRuntime;
        private string adquiridaEtiqueta;

        private Color colorHueco = new Color(1f, 1f, 1f, 0.10f);
        private Image.Type tipoHueco = Image.Type.Simple;

        public int GridSize { get; private set; }
        public string CurrentLabel => etiquetaActual;

        void Awake()
        {
            if (slotsRoot == null) slotsRoot = BuscarPorNombre("Slots");
            if (piecesRoot == null) piecesRoot = BuscarPorNombre("Piezas");

            if (slotsRoot == null || piecesRoot == null)
                Debug.LogError("[PuzzleGenerator] No encontre los contenedores 'Slots' y/o 'Piezas'. " +
                               "Asignalos en el Inspector.", this);

            if (headBreaker == null) headBreaker = FindAnyObjectByType<Kiosko.HeadBreakerResultados>();

            CapturarEstiloDeHueco();
            if (limpiarEscenaAlIniciar) { VaciarContenedor(slotsRoot); VaciarContenedor(piecesRoot); }
        }

        public IEnumerator BuildPuzzle(int n)
        {
            n = Mathf.Clamp(n, 2, 8);
            GridSize = n;

            yield return UnloadCurrentPuzzle();

            yield return AdquirirImagen();
            if (adquirida == null)
            {
                Debug.LogError("[PuzzleGenerator] No se pudo conseguir ninguna imagen (ni remota ni local).", this);
                yield break;
            }

            texturaActual = PuzzleSplitter.Preparar(adquirida, 1024);
            texturaEsRuntime = true;
            etiquetaActual = adquiridaEtiqueta;
            if (adquiridaEsRuntime) Destroy(adquirida);
            adquirida = null;

            yield return null;

            PiezasRompecabezas jig = PuzzleSplitter.CortarJigsaw(texturaActual, n);
            spritesActuales.AddRange(jig.piezas);
            spritesActuales.AddRange(jig.siluetas);

            if (backgroundImage != null)
            {
                spriteModelo = PuzzleSplitter.ImagenCompleta(texturaActual, n);
                backgroundImage.sprite = spriteModelo;
                backgroundImage.preserveAspect = true;
                backgroundImage.enabled = true;
            }

            ConstruirTablero(n, jig);

            if (verboseLog)
                Debug.Log($"[PuzzleGenerator] '{etiquetaActual}' {texturaActual.width}x{texturaActual.height} " +
                          $"cortada en {n}x{n} = {n * n} piezas.");
        }

        public IEnumerator UnloadCurrentPuzzle()
        {
            VaciarContenedor(slotsRoot);
            VaciarContenedor(piecesRoot);

            if (backgroundImage != null) backgroundImage.sprite = null;

            foreach (Sprite s in spritesActuales)
            {
                if (s == null) continue;
                Texture2D t = s.texture;
                Destroy(s);
                if (t != null) Destroy(t);
            }
            spritesActuales.Clear();

            if (spriteModelo != null) { Destroy(spriteModelo); spriteModelo = null; }

            if (texturaEsRuntime && texturaActual != null) Destroy(texturaActual);
            texturaActual = null;
            texturaEsRuntime = false;

            yield return Resources.UnloadUnusedAssets();
        }

        private IEnumerator AdquirirImagen()
        {
            adquirida = null;
            adquiridaEsRuntime = false;
            adquiridaEtiqueta = "";

            if (headBreaker != null) headBreaker.CancelarPartida();

            if (usarKiosko && headBreaker != null)
            {
                yield return AdquirirDelKiosko();
                if (adquirida != null) yield break;
                Debug.LogWarning("[PuzzleGenerator] El kiosko no dio imagen; uso el banco local (esta partida no se guarda).", this);
            }

            if (usarRemoto && !string.IsNullOrEmpty(urlListaImagenes))
            {
                yield return AdquirirRemota();
                if (adquirida != null || !fallbackALocal) yield break;
                Debug.LogWarning("[PuzzleGenerator] Remoto sin resultado; uso el banco local.", this);
            }

            AdquirirLocal();
        }

        private IEnumerator AdquirirDelKiosko()
        {
            float limite = Time.unscaledTime + Mathf.Max(1, timeoutSegundos);
            while (headBreaker.Config == null && Time.unscaledTime < limite) yield return null;

            Kiosko.ConfigHeadBreaker cfg = headBreaker.Config;
            if (cfg == null || cfg.dificultades == null || cfg.imagenes == null || cfg.imagenes.Length == 0)
                yield break;

            Kiosko.DificultadHB dificultad = null;
            foreach (Kiosko.DificultadHB d in cfg.dificultades)
                if (d.piezas_por_lado == GridSize) { dificultad = d; break; }
            if (dificultad == null)
            {
                Debug.LogWarning($"[PuzzleGenerator] La base no tiene una dificultad de {GridSize}x{GridSize}.", this);
                yield break;
            }

            List<string> ids = new List<string>();
            foreach (Kiosko.ImagenHB im in cfg.imagenes) ids.Add(im.id_imagen.ToString());
            string elegido = ElegirSinRepetir(ids);
            Kiosko.ImagenHB imagen = null;
            foreach (Kiosko.ImagenHB im in cfg.imagenes)
                if (im.id_imagen.ToString() == elegido) { imagen = im; break; }

            bool termino = false;
            headBreaker.IniciarRompecabezas(dificultad.id_dificultad, imagen.id_imagen,
                (tex, _) =>
                {
                    tex.name = "kiosko_" + imagen.id_imagen;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    adquirida = tex;
                    adquiridaEsRuntime = true;
                    adquiridaEtiqueta = imagen.titulo;
                    termino = true;
                },
                _ => termino = true);

            limite = Time.unscaledTime + Mathf.Max(1, timeoutSegundos) + 15f;
            while (!termino && Time.unscaledTime < limite) yield return null;
            if (!termino) headBreaker.CancelarPartida();
        }

        private void AdquirirLocal()
        {
            if (imageCount <= 0)
            {
                Debug.LogError("[PuzzleGenerator] imageCount es 0. Ajustalo en el Inspector.", this);
                return;
            }

            string[] candidatos = new string[imageCount];
            for (int i = 0; i < imageCount; i++) candidatos[i] = RutaLocal(i + 1);

            string elegido = ElegirSinRepetir(new List<string>(candidatos));
            Texture2D tex = Resources.Load<Texture2D>(elegido);

            if (tex == null)
            {
                Debug.LogError($"[PuzzleGenerator] No se encontro 'Resources/{elegido}'. Revisa el nombre y el padding.", this);
                return;
            }

            adquirida = tex;
            adquiridaEsRuntime = false;
            adquiridaEtiqueta = elegido;
        }

        private IEnumerator AdquirirRemota()
        {
            using (UnityWebRequest req = UnityWebRequest.Get(urlListaImagenes))
            {
                req.timeout = Mathf.Max(1, timeoutSegundos);
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[PuzzleGenerator] No pude leer la lista de imagenes: {req.error}", this);
                }
                else
                {
                    List<string> nuevas = ParsearLista(req.downloadHandler.text);
                    if (nuevas.Count > 0) urlsRemotas = nuevas;
                    else Debug.LogWarning("[PuzzleGenerator] La lista de imagenes llego vacia o con formato inesperado.", this);
                }
            }

            if (urlsRemotas == null || urlsRemotas.Count == 0) yield break;

            string url = ElegirSinRepetir(urlsRemotas);
            using (UnityWebRequest tr = UnityWebRequestTexture.GetTexture(url))
            {
                tr.timeout = Mathf.Max(1, timeoutSegundos);
                yield return tr.SendWebRequest();

                if (tr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[PuzzleGenerator] No pude descargar '{url}': {tr.error}", this);
                    yield break;
                }

                Texture2D tex = DownloadHandlerTexture.GetContent(tr);
                tex.name = "kiosko_" + Mathf.Abs(url.GetHashCode());
                tex.wrapMode = TextureWrapMode.Clamp;

                adquirida = tex;
                adquiridaEsRuntime = true;
                adquiridaEtiqueta = url;
            }
        }

        private List<string> ParsearLista(string json)
        {
            List<string> res = new List<string>();
            if (string.IsNullOrWhiteSpace(json)) return res;

            string t = json.TrimStart();
            if (t.StartsWith("[")) json = "{\"items\":" + json + "}";

            ListaRemota lista;
            try { lista = JsonUtility.FromJson<ListaRemota>(json); }
            catch (Exception e)
            {
                Debug.LogWarning("[PuzzleGenerator] JSON de imagenes invalido: " + e.Message, this);
                return res;
            }
            if (lista == null || lista.items == null) return res;

            Uri baseUri = null;
            try { baseUri = new Uri(urlListaImagenes); } catch (Exception) { }

            foreach (ItemRemoto it in lista.items)
            {
                if (it == null || string.IsNullOrWhiteSpace(it.url)) continue;
                string u = it.url.Trim();
                if (!u.StartsWith("http", StringComparison.OrdinalIgnoreCase) && baseUri != null)
                {
                    try { u = new Uri(baseUri, u).ToString(); } catch (Exception) { continue; }
                }
                res.Add(u);
            }
            return res;
        }

        private string ElegirSinRepetir(List<string> opciones)
        {
            int maxEvitar = Mathf.Clamp(avoidRepeatLast, 0, Mathf.Max(0, opciones.Count - 1));

            string elegido;
            int guardia = 0;
            do
            {
                elegido = opciones[UnityEngine.Random.Range(0, opciones.Count)];
                guardia++;
            }
            while (recientes.Contains(elegido) && guardia < 200);

            recientes.Enqueue(elegido);
            while (recientes.Count > maxEvitar) recientes.Dequeue();
            return elegido;
        }

        private string RutaLocal(int index)
        {
            string carpeta = string.IsNullOrEmpty(resourcesSubfolder) ? "" : resourcesSubfolder.TrimEnd('/') + "/";
            return carpeta + filePrefix + index.ToString().PadLeft(numberPadding, '0');
        }

        private void ConstruirTablero(int n, PiezasRompecabezas jig)
        {
            if (slotsRoot == null || piecesRoot == null) return;

            float celda = boardSize / n;
            float paso = celda;
            float lado = celda * jig.proporcion;
            float pasoBandeja = traySize / n;
            float mitad = (n - 1) * 0.5f;

            List<Vector2> bandeja = new List<Vector2>(n * n);
            for (int f = 0; f < n; f++)
                for (int c = 0; c < n; c++)
                    bandeja.Add(trayCenter + new Vector2((c - mitad) * pasoBandeja, (mitad - f) * pasoBandeja));
            PuzzleSplitter.Revolver(bandeja);

            RectTransform[] huecos = new RectTransform[n * n];
            for (int f = 0; f < n; f++)
            {
                for (int c = 0; c < n; c++)
                {
                    int i = f * n + c;
                    Vector2 pos = boardCenter + new Vector2((c - mitad) * paso, (mitad - f) * paso);
                    huecos[i] = CrearHueco(i, pos, lado, jig.siluetas[i]);
                }
            }

            for (int i = 0; i < n * n; i++)
                CrearPieza(i, bandeja[i], lado, celda, jig.piezas[i], huecos[i]);
        }

        private RectTransform CrearHueco(int i, Vector2 pos, float lado, Sprite silueta)
        {
            GameObject go = new GameObject("Slot_" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            Colocar(rt, slotsRoot, pos, lado);

            Image img = go.GetComponent<Image>();
            img.sprite = silueta;
            img.type = Image.Type.Simple;
            img.color = new Color(1f, 1f, 1f, 0.40f);
            img.enabled = mostrarContornoHuecos;
            img.raycastTarget = false;
            return rt;
        }

        private void CrearPieza(int i, Vector2 posInicial, float lado, float celda, Sprite sprite, RectTransform hueco)
        {
            GameObject go = new GameObject("Pieza_" + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            Colocar(rt, piecesRoot, posInicial, lado);

            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = true;
            img.alphaHitTestMinimumThreshold = 0.2f;

            PuzzlePiece pp = go.AddComponent<PuzzlePiece>();
            pp.targetSlot = hueco;
            pp.snapDistance = celda * snapFraction;

            if (GameManager.Instance != null) GameManager.Instance.RegisterPiece(pp);
        }

        private static void Colocar(RectTransform rt, RectTransform padre, Vector2 pos, float lado)
        {
            rt.SetParent(padre, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(lado, lado);
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one;
        }

        private void CapturarEstiloDeHueco()
        {
            if (slotsRoot != null && slotsRoot.childCount > 0)
            {
                Image modelo = slotsRoot.GetChild(0).GetComponent<Image>();
                if (modelo != null)
                {
                    if (slotSprite == null) slotSprite = modelo.sprite;
                    colorHueco = modelo.color;
                    tipoHueco = modelo.type;
                    return;
                }
            }
            if (slotSprite != null) tipoHueco = Image.Type.Sliced;
        }

        private static void VaciarContenedor(RectTransform raiz)
        {
            if (raiz == null) return;
            for (int i = raiz.childCount - 1; i >= 0; i--)
            {
                Transform hijo = raiz.GetChild(i);
                hijo.gameObject.SetActive(false);
                hijo.SetParent(null, false);
                Destroy(hijo.gameObject);
            }
        }

        private static RectTransform BuscarPorNombre(string nombre)
        {
            GameObject go = GameObject.Find(nombre);
            return go != null ? go.GetComponent<RectTransform>() : null;
        }
    }
}
