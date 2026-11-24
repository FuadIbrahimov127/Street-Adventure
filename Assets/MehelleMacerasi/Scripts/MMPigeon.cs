using UnityEngine;

// Göyərçin: yerdə dənləyir, kimsə yaxınlaşanda uçub gedir, bir az sonra geri qonur.
public class MMPigeon : MonoBehaviour
{
    public static readonly System.Collections.Generic.List<MMPigeon> All = new System.Collections.Generic.List<MMPigeon>();
    public SpriteRenderer Body;
    public Sprite[] Frames = new Sprite[4];   // 0 dayanır, 1 dənləyir, 2-3 uçur
    public float ScareRadius = 1.8f;

    enum Phase { Ground, FlyAway, Gone, Landing, ToFood }
    Phase phase = Phase.Ground;
    Vector3 home;
    Vector3 flyDir;
    float t, animT;
    bool pecking;
    MMCrumbs food;
    Vector3 foodSpot;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public bool OnGround => phase == Phase.Ground || phase == Phase.ToFood;

    // qorxub uçur (oyunçu, piyada, yağış və ya qartopu)
    public void Flee(Vector3 from)
    {
        var away = (transform.position - from); away.z = 0;
        if (away.sqrMagnitude < 0.01f) away = Vector3.right;
        flyDir = (away.normalized + Vector3.up * 1.3f).normalized;
        Body.flipX = flyDir.x < 0;
        phase = Phase.FlyAway; t = 1.4f;
        food = null;
        MMAudio.Play("flap");
    }

    void Start()
    {
        home = transform.position;
        t = Random.Range(0.3f, 1.5f);
        Body.flipX = Random.value < 0.5f;
    }

    void SetAlpha(float a)
    {
        var c = Body.color; c.a = a; Body.color = c;
    }

    bool Threat(out Vector3 from)
    {
        from = Vector3.zero;
        if (food != null) return false;     // çörək yeyəndə qorxmur
        var g = MMGame.I;
        if (g != null && g.Player != null && (g.Player.transform.position - transform.position).sqrMagnitude < ScareRadius * ScareRadius)
        {
            from = g.Player.transform.position;
            return true;
        }
        foreach (var w in MMWalker.All)
            if (w.Visible && (w.transform.position - transform.position).sqrMagnitude < 0.9f) { from = w.transform.position; return true; }
        return false;
    }

    void Update()
    {
        var g = MMGame.I;
        if (g != null && g.WorldPaused) return;
        float dt = Time.deltaTime;
        switch (phase)
        {
            case Phase.Ground:
                t -= dt;
                if (t <= 0)
                {
                    pecking = !pecking;
                    t = pecking ? Random.Range(0.2f, 0.5f) : Random.Range(0.6f, 2f);
                    if (!pecking && Random.value < 0.3f) Body.flipX = !Body.flipX;
                }
                Body.sprite = Frames[pecking ? 1 : 0];
                if (food == null)
                {
                    var c = MMCrumbs.Nearest(transform.position, 10f);
                    if (c != null && (c.transform.position - transform.position).sqrMagnitude > 0.5f)
                    {
                        food = c;
                        foodSpot = c.transform.position + new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(-0.3f, 0.3f), 0);
                        phase = Phase.ToFood;
                        break;
                    }
                }
                // yağış yağanda göyərçinlər uçub gizlənir
                bool rainy = MMWeather.I != null && MMWeather.I.IsRaining;
                Vector3 from = transform.position + new Vector3(Random.Range(-1f, 1f), -1f, 0);
                if (rainy || Threat(out from)) Flee(from);
                break;
            case Phase.FlyAway:
                t -= dt;
                transform.position += flyDir * 4.5f * dt;
                Flap(dt);
                SetAlpha(Mathf.Clamp01(t / 0.6f));
                if (t <= 0) { phase = Phase.Gone; t = Random.Range(6f, 12f); Body.enabled = false; }
                break;
            case Phase.Gone:
                t -= dt;
                if (t <= 0 && (MMWeather.I == null || !MMWeather.I.IsRaining) && (g == null || g.Player == null || (g.Player.transform.position - home).sqrMagnitude > 16f))
                {
                    phase = Phase.Landing; t = 1.2f;
                    transform.position = home + new Vector3(Random.Range(-3f, 3f), 4f, 0);
                    Body.enabled = true;
                    Body.flipX = transform.position.x > home.x;
                }
                break;
            case Phase.ToFood:
                if (food == null) { home = transform.position; phase = Phase.Ground; break; }
                Body.flipX = foodSpot.x < transform.position.x;
                transform.position = Vector3.MoveTowards(transform.position, foodSpot, 3.5f * dt);
                Flap(dt);
                if ((transform.position - foodSpot).sqrMagnitude < 0.0005f) { home = foodSpot; phase = Phase.Ground; pecking = true; t = 0.3f; }
                break;
            case Phase.Landing:
                t -= dt;
                transform.position = Vector3.MoveTowards(transform.position, home, 4f * dt);
                Flap(dt);
                SetAlpha(Mathf.Clamp01(1.2f - t));
                if ((transform.position - home).sqrMagnitude < 0.001f) { phase = Phase.Ground; SetAlpha(1); t = 1f; }
                break;
        }
    }

    void Flap(float dt)
    {
        animT += dt * 12f;
        Body.sprite = Frames[2 + Mathf.FloorToInt(animT) % 2];
    }
}
