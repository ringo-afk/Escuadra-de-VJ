using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuizStepDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int ordenCorrecto;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private Canvas canvas;

    private Transform contenedor;
    private RectTransform marcador;
    private Vector3 desfase;
    private bool arrastrando;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        canvasRect = canvas.GetComponent<RectTransform>();
    }

    void OnDisable()
    {
        if (arrastrando)
            Terminar();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (arrastrando || eventData.button != PointerEventData.InputButton.Left)
            return;

        arrastrando = true;
        contenedor = transform.parent;

        Vector2 tamano = rectTransform.rect.size;
        int indice = transform.GetSiblingIndex();

        GameObject objetoMarcador = new GameObject(
            "Marcador",
            typeof(RectTransform),
            typeof(LayoutElement)
        );

        marcador = objetoMarcador.GetComponent<RectTransform>();
        marcador.SetParent(contenedor, false);

        LayoutElement espacio = objetoMarcador.GetComponent<LayoutElement>();
        espacio.preferredWidth = tamano.x;
        espacio.preferredHeight = tamano.y;
        espacio.flexibleWidth = 0f;
        espacio.flexibleHeight = 0f;

        marcador.SetSiblingIndex(indice);

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, tamano.x);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tamano.y);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;

        desfase = rectTransform.position - PuntoEnMundo(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!arrastrando)
            return;

        Vector3 posicion = PuntoEnMundo(eventData) + desfase;
        posicion.x = marcador.position.x;
        rectTransform.position = posicion;

        int nuevaPosicion = 0;

        for (int i = 0; i < contenedor.childCount; i++)
        {
            Transform hijo = contenedor.GetChild(i);

            if (hijo == marcador)
                continue;

            if (rectTransform.position.y < hijo.position.y)
                nuevaPosicion++;
        }

        if (marcador.GetSiblingIndex() != nuevaPosicion)
            marcador.SetSiblingIndex(nuevaPosicion);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (arrastrando)
            Terminar();
    }

    private void Terminar()
    {
        arrastrando = false;

        int indice = marcador.GetSiblingIndex();

        marcador.SetParent(null, false);
        Destroy(marcador.gameObject);
        marcador = null;

        transform.SetParent(contenedor, false);
        transform.SetSiblingIndex(indice);

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            contenedor.GetComponent<RectTransform>()
        );
    }

    private Vector3 PuntoEnMundo(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 punto
        );

        return punto;
    }
}
