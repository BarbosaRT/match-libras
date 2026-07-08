using UnityEngine;
using System.Collections;

// Coloque este script num GameObject filho da Canvas de tutorial contendo o
// sprite da mao (pode ser o mesmo sprite usado no sinal de libras do numero 1).
public class TutorialHandPointer : MonoBehaviour
{
    [SerializeField] private RectTransform handRect;
    [SerializeField] private float tapScale = 0.85f;
    [SerializeField] private float tapDuration = 0.35f;

    [Header("Deslocamento visual")]
    [Tooltip("Desloca a mao em relacao a peca/fantasma (em pixels), tipo um dedo tocando a peca em vez de ficar exatamente em cima dela.")]
    [SerializeField] private Vector2 offsetPixels = new Vector2(45f, -45f);

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    public void SetWorldPosition(Vector3 worldPos)
    {
        if (handRect == null)
        {
            Debug.LogError("TutorialHandPointer: 'Hand Rect' nao foi atribuido no Inspector.", this);
            return;
        }
        // Aplica o offset na escala do proprio RectTransform da mao, para o
        // deslocamento em pixels ficar coerente com qualquer Canvas Scaler.
        Vector3 offset = new Vector3(offsetPixels.x, offsetPixels.y, 0f) * handRect.lossyScale.x;
        handRect.position = worldPos + offset;
    }

    /// Pequeno "toque" de confirmacao ao chegar no destino.
    public IEnumerator PlayTap()
    {
        if (handRect == null) yield break;

        Vector3 originalScale = handRect.localScale;
        float half = tapDuration * 0.5f;

        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            handRect.localScale = Vector3.Lerp(originalScale, originalScale * tapScale, t / half);
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            handRect.localScale = Vector3.Lerp(originalScale * tapScale, originalScale, t / half);
            yield return null;
        }
        handRect.localScale = originalScale;
    }
}
