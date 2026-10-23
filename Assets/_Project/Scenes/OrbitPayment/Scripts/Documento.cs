using UnityEngine;

public class Documento : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            DocumentManager.instance.AgregarDocumento();

            Destroy(gameObject);
        }
        PlayerAudio audio = collision.GetComponent<PlayerAudio>();

        if (audio != null)
        {
            audio.ReproducirDocumento();
        }
    }
}