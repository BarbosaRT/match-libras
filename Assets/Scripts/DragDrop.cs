using UnityEngine;
using UnityEngine.EventSystems;

public class DragDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private Transform cachedTransform;
    private Transform spriteTransform;
    private Collider2D col;
    private Vector3 originalScale;
    public float scaleMultiplier = 1.2f;

    private Camera mainCamera;

    void Start() => col = GetComponent<Collider2D>();

    private void Awake()
    {
        cachedTransform = transform;
        spriteTransform = GetComponentInChildren<SpriteRenderer>().transform;

        originalScale = spriteTransform.localScale;
        mainCamera = Camera.main;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //Debug.Log("Clicked");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //Debug.Log("Start Drag");
        col.enabled = false;
        spriteTransform.localScale = originalScale * scaleMultiplier;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0f;

        cachedTransform.position = worldPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log("End Drag");
        col.enabled = true;
        spriteTransform.localScale = originalScale;
    }
}