using UnityEngine;
using UnityEngine.Rendering.Universal;

// Gecə-gündüz dövrü: oyun saatı, qlobal işığın rəngi/gücü, gecə işıqları və musiqi keçidi.
public class MMDayNight : MonoBehaviour
{
    public static MMDayNight I;

    public Light2D GlobalLight;
    public float TimeOfDay = 8 * 60f;        // dəqiqə (0..1440)
    public float MinutesPerSecond = 2f;      // 1 real saniyə = 2 oyun dəqiqəsi (tam gün 12 dəqiqə)

    public float Night { get; private set; } // 0 = gündüz, 1 = tam gecə
    public float WeatherDim;                 // yağışda hava tutulur (0..1), MMWeather təyin edir
    public int Day = 1;                      // oyun günü (gecə yarısı artır)

    struct Key
    {
        public float Hour; public Color Color; public float Intensity;
        public Key(float h, float r, float g, float b, float i) { Hour = h; Color = new Color(r, g, b); Intensity = i; }
    }

    // saata görə işıq: səhər çəhrayı, gündüz ağ, axşam narıncı, gecə mavi
    static readonly Key[] Keys =
    {
        new Key(0f, 0.40f, 0.46f, 0.86f, 0.33f),
        new Key(5f, 0.40f, 0.46f, 0.86f, 0.33f),
        new Key(6.5f, 1.00f, 0.76f, 0.66f, 0.78f),
        new Key(8f, 1.00f, 0.98f, 0.95f, 1.00f),
        new Key(16.5f, 1.00f, 0.98f, 0.95f, 1.00f),
        new Key(18.5f, 1.00f, 0.74f, 0.52f, 0.86f),
        new Key(20f, 0.62f, 0.54f, 0.86f, 0.58f),
        new Key(21.5f, 0.40f, 0.46f, 0.86f, 0.33f),
        new Key(24f, 0.40f, 0.46f, 0.86f, 0.33f),
    };

    public int Hour => Mathf.FloorToInt(TimeOfDay / 60f) % 24;
    public int Minute => Mathf.FloorToInt(TimeOfDay % 60f);
    public bool IsNight => Night > 0.5f;
    // Stardew kimi saat 10 dəqiqəlik addımlarla göstərilir
    public string ClockText => $"{Hour:00}:{(Minute / 10) * 10:00}";

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        Apply();
    }

    void Update()
    {
        var g = MMGame.I;
        if (g != null && g.State == MMState.Playing)
        {
            TimeOfDay += MinutesPerSecond * Time.deltaTime;
            if (TimeOfDay >= 1440f) { TimeOfDay -= 1440f; AdvanceDay(); }
        }
        Apply();
    }

    // yeni gün: bitkilər böyüyür, fəsil irəliləyir
    public void AdvanceDay()
    {
        Day++;
        if (MMGarden.I != null) MMGarden.I.OnNewDay();
        if (MMSeasons.I != null) MMSeasons.I.OnNewDay();
    }

    // yataqda yatanda: növbəti səhər saat 6:00
    public void SleepToMorning()
    {
        if (TimeOfDay >= 6 * 60f) AdvanceDay();
        SetTime(6 * 60f);
    }

    public void SetTime(float minutes)
    {
        TimeOfDay = Mathf.Repeat(minutes, 1440f);
        Apply();
    }

    public void Apply()
    {
        float h = TimeOfDay / 60f;
        Key a = Keys[0], b = Keys[Keys.Length - 1];
        for (int i = 0; i < Keys.Length - 1; i++)
        {
            if (h >= Keys[i].Hour && h <= Keys[i + 1].Hour) { a = Keys[i]; b = Keys[i + 1]; break; }
        }
        float t = Mathf.InverseLerp(a.Hour, b.Hour, h);
        t = t * t * (3f - 2f * t);
        Color c = Color.Lerp(a.Color, b.Color, t);
        float inten = Mathf.Lerp(a.Intensity, b.Intensity, t);
        // yağışlı hava: boz-mavi və bir az qaranlıq
        c = Color.Lerp(c, new Color(c.r * 0.72f, c.g * 0.78f, c.b * 0.92f), WeatherDim);
        inten *= 1f - 0.3f * WeatherDim;
        if (GlobalLight != null)
        {
            GlobalLight.color = c;
            GlobalLight.intensity = inten;
        }
        Night = Mathf.Clamp01(Mathf.InverseLerp(0.95f, 0.45f, inten));
        foreach (var l in MMNightLight.All) l.Apply(Night);
        MMAudio.SetNightMusic(h >= 19.5f || h < 6f);
    }
}
