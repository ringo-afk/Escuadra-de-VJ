
using UnityEngine;

public class PortalUnlock : MonoBehaviour
{
    public GameObject portal;
    public int preguntasNecesarias = 1;

    private int preguntasCompletadas = 0;
    private bool desbloqueado = false;

    [Header("Sonido del portal")]
    public AudioSource audioSource;
    public AudioClip sonidoPortal;

    void Start()
    {
        if (portal != null)
        {
            portal.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No asignaste el Portal en PortalUnlock.");
        }
    }

    public void PreguntaCompletada()
    {
        if (desbloqueado)
            return;

        preguntasCompletadas++;

        if (preguntasCompletadas >= preguntasNecesarias)
        {
            desbloqueado = true;

            if (portal != null)
            {
                portal.SetActive(true);

                if (audioSource != null && sonidoPortal != null)
                {
                    audioSource.PlayOneShot(sonidoPortal);
                }

                Debug.Log("¡Portal desbloqueado!");
            }
            else
            {
                Debug.LogError("No se puede desbloquear el portal: falta asignarlo.");
            }
        }
    }
}
