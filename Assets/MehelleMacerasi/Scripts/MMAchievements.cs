using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Nailiyyətlərin yaddaşı (bütün oyunlar üçün ümumidir)
[System.Serializable]
public class MMAchievementSave
{
    public List<string> Unlocked = new List<string>();
    public List<string> CounterKeys = new List<string>();
    public List<int> CounterValues = new List<int>();
}

// Nailiyyətlər: 20 medal, sayğaclar, açılanda yuxarıda bildiriş, [N] ilə pəncərə.
// Yeni oyun başlayanda nailiyyətlər silinmir: ayrıca faylda saxlanılır.
public class MMAchievements : MonoBehaviour
{
    public struct Def
    {
        public string Id, Title, Desc, Icon, Counter;
        public int Goal;
    }

    static Def D(string id, string title, string desc, string icon, string counter = null, int goal = 1)
    {
        return new Def { Id = id, Title = title, Desc = desc, Icon = icon, Counter = counter, Goal = goal };
    }

    // bütün nailiyyətlər (sıra pəncərədəki sıradır)
    public static readonly Def[] Table =
    {
        D("cat", "Pişik dostu", "Saranın pişiyi Mestanı tap və evə qaytar.", "cat_icon"),
        D("letters", "Poçt köməkçisi", "Elçinin itirdiyi 3 məktubu tap.", "letter_icon"),
        D("tea", "Çay daşıyan", "Parkdakı babalara çay apar.", "tea_icon"),
        D("gardener", "Bağban", "Məhəllədəki 6 gül ləkinin hamısını sula.", "flowerbed", "flowers", 6),
        D("photographer", "Fotoqraf", "Fotoaparatla 5 şəkil çək.", "camera_icon", "photos", 5),
        D("pigeons", "Göyərçin dostu", "Göyərçinlərə 3 dəfə çörək at.", "pigeon_0", "bread", 3),
        D("dog", "İtin dostu", "Topu at, it onu sənə gətirsin.", "dog_idle_0"),
        D("wishes", "Arzu tutan", "Fəvvarəyə 5 dəfə pul at.", "coin_icon", "wishes", 5),
        D("shopper", "Alıcı", "Aysel xalanın marketindən 5 şey al.", "icecream_icon", "shop", 5),
        D("nightowl", "Gecə quşu", "Gecə saat 00:00-da çöldə ol.", "moon_icon"),
        D("rain", "Yağış adamı", "Yağışda çətirlə 15 saniyə gəz.", "umbrella_icon"),
        D("guest", "Ev qonağı", "İki evin içinə gir.", "house_icon", "houses", 2),
        D("farmer", "Fermer", "Bağçadan 10 məhsul yığ.", "tomato_icon", "harvest", 10),
        D("snowman", "Qar adamı", "Qışda yerkökü burunlu qar adamı düzəlt.", "snowman_3"),
        D("snowball", "Qartopu ustası", "Qartopu ilə 5 dəfə vur.", "snowball", "snowhits", 5),
        D("festival", "Məhəllə şənliyi", "İkinci hekayəni bitir: şənlik olsun!", "invite_icon"),
        D("digger", "Bel ustası", "Tarlada 20 xana torpaq bellə.", "spade_icon", "dug", 20),
        D("football", "Futbolçu", "Meydançada 3 qol vur.", "ball_icon", "goals", 3),
        D("books", "Kitabsevər", "Kitabxananın itmiş 3 kitabını tap.", "book_icon"),
        D("baker", "Təndir dostu", "Gülnarə xalaya tərəvəz sat.", "bread_icon"),
    };

    public static MMAchievements I;

    [Header("Window")]
    public GameObject Panel;
    public RectTransform FitRoot;
    public Text HeaderText;
    public Image[] Medals;
    public Image[] Icons;
    public Text[] Titles;
    public Text[] Descs;
    public Sprite MedalSprite, MedalLockedSprite;
    public Sprite[] IconSprites;     // Table ilə eyni sırada

    [Header("Popup")]
    public RectTransform Popup;
    public Image PopupIcon;
    public Text PopupTitle;

    readonly HashSet<string> unlocked = new HashSet<string>();
    readonly Dictionary<string, int> counters = new Dictionary<string, int>();
    readonly Queue<int> popupQueue = new Queue<int>();
    float popupT;
    int openedFrame = -1;
    bool fromPause;

    const float PopupTime = 3.4f;
    const float PopupShownY = -14f, PopupHiddenY = 130f;

    static string FilePath => Path.Combine(Application.persistentDataPath, "mehelle_achievements.json");

    public static int UnlockedCount => I != null ? I.unlocked.Count : 0;
    public static bool Has(string id) => I != null && I.unlocked.Contains(id);
    public static int Get(string counter) => I != null && I.counters.TryGetValue(counter, out var v) ? v : 0;

    void Awake()
    {
        I = this;
        Load();
    }

    void Start()
    {
        if (Panel != null) Panel.SetActive(false);
        if (Popup != null) { Popup.anchoredPosition = new Vector2(0, PopupHiddenY); Popup.gameObject.SetActive(false); }
    }

    // ---------------- API ----------------
    public static void Unlock(string id)
    {
        if (I == null || I.unlocked.Contains(id)) return;
        int idx = IndexOf(id);
        if (idx < 0) { Debug.LogWarning("[MM] Unknown achievement: " + id); return; }
        I.unlocked.Add(id);
        I.Save();
        I.popupQueue.Enqueue(idx);
    }

    // sayğacı artır (məs. "photos")
    public static void Add(string counter, int n = 1)
    {
        if (I == null) return;
        Set(counter, Get(counter) + n);
    }

    // sayğacı təyin et (yalnız artır, azalmır)
    public static void Set(string counter, int value)
    {
        if (I == null) return;
        int old = Get(counter);
        if (value <= old) return;
        I.counters[counter] = value;
        I.Save();
        foreach (var d in Table)
            if (d.Counter == counter && value >= d.Goal) Unlock(d.Id);
    }

    // evin içinə girəndə: fərqli evləri sayırıq
    public static void Visit(string roomId)
    {
        if (I == null || string.IsNullOrEmpty(roomId)) return;
        string key = "visit_" + roomId;
        if (Get(key) > 0) return;
        I.counters[key] = 1;
        int n = 0;
        foreach (var k in I.counters.Keys) if (k.StartsWith("visit_")) n++;
        Set("houses", n);
        I.Save();
    }

    static int IndexOf(string id)
    {
        for (int i = 0; i < Table.Length; i++) if (Table[i].Id == id) return i;
        return -1;
    }

    // ---------------- yaddaş ----------------
    void Load()
    {
        unlocked.Clear(); counters.Clear();
        try
        {
            if (!File.Exists(FilePath)) return;
            var d = JsonUtility.FromJson<MMAchievementSave>(File.ReadAllText(FilePath));
            if (d == null) return;
            foreach (var id in d.Unlocked) unlocked.Add(id);
            for (int i = 0; i < d.CounterKeys.Count && i < d.CounterValues.Count; i++) counters[d.CounterKeys[i]] = d.CounterValues[i];
        }
        catch (System.Exception e) { Debug.LogWarning("[MM] Achievements load failed: " + e.Message); }
    }

    void Save()
    {
        var d = new MMAchievementSave();
        d.Unlocked.AddRange(unlocked);
        foreach (var kv in counters) { d.CounterKeys.Add(kv.Key); d.CounterValues.Add(kv.Value); }
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(d, true)); }
        catch (System.Exception e) { Debug.LogWarning("[MM] Achievements save failed: " + e.Message); }
    }

    // test üçün: hamısını sıfırla
    public static void ResetAll()
    {
        if (I == null) return;
        I.unlocked.Clear(); I.counters.Clear(); I.popupQueue.Clear();
        I.Save();
    }

    // ---------------- pəncərə ----------------
    public void Open(bool openedFromPause)
    {
        var g = MMGame.I;
        fromPause = openedFromPause;
        g.State = MMState.Achievements;
        openedFrame = Time.frameCount;
        Panel.SetActive(true);
        Panel.transform.SetAsLastSibling();
        g.UI.HudRoot.SetActive(false);
        Render();
        Fit();
        // bildiriş başlığı örtməsin: tez yuxarı qalxsın
        if (popupT > 0.35f) popupT = 0.35f;
        MMAudio.Play("blip");
    }

    public void Close()
    {
        var g = MMGame.I;
        Panel.SetActive(false);
        if (fromPause)
        {
            var pause = FindAnyObjectByType<MMPauseMenu>();
            if (pause != null) { pause.Open(MMPauseMenu.AchievementsRow); return; }
        }
        g.State = MMState.Playing;
        g.UI.ClosedFrame = Time.frameCount;
        g.UI.HudRoot.SetActive(true);
        g.UI.Refresh();
    }

    void Fit()
    {
        if (FitRoot == null) return;
        var area = ((RectTransform)Panel.transform).rect;
        float k = Mathf.Clamp(Mathf.Min((area.height - 20f) / FitRoot.rect.height, (area.width - 20f) / FitRoot.rect.width), 0.4f, 1f);
        FitRoot.localScale = new Vector3(k, k, 1f);
    }

    void Render()
    {
        if (HeaderText != null) HeaderText.text = "Nailiyyətlər   " + unlocked.Count + "/" + Table.Length;
        for (int i = 0; i < Table.Length && i < Medals.Length; i++)
        {
            var d = Table[i];
            bool has = unlocked.Contains(d.Id);
            Medals[i].sprite = has ? MedalSprite : MedalLockedSprite;
            Icons[i].color = has ? Color.white : new Color(0.62f, 0.62f, 0.7f, 0.45f);
            Titles[i].text = d.Title;
            Titles[i].color = has ? new Color(0.36f, 0.2f, 0.08f) : new Color(0.45f, 0.42f, 0.4f);
            string desc = d.Desc;
            if (!has && d.Goal > 1) desc += "  <color=#b0402a>" + Mathf.Min(Get(d.Counter), d.Goal) + "/" + d.Goal + "</color>";
            if (has) desc = "<color=#3d7a2a>Qazanıldı!</color> " + desc;
            Descs[i].text = desc;
        }
    }

    // ---------------- hər kadr ----------------
    void Update()
    {
        var g = MMGame.I;
        var kb = Keyboard.current;
        if (g == null) return;

        // gecə quşu: gecə yarısı çöldə olmaq
        var dn = MMDayNight.I;
        if (g.State == MMState.Playing && g.CurrentRoom == null && dn != null && dn.Hour == 0) Unlock("nightowl");

        if (kb != null)
        {
            if (g.State == MMState.Playing && kb.nKey.wasPressedThisFrame && Time.frameCount != g.UI.ClosedFrame) Open(false);
            else if (g.State == MMState.Achievements && Time.frameCount != openedFrame &&
                     (kb.nKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                Close();
        }
        if (g.State == MMState.Achievements && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) Close();

        UpdatePopup();
    }

    void UpdatePopup()
    {
        if (Popup == null) return;
        if (popupT <= 0)
        {
            if (popupQueue.Count == 0) { if (Popup.gameObject.activeSelf) Popup.gameObject.SetActive(false); return; }
            // pəncərə açıqdırsa, bildirişə ehtiyac yoxdur: medal elə orada parlayır
            if (Panel != null && Panel.activeSelf)
            {
                popupQueue.Clear();
                MMAudio.Play("achieve");
                Render();
                return;
            }
            var d = Table[popupQueue.Dequeue()];
            int idx = IndexOf(d.Id);
            PopupTitle.text = "<size=15><color=#b0402a>Yeni nailiyyət!</color></size>\n" + d.Title;
            PopupIcon.sprite = IconSprites != null && idx < IconSprites.Length ? IconSprites[idx] : null;
            Popup.gameObject.SetActive(true);
            Popup.SetAsLastSibling();
            popupT = PopupTime;
            MMAudio.Play("achieve");
            if (Panel != null && Panel.activeSelf) Render();
        }
        popupT -= Time.unscaledDeltaTime;
        // aşağı sürüşür, gözləyir, yuxarı qayıdır
        float shown = PopupTime - popupT;
        float k = Mathf.Min(Mathf.Clamp01(shown / 0.35f), Mathf.Clamp01(popupT / 0.35f));
        k = 1f - (1f - k) * (1f - k);
        Popup.anchoredPosition = new Vector2(0, Mathf.Lerp(PopupHiddenY, PopupShownY, k));
    }
}
