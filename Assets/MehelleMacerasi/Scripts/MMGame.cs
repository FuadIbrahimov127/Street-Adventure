using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public enum MMState { Menu, Playing, Dialogue, Won, GameOver, Map, Journal, Shop, Inventory, Transition, Paused, Achievements }

// Oyunun mərkəzi: vəziyyət, ürəklər, pullar, tapşırıqlar, əşyalar və yaddaş.
public class MMGame : MonoBehaviour
{
    public static MMGame I;

    // səhnə yenidən yüklənəndə menyunu keçib birbaşa nə edilsin
    enum BootAction { None, Continue, NewGame }
    static BootAction boot = BootAction.None;
    static bool refillHearts;

    public MMState State = MMState.Menu;
    public int MaxHearts = 3;
    public int Hearts;
    public int Coins;
    public float PlayTime;

    [Header("Quests")]
    public bool CatQuestStarted, CatFound, CatReturned;
    public bool LetterQuestStarted, LettersReturned;
    public int LettersFound;
    public const int LettersTotal = 3;
    public bool TeaQuestStarted, HasTea, TeaDelivered;
    public const int TeaPrice = 3;

    [Header("Items")]
    public bool HasCharm;          // göz muncuğu: bir maşın zərbəsindən qoruyur
    public float SpeedBoostT;      // dondurma: müvəqqəti sürət (saniyə)
    public bool HasSneakers;       // qaçış ayaqqabısı: həmişəlik sürət
    public int Wishes;             // fəvvarəyə atılan pullar

    [Header("References")]
    public MMUI UI;
    public MMPlayer Player;
    public MMDayNight DayNight;
    public Tilemap RoadMap;
    public List<Rect> Crosswalks = new List<Rect>();
    public string[] WalkGrid;      // piyadalar üçün: s/p = gəzilən, x = keçid, . = bağlı

    public readonly HashSet<string> Collected = new HashSet<string>();
    public Vector2 WorldSize = new Vector2(48, 36);
    public MMRoom CurrentRoom;     // null = küçədəyik
    public MMZone CurrentZone;     // hansı xəritədəyik (məhəllə, məktəb məhəlləsi)

    // xəritədə oyunçunun yeri (evin içindəsə, evin qapısı)
    public Vector3 PlayerMapPosition => CurrentRoom != null ? (Vector3)CurrentRoom.MapPosition : (Player != null ? Player.transform.position : Vector3.zero);

    int stateFrame = -1;
    float autosaveT;
    const float AutosaveEvery = 20f;

    // dünya (maşınlar, piyadalar) dayanmalıdırmı?
    public bool WorldPaused => State == MMState.Dialogue || State == MMState.Won || State == MMState.GameOver ||
                               State == MMState.Map || State == MMState.Journal || State == MMState.Shop || State == MMState.Inventory || State == MMState.Transition || State == MMState.Paused ||
                               State == MMState.Achievements;

    public float SpeedMultiplier => (HasSneakers ? 1.2f : 1f) * (SpeedBoostT > 0 ? 1.5f : 1f);

    void Awake()
    {
        I = this;
        Hearts = MaxHearts;
    }

    void Start()
    {
        // əvvəlcə yalnız oyunçunun olduğu xəritəni yandır
        if (Player != null) SetZone(MMZone.Find(Player.transform.position) ?? MMZone.Main);
        var action = boot;
        boot = BootAction.None;
        if (action == BootAction.Continue && MMSave.Exists)
        {
            LoadGame(refillHearts);
            refillHearts = false;
            BeginPlay("Son yaddaşdan davam edirsən.");
        }
        else if (action == BootAction.NewGame)
        {
            MMSave.Clear();
            GiveStartItems();
            BeginPlay("Məhəlləni gəz, qonşulara kömək et! Çanta: [I]");
        }
        else if (UI != null)
        {
            UI.ShowMenu(true, MMSave.Exists);
            UI.Refresh();
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        bool confirm = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
        switch (State)
        {
            case MMState.Menu:
                if (confirm)
                {
                    if (MMSave.Exists) ContinueGame();
                    else NewGame();
                }
                else if (kb.nKey.wasPressedThisFrame) NewGame();
                // əsas menyuda Esc: oyundan çıx (tam ekranda lazımdır)
                else if (kb.escapeKey.wasPressedThisFrame && Time.frameCount != UI.ClosedFrame) Application.Quit();
                break;

            case MMState.Playing:
                PlayTime += Time.deltaTime;
                if (SpeedBoostT > 0)
                {
                    SpeedBoostT -= Time.deltaTime;
                    if (SpeedBoostT <= 0) { SpeedBoostT = 0; UI.Toast("Dondurmanın təsiri bitdi."); UI.Refresh(); }
                }
                autosaveT += Time.deltaTime;
                if (autosaveT >= AutosaveEvery) SaveGame();
                if (Time.frameCount != UI.ClosedFrame)
                {
                    if (kb.mKey.wasPressedThisFrame) OpenMap();
                    else if (kb.jKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) OpenJournal(null);
                    else if (kb.iKey.wasPressedThisFrame) OpenBag();
                }
                break;

            case MMState.Map:
                if (Time.frameCount != stateFrame && (kb.mKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) CloseOverlay();
                break;

            case MMState.Journal:
                if (Time.frameCount != stateFrame && (kb.jKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame ||
                    kb.eKey.wasPressedThisFrame || confirm)) CloseOverlay();
                break;

            case MMState.Inventory:
                if (Time.frameCount != stateFrame && (kb.iKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)) CloseOverlay();
                break;

            case MMState.Won:
                if (kb.rKey.wasPressedThisFrame) Reload(false);
                else if (confirm && Time.frameCount != stateFrame) ContinueAfterWin();
                break;

            case MMState.GameOver:
                if (confirm) Reload(MMSave.Exists);
                else if (kb.nKey.wasPressedThisFrame) Reload(false);
                break;
        }
    }

    // ---------------- oyunun başlaması ----------------
    void ContinueGame()
    {
        LoadGame(false);
        BeginPlay("Xoş gəldin! Qaldığın yerdən davam edirsən.");
    }

    void NewGame()
    {
        MMSave.Clear();
        GiveStartItems();
        BeginPlay("Məhəlləni gəz, qonşulara kömək et! Çanta: [I]");
    }

    // yeni oyunda çantada fotoaparat olur
    void GiveStartItems()
    {
        var inv = MMInventory.I;
        if (inv == null) return;
        inv.Clear();
        inv.Add("camera");
    }

    void BeginPlay(string message)
    {
        State = MMState.Playing;
        autosaveT = 0;
        UI.ShowMenu(false, false);
        UI.Refresh();
        UI.Toast(message);
        MMAudio.Play("start");
    }

    // cont=true: son yaddaşdan davam (ürəklər dolu), false: tamamilə yeni oyun
    void Reload(bool cont)
    {
        boot = cont ? BootAction.Continue : BootAction.NewGame;
        refillHearts = cont;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // fasilə menyusundan: yadda saxla və əsas menyuya qayıt
    public void QuitToMenu()
    {
        SaveGame();
        boot = BootAction.None;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------------- pəncərələr ----------------
    public void OpenMap()
    {
        State = MMState.Map;
        stateFrame = Time.frameCount;
        UI.ShowMap(true);
        MMAudio.Play("blip");
    }

    public void OpenJournal(string highlightId)
    {
        State = MMState.Journal;
        stateFrame = Time.frameCount;
        UI.ShowJournal(true, highlightId);
        MMAudio.Play("blip");
    }

    public void OpenBag()
    {
        State = MMState.Inventory;
        stateFrame = Time.frameCount;
        UI.ShowBag(true);
        MMAudio.Play("blip");
    }

    void CloseOverlay()
    {
        State = MMState.Playing;
        UI.ShowMap(false);
        UI.ShowJournal(false, null);
        UI.ShowBag(false);
        UI.ClosedFrame = Time.frameCount;
        UI.Refresh();
    }

    // NPC tapşırıq verəndə çağırılır: jurnal açılır və yeni tapşırıq göstərilir
    public void OnQuestAccepted(string id)
    {
        UI.Refresh();
        MMAudio.Play("quest_new");
        SaveGame();
        OpenJournal(id);
    }

    // ---------------- qapılar və otaqlar ----------------
    public void UseDoor(MMDoor door)
    {
        if (State != MMState.Playing) return;
        StartCoroutine(DoorRoutine(door));
    }

    System.Collections.IEnumerator DoorRoutine(MMDoor door)
    {
        State = MMState.Transition;
        MMAudio.Play("door");
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.25f) { UI.SetFade(t); yield return null; }
        UI.SetFade(1f);
        Player.Teleport(door.Target);
        Player.SetFacing(door.FaceAfter);
        if (door.TargetZone != null) SetZone(door.TargetZone);
        // izləyən pişik də bizimlə gəlsin
        var cat = FindAnyObjectByType<MMCat>();
        if (cat != null && cat.ModeIndex == 1 && door.TargetZone == null) cat.transform.position = (Vector3)(door.Target - door.FaceAfter * 0.8f);
        SetRoom(door.TargetRoom);
        yield return new WaitForSecondsRealtime(0.12f);
        for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.3f) { UI.SetFade(t); yield return null; }
        UI.SetFade(0f);
        State = MMState.Playing;
        UI.ClosedFrame = Time.frameCount;
        if (!string.IsNullOrEmpty(door.Message)) UI.Toast(door.Message);
        if (door.TargetZone != null) SaveGame();
    }

    // xəritə (zona) dəyiş: yalnız bu zona aktiv qalır
    public void SetZone(MMZone zone)
    {
        if (zone == null) return;
        foreach (var z in MMZone.All)
            if (z.Root != null && z.Root.activeSelf != (z == zone)) z.Root.SetActive(z == zone);
        CurrentZone = zone;
        if (CurrentRoom == null) SetRoom(null);
    }

    public void SetRoom(MMRoom room)
    {
        CurrentRoom = room;
        if (room != null) MMAchievements.Visit(room.Id);
        var follow = Camera.main != null ? Camera.main.GetComponent<MMCameraFollow>() : null;
        if (follow == null) return;
        if (room != null) follow.SetBounds(room.Bounds.min, room.Bounds.max);
        else if (CurrentZone != null) follow.SetBounds(CurrentZone.Bounds.min, CurrentZone.Bounds.max);
        else follow.SetBounds(Vector2.zero, WorldSize);
    }

    // ---------------- oyun məntiqi ----------------
    public bool IsRoad(Vector2 worldPos)
    {
        if (RoadMap == null) return false;
        return RoadMap.HasTile(RoadMap.WorldToCell(worldPos));
    }

    public void AddCoins(int n)
    {
        Coins += n;
        UI.Refresh();
    }

    public void Damage()
    {
        if (State != MMState.Playing) return;
        if (HasCharm)
        {
            HasCharm = false;
            UI.Refresh();
            UI.Toast("Göz muncuğu səni qorudu! Amma muncuq sındı.");
            MMAudio.Play("charm");
            return;
        }
        Hearts = Mathf.Max(0, Hearts - 1);
        UI.Refresh();
        MMAudio.Play("hit");
        if (Hearts <= 0)
        {
            State = MMState.GameOver;
            UI.ShowGameOver(MMSave.Exists);
            MMAudio.Play("lose");
        }
        else UI.Toast("Ehtiyatlı ol! Yolu zolaqlı keçiddən keç.");
    }

    public void QuestDone(string msg)
    {
        UI.Toast(msg);
        UI.Refresh();
        MMAudio.Play("quest");
        if (CatReturned) MMAchievements.Unlock("cat");
        if (LettersReturned) MMAchievements.Unlock("letters");
        if (TeaDelivered) MMAchievements.Unlock("tea");
        SaveGame();
        // birinci hissənin sonu (ikinci hekayədə təkrar olmasın)
        if (CatReturned && LettersReturned && TeaDelivered && !Collected.Contains("ch2"))
            Invoke(nameof(Win), 1.6f);
    }

    void Win()
    {
        if (State == MMState.GameOver || Collected.Contains("ch2")) return;
        if (State != MMState.Playing) { Invoke(nameof(Win), 0.5f); return; }
        SaveGame();
        State = MMState.Won;
        stateFrame = Time.frameCount;
        int m = Mathf.FloorToInt(PlayTime / 60f), s = Mathf.FloorToInt(PlayTime % 60f);
        UI.ShowWinCustom("TƏBRİKLƏR!", "Sən məhəllənin əsl qəhrəmanısan!\nMestan evdədir, məktublar çatdı, babalar çay içir.",
            $"Pulların: {Coins} manat     Arzular: {Wishes}     Vaxt: {m:00}:{s:00}",
            "[Enter] Davam et: yeni hekayə səni gözləyir!        [R] Yeni oyun");
        MMAudio.Play("win");
    }

    // qələbə ekranından sonra oyun davam edir: ikinci hekayə başlayır
    void ContinueAfterWin()
    {
        State = MMState.Playing;
        UI.HideWin();
        UI.ClosedFrame = Time.frameCount;
        if (!Collected.Contains("ch2"))
        {
            Collected.Add("ch2");
            UI.Toast("Yeni hekayə: Məhəllə şənliyi! Poçtda Leyla xala səni gözləyir.");
            MMAudio.Play("quest_new");
        }
        else UI.Toast("Şənlik davam edir! Qonşularla danış, məhəlləni gəz.");
        UI.Refresh();
        SaveGame();
    }

    // ---------------- yuxu ----------------
    public void Sleep()
    {
        if (State != MMState.Playing) return;
        StartCoroutine(SleepRoutine());
    }

    System.Collections.IEnumerator SleepRoutine()
    {
        State = MMState.Transition;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.7f) { UI.SetFade(t); yield return null; }
        UI.SetFade(1f);
        if (DayNight != null) DayNight.SleepToMorning();
        Hearts = MaxHearts;
        UI.Refresh();
        yield return new WaitForSecondsRealtime(0.6f);
        SaveGame();
        for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.7f) { UI.SetFade(t); yield return null; }
        UI.SetFade(0f);
        State = MMState.Playing;
        UI.ClosedFrame = Time.frameCount;
        string label = MMSeasons.I != null ? MMSeasons.I.Label : "";
        UI.Toast($"Sabahın xeyir! {label}. Bitkiləri sulamağı unutma!");
        MMAudio.Play("start");
    }

    // ---------------- yaddaş ----------------
    public void SaveGame()
    {
        autosaveT = 0;
        if (State == MMState.Menu || State == MMState.Won || State == MMState.GameOver || Hearts <= 0) return;
        var d = new MMSaveData
        {
            Coins = Coins, Hearts = Hearts, Wishes = Wishes, LettersFound = LettersFound,
            PlayTime = PlayTime, SpeedBoostT = SpeedBoostT,
            TimeOfDay = DayNight != null ? DayNight.TimeOfDay : 480f,
            CatQuestStarted = CatQuestStarted, CatFound = CatFound, CatReturned = CatReturned,
            LetterQuestStarted = LetterQuestStarted, LettersReturned = LettersReturned,
            TeaQuestStarted = TeaQuestStarted, HasTea = HasTea, TeaDelivered = TeaDelivered,
            HasCharm = HasCharm, HasSneakers = HasSneakers,
        };
        if (Player != null) { d.PlayerX = Player.transform.position.x; d.PlayerY = Player.transform.position.y; }
        var cat = FindAnyObjectByType<MMCat>(FindObjectsInactive.Include);
        if (cat != null) { d.CatMode = cat.ModeIndex; d.CatX = cat.SavePosition.x; d.CatY = cat.SavePosition.y; }
        d.Collected.AddRange(Collected);
        var inv = MMInventory.I;
        if (inv != null) { d.Inventory = inv.Serialize(); d.Water = inv.Water; d.Selected = inv.Selected; }
        if (DayNight != null) d.Day = DayNight.Day;
        if (MMSeasons.I != null) { d.Season = (int)MMSeasons.I.Season; d.SeasonDay = MMSeasons.I.SeasonDay; }
        if (MMGarden.I != null) d.Plots = MMGarden.I.Serialize();
        foreach (var sm in FindObjectsByType<MMSnowman>(FindObjectsInactive.Include)) { while (d.Snowmen.Count <= sm.Index) d.Snowmen.Add(-1); d.Snowmen[sm.Index] = sm.Stage; }
        MMSave.Write(d);
    }

    void LoadGame(bool fullHearts)
    {
        var d = MMSave.Read();
        if (d == null) return;
        Coins = d.Coins; Hearts = fullHearts ? MaxHearts : Mathf.Clamp(d.Hearts, 1, MaxHearts);
        Wishes = d.Wishes; LettersFound = d.LettersFound; PlayTime = d.PlayTime; SpeedBoostT = d.SpeedBoostT;
        CatQuestStarted = d.CatQuestStarted; CatFound = d.CatFound; CatReturned = d.CatReturned;
        LetterQuestStarted = d.LetterQuestStarted; LettersReturned = d.LettersReturned;
        TeaQuestStarted = d.TeaQuestStarted; HasTea = d.HasTea; TeaDelivered = d.TeaDelivered;
        HasCharm = d.HasCharm; HasSneakers = d.HasSneakers;
        if (DayNight != null) { DayNight.SetTime(d.TimeOfDay); DayNight.Day = Mathf.Max(1, d.Day); }
        if (MMSeasons.I != null) MMSeasons.I.Load(d.Season, d.SeasonDay);
        if (MMGarden.I != null) MMGarden.I.Load(d.Plots);
        if (d.Snowmen != null) foreach (var sm in FindObjectsByType<MMSnowman>(FindObjectsInactive.Include)) if (sm.Index < d.Snowmen.Count) sm.Stage = d.Snowmen[sm.Index];
        if (Player != null)
        {
            var pos = new Vector2(d.PlayerX, d.PlayerY);
            Player.Teleport(pos);
            var room = MMRoom.Find(pos);
            CurrentRoom = room;
            SetZone(MMZone.Find(room != null ? room.MapPosition : pos) ?? MMZone.Main);
            SetRoom(room);
        }
        Collected.Clear();
        foreach (var id in d.Collected) Collected.Add(id);
        foreach (var p in FindObjectsByType<MMPickup>(FindObjectsInactive.Include))
            if (Collected.Contains(p.Id)) Destroy(p.gameObject);
        foreach (var b in MMFlowerbed.All) if (Collected.Contains(b.Id)) b.SetBloom();
        var inv = MMInventory.I;
        if (inv != null)
        {
            if (d.Inventory != null && d.Inventory.Count > 0) { inv.Load(d.Inventory); inv.Water = d.Water; inv.Selected = Mathf.Clamp(d.Selected, 0, MMInventory.HotbarSize - 1); }
            else GiveStartItems();
        }
        var cat = FindAnyObjectByType<MMCat>(FindObjectsInactive.Include);
        if (cat != null) cat.Restore(d.CatMode, new Vector3(d.CatX, d.CatY, 0));
        if (CatFound && !CatReturned && cat != null && Player != null) cat.transform.position = Player.transform.position + new Vector3(-0.8f, 0, 0);
    }

    void OnApplicationQuit()
    {
        if (State == MMState.Playing || State == MMState.Map || State == MMState.Journal || State == MMState.Inventory || State == MMState.Paused || State == MMState.Achievements) SaveGame();
    }
}
