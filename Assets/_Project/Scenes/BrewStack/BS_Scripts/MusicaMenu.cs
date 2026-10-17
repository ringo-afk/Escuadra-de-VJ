using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicaMenu : MonoBehaviour
{
    [SerializeField] private AudioSource fuente;
    [Range(0f, 1f)]
    [SerializeField] private float volumenMusica = 0.1f;
    [SerializeField] private float velocidadCambio = 0.1f;

    private bool saliendo;

    private void Start()
    {
        // empieza en cero y sube sola en Update, así no entra de golpe
        fuente.volume = 0f;
        fuente.loop = true;
        if (fuente.clip != null) fuente.Play();
    }

    private void Update()
    {
        float objetivo = saliendo ? 0f : volumenMusica * OpcionesAudio.Musica;
        fuente.volume = Mathf.MoveTowards(fuente.volume, objetivo, velocidadCambio * Time.unscaledDeltaTime);
    }

    public void SalirA(string escena)
    {
        if (saliendo) return;
        saliendo = true;
        StartCoroutine(BajarYCargar(escena));
    }

    private IEnumerator BajarYCargar(string escena)
    {
        // con límite por si tarda, que el cambio de escena no se quede esperando
        float limite = 1f;
        while (fuente.volume > 0.001f && limite > 0f)
        {
            limite -= Time.unscaledDeltaTime;
            yield return null;
        }
        SceneManager.LoadScene(escena);
    }
}
