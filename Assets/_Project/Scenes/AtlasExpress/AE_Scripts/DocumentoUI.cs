using System.Collections;
using UnityEngine;
using TMPro;

public class DocumentoUI : MonoBehaviour
{
    public GameObject panelPapel;
    public TextMeshProUGUI textoNombre;
    public TextMeshProUGUI textoTipo;

    [Header("Audio")]
    public AudioSource audioSource;   
    public AudioClip clipAbrir, clipCerrar;

    private Coroutine popup;

    public void MostrarDocumento(DocumentoData datos)
    {
        textoNombre.text = "Nombre: " + datos.nombrePasajero;
        textoTipo.text = "Documento: " + datos.tipoDocumento;

        panelPapel.SetActive(true);
        if (audioSource && clipAbrir) audioSource.PlayOneShot(clipAbrir);

        if (popup != null) StopCoroutine(popup);
        popup = StartCoroutine(PopUp());
    }

    public void CerrarDocumento()
    {
        if (popup != null) StopCoroutine(popup);
        panelPapel.transform.localScale = Vector3.one;
        panelPapel.SetActive(false);
        if (audioSource && clipCerrar) audioSource.PlayOneShot(clipCerrar);
    }

    private IEnumerator PopUp()
    {
        Transform tr = panelPapel.transform;
        float dur = 0.2f, t = 0f;
        tr.localScale = Vector3.one * 0.7f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float e = EaseOutBack(Mathf.Clamp01(t / dur));
            tr.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, e);
            yield return null;
        }
        tr.localScale = Vector3.one;
        popup = null;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
    }
}