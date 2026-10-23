using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class ControladorVolumen : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider sliderVolumen;

    void Start()
    {

        float volumenActual = PlayerPrefs.GetFloat("VolumenJuego", 0.1f);
        sliderVolumen.value = volumenActual;
        CambiarVolumen(volumenActual);
    }

    public void CambiarVolumen(float valor)
    {
       
        float decibeles = Mathf.Log10(Mathf.Clamp(valor, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat("Volume", decibeles);

        
        PlayerPrefs.SetFloat("VolumenJuego", valor);
        PlayerPrefs.Save();
    }
}