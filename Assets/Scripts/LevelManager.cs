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
    [field: SerializeField] public List<GameObject> vidaImages { get; private set; }

    [Header("Container para as Peças")]
    public RectTransform containerPecas;  // Arraste aqui o objeto (ex: Panel) onde as peças serão filhas

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

    [Header("Paineis")]
    public GameObject painelVitoria;
    public GameObject painelDerrota;

    [Header("Feedback - Vida e Vitória")]
    public ParticleSystem particulasAcerto;
    public float shakeDuracao = 0.3f;
    public float shakeMagnitude = 20f;

    private ValorComida comidaCorreta;
    private List<GameObject> todasPecas = new List<GameObject>();
    private int vidas = 3;
    private List<int> numeros = new List<int>();

    // Propriedade auxiliar para obter o transform pai das peças
    private Transform ParentTransform => containerPecas != null ? containerPecas : canvas.transform;

    // Versao publica para ItemSlot.RemoverDoSlot reparentar para o container correto.
    public Transform PecasParent => ParentTransform;

    void Start()
    {
        numeros = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Embaralhar(numeros);

        AtualizarVidas();
        StartCoroutine(StartComDelay());
    }

    void AtualizarVidas()
    {
        int v = vidas;
        foreach (GameObject obj in vidaImages)
        {
            obj.SetActive(v > 0);
            v--;
        }
        if (vidas <= 0)
        {
            DerrotaFinal();
        }
    }

    IEnumerator StartComDelay()
    {
        yield return new WaitForSeconds(2.5f);
        SpawnarRodada();
    }

    public void SpawnarRodada()
    {
        ResetarSlots();

        if (numeros.Count == 0)
        {
            VitoriaFinal();
            return;
        }

        number = numeros[0];
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
        RectTransform parentRect = containerPecas != null ? containerPecas : canvas.GetComponent<RectTransform>();
        Vector2 destino = GerarPosicaoLivre(); // já em coordenadas locais do container

        float altura = parentRect.rect.height;
        float offsetY = altura + 200f;
        float xOffset = Random.Range(-150f, 150f);

        Vector2 origem;
        if (tipo == TipoPeca.Numero)
            origem = destino + new Vector2(xOffset, offsetY);   // vem de cima
        else
            origem = destino + new Vector2(xOffset, -offsetY);  // vem de baixo

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
                && p.transform.parent == ParentTransform;
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
                && p.transform.parent == ParentTransform;
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

        // Numeros Distratores
        var disponiveis = new List<int>();
        for (int i = 1; i <= 9; i++)
            if (i != number) disponiveis.Add(i);
        Embaralhar(disponiveis);
        lista.Add((TipoPeca.Numero, (ValorNumero)disponiveis[0], comidaCorreta, "Numero"));

        int quantidadeNumerosDistratores = Random.Range(2, 5); // Ajuste os valores
        for (int i = 0; i < quantidadeNumerosDistratores && i < disponiveis.Count; i++)
        {
            lista.Add((TipoPeca.Numero, (ValorNumero)disponiveis[i], comidaCorreta, "Numero"));
        }


        int totalDistratores = Random.Range(1, 10);
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

        SoundManager.Instance?.Play("Acerto");
        if (particulasAcerto != null)
            particulasAcerto.Play();

        if (vidas > 0)
        {
            StartCoroutine(AvancarRodada());
        }
    }

    private IEnumerator AvancarRodada()
    {
        numeros.Remove(number);
        ResetarSlots();
        SpawnarRodada();
        yield return null;
    }

    private void VitoriaFinal()
    {
        Cronometro.Instance?.Parar();
        SoundManager.Instance?.Play("VitoriaFinal");
        if (painelVitoria != null)
            painelVitoria.SetActive(true);
    }


    private void DerrotaFinal()
    {
        SoundManager.Instance?.Play("DerrotaFinal");
        if (painelDerrota != null)
            painelDerrota.SetActive(true);
    }

    public void ExpulsarSlots()
    {
        vidas--;
        AtualizarVidas();
        SoundManager.Instance?.Play("PerderVida");
        ScreenShake.Instance?.Shake(shakeDuracao, shakeMagnitude);
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
        var obj = Instantiate(pecaPrefab, ParentTransform);
        obj.tag = tag;
        var peca = obj.GetComponent<DragDrop>();
        peca.DefinirParentOriginal(ParentTransform);
        peca.tipoPeca = tipo;
        peca.valorNumero = valorNum;
        peca.valorComida = valorCom;
        peca.AplicarSprite();
        var fisica = obj.GetComponent<PecaFisica>();
        if (fisica != null) fisica.Inicializar(canvas, areaDeSpawn, ParentTransform);
        return obj;
    }

    private Vector2 GerarPosicaoLivre()
    {
        Rect rect = areaDeSpawn.rect;
        float localX = Random.Range(rect.xMin, rect.xMax);
        float localY = Random.Range(rect.yMin, rect.yMax);
        Vector3 worldPos = areaDeSpawn.TransformPoint(new Vector3(localX, localY, 0));

        RectTransform parentRect = containerPecas != null ? containerPecas : canvas.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPos),
            canvas.worldCamera,
            out localPos
        );
        return localPos;
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