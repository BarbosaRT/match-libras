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
    public GameObject pecaNumeroPrefab;
    public GameObject pecaComidaPrefab;

    [Header("Spawn de Comidas")]
    public float delayEntreComidas = 0.15f;
    public float animacaoDuracao = 0.4f;
    public float spacingHorizontal = 160f; // distancia entre as pecas na linha

    // posicao fora da tela (origem da animacao)
    private Vector2 SpawnOffscreen => new Vector2(0, -Screen.height);

    private List<GameObject> pecasComidaAtivas = new List<GameObject>();
    private GameObject pecaNumeroAtiva;

    void Start()
    {
        SpawnarRodada();
    }

    public void SpawnarRodada()
    {
        // limpa pecas anteriores
        LimparPecas();

        number = Random.Range(0, 10);
        spriteRenderer.sprite = LibraSprites[number];

        // spawna a peca de numero
        var objNum = Instantiate(pecaNumeroPrefab, canvas.transform);
        var pecaNum = objNum.GetComponent<DragDrop>();
        pecaNum.tipoPeca = TipoPeca.Numero;
        pecaNum.valorNumero = (ValorNumero)number;
        pecaNum.AplicarSprite();
        pecaNumeroAtiva = objNum;

        // define qual comida será usada nessa rodada
        ValorComida comidaEscolhida = (ValorComida)Random.Range(0, System.Enum.GetValues(typeof(ValorComida)).Length);

        // spawna as pecas de comida com animacao
        StartCoroutine(SpawnarComidasAnimado(comidaEscolhida));
    }

    private IEnumerator SpawnarComidasAnimado(ValorComida comidaEscolhida)
    {
        // calcula posicoes finais centralizadas em linha
        List<Vector2> posicoesFinais = CalcularPosicoesFinais(number);

        for (int i = 0; i < number; i++)
        {
            var obj = Instantiate(pecaComidaPrefab, canvas.transform);
            var peca = obj.GetComponent<DragDrop>();
            peca.tipoPeca = TipoPeca.Comida;
            peca.valorComida = comidaEscolhida;
            peca.AplicarSprite();
            pecasComidaAtivas.Add(obj);

            // inicia animacao individual
            var rt = obj.GetComponent<RectTransform>();
            StartCoroutine(AnimarPeca(rt, SpawnOffscreen, posicoesFinais[i], animacaoDuracao));

            yield return new WaitForSeconds(delayEntreComidas);
        }
    }

    private List<Vector2> CalcularPosicoesFinais(int quantidade)
    {
        var posicoes = new List<Vector2>();
        float totalWidth = (quantidade - 1) * spacingHorizontal;
        float startX = -totalWidth / 2f;
        float targetY = -200f; // altura final na tela (ajuste conforme seu layout)

        for (int i = 0; i < quantidade; i++)
            posicoes.Add(new Vector2(startX + i * spacingHorizontal, targetY));

        return posicoes;
    }

    private IEnumerator AnimarPeca(RectTransform rt, Vector2 origem, Vector2 destino, float duracao)
    {
        rt.anchoredPosition = origem;
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tempo / duracao); // suaviza a curva
            rt.anchoredPosition = Vector2.Lerp(origem, destino, t);
            yield return null;
        }

        rt.anchoredPosition = destino;
    }

    private void LimparPecas()
    {
        foreach (var p in pecasComidaAtivas)
            if (p != null) Destroy(p);
        pecasComidaAtivas.Clear();

        if (pecaNumeroAtiva != null)
            Destroy(pecaNumeroAtiva);
    }
}