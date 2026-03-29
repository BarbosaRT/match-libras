using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    // How many world units wide you want the play area to be
    // Set this to match your table design — e.g. 10 means the table
    // spans from x = -5 to x = 5
    public float targetWorldWidth = 10f;

    void Awake()
    {
        FitCameraToWidth();
    }

    void FitCameraToWidth()
    {
        Camera cam = GetComponent<Camera>();

        // aspect = screen width / screen height  (e.g. 0.46 for 9:19.5)
        float aspect = (float)Screen.width / Screen.height;

        // orthographicSize is always HALF the visible height in world units
        // so: visibleWidth = orthographicSize * 2 * aspect
        // solving for orthographicSize: size = (targetWidth / 2) / aspect
        cam.orthographicSize = (targetWorldWidth / 2f) / aspect;
    }
}