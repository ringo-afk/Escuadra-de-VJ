using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Headbreaker
{
    public class EfectoFuego : MonoBehaviour
    {
        public static float duracion = 0.95f;
        public static float radioCirculo = 0.6f;
        public static bool mostrarCirculo = false;
        public static TMP_FontAsset fuente;

        private static readonly Color[] COLORES =
        {
            new Color(1.00f, 0.62f, 0.15f),
            new Color(1.00f, 0.32f, 0.10f),
            new Color(0.95f, 0.20f, 0.60f),
            new Color(0.62f, 0.32f, 1.00f),
            new Color(0.25f, 0.78f, 1.00f),
        };

        public static Color ColorDeRacha(int racha) =>
            COLORES[Mathf.Clamp(racha, 1, COLORES.Length) - 1];

        private static Sprite spriteFlama, spritePunto, spriteAro;

        private RectTransform rt;
        private CanvasGroup grupo;
        private RectTransform[] flamas;
        private float[] fase;
        private Vector3[] escalaBase;
        private RectTransform[] chispas;
        private Vector2[] velChispa;
        private RectTransform texto;
        private RectTransform aro;
        private Vector2 inicio;
        private float subida;
        private float t;

        public static void Mostrar(RectTransform slot, string valor, int racha)
        {
            if (slot == null) return;
            RectTransform capa = Capa(slot);
            if (capa == null) return;
            CrearSprites();

            float lado = Mathf.Max(slot.rect.width, 40f);
            float escala = Mathf.Clamp(lado / 200f, 0.65f, 1.1f);
            float radio = lado * radioCirculo;

            float margen = Mathf.Max(0f, radio - 45f * escala);
            Vector2 centro = slot.anchoredPosition;
            Vector2 pos = centro + Random.insideUnitCircle * margen;

            GameObject go = new GameObject("Fuego " + valor, typeof(RectTransform), typeof(CanvasGroup));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(capa, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(160f, 160f) * escala;
            r.anchoredPosition = pos;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;

            EfectoFuego ef = go.AddComponent<EfectoFuego>();
            ef.Armar(r, cg, escala, racha, valor, centro - pos, radio);
        }

        private void Armar(RectTransform raiz, CanvasGroup cg, float escala, int racha, string valor,
                           Vector2 centroCirculoLocal, float radio)
        {
            rt = raiz;
            grupo = cg;
            inicio = raiz.anchoredPosition;
            subida = 55f * escala;

            Color c = ColorDeRacha(racha);
            float tamRacha = 1f + 0.08f * (Mathf.Clamp(racha, 1, 8) - 1);

            if (mostrarCirculo)
            {
                aro = CrearImagen("Circulo", spriteAro, new Color(c.r, c.g, c.b, 0.35f),
                                  new Vector2(radio * 2f, radio * 2f), centroCirculoLocal, new Vector2(0.5f, 0.5f));
            }

            Color media = Color.Lerp(c, Color.white, 0.35f);
            Color nucleo = Color.Lerp(c, Color.white, 0.8f);
            flamas = new[]
            {
                CrearImagen("FlamaExterior", spriteFlama, new Color(c.r, c.g, c.b, 0.9f),
                            new Vector2(120f, 155f) * escala * tamRacha, new Vector2(0f, -62f * escala), new Vector2(0.5f, 0f)),
                CrearImagen("FlamaMedia", spriteFlama, new Color(media.r, media.g, media.b, 0.85f),
                            new Vector2(86f, 115f) * escala * tamRacha, new Vector2(0f, -62f * escala), new Vector2(0.5f, 0f)),
                CrearImagen("Nucleo", spriteFlama, new Color(nucleo.r, nucleo.g, nucleo.b, 0.9f),
                            new Vector2(48f, 66f) * escala * tamRacha, new Vector2(0f, -62f * escala), new Vector2(0.5f, 0f)),
            };
            fase = new float[flamas.Length];
            escalaBase = new Vector3[flamas.Length];
            for (int i = 0; i < flamas.Length; i++)
            {
                fase[i] = Random.Range(0f, 10f);
                escalaBase[i] = Vector3.one;
                flamas[i].localScale = new Vector3(0.3f, 0.3f, 1f);
            }

            int nChispas = 3 + Mathf.Min(racha, 5);
            chispas = new RectTransform[nChispas];
            velChispa = new Vector2[nChispas];
            for (int i = 0; i < nChispas; i++)
            {
                float tam = Random.Range(6f, 11f) * escala;
                chispas[i] = CrearImagen("Chispa", spritePunto, Color.Lerp(c, Color.white, 0.4f),
                                         new Vector2(tam, tam),
                                         new Vector2(Random.Range(-30f, 30f), Random.Range(-40f, 0f)) * escala,
                                         new Vector2(0.5f, 0.5f));
                velChispa[i] = new Vector2(Random.Range(-25f, 25f), Random.Range(70f, 140f)) * escala;
            }

            CrearTexto("Sombra", valor, new Color(0f, 0f, 0f, 0.55f), escala, new Vector2(3f, -3f) * escala);
            texto = CrearTexto("Valor", valor, Color.white, escala, Vector2.zero);
            texto.localScale = Vector3.one * 0.6f;
        }

        private RectTransform CrearImagen(string nombre, Sprite sprite, Color color, Vector2 tam, Vector2 pos, Vector2 pivote)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(rt, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = pivote;
            r.sizeDelta = tam;
            r.anchoredPosition = pos;

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return r;
        }

        private RectTransform CrearTexto(string nombre, string valor, Color color, float escala, Vector2 desfase)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(rt, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(220f, 80f) * escala;
            r.anchoredPosition = desfase;

            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            if (fuente != null) t.font = fuente;
            t.text = valor;
            t.fontSize = 46f * escala;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.raycastTarget = false;
            return r;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duracion);

            float subeK = 1f - Mathf.Pow(1f - k, 3f);
            rt.anchoredPosition = inicio + new Vector2(0f, subida * subeK);

            float crece = Mathf.Clamp01(t / 0.12f);
            crece = 1f - Mathf.Pow(1f - crece, 2f);
            for (int i = 0; i < flamas.Length; i++)
            {
                float sx = 1f + 0.10f * Mathf.Sin(t * 23f + fase[i]);
                float sy = 1f + 0.16f * Mathf.Sin(t * 29f + fase[i] * 1.7f);
                float s = Mathf.Lerp(0.3f, 1f, crece);
                flamas[i].localScale = new Vector3(sx * s, sy * s, 1f);
                flamas[i].localRotation = Quaternion.Euler(0f, 0f, 6f * Mathf.Sin(t * 13f + fase[i]));
            }

            for (int i = 0; i < chispas.Length; i++)
            {
                chispas[i].anchoredPosition += velChispa[i] * Time.unscaledDeltaTime;
                chispas[i].localScale = Vector3.one * (1f - k * 0.7f);
            }

            float pop = t < 0.10f ? Mathf.Lerp(0.6f, 1.18f, t / 0.10f)
                      : t < 0.20f ? Mathf.Lerp(1.18f, 1f, (t - 0.10f) / 0.10f)
                      : 1f;
            texto.localScale = Vector3.one * pop;

            grupo.alpha = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;

            if (k >= 1f) Destroy(gameObject);
        }

        private static RectTransform Capa(RectTransform slot)
        {
            Transform slots = slot.parent;
            Transform board = slots != null ? slots.parent : null;
            if (board == null) return null;

            Transform existente = board.Find("EfectosPuntos");
            RectTransform capa;
            if (existente != null)
            {
                capa = (RectTransform)existente;
            }
            else
            {
                GameObject go = new GameObject("EfectosPuntos", typeof(RectTransform));
                capa = go.GetComponent<RectTransform>();
                capa.SetParent(board, false);
                capa.anchorMin = Vector2.zero;
                capa.anchorMax = Vector2.one;
                capa.offsetMin = Vector2.zero;
                capa.offsetMax = Vector2.zero;
            }
            capa.SetAsLastSibling();
            return capa;
        }

        private static float Suave(float desde, float hasta, float x)
        {
            float k = Mathf.Clamp01((x - desde) / (hasta - desde));
            return k * k * (3f - 2f * k);
        }

        private static void CrearSprites()
        {
            if (spriteFlama != null && spritePunto != null && spriteAro != null) return;

            const int W = 64, H = 96;
            Texture2D tf = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Flama" };
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;
                float hw = v < 0.38f
                    ? 0.92f * Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Pow((0.38f - v) / 0.38f, 2f)))
                    : 0.92f * Mathf.Pow(Mathf.Max(0f, 1f - (v - 0.38f) / 0.62f), 1.25f);
                for (int x = 0; x < W; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / W * 2f - 1f);
                    float a = hw > 0.001f ? Suave(1f, 0.5f, u / hw) : 0f;
                    a *= 1f - 0.5f * Suave(0.7f, 1f, v);
                    tf.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tf.Apply();
            spriteFlama = Sprite.Create(tf, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 100f);

            const int P = 32;
            Texture2D tp = new Texture2D(P, P, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Chispa" };
            for (int y = 0; y < P; y++)
                for (int x = 0; x < P; x++)
                {
                    float r = new Vector2((x + 0.5f) / P * 2f - 1f, (y + 0.5f) / P * 2f - 1f).magnitude;
                    tp.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(Suave(1f, 0f, r), 1.5f)));
                }
            tp.Apply();
            spritePunto = Sprite.Create(tp, new Rect(0, 0, P, P), new Vector2(0.5f, 0.5f), 100f);

            const int A = 128;
            Texture2D ta = new Texture2D(A, A, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Aro" };
            for (int y = 0; y < A; y++)
                for (int x = 0; x < A; x++)
                {
                    float r = new Vector2((x + 0.5f) / A * 2f - 1f, (y + 0.5f) / A * 2f - 1f).magnitude;
                    float a = Suave(0.08f, 0f, Mathf.Abs(r - 0.9f));
                    ta.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            ta.Apply();
            spriteAro = Sprite.Create(ta, new Rect(0, 0, A, A), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
