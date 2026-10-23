using System.Collections;
using UnityEngine;

public class Buzon : MonoBehaviour
{
    public CategoriaCarpeta miCategoria;

    Vector3 escalaOriginal;
    Coroutine rebote;

    void Awake() => escalaOriginal = transform.localScale;

    
    public void AnimarRecepcion()
    {
        if (rebote != null) StopCoroutine(rebote);
        transform.localScale = escalaOriginal;
        rebote = StartCoroutine(Rebote());
    }

    IEnumerator Rebote()
    {
        float dur = 0.3f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            // encoge un poco y se estabiliza
            float k = 1f + 0.25f * Mathf.Sin(p * Mathf.PI * 2f) * (1f - p);
            transform.localScale = escalaOriginal * k;
            yield return null;
        }
        transform.localScale = escalaOriginal;
        rebote = null;
    }
}