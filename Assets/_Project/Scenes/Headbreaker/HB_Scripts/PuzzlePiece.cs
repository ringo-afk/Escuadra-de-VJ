using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Headbreaker
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class PuzzlePiece : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Objetivo")]
        [Tooltip("RectTransform del slot destino (la silueta) al que debe encajar esta pieza.")]
        public RectTransform targetSlot;

        [Header("Ajustes de Snap")]
        [Tooltip("Distancia maxima (en unidades del Canvas / pixeles de la resolucion de referencia) para que la pieza encaje al soltarla.")]
        public float snapDistance = 80f;

        [Tooltip("Duracion en segundos de la animacion de encaje.")]
        public float snapDuration = 0.12f;

        [Tooltip("Duracion en segundos del regreso a la posicion inicial.")]
        public float returnDuration = 0.25f;

        [Header("Feedback visual (opcional)")]
        [Tooltip("Escala aplicada mientras se arrastra la pieza.")]
        public float dragScale = 1.08f;

        [Tooltip("Opacidad de la pieza mientras se arrastra (1 = opaca).")]
        [Range(0.1f, 1f)] public float dragAlpha = 0.9f;

        private RectTransform rect;
        private RectTransform parentRect;      
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private Camera uiCamera;               

        private Vector2 startAnchoredPos;     
        private int startSiblingIndex;
        private Vector2 pointerOffset;        
        private Coroutine moveRoutine;

        private bool isLocked = false;
        public bool IsLocked => isLocked;

        public Vector2 StartPosition => startAnchoredPos;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            parentRect = rect.parent as RectTransform;

            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;

            uiCamera = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                       ? rootCanvas.worldCamera
                       : null;

            startAnchoredPos = rect.anchoredPosition;
            startSiblingIndex = rect.GetSiblingIndex();
        }

        void Start()
        {
            if (targetSlot == null)
                Debug.LogError($"[PuzzlePiece] '{name}' no tiene asignado targetSlot en el Inspector.", this);

            if (GameManager.Instance != null)
                GameManager.Instance.RegisterPiece(this);
        }

    
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isLocked) return;

            if (moveRoutine != null) { StopCoroutine(moveRoutine); moveRoutine = null; }

            rect.SetAsLastSibling();

            Vector2 localPointer;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, uiCamera, out localPointer))
            {
                pointerOffset = localPointer - rect.anchoredPosition;
            }
            else
            {
                pointerOffset = Vector2.zero;
            }

            canvasGroup.alpha = dragAlpha;
            canvasGroup.blocksRaycasts = false;   
            rect.localScale = Vector3.one * dragScale;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isLocked) return;

            Vector2 localPointer;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect, eventData.position, uiCamera, out localPointer))
            {
                rect.anchoredPosition = localPointer - pointerOffset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isLocked) return;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            rect.localScale = Vector3.one;

            if (targetSlot == null) { ReturnToStart(); return; }

            Vector2 targetPos = GetTargetAnchoredPosition();
            float distance = Vector2.Distance(rect.anchoredPosition, targetPos);

            if (distance <= snapDistance)
            {
                Snap(targetPos);
            }
            else
            {
                if (GameManager.Instance != null) GameManager.Instance.OnPieceMissed(this);
                ReturnToStart();
            }
        }

 
        private Vector2 GetTargetAnchoredPosition()
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, targetSlot.position);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, screenPoint, uiCamera, out local);
            return local;
        }

        private void Snap(Vector2 targetPos)
        {
            isLocked = true;
            canvasGroup.blocksRaycasts = false;  
            rect.SetSiblingIndex(startSiblingIndex);

            moveRoutine = StartCoroutine(MoveTo(targetPos, snapDuration, () =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.OnPiecePlaced(this);
            }));
        }

        private void ReturnToStart()
        {
            rect.SetSiblingIndex(startSiblingIndex);
            moveRoutine = StartCoroutine(MoveTo(startAnchoredPos, returnDuration, null));
        }

        private IEnumerator MoveTo(Vector2 destination, float duration, System.Action onComplete)
        {
            Vector2 origin = rect.anchoredPosition;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                k = 1f - Mathf.Pow(1f - k, 3f);       
                rect.anchoredPosition = Vector2.Lerp(origin, destination, k);
                yield return null;
            }

            rect.anchoredPosition = destination;
            moveRoutine = null;
            onComplete?.Invoke();
        }

   
        public void SetStartPosition(Vector2 nuevaPosicion, bool moverAhora = true)
        {
            startAnchoredPos = nuevaPosicion;

            if (moverAhora && !isLocked)
            {
                if (moveRoutine != null) { StopCoroutine(moveRoutine); moveRoutine = null; }
                rect.anchoredPosition = startAnchoredPos;
            }
        }

        public void ResetPiece()
        {
            if (moveRoutine != null) { StopCoroutine(moveRoutine); moveRoutine = null; }

            isLocked = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = startAnchoredPos;
            rect.SetSiblingIndex(startSiblingIndex);
        }

        void OnDrawGizmosSelected()
        {
            if (targetSlot == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetSlot.position, snapDistance * targetSlot.lossyScale.x);
        }
    }
}
