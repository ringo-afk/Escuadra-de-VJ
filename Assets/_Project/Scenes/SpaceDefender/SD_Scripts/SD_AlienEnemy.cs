using UnityEngine;
using UnityEngine.Events;

public class SD_AlienEnemy : MonoBehaviour
{
    public enum AlienType
    {
        Green,
        Red,
        Purple
    }

    [Header("Tipo")]
    public AlienType type;

    [Header("Referencias")]
    public SD_Player player;
    public SpriteRenderer visual;

    [Header("Estadísticas")]
    public float moveSpeed = 2f;
    public float maxHealth = 60f;
    public float contactDamage = 10f;
    public float attackInterval = 1f;

    [Header("Evento del alien morado")]
    public UnityEvent onPurpleDeath = new UnityEvent();

    private Rigidbody2D body;
    private float currentHealth;
    private float nextAttackTime;
    private bool dead;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    public void Initialize(SD_Player target)
    {
        player = target;
    }

    void FixedUpdate()
    {
        if (dead || player == null || !player.alive)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction =
            (Vector2)player.transform.position - body.position;

        body.linearVelocity = direction.normalized * moveSpeed;

        // Supone que el dibujo original mira hacia la izquierda.
        if (visual != null && Mathf.Abs(direction.x) > 0.01f)
        {
            visual.flipX = direction.x > 0f;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryAttack(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryAttack(other);
    }

    void TryAttack(Collider2D other)
    {
        if (dead || Time.time < nextAttackTime)
            return;

        SD_Player target =
            other.GetComponentInParent<SD_Player>();

        if (target == null || !target.alive)
            return;

        target.Damage(contactDamage);

        nextAttackTime =
            Time.time + Mathf.Max(0.05f, attackInterval);
    }

    public void TakeDamage(float damage)
    {
        if (dead || damage <= 0f)
            return;

        currentHealth -= damage;

        Debug.Log(
            $"{name} recibió {damage} de daño. Vida restante: {currentHealth}"
        );

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (dead)
            return;

        dead = true;

        body.linearVelocity = Vector2.zero;
        GetComponent<BoxCollider2D>().enabled = false;

        if (type == AlienType.Purple)
        {
            Debug.Log("Alien morado eliminado: solicitar trivia.");
            onPurpleDeath.Invoke();
        }

        Destroy(gameObject);
    }
}