using UnityEngine;
using UnityEngine.UI;
namespace Headbreaker
{
    public class MusicaFondo : MonoBehaviour
    {
        private const string ClaveVolumen = "hb_volumen";
        private const string ClaveSilencio = "hb_silencio";

        private AudioSource fuente;
        private Slider barra;
        private Image iconoBoton;
        private Sprite iconoMute;
        private Sprite iconoBajo;
        private Sprite iconoAlto;
        private float volumen = 0.4f;
        private bool silencio;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (FindAnyObjectByType<MusicaFondo>() != null) return;
            GameObject go = new GameObject("MusicaFondo");
            go.AddComponent<MusicaFondo>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            volumen = PlayerPrefs.GetFloat(ClaveVolumen, 0.4f);
            silencio = PlayerPrefs.GetInt(ClaveSilencio, 0) == 1;

            fuente = gameObject.AddComponent<AudioSource>();
            fuente.clip = Resources.Load<AudioClip>("Audio/ambiente");
            fuente.loop = true;
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;

            ConstruirPanel();
            Aplicar();

            if (fuente.clip != null) fuente.Play();
            else Debug.LogWarning("[MusicaFondo] No encontre Resources/Audio/ambiente.");
        }

        private void ConstruirPanel()
        {
            iconoMute = CrearIcono(0, true);
            iconoBajo = CrearIcono(1, false);
            iconoAlto = CrearIcono(2, false);
            Sprite pildora = CrearRedondeado(64, 32, true);
            Sprite circulo = CrearRedondeado(64, 32, false);

            GameObject goCanvas = new GameObject("PanelMusica", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            goCanvas.transform.SetParent(transform, false);
            Canvas canvas = goCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler cs = goCanvas.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.5f;

            RectTransform panel = Rect("Panel", goCanvas.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                                       new Vector2(-28f, 28f), new Vector2(290f, 72f));
            Image fondo = panel.gameObject.AddComponent<Image>();
            fondo.sprite = pildora;
            fondo.type = Image.Type.Sliced;
            fondo.color = new Color(0.04f, 0.07f, 0.18f, 0.72f);

            RectTransform btn = Rect("Icono", panel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                                     new Vector2(40f, 0f), new Vector2(52f, 52f));
            Image fondoBtn = btn.gameObject.AddComponent<Image>();
            fondoBtn.sprite = circulo;
            fondoBtn.color = new Color(1f, 1f, 1f, 0.14f);
            Button boton = btn.gameObject.AddComponent<Button>();
            boton.targetGraphic = fondoBtn;
            boton.onClick.AddListener(AlternarSilencio);

            RectTransform ic = Rect("Dibujo", btn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                    Vector2.zero, new Vector2(34f, 34f));
            iconoBoton = ic.gameObject.AddComponent<Image>();
            iconoBoton.raycastTarget = false;
            iconoBoton.color = Color.white;

            RectTransform sl = Rect("Volumen", panel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                    new Vector2(82f, 0f), new Vector2(188f, 36f));
            barra = sl.gameObject.AddComponent<Slider>();
            barra.minValue = 0f;
            barra.maxValue = 1f;
            barra.direction = Slider.Direction.LeftToRight;

            RectTransform bg = Rect("Fondo", sl, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 10f));
            Image imgBg = bg.gameObject.AddComponent<Image>();
            imgBg.sprite = pildora;
            imgBg.type = Image.Type.Sliced;
            imgBg.color = new Color(1f, 1f, 1f, 0.22f);

            RectTransform areaRelleno = Rect("AreaRelleno", sl, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-28f, 10f));
            RectTransform relleno = Rect("Relleno", areaRelleno, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image imgRelleno = relleno.gameObject.AddComponent<Image>();
            imgRelleno.sprite = pildora;
            imgRelleno.type = Image.Type.Sliced;
            imgRelleno.color = new Color(0.96f, 0.66f, 0.16f, 1f);

            RectTransform areaMango = Rect("AreaMango", sl, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-28f, 28f));
            RectTransform mango = Rect("Mango", areaMango, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 0f));
            Image imgMango = mango.gameObject.AddComponent<Image>();
            imgMango.sprite = circulo;
            imgMango.color = Color.white;

            barra.fillRect = relleno;
            barra.handleRect = mango;
            barra.targetGraphic = imgMango;
            barra.onValueChanged.AddListener(AlMoverBarra);
        }

        private static RectTransform Rect(string nombre, Transform padre, Vector2 aMin, Vector2 aMax, Vector2 pivote, Vector2 pos, Vector2 tam)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(padre, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivote;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tam;
            return rt;
        }

        private void AlMoverBarra(float v)
        {
            volumen = Mathf.Clamp01(v);
            silencio = volumen <= 0.001f;
            Aplicar(false);
        }

        private void AlternarSilencio()
        {
            silencio = !silencio;
            if (!silencio && volumen <= 0.001f) volumen = 0.4f;
            Aplicar();
        }

        private void Aplicar(bool actualizarBarra = true)
        {
            fuente.volume = silencio ? 0f : volumen;
            if (actualizarBarra && barra != null) barra.SetValueWithoutNotify(silencio ? 0f : volumen);
            if (iconoBoton != null)
                iconoBoton.sprite = silencio ? iconoMute : (volumen < 0.5f ? iconoBajo : iconoAlto);
            PlayerPrefs.SetFloat(ClaveVolumen, volumen);
            PlayerPrefs.SetInt(ClaveSilencio, silencio ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static Sprite CrearRedondeado(int tam, int radio, bool conBorde)
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

        private static Sprite CrearIcono(int ondas, bool mute)
        {
            const int tam = 96;
            const int ss = 3;
            Color32[] pix = new Color32[tam * tam];
            for (int py = 0; py < tam; py++)
            {
                for (int px = 0; px < tam; px++)
                {
                    int dentro = 0;
                    for (int sy = 0; sy < ss; sy++)
                    {
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float x = px + (sx + 0.5f) / ss - tam * 0.5f;
                            float y = py + (sy + 0.5f) / ss - tam * 0.5f;
                            if (PuntoDentro(x, y, ondas, mute)) dentro++;
                        }
                    }
                    pix[py * tam + px] = new Color32(255, 255, 255, (byte)(255 * dentro / (ss * ss)));
                }
            }
            Texture2D t = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            t.SetPixels32(pix);
            t.Apply(false, true);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            return Sprite.Create(t, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static bool PuntoDentro(float x, float y, int ondas, bool mute)
        {
            if (x >= -36f && x <= -18f && Mathf.Abs(y) <= 13f) return true;

            if (x > -18f && x <= 2f)
            {
                float t = (x + 18f) / 20f;
                float medio = 13f + t * 20f;
                if (Mathf.Abs(y) <= medio) return true;
            }

            if (mute)
            {
                float cx = x - 22f;
                float a = Mathf.Abs(cx - y);
                float b = Mathf.Abs(cx + y);
                if (Mathf.Abs(cx) <= 17f && Mathf.Abs(y) <= 17f && (a <= 4.2f || b <= 4.2f)) return true;
                return false;
            }

            float dx = x - 2f;
            float r = Mathf.Sqrt(dx * dx + y * y);
            if (dx > 0f && Mathf.Abs(y) <= dx * 1.2f)
            {
                if (ondas >= 1 && r >= 15f && r <= 21f) return true;
                if (ondas >= 2 && r >= 28f && r <= 34f) return true;
            }
            return false;
        }
    }
}
