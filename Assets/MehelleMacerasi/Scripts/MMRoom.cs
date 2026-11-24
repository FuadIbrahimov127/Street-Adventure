using System.Collections.Generic;
using UnityEngine;

// Evin içi (otaq): kamera bu sərhədlərdə qalır, xəritədə oyunçu evin qapısında göstərilir.
public class MMRoom : MonoBehaviour
{
    public static readonly List<MMRoom> All = new List<MMRoom>();
    public string Id;
    public string Title;
    public Rect Bounds;            // dünya koordinatlarında
    public Vector2 MapPosition;    // xəritədəki yeri (evin qapısı)

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public static MMRoom Find(Vector2 p)
    {
        foreach (var r in All) if (r.Bounds.Contains(p)) return r;
        return null;
    }
}
