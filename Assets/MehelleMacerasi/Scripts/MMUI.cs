using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Bütün ekran elementləri: HUD, saat, dialoq, xəritə, jurnal, mağaza, menyu, qələbə.
public class MMUI : MonoBehaviour
{
    [Header("HUD")]
    public Image[] HeartImages;
    public Sprite HeartFull, HeartEmpty;
    public Text CoinText;
    public Text QuestText;
    public GameObject QuestPanel;
    public GameObject HudRoot;
    public Text ToastText;
    public RectTransform ToastBox;

    [Header("Item icons")]
    public Image TeaIcon;
    public Image CatIcon;
    public Image CharmIcon;
    public Image ShoesIcon;
    public Image SpeedIcon;
    public Text SpeedText;

    [Header("Clock")]
    public Text ClockText;
    public Image ClockIcon;
    public Sprite SunSprite, MoonSprite;
    public Text SeasonText;
    public Image SeasonIcon;
    public Sprite SnowSprite;

    [Header("Dialogue")]
    public GameObject DialoguePanel;
    public Text DialogueName;
    public Text DialogueBody;
    public Text DialogueHint;
    public Image DialoguePortrait;
    public GameObject PortraitFrame;

    [Header("Map and journal")]
    public MMMap Map;
    public GameObject JournalPanel;
    public Text JournalText;

    [Header("Shop")]
    public GameObject ShopPanel;
    public Text ShopTitle;
    public Image[] ShopRows;
    public Image[] ShopIcons;
    public Text[] ShopNames;
    public Text[] ShopPrices;
    public Text ShopDesc;
    public Text ShopCoins;
    public Text ShopMessage;
    public Sprite RowNormal, RowSelected;

    [Header("Inventory")]
    public GameObject HotbarRoot;
    public MMSlotUI[] HotbarSlots;
    public GameObject BagPanel;
    public MMSlotUI[] BagSlots;
    public Text BagInfoName, BagInfoDesc;
    public Image DragIcon;
    public Text ItemNameText;
    public Image FlashImage;
    public Image FadeImage;
    public Sprite SlotNormal, SlotSelected;
    float itemNameT, flashT;
    bool photoMode;
    Color flashColor = Color.white;
    float flashMax = 0.9f;
    public Sprite RainSprite;

    [Header("Screens")]
    public GameObject MenuPanel;
    public Text MenuHint;
    public GameObject WinPanel;
    public Text WinStats;
    public Text WinTitle, WinText, WinHint;
    public GameObject GameOverPanel;
    public Text GameOverHint;

    string[] lines;
    int lineIdx;
    float typed;
    Action onDone;
    int openedFrame;
    public int ClosedFrame = -1;
    float toastT;
    const float CharsPerSec = 45f;

    const string Done = "#8fe07a";
    const string Todo = "#fff4dc";

    // ---------------- ekranlar ----------------
    public void ShowMenu(bool on, bool hasSave)
    {
        MenuPanel.SetActive(on);
        HudRoot.SetActive(!on);
        HideOverlays();
        WinPanel.SetActive(false);
        GameOverPanel.SetActive(false);
        DialoguePanel.SetActive(false);
        if (MenuHint != null)
            MenuHint.text = hasSave ? "[Enter] Davam et      [N] Yeni oyun      [Esc] Çıx" : "[Enter] Oyuna başla      [Esc] Çıx";
    }

    void HideOverlays()
    {
        if (Map != null) Map.SetOpen(false);
        if (JournalPanel != null) JournalPanel.SetActive(false);
        if (ShopPanel != null) ShopPanel.SetActive(false);
        if (BagPanel != null) BagPanel.SetActive(false);
    }

    public void ShowMap(bool on)
    {
        if (Map != null) Map.SetOpen(on);
        HudRoot.SetActive(!on);
    }

    public void ShowJournal(bool on, string highlightId)
    {
        if (JournalPanel == null) return;
        JournalPanel.SetActive(on);
        HudRoot.SetActive(!on);
        if (!on) return;
        JournalText.text = BuildJournal(highlightId);
        FitToScreen((RectTransform)JournalText.transform.parent, (RectTransform)JournalPanel.transform, 24f);
    }

    // kiçik ekranlarda pəncərə ekrana sığsın
    static void FitToScreen(RectTransform box, RectTransform area, float margin)
    {
        var pr = area.rect;
        float k = Mathf.Clamp(Mathf.Min((pr.height - margin) / box.rect.height, (pr.width - margin) / box.rect.width), 0.4f, 1f);
        box.localScale = new Vector3(k, k, 1f);
    }

    string BuildJournal(string highlightId)
    {
        var g = MMGame.I;
        var quests = MMQuests.Build(g);
        quests.Sort((a, b) => Rank(a, highlightId).CompareTo(Rank(b, highlightId)));
        var sb = new System.Text.StringBuilder();
        int notStarted = 0;
        bool any = false;
        foreach (var q in quests)
        {
            if (!q.Started) { notStarted++; continue; }
            any = true;
            if (q.Done)
            {
                sb.Append($"<color=#3f7d3b>{q.Title}   TAMAMLANDI</color>\n\n");
                continue;
            }
            sb.Append($"<size=26>{q.Title}</size>");
            if (q.Id == highlightId) sb.Append("   <color=#c0392b>YENİ!</color>");
            sb.Append($"\n<color=#8a5a32>Tapşırığı verən: {q.Giver}</color>\n");
            sb.Append(q.Description).Append("\n");
            bool currentShown = false;
            foreach (var st in q.Steps)
            {
                if (st.Done) sb.Append($"<color=#3f7d3b>   [x]  {st.Text}</color>\n");
                else if (!currentShown) { sb.Append($"   [  ]  {st.Text}   <color=#c0392b>« indi bunu et</color>\n"); currentShown = true; }
                else sb.Append($"<color=#9a8a72>   [  ]  {st.Text}</color>\n");
            }
            sb.Append("\n");
        }
        if (!any) sb.Append("Hələ heç bir tapşırığın yoxdur.\n\nBaşının üstündə sarı ! işarəsi olan qonşularla danış, onlar sənə tapşırıq verəcək.\n\n");
        else if (notStarted > 0) sb.Append($"<color=#8a7a62>Məhəllədə daha {notStarted} tapşırıq səni gözləyir. Sarı ! işarələrini axtar.</color>\n");
        sb.Append("\n<color=#6e4a2e>İpucu: fəvvarəyə 1 manat at, Aysel xalanın marketinə də baş çək!</color>");
        return sb.ToString();
    }

    static int Rank(MMQuestInfo q, string highlightId)
    {
        if (q.Id == highlightId) return 0;
        if (q.Started && !q.Done) return 1;
        if (q.Done) return 2;
        return 3;
    }

    public void ShowWin(int coins, float time, int wishes)
    {
        int m = Mathf.FloorToInt(time / 60f), s = Mathf.FloorToInt(time % 60f);
        ShowWinCustom(null, null, $"Pulların: {coins} manat     Arzular: {wishes}     Vaxt: {m:00}:{s:00}", null);
    }

    // qələbə ekranı: başlıq, mətn, statistika və ipucu dəyişə bilər
    public void ShowWinCustom(string title, string text, string stats, string hint)
    {
        HideOverlays();
        HudRoot.SetActive(false);
        DialoguePanel.SetActive(false);
        WinPanel.SetActive(true);
        WinPanel.transform.SetAsLastSibling();
        if (title != null && WinTitle != null) WinTitle.text = title;
        if (text != null && WinText != null) WinText.text = text;
        if (hint != null && WinHint != null) WinHint.text = hint;
        WinStats.text = stats;
    }

    public void HideWin()
    {
        WinPanel.SetActive(false);
        HudRoot.SetActive(true);
    }

    public void ShowGameOver(bool hasSave)
    {
        HideOverlays();
        HudRoot.SetActive(false);
        DialoguePanel.SetActive(false);
        GameOverPanel.SetActive(true);
        if (GameOverHint != null)
            GameOverHint.text = hasSave ? "[Enter] Son yaddaşdan davam et          [N] Yeni oyun" : "[Enter] Yenidən başla";
    }

    // ---------------- mağaza ----------------
    public void ShowShop(bool on)
    {
        if (ShopPanel == null) return;
        ShopPanel.SetActive(on);
        HudRoot.SetActive(!on);
        if (on) FitToScreen((RectTransform)ShopPanel.transform.GetChild(0), (RectTransform)ShopPanel.transform, 24f);
    }

    public void RenderShop(MMShop shop, int selected, string message)
    {
        var g = MMGame.I;
        if (ShopTitle != null) ShopTitle.text = shop.Title;
        for (int i = 0; i < ShopRows.Length; i++)
        {
            bool has = i < shop.Items.Count;
            ShopRows[i].gameObject.SetActive(has);
            if (!has) continue;
            var it = shop.Items[i];
            ShopRows[i].sprite = i == selected ? RowSelected : RowNormal;
            ShopIcons[i].sprite = MMInventory.I != null ? MMInventory.I.Icon(it.Id) : it.Icon;
            string status = shop.StatusOf(it);
            ShopNames[i].text = status != null ? $"{it.Name}  <color=#8a7a62>({status})</color>" : it.Name;
            ShopPrices[i].text = $"{it.Price} manat";
            ShopPrices[i].color = g.Coins >= it.Price ? new Color32(70, 42, 26, 255) : new Color32(176, 46, 54, 255);
        }
        ShopDesc.text = shop.Items[selected].Desc;
        ShopCoins.text = $"Səndə: {g.Coins} manat";
        ShopMessage.text = message;
    }

    // ---------------- HUD ----------------
    public void Toast(string msg)
    {
        ToastText.text = msg;
        toastT = 3.2f;
        ToastText.gameObject.SetActive(true);
        if (ToastBox != null)
        {
            ToastBox.gameObject.SetActive(true);
            ToastBox.sizeDelta = new Vector2(ToastText.preferredWidth + 56f, ToastBox.sizeDelta.y);
        }
    }

    public void Refresh()
    {
        var g = MMGame.I;
        if (g == null) return;
        if (MMInventory.I != null) MMInventory.I.SyncFlags(g);
        RefreshSlots();
        for (int i = 0; i < HeartImages.Length; i++) HeartImages[i].sprite = i < g.Hearts ? HeartFull : HeartEmpty;
        CoinText.text = g.Coins.ToString();
        if (TeaIcon) TeaIcon.gameObject.SetActive(g.HasTea);
        if (CatIcon) CatIcon.gameObject.SetActive(g.CatFound && !g.CatReturned);
        if (CharmIcon) CharmIcon.gameObject.SetActive(g.HasCharm);
        if (ShoesIcon) ShoesIcon.gameObject.SetActive(g.HasSneakers);
        LayoutItems();

        if (MMStory.Ch2(g)) { QuestText.text = MMStory.Panel(g).TrimEnd('\n'); return; }
        string q = "Tapşırıqlar\n";
        if (!g.CatQuestStarted && !g.CatFound) q += Line(Todo, "Sara nənə ilə danış");
        else if (!g.CatFound) q += Line(Todo, "Mestanı tap (parkda)");
        else if (!g.CatReturned) q += Line(Todo, "Mestanı Sara nənəyə apar");
        else q += Line(Done, "Mestan evdədir");

        if (!g.LetterQuestStarted && g.LettersFound < MMGame.LettersTotal) q += Line(Todo, "Poçtalyon Elçinlə danış");
        else if (g.LettersFound < MMGame.LettersTotal) q += Line(Todo, $"Məktubları tap ({g.LettersFound}/{MMGame.LettersTotal})");
        else if (!g.LettersReturned) q += Line(Todo, "Məktubları Elçinə ver");
        else q += Line(Done, "Məktublar çatdı");

        if (!g.TeaQuestStarted) q += Line(Todo, "Parkdakı babalarla danış");
        else if (g.TeaDelivered) q += Line(Done, "Babalar çay içir");
        else if (g.HasTea) q += Line(Todo, "Çayı babalara apar");
        else q += Line(Todo, $"Çayxanadan çay al ({MMGame.TeaPrice} manat)");
        QuestText.text = q.TrimEnd('\n');
    }

    // aktiv əşya ikonlarını yan-yana düz (boşluq qalmasın)
    void LayoutItems()
    {
        float x = 0;
        foreach (var img in new[] { CatIcon, TeaIcon, CharmIcon, ShoesIcon, SpeedIcon })
        {
            if (img == null || !img.gameObject.activeSelf) continue;
            var rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
            x += img == SpeedIcon ? 96f : 52f;
        }
    }

    static string Line(string col, string t) => $"<color={col}>• {t}</color>\n";

    // ---------------- çanta və əşya paneli ----------------
    void OnEnable()
    {
        if (MMInventory.I != null) MMInventory.I.Changed += RefreshSlots;
    }

    void Start()
    {
        if (MMInventory.I != null) { MMInventory.I.Changed -= RefreshSlots; MMInventory.I.Changed += RefreshSlots; }
        RefreshSlots();
    }

    void OnDisable()
    {
        if (MMInventory.I != null) MMInventory.I.Changed -= RefreshSlots;
    }

    public void RefreshSlots()
    {
        var inv = MMInventory.I;
        if (inv == null) return;
        if (HotbarSlots != null) foreach (var s in HotbarSlots) DrawSlot(s, inv);
        if (BagSlots != null && BagPanel != null && BagPanel.activeSelf) foreach (var s in BagSlots) DrawSlot(s, inv);
    }

    void DrawSlot(MMSlotUI ui, MMInventory inv)
    {
        if (ui == null) return;
        var slot = inv.Slots[ui.Index];
        bool sel = ui.Index == inv.Selected && ui.Index < MMInventory.HotbarSize;
        ui.Background.sprite = sel ? SlotSelected : SlotNormal;
        if (slot.Empty)
        {
            ui.Icon.enabled = false;
            ui.CountText.text = "";
            return;
        }
        ui.Icon.enabled = true;
        ui.Icon.sprite = inv.Icon(slot.Id);
        if (slot.Id == "can") ui.CountText.text = inv.Water + "/" + MMInventory.MaxWater;
        else ui.CountText.text = slot.Count > 1 ? slot.Count.ToString() : "";
    }

    public void ShowBag(bool on)
    {
        if (BagPanel == null) return;
        BagPanel.SetActive(on);
        HudRoot.SetActive(!on);
        if (!on) { EndDrag(); return; }
        FitToScreen((RectTransform)BagPanel.transform.GetChild(0), (RectTransform)BagPanel.transform, 24f);
        RefreshSlots();
        ShowItemInfo(MMInventory.I != null ? MMInventory.I.Selected : 0);
    }

    public void ShowItemInfo(int index)
    {
        var inv = MMInventory.I;
        if (inv == null || BagInfoName == null) return;
        var slot = inv.Slots[index];
        if (slot.Empty) { BagInfoName.text = ""; BagInfoDesc.text = "Boş xana. Əşyaları siçanla sürükləyib bura qoya bilərsən."; return; }
        var def = MMItems.Get(slot.Id);
        BagInfoName.text = def.Name + (slot.Count > 1 ? $"  x{slot.Count}" : "") + (slot.Id == "can" ? $"  (su: {inv.Water}/{MMInventory.MaxWater})" : "");
        BagInfoDesc.text = def.Desc;
    }

    public void BeginDrag(Sprite sp, Vector2 screenPos)
    {
        if (DragIcon == null) return;
        DragIcon.sprite = sp;
        DragIcon.gameObject.SetActive(true);
        DragIcon.transform.SetAsLastSibling();
        MoveDrag(screenPos);
    }

    public void MoveDrag(Vector2 screenPos)
    {
        if (DragIcon != null) DragIcon.rectTransform.position = screenPos;
    }

    public void EndDrag()
    {
        if (DragIcon != null) DragIcon.gameObject.SetActive(false);
    }

    public void ShowItemName(string name)
    {
        if (ItemNameText == null) return;
        ItemNameText.text = name;
        itemNameT = 1.8f;
    }

    // şəkil çəkiləndə HUD gizlənsin
    public void SetPhotoMode(bool on)
    {
        photoMode = on;
        HudRoot.SetActive(!on);
        if (HotbarRoot != null) HotbarRoot.SetActive(!on);
        if (ToastBox != null && on) ToastBox.gameObject.SetActive(false);
    }

    // qapıdan keçəndə ekran qaralır
    public void SetFade(float a)
    {
        if (FadeImage == null) return;
        FadeImage.gameObject.SetActive(a > 0.001f);
        FadeImage.color = new Color(0.03f, 0.02f, 0.05f, a);
        FadeImage.transform.SetAsLastSibling();
    }

    // ildırım: rəngli qısa parıltı
    public void Flash(Color c, float strength)
    {
        flashColor = c; flashMax = strength;
        flashT = 0.5f;
        if (FlashImage != null) { FlashImage.gameObject.SetActive(true); FlashImage.transform.SetAsLastSibling(); }
    }

    public void Flash()
    {
        flashColor = Color.white; flashMax = 0.9f;
        flashT = 0.5f;
        if (FlashImage != null) { FlashImage.gameObject.SetActive(true); FlashImage.transform.SetAsLastSibling(); }
    }

    // ---------------- dialoq ----------------
    public void ShowDialogue(string speaker, string[] newLines, Action done, Sprite portrait = null)
    {
        var g = MMGame.I;
        g.State = MMState.Dialogue;
        lines = newLines;
        lineIdx = 0;
        typed = 0;
        onDone = done;
        openedFrame = Time.frameCount;
        DialogueName.text = speaker;
        DialogueBody.text = "";
        if (DialoguePortrait != null)
        {
            DialoguePortrait.sprite = portrait;
            if (PortraitFrame != null) PortraitFrame.SetActive(portrait != null);
        }
        DialoguePanel.SetActive(true);
    }

    void Update()
    {
        if (toastT > 0)
        {
            toastT -= Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(toastT / 0.6f);
            var c = ToastText.color; c.a = a; ToastText.color = c;
            if (ToastBox != null)
            {
                var img = ToastBox.GetComponent<Image>();
                if (img != null) { var bc = img.color; bc.a = a; img.color = bc; }
            }
            if (toastT <= 0)
            {
                ToastText.gameObject.SetActive(false);
                if (ToastBox != null) ToastBox.gameObject.SetActive(false);
            }
        }

        var g = MMGame.I;
        if (HotbarRoot != null && g != null)
        {
            bool show = g.State == MMState.Playing && !photoMode;
            if (HotbarRoot.activeSelf != show) HotbarRoot.SetActive(show);
        }
        if (ItemNameText != null)
        {
            itemNameT -= Time.unscaledDeltaTime;
            var c = ItemNameText.color; c.a = Mathf.Clamp01(itemNameT / 0.4f); ItemNameText.color = c;
        }
        if (FlashImage != null && flashT > 0)
        {
            flashT -= Time.unscaledDeltaTime;
            FlashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, Mathf.Clamp01(flashT / 0.5f) * flashMax);
            if (flashT <= 0) FlashImage.gameObject.SetActive(false);
        }
        if (g != null && SpeedIcon != null)
        {
            bool on = g.SpeedBoostT > 0;
            if (SpeedIcon.gameObject.activeSelf != on) { SpeedIcon.gameObject.SetActive(on); LayoutItems(); }
            if (on) SpeedText.text = Mathf.CeilToInt(g.SpeedBoostT).ToString();
        }
        var dn = MMDayNight.I;
        if (dn != null && ClockText != null)
        {
            ClockText.text = dn.ClockText;
            var w = MMWeather.I;
            if (ClockIcon != null)
                ClockIcon.sprite = w != null && w.IsRaining && RainSprite != null ? RainSprite :
                                   w != null && w.IsSnowing && SnowSprite != null ? SnowSprite :
                                   dn.Hour >= 6 && dn.Hour < 19 ? SunSprite : MoonSprite;
            var se = MMSeasons.I;
            if (se != null)
            {
                if (SeasonText != null) SeasonText.text = se.Label;
                if (SeasonIcon != null) SeasonIcon.sprite = se.Icon;
            }
        }

        if (g == null || g.State != MMState.Dialogue || lines == null) return;
        string full = lines[lineIdx];
        int prev = Mathf.FloorToInt(typed);
        typed = Mathf.Min(full.Length, typed + CharsPerSec * Time.deltaTime);
        int now = Mathf.FloorToInt(typed);
        if (now != prev && now % 3 == 0 && now < full.Length) MMAudio.Play("blip");
        DialogueBody.text = full.Substring(0, now);
        DialogueHint.text = now >= full.Length ? (lineIdx < lines.Length - 1 ? "[E] davam »" : "[E] bağla") : "";

        var kb = Keyboard.current;
        if (kb == null || Time.frameCount == openedFrame) return;
        if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
        {
            if (now < full.Length) typed = full.Length;
            else if (lineIdx < lines.Length - 1) { lineIdx++; typed = 0; }
            else
            {
                DialoguePanel.SetActive(false);
                lines = null;
                ClosedFrame = Time.frameCount;
                g.State = MMState.Playing;
                var cb = onDone; onDone = null;
                cb?.Invoke();
                Refresh();
            }
        }
    }
}
