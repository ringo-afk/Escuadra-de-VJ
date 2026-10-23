using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelOpciones : MonoBehaviour
{
    [SerializeField] private Slider sliderMusica;
    [SerializeField] private Slider sliderEfectos;
    [SerializeField] private Button botonSilencio;
    [SerializeField] private TMP_Text textoSilencio;

    private void Awake()
    {
        sliderMusica.onValueChanged.AddListener(CambiarMusica);
        sliderEfectos.onValueChanged.AddListener(CambiarEfectos);
        botonSilencio.onClick.AddListener(CambiarSilencio);
    }

    private void OnEnable()
    {
        sliderMusica.SetValueWithoutNotify(OpcionesAudio.VolumenMusica);
        sliderEfectos.SetValueWithoutNotify(OpcionesAudio.VolumenEfectos);
        ActualizarTexto();
    }

    // se guarda al cerrar y no en cada movimiento del slider, en Web guardar es lento
    private void OnDisable()
    {
        OpcionesAudio.Guardar();
    }

    private void CambiarMusica(float valor)
    {
        OpcionesAudio.VolumenMusica = valor;
    }

    private void CambiarEfectos(float valor)
    {
        OpcionesAudio.VolumenEfectos = valor;
    }

    private void CambiarSilencio()
    {
        OpcionesAudio.Silencio = !OpcionesAudio.Silencio;
        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        textoSilencio.text = OpcionesAudio.Silencio ? "Activar sonido" : "Silenciar";
    }
}
