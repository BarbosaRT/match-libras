using UnityEngine;

public class TileFlip : MonoBehaviour
{
    private float _delay;
    private float _tileW;
    private float _tileH;
    private int _col, _row, _columns, _rows;
    private int _screenW, _screenH;

    public Texture2D TopTexture { get; private set; }
    public Texture2D BottomTexture { get; private set; }

    private MeshRenderer _topRend;
    private MeshRenderer _bottomRend;

    static readonly Color SideColor = new Color(0xa3 / 255f, 0x5c / 255f, 0x4c / 255f);

    public void Init(Vector3 position, float width, float height, float depth,
                     Texture2D frontTexture, float delay,
                     int col, int row, int columns, int rows,
                     int screenW, int screenH)
    {
        transform.position = position;
        _delay = delay;
        _tileW = width;
        _tileH = height;
        _col = col;
        _row = row;
        _columns = columns;
        _rows = rows;
        _screenW = screenW;
        _screenH = screenH;
        TopTexture = frontTexture;

        float hw = width * 0.5f;
        float hh = height * 0.5f;
        float hd = depth * 0.5f;

        // Top face — faces camera (local +Y up = world -Z for camera forward)
        // For orthographic camera looking down -Z, top of cube = face at local Z- 
        _topRend = CreateFace("Top",
            new Vector3(0, 0, -hd),
            Quaternion.identity,
            width, height);
        SetTexture(_topRend, frontTexture);

        // Bottom face — faces away from camera (behind the cube)
        _bottomRend = CreateFace("Bottom",
            new Vector3(0, 0, hd),
            Quaternion.Euler(0f, 180f, 0f),
            width, height);
        SetColor(_bottomRend, Color.black); // placeholder

        // Four lateral faces
        // Front (+Y in world = top of screen, we use Y axis here)
        CreateFace("FaceTop",
            new Vector3(0, hh, 0),
            Quaternion.Euler(-90f, 0f, 0f),
            width, depth, SideColor);

        CreateFace("FaceBottom",
            new Vector3(0, -hh, 0),
            Quaternion.Euler(90f, 0f, 0f),
            width, depth, SideColor);

        CreateFace("FaceLeft",
            new Vector3(-hw, 0, 0),
            Quaternion.Euler(0f, -90f, 0f),
            depth, height, SideColor);

        CreateFace("FaceRight",
            new Vector3(hw, 0, 0),
            Quaternion.Euler(0f, 90f, 0f),
            depth, height, SideColor);
    }

    public void SetBottomTexture(Texture2D newSceneTex)
    {
        int pixTileW = Mathf.Max(1, Mathf.FloorToInt((float)_screenW / _columns));
        int pixTileH = Mathf.Max(1, Mathf.FloorToInt((float)_screenH / _rows));

        int pixX = Mathf.Min(_col * pixTileW, newSceneTex.width - pixTileW);
        int pixY = Mathf.Min(_row * pixTileH, newSceneTex.height - pixTileH);

        Color[] pixels = newSceneTex.GetPixels(pixX, pixY, pixTileW, pixTileH);
        BottomTexture = new Texture2D(pixTileW, pixTileH, TextureFormat.RGB24, false);
        BottomTexture.SetPixels(pixels);
        BottomTexture.Apply();

        SetTexture(_bottomRend, BottomTexture);
    }

    public void UpdateFlip(float globalProgress)
    {
        // localProgress goes 0→1 after this tile's delay
        float localProgress = Mathf.Clamp01((globalProgress - _delay) / (1f - _delay));

        // Rotate around X axis: 0° → 180°
        // At   0°: front face (-Z) faces camera ✓
        // At  90°: side faces visible ✓  
        // At 180°: back face (+Z) now faces camera ✓
        float angle = Mathf.Lerp(0f, 90f, localProgress);
        transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    MeshRenderer CreateFace(string faceName, Vector3 localPos, Quaternion localRot,
                             float width, float height, Color? color = null)
    {
        GameObject go = new GameObject(faceName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        float hw = width * 0.5f;
        float hh = height * 0.5f;

        Mesh mesh = new Mesh();
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

        Material mat;
        if (color.HasValue)
        {
            mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = color.Value;
        }
        else
        {
            mat = new Material(Shader.Find("Unlit/Texture"));
        }
        mr.material = mat;

        return mr;
    }

    void SetTexture(MeshRenderer mr, Texture2D tex)
    {
        mr.material = new Material(Shader.Find("Unlit/Texture"));
        mr.material.mainTexture = tex;
    }

    void SetColor(MeshRenderer mr, Color color)
    {
        mr.material = new Material(Shader.Find("Unlit/Color"));
        mr.material.color = color;
    }
}