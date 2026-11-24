using UnityEngine;

// Uçan qartopu: düz uçur, kimə dəysə, partlayır.
public class MMSnowball : MonoBehaviour
{
    public Vector2 Dir;
    public float Speed = 9f, Life = 0.6f;
    public Transform Visual;
    public Sprite SplatSprite;
    float t;

    void Update()
    {
        var g = MMGame.I;
        if (g != null && g.WorldPaused) return;
        float dt = Time.deltaTime;
        t += dt;
        transform.position += (Vector3)(Dir * Speed * dt);
        // qövs boyunca uçur
        if (Visual != null) Visual.localPosition = new Vector3(0, 0.55f + Mathf.Sin(Mathf.Clamp01(t / Life) * Mathf.PI) * 0.35f, 0);
        if (MMSnowballs.TryHit(transform.position + new Vector3(0, 0.5f, 0), Dir)) { Splat(); return; }
        if (t >= Life) Splat();
    }

    void Splat()
    {
        var s = new GameObject("SnowSplat");
        s.transform.position = transform.position + new Vector3(0, 0.3f, 0);
        var sr = s.AddComponent<SpriteRenderer>();
        sr.sprite = SplatSprite; sr.sortingOrder = 1;
        s.AddComponent<MMFade>().Life = 0.7f;
        MMAudio.Play("snow");
        Destroy(gameObject);
    }
}
