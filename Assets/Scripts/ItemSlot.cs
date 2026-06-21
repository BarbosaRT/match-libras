using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler
{
    public LevelManager levelManager;
    public TipoPeca tipoPeca;

    [Header("Rejeicao")]
    public float rejeicaoDuracao = 0.5f;

    [Header("Grid de Comidas")]
    public float gridSpacing = 120f;

    private List<DragDrop> pecasNoSlot = new List<DragDrop>();

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Prralo");
        if (eventData.pointerDrag == null) return;

        Debug.Log("Porralsk");
        var peca = eventData.pointerDrag.GetComponent<DragDrop>();
        if (peca == null) return;
        Debug.Log("Porralsaaaaaaaaaaaaaaak");

        // rejeita tag errada
        if (eventData.pointerDrag.tag != gameObject.tag)
        {

            Debug.Log("Prral");
            RejeitarPeca(peca);
            return;
        }

        Debug.Log("Prssra");
        // rejeita tipo de peca errado pro slot
        if (peca.tipoPeca != tipoPeca)
        {

            Debug.Log("Prrao");
            RejeitarPeca(peca);
            return;
        }

        switch (tipoPeca)
        {
            case TipoPeca.Numero:
                Debug.Log("Psrra");
                if (pecasNoSlot.Count > 0)
                {
                    Debug.Log("Prra");
                    RejeitarPeca(peca);
                    break;
                }
                EncaixarPeca(peca);
                break;

            case TipoPeca.Comida:
                TentarAdicionarComida(peca);
                break;
        }
    }

    private void TentarAdicionarComida(DragDrop peca)
    {
        if (pecasNoSlot.Count > 0 && pecasNoSlot[0].valorComida != peca.valorComida)
        {
            Debug.Log("Tipo de comida errado � expulsando todas!");
            ExpulsarTodasPecas();
            RejeitarPeca(peca);
            return;
        }

        //if (pecasNoSlot.Count >= levelManager.number)
        //{
        //    Debug.Log("Slot cheio!");
        //    RejeitarPeca(peca);
        //    return;
        //}

        EncaixarPeca(peca);
        Debug.Log($"Comida adicionada: {pecasNoSlot.Count}/{levelManager.number}");

        if (pecasNoSlot.Count == levelManager.number)
            Debug.Log("Combinacao correta!");

        if (pecasNoSlot.Count == levelManager.number)
        {
            Debug.Log("Combinacao correta!");
        }
    }

    private void EncaixarPeca(DragDrop peca)
    {
        pecasNoSlot.Add(peca);
        peca.GetComponent<CanvasGroup>().blocksRaycasts = false;

        RectTransform slotRT = GetComponent<RectTransform>();
        RectTransform pecaRT = peca.GetComponent<RectTransform>();
        pecaRT.SetParent(slotRT, true);

        if (tipoPeca == TipoPeca.Numero)
        {
            pecaRT.anchoredPosition = Vector2.zero;
        }
        else
        {
            OrganizarGrid();
        }
    }

    public bool EstaCompleto()
    {
        if (tipoPeca == TipoPeca.Numero)
            return pecasNoSlot.Count == 1;
        else
            return pecasNoSlot.Count == levelManager.number;
    }

    public void LimparSlot()
    {
        foreach (var p in pecasNoSlot)
            if (p != null) Destroy(p.gameObject);
        pecasNoSlot.Clear();
    }

    private void OrganizarGrid()
    {
        int colunas = 3;

        for (int i = 0; i < pecasNoSlot.Count; i++)
        {
            int col = i % colunas;
            int row = i / colunas;

            int totalColunas = Mathf.Min(pecasNoSlot.Count, colunas);
            int totalLinhas = Mathf.CeilToInt(pecasNoSlot.Count / (float)colunas);

            float offsetX = (totalColunas - 1) * gridSpacing / 2f;
            float offsetY = (totalLinhas - 1) * gridSpacing / 2f;

            Vector2 posAlvo = new Vector2(
                col * gridSpacing - offsetX,
                -(row * gridSpacing - offsetY)
            );

            StartCoroutine(MoverParaPosicao(pecasNoSlot[i].GetComponent<RectTransform>(), posAlvo, 0.2f));
        }
    }

    private IEnumerator MoverParaPosicao(RectTransform rt, Vector2 destino, float duracao)
    {
        if (rt == null) yield break; // <- guard contra objeto destruido

        Vector2 origem = rt.anchoredPosition;
        float tempo = 0f;

        while (tempo < duracao)
        {
            if (rt == null) yield break; // <- checa a cada frame tambem
            tempo += Time.deltaTime;
            float t = tempo / duracao;
            t = t * t * (3f - 2f * t);
            rt.anchoredPosition = Vector2.Lerp(origem, destino, t);
            yield return null;
        }

        if (rt != null)
            rt.anchoredPosition = destino;
    }

    private void RejeitarPeca(DragDrop peca)
    {
        // garante que esta no canvas antes de voltar
        Canvas canvas = levelManager.canvas;
        peca.transform.SetParent(canvas.transform, true);
        peca.VoltarParaOrigem(rejeicaoDuracao, this);
    }

    public void ExpulsarTodasPecas()
    {
        foreach (var p in pecasNoSlot)
        {
            if (p == null) continue;
            Canvas canvas = levelManager.canvas;
            p.transform.SetParent(canvas.transform, true);
            p.VoltarParaOrigem(rejeicaoDuracao, this);
        }
        pecasNoSlot.Clear();
    }

    // chamado pelo DragDrop quando o jogador come�a a arrastar uma peca do slot
    public void RemoverDoSlot(DragDrop peca)
    {
        if (pecasNoSlot.Remove(peca))
        {
            peca.GetComponent<CanvasGroup>().blocksRaycasts = true;
            peca.transform.SetParent(levelManager.canvas.transform, true);
            OrganizarGrid(); // reorganiza as que ficaram
        }
    }
}