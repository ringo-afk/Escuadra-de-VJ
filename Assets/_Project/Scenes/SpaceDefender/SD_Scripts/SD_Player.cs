using UnityEngine;

public class SD_Player : MonoBehaviour
{
    Animator anim;
    Rigidbody2D body;
    public float vel = 5f;
    bool lookRight = true;
    AudioSource sfx;
    public AudioClip[] soundEffects;
    public float energia = 100f;
    public float energiaMax = 100f;    
    public bool alive;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        alive = true;
        anim = GetComponent<Animator>();
        sfx = GetComponent<AudioSource>();
        body = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if(alive)
        {
            // Movement
            float movX = Input.GetAxisRaw("Horizontal");
            float movY = Input.GetAxisRaw("Vertical");
            body.linearVelocity = new Vector2(movX*vel, movY*vel);
            bool moving = movX != 0 || movY != 0;
            anim.SetBool("MOVING", moving);

            if(lookRight && movX < 0)
            {
                Invertir();
            }
            if(!lookRight && movX > 0)
            {
                Invertir();
            }

            if(energia<=0 && alive)
            {
                energia = 0;
                alive = false;
                //anim.Play("JugadorMuerte");
            }

        }
    }
    
    void Invertir()
    {
        lookRight = !lookRight;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    public void Damage(float cantidad)
    {
        if (!alive)
            return;

        energia -= cantidad;

        if (energia < 0)
            energia = 0;

        Debug.Log("Jugador recibió daño. Energía: " + energia);

        if (energia <= 0)
        {
            alive = false;
            body.linearVelocity = Vector2.zero;
            // anim.Play("JugadorMuerte");
        }
    }

}
