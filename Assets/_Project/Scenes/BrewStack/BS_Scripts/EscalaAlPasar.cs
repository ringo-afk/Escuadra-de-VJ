using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EscalaAlPasar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float escalaEncima = 1.06f;
    [SerializeField] private float velocidad = 12f;

    private Vector3 escalaNormal;
    private Selectable selectable;
    private bool encima;

    private void Awake()
    {
        escalaNormal = transform.localScale;
        selectable = GetComponent<Selectable>();
    }

    private void OnDisable()
    {
        // si el panel se oculta con el mouse encima, que no se quede grande
        encima = false;
        transform.localScale = escalaNormal;
    }

    private void Update()
    {
        bool crecer = encima && (selectable == null || selectable.interactable);
        Vector3 objetivo = crecer ? escalaNormal * escalaEncima : escalaNormal;
        // con unscaled también funciona en la pausa
        transform.localScale = Vector3.Lerp(transform.localScale, objetivo, velocidad * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        encima = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        encima = false;
    }
}
