using UnityEngine;

public class Coin : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            CoinManager.instance.AgregarMoneda();

            Destroy(gameObject);
        }
         PlayerAudio audio = collision.GetComponent<PlayerAudio>();

        if (audio != null)
        {
            audio.ReproducirMoneda();
        }

        Destroy(gameObject);
    }
}