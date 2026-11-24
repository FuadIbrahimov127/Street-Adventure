using UnityEngine;

// Kamera oyunçunu izləyir, xəritədən çıxmır və pikselləri tam ədəd miqyasla göstərir.
public class MMCameraFollow : MonoBehaviour
{
    public Transform Target;
    public Vector2 MapMin = Vector2.zero, MapMax = new Vector2(48, 36);
    public float Sharpness = 7f;
    public int PixelsPerUnit = 16;
    public int ReferenceHeight = 180;   // ekranda ən azı bu qədər oyun pikseli görünür
    static float shake;
    Camera cam;
    Vector3 smooth;
    int zoom = 1;

    public static void Shake(float t) { shake = Mathf.Max(shake, t); }

    // kameranın sərhədlərini dəyiş (məs. evə girəndə) və dərhal yeni yerə keç
    public void SetBounds(Vector2 min, Vector2 max)
    {
        MapMin = min; MapMax = max;
        if (cam == null) cam = GetComponent<Camera>();
        if (Target != null)
        {
            smooth = Clamp(Target.position + new Vector3(0, 0.6f, 0));
            transform.position = Snap(smooth);
        }
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        UpdateZoom();
    }

    void Start()
    {
        smooth = Target != null ? Clamp(Target.position + new Vector3(0, 0.6f, 0)) : transform.position;
        transform.position = Snap(smooth);
    }

    void UpdateZoom()
    {
        zoom = Mathf.Max(1, Mathf.FloorToInt(Screen.height / (float)ReferenceHeight));
        cam.orthographicSize = Screen.height / (2f * PixelsPerUnit * zoom);
    }

    Vector3 Clamp(Vector3 p)
    {
        float h = cam.orthographicSize, w = h * cam.aspect;
        p.x = MapMax.x - MapMin.x <= 2 * w ? (MapMin.x + MapMax.x) / 2 : Mathf.Clamp(p.x, MapMin.x + w, MapMax.x - w);
        p.y = MapMax.y - MapMin.y <= 2 * h ? (MapMin.y + MapMax.y) / 2 : Mathf.Clamp(p.y, MapMin.y + h, MapMax.y - h);
        p.z = -10f;
        return p;
    }

    Vector3 Snap(Vector3 p)
    {
        float s = PixelsPerUnit * zoom;
        return new Vector3(Mathf.Round(p.x * s) / s, Mathf.Round(p.y * s) / s, -10f);
    }

    void LateUpdate()
    {
        UpdateZoom();
        if (Target == null) return;
        Vector3 goal = Clamp(Target.position + new Vector3(0, 0.6f, 0));
        smooth = Vector3.Lerp(smooth, goal, 1f - Mathf.Exp(-Sharpness * Time.deltaTime));
        Vector3 p = smooth;
        if (shake > 0)
        {
            shake -= Time.deltaTime;
            p += (Vector3)(Random.insideUnitCircle * 0.12f);
        }
        transform.position = Snap(p);
    }
}
