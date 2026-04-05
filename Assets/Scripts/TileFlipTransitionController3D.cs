using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TileFlipTransitionController3D : MonoBehaviour
{
    [Header("Transition Settings")]
    public string nextScene = "GameScene";
    public float duration = 2f;
    public int columns = 8;
    public int rows = 12;
    [Range(0f, 1f)]
    public float stagger = 0.4f;
    public bool checkerPattern = true;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void PlayTransition()
    {
        StartCoroutine(DoTransition());
    }

    IEnumerator DoTransition()
    {
        yield return new WaitForEndOfFrame();

        // 1. Capture current screen
        Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTex.Apply();

        // 2. Create a dedicated camera for the tiles so they always render on top
        GameObject camGO = new GameObject("TileCam");
        DontDestroyOnLoad(camGO);
        Camera tileCam = camGO.AddComponent<Camera>();
        tileCam.clearFlags = CameraClearFlags.Depth;
        tileCam.cullingMask = LayerMask.GetMask("TransitionTiles");
        tileCam.depth = 99;
        tileCam.orthographic = false;

        // 3. Make sure the layer exists at runtime (add "TransitionTiles" layer in Project Settings)
        int tileLayer = LayerMask.NameToLayer("TransitionTiles");

        // 4. Load next scene immediately — tiles will cover the swap
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextScene);
        asyncLoad.allowSceneActivation = true;

        // 5. Create all tile cubes
        Camera mainCam = Camera.main;
        List<TileFlip> tiles = CreateTiles(mainCam, screenTex, tileLayer);

        // 6. Wait for new scene to fully load and render
        yield return new WaitUntil(() => asyncLoad.isDone);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // 7. Capture new scene for bottom face
        Texture2D newSceneTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        newSceneTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        newSceneTex.Apply();

        foreach (var tile in tiles)
            tile.SetBottomTexture(newSceneTex);

        // 8. Animate
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float globalProgress = Mathf.Clamp01(elapsed / duration);

            foreach (var tile in tiles)
                tile.UpdateFlip(globalProgress);

            yield return null;
        }

        // 9. Hold last frame briefly so it doesn't pop
        yield return new WaitForSeconds(0.05f);

        // 10. Cleanup
        foreach (var tile in tiles)
        {
            Destroy(tile.TopTexture);
            Destroy(tile.BottomTexture);
            Destroy(tile.gameObject);
        }

        Destroy(screenTex);
        Destroy(newSceneTex);
        Destroy(camGO);
        Destroy(gameObject);
    }

    List<TileFlip> CreateTiles(Camera cam, Texture2D screenTex, int tileLayer)
    {
        var tiles = new List<TileFlip>();

        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float tileW = camWidth / columns;
        float tileH = camHeight / rows;
        float tileD = (tileW + tileH) * 0.5f;

        Vector3 camPos = cam.transform.position;

        // Place cubes just past the near clip plane so they're guaranteed visible
        float zOffset = cam.nearClipPlane + tileD * 0.5f + 0.01f;

        float startX = camPos.x - camWidth * 0.5f + tileW * 0.5f;
        float startY = camPos.y - camHeight * 0.5f + tileH * 0.5f;

        int pixTileW = Mathf.Max(1, Mathf.FloorToInt((float)screenTex.width / columns));
        int pixTileH = Mathf.Max(1, Mathf.FloorToInt((float)screenTex.height / rows));

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float delay = checkerPattern
                    ? ((col + row) % 2) * stagger
                    : ((float)col / columns + (float)row / rows) * 0.5f * stagger;

                int pixX = Mathf.Min(col * pixTileW, screenTex.width - pixTileW);
                int pixY = Mathf.Min(row * pixTileH, screenTex.height - pixTileH);

                Color[] pixels = screenTex.GetPixels(pixX, pixY, pixTileW, pixTileH);
                Texture2D tileTex = new Texture2D(pixTileW, pixTileH, TextureFormat.RGB24, false);
                tileTex.SetPixels(pixels);
                tileTex.Apply();

                // Z is in camera's forward direction, not world Z
                Vector3 pos = camPos + cam.transform.forward * zOffset
                            + cam.transform.right * (startX - camPos.x + col * tileW)
                            + cam.transform.up * (startY - camPos.y + row * tileH);

                GameObject go = new GameObject($"Tile_{col}_{row}");
                DontDestroyOnLoad(go);
                TileFlip tile = go.AddComponent<TileFlip>();
                tile.Init(pos, tileW, tileH, tileD, tileTex, delay, tileLayer);
                tiles.Add(tile);
            }
        }

        return tiles;
    }
}