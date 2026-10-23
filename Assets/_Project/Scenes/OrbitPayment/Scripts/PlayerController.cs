using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{

    private Rigidbody2D cuerpo;
    private Animator anim;


    public float velocidad = 5f;
    private bool verDerecha = true;


    public float fuerzaSalto = 8f;
    public Transform pies;
    public float radioPies = 0.2f;
    public LayerMask esPiso;

    private bool piso;

  
    public int vida = 3;
    public bool vivo = true;
    public float limiteCaida = 20f;
    public HealthUI healthUI;
    public float tiempoInvulnerable = 1.5f;

    private bool invulnerable = false;
    private SpriteRenderer spriteRenderer;
    private Vector3 puntoRespawn;
    public GameOverManager gameOverManager;

    [Header("Sonidos del jugador")]
    public AudioSource audioSource;
    public AudioClip sonidoSalto;

    [Header("Sonidos de daño")]
    public PlayerAudio playerAudio;

    void Start()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        healthUI.ActualizarVida(vida);
        spriteRenderer = GetComponent<SpriteRenderer>();
        puntoRespawn = transform.position;

        if (playerAudio == null)
        {
            playerAudio = GetComponent<PlayerAudio>();
        }

    }

    void Update()
    {
         if (!vivo || Time.timeScale == 0f)
        {
            cuerpo.linearVelocity = Vector2.zero;
            return;
        }
        // Red de seguridad: si cae muy por debajo del nivel (fuera de la DeathZone), cuenta como caida al vacio
        if (transform.position.y < puntoRespawn.y - limiteCaida)
        {
            CaerAlVacio();
            return;
        }

        piso = Physics2D.OverlapCircle(
            pies.position,
            radioPies,
            esPiso
        );

       
        float movX = Input.GetAxisRaw("Horizontal");

        cuerpo.linearVelocity = new Vector2(
            movX * velocidad,
            cuerpo.linearVelocity.y
        );

       
        if (Input.GetKeyDown(KeyCode.Space) && piso)
        {
            cuerpo.linearVelocity = new Vector2(
                cuerpo.linearVelocity.x,
                fuerzaSalto
            );
             if (audioSource != null && sonidoSalto != null)
            {
                audioSource.PlayOneShot(sonidoSalto);
            }
        }

       
        anim.SetFloat("Velocidad", Mathf.Abs(movX));
        anim.SetBool("Piso", piso);
        anim.SetFloat("VelocidadY", cuerpo.linearVelocity.y);

        if (movX < 0 && verDerecha)
        {
            Invertir();
        }
        else if (movX > 0 && !verDerecha)
        {
            Invertir();
        }
    }

    void Invertir()
    {
        verDerecha = !verDerecha;

        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    
    void OnDrawGizmosSelected()
    {
        if (pies != null)
        {
            Gizmos.DrawWireSphere(pies.position, radioPies);
        }
    }


    public void RecibirDanio(int cantidad)
        {
            if (!vivo || invulnerable)
                return;

            vida -= cantidad;

            if (vida < 0)
                vida = 0;

            healthUI.ActualizarVida(vida);

            Debug.Log("Vida restante: " + vida);

            if (vida > 0 && playerAudio != null)
            {
                playerAudio.ReproducirDanio();
            }
  
            if (vida <= 0)
            {
                vivo = false;

                cuerpo.linearVelocity = Vector2.zero;
                Debug.Log("GAME OVER - Sin corazones");

                gameOverManager.MostrarGameOver();

                return;
            }
                invulnerable = true;
                StartCoroutine(Parpadear());
            }
    IEnumerator Parpadear()
    {
        float tiempo = 0f;

        while (tiempo < tiempoInvulnerable)
        {
            spriteRenderer.enabled = false;

            yield return new WaitForSeconds(0.1f);

            spriteRenderer.enabled = true;

            yield return new WaitForSeconds(0.1f);

            tiempo += 0.2f;
        }

        spriteRenderer.enabled = true;
        invulnerable = false;
    }
    void Respawn()
{
   
    transform.position = puntoRespawn;
    cuerpo.linearVelocity = Vector2.zero;

   
    vida = 3;
    vivo = true;

    
    healthUI.ActualizarVida(vida);

    spriteRenderer.enabled = true;

    invulnerable = false;

    
    anim.SetFloat("Velocidad", 0);
    anim.SetFloat("VelocidadY", 0);
    

    Debug.Log("Jugador reapareció");
}
public void CaerAlVacio()
    {
        if (!vivo)
            return;

        
        vida -= 1;

        if (vida < 0)
            vida = 0;

        healthUI.ActualizarVida(vida);

        transform.position = puntoRespawn;
        cuerpo.linearVelocity = Vector2.zero;

        
        if (vida <= 0)
        {
            vivo = false;

            cuerpo.linearVelocity = Vector2.zero;
            Debug.Log("GAME OVER - Caída al vacío");

            gameOverManager.MostrarGameOver();
        }

        else
        {
            if (playerAudio != null)
            {
                playerAudio.ReproducirDanio();
            }
            
            StopAllCoroutines();
            spriteRenderer.enabled = true;
            invulnerable = true;
            StartCoroutine(Parpadear());
        }
    }
}
