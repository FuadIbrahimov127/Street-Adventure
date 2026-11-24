using UnityEngine;

// Parkda kiçik canlılar: gündüz kəpənəklər uçur, gecə atəşböcəkləri parıldayır.
public class MMCritter : MonoBehaviour
{
    public enum Kind { Butterfly, Firefly }
    public Kind Type;
    public SpriteRenderer Body;
    public Sprite[] Frames;
    public float Radius = 2.2f;

    Vector3 home;
    float t, seed, alpha;
    Color baseColor;

    void Start()
    {
        home = transform.position;
        seed = Random.value * 100f;
        t = seed;
        baseColor = Body.color;
        alpha = 0;
    }

    void Update()
    {
        var g = MMGame.I;
        if (g != null && g.WorldPaused) return;
        float night = MMDayNight.I != null ? MMDayNight.I.Night : 0f;
        bool rain = MMWeather.I != null && MMWeather.I.IsRaining;
        // kəpənəklər yazda və yayda, atəşböcəkləri qışdan başqa hər fəsildə
        var season = MMSeasons.I != null ? MMSeasons.I.Season : MMSeason.Spring;
        bool seasonOk = Type == Kind.Butterfly ? season == MMSeason.Spring || season == MMSeason.Summer : season != MMSeason.Winter;
        bool want = !rain && seasonOk && (Type == Kind.Butterfly ? night < 0.3f : night > 0.65f);
        alpha = Mathf.MoveTowards(alpha, want ? 1f : 0f, Time.deltaTime * 0.8f);
        Body.enabled = alpha > 0.01f;
        if (!Body.enabled) return;

        t += Time.deltaTime;
        Vector3 prev = transform.position;
        if (Type == Kind.Butterfly)
        {
            float x = Mathf.Sin(t * 0.55f + seed) * Radius + Mathf.Sin(t * 1.7f + seed * 2f) * 0.4f;
            float y = Mathf.Sin(t * 0.8f + seed * 3f) * Radius * 0.5f + 0.9f + Mathf.Abs(Mathf.Sin(t * 5f)) * 0.15f;
            transform.position = home + new Vector3(x, y, 0);
            if (Frames != null && Frames.Length > 1) Body.sprite = Frames[Mathf.FloorToInt(t * 9f) % 2];
            if (Mathf.Abs(transform.position.x - prev.x) > 0.001f) Body.flipX = transform.position.x < prev.x;
            var c = baseColor; c.a = alpha; Body.color = c;
        }
        else
        {
            float x = Mathf.PerlinNoise(t * 0.25f, seed) * 2f - 1f;
            float y = Mathf.PerlinNoise(seed, t * 0.25f) * 2f - 1f;
            transform.position = home + new Vector3(x * Radius, y * Radius * 0.6f + 0.7f, 0);
            float pulse = 0.35f + 0.65f * Mathf.Max(0f, Mathf.Sin(t * 2.3f + seed));
            var c = baseColor; c.a = alpha * pulse; Body.color = c;
        }
    }
}
