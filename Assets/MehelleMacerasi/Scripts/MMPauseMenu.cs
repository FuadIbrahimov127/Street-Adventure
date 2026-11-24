using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Fasilə menyusu (Esc): davam et, musiqi və səs səviyyəsi, hava, tam ekran, nailiyyətlər, menyuya çıxış.
// Ayarlar PlayerPrefs-də saxlanılır və oyun açılanda tətbiq olunur.
public class MMPauseMenu : MonoBehaviour
{
    public GameObject Panel;
    public Image[] Rows;
    public Text[] Labels;
    public Sprite RowNormal, RowSelected;

    const int Resume = 0, Music = 1, Sfx = 2, Weather = 3, SeasonRow = 4, Fullscreen = 5, Achievements = 6, Quit = 7;
    public const int AchievementsRow = Achievements;
    int selected;
    int openedFrame;

    public static float MusicLevel = 0.7f, SfxLevel = 0.8f;
    public static bool FullscreenOn = true;

    void Awake()
    {
        MusicLevel = PlayerPrefs.GetFloat("mm_music", 0.7f);
        SfxLevel = PlayerPrefs.GetFloat("mm_sfx", 0.8f);
        FullscreenOn = PlayerPrefs.GetInt("mm_fullscreen", 1) == 1;
    }

    void Start()
    {
        ApplySettings();
        if (Panel != null) Panel.SetActive(false);
    }

    static void ApplySettings()
    {
        MMAudio.SetVolumes(MusicLevel, SfxLevel);
        if (!Application.isEditor) Screen.fullScreen = FullscreenOn;
    }

    static void SaveSettings()
    {
        PlayerPrefs.SetFloat("mm_music", MusicLevel);
        PlayerPrefs.SetFloat("mm_sfx", SfxLevel);
        PlayerPrefs.SetInt("mm_fullscreen", FullscreenOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void Open() { Open(0); }

    public void Open(int select)
    {
        var g = MMGame.I;
        g.State = MMState.Paused;
        openedFrame = Time.frameCount;
        selected = Mathf.Clamp(select, 0, Rows.Length - 1);
        Panel.SetActive(true);
        g.UI.HudRoot.SetActive(false);
        Render();
        MMAudio.Play("blip");
    }

    public void Close()
    {
        var g = MMGame.I;
        Panel.SetActive(false);
        g.State = MMState.Playing;
        g.UI.ClosedFrame = Time.frameCount;
        g.UI.HudRoot.SetActive(true);
        g.UI.Refresh();
    }

    void Update()
    {
        var g = MMGame.I;
        var kb = Keyboard.current;
        if (g == null || kb == null) return;
        if (g.State == MMState.Playing)
        {
            if (kb.escapeKey.wasPressedThisFrame && Time.frameCount != g.UI.ClosedFrame) Open();
            return;
        }
        if (g.State != MMState.Paused || Time.frameCount == openedFrame) return;
        if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) { Select(selected - 1); }
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) { Select(selected + 1); }
        else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Adjust(-1);
        else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Adjust(1);
        else if (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Activate(selected);
    }

    public void Select(int i)
    {
        selected = (i + Rows.Length) % Rows.Length;
        MMAudio.Play("blip");
        Render();
    }

    void Adjust(int dir)
    {
        if (selected == Music) MusicLevel = Mathf.Clamp01(Mathf.Round((MusicLevel + dir * 0.1f) * 10f) / 10f);
        else if (selected == Sfx) SfxLevel = Mathf.Clamp01(Mathf.Round((SfxLevel + dir * 0.1f) * 10f) / 10f);
        else if (selected == Fullscreen) FullscreenOn = !FullscreenOn;
        else if (selected == Weather) CycleWeather(dir);
        else if (selected == SeasonRow) CycleSeason(dir);
        else return;
        ApplySettings();
        SaveSettings();
        MMAudio.Play("blip");
        Render();
    }

    // siçanla klik: sürgülər dövr edir (0 → 100%)
    public void Activate(int i)
    {
        selected = i;
        var g = MMGame.I;
        switch (i)
        {
            case Resume: Close(); return;
            case Music: MusicLevel = MusicLevel >= 0.99f ? 0f : Mathf.Round((MusicLevel + 0.1f) * 10f) / 10f; break;
            case Sfx: SfxLevel = SfxLevel >= 0.99f ? 0f : Mathf.Round((SfxLevel + 0.1f) * 10f) / 10f; break;
            case Fullscreen: FullscreenOn = !FullscreenOn; break;
            case Weather: CycleWeather(1); break;
            case SeasonRow: CycleSeason(1); break;
            case Achievements:
                if (MMAchievements.I == null) return;
                Panel.SetActive(false);
                MMAchievements.I.Open(true);
                return;
            case Quit:
                Panel.SetActive(false);
                g.QuitToMenu();
                return;
        }
        ApplySettings();
        SaveSettings();
        MMAudio.Play("blip");
        Render();
    }

    // hava rejimi: Avtomatik → Günəşli → Yağışlı
    static void CycleWeather(int dir)
    {
        int m = ((int)MMWeather.Mode + dir + 3) % 3;
        MMWeather.SetMode((MMWeatherMode)m);
    }

    // fəsli dərhal dəyiş (uşaqlara qışı tez göstərmək üçün)
    static void CycleSeason(int dir)
    {
        var s = MMSeasons.I;
        if (s == null) return;
        s.SetSeason((MMSeason)(((int)s.Season + dir + 4) % 4), true);
    }

    static string WeatherName(MMWeatherMode m)
    {
        return m == MMWeatherMode.Sunny ? "Günəşli" : m == MMWeatherMode.Rainy ? "Yağışlı" : "Avtomatik";
    }

    static string Bar(float v)
    {
        int n = Mathf.RoundToInt(v * 10f);
        return "<size=34><color=#c0392b>" + new string('•', n) + "</color><color=#c8b898>" + new string('•', 10 - n) + "</color></size>  " + Mathf.RoundToInt(v * 100f) + "%";
    }

    void Render()
    {
        string[] text =
        {
            "Davam et",
            "Musiqi:   " + Bar(MusicLevel),
            "Səslər:   " + Bar(SfxLevel),
            "Hava:   « " + WeatherName(MMWeather.Mode) + " »",
            "Fəsil:   « " + (MMSeasons.I != null ? MMSeasons.Names[(int)MMSeasons.I.Season] : "Yaz") + " »",
            "Tam ekran:   " + (FullscreenOn ? "Bəli" : "Xeyr"),
            "Nailiyyətlər   " + MMAchievements.UnlockedCount + "/" + MMAchievements.Table.Length,
            "Yadda saxla və menyuya çıx",
        };
        for (int i = 0; i < Rows.Length && i < text.Length; i++)
        {
            Rows[i].sprite = i == selected ? RowSelected : RowNormal;
            Labels[i].text = text[i];
        }
    }
}
