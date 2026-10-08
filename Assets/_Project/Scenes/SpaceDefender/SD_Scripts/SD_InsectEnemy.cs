using UnityEngine;
using System.Collections;

public class SD_InsectEnemy : MonoBehaviour
{
    Animator anim;
    Rigidbody2D cuerpo;

    public float vel = 5f;
    bool verDerecha = true;

    // Referencia al jugador
    public SD_Player jugador;

    public bool vivo;
    public float duracionAccion = 1f;
    public float tiempoEntreAcciones = 2f;
    public GameObject hitboxAtaque;
    Coroutine comportamiento;


    void Start()
    {
        vivo = true;

        anim = GetComponent<Animator>();
        cuerpo = GetComponent<Rigidbody2D>();
        hitboxAtaque.SetActive(false);
        comportamiento = StartCoroutine(ComportamientoEnemigo());
    }


    void Update()
    {
        if (jugador == null)
            return;

        float porcentajeEnergia = jugador.energia / jugador.energiaMax;

        // 50% o menos
        if (porcentajeEnergia <= 0.5f)
        {
            DetenerEnemigo();
        }
        else
        {
            if (comportamiento == null)
            {
                comportamiento = StartCoroutine(ComportamientoEnemigo());
            }
        }
    }


    void DetenerEnemigo()
    {
        if (comportamiento != null)
        {
            StopCoroutine(comportamiento);
            comportamiento = null;
        }

        cuerpo.linearVelocity = Vector2.zero;

        anim.Play("SD_InsectEnemyIdle");
    }


    IEnumerator ComportamientoEnemigo()
    {
        while (true)
        {
            // Idle mientras espera
            anim.Play("SD_InsectEnemyIdle");
            cuerpo.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(tiempoEntreAcciones);


            // Comprobar energía antes de elegir acción
            if (jugador.energia / jugador.energiaMax <= 0.5f)
            {
                comportamiento = null;
                yield break;
            }


            int accion = Random.Range(0, 3);

            switch (accion)
            {
                case 0:
                    yield return StartCoroutine(MoverIzquierda());
                    break;

                case 1:
                    yield return StartCoroutine(MoverDerecha());
                    break;

                case 2:
                    yield return StartCoroutine(Atacar());
                    break;
            }
        }
    }


    IEnumerator MoverIzquierda()
    {
        Debug.Log("Enemigo: Mover izquierda");

        anim.Play("SD_InsectEnemyWalk");

        cuerpo.linearVelocity =
            new Vector2(-vel, 0);

        yield return new WaitForSeconds(duracionAccion);

        cuerpo.linearVelocity = Vector2.zero;
    }


    IEnumerator MoverDerecha()
    {
        Debug.Log("Enemigo: Mover derecha");

        anim.Play("SD_InsectEnemyWalk");

        cuerpo.linearVelocity =
            new Vector2(vel, 0);

        yield return new WaitForSeconds(duracionAccion);

        cuerpo.linearVelocity = Vector2.zero;
    }


    IEnumerator Atacar()
    {
        Debug.Log("Enemigo: Ataque");

        cuerpo.linearVelocity = Vector2.zero;

        anim.Play("SD_InsectEnemyAttack");

        // Esperar a que la animación llegue al golpe
        yield return new WaitForSeconds(0.3f);

        // Activar hitbox
        hitboxAtaque.SetActive(true);

        // El golpe solamente existe durante 0.2 segundos
        yield return new WaitForSeconds(0.2f);

        // Desactivar hitbox
        hitboxAtaque.SetActive(false);

        // Esperar a que termine la animación
        yield return new WaitForSeconds(0.5f);
    }
}