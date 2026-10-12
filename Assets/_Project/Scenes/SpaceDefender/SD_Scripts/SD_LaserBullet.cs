using UnityEngine;

public class SD_LaserBullet : MonoBehaviour
{
    public float lifetime = 2f;

    public void Launch(Vector2 direction, float speed)
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        body.linearVelocity = direction * speed;

        Destroy(gameObject, lifetime);
    }
}