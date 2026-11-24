using UnityEngine;

// Bir yol zolağı: maşınları yaradır (gecə faraları ilə).
public class MMLane : MonoBehaviour
{
    public int Dir = 1;
    public float StartX = -3f, EndX = 51f;
    public float MinInterval = 2.4f, MaxInterval = 5.2f;
    public float MinSpeed = 3.0f, MaxSpeed = 4.6f;
    public Sprite[] CarSprites;
    public Sprite Shadow;
    public Sprite HeadlightSprite;
    public Material UnlitMaterial;
    float timer;

    void Start()
    {
        // başlanğıcda yolda bir neçə maşın olsun
        float x = StartX + Dir * Random.Range(3f, 8f);
        while ((EndX - x) * Dir > 4f)
        {
            Spawn(x);
            x += Dir * Random.Range(10f, 17f);
        }
        timer = Random.Range(MinInterval, MaxInterval);
    }

    void Update()
    {
        var g = MMGame.I;
        if (g != null && g.WorldPaused) return;
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            bool blocked = false;
            foreach (var c in MMCar.All)
                if (Mathf.Abs(c.LaneY - transform.position.y) < 0.1f && Mathf.Abs(c.transform.position.x - StartX) < 3.5f) blocked = true;
            if (!blocked) Spawn(StartX);
            timer = Random.Range(MinInterval, MaxInterval);
        }
    }

    void Spawn(float x)
    {
        if (CarSprites == null || CarSprites.Length == 0) return;
        var go = new GameObject("Car");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(x, transform.position.y, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CarSprites[Random.Range(0, CarSprites.Length)];
        sr.flipX = Dir < 0;
        sr.spriteSortPoint = SpriteSortPoint.Pivot;
        if (Shadow != null)
        {
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(go.transform, false);
            sh.transform.localPosition = new Vector3(0, 0.02f, 0);
            sh.transform.localScale = new Vector3(2.6f, 1.3f, 1);
            var ssr = sh.AddComponent<SpriteRenderer>();
            ssr.sprite = Shadow; ssr.sortingOrder = -2;
        }
        if (HeadlightSprite != null)
        {
            // gecə faralarının işıq konusu
            var hl = new GameObject("Headlight");
            hl.transform.SetParent(go.transform, false);
            hl.transform.localPosition = new Vector3(Dir * 2.1f, -0.2f, 0);
            var hsr = hl.AddComponent<SpriteRenderer>();
            hsr.sprite = HeadlightSprite;
            hsr.flipX = Dir < 0;
            hsr.sortingOrder = -1;
            if (UnlitMaterial != null) hsr.sharedMaterial = UnlitMaterial;
            var nl = hl.AddComponent<MMNightLight>();
            nl.Glow = hsr;
            nl.Threshold = 0.3f;
        }
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.2f, 0.6f);
        col.offset = new Vector2(0, 0.45f);
        var car = go.AddComponent<MMCar>();
        car.Dir = Dir;
        car.Speed = Random.Range(MinSpeed, MaxSpeed);
        car.EndX = EndX;
        car.LaneY = transform.position.y;
    }
}
