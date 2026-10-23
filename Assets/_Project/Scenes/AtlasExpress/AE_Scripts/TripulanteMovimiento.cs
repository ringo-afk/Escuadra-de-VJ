using UnityEngine;

public class TripulanteMovimiento : MonoBehaviour
{
    public float velocidad = 4f;

    private Transform puntoB;
    private Transform puntoC;
    private GameManager gameManager;

    private enum Estado { MoviendoAB, Esperando, MoviendoAC }
    private Estado estadoActual;

   
    public void InicializarRuta(Transform a, Transform b, Transform c, GameManager manager)
    {
        puntoB = b;
        puntoC = c;
        gameManager = manager;

        transform.position = a.position;
        estadoActual = Estado.MoviendoAB;
    }

    void Update()
    {
        if (estadoActual == Estado.MoviendoAB)
        {
            transform.position = Vector2.MoveTowards(transform.position, puntoB.position, velocidad * Time.deltaTime);

            if (Vector2.Distance(transform.position, puntoB.position) < 0.01f)
            {
                estadoActual = Estado.Esperando;
                
                gameManager.TripulanteLlegoAlMostrador();
            }
        }
        else if (estadoActual == Estado.MoviendoAC)
        {
            transform.position = Vector2.MoveTowards(transform.position, puntoC.position, velocidad * Time.deltaTime);

            if (Vector2.Distance(transform.position, puntoC.position) < 0.01f)
            {
                
                gameManager.SiguienteTurno();
                Destroy(gameObject);
            }
        }
    }

    public void DocumentoClasificado()
    {
        estadoActual = Estado.MoviendoAC;
    }
}