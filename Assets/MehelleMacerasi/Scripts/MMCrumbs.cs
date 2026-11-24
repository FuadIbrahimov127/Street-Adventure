using System.Collections.Generic;
using UnityEngine;

// Yerə atılmış çörək qırıntıları: göyərçinləri cəlb edir, bir az sonra yox olur.
public class MMCrumbs : MonoBehaviour
{
    public static readonly List<MMCrumbs> All = new List<MMCrumbs>();
    public float Life = 12f;
    SpriteRenderer sr;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        var g = MMGame.I;
        if (g != null && g.WorldPaused) return;
        Life -= Time.deltaTime;
        if (sr != null && Life < 2f) { var c = sr.color; c.a = Mathf.Clamp01(Life / 2f); sr.color = c; }
        if (Life <= 0) Destroy(gameObject);
    }

    public static MMCrumbs Nearest(Vector3 p, float radius)
    {
        MMCrumbs best = null;
        float bd = radius * radius;
        foreach (var c in All)
        {
            float d = (c.transform.position - p).sqrMagnitude;
            if (d < bd) { bd = d; best = c; }
        }
        return best;
    }
}
