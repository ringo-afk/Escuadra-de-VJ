using UnityEngine;

public class SD_LaserBullet : MonoBehaviour
{
    public float lifetime = 2f;
    public float damage = 20f;

    private bool spent;

    public void Launch(Vector2 direction, float speed)
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.linearVelocity = direction * speed;

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (spent)
            return;

        SD_AlienEnemy enemy =
            other.GetComponentInParent<SD_AlienEnemy>();

        bool hitBorder =
            other.gameObject.layer == LayerMask.NameToLayer("Border");

        if (enemy == null && !hitBorder)
            return;

        spent = true;

        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}