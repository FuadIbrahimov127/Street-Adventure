using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// Çanta: 21 xana (ilk 7-si ekranın aşağısındakı əşya panelidir) və seçilmiş əşyanın işlədilməsi.
public class MMInventory : MonoBehaviour
{
    public static MMInventory I;
    public const int Size = 21, HotbarSize = 7, MaxWater = 8;

    [System.Serializable]
    public class Slot
    {
        public string Id;
        public int Count;
        public bool Empty => string.IsNullOrEmpty(Id) || Count <= 0;
    }

    public Slot[] Slots = new Slot[Size];
    public int Selected;
    public int Water;                      // su qabındakı su

    [Header("Sprites")]
    public string[] IconNames;
    public Sprite[] IconSprites;
    public Sprite BallSprite, CrumbsSprite, DropsSprite, ShadowSprite, PromptSprite;

    [Header("Flashlight")]
    public Light2D Flashlight;
    bool flashlightOn;

    [Header("Umbrella")]
    public SpriteRenderer PlayerUmbrella;   // oyunçunun başı üstündəki çətir
    public bool UmbrellaOpen { get; private set; }

    readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
    public event System.Action Changed;    // UI yenilənsin deyə

    void Awake()
    {
        I = this;
        for (int i = 0; i < Size; i++) if (Slots[i] == null) Slots[i] = new Slot();
        if (IconNames != null)
            for (int i = 0; i < IconNames.Length && i < IconSprites.Length; i++) icons[IconNames[i]] = IconSprites[i];
    }

    public Sprite Icon(string id)
    {
        var d = MMItems.Get(id);
        return d != null && icons.TryGetValue(d.Icon, out var s) ? s : null;
    }

    // ---------------- xanalar ----------------
    public int Count(string id)
    {
        int n = 0;
        foreach (var s in Slots) if (s.Id == id) n += s.Count;
        return n;
    }

    // preferBag: ilk 7 xananı (paneli) işlədilən əşyalar üçün boş saxla
    public bool Add(string id, int n = 1, bool preferBag = false)
    {
        var def = MMItems.Get(id);
        if (def == null || n <= 0) return false;
        foreach (var s in Slots)
        {
            if (n <= 0) break;
            if (s.Id == id && s.Count < def.MaxStack)
            {
                int k = Mathf.Min(n, def.MaxStack - s.Count);
                s.Count += k; n -= k;
            }
        }
        while (n > 0)
        {
            int idx = FreeSlot(preferBag);
            if (idx < 0) { Notify(); return false; }
            int k = Mathf.Min(n, def.MaxStack);
            Slots[idx].Id = id; Slots[idx].Count = k; n -= k;
        }
        Notify();
        return true;
    }

    int FreeSlot(bool preferBag)
    {
        if (preferBag)
        {
            for (int i = HotbarSize; i < Size; i++) if (Slots[i].Empty) return i;
            for (int i = 0; i < HotbarSize; i++) if (Slots[i].Empty) return i;
        }
        else
            for (int i = 0; i < Size; i++) if (Slots[i].Empty) return i;
        return -1;
    }

    public bool Remove(string id, int n = 1)
    {
        if (Count(id) < n) return false;
        // əvvəlcə seçilmiş xanadan götür
        var sel = Slots[Selected];
        if (sel.Id == id) { int k = Mathf.Min(n, sel.Count); sel.Count -= k; n -= k; }
        for (int i = Size - 1; i >= 0 && n > 0; i--)
        {
            var s = Slots[i];
            if (s.Id != id) continue;
            int k = Mathf.Min(n, s.Count);
            s.Count -= k; n -= k;
        }
        Notify();
        return true;
    }

    public void SetCount(string id, int n, bool preferBag)
    {
        int c = Count(id);
        if (c < n) Add(id, n - c, preferBag);
        else if (c > n) Remove(id, c - n);
    }

    // sürükləyib-buraxma: eyni əşyadırsa birləşdir, yoxsa yerlərini dəyiş
    public void Move(int from, int to)
    {
        if (from == to || from < 0 || to < 0) return;
        var a = Slots[from];
        var b = Slots[to];
        if (!a.Empty && a.Id == b.Id)
        {
            var def = MMItems.Get(a.Id);
            int k = Mathf.Min(a.Count, def.MaxStack - b.Count);
            b.Count += k; a.Count -= k;
        }
        else
        {
            Slots[from] = b;
            Slots[to] = a;
        }
        Notify();
    }

    public void Select(int i)
    {
        Selected = (i % HotbarSize + HotbarSize) % HotbarSize;
        Notify();
        MMAudio.Play("blip");
        var s = Slots[Selected];
        if (!s.Empty && MMGame.I != null) MMGame.I.UI.ShowItemName(MMItems.Get(s.Id).Name);
    }

    void Notify()
    {
        foreach (var s in Slots) if (s.Count <= 0) { s.Id = null; s.Count = 0; }
        Changed?.Invoke();
    }

    public void Clear()
    {
        for (int i = 0; i < Size; i++) Slots[i] = new Slot();
        Water = 0; Selected = 0; flashlightOn = false;
        UmbrellaOpen = false;
        if (PlayerUmbrella != null) PlayerUmbrella.enabled = false;
        Notify();
    }

    // tapşırıq və mağaza bayraqlarından asılı əşyalar çantada görünsün
    public void SyncFlags(MMGame g)
    {
        SetCount("charm", g.HasCharm ? 1 : 0, true);
        SetCount("shoes", g.HasSneakers ? 1 : 0, true);
        SetCount("letter", g.LettersReturned ? 0 : g.LettersFound, true);
        SetCount("tea", g.HasTea ? 1 : 0, true);
    }

    // ---------------- yaddaş ----------------
    public List<string> Serialize()
    {
        var list = new List<string>();
        foreach (var s in Slots) list.Add(s.Empty ? "" : s.Id + ":" + s.Count);
        return list;
    }

    public void Load(List<string> data)
    {
        for (int i = 0; i < Size; i++)
        {
            Slots[i] = new Slot();
            if (data == null || i >= data.Count || string.IsNullOrEmpty(data[i])) continue;
            var parts = data[i].Split(':');
            if (parts.Length == 2 && MMItems.Get(parts[0]) != null && int.TryParse(parts[1], out int c))
            {
                Slots[i].Id = parts[0];
                Slots[i].Count = c;
            }
        }
        Notify();
    }

    // ---------------- işlətmə ----------------
    void Update()
    {
        var g = MMGame.I;
        if (g == null) return;
        UpdateFlashlight(g);
        // evin içində çətir görünmür
        if (PlayerUmbrella != null) PlayerUmbrella.enabled = UmbrellaOpen && g.CurrentRoom == null;
        if (g.State != MMState.Playing) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        bool canUse = Time.frameCount != g.UI.ClosedFrame;
        if (kb != null)
        {
            var keys = new[] { kb.digit1Key, kb.digit2Key, kb.digit3Key, kb.digit4Key, kb.digit5Key, kb.digit6Key, kb.digit7Key };
            for (int i = 0; i < keys.Length; i++) if (keys[i].wasPressedThisFrame) Select(i);
            if (kb.fKey.wasPressedThisFrame && canUse) UseSelected();
        }
        if (mouse != null)
        {
            float sc = mouse.scroll.ReadValue().y;
            if (sc > 0.1f) Select(Selected - 1);
            else if (sc < -0.1f) Select(Selected + 1);
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mouse.leftButton.wasPressedThisFrame && !overUI && canUse) UseSelected();
        }
    }

    public void UseSelected()
    {
        var g = MMGame.I;
        var s = Slots[Selected];
        if (s.Empty || g.Player == null) return;
        var def = MMItems.Get(s.Id);
        Vector2 pos = g.Player.transform.position;
        Vector2 f = g.Player.Facing;
        switch (def.Use)
        {
            case MMItemUse.Eat:
                Remove(s.Id);
                g.SpeedBoostT = Mathf.Min(60f, g.SpeedBoostT + 20f);
                MMAudio.Play("munch");
                g.UI.Toast("Dondurma yedin! 20 saniyə sürətli qaçırsan.");
                g.UI.Refresh();
                break;
            case MMItemUse.Drink:
                if (g.Hearts >= g.MaxHearts) { g.UI.Toast("Ürəklərin artıq doludur. Sonraya saxla!"); return; }
                Remove(s.Id);
                g.Hearts++;
                MMAudio.Play("lucky");
                g.UI.Toast("Nar şirəsi içdin: +1 ürək!");
                g.UI.Refresh();
                break;
            case MMItemUse.Throw:
                ThrowBall(pos, f);
                break;
            case MMItemUse.Bread:
                ThrowBread(pos, f);
                break;
            case MMItemUse.Water:
                UseCan(pos, f);
                break;
            case MMItemUse.Photo:
                StartCoroutine(TakePhoto());
                break;
            case MMItemUse.Umbrella:
                UmbrellaOpen = !UmbrellaOpen;
                if (PlayerUmbrella != null) PlayerUmbrella.enabled = UmbrellaOpen;
                MMAudio.Play("blip");
                g.UI.Toast(UmbrellaOpen ? (MMWeather.I != null && MMWeather.I.IsRaining ? "Çətiri açdın. İndi islanmırsan!" : "Çətiri açdın. Günəşdən də qoruyur!") : "Çətiri bağladın.");
                break;
            case MMItemUse.Seed:
                if (MMGarden.I == null || !MMGarden.I.TryPlant(MMGarden.TargetPoint(g.Player), s.Id))
                    g.UI.Toast("Toxumu tarlada, bellənmiş torpaqda ək. Tarla Məktəb məhəlləsindədir.");
                break;
            case MMItemUse.Hoe:
                if (MMGarden.I != null) MMGarden.I.TryTill(MMGarden.TargetPoint(g.Player));
                break;
            case MMItemUse.Light:
                flashlightOn = !flashlightOn;
                MMAudio.Play("blip");
                g.UI.Toast(flashlightOn ? "Fənəri yandırdın. Gecə daha yaxşı görünür!" : "Fənəri söndürdün.");
                break;
            default:
                g.UI.Toast(def.Desc);
                break;
        }
    }

    GameObject Spawn(string name, Sprite sp, Vector3 pos, bool shadow)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        if (shadow && ShadowSprite != null)
        {
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(go.transform, false);
            sh.transform.localPosition = new Vector3(0, 0.02f, 0);
            sh.transform.localScale = new Vector3(0.6f, 0.8f, 1);
            var ssr = sh.AddComponent<SpriteRenderer>();
            ssr.sprite = ShadowSprite; ssr.sortingOrder = -1;
        }
        return go;
    }

    void ThrowBall(Vector2 pos, Vector2 f)
    {
        Remove("ball");
        var go = Spawn("Ball", BallSprite, pos + f * 0.65f + new Vector2(0, f.y < 0 ? -0.1f : 0.05f), true);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.linearDamping = 1.4f; rb.freezeRotation = true; rb.mass = 0.2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.18f; col.offset = new Vector2(0, 0.18f);
        col.sharedMaterial = new PhysicsMaterial2D("BallBounce") { bounciness = 0.65f, friction = 0f };
        var ball = go.AddComponent<MMBall>();
        ball.InteractOffset = new Vector2(0, 0.2f);
        if (PromptSprite != null)
        {
            var pr = new GameObject("Prompt");
            pr.transform.SetParent(go.transform, false);
            pr.transform.localPosition = new Vector3(0, 0.9f, 0);
            var psr = pr.AddComponent<SpriteRenderer>(); psr.sprite = PromptSprite; psr.sortingOrder = 50;
            pr.SetActive(false);
            ball.Prompt = pr;
        }
        rb.linearVelocity = f * 8.5f;
        MMAudio.Play("throw");
    }

    void ThrowBread(Vector2 pos, Vector2 f)
    {
        Remove("bread");
        var go = Spawn("Crumbs", CrumbsSprite, pos + f * 1.1f, false);
        go.GetComponent<SpriteRenderer>().sortingOrder = -1;
        go.AddComponent<MMCrumbs>();
        MMAudio.Play("throw");
        MMAchievements.Add("bread");
        int near = 0;
        foreach (var p in FindObjectsByType<MMPigeon>())
            if ((p.transform.position - go.transform.position).sqrMagnitude < 100f) near++;
        MMGame.I.UI.Toast(near > 0 ? "Çörək atdın. Göyərçinlər gəlir!" : "Çörək atdın. Göyərçinlərin yanında atsan, qaçıb gələcəklər.");
    }

    void UseCan(Vector2 pos, Vector2 f)
    {
        var g = MMGame.I;
        var fountain = FindAnyObjectByType<MMFountain>();
        bool atTap = MMGarden.I != null && MMGarden.I.NearTap(pos);
        if (atTap || (fountain != null && ((Vector2)fountain.transform.position + new Vector2(0, 0.75f) - pos).magnitude < 2.8f))
        {
            Water = MaxWater;
            Notify();
            MMAudio.Play("water");
            g.UI.Toast($"Su qabını doldurdun! ({Water}/{MaxWater})");
            return;
        }
        if (Water <= 0)
        {
            g.UI.Toast("Su qabı boşdur. Fəvvarədə və ya bağçadakı kranda doldur!");
            return;
        }
        Water--;
        Notify();
        Vector2 target = pos + f * 0.9f;
        var drops = Spawn("WaterDrops", DropsSprite, target, false);
        drops.AddComponent<MMFade>().Life = 0.6f;
        MMAudio.Play("water");
        // tarla ləki
        var plot = MMGarden.I != null ? MMGarden.I.PlotFor(MMGarden.TargetPoint(g.Player)) : null;
        if (plot != null)
        {
            bool first = !plot.Watered;
            plot.Water();
            g.UI.Toast(plot.Empty ? $"Torpağı suladın. Toxum ək! ({Water}/{MaxWater})" :
                       first ? $"Suladın! Sabah böyüyəcək. ({Water}/{MaxWater})" : $"Bu bitki bu gün artıq sulanıb. ({Water}/{MaxWater})");
            return;
        }
        var bed = MMFlowerbed.Nearest(target, 1.3f);
        if (bed != null && !bed.Bloomed)
        {
            bed.SetBloom();
            g.Collected.Add(bed.Id);
            g.AddCoins(1);
            int bloomed = 0;
            foreach (var b in MMFlowerbed.All) if (b.Bloomed) bloomed++;
            MMAchievements.Set("flowers", bloomed);
            MMAudio.Play("lucky");
            int left = 0;
            foreach (var b in MMFlowerbed.All) if (!b.Bloomed) left++;
            g.UI.Toast(left > 0 ? $"Güllər açıldı! +1 manat. Daha {left} quru gül ləki var." : "Bütün güllər açıldı! Məhəllə gözəlləşdi! +1 manat");
            g.SaveGame();
        }
    }

    IEnumerator TakePhoto()
    {
        var g = MMGame.I;
        g.UI.SetPhotoMode(true);
        yield return new WaitForEndOfFrame();
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        g.UI.SetPhotoMode(false);
        g.UI.Flash();
        MMAudio.Play("shutter");
        try
        {
            string dir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures), "MehelleMacerasi");
            System.IO.Directory.CreateDirectory(dir);
            string file = System.IO.Path.Combine(dir, "sekil_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
            g.UI.Toast("Şəkil çəkildi! Şəkillər > MehelleMacerasi qovluğuna bax.");
            MMAchievements.Add("photos");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MM] Photo failed: " + e.Message);
            g.UI.Toast("Şəkli saxlamaq alınmadı.");
        }
        Destroy(tex);
    }

    void UpdateFlashlight(MMGame g)
    {
        if (Flashlight == null || g.Player == null) return;
        float target = flashlightOn ? 1.1f : 0f;
        Flashlight.intensity = Mathf.MoveTowards(Flashlight.intensity, target, Time.deltaTime * 4f);
        Flashlight.enabled = Flashlight.intensity > 0.01f;
        Vector3 want = g.Player.transform.position + (Vector3)(g.Player.Facing * 1.5f) + new Vector3(0, 0.5f, 0);
        Flashlight.transform.position = Vector3.Lerp(Flashlight.transform.position, want, Time.deltaTime * 12f);
    }
}
