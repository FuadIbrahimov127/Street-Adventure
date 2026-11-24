using System.Collections.Generic;
using UnityEngine;

// Danışıla və ya istifadə oluna bilən hər şeyin əsası.
public abstract class MMInteractable : MonoBehaviour
{
    static readonly List<MMInteractable> all = new List<MMInteractable>();
    public GameObject Prompt;          // "E" işarəsi
    public Vector2 InteractOffset = new Vector2(0, 0.4f);
    public float RadiusBonus = 0f;     // böyük obyektlər üçün əlavə məsafə

    protected virtual void OnEnable() { all.Add(this); }
    protected virtual void OnDisable() { all.Remove(this); }

    public virtual bool CanInteract => true;

    public static MMInteractable FindNearest(Vector2 pos, float radius)
    {
        MMInteractable best = null;
        float bestD = float.MaxValue;
        foreach (var it in all)
        {
            if (!it.CanInteract) continue;
            float r = radius + it.RadiusBonus;
            float d = ((Vector2)it.transform.position + it.InteractOffset - pos).sqrMagnitude;
            if (d <= r * r && d < bestD) { bestD = d; best = it; }
        }
        return best;
    }

    public void SetHighlight(bool on)
    {
        if (Prompt != null) Prompt.SetActive(on);
    }

    public abstract void Interact(MMPlayer player);
}
