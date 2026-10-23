using UnityEngine;
using System.Collections;

public class PortalNivel : MonoBehaviour
{
    public LevelCompletePopup popup;
    public CameraFollow camara;
    public Transform centroCamara;

    private bool activado = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (activado || !collision.CompareTag("Player"))
            return;

        PlayerController jugador =
            collision.GetComponent<PlayerController>();

        if (jugador != null && jugador.vivo)
        {
            activado = true;
            StartCoroutine(MostrarFinal());
        }
    }

    IEnumerator MostrarFinal()
{
   
    camara.CentrarCamara();

    yield return new WaitForSeconds(0.5f);

    popup.MostrarVictoria();
}
}