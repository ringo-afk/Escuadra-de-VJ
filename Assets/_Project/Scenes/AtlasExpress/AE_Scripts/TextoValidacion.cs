using UnityEngine;
using TMPro;
using System.Collections;

public class ValidadorFeedback : MonoBehaviour
{
    [Header("UI y Textos")]
    public TextMeshProUGUI textoValidacion;

    [Header("Audios")]
    public AudioSource audioSource;
    public AudioClip clipAcierto; 
    public AudioClip clipError;   

    public AudioClip clipAgarrar, clipSoltar;
    public void SonidoAgarrar() { if (audioSource && clipAgarrar) audioSource.PlayOneShot(clipAgarrar); }
    public void SonidoSoltar() { if (audioSource && clipSoltar) audioSource.PlayOneShot(clipSoltar); }
    public void MostrarFeedback(bool esCorrecto)
    {
        StopAllCoroutines(); 
        StartCoroutine(RutinaFeedback(esCorrecto));
    }

    private IEnumerator RutinaFeedback(bool esCorrecto)
    {
        if (textoValidacion != null)
        {
            if (esCorrecto)
            {
                textoValidacion.text = "¡CORRECTO!";
                textoValidacion.color = Color.green;
                if (audioSource != null && clipAcierto != null)
                    audioSource.PlayOneShot(clipAcierto);
            }
            else
            {
                textoValidacion.text = "¡INCORRECTO!";
                textoValidacion.color = Color.red;
                if (audioSource != null && clipError != null)
                    audioSource.PlayOneShot(clipError);
            }

            textoValidacion.gameObject.SetActive(true);

            
            yield return new WaitForSeconds(1.2f);

            textoValidacion.gameObject.SetActive(false);
        }
    }
}