using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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
    // adicione esse campo
    public Vector2 PosicaoOriginal { get; private set; }
    [Header("Sombra")]
    public string nomeSombra = "Sombra"; // nome do filho Image de sombra
    public Image imagemSombra;
    private float alphaOriginalSombra;

    private Coroutine escalaCoroutine;

    public void DefinirPosicaoOriginal()
    {
        PosicaoOriginal = rectTransform.anchoredPosition;
    }

    public void VoltarParaOrigem(float duracao, MonoBehaviour runner)
    {
        // devolve ao canvas antes de animar
        transform.SetParent(runner.GetComponentInParent<Canvas>().transform, true);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
        runner.StartCoroutine(AnimarVolta(duracao));
    }

    private IEnumerator AnimarVolta(float duracao)
    {
        canvasGroup.blocksRaycasts = false;

        Vector2 atual = rectTransform.anchoredPosition;
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tempo / duracao);
            rectTransform.anchoredPosition = Vector2.Lerp(atual, PosicaoOriginal, t);
            yield return null;
        }

        rectTransform.anchoredPosition = PosicaoOriginal;
        canvasGroup.blocksRaycasts = true;
    }
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        imageTipo = transform.Find("Tipo").GetComponent<Image>();
        canvas = GetComponentInParent<Canvas>();
        originalScale = rectTransform.localScale;
        var sombraTransform = transform.Find(nomeSombra);
        if (sombraTransform != null)
        {
            imagemSombra = sombraTransform.GetComponent<Image>();
            alphaOriginalSombra = imagemSombra.color.a;
        }
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
        // avisa o slot pai se estiver encaixada em um
        var slot = GetComponentInParent<ItemSlot>();
        if (slot != null)
            slot.RemoverDoSlot(this);

        // reparenta pro canvas para ficar por cima de tudo
        var rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            transform.SetParent(rootCanvas.transform, true);

        if (escalaCoroutine != null) StopCoroutine(escalaCoroutine);
        escalaCoroutine = StartCoroutine(AnimarEscalaESombra(originalScale * scaleMultiplier, 1f, 0.15f));

        canvasGroup.blocksRaycasts = false;
        var fisica = GetComponent<PecaFisica>();
        if (fisica != null)
        {
            fisica.enabled = false;
        }
        rectTransform.SetAsLastSibling();
        StartCoroutine(AnimarRotacao(Quaternion.identity, 0.15f));

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
        if (escalaCoroutine != null) StopCoroutine(escalaCoroutine);
        escalaCoroutine = StartCoroutine(AnimarEscalaESombra(originalScale, alphaOriginalSombra, 0.15f));
        canvasGroup.blocksRaycasts = true;
        var fisica = GetComponent<PecaFisica>();
        if (fisica != null)
        {
            fisica.enabled = true;
            // da um leve impulso na direcao que estava arrastando ao soltar
            fisica.AplicarImpulso(eventData.delta * 3f);
        }
    }

    private IEnumerator AnimarEscalaESombra(Vector3 escalaAlvo, float alphaAlvo, float duracao)
    {
        Vector3 escalaInicial = rectTransform.localScale;
        float alphaInicial = imagemSombra != null ? imagemSombra.color.a : 0f;
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float t = EaseInOut(tempo / duracao);
            rectTransform.localScale = Vector3.Lerp(escalaInicial, escalaAlvo, t);

            if (imagemSombra != null)
            {
                Color c = imagemSombra.color;
                c.a = Mathf.Lerp(alphaInicial, alphaAlvo, t);
                imagemSombra.color = c;
            }

            yield return null;
        }

        rectTransform.localScale = escalaAlvo;
    }

    private IEnumerator AnimarRotacao(Quaternion alvo, float duracao)
    {
        Quaternion inicial = rectTransform.rotation;
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float t = EaseInOut(tempo / duracao);
            rectTransform.rotation = Quaternion.Lerp(inicial, alvo, t);
            yield return null;
        }

        rectTransform.rotation = alvo;
    }

    private float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t); // smoothstep equivalente
    }
}