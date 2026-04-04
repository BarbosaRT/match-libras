using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

public class DragDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private Image imageTipo;
    private Vector3 originalScale;
    public float scaleMultiplier = 1.2f;

    private Canvas canvas; // usa o canvas.scaleFactor caso precise
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 lastMouseLocalPos;
    [Header("Tipo da Peça")]
    public TipoPeca tipoPeca;

    [HideInInspector] public ValorNumero valorNumero;
    [HideInInspector] public ValorComida valorComida;

    public int Valor => tipoPeca == TipoPeca.Numero
        ? (int)valorNumero
        : (int)valorComida;

    [field: SerializeField] public List<Sprite> NumeroSprites { get; private set; }
    [field: SerializeField] public List<Sprite> ComidasSprites { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        imageTipo = transform.Find("Tipo").GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        originalScale = rectTransform.localScale;
    }

    // Chame este método após definir tipoPeca e valorNumero/valorComida
    public void AplicarSprite()
    {
        if (tipoPeca == TipoPeca.Numero)
            imageTipo.sprite = NumeroSprites[(int)valorNumero];
        else
            imageTipo.sprite = ComidasSprites[(int)valorComida];
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //Debug.Log("Clicked");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //Debug.Log("Start Drag");
        //col.enabled = false;
        rectTransform.localScale = originalScale * scaleMultiplier;
        canvasGroup.blocksRaycasts = false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            canvas.worldCamera,
            out lastMouseLocalPos
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Converte a posição atual do mouse para o espaço local do canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            canvas.worldCamera,
            out Vector2 currentMouseLocalPos
        );

        // Delta real no espaço do canvas
        Vector2 delta = currentMouseLocalPos - lastMouseLocalPos;
        rectTransform.anchoredPosition += delta;

        lastMouseLocalPos = currentMouseLocalPos;
        //rectTransform.anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Debug.Log("End Drag");
        canvasGroup.blocksRaycasts = true;
        rectTransform.localScale = originalScale;
    }
}