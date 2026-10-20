using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class FondoDiagonal : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Hacia donde se mueve el patron (x derecha, y arriba). (1,1) = diagonal hacia arriba a la derecha.")]
    public Vector2 direccion = new Vector2(1f, 1f);

    [Tooltip("Velocidad en unidades del Canvas por segundo (el mosaico mide 136).")]
    public float velocidad = 22f;

    [Header("Apariencia")]
    [Tooltip("Cuanto se ve la reticula. 1 = igual que la imagen original.")]
    [Range(0f, 2f)] public float intensidadPatron = 1f;

    private const int PERIODO = 136;
    private const int SUBPIXELES = 2;

    private static Texture2D texPatron;
    private static Texture2D texBrillo;

    private RawImage capaPatron;
    private RectTransform rectPatron;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAgregar()
    {
        foreach (string nombre in new[] { "Fondo", "MenuPanel" })
        {
            GameObject go = BuscarEnCanvas(nombre);
            if (go != null && go.GetComponent<FondoDiagonal>() == null)
                go.AddComponent<FondoDiagonal>();
        }
    }

    private static GameObject BuscarEnCanvas(string nombre)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas c in canvases)
        {
            Transform[] todos = c.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in todos)
                if (t.name == nombre) return t.gameObject;
        }
        return null;
    }

    void Awake()
    {
        Texture2D brillo = ObtenerBrillo();
        if (brillo == null)
        {
            Debug.LogWarning("[FondoDiagonal] No encontre Resources/UI/fondo_glow.bytes; " +
                             "el fondo se queda estatico.", this);
            enabled = false;
            return;
        }

        Image original = GetComponent<Image>();
        if (original != null)
        {
            if (original.raycastTarget)
            {
                original.sprite = null;
                original.color = new Color(0f, 0f, 0f, 0f);
                original.canvasRenderer.cullTransparentMesh = false;
            }
            else
            {
                original.enabled = false;
            }
        }

        RawImage capaBrillo = NuevaCapa("Brillo", brillo, 0);
        capaPatron = NuevaCapa("Patron", ObtenerPatron(), 1);
        capaPatron.color = new Color(1f, 1f, 1f, Mathf.Clamp01(intensidadPatron));
        rectPatron = capaPatron.rectTransform;

        ActualizarPatron();
    }

    void Update()
    {
        ActualizarPatron();
    }

    private RawImage NuevaCapa(string nombre, Texture tex, int indiceHermano)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetSiblingIndex(indiceHermano);

        RawImage ri = go.GetComponent<RawImage>();
        ri.texture = tex;
        ri.raycastTarget = false;
        return ri;
    }

    private void ActualizarPatron()
    {
        if (capaPatron == null) return;

        Rect r = rectPatron.rect;
        float w = Mathf.Max(1f, r.width) / PERIODO;
        float h = Mathf.Max(1f, r.height) / PERIODO;

        Vector2 dir = direccion.sqrMagnitude > 0.0001f ? direccion.normalized : Vector2.zero;
        float t = Time.unscaledTime * velocidad / PERIODO;
        float ox = Mathf.Repeat(-dir.x * t, 1f);
        float oy = Mathf.Repeat(-dir.y * t, 1f);

        capaPatron.uvRect = new Rect(ox, oy, w, h);
    }

    private static Texture2D ObtenerBrillo()
    {
        if (texBrillo != null) return texBrillo;

        TextAsset bytes = Resources.Load<TextAsset>("UI/fondo_glow");
        if (bytes == null) return null;

        Texture2D t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!t.LoadImage(bytes.bytes, false)) { Object.Destroy(t); return null; }
        t.name = "fondo_glow_runtime";
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        texBrillo = t;
        return texBrillo;
    }

    private static Texture2D ObtenerPatron()
    {
        if (texPatron != null) return texPatron;

        int S = PERIODO * SUBPIXELES;
        Color32[] px = new Color32[S * S];

        Color cLinea = new Color(72f / 255f, 115f / 255f, 185f / 255f);
        Color cPunto = new Color(70f / 255f, 100f / 255f, 160f / 255f);
        Color cAmbar = new Color(105f / 255f, 85f / 255f, 68f / 255f);
        const float aLinea = 0.32f, aPunto = 0.38f, aAmbar = 0.55f;
        const float grosor = 2.0f, suavizado = 0.6f;

        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / SUBPIXELES;
                float v = (y + 0.5f) / SUBPIXELES;

                float d1 = Mathf.Abs(Envolver(u + v - 68f)) * 0.70710678f;
                float d2 = Mathf.Abs(Envolver(u - v - 68f)) * 0.70710678f;
                float cobLinea = Mathf.Max(Cobertura(d1, grosor * 0.5f, suavizado),
                                           Cobertura(d2, grosor * 0.5f, suavizado));

                float dAzul = Mathf.Sqrt(Sq(Envolver(u - 68f)) + Sq(Envolver(v - 68f)));
                float dAmbar = Mathf.Sqrt(Sq(Envolver(u)) + Sq(Envolver(v)));

                float a1 = cobLinea * aLinea;
                float a2 = Cobertura(dAzul, 7.5f, suavizado) * aPunto;
                float a3 = Cobertura(dAmbar, 6.0f, suavizado) * aAmbar;

                float aTotal = 1f - (1f - a1) * (1f - a2) * (1f - a3);
                float suma = a1 + a2 + a3;
                Color c = suma > 1e-5f
                    ? (cLinea * a1 + cPunto * a2 + cAmbar * a3) / suma
                    : cLinea;

                px[y * S + x] = new Color32(
                    (byte)Mathf.RoundToInt(c.r * 255f),
                    (byte)Mathf.RoundToInt(c.g * 255f),
                    (byte)Mathf.RoundToInt(c.b * 255f),
                    (byte)Mathf.RoundToInt(aTotal * 255f));
            }
        }

        Texture2D t = new Texture2D(S, S, TextureFormat.RGBA32, false);
        t.name = "patron_rombos_runtime";
        t.SetPixels32(px);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        t.Apply(false, true);
        texPatron = t;
        return texPatron;
    }

    private static float Envolver(float v)
    {
        return Mathf.Repeat(v + PERIODO * 0.5f, PERIODO) - PERIODO * 0.5f;
    }

    private static float Sq(float v) { return v * v; }

    private static float Cobertura(float distancia, float radio, float suave)
    {
        return Mathf.Clamp01((radio - distancia) / suave + 0.5f);
    }
}
