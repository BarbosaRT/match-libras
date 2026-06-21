using System.Collections;
using UnityEngine;

public class IntroAnimator : MonoBehaviour
{

    [Header("Peças LIBRAS")]
    public RectTransform[] piecesLIBRAS;


    [Header("Configurações")]
    public float slideDistance = 700f;
    public float meetDuration = 0.55f;
    public float meetDelay = 0.2f;
    public float atchStagger = 0.06f;

    public float librasDelay = 1.15f;
    public float librasRise = 120f;
    public float librasDuration = 0.4f;
    public float librasStagger = 0.09f;


    void Start() => StartCoroutine(PlayIntro());

    IEnumerator PlayIntro()
    {
        
        foreach (var p in piecesLIBRAS) Hide(p);
        yield return new WaitForSeconds(librasDelay - meetDelay);

        for (int i = 0; i < piecesLIBRAS.Length; i++)
            StartCoroutine(RiseUp(piecesLIBRAS[i], -librasRise, 0f, librasDuration, i * librasStagger));

      
    }

    IEnumerator SlideX(RectTransform rt, float fromX, float toX, float dur, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetAlpha(rt, 0f);
        SetAnchoredX(rt, fromX);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = EaseOutExpo(t / dur);
            SetAnchoredX(rt, Mathf.LerpUnclamped(fromX, toX, p));
            SetAlpha(rt, Mathf.Clamp01(t / (dur * 0.2f)));
            yield return null;
        }
        SetAnchoredX(rt, toX);
        SetAlpha(rt, 1f);
    }

    IEnumerator RiseUp(RectTransform rt, float fromY, float toY, float dur, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetAlpha(rt, 0f);
        SetAnchoredY(rt, fromY);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = EaseOutBack(t / dur);
            SetAnchoredY(rt, Mathf.LerpUnclamped(fromY, toY, p));
            SetAlpha(rt, Mathf.Clamp01(t / (dur * 0.25f)));
            yield return null;
        }
        SetAnchoredY(rt, toY);
        SetAlpha(rt, 1f);
    }

    float EaseOutExpo(float x) => x >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * x);

    float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    void Hide(RectTransform rt) => SetAlpha(rt, 0f);

    void SetAlpha(RectTransform rt, float a)
    {
        var cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = a;
    }

    void SetAnchoredX(RectTransform rt, float x)
    {
        var p = rt.anchoredPosition; p.x = x; rt.anchoredPosition = p;
    }

    void SetAnchoredY(RectTransform rt, float y)
    {
        var p = rt.anchoredPosition; p.y = y; rt.anchoredPosition = p;
    }
}