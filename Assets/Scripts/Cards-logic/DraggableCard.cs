using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverScaleAmount = 1.1f;
    private Vector3 originalScale;

    // State tracking for when the card is dropped incorrectly
    [HideInInspector] public Transform OriginalParent;
    private int originalSiblingIndex;
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    // --- HOVER JUICE ---
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Don't pop up if we are currently dragging it
        if (eventData.dragging) return; 
        
        rectTransform.localScale = originalScale * hoverScaleAmount;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.localScale = originalScale;
    }

    // --- DRAG LOGIC ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. Save our starting location and position in the layout group
        OriginalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // 2. Pop the card out of the Hand layout so it floats freely
        // (Setting it to the root Canvas ensures it renders on top of everything else)
        transform.SetParent(transform.root);

        // 3. Turn OFF raycasts for this card. 
        // If we don't do this, the card blocks the mouse from "seeing" the drop zone underneath it!
        canvasGroup.blocksRaycasts = false;
        
        // Reset scale in case they dragged while hovered
        rectTransform.localScale = originalScale; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Lock the card's position to the mouse pointer
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 1. Turn raycasts back ON so the card can be hovered/dragged again
        canvasGroup.blocksRaycasts = true;

        // 2. Did we drop it in a valid zone? 
        // If the parent is still the root canvas, it means no DropZone intercepted it.
        if (transform.parent == transform.root)
        {
            ReturnToHand();
        }
    }

    public void ReturnToHand()
    {
        // Snap back to the original parent and original slot in the layout
        transform.SetParent(OriginalParent);
        transform.SetSiblingIndex(originalSiblingIndex);
    }
}