using System.Collections.Generic;
using UnityEngine;

// Fəslə görə sprite dəyişən obyekt (ağac, kol, bina, skamya).
// Variants[0] = yaz (əsas), 1 = yay, 2 = payız, 3 = qış. Boş olan fəsildə əsas sprite qalır.
public class MMSeasonal : MonoBehaviour
{
    public static readonly List<MMSeasonal> All = new List<MMSeasonal>();
    public SpriteRenderer Target;
    public Sprite[] Variants = new Sprite[4];

    void OnEnable()
    {
        All.Add(this);
        // zona yenidən yananda cari fəsli tətbiq et
        if (MMSeasons.I != null) Apply((int)MMSeasons.I.Season);
    }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        Apply(MMSeasons.I != null ? (int)MMSeasons.I.Season : 0);
    }

    public void Apply(int season)
    {
        if (Target == null || Variants == null || Variants.Length == 0) return;
        var sp = season < Variants.Length && Variants[season] != null ? Variants[season] : Variants[0];
        if (sp != null) Target.sprite = sp;
    }
}
