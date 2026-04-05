using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TileFlipTransitionController : MonoBehaviour
{
    [Header("Transition Settings")]
    public string nextScene = "GameScene";
    public float duration = 1.5f;
    public int columns = 8;
    public int rows = 6;
    [Range(0f, 0.6f)]
    public float stagger = 0.4f;
    public Material transitionMaterial;

    void Awake()
    {
        // O controller sobrevive junto com o canvas
        DontDestroyOnLoad(gameObject);
    }

    public void PlayTransition()
    {
        StartCoroutine(DoTransition());
    }

    IEnumerator DoTransition()
    {
        // 1. Captura a tela atual
        yield return new WaitForEndOfFrame();

        Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTex.Apply();

        // 2. Cria o overlay antes de trocar a cena
        Canvas canvas = CreateOverlayCanvas();
        RawImage rawImage = canvas.GetComponentInChildren<RawImage>();

        Material mat = new Material(transitionMaterial);
        rawImage.texture = screenTex;
        rawImage.material = mat;

        mat.SetFloat("_Cols", columns);
        mat.SetFloat("_Rows", rows);
        mat.SetFloat("_Stagger", stagger);
        mat.SetFloat("_Progress", 0f);

        // 3. Ativa a nova cena — o overlay esconde a troca
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextScene);
        asyncLoad.allowSceneActivation = true;

        // 4. Espera carregar
        yield return new WaitUntil(() => asyncLoad.isDone);

        // 5. Anima — agora rodando na nova cena, coroutine não morre
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mat.SetFloat("_Progress", Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        mat.SetFloat("_Progress", 1f);
        yield return new WaitForEndOfFrame();

        // 6. Limpa tudo incluindo o próprio controller
        Destroy(canvas.gameObject);
        Destroy(screenTex);
        Destroy(mat);
        Destroy(gameObject); // remove o controller da nova cena
    }

    Canvas CreateOverlayCanvas()
    {
        GameObject go = new GameObject("TransitionOverlay");
        DontDestroyOnLoad(go);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        GameObject imgGO = new GameObject("Image");
        imgGO.transform.SetParent(go.transform, false);

        RawImage img = imgGO.AddComponent<RawImage>();
        RectTransform rect = img.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return canvas;
    }
}