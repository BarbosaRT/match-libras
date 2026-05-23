using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Unity.VisualScripting;

public class LevelManager : MonoBehaviour
{
    [field: SerializeField] public List<Sprite> LibraSprites { get; private set; }
    public Image spriteRenderer;
    public int number;
    public Canvas canvas;
    public GameObject pecaPrefab;
    public TextMeshProUGUI vidasText;

    [Header("Spawn - Geral")]
    public float animacaoDuracao = 0.4f;

    [Header("Caos da Animacao")]
    public float caosRotacao = 25f;
    public float caosOffsetPos = 40f;
    public float caosVelocidade = 0.15f;

    [Header("Spawn - Posicionamento Livre")]
    public RectTransform areaDeSpawn;

    [Header("Limite de Pecas")]
    public int limiteNumeros = 5;
    public int limiteComidas = 10;

    [Header("Repulsao entre Pecas")]
    public float raioRepulsao = 100f;
    public float forcaRepulsao = 300f;
    public float duracaoRepulsao = 0.3f;

    private ValorComida comidaCorreta;
    private List<GameObject> todasPecas = new List<GameObject>();
    private int vidas = 3;

    void Start()
    {
        vidasText.text = new string('❤', vidas);
        StartCoroutine(StartComDelay());
    }

    IEnumerator StartComDelay()
    {
        yield return new WaitForSeconds(2.5f);
        SpawnarRodada();
    }

    public void SpawnarRodada()
    {
        ResetarSlots();

        number = Random.Range(1, 10);
        spriteRenderer.sprite = LibraSprites[number];
        comidaCorreta = (ValorComida)Random.Range(0, System.Enum.GetValues(typeof(ValorComida)).Length);

        StartCoroutine(SpawnarRodadaCoroutine());
    }

    // ── SPAWN PRINCIPAL ───────────────────────────────────────────────

    private IEnumerator SpawnarRodadaCoroutine()
    {
        todasPecas.RemoveAll(p => p == null);

        var necessarias = PecasNecessarias();

        // necessarias sempre spawnam, ignoram o limite
        foreach (var (tipo, valNum, valCom, tag) in necessarias)
        {
            var obj = SpawnarPeca(tipo, valNum, valCom, tag);
            todasPecas.Add(obj);
            AnimarSpawn(obj, tipo);
        }

        // distratores respeitam limite por tipo
        var distratores = GerarDistratores();
        foreach (var (tipo, valNum, valCom, tag) in distratores)
        {
            todasPecas.RemoveAll(p => p == null);
            if (tipo == TipoPeca.Numero && ContarPorTipo(TipoPeca.Numero) >= limiteNumeros) continue;
            if (tipo == TipoPeca.Comida && ContarPorTipo(TipoPeca.Comida) >= limiteComidas) continue;

            var obj = SpawnarPeca(tipo, valNum, valCom, tag);
            todasPecas.Add(obj);
            AnimarSpawn(obj, tipo);
        }

        // aguarda animacoes de entrada e entao repele sobreposicoes
        //yield return new WaitForSeconds(animacaoDuracao - 0.15f);
        //RepelirPecas();
        yield return null;
    }

    private int ContarPorTipo(TipoPeca tipo)
    {
        return todasPecas.Count(p =>
        {
            if (p == null) return false;
            var d = p.GetComponent<DragDrop>();
            return d != null && d.tipoPeca == tipo;
        });
    }

    private void AnimarSpawn(GameObject obj, TipoPeca tipo)
    {
        Vector2 destino = GerarPosicaoLivre();
        float origemY = tipo == TipoPeca.Numero ? Screen.height : -Screen.height;
        Vector2 origem = new Vector2(destino.x + Random.Range(-150f, 150f), origemY);
        StartCoroutine(AnimarCaotico(obj.GetComponent<RectTransform>(), origem, destino));
    }


    // ── PECAS NECESSARIAS ─────────────────────────────────────────────

    private List<(TipoPeca, ValorNumero, ValorComida, string)> PecasNecessarias()
    {
        var lista = new List<(TipoPeca, ValorNumero, ValorComida, string)>();

        bool numeroJaExiste = todasPecas.Any(p =>
        {
            if (p == null) return false;
            var d = p.GetComponent<DragDrop>();
            return d != null
                && d.tipoPeca == TipoPeca.Numero
                && d.valorNumero == (ValorNumero)number
                && p.transform.parent == canvas.transform;
        });

        if (!numeroJaExiste)
            lista.Add((TipoPeca.Numero, (ValorNumero)number, comidaCorreta, "Numero"));

        int comidasExistentes = todasPecas.Count(p =>
        {
            if (p == null) return false;
            var d = p.GetComponent<DragDrop>();
            return d != null
                && d.tipoPeca == TipoPeca.Comida
                && d.valorComida == comidaCorreta
                && p.transform.parent == canvas.transform;
        });

        int comidasParaSpawnar = Mathf.Max(0, number - comidasExistentes);
        for (int i = 0; i < comidasParaSpawnar; i++)
            lista.Add((TipoPeca.Comida, ValorNumero.Zero, comidaCorreta, "Comida"));

        return lista;
    }

    // ── DISTRATORES ───────────────────────────────────────────────────

    private List<(TipoPeca, ValorNumero, ValorComida, string)> GerarDistratores()
    {
        var lista = new List<(TipoPeca, ValorNumero, ValorComida, string)>();

        var disponiveis = new List<int>();
        for (int i = 0; i <= 9; i++)
            if (i != number) disponiveis.Add(i);
        Embaralhar(disponiveis);
        lista.Add((TipoPeca.Numero, (ValorNumero)disponiveis[0], comidaCorreta, "Numero"));

        int totalDistratores = Random.Range(1, 5);
        var outrosTipos = new List<ValorComida>();
        foreach (ValorComida v in System.Enum.GetValues(typeof(ValorComida)))
            if (v != comidaCorreta) outrosTipos.Add(v);

        for (int i = 0; i < totalDistratores; i++)
        {
            ValorComida tipo = Random.value > 0.5f
                ? comidaCorreta
                : outrosTipos[Random.Range(0, outrosTipos.Count)];
            lista.Add((TipoPeca.Comida, ValorNumero.Zero, tipo, "Comida"));
        }

        return lista;
    }

    // ── VITORIA ───────────────────────────────────────────────────────

    public void VerificarVitoria()
    {
        var todosSlots = FindObjectsByType<ItemSlot>(FindObjectsSortMode.None);
        foreach (var slot in todosSlots)
            if (!slot.EstaCompleto()) { ExpulsarSlots(); return; }

        StartCoroutine(AvancarRodada());
    }

    private IEnumerator AvancarRodada()
    {
        ResetarSlots();
        SpawnarRodada();
        yield return null;
    }
    public void ExpulsarSlots()
    {
        vidas--;
        vidasText.text = new string('❤', vidas);
        var todosSlots = FindObjectsByType<ItemSlot>(FindObjectsSortMode.None);
        foreach (var slot in todosSlots)
            slot.ExpulsarTodasPecas();
    }
    public void ResetarSlots()
    {
        var todosSlots = FindObjectsByType<ItemSlot>(FindObjectsSortMode.None);
        foreach (var slot in todosSlots)
            slot.LimparSlot();
    }

    // ── ANIMACAO ─────────────────────────────────────────────────────

    public IEnumerator AnimarCaotico(RectTransform rt, Vector2 origem, Vector2 destino)
    {
        if (rt == null) yield break;

        rt.anchoredPosition = origem;
        rt.rotation = Quaternion.Euler(0, 0, Random.Range(-45f, 45f));

        float rotacaoFinal = Random.Range(-caosRotacao, caosRotacao);
        Vector2 destinoComCaos = destino + Random.insideUnitCircle * caosOffsetPos;
        float duracao = animacaoDuracao + Random.Range(-caosVelocidade, caosVelocidade);
        duracao = Mathf.Max(0.2f, duracao);

        float tempo = 0f;
        Quaternion rotInicial = rt.rotation;
        Quaternion rotFinal = Quaternion.Euler(0, 0, rotacaoFinal);

        while (tempo < duracao)
        {
            if (rt == null) yield break;
            tempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tempo / duracao);
            rt.anchoredPosition = Vector2.Lerp(origem, destinoComCaos, t);
            rt.rotation = Quaternion.Lerp(rotInicial, rotFinal, t);
            yield return null;
        }

        if (rt == null) yield break;
        rt.anchoredPosition = destinoComCaos;
        rt.rotation = rotFinal;
        rt.GetComponent<DragDrop>()?.DefinirPosicaoOriginal();
    }

    public IEnumerator AnimarPeca(RectTransform rt, Vector2 origem, Vector2 destino, float duracao)
    {
        if (rt == null) yield break;

        rt.anchoredPosition = origem;
        float tempo = 0f;

        while (tempo < duracao)
        {
            if (rt == null) yield break;
            tempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tempo / duracao);
            rt.anchoredPosition = Vector2.Lerp(origem, destino, t);
            yield return null;
        }

        if (rt != null)
        {
            rt.anchoredPosition = destino;
            rt.GetComponent<DragDrop>()?.DefinirPosicaoOriginal();
        }
    }

    // ── HELPERS ──────────────────────────────────────────────────────

    private GameObject SpawnarPeca(TipoPeca tipo, ValorNumero valorNum, ValorComida valorCom, string tag)
    {
        var obj = Instantiate(pecaPrefab, canvas.transform);
        obj.tag = tag;
        var peca = obj.GetComponent<DragDrop>();
        peca.tipoPeca = tipo;
        peca.valorNumero = valorNum;
        peca.valorComida = valorCom;
        peca.AplicarSprite();
        var fisica = obj.GetComponent<PecaFisica>();
        if (fisica != null) fisica.Inicializar(canvas, areaDeSpawn);
        return obj;
    }

    private Vector2 GerarPosicaoLivre()
    {
        Rect rect = areaDeSpawn.rect;
        float localX = Random.Range(rect.xMin, rect.xMax);
        float localY = Random.Range(rect.yMin, rect.yMax);

        Vector3 worldPos = areaDeSpawn.TransformPoint(new Vector3(localX, localY, 0));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPos),
            canvas.worldCamera,
            out Vector2 canvasLocal
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
}