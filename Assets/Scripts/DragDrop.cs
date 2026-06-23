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

        // REMOVIDO: O trecho que mudava o parent para o rootCanvas.
        // A peça precisa continuar no 'containerPecas' para que a física das 
        // outras peças consiga calcular a distância corretamente usando anchoredPosition.

        if (escalaCoroutine != null) StopCoroutine(escalaCoroutine);
        escalaCoroutine = StartCoroutine(AnimarEscalaESombra(originalScale * scaleMultiplier, 1f, 0.15f));

        canvasGroup.blocksRaycasts = false;

        // A peça arrastada ainda pausa a PRÓPRIA física para seguir o mouse sem tremer.
        GetComponent<PecaFisica>()?.PausarFisica();

        // MODIFICADO: Trocamos SetAsLastSibling() por SetAsFirstSibling().
        // Isso coloca o objeto no topo da hierarquia, o que na Unity UI significa
        // que ele será renderizado PRIMEIRO, ficando sob (por baixo de) todas as outras peças.
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

        // CORREÇÃO: Assim que o jogador solta a peça, jogamos ela para o final da hierarquia (Last Sibling).
        // Isso faz com que ela volte a ser renderizada por CIMA dos fundos e outros elementos,
        // liberando o Raycast para que ela possa ser clicada e movida novamente.
        rectTransform.SetAsLastSibling();

        if (!foiTratadoNoDrop)
        {
            // 1. Em vez de voltar para a origem, reparenta para o container original (onde a física atua)
            Transform alvo = parentOriginal != null ? parentOriginal : GetComponentInParent<Canvas>().transform;
            transform.SetParent(alvo, true);

            // 2. Define o local atual como a nova posição original para evitar saltos indesejados
            DefinirPosicaoOriginal();

            // 3. Retoma a física aplicando a força do movimento do mouse (impulso)
            GetComponent<PecaFisica>()?.RetomarFisica(eventData.delta * 3f);
        }
        else if (foiAceitoNoSlot)
        {
            // Foi aceito pelo slot — retoma fisica com impulso.
            GetComponent<PecaFisica>()?.RetomarFisica(eventData.delta * 3f);

            if (particulasAcerto != null)
                particulasAcerto.Play();
        }
        // Caso rejeitado, o ItemSlot já chamou VoltarParaOrigem.
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

    private float EaseInOut(float t) => t * t * (3f - 2f * t);
}