using UnityEngine;

public class SD_InsectEnemyAttack : MonoBehaviour
{
    public float damage = 25f;

    private void OnTriggerEnter2D(Collider2D jugador)
    {
        if(jugador.gameObject.tag == "Player" && jugador.gameObject.GetComponent<SD_Player>().alive)
        {
            jugador.gameObject.GetComponent<SD_Player>().energia -= damage;
        }
    }
}