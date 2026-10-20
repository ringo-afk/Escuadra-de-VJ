using UnityEngine;

public class Damage : MonoBehaviour
{
    [Header("Daño")]
    public int herida = 1;

    private void OnCollisionEnter2D(Collision2D jugador)
    {
        HacerDanio(jugador.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D jugador)
    {
        HacerDanio(jugador.gameObject);
    }

    void HacerDanio(GameObject objeto)
    {
        if (objeto.CompareTag("Player"))
        {
            PlayerController player = objeto.GetComponent<PlayerController>();

            if (player != null)
            {
                player.RecibirDanio(herida);
            }
        }
    }
}