using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Coloque este script numa Image full-screen (stretch 0,0 -> 1,1) que usa o
// material criado a partir do shader "UI/TutorialSpotlight".
[RequireComponent(typeof(Image))]
public class TutorialSpotlight : MonoBehaviour
{
    [Header("Necessario para Canvas em modo Screen Space - Camera")]
    [Tooltip("Arraste aqui a mesma camera configurada em Canvas > Render Camera")]
    [SerializeField] private Camera uiCamera;

    private Image image;
    private Material materialInstance;

    private static readonly int CenterProp = Shader.PropertyToID("_Center");
    private static readonly int RadiusProp = Shader.PropertyToID("_Radius");
    private static readonly int AspectProp = Shader.PropertyToID("_Aspect");

    void Awake()
    {
        image = GetComponent<Image>();
        // Instancia o material para nao alterar o asset original.
        materialInstance = Instantiate(image.material);
        image.material = materialInstance;
        materialInstance.SetFloat(AspectProp, (float)Screen.width / Screen.height);

        // Comeca invisivel e sem bloquear clique/drag do jogo por baixo.
        // Show() e quem liga isso quando o tutorial realmente precisa dele.
        image.raycastTarget = false;
        var c0 = image.color;
        image.color = new Color(c0.r, c0.g, c0.b, 0f);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, 1f);
    }

    public IEnumerator FadeOut(float duration)
    {
        float startAlpha = image.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t / duration));
            var c = image.color;
            image.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        var c2 = image.color;
        image.color = new Color(c2.r, c2.g, c2.b, 0f);
        gameObject.SetActive(false);
    }

    /// Posiciona o spotlight instantaneamente sobre um alvo (sem animar).
    public void SetInstant(RectTransform target, float paddingPixels = 24f)
    {
        Vector2 screenPos = WorldToScreen(CenterOf(target));
        float radiusPixels = RadiusOf(target, paddingPixels);
        SetInstantAtScreenPosition(screenPos, radiusPixels);
    }

    /// Posiciona o spotlight instantaneamente numa posicao livre do mundo
    /// (usado para "seguir" a mao/peca durante uma demonstracao de arrasto).
    public void SetInstantAtWorldPos(Vector3 worldPos, float radiusPixels)
    {
        SetInstantAtScreenPosition(WorldToScreen(worldPos), radiusPixels);
    }

    private void SetInstantAtScreenPosition(Vector2 screenPos, float radiusPixels)
    {
        Vector2 uvCenter = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
        float uvRadius = radiusPixels / Screen.height;
        materialInstance.SetVector(CenterProp, uvCenter);
        materialInstance.SetFloat(RadiusProp, uvRadius);
    }

    /// Anima o spotlight do estado atual ate cobrir o novo alvo (usado so
    /// para o salto inicial sinal-de-libras -> numero na tela).
    public IEnumerator MoveTo(RectTransform target, float duration, float paddingPixels = 24f)
    {
        Vector4 startVec = materialInstance.GetVector(CenterProp);
        Vector2 startCenter = new Vector2(startVec.x, startVec.y);
        float startRadius = materialInstance.GetFloat(RadiusProp);

        Vector2 endCenter = new Vector2(WorldToScreen(CenterOf(target)).x / Screen.width, WorldToScreen(CenterOf(target)).y / Screen.height);
        float endRadius = RadiusOf(target, paddingPixels) / Screen.height;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            Vector2 c = Vector2.Lerp(startCenter, endCenter, k);
            float r = Mathf.Lerp(startRadius, endRadius, k);
            materialInstance.SetVector(CenterProp, c);
            materialInstance.SetFloat(RadiusProp, r);
            yield return null;
        }
        materialInstance.SetVector(CenterProp, endCenter);
        materialInstance.SetFloat(RadiusProp, endRadius);
    }

    public Vector2 WorldToScreen(Vector3 worldPos) => RectTransformUtility.WorldToScreenPoint(uiCamera, worldPos);

    private Vector3 CenterOf(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    private float RadiusOf(RectTransform target, float paddingPixels)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 min = WorldToScreen(corners[0]);
        Vector2 max = WorldToScreen(corners[2]);
        return Vector2.Distance(min, max) * 0.5f + paddingPixels;
    }
}
