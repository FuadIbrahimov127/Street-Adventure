using System.Collections.Generic;
using UnityEngine;

public enum MMWeatherMode { Auto, Sunny, Rainy }

// Hava: arabir yağış yağır (qışda qar). Damcılar, sıçrayışlar, gölməçələr, qaralma, gecə ildırım və göy gurultusu.
public class MMWeather : MonoBehaviour
{
    public static MMWeather I;
    public static MMWeatherMode Mode = MMWeatherMode.Auto;

    [Header("Sprites")]
    public Sprite DropSprite;
    public Sprite SnowSprite;
    public Sprite[] SplashFrames;
    public Sprite[] RippleFrames;
    public SpriteRenderer[] Puddles;
    public int MaxDrops = 260;

    public float Rain { get; private set; }     // 0 = quru, 1 = güclü yağış (qışda qar)
    static bool Snowy => MMSeasons.I != null && MMSeasons.I.IsWinter;
    public bool IsRaining => Rain > 0.3f && !Snowy;
    public bool IsSnowing => Rain > 0.3f && Snowy;
    bool dropsAreSnow;

    float autoTarget;
    float timer = 150f;          // ilk yağışa qədər (real saniyə)
    float puddleLevel;
    float lightningT = 8f;
    float umbrellaT;
    readonly List<Transform> drops = new List<Transform>();
    readonly List<Vector2> dropEnd = new List<Vector2>();
    readonly List<SpriteRenderer> splashes = new List<SpriteRenderer>();
    readonly List<float> splashT = new List<float>();
    int splashNext;
    Transform fx;

    void Awake()
    {
        I = this;
        Mode = (MMWeatherMode)PlayerPrefs.GetInt("mm_weather", 0);
    }

    public static void SetMode(MMWeatherMode m)
    {
        Mode = m;
        PlayerPrefs.SetInt("mm_weather", (int)m);
        PlayerPrefs.Save();
    }

    void Start()
    {
        fx = new GameObject("RainFX").transform;
        fx.SetParent(transform, false);
        for (int i = 0; i < MaxDrops; i++)
        {
            var d = new GameObject("Drop");
            d.transform.SetParent(fx, false);
            var sr = d.AddComponent<SpriteRenderer>();
            sr.sprite = DropSprite; sr.sortingOrder = 30;
            d.SetActive(false);
            drops.Add(d.transform);
            dropEnd.Add(Vector2.zero);
        }
        for (int i = 0; i < 40; i++)
        {
            var s = new GameObject("Splash");
            s.transform.SetParent(fx, false);
            var sr = s.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -5;
            s.SetActive(false);
            splashes.Add(sr);
            splashT.Add(0);
        }
        SetPuddles(0);
    }

    void Update()
    {
        var g = MMGame.I;
        if (g == null) return;
        float dt = Time.deltaTime;
        bool playing = g.State == MMState.Playing;

        // avtomatik rejim: arabir yağış başlayır və kəsir
        if (playing && Mode == MMWeatherMode.Auto)
        {
            timer -= dt;
            if (timer <= 0)
            {
                autoTarget = autoTarget > 0 ? 0f : Random.Range(0.6f, 1f);
                // qışda qar daha tez-tez yağır
                timer = autoTarget > 0 ? Random.Range(60f, 140f) : Snowy ? Random.Range(50f, 110f) : Random.Range(150f, 300f);
            }
        }
        float target = Mode == MMWeatherMode.Sunny ? 0f : Mode == MMWeatherMode.Rainy ? 1f : autoTarget;
        float prev = Rain;
        Rain = Mathf.MoveTowards(Rain, target, dt / 5f);
        if (prev <= 0.3f && Rain > 0.3f && playing)
        {
            bool hasUmbrella = MMInventory.I != null && MMInventory.I.Count("umbrella") > 0;
            if (Snowy) g.UI.Toast("Qar yağır! [G] ilə qartopu at, parkda qar adamı düzəlt.");
            else g.UI.Toast(hasUmbrella ? "Yağış başladı! Çətiri seç və [F] ilə aç." : "Yağış başladı! Marketdən çətir ala bilərsən.");
        }

        bool indoors = g.CurrentRoom != null;
        if (MMDayNight.I != null) MMDayNight.I.WeatherDim = Rain * (Snowy ? 0.4f : 1f);
        MMAudio.SetRain(Snowy ? 0f : Rain * (indoors ? 0.3f : 1f));

        UpdateDrops(dt, indoors || g.State == MMState.Menu ? 0f : Rain);
        UpdateSplashes(dt);

        // gölməçələr: yağışda yavaş-yavaş əmələ gəlir, sonra quruyur
        puddleLevel = Mathf.MoveTowards(puddleLevel, IsRaining ? 1f : 0f, dt / (IsRaining ? 15f : Snowy ? 4f : 50f));
        SetPuddles(puddleLevel);

        // gecə güclü yağışda ildırım
        if (playing && !indoors && !Snowy && Rain > 0.7f && MMDayNight.I != null && MMDayNight.I.Night > 0.5f)
        {
            lightningT -= dt;
            if (lightningT <= 0)
            {
                lightningT = Random.Range(9f, 22f);
                g.UI.Flash(new Color(0.85f, 0.9f, 1f), 0.35f);
                Invoke(nameof(Thunder), Random.Range(0.4f, 1.4f));
            }
        }

        // nailiyyət: yağışda çətirlə gəzmək
        if (playing && !indoors && !Snowy && Rain > 0.5f && MMInventory.I != null && MMInventory.I.UmbrellaOpen && g.Player != null && g.Player.IsMoving)
        {
            umbrellaT += dt;
            if (umbrellaT >= 15f) MMAchievements.Unlock("rain");
        }
    }

    void Thunder() { MMAudio.Play("thunder"); }

    void UpdateDrops(float dt, float intensity)
    {
        var cam = Camera.main;
        if (cam == null) return;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector3 c = cam.transform.position;
        bool snow = Snowy;
        // yağış ↔ qar: damcıların şəklini dəyiş
        if (snow != dropsAreSnow)
        {
            dropsAreSnow = snow;
            foreach (var d in drops) d.GetComponent<SpriteRenderer>().sprite = snow && SnowSprite != null ? SnowSprite : DropSprite;
        }
        int want = Mathf.RoundToInt(MaxDrops * intensity * (snow ? 0.7f : 1f));
        var vel = snow ? new Vector3(-0.4f, -2.3f, 0) : new Vector3(-2.2f, -15f, 0);
        for (int i = 0; i < drops.Count; i++)
        {
            var d = drops[i];
            bool on = i < want;
            if (!on)
            {
                if (d.gameObject.activeSelf && d.position.y < dropEnd[i].y) d.gameObject.SetActive(false);
                else if (d.gameObject.activeSelf) d.position += vel * dt;
                continue;
            }
            if (!d.gameObject.activeSelf)
            {
                Respawn(i, c, w, h, true);
                d.gameObject.SetActive(true);
            }
            d.position += vel * dt;
            // qar dənələri yellənə-yellənə düşür
            if (snow) d.position += new Vector3(Mathf.Sin(Time.time * 1.7f + i * 0.73f) * 0.5f * dt, 0, 0);
            // kamera uzaqlaşıbsa, dənəni ekranın içində yenidən yarat
            if (Mathf.Abs(d.position.x - c.x) > w + 4f || Mathf.Abs(d.position.y - c.y) > h + 5f) { Respawn(i, c, w, h, true); continue; }
            if (d.position.y <= dropEnd[i].y)
            {
                if (!snow && Random.value < 0.35f) Splash(dropEnd[i]);
                Respawn(i, c, w, h, false);
            }
        }
    }

    void Respawn(int i, Vector3 c, float w, float h, bool anywhere)
    {
        float x = Random.Range(c.x - w - 1f, c.x + w + 3f);
        float endY = Random.Range(c.y - h, c.y + h);
        float y = anywhere ? Random.Range(endY, c.y + h + 2f) : c.y + h + Random.Range(0.5f, 3f);
        drops[i].position = new Vector3(x, y, 0);
        bool snow = dropsAreSnow;
        float fall = (y - endY) / (snow ? 2.3f : 15f);
        dropEnd[i] = new Vector2(x - (snow ? 0.4f : 2.2f) * fall, endY);
    }

    void Splash(Vector2 p)
    {
        var s = splashes[splashNext];
        splashT[splashNext] = 0.18f;
        s.transform.position = p;
        s.sprite = SplashFrames != null && SplashFrames.Length > 0 ? SplashFrames[0] : null;
        s.gameObject.SetActive(true);
        splashNext = (splashNext + 1) % splashes.Count;
    }

    void UpdateSplashes(float dt)
    {
        for (int i = 0; i < splashes.Count; i++)
        {
            if (!splashes[i].gameObject.activeSelf) continue;
            splashT[i] -= dt;
            if (splashT[i] < 0.09f && SplashFrames.Length > 1) splashes[i].sprite = SplashFrames[1];
            if (splashT[i] <= 0) splashes[i].gameObject.SetActive(false);
        }
    }

    void SetPuddles(float level)
    {
        if (Puddles == null) return;
        int frame = Mathf.FloorToInt(Time.time * 6f);
        for (int i = 0; i < Puddles.Length; i++)
        {
            var p = Puddles[i];
            if (p == null) continue;
            var col = p.color; col.a = level; p.color = col;
            p.enabled = level > 0.01f;
            // yağış damcıları gölməçədə dairələr açır
            if (p.transform.childCount > 0)
            {
                var r = p.transform.GetChild(0).GetComponent<SpriteRenderer>();
                bool show = level > 0.3f && Rain > 0.3f;
                r.enabled = show;
                if (show && RippleFrames != null && RippleFrames.Length > 0) r.sprite = RippleFrames[(frame + i) % RippleFrames.Length];
            }
        }
    }
}
