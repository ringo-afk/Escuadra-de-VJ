using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarField : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform container; 
    [SerializeField] private Sprite starSprite;       

    [Header("Configuración")]
    [SerializeField] private int cantidadEstrellas = 25;
    [SerializeField] private float tamañoMinimo = 1.5f;
    [SerializeField] private float tamañoMaximo = 3.5f;
    [SerializeField] private Color colorBlanco = Color.white;
    [SerializeField] private Color colorCeleste = new Color(0.62f, 0.85f, 1f);
    [Range(0f, 1f)]
    [SerializeField] private float probabilidadCeleste = 0.35f;

    [Header("Titileo")]
    [SerializeField] private bool animarTitileo = true;
    [SerializeField] private float velocidadTitileoMin = 0.5f;
    [SerializeField] private float velocidadTitileoMax = 1.5f;
    [Range(0f, 1f)]
    [SerializeField] private float alphaMinimo = 0.25f;

    private struct EstrellaAnimada
    {
        public Image imagen;
        public float fase;
        public float velocidad;
        public Color colorBase;
    }

    private readonly List<EstrellaAnimada> estrellasAnimadas = new List<EstrellaAnimada>();

    private void Start()
    {
        GenerarEstrellas();
    }

    public void GenerarEstrellas()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        estrellasAnimadas.Clear();

        Rect bounds = container.rect;

        for (int i = 0; i < cantidadEstrellas; i++)
        {
            GameObject star = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
            star.transform.SetParent(transform, false);

            RectTransform rect = star.GetComponent<RectTransform>();
            float size = Random.Range(tamañoMinimo, tamañoMaximo);
            rect.sizeDelta = new Vector2(size, size);
            
            float x = Random.Range(bounds.xMin, bounds.xMax);
            float y = Random.Range(bounds.yMin, bounds.yMax);
            rect.anchoredPosition = new Vector2(x, y);

            Image img = star.GetComponent<Image>();
            img.sprite = starSprite;
            Color colorBase = Random.value < probabilidadCeleste ? colorCeleste : colorBlanco;
            img.color = colorBase;
            img.raycastTarget = false; 

            if (animarTitileo)
            {
                estrellasAnimadas.Add(new EstrellaAnimada
                {
                    imagen = img,
                    fase = Random.Range(0f, Mathf.PI * 2f),
                    velocidad = Random.Range(velocidadTitileoMin, velocidadTitileoMax),
                    colorBase = colorBase
                });
            }
        }
    }

    private void Update()
    {
        if (!animarTitileo) return;

        for (int i = 0; i < estrellasAnimadas.Count; i++)
        {
            EstrellaAnimada e = estrellasAnimadas[i];
            if (e.imagen == null) continue;

            float t = (Mathf.Sin(Time.time * e.velocidad + e.fase) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(alphaMinimo, 1f, t);

            Color c = e.colorBase;
            c.a = alpha;
            e.imagen.color = c;
        }
    }
}
