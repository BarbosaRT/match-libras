using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

public class DragDrop : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    public Image imageTipo;
    private Vector3 originalScale;
    public float scaleMultiplier = 1.2f;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 lastMouseLocalPos;

    [Header("Tipo da Peca")]
    public TipoPeca tipoPeca;

    [HideInInspector] public ValorNumero valorNumero;
    [HideInInspector] public ValorComida valorComida;

    public int Valor => tipoPeca == TipoPeca.Numero
        ? (int)valorNumero
        : (int)valorComida;

    [field: SerializeField] public List<Sprite> NumeroSprites { get; private set; }
    [field: SerializeField] public List<Sprite> ComidasSprites { get; private set; }

    public Vector2 PosicaoOriginal { get; private set; }
    private Transform parentOriginal;
    private bool foiTratadoNoDrop;
    private bool foiAceitoNoSlot;
    // Variáveis para gerenciar o Canvas interno e partículas
    private Canvas canvasInterno;
    private int ordemOriginalCanvasInterno;
    private ParticleSystemRenderer dustRenderer;
    private int ordemOriginalDust;
    [Header("Sombra")]
    public string nomeSombra = "Sombra";
    public Image imagemSombra;
    private float alphaOriginalSombra;
    public ParticleSystem particulasAcerto;

    private Coroutine escalaCoroutine;

    public void DefinirPosicaoOriginal()
    {
        PosicaoOriginal = rectTransform.anchoredPosition;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2)
        {
            var slotAtual = GetComponentInParent<ItemSlot>();

            // SE A PEÇA JÁ ESTÁ NO SLOT: Remove e devolve para a base (origem)
            if (slotAtual != null)
            {
                slotAtual.RemoverDoSlot(this);
                SoundManager.Instance?.Play("Drag_1");

                // Aproveitamos a sua função que já faz a animação suave e pausa a física!
                VoltarParaOrigem(0.2f, this);

                return; // Interrompe aqui para ela não tentar ir para outro slot
            }

            // SE A PEÇA NÃO ESTÁ NO SLOT: Procura um slot válido e envia para lá
            ItemSlot[] slots = FindObjectsByType<ItemSlot>(FindObjectsSortMode.None);

            foreach (var slot in slots)
            {
                if (slot.PodeAceitarPeca(this))
                {
                    SoundManager.Instance?.Play("Drag_1");
                    parentOriginal = transform.parent;
                    slot.AutoEncaixarPeca(this);
                    break;
                }
            }
        }
    }
    // Chamado por LevelManager ao spawnar a peca, garante que parentOriginal
    // esta correto mesmo antes do primeiro drag.
    public void DefinirParentOriginal(Transform parent)
    {
        parentOriginal = parent;
    }

    // Chamado por ItemSlot quando REJEITA a peca.
    public void MarcarTratadoNoDrop()
    {
        foiTratadoNoDrop = true;
    }

    // Chamado por ItemSlot quando ACEITA a peca.
    public void MarcarAceitoNoSlot()
    {
        foiTratadoNoDrop = true;
        foiAceitoNoSlot = true;
    }

    // Anima a peca de volta para PosicaoOriginal dentro do parentOriginal.
    // Pausa a fisica durante a animacao e a retoma ao terminar.
    public void VoltarParaOrigem(float duracao, MonoBehaviour runner)
    {
        // Reparenta para o container correto (containerPecas), nao para o canvas raiz.
        // PosicaoOriginal esta em espaco de containerPecas, entao o parent precisa ser o mesmo.
        Transform alvo = parentOriginal != null
            ? parentOriginal
            : runner.GetComponentInParent<Canvas>().transform;

        transform.SetParent(alvo, true);
        GetComponent<CanvasGroup>().blocksRaycasts = false;

        // Pausa fisica para nao conflitar com a animacao de volta.
        GetComponent<PecaFisica>()?.PausarFisica();

        runner.StartCoroutine(AnimarVolta(duracao));
    }

    private IEnumerator AnimarVolta(float duracao)
    {
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

        // Retoma fisica sem impulso — peca chegou na origem, fica quieta.
        GetComponent<PecaFisica>()?.RetomarFisica(Vector2.zero);
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
        originalScale = rectTransform.localScale;
        var sombraTransform = transform.Find(nomeSombra);
        if (sombraTransform != null)
        {
            imagemSombra = sombraTransform.GetComponent<Image>();
            alphaOriginalSombra = imagemSombra.color.a;
        }
    }

    public void AplicarSprite()
    {
        if (tipoPeca == TipoPeca.Numero)
            imageTipo.sprite = NumeroSprites[(int)valorNumero];
        else
            imageTipo.sprite = ComidasSprites[(int)valorComida];
    }

    public void OnPointerDown(PointerEventData eventData) { }

    public void OnBeginDrag(PointerEventData eventData)
    {
        var slot = GetComponentInParent<ItemSlot>();
        if (slot != null)
            slot.RemoverDoSlot(this);

        parentOriginal = transform.parent;
        foiTratadoNoDrop = false;
        foiAceitoNoSlot = false;

        if (escalaCoroutine != null) StopCoroutine(escalaCoroutine);
        escalaCoroutine = StartCoroutine(AnimarEscalaESombra(originalScale * scaleMultiplier, 1f, 0.15f));

        canvasGroup.blocksRaycasts = false;
        GetComponent<PecaFisica>()?.PausarFisica();
        SoundManager.Instance?.Play("Drag_1");

        rectTransform.SetAsLastSibling();
        rectTransform.anchoredPosition3D = new Vector3(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y, 0f);

        // 1. CANVAS RAIZ (Afeta a Sombra)
        Canvas canvasLocal = GetComponent<Canvas>();
        if (canvasLocal == null) canvasLocal = gameObject.AddComponent<Canvas>();
        canvasLocal.overrideSorting = true;
        canvasLocal.sortingOrder = 4; // Sombra fica no 990

        // 2. CANVAS INTERNO (Afeta Base e Tipo)
        canvasInterno = transform.Find("Canvas")?.GetComponent<Canvas>();
        if (canvasInterno != null)
        {
            ordemOriginalCanvasInterno = canvasInterno.sortingOrder;
            canvasInterno.sortingOrder = 5; // 995 é maior que 990, então fica ACIMA da sombra
        }

        // 3. PARTÍCULAS (Dust)
        Transform dustTransform = transform.Find("Dust");
        if (dustTransform != null)
        {
            dustRenderer = dustTransform.GetComponent<ParticleSystemRenderer>();
            if (dustRenderer != null)
            {
                ordemOriginalDust = dustRenderer.sortingOrder;
                dustRenderer.sortingOrder = 985; // 985 é menor que 990, então fica ABAIXO da sombra
            }
        }

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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            canvas.worldCamera,
            out Vector2 currentMouseLocalPos
        );

        Vector2 delta = currentMouseLocalPos - lastMouseLocalPos;
        rectTransform.anchoredPosition += delta;
        lastMouseLocalPos = currentMouseLocalPos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (escalaCoroutine != null) StopCoroutine(escalaCoroutine);
        escalaCoroutine = StartCoroutine(AnimarEscalaESombra(originalScale, alphaOriginalSombra, 0.15f));
        canvasGroup.blocksRaycasts = true;

        rectTransform.SetAsLastSibling();
        rectTransform.anchoredPosition3D = new Vector3(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y, 0f);

        // REMOVE O CANVAS TEMPORÁRIO DA RAIZ
        Canvas canvasLocal = GetComponent<Canvas>();
        if (canvasLocal != null)
        {
            Destroy(canvasLocal);
        }

        // RESTAURA O CANVAS INTERNO
        if (canvasInterno != null)
        {
            canvasInterno.sortingOrder = ordemOriginalCanvasInterno;
        }

        // RESTAURA AS PARTÍCULAS
        if (dustRenderer != null)
        {
            dustRenderer.sortingOrder = ordemOriginalDust;
        }

        if (!foiTratadoNoDrop)
        {
            Transform alvo = parentOriginal != null ? parentOriginal : GetComponentInParent<Canvas>().transform;
            transform.SetParent(alvo, true);
            DefinirPosicaoOriginal();
            GetComponent<PecaFisica>()?.RetomarFisica(eventData.delta * 3f);
        }
        else if (foiAceitoNoSlot)
        {
            GetComponent<PecaFisica>()?.RetomarFisica(eventData.delta * 3f);

            if (particulasAcerto != null)
                particulasAcerto.Play();
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

    public IEnumerator AnimarRotacao(Quaternion alvo, float duracao)
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

    private float EaseInOut(float t) => t * t * (3f - 2f * t);
}