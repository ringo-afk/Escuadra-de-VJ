using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform jugador;


    public float suavidad = 5f;
    public float offsetX = 2f;

    private float posicionY;
    private float posicionZ;
    private bool seguirJugador = true;

    private bool mostrarCentro = false;
    private Vector3 posicionCentro;
    public void RegresarAlCentro(Vector3 destino)
    {
        mostrarCentro = true;

        posicionCentro = new Vector3(
            destino.x,
            destino.y,
            transform.position.z
        );
    }
        public void CentrarCamara()
    {
        seguirJugador = false;

        transform.position = new Vector3(
            0f,
            0f,
            transform.position.z
        );
    }
    void Start()
    {
        posicionY = transform.position.y;
        posicionZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (!seguirJugador) return;
        if (mostrarCentro)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                posicionCentro,
                15f * Time.unscaledDeltaTime
            );

            return;
        }
        if (jugador == null)
            return;
       

        Vector3 posicionDeseada = new Vector3(
            jugador.position.x + offsetX,
            posicionY,
            posicionZ
        );

        transform.position = Vector3.Lerp(
            transform.position,
            posicionDeseada,
            suavidad * Time.deltaTime
        );
    }
}