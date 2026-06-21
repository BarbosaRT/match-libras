using System.Collections;
using UnityEngine;

/// <summary>
/// Shake de câmera. Anexe na câmera atribuída ao campo "Render Camera" do seu
/// Canvas (Screen Space - Camera). Tremer essa câmera desloca a UI inteira,
/// já que o Canvas é renderizado como um plano na frente dela.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [Header("Rotação (opcional, dá uma sensação de 'impacto')")]
    public bool incluirRotacao = true;
    public float rotacaoMagnitudeMultiplicador = 1.5f;

    private Vector3 posicaoOriginal;
    private Quaternion rotacaoOriginal;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        Instance = this;
        posicaoOriginal = transform.localPosition;
        rotacaoOriginal = transform.localRotation;
    }

    /// <summary>
    /// Dispara um shake. Pode ser chamado várias vezes seguidas — cada
    /// chamada reinicia o shake atual com os novos parâmetros.
    /// magnitude é em unidades de mundo (não pixels) — comece com algo
    /// pequeno, tipo 0.1 a 0.3, e ajuste conforme o tamanho da sua cena.
    /// </summary>
    public void Shake(float duracao = 0.3f, float magnitude = 0.2f)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeCoroutine(duracao, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duracao, float magnitude)
    {
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime; // funciona mesmo com o jogo pausado
            float forcaAtual = magnitude * (1f - tempo / duracao); // decai com o tempo

            Vector2 offset = Random.insideUnitCircle * forcaAtual;
            transform.localPosition = posicaoOriginal + new Vector3(offset.x, offset.y, 0f);

            if (incluirRotacao)
            {
                float anguloZ = Random.Range(-1f, 1f) * forcaAtual * rotacaoMagnitudeMultiplicador * 50f;
                transform.localRotation = rotacaoOriginal * Quaternion.Euler(0f, 0f, anguloZ);
            }

            yield return null;
        }

        transform.localPosition = posicaoOriginal;
        transform.localRotation = rotacaoOriginal;
        shakeCoroutine = null;
    }
}