using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// Coloque este script num GameObject vazio (ex: "TutorialManager"), filho da
// Canvas de tutorial, e arraste as referencias no Inspector.
//
// Este script NAO usa pecas fixas montadas na cena: ele manda o LevelManager
// spawnar uma rodada de verdade (igual ao jogo normal), sorteando um numero
// entre numeroMin e numeroMax, e depois procura entre as pecas recem-criadas
// qual e o numero certo e quais sao as comidas certas para demonstrar.
public class TutorialSequence : MonoBehaviour
{
    [Header("Referencia ao gerenciador do nivel (mesmo do jogo normal)")]
    [SerializeField] private LevelManager levelManager;

    [Header("Slots de destino (os mesmos ItemSlot usados no jogo)")]
    [SerializeField] private ItemSlot slotNumero;
    [SerializeField] private ItemSlot slotComida;

    [Header("Componentes visuais do tutorial")]
    [SerializeField] private TutorialSpotlight spotlight;
    [SerializeField] private TutorialHandPointer handPointer;
    [SerializeField] private TutorialGhostPiece ghostPiece;

    [Header("Introducao (sinal de libras -> peca numerica)")]
    [SerializeField] private RectTransform libraSignCard;

    [Header("Botao de confirmar (spotlight guia ate ele no final)")]
    [SerializeField] private Button botaoConfirmar;

    [Header("Chamado depois que o jogador clica em confirmar com tudo certo")]
    [Tooltip("Conecte aqui, por exemplo, um metodo que troca de tela/carrega a proxima cena")]
    [SerializeField] private UnityEvent aoConcluirTutorial;

    [Header("Sorteio do numero do tutorial")]
    [SerializeField] private int numeroMin = 1;
    [SerializeField] private int numeroMax = 4;

    [Header("Feedback")]
    [Tooltip("Pode ser o mesmo ParticleSystem referenciado no LevelManager, ou um dedicado ao tutorial")]
    [SerializeField] private ParticleSystem particulasAcerto;

    [Header("Tempos (segundos)")]
    [SerializeField] private float esperaAposSpawn = 1.2f;
    [SerializeField] private float pauseInicial = 0.4f;
    [SerializeField] private float spotlightMoveDuration = 0.6f;
    [SerializeField] private float pauseEntrePassos = 0.5f;
    [SerializeField] private float handMoveDuration = 0.9f;
    [SerializeField] private float spotlightRadiusPixelsDurantePasso = 90f;

    void Awake()
    {
        // Awake roda antes do Start() de qualquer script da cena, entao isso
        // garante que o LevelManager nao vai spawnar sozinho (via StartComDelay)
        // antes do tutorial pedir o spawn forcado.
        if (levelManager != null)
            levelManager.iniciarAutomaticamente = false;
    }

    void Start()
    {
        if (levelManager == null || slotNumero == null || slotComida == null ||
            spotlight == null || handPointer == null || ghostPiece == null)
        {
            Debug.LogError("TutorialSequence: alguma referencia obrigatoria nao foi atribuida no Inspector.", this);
            return;
        }
        StartCoroutine(RunTutorial());
    }

    private IEnumerator RunTutorial()
    {
        int numeroSorteado = Random.Range(numeroMin, numeroMax + 1);
        levelManager.SpawnarRodadaParaTutorial(numeroSorteado);

        // Espera o spawn e a animacao caotica de entrada das pecas terminarem.
        yield return new WaitForSeconds(esperaAposSpawn);

        RectTransform pecaNumero = EncontrarPecaNumero(numeroSorteado);
        List<RectTransform> pecasComida = EncontrarPecasComida(numeroSorteado);

        if (pecaNumero == null)
        {
            Debug.LogError($"TutorialSequence: nao encontrei entre as pecas spawnadas o numero {numeroSorteado}.", this);
            yield break;
        }
        if (pecasComida.Count < numeroSorteado)
        {
            Debug.LogWarning($"TutorialSequence: esperava {numeroSorteado} comida(s) corretas, encontrei {pecasComida.Count}.", this);
        }

        // Introducao: sinal de libras -> a propria peca numerica que acabou de nascer
        if (libraSignCard != null)
        {
            spotlight.Show();
            spotlight.SetInstant(libraSignCard);
            yield return new WaitForSeconds(pauseInicial);

            yield return StartCoroutine(spotlight.MoveTo(pecaNumero, spotlightMoveDuration));
            yield return new WaitForSeconds(pauseEntrePassos);
        }

        // Passo do numero: nao usamos slotNumero.EstaCompleto() sozinho, pois
        // ele so verifica se o slot esta ocupado (aceita qualquer numero,
        // certo ou errado — isso e proposital no jogo normal, para o jogador
        // poder errar e aprender). O tutorial lida com 3 situacoes:
        //   1) slot vazio -> demonstra a peca certa
        //   2) peca errada no slot -> guia o jogador a tira-la primeiro
        //   3) peca certa no slot -> nada a fazer, segue para as comidas
        RectTransform slotNumeroRect = slotNumero.GetComponent<RectTransform>();
        while (pecaNumero.parent != slotNumeroRect)
        {
            var pecaAtual = slotNumero.PecaAtual;

            if (pecaAtual == null)
            {
                // Slot vazio: demonstra a peca certa. Termina o passo assim que
                // QUALQUER peca entrar no slot (pode ser a certa ou uma errada
                // que o jogador tenha colocado por conta propria) — o loop
                // reavalia em seguida qual dos 3 casos se aplica agora.
                yield return StartCoroutine(DemonstrarArrasto(
                    pecaNumero,
                    slotNumeroRect,
                    () => slotNumero.PecaAtual != null,
                    tocarParticulaAoConcluir: false
                ));
            }
            else
            {
                // Peca errada esta ocupando o slot: guia a remocao dela antes
                // de poder demonstrar a peca certa.
                RectTransform areaLivre = levelManager.areaDeSpawn != null
                    ? levelManager.areaDeSpawn
                    : slotNumeroRect;

                yield return StartCoroutine(GuiarRemocaoDoSlot(
                    slotNumero,
                    pecaAtual.GetComponent<RectTransform>(),
                    areaLivre
                ));
            }

            yield return new WaitForSeconds(pauseEntrePassos);
        }

        // So chega aqui quando a peca certa esta de fato no slot.
        if (particulasAcerto != null)
        {
            particulasAcerto.transform.position = slotNumeroRect.position;
            particulasAcerto.Play();
        }

        // Passos das comidas: em vez de seguir uma lista fixa de objetos
        // escolhidos no inicio, a cada iteracao verifica o ESTADO REAL do
        // slot (EstaCompleto). Assim, se o jogador soltar pecas diferentes
        // das demonstradas (inclusive distratoras do mesmo tipo de comida),
        // a contagem sobe do mesmo jeito e o tutorial reconhece que o passo
        // avancou, em vez de insistir numa peca especifica que ja nao faz
        // mais diferenca.
        while (!slotComida.EstaCompleto())
        {
            RectTransform proximaComida = EncontrarProximaComidaDisponivel();
            if (proximaComida == null)
            {
                // Nao sobrou nenhuma peca do tipo certo para demonstrar, mas
                // o slot ainda nao esta completo (ex: distratoras erradas
                // ocuparam o slot). Nao ha o que fazer alem de esperar o
                // jogador corrigir por conta propria.
                yield return new WaitUntil(() => slotComida.EstaCompleto());
                break;
            }

            int contagemAntes = slotComida.QuantidadeNoSlot;

            yield return StartCoroutine(DemonstrarArrasto(
                proximaComida,
                slotComida.GetComponent<RectTransform>(),
                // Conclui o passo assim que QUALQUER peca nova entrar no slot
                // (o jogador pode ter colocado uma peca diferente da
                // demonstrada) OU quando o slot ja estiver completo.
                () => slotComida.QuantidadeNoSlot > contagemAntes || slotComida.EstaCompleto()
            ));
            yield return new WaitForSeconds(pauseEntrePassos);
        }

        // Todas as pecas foram colocadas corretamente: guia o spotlight (e a
        // mao, com o mesmo "tap" de sempre) ate o botao de confirmar.
        if (botaoConfirmar != null)
        {
            RectTransform botaoRect = botaoConfirmar.GetComponent<RectTransform>();
            spotlight.Show();
            yield return StartCoroutine(spotlight.MoveTo(botaoRect, spotlightMoveDuration));
            handPointer.SetWorldPosition(botaoRect.position);
            handPointer.Show();
            yield return StartCoroutine(handPointer.PlayTap());
            yield return new WaitForSeconds(pauseEntrePassos);
            handPointer.Hide();
            yield return StartCoroutine(spotlight.FadeOut(0.3f));

            bool confirmado = false;
            UnityAction onClick = () => confirmado = true;
            botaoConfirmar.onClick.AddListener(onClick);

            yield return new WaitUntil(() => confirmado);

            botaoConfirmar.onClick.RemoveListener(onClick);
            handPointer.Hide();
            yield return StartCoroutine(spotlight.FadeOut(0.3f));
        }

        MarcarTutorialComoJogado();
        aoConcluirTutorial?.Invoke();

        // Desativa SO este componente, nao o GameObject inteiro — o LevelManager
        // esta no mesmo GameObject e precisa continuar ativo depois do tutorial.
        enabled = false;
    }

    private IEnumerator DemonstrarArrasto(RectTransform peca, RectTransform slotRect, System.Func<bool> condicaoDeConclusao, bool tocarParticulaAoConcluir = true)
    {
        // 1) Spotlight foca na peca que vai ser arrastada
        spotlight.Show();
        spotlight.SetInstant(peca);
        yield return new WaitForSeconds(pauseInicial);

        // 2) Mao e fantasma (copia semi-transparente da peca) aparecem sobre ela
        // O sprite real da peca fica em Peca > Canvas > Tipo (nao no primeiro
        // Image que aparece na hierarquia, que seria a Sombra).
        Transform tipoTransform = peca.Find("Canvas/Tipo");
        Image pecaImage = tipoTransform != null ? tipoTransform.GetComponent<Image>() : null;
        if (pecaImage == null)
            Debug.LogWarning($"TutorialSequence: nao encontrei 'Canvas/Tipo' em '{peca.name}' para copiar o sprite do fantasma.", this);
        ghostPiece.SetSprite(pecaImage != null ? pecaImage.sprite : null);
        ghostPiece.SetWorldPosition(peca.position);
        ghostPiece.Show();

        handPointer.SetWorldPosition(peca.position);
        handPointer.Show();
        yield return new WaitForSeconds(0.25f);

        // 3) Mao + fantasma + spotlight se movem juntos ate o slot de destino
        Vector3 origem = peca.position;
        Vector3 destino = slotRect.position;
        float t = 0f;
        while (t < handMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / handMoveDuration));
            Vector3 pos = Vector3.Lerp(origem, destino, k);

            handPointer.SetWorldPosition(pos);
            ghostPiece.SetWorldPosition(pos);
            spotlight.SetInstantAtWorldPos(pos, spotlightRadiusPixelsDurantePasso);
            yield return null;
        }
        handPointer.SetWorldPosition(destino);
        ghostPiece.SetWorldPosition(destino);
        spotlight.SetInstantAtWorldPos(destino, spotlightRadiusPixelsDurantePasso);

        yield return StartCoroutine(handPointer.PlayTap());

        // 4) Some com tudo para o jogador poder fazer a acao de verdade
        handPointer.Hide();
        ghostPiece.Hide();
        yield return StartCoroutine(spotlight.FadeOut(0.3f));

        // 5) Espera o jogador realmente soltar a peca no slot correto,
        //    checando o proprio ItemSlot (EstaCompleto / QuantidadeNoSlot).
        yield return new WaitUntil(condicaoDeConclusao);

        // 6) Feedback de acerto
        if (tocarParticulaAoConcluir && particulasAcerto != null)
        {
            particulasAcerto.transform.position = slotRect.position;
            particulasAcerto.Play();
        }
    }

    /// Guia o jogador a retirar uma peca ERRADA que esta ocupando um slot,
    /// "puxando" ela visualmente de volta para a area livre. Nao dispara
    /// particula de acerto (nao houve acerto nenhum aqui).
    private IEnumerator GuiarRemocaoDoSlot(ItemSlot slot, RectTransform pecaErrada, RectTransform areaLivre)
    {
        spotlight.Show();
        spotlight.SetInstant(pecaErrada);
        yield return new WaitForSeconds(pauseInicial);

        Transform tipoTransform = pecaErrada.Find("Canvas/Tipo");
        Image pecaImage = tipoTransform != null ? tipoTransform.GetComponent<Image>() : null;
        ghostPiece.SetSprite(pecaImage != null ? pecaImage.sprite : null);
        ghostPiece.SetWorldPosition(pecaErrada.position);
        ghostPiece.Show();

        handPointer.SetWorldPosition(pecaErrada.position);
        handPointer.Show();
        yield return new WaitForSeconds(0.25f);

        // Anima mao + fantasma "puxando" a peca errada de volta para a area livre
        Vector3 origem = pecaErrada.position;
        Vector3 destino = areaLivre.position;
        float t = 0f;
        while (t < handMoveDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / handMoveDuration));
            Vector3 pos = Vector3.Lerp(origem, destino, k);

            handPointer.SetWorldPosition(pos);
            ghostPiece.SetWorldPosition(pos);
            spotlight.SetInstantAtWorldPos(pos, spotlightRadiusPixelsDurantePasso);
            yield return null;
        }
        handPointer.SetWorldPosition(destino);
        ghostPiece.SetWorldPosition(destino);
        spotlight.SetInstantAtWorldPos(destino, spotlightRadiusPixelsDurantePasso);

        yield return StartCoroutine(handPointer.PlayTap());

        handPointer.Hide();
        ghostPiece.Hide();
        yield return StartCoroutine(spotlight.FadeOut(0.3f));

        // Espera o jogador realmente tirar a peca errada do slot
        yield return new WaitUntil(() => slot.PecaAtual == null);
    }

    private RectTransform EncontrarPecaNumero(int numero)
    {
        foreach (var obj in levelManager.TodasPecas)
        {
            if (obj == null) continue;
            var d = obj.GetComponent<DragDrop>();
            if (d != null && d.tipoPeca == TipoPeca.Numero && (int)d.valorNumero == numero)
                return obj.GetComponent<RectTransform>();
        }
        return null;
    }

    private RectTransform EncontrarProximaComidaDisponivel()
    {
        var comidaCorreta = levelManager.ComidaCorreta;
        RectTransform slotComidaRect = slotComida.GetComponent<RectTransform>();

        foreach (var obj in levelManager.TodasPecas)
        {
            if (obj == null) continue;
            var d = obj.GetComponent<DragDrop>();
            if (d == null || d.tipoPeca != TipoPeca.Comida || d.valorComida != comidaCorreta)
                continue;

            var rt = obj.GetComponent<RectTransform>();
            if (rt.parent == slotComidaRect) continue; // ja esta no slot

            return rt;
        }
        return null;
    }

    private List<RectTransform> EncontrarPecasComida(int quantidade)
    {
        var resultado = new List<RectTransform>();
        var comidaCorreta = levelManager.ComidaCorreta;
        foreach (var obj in levelManager.TodasPecas)
        {
            if (resultado.Count >= quantidade) break;
            if (obj == null) continue;
            var d = obj.GetComponent<DragDrop>();
            if (d != null && d.tipoPeca == TipoPeca.Comida && d.valorComida == comidaCorreta)
                resultado.Add(obj.GetComponent<RectTransform>());
        }
        return resultado;
    }

    private const string CHAVE_TUTORIAL_JOGADO = "TutorialJogado";

    /// Marca no PlayerPrefs que o jogador ja completou o tutorial. O
    /// MenuManager le essa mesma chave para decidir se manda o jogador
    /// direto para o jogo ou de volta para o tutorial.
    public void MarcarTutorialComoJogado()
    {
        PlayerPrefs.SetInt(CHAVE_TUTORIAL_JOGADO, 1);
        PlayerPrefs.Save();
    }
}