using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class BotonAnimado : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public AudioSource fuente;     
    public AudioClip clipClic;      
    Vector3 original;
    Coroutine anim;

    void Awake() => original = transform.localScale;
    void OnDisable() => transform.localScale = original;

    public void OnPointerEnter(PointerEventData e) => IrA(1.08f);
    public void OnPointerExit(PointerEventData e) => IrA(1f);
    public void OnPointerUp(PointerEventData e) => IrA(1.08f);
    public void OnPointerDown(PointerEventData e)
    {
        IrA(0.92f);
        if (fuente && clipClic) fuente.PlayOneShot(clipClic);
    }

    void IrA(float factor)
    {
        if (!gameObject.activeInHierarchy) return;
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Escalar(original * factor));
    }

    IEnumerator Escalar(Vector3 destino)
    {
        Vector3 inicio = transform.localScale;
        float t = 0f, dur = 0.1f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(inicio, destino, t / dur);
            yield return null;
        }
        transform.localScale = destino;
    }
}