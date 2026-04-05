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
        // 1. Capture current screen
        yield return new WaitForEndOfFrame();

        Texture2D screenTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTex.Apply();

        // 2. Store main cam reference before scene changes
        Camera mainCam = Camera.main;
        float camHeight = 2f * mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        float tileW = camWidth / columns;
        float tileH = camHeight / rows;
        float tileD = (tileW + tileH) * 0.5f;

        // Z: place cubes just in front of camera, well within clip range
        // For orthographic: anything between nearClip and farClip is visible
        // We place at nearClip + small offset so they render in front of scene
        float tileZ = mainCam.transform.position.z + mainCam.nearClipPlane + tileD + 0.1f;

        // 3. Load next scene — cubes will cover the transition
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextScene);
        asyncLoad.allowSceneActivation = true;

        // 4. Create cubes BEFORE scene unloads so we have cam data
        List<TileFlip> tiles = CreateTiles(mainCam, screenTex, tileW, tileH, tileD, tileZ);

        // 5. Wait for new scene to fully load and render two frames
        yield return new WaitUntil(() => asyncLoad.isDone);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // 6. Capture new scene for bottom faces
        Texture2D newSceneTex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        newSceneTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        newSceneTex.Apply();

        foreach (var tile in tiles)
            tile.SetBottomTexture(newSceneTex);

        // 7. Animate
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            foreach (var tile in tiles)
                tile.UpdateFlip(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);

        // 8. Cleanup
        foreach (var tile in tiles)
            Destroy(tile.gameObject);

        Destroy(screenTex);
        Destroy(newSceneTex);
        Destroy(gameObject);
    }

    List<TileFlip> CreateTiles(Camera cam, Texture2D screenTex,
                                float tileW, float tileH, float tileD, float tileZ)
    {
        var tiles = new List<TileFlip>();

        float camHeight = 2f * cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        Vector3 camPos = cam.transform.position;
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

                Vector3 pos = new Vector3(
                    startX + col * tileW,
                    startY + row * tileH,
                    tileZ
                );

                GameObject go = new GameObject($"Tile_{col}_{row}");
                DontDestroyOnLoad(go);

                TileFlip tile = go.AddComponent<TileFlip>();
                tile.Init(pos, tileW, tileH, tileD, tileTex, delay,
                          col, row, columns, rows, screenTex.width, screenTex.height);
                tiles.Add(tile);
            }
        }

        return tiles;
    }
}