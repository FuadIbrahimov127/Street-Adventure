using System.Collections.Generic;
using UnityEngine;

// Bellənmiş bir lək (MMGarden yaradır): toxum ək [F/E], su qabı ilə sula [F], yetişəndə yığ [E].
public class MMPlot : MMInteractable
{
    public static readonly List<MMPlot> All = new List<MMPlot>();
    public MMPlotData Data;
    public SpriteRenderer Soil, PlantBody;

    public string Crop { get => Data.Crop; set => Data.Crop = value; }
    public int Days { get => Data.Days; set => Data.Days = value; }
    public bool Watered { get => Data.Watered; set => Data.Watered = value; }

    public bool Empty => string.IsNullOrEmpty(Crop);
    MMCropDef Def => MMGarden.Crop(Crop);
    public bool Ripe => !Empty && Def != null && Days >= Def.Days;

    protected override void OnEnable() { base.OnEnable(); All.Add(this); }
    protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

    public static MMPlot Nearest(Vector2 p, float radius)
    {
        MMPlot best = null;
        float bd = radius * radius;
        foreach (var x in All)
        {
            float d = ((Vector2)x.transform.position + new Vector2(0, 0.5f) - p).sqrMagnitude;
            if (d < bd) { bd = d; best = x; }
        }
        return best;
    }

    int Stage
    {
        get
        {
            var d = Def;
            if (d == null || Days <= 0) return 0;
            if (Days >= d.Days) return 3;
            return Mathf.Clamp(Days * 3 / d.Days, 1, 2);
        }
    }

    public void Refresh()
    {
        var gdn = MMGarden.I;
        if (Data == null) return;
        if (Soil != null && gdn != null) Soil.sprite = Watered ? gdn.SoilWet : gdn.SoilDry;
        if (PlantBody == null) return;
        var d = Def;
        PlantBody.enabled = d != null;
        if (d != null && d.Stages != null && d.Stages.Length == 4) PlantBody.sprite = d.Stages[Stage];
    }

    public void Clear()
    {
        Crop = ""; Days = 0;
        Refresh();
    }

    public void Plant(string seed)
    {
        var g = MMGame.I;
        var d = MMGarden.CropOfSeed(seed);
        if (d == null || MMInventory.I == null) return;
        if (MMGarden.Frozen) { g.UI.Toast("Qışda torpaq donub. Yaz gələndə ək!"); return; }
        if (!MMInventory.I.Remove(seed)) return;
        Crop = d.Id; Days = 0;
        Refresh();
        MMAudio.Play("dig");
        g.UI.Toast(Watered ? $"{d.Name} əkdin! Torpaq yaşdır. {d.Days} günə yetişəcək, hər gün sula."
                           : $"{d.Name} əkdin! İndi su qabı ilə sula [F]. {d.Days} günə yetişəcək.");
        g.SaveGame();
    }

    public void Water()
    {
        Watered = true;
        Refresh();
    }

    public override void Interact(MMPlayer player)
    {
        var g = MMGame.I;
        var inv = MMInventory.I;
        var d = Def;
        if (Ripe)
        {
            if (inv == null || !inv.Add(d.Id)) { g.UI.Toast("Çantan doludur! Əvvəlcə bir şey sat və ya işlət."); return; }
            Clear();
            MMAudio.Play("lucky");
            MMAchievements.Add("harvest");
            g.UI.Toast($"{d.Name} yığdın! Satış qutusunda və ya Gülnarə xalaya sat.");
            g.UI.Refresh();
            g.SaveGame();
            return;
        }
        if (Empty)
        {
            var sel = inv != null ? inv.Slots[inv.Selected] : null;
            if (sel != null && !sel.Empty && MMGarden.CropOfSeed(sel.Id) != null) { Plant(sel.Id); return; }
            g.UI.Toast(MMGarden.Frozen ? "Qışda torpaq donub. Yaz gələndə ək!" :
                       Watered ? "Bellənmiş, sulanmış torpaq. Toxumu seç (1-7) və [E] bas." : "Bellənmiş torpaq. Su qabı ilə sula, sonra toxum ək.");
            return;
        }
        string state = MMGarden.Frozen ? "Qışda böyümür, yazı gözləyir." : Watered ? "Bu gün sulanıb. Yatandan sonra böyüyəcək." : "Bu gün sulanmayıb! Su qabı ilə sula [F].";
        g.UI.Toast($"{d.Name}: {Mathf.Min(Days, d.Days)}/{d.Days} gün. {state}");
    }
}
