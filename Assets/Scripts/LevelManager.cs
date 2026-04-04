using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [field: SerializeField] public List<Sprite> LibraSprites { get; private set; }
    public Image spriteRenderer;
    public int number;
    public Canvas canvas;
    public GameObject pecaPrefab;

    [Header("Spawn - Geral")]
    public float animacaoDuracao = 0.4f;

    [Header("Spawn - Numeros")]
    public int quantidadeDistratoresNumero = 2;
    public float spacingHorizontalNumero = 160f;
    public float targetYNumero = 200f;

    [Header("Spawn - Comidas")]
    public float spacingHorizontalComida = 160f;
    public float targetYComida = -200f;

    [Header("Caos da Animacao")]
    public float caosRotacao = 25f;       // graus max de rotacao final
    public float caosOffsetPos = 40f;     // desvio na posicao final
    public float caosVelocidade = 0.15f;  // variacao na duracao

    private ValorComida comidaCorreta;
    private List<GameObject> pecasComidaAtivas = new List<GameObject>();
    private List<GameObject> pecasNumeroAtivas = new List<GameObject>();

    [Header("Spawn - Posicionamento Livre")]
    public RectTransform areaDeSpawn;

    
    private List<Vector2> posicoesUsadas = new List<Vector2>();
    void Start() => SpawnarRodada();

    public void SpawnarRodada()
    {
        LimparPecas();
        number = Random.Range(1, 10);
        spriteRenderer.sprite = LibraSprites[number];
        comidaCorreta = (ValorComida)Random.Range(0, System.Enum.GetValues(typeof(ValorComida)).Length);

        StartCoroutine(SpawnarNumerosAnimado());
        StartCoroutine(SpawnarComidasAnimado());
    }

    // ── NUMEROS ──────────────────────────────────────────────────────

    private IEnumerator SpawnarNumerosAnimado()
    {
        List<ValorNumero> valores = GerarValoresNumero();

        for (int i = 0; i < valores.Count; i++)
        {
            var obj = SpawnarPeca(TipoPeca.Numero, valores[i], comidaCorreta);
            obj.tag = "Numero";
            pecasNumeroAtivas.Add(obj);

            Vector2 destino = GerarPosicaoLivre();
            Vector2 origem = new Vector2(destino.x + Random.Range(-150f, 150f), -Screen.height);
            StartCoroutine(AnimarCaotico(obj.GetComponent<RectTransform>(), origem, destino));
        }

        yield return null;
    }

    private List<ValorNumero> GerarValoresNumero()
    {
        var lista = new List<ValorNumero> { (ValorNumero)number };
        var disponiveis = new List<int>();

        for (int i = 0; i <= 9; i++)
            if (i != number) disponiveis.Add(i);

        Embaralhar(disponiveis);

        for (int i = 0; i < quantidadeDistratoresNumero; i++)
            lista.Add((ValorNumero)disponiveis[i]);

        Embaralhar(lista);
        return lista;
    }

    // ── COMIDAS ──────────────────────────────────────────────────────

    private IEnumerator SpawnarComidasAnimado()
    {
        List<(ValorComida tipo, bool correta)> comidas = GerarComidas();

        for (int i = 0; i < comidas.Count; i++)
        {
            var obj = SpawnarPeca(TipoPeca.Comida, ValorNumero.Zero, comidas[i].tipo);
            obj.tag = "Comida";
            pecasComidaAtivas.Add(obj);

            Vector2 destino = GerarPosicaoLivre();
            Vector2 origem = new Vector2(destino.x + Random.Range(-150f, 150f), -Screen.height);
            StartCoroutine(AnimarCaotico(obj.GetComponent<RectTransform>(), origem, destino));
        }

        yield return null;
    }

    private List<(ValorComida, bool)> GerarComidas()
    {
        var lista = new List<(ValorComida, bool)>();

        // comidas corretas
        for (int i = 0; i < number; i++)
            lista.Add((comidaCorreta, true));

        // distratores: quantidade aleatoria (1 a 4)
        int totalDistratores = Random.Range(1, 5);

        var outrostipos = new List<ValorComida>();
        foreach (ValorComida v in System.Enum.GetValues(typeof(ValorComida)))
            if (v != comidaCorreta) outrostipos.Add(v);

        for (int i = 0; i < totalDistratores; i++)
        {
            // 50% chance: mesmo tipo (quantidade errada ja garantida pelo total)
            // 50% chance: tipo diferente
            ValorComida tipo = Random.value > 0.5f
                ? comidaCorreta
                : outrostipos[Random.Range(0, outrostipos.Count)];

            lista.Add((tipo, false));
        }

        Embaralhar(lista);
        return lista;
    }

    // ── ANIMACAO ─────────────────────────────────────────────────────

    public IEnumerator AnimarCaotico(RectTransform rt, Vector2 origem, Vector2 destino)
    {
        rt.anchoredPosition = origem;
        rt.rotation = Quaternion.Euler(0, 0, Random.Range(-45f, 45f)); // rotacao inicial

        float rotacaoFinal = Random.Range(-caosRotacao, caosRotacao);
        Vector2 destinoComCaos = destino + Random.insideUnitCircle * caosOffsetPos;
        float duracao = animacaoDuracao + Random.Range(-caosVelocidade, caosVelocidade);
        duracao = Mathf.Max(0.2f, duracao);

        float tempo = 0f;
        Quaternion rotInicial = rt.rotation;
        Quaternion rotFinal = Quaternion.Euler(0, 0, rotacaoFinal);

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tempo / duracao);
            rt.anchoredPosition = Vector2.Lerp(origem, destinoComCaos, t);
            rt.rotation = Quaternion.Lerp(rotInicial, rotFinal, t);
            yield return null;
        }

        rt.anchoredPosition = destinoComCaos;
        rt.rotation = rotFinal;

        rt.GetComponent<DragDrop>()?.DefinirPosicaoOriginal();
    }

    // ── HELPERS ──────────────────────────────────────────────────────

    private GameObject SpawnarPeca(TipoPeca tipo, ValorNumero valorNum, ValorComida valorCom)
    {
        var obj = Instantiate(pecaPrefab, canvas.transform);
        var peca = obj.GetComponent<DragDrop>();
        peca.tipoPeca = tipo;
        peca.valorNumero = valorNum;
        peca.valorComida = valorCom;
        peca.AplicarSprite();
        return obj;
    }

    private Vector2 GerarPosicaoLivre()
    {
        Rect rect = areaDeSpawn.rect;

        // posicao aleatoria no espaco local do areaDeSpawn
        float localX = Random.Range(rect.xMin, rect.xMax);
        float localY = Random.Range(rect.yMin, rect.yMax);

        // converte para espaco local do canvas
        Vector3 worldPos = areaDeSpawn.TransformPoint(new Vector3(localX, localY, 0));
        Vector2 canvasLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPos),
            canvas.worldCamera,
            out canvasLocal
        );

        return canvasLocal;
    }

    private void Embaralhar<T>(List<T> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }

    public IEnumerator AnimarPeca(RectTransform rt, Vector2 origem, Vector2 destino, float duracao)
    {
        rt.anchoredPosition = origem;
        float tempo = 0f;
        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tempo / duracao);
            rt.anchoredPosition = Vector2.Lerp(origem, destino, t);
            yield return null;
        }
        rt.anchoredPosition = destino;
        rt.GetComponent<DragDrop>()?.DefinirPosicaoOriginal();
    }

    private void LimparPecas()
    {
        foreach (var p in pecasComidaAtivas) if (p != null) Destroy(p);
        foreach (var p in pecasNumeroAtivas) if (p != null) Destroy(p);
        pecasComidaAtivas.Clear();
        pecasNumeroAtivas.Clear();
        posicoesUsadas.Clear();
    }

    public void VerificarVitoria(ItemSlot slotCompletado)
    {
        // busca todos os slots da cena
        var todosSlots = FindObjectsByType<ItemSlot>(FindObjectsSortMode.None);

        foreach (var slot in todosSlots)
            if (!slot.EstaCompleto()) return;

        // todos completos
        StartCoroutine(AvancarRodada());
    }

    private IEnumerator AvancarRodada()
    {
        // reseta apenas o estado interno dos slots, sem destruir as pecas
        var todosSlots = FindObjectsByType<ItemSlot>(FindObjectsSortMode.None);
        foreach (var slot in todosSlots)
            slot.ResetarEstado();

        SpawnarRodada();
        yield return null;
    }
}