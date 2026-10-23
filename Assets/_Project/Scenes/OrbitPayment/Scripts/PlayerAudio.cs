using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Sonidos de coleccionables")]
    public AudioClip sonidoMoneda;
    public AudioClip sonidoDocumento;

    [Header("Daño y Game Over")]
    public AudioClip sonidoDanio;
    public AudioClip sonidoGameOver;

    [Header("Quiz")]
    public AudioClip sonidoCorrecto;
    public AudioClip sonidoIncorrecto;

    [Header("Portal y victoria")]
    public AudioClip sonidoPortal;
    public AudioClip sonidoVictoria;

    [Header("Interfaz")]
    public AudioClip sonidoBoton;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void ReproducirMoneda()
    {
        if (audioSource != null && sonidoMoneda != null)
        {
            audioSource.PlayOneShot(sonidoMoneda);
        }
    }

    public void ReproducirDocumento()
    {
        if (audioSource != null && sonidoDocumento != null)
        {
            audioSource.PlayOneShot(sonidoDocumento);
        }
    }

    public void ReproducirDanio()
    {
        Reproducir(sonidoDanio);
    }

    public void ReproducirGameOver()
    {
        Reproducir(sonidoGameOver);
    }

    public void ReproducirCorrecto()
    {
        Reproducir(sonidoCorrecto);
    }

    public void ReproducirIncorrecto()
    {
        Reproducir(sonidoIncorrecto);
    }

    public void ReproducirPortal()
    {
        Reproducir(sonidoPortal);
    }

    public void ReproducirVictoria()
    {
        Reproducir(sonidoVictoria);
    }

    public void ReproducirBoton()
    {
        Reproducir(sonidoBoton);
    }

    private void Reproducir(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}