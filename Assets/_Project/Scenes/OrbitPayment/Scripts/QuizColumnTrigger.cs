using UnityEngine;

public class QuizColumnTrigger : MonoBehaviour
{
    public QuizManager quizManager;
    public int idPregunta = 1;

    private bool completado = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController jugador =
            collision.GetComponentInParent<PlayerController>();

        if (completado || jugador == null || !jugador.vivo)
            return;

        quizManager.AbrirQuiz(idPregunta, this);
    }

    public void Completar()
    {
        completado = true;
    }
}