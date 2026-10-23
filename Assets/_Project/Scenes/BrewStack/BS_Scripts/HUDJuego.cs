using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDJuego : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Torre torre;
    [SerializeField] private TMP_Text textoPuntos;
    [SerializeField] private TMP_Text textoAltura;
    [SerializeField] private TMP_Text textoTiempo;
    [SerializeField] private RectTransform barraEstabilidad;
    [SerializeField] private Image imagenEstabilidad;

    [Header("Barra de estabilidad")]
    [SerializeField] private Color colorEstable = new Color(0.62f, 0.85f, 1f);
    [SerializeField] private Color colorRiesgo = new Color(0.88f, 0.59f, 0.23f);
    [SerializeField] private Color colorPeligro = new Color(0.93f, 0.3f, 0.3f);
    [Range(0f, 1f)]
    [SerializeField] private float limiteRiesgo = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float limitePeligro = 0.3f;
    [SerializeField] private float velocidadBarra = 5f;

    [Header("Pop del puntaje")]
    [SerializeField] private float escalaPop = 1.3f;
    [SerializeField] private float duracionPop = 0.2f;

    [Header("Nivel")]
    [SerializeField] private TMP_Text textoNivel;
    [SerializeField] private TMP_Text textoAvisoNivel;
    [SerializeField] private float duracionAviso = 1.8f;

    private float estabilidadMostrada = 1f;
    private int puntosAnteriores;
    private float tiempoPop = 1f;
    private float tiempoAviso = 10f;

    private void Update()
    {
        textoAltura.text = $"Altura: {torre.Altura}";

        estabilidadMostrada = Mathf.Lerp(estabilidadMostrada, torre.Estabilidad, velocidadBarra * Time.deltaTime);
        barraEstabilidad.anchorMax = new Vector2(estabilidadMostrada, 1f);

        if (torre.Estabilidad <= limitePeligro) imagenEstabilidad.color = colorPeligro;
        else if (torre.Estabilidad <= limiteRiesgo) imagenEstabilidad.color = colorRiesgo;
        else imagenEstabilidad.color = colorEstable;

        if (tiempoPop < duracionPop)
        {
            tiempoPop += Time.deltaTime;
            float avance = Mathf.Clamp01(tiempoPop / duracionPop);
            float escala = 1f + (escalaPop - 1f) * Mathf.Sin(avance * Mathf.PI);
            textoPuntos.rectTransform.localScale = new Vector3(escala, escala, 1f);
        }

        if (textoAvisoNivel != null && tiempoAviso < duracionAviso)
        {
            tiempoAviso += Time.deltaTime;
            float avance = Mathf.Clamp01(tiempoAviso / duracionAviso);
            // entra un poco grande y se va desvaneciendo
            float escala = 1f + 0.3f * (1f - Mathf.Min(avance * 5f, 1f));
            textoAvisoNivel.rectTransform.localScale = new Vector3(escala, escala, 1f);
            textoAvisoNivel.alpha = 1f - avance * avance;
            if (avance >= 1f) textoAvisoNivel.gameObject.SetActive(false);
        }
    }

    public void MostrarNivel(int nivel, bool avisar)
    {
        if (textoNivel != null) textoNivel.text = $"Nivel {nivel}";
        if (!avisar || textoAvisoNivel == null) return;

        textoAvisoNivel.text = $"¡Nivel {nivel}! Preguntas más difíciles";
        textoAvisoNivel.gameObject.SetActive(true);
        tiempoAviso = 0f;
    }

    public void MostrarPuntos(int puntos)
    {
        if (puntos > puntosAnteriores) tiempoPop = 0f;
        puntosAnteriores = puntos;
        textoPuntos.text = $"Puntos: {puntos}";
    }

    public void MostrarTiempo(float segundos)
    {
        int total = Mathf.CeilToInt(Mathf.Max(segundos, 0f));
        textoTiempo.text = $"Tiempo: {total / 60}:{total % 60:00}";
    }

    public void Reiniciar()
    {
        estabilidadMostrada = 1f;
        puntosAnteriores = 0;
        tiempoPop = duracionPop;
        textoPuntos.rectTransform.localScale = Vector3.one;

        tiempoAviso = duracionAviso;
        if (textoAvisoNivel != null) textoAvisoNivel.gameObject.SetActive(false);
    }
}
