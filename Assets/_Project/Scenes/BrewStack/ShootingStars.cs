using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Genera estrellas fugaces periódicas: aparecen en un punto aleatorio,
/// cruzan la pantalla en diagonal con un fundido de entrada/salida, y se destruyen.
/// Vive como componente en un GameObject dentro del mismo espacio que StarField.
/// </summary>
public class ShootingStars : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform container; // el RectTransform de Background
    [SerializeField] private Sprite streakSprite;      // estrella_fugaz.png importado como Sprite

    [Header("Frecuencia")]
    [SerializeField] private float intervaloMinimo = 3f;
    [SerializeField] private float intervaloMaximo = 8f;

    [Header("Apariencia")]
    [SerializeField] private float largoMinimo = 100f;
    [SerializeField] private float largoMaximo = 180f;
    [SerializeField] private float grosor = 3f;
    [SerializeField] private Color color = new Color(0.9f, 0.95f, 1f);

    [Header("Movimiento")]
    [SerializeField] private float duracionMinima = 0.5f;
    [SerializeField] private float duracionMaxima = 0.9f;
    [SerializeField] private float distanciaRecorrida = 500f;

    private void Start()
    {
        StartCoroutine(GenerarEstrellasFugacesLoop());
    }

    private IEnumerator GenerarEstrellasFugacesLoop()
    {
        while (true)
        {
            float espera = Random.Range(intervaloMinimo, intervaloMaximo);
            yield return new WaitForSeconds(espera);
            StartCoroutine(AnimarEstrellaFugaz());
        }
    }

    private IEnumerator AnimarEstrellaFugaz()
    {
        // Punto de inicio: en el borde superior o izquierdo, aleatorio
        Rect bounds = container.rect;
        float startX = Random.Range(bounds.xMin, bounds.xMax * 0.5f);
        float startY = Random.Range(bounds.yMax * 0.3f, bounds.yMax);
        Vector2 start = new Vector2(startX, startY);

        // Dirección: siempre hacia abajo-derecha, con algo de variación
        float angulo = Random.Range(-35f, -20f); // grados, negativo = hacia abajo
        Vector2 direccion = new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
        Vector2 end = start + direccion * distanciaRecorrida;

        float largo = Random.Range(largoMinimo, largoMaximo);
        float duracion = Random.Range(duracionMinima, duracionMaxima);

        GameObject obj = new GameObject("ShootingStar", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(transform, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.pivot = new Vector2(1f, 0.5f); // la cabeza de la estela es el punto de referencia
        rect.sizeDelta = new Vector2(largo, grosor);
        rect.anchoredPosition = start;

        float anguloRotacion = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        rect.localEulerAngles = new Vector3(0f, 0f, anguloRotacion);

        Image img = obj.GetComponent<Image>();
        img.sprite = streakSprite;
        img.raycastTarget = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            rect.anchoredPosition = Vector2.Lerp(start, end, t);

            // Fundido: entra rápido, sale suave
            float alpha = t < 0.15f
                ? Mathf.InverseLerp(0f, 0.15f, t)
                : Mathf.InverseLerp(1f, 0.5f, t);

            Color c = color;
            c.a = Mathf.Clamp01(alpha);
            img.color = c;

            yield return null;
        }

        Destroy(obj);
    }
}
