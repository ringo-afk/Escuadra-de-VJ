using UnityEngine;
using UnityEngine.InputSystem;

public class CarpetaInteractiva : MonoBehaviour
{
    private DocumentoData misDatos;
    private Vector3 posicionInicial;
    private bool fueArrastrado = false;
    private bool sonidoAgarrarReproducido = false;

    public void AsignarDatos(DocumentoData datos)
    {
        misDatos = datos;
        posicionInicial = transform.position;
    }

    private void OnMouseDown()
    {
        fueArrastrado = false;
        sonidoAgarrarReproducido = false;
    }

    private void OnMouseDrag()
    {
        Vector2 posicionMousePantalla = Mouse.current.position.ReadValue();
        Vector3 posicionMouseMundo = Camera.main.ScreenToWorldPoint(posicionMousePantalla);
        posicionMouseMundo.z = 0f;
        transform.position = posicionMouseMundo;

        if (Vector3.Distance(posicionInicial, transform.position) > 0.5f)
        {
            fueArrastrado = true;
            if (!sonidoAgarrarReproducido)
            {
                FindAnyObjectByType<ValidadorFeedback>().SonidoAgarrar();
                sonidoAgarrarReproducido = true;
            }
        }
    }

    private void OnMouseUp()
    {
        if (!fueArrastrado)
        {
            if (misDatos != null)
                FindAnyObjectByType<DocumentoUI>().MostrarDocumento(misDatos);
        }
        else
        {
            ValidarSoltado();
        }
    }

    private void ValidarSoltado()
    {
        Collider2D[] colisiones = Physics2D.OverlapPointAll(transform.position);
        bool depositado = false;

        foreach (Collider2D colision in colisiones)
        {
            Buzon buzonSoltado = colision.GetComponent<Buzon>();
            if (buzonSoltado != null)
            {
                bool fueCorrecto = (buzonSoltado.miCategoria == misDatos.carpetaCorrecta);

                buzonSoltado.AnimarRecepcion();
                FindAnyObjectByType<ValidadorFeedback>().SonidoSoltar();
                FindAnyObjectByType<ValidadorFeedback>().MostrarFeedback(fueCorrecto);
                FindAnyObjectByType<UI_Pantallas>().DocumentoEvaluado(fueCorrecto);
                FindAnyObjectByType<GameManager>().ValidarDocumento(fueCorrecto);
                Destroy(gameObject);

                depositado = true;
                break;
            }
        }

        if (!depositado)
            transform.position = posicionInicial;
    }
}