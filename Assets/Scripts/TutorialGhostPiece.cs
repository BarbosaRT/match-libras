using UnityEngine;
using UnityEngine.UI;

// Coloque num GameObject filho da Canvas de tutorial, com um componente Image.
// Ele copia o sprite da peca real e fica semi-transparente, seguindo a mao.
[RequireComponent(typeof(Image))]
public class TutorialGhostPiece : MonoBehaviour
{
    [SerializeField] private RectTransform ghostRect;
    [Range(0f, 1f)]
    [SerializeField] private float alfaFantasma = 0.5f;

    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        if (ghostRect == null) ghostRect = GetComponent<RectTransform>();
    }

    public void SetSprite(Sprite sprite)
    {
        image.sprite = sprite;
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, alfaFantasma);
        // Deixa o fantasma com o tamanho nativo do sprite (evita esticar).
        image.SetNativeSize();
    }

    public void SetWorldPosition(Vector3 worldPos)
    {
        ghostRect.position = worldPos;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}
