using System.Collections.Generic;
using UnityEngine;

// Xəritə zonası (məs. "Məhəllə" və "Məktəb məhəlləsi").
// Yalnız oyunçunun olduğu zona aktivdir, o biri tamamilə söndürülür (oyun yüngül işləyir).
public class MMZone : MonoBehaviour
{
    public static readonly List<MMZone> All = new List<MMZone>();
    public string Id;
    public string Title;
    public GameObject Root;          // söndürülüb-yandırılan hissə (plitələr, obyektlər)
    public Rect Bounds;              // dünya koordinatlarında
    public Vector2 Origin => Bounds.position;

    void Awake() { if (!All.Contains(this)) All.Add(this); }
    void OnDestroy() { All.Remove(this); }

    public static MMZone Main => All.Count > 0 ? Get("main") ?? All[0] : null;

    public static MMZone Get(string id)
    {
        foreach (var z in All) if (z.Id == id) return z;
        return null;
    }

    public static MMZone Find(Vector2 p)
    {
        foreach (var z in All) if (z.Bounds.Contains(p)) return z;
        return null;
    }
}
