using System.Collections.Generic;
using UnityEngine;

// Suvarılacaq gül ləki: su qabı ilə suvaranda güllər açılır.
public class MMFlowerbed : MonoBehaviour
{
    public static readonly List<MMFlowerbed> All = new List<MMFlowerbed>();
    public string Id;
    public SpriteRenderer Body;
    public Sprite DrySprite, BloomSprite;
    public bool Bloomed { get; private set; }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        var g = MMGame.I;
        if (g != null && g.Collected.Contains(Id)) SetBloom();
    }

    public void SetBloom()
    {
        Bloomed = true;
        if (Body != null && BloomSprite != null) Body.sprite = BloomSprite;
    }

    public static MMFlowerbed Nearest(Vector2 p, float radius)
    {
        MMFlowerbed best = null;
        float bd = radius * radius;
        foreach (var b in All)
        {
            float d = ((Vector2)b.transform.position + new Vector2(0, 0.35f) - p).sqrMagnitude;
            if (d < bd) { bd = d; best = b; }
        }
        return best;
    }
}
