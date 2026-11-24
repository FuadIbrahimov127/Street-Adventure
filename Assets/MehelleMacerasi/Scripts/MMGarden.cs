using System.Collections.Generic;
using UnityEngine;

// Bitki növü: toxum, neçə günə yetişir, satış qiyməti, böyümə şəkilləri
public class MMCropDef
{
    public string Id, Name, Seed;
    public int Days, Price;
    public Sprite[] Stages;
}

// Bir lək haqqında məlumat (oyunçu başqa xəritədə olanda da yadda qalır və böyüyür)
[System.Serializable]
public class MMPlotData
{
    public Vector2Int Cell;
    public string Crop = "";
    public int Days;
    public bool Watered;
}

// Tarla: oyunçu bel ilə torpağı belləyir [F], su qabı ilə sulayır, toxum əkir.
// Sulanan bitki hər yeni gündə bir addım böyüyür. Yağış hər şeyi sulayır. Qışda torpaq donur.
public class MMGarden : MonoBehaviour
{
    public static MMGarden I;

    public Sprite SoilDry, SoilWet, PromptSprite, CursorSprite;
    public Sprite[] Dust = new Sprite[2];
    public Sprite[] Tomato = new Sprite[4], Carrot = new Sprite[4], Melon = new Sprite[4];
    public Transform Tap;                  // su kranı: su qabını doldurur
    public Transform PlotRoot;             // ləklərin şəkilləri bura yaradılır (tarlanın zonasında)
    public Vector2Int[] Tillable;          // bellənə bilən xanalar (dünya koordinatları)

    public readonly Dictionary<string, MMCropDef> Crops = new Dictionary<string, MMCropDef>();
    readonly HashSet<Vector2Int> tillable = new HashSet<Vector2Int>();
    readonly Dictionary<Vector2Int, MMPlotData> plots = new Dictionary<Vector2Int, MMPlotData>();
    readonly Dictionary<Vector2Int, MMPlot> views = new Dictionary<Vector2Int, MMPlot>();
    SpriteRenderer cursor;
    int tillCount;

    void Awake()
    {
        I = this;
        Crops["tomato"] = new MMCropDef { Id = "tomato", Name = "Pomidor", Seed = "seed_tomato", Days = 3, Price = 5, Stages = Tomato };
        Crops["carrot"] = new MMCropDef { Id = "carrot", Name = "Yerkökü", Seed = "seed_carrot", Days = 2, Price = 3, Stages = Carrot };
        Crops["melon"] = new MMCropDef { Id = "melon", Name = "Qarpız", Seed = "seed_melon", Days = 4, Price = 12, Stages = Melon };
        if (Tillable != null) foreach (var c in Tillable) tillable.Add(c);
        if (PlotRoot != null && CursorSprite != null)
        {
            var go = new GameObject("TileCursor");
            go.transform.SetParent(PlotRoot, false);
            cursor = go.AddComponent<SpriteRenderer>();
            cursor.sprite = CursorSprite; cursor.sortingOrder = -14; cursor.enabled = false;
        }
    }

    public static MMCropDef Crop(string id)
    {
        if (I == null || string.IsNullOrEmpty(id)) return null;
        return I.Crops.TryGetValue(id, out var d) ? d : null;
    }

    public static MMCropDef CropOfSeed(string seed)
    {
        if (I == null) return null;
        foreach (var d in I.Crops.Values) if (d.Seed == seed) return d;
        return null;
    }

    public static bool Frozen => MMSeasons.I != null && MMSeasons.I.IsWinter;
    public static Vector2Int CellOf(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

    // oyunçunun qabağındakı nöqtə (bellə, ək, sula)
    public static Vector2 TargetPoint(MMPlayer p) => (Vector2)p.transform.position + new Vector2(0, 0.3f) + p.Facing * 0.8f;

    public bool IsTillable(Vector2Int c) => tillable.Contains(c);
    public MMPlot ViewAt(Vector2Int c) => views.TryGetValue(c, out var v) ? v : null;

    void Update()
    {
        var g = MMGame.I;
        // yağış bütün tarlanı sulayır
        if (MMWeather.I != null && MMWeather.I.IsRaining)
            foreach (var d in plots.Values) if (!d.Watered) { d.Watered = true; Refresh(d.Cell); }
        UpdateCursor(g);
    }

    // seçilmiş əşya bel, toxum və ya su qabıdırsa, hədəf xananı göstər
    void UpdateCursor(MMGame g)
    {
        if (cursor == null) return;
        bool show = false;
        var inv = MMInventory.I;
        if (g != null && g.State == MMState.Playing && g.Player != null && g.CurrentRoom == null && inv != null)
        {
            var s = inv.Slots[inv.Selected];
            var def = s.Empty ? null : MMItems.Get(s.Id);
            if (def != null && (def.Use == MMItemUse.Hoe || def.Use == MMItemUse.Seed || def.Use == MMItemUse.Water))
            {
                var c = CellOf(TargetPoint(g.Player));
                if (tillable.Contains(c))
                {
                    show = true;
                    cursor.transform.position = new Vector3(c.x + 0.5f, c.y, 0);
                    bool ok = def.Use == MMItemUse.Hoe ? !plots.ContainsKey(c) : plots.ContainsKey(c);
                    cursor.color = ok ? new Color(1f, 1f, 0.85f, 1f) : new Color(1f, 0.55f, 0.45f, 0.8f);
                }
            }
        }
        cursor.enabled = show;
    }

    public void OnNewDay()
    {
        bool frozen = Frozen;
        foreach (var d in plots.Values)
        {
            var c = Crop(d.Crop);
            if (c != null && !frozen && d.Watered && d.Days < c.Days) d.Days++;
            d.Watered = false;
            Refresh(d.Cell);
        }
    }

    public bool NearTap(Vector2 p) => Tap != null && ((Vector2)Tap.position + new Vector2(0, 0.4f) - p).magnitude < 1.9f;

    // ---------------- belləmək ----------------
    public void TryTill(Vector2 target)
    {
        var g = MMGame.I;
        var c = CellOf(target);
        if (!tillable.Contains(c))
        {
            g.UI.Toast(g.CurrentZone != null && g.CurrentZone.Id == "school" ? "Burada belləmək olmaz. Hasarlı tarlanın içində bellə." : "Burada belləmək olmaz. Tarla Məktəb məhəlləsindədir: çayxananın yanındakı səki ilə sağa get.");
            return;
        }
        if (Frozen) { g.UI.Toast("Qışda torpaq donub, belləmək olmur. Yazı gözlə!"); return; }
        if (plots.TryGetValue(c, out var d))
        {
            g.UI.Toast(string.IsNullOrEmpty(d.Crop) ? "Bu torpaq artıq bellənib. İndi sula və toxum ək!" : "Burada bitki bitir. Boş yerdə bellə.");
            return;
        }
        d = new MMPlotData { Cell = c };
        plots[c] = d;
        CreateView(d);
        SpawnDust(new Vector3(c.x + 0.5f, c.y + 0.2f, 0));
        MMAudio.Play("dig");
        MMAchievements.Add("dug");
        tillCount++;
        if (tillCount <= 2) g.UI.Toast("Torpağı bellədin! İndi su qabı ilə sula və toxum ək [F].");
    }

    void SpawnDust(Vector3 p)
    {
        if (Dust == null || Dust.Length == 0 || Dust[0] == null) return;
        var go = new GameObject("Dust");
        go.transform.position = p;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Dust[0]; sr.sortingOrder = 5;
        var anim = go.AddComponent<MMSpriteAnim>();
        anim.Frames = Dust; anim.Fps = 8f;
        go.AddComponent<MMFade>().Life = 0.45f;
    }

    void CreateView(MMPlotData d)
    {
        if (PlotRoot == null) return;
        var pos = new Vector3(d.Cell.x + 0.5f, d.Cell.y + 0.02f, 0);
        var go = new GameObject("Plot_" + d.Cell.x + "_" + d.Cell.y);
        go.transform.SetParent(PlotRoot, false);
        go.transform.position = pos;
        var soil = new GameObject("Soil").AddComponent<SpriteRenderer>();
        soil.transform.SetParent(go.transform, false);
        soil.sprite = SoilDry; soil.sortingOrder = -16;
        var plant = new GameObject("Plant").AddComponent<SpriteRenderer>();
        plant.transform.SetParent(go.transform, false);
        plant.transform.localPosition = new Vector3(0, 0.1f, 0);
        plant.spriteSortPoint = SpriteSortPoint.Pivot;
        plant.enabled = false;
        var p = go.AddComponent<MMPlot>();
        p.Data = d; p.Soil = soil; p.PlantBody = plant;
        p.InteractOffset = new Vector2(0, 0.5f);
        if (PromptSprite != null)
        {
            var pr = new GameObject("Prompt");
            pr.transform.SetParent(go.transform, false);
            pr.transform.localPosition = new Vector3(0, 1.5f, 0);
            var psr = pr.AddComponent<SpriteRenderer>(); psr.sprite = PromptSprite; psr.sortingOrder = 50;
            pr.SetActive(false);
            p.Prompt = pr;
        }
        views[d.Cell] = p;
        p.Refresh();
    }

    void Refresh(Vector2Int c)
    {
        if (views.TryGetValue(c, out var v) && v != null) v.Refresh();
    }

    // F ilə toxum əkmək (oyunçunun qabağındakı ləkə)
    public bool TryPlant(Vector2 target, string seed)
    {
        var g = MMGame.I;
        var c = CellOf(target);
        var plot = ViewAt(c) ?? MMPlot.Nearest(target, 0.7f);
        if (plot == null)
        {
            if (!tillable.Contains(c)) return false;
            g.UI.Toast("Əvvəlcə torpağı bel ilə bellə [F], sonra toxum ək.");
            return true;
        }
        if (!plot.Empty) { g.UI.Toast("Bu ləkdə artıq bitki var. Boş ləkin yanında ək."); return true; }
        plot.Plant(seed);
        return true;
    }

    // su qabı: hədəf xanadakı ləki sula
    public MMPlot PlotFor(Vector2 target) => ViewAt(CellOf(target)) ?? MMPlot.Nearest(target, 0.7f);

    // yaddaş: "x,y:bitki:gün:sulanıb"
    public List<string> Serialize()
    {
        var list = new List<string>();
        foreach (var d in plots.Values) list.Add(d.Cell.x + "," + d.Cell.y + ":" + (d.Crop ?? "") + ":" + d.Days + ":" + (d.Watered ? 1 : 0));
        return list;
    }

    public void Load(List<string> data)
    {
        foreach (var v in views.Values) if (v != null) Destroy(v.gameObject);
        views.Clear(); plots.Clear();
        if (data == null) return;
        foreach (var line in data)
        {
            var parts = line.Split(':');
            if (parts.Length < 4) continue;
            var xy = parts[0].Split(',');
            if (xy.Length != 2 || !int.TryParse(xy[0], out var x) || !int.TryParse(xy[1], out var y)) continue;
            var d = new MMPlotData { Cell = new Vector2Int(x, y), Crop = parts[1], Watered = parts[3] == "1" };
            int.TryParse(parts[2], out d.Days);
            if (!tillable.Contains(d.Cell)) continue;
            plots[d.Cell] = d;
            CreateView(d);
        }
    }
}
