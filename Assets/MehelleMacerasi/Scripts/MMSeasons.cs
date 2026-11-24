using UnityEngine;
using UnityEngine.Tilemaps;

public enum MMSeason { Spring, Summer, Autumn, Winter }

// Fəsillər: hər 3 oyun günündən bir dəyişir.
// Ağaclar rəng dəyişir, payızda yerə yarpaq tökülür, qışda hər yer qarla örtülür.
public class MMSeasons : MonoBehaviour
{
    public static MMSeasons I;
    public const int DaysPerSeason = 3;
    public static readonly string[] Names = { "Yaz", "Yay", "Payız", "Qış" };

    public MMSeason Season = MMSeason.Spring;
    public int SeasonDay = 1;

    [Header("References")]
    public Tilemap[] Tinted;               // ot və kənar plitələri: fəslə görə rənglənir
    public Tilemap[] LeavesMaps, SnowMaps;   // hər zonanın öz örtüyü
    public Sprite[] Icons = new Sprite[4];

    public float SnowLevel { get; private set; }   // yerdəki qar (0..1)
    float leavesLevel;
    string pendingToast;
    float pendingT;

    public bool IsWinter => Season == MMSeason.Winter;
    public Sprite Icon => Icons != null && Icons.Length == 4 ? Icons[(int)Season] : null;
    public string Label => Names[(int)Season] + ", " + SeasonDay + Ordinal(SeasonDay) + " gün";

    // Azərbaycan dilində sıra şəkilçisi: 1-ci, 2-ci, 3-cü, 4-cü, 6-cı...
    public static string Ordinal(int n)
    {
        int d = n % 10;
        if (d == 0)
        {
            if (n % 100 == 0) return "-cü";
            int t = (n / 10) % 10;
            return t == 1 || t == 3 ? "-cu" : t == 4 || t == 6 || t == 9 ? "-cı" : "-ci";
        }
        return d == 3 || d == 4 ? "-cü" : d == 6 ? "-cı" : d == 9 ? "-cu" : "-ci";
    }

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        ApplyInstant();
    }

    void Update()
    {
        var g = MMGame.I;
        float dt = Time.deltaTime;
        // qış gəlir: qar yavaş-yavaş örtür; yazda əriyir
        SnowLevel = Mathf.MoveTowards(SnowLevel, IsWinter ? 1f : 0f, dt / (IsWinter ? 6f : 10f));
        leavesLevel = Mathf.MoveTowards(leavesLevel, Season == MMSeason.Autumn ? 1f : 0f, dt / 6f);
        SetAlpha(SnowMaps, SnowLevel * 0.95f);
        SetAlpha(LeavesMaps, leavesLevel);
        var tint = GroundTint(Season);
        if (Tinted != null)
            foreach (var t in Tinted)
            {
                if (t == null) continue;
                var c = t.color;
                if (Mathf.Abs(c.r - tint.r) + Mathf.Abs(c.g - tint.g) + Mathf.Abs(c.b - tint.b) > 0.004f) t.color = Color.Lerp(c, tint, Mathf.Min(1f, dt * 1.5f));
            }

        // fəsil dəyişəndə bildiriş (oyun davam edəndə göstərilir)
        if (pendingToast != null && g != null && g.State == MMState.Playing)
        {
            pendingT -= dt;
            if (pendingT <= 0) { g.UI.Toast(pendingToast); MMAudio.Play("quest_new"); pendingToast = null; }
        }
    }

    static void SetAlpha(Tilemap[] maps, float a)
    {
        if (maps == null) return;
        foreach (var tm in maps) SetAlpha(tm, a);
    }

    static void SetAlpha(Tilemap tm, float a)
    {
        if (tm == null) return;
        var c = tm.color;
        if (Mathf.Abs(c.a - a) < 0.002f) return;
        c.a = a; tm.color = c;
        var r = tm.GetComponent<TilemapRenderer>();
        if (r != null) r.enabled = a > 0.01f;
    }

    static Color GroundTint(MMSeason s)
    {
        switch (s)
        {
            case MMSeason.Summer: return new Color(1f, 0.98f, 0.9f);
            case MMSeason.Autumn: return new Color(1f, 0.9f, 0.74f);
            case MMSeason.Winter: return new Color(0.9f, 0.94f, 1f);
            default: return Color.white;
        }
    }

    // yeni gün başlayanda (MMDayNight çağırır)
    public void OnNewDay()
    {
        SeasonDay++;
        if (SeasonDay > DaysPerSeason) SetSeason((MMSeason)(((int)Season + 1) % 4), true);
    }

    public void SetSeason(MMSeason s, bool announce)
    {
        Season = s;
        SeasonDay = 1;
        foreach (var o in MMSeasonal.All) o.Apply((int)s);
        if (!announce) return;
        switch (s)
        {
            case MMSeason.Spring: pendingToast = "Yaz gəldi! Qar əriyir, bağçaya toxum əkmək vaxtıdır."; break;
            case MMSeason.Summer: pendingToast = "Yay gəldi! Günəşli, isti günlər başlayır."; break;
            case MMSeason.Autumn: pendingToast = "Payız gəldi! Ağacların yarpaqları saralır."; break;
            case MMSeason.Winter: pendingToast = "Qış gəldi! [G] ilə qartopu at, parkda qar adamı düzəlt!"; break;
        }
        pendingT = 1.6f;
    }

    // yaddaşdan yükləyəndə: animasiyasız
    public void Load(int season, int day)
    {
        Season = (MMSeason)Mathf.Clamp(season, 0, 3);
        SeasonDay = Mathf.Clamp(day, 1, DaysPerSeason);
        ApplyInstant();
    }

    void ApplyInstant()
    {
        foreach (var o in MMSeasonal.All) o.Apply((int)Season);
        SnowLevel = IsWinter ? 1f : 0f;
        leavesLevel = Season == MMSeason.Autumn ? 1f : 0f;
        SetAlpha(SnowMaps, SnowLevel * 0.95f);
        SetAlpha(LeavesMaps, leavesLevel);
        if (Tinted != null) foreach (var t in Tinted) if (t != null) t.color = GroundTint(Season);
    }
}
