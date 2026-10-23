using UnityEngine;

[RequireComponent(typeof(Collider2D))] 
[RequireComponent(typeof(AudioSource))] 
public class PelucheAtlasito : MonoBehaviour
{
    private AudioSource audioSource;
    private Vector3 escalaOriginal;
    private bool estaAnimando = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        escalaOriginal = transform.localScale;
    }

    
    void OnMouseDown()
    {
        PlaySonidoYAnimacion();
    }

    public void PlaySonidoYAnimacion()
    {
        
        if (audioSource != null && audioSource.clip != null)
        {
            
            audioSource.PlayOneShot(audioSource.clip);
        }

        
        if (!estaAnimando)
        {
            StartCoroutine(EfectoAplastado());
        }
    }

    private System.Collections.IEnumerator EfectoAplastado()
    {
        estaAnimando = true;

        
        Vector3 escalaAplastado = new Vector3(escalaOriginal.x * 1.15f, escalaOriginal.y * 0.75f, escalaOriginal.z);
        float tiempo = 0f;

        while (tiempo < 0.08f)
        {
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaAplastado, tiempo / 0.08f);
            tiempo += Time.deltaTime;
            yield return null;
        }

        tiempo = 0f;
        
        while (tiempo < 0.15f)
        {
            transform.localScale = Vector3.Lerp(escalaAplastado, escalaOriginal, tiempo / 0.15f);
            tiempo += Time.deltaTime;
            yield return null;
        }

        transform.localScale = escalaOriginal;
        estaAnimando = false;
    }
}