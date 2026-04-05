using UnityEngine;

public class TileFlip : MonoBehaviour
{
    private float _delay;
    private bool _flipped;

    public Texture2D TopTexture { get; private set; }
    public Texture2D BottomTexture { get; private set; }

    private MeshRenderer _topRend;
    private MeshRenderer _bottomRend;

    // Rotation pivot sits at the center of the cube
    // We rotate the whole GO from 0 → -180 on X axis:
    //   0°   = top face facing camera (current scene)
    //   90°  = side faces visible
    //   180° = bottom face facing camera (new scene)

    public void Init(Vector3 position, float width, float height, float depth,
                     Texture2D frontTexture, float delay, int layer)
    {
        transform.position = position;
        _delay = delay;
        TopTexture = frontTexture;

        Color sideColor = new Color(0xa3 / 255f, 0x5c / 255f, 0x4c / 255f);

        // Top face (current scene) — starts facing the camera
        _topRend = CreateFace("Top", new Vector3(0, height * 0.5f, 0),
                                 Quaternion.Euler(90f, 0, 0), width, depth, layer);
        SetTexture(_topRend, frontTexture);

        // Bottom face (new scene) — starts facing away
        _bottomRend = CreateFace("Bottom", new Vector3(0, -height * 0.5f, 0),
                                 Quaternion.Euler(-90f, 0, 0), width, depth, layer);
        SetColor(_bottomRend, Color.black); // placeholder until new scene loads

        // Front face
        CreateFace("Front", new Vector3(0, 0, depth * 0.5f),
                   Quaternion.identity, width, height, layer, sideColor);

        // Back face
        CreateFace("Back", new Vector3(0, 0, -depth * 0.5f),
                   Quaternion.Euler(0, 180f, 0), width, height, layer, sideColor);

        // Left face
        CreateFace("Left", new Vector3(-width * 0.5f, 0, 0),
                   Quaternion.Euler(0, -90f, 0), depth, height, layer, sideColor);

        // Right face
        CreateFace("Right", new Vector3(width * 0.5f, 0, 0),
                   Quaternion.Euler(0, 90f, 0), depth, height, layer, sideColor);
    }

    public void SetBottomTexture(Texture2D newSceneTex)
    {
        // Slice the new scene texture at this tile's screen position
        Camera cam = Camera.main;
        float camH = 2f * cam.orthographicSize;
        float camW = camH * cam.aspect;
        Vector3 camP = cam.transform.position;

        float u = (transform.position.x - (camP.x - camW * 0.5f)) / camW;
        float v = (transform.position.y - (camP.y - camH * 0.5f)) / camH;

        int pixW = Mathf.Max(1, Mathf.FloorToInt(newSceneTex.width * (GetFaceWidth() / camW)));
        int pixH = Mathf.Max(1, Mathf.FloorToInt(newSceneTex.height * (GetFaceHeight() / camH)));
        int pixX = Mathf.Clamp(Mathf.FloorToInt(u * newSceneTex.width - pixW * 0.5f), 0, newSceneTex.width - pixW);
        int pixY = Mathf.Clamp(Mathf.FloorToInt(v * newSceneTex.height - pixH * 0.5f), 0, newSceneTex.height - pixH);

        Color[] pixels = newSceneTex.GetPixels(pixX, pixY, pixW, pixH);
        BottomTexture = new Texture2D(pixW, pixH, TextureFormat.RGB24, false);
        BottomTexture.SetPixels(pixels);
        BottomTexture.Apply();

        // Bottom face needs to be flipped horizontally so it reads correctly when facing camera
        FlipTextureHorizontal(BottomTexture);

        SetTexture(_bottomRend, BottomTexture);
    }

    public void UpdateFlip(float globalProgress)
    {
        float localProgress = Mathf.Clamp01((globalProgress - _delay) / (1f - _delay));

        // Rotate X from 0 → -180: top face flips down to reveal bottom face
        float angle = Mathf.Lerp(0f, -180f, localProgress);
        transform.rotation = Quaternion.Euler(angle, 0f, 0f);
    }

    // --- helpers ---

    float GetFaceWidth()
    {
        var faces = GetComponentsInChildren<MeshFilter>();
        if (faces.Length > 0)
        {
            var b = faces[0].mesh.bounds;
            return b.size.x;
        }
        return 1f;
    }

    float GetFaceHeight()
    {
        var faces = GetComponentsInChildren<MeshFilter>();
        if (faces.Length > 0)
        {
            var b = faces[0].mesh.bounds;
            return b.size.y;
        }
        return 1f;
    }

    MeshRenderer CreateFace(string faceName, Vector3 localPos, Quaternion localRot,
                             float width, float height, int layer,
                             Color? color = null)
    {
        GameObject go = new GameObject(faceName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.layer = layer;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        float hw = width * 0.5f;
        float hh = height * 0.5f;

        mesh.vertices = new Vector3[]
        {
            new Vector3(-hw, -hh, 0),
            new Vector3( hw, -hh, 0),
            new Vector3(-hw,  hh, 0),
            new Vector3( hw,  hh, 0),
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        // Unlit so lighting doesn't affect texture accuracy
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        mr.material = mat;

        if (color.HasValue)
            SetColor(mr, color.Value);

        return mr;
    }

    void SetTexture(MeshRenderer mr, Texture2D tex)
    {
        mr.material.mainTexture = tex;
    }

    void SetColor(MeshRenderer mr, Color color)
    {
        mr.material = new Material(Shader.Find("Unlit/Color"));
        mr.material.color = color;
    }

    void FlipTextureHorizontal(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w / 2; x++)
            {
                Color tmp = tex.GetPixel(x, y);
                tex.SetPixel(x, y, tex.GetPixel(w - 1 - x, y));
                tex.SetPixel(w - 1 - x, y, tmp);
            }
        }
        tex.Apply();
    }
}