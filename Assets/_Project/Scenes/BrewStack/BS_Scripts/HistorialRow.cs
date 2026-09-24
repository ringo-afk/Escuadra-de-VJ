using UnityEngine;
using TMPro;

public class HistorialRow : MonoBehaviour
{
    [SerializeField] private TMP_Text fechaText;
    [SerializeField] private TMP_Text modoText;
    [SerializeField] private TMP_Text alturaText;
    [SerializeField] private TMP_Text puntosText;

    public void Configurar(string fecha, string modo, int altura, int puntos)
    {
        fechaText.text = fecha;
        modoText.text = modo;
        alturaText.text = altura.ToString();
        puntosText.text = puntos.ToString();
    }
}
