using System.Collections.Generic;
using UnityEngine;

// Yolda hərəkət edən maşın. Zolaqlı keçiddə oyunçuya və piyadalara yol verir, öndəki maşına dəymir.
[RequireComponent(typeof(Rigidbody2D))]
public class MMCar : MonoBehaviour
{
    public static readonly List<MMCar> All = new List<MMCar>();
    public float Speed = 3.5f;
    public int Dir = 1;          // +1 sağa, -1 sola
    public float EndX = 60f;
    public float LaneY;
    Rigidbody2D rb;
    float curSpeed;

    public float CurrentSpeed => curSpeed;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        curSpeed = Speed;
    }

    bool InFrontOnCrosswalk(Vector2 p, float x)
    {
        var g = MMGame.I;
        foreach (var r in g.Crosswalks)
        {
            if (!r.Contains(p)) continue;
            float dx = (r.center.x - x) * Dir;
            if (dx > 0.6f && dx < 3.8f) return true;
        }
        return false;
    }

    bool ShouldStop()
    {
        float x = transform.position.x;
        // öndəki maşın
        foreach (var c in All)
        {
            if (c == this || Mathf.Abs(c.LaneY - LaneY) > 0.1f) continue;
            float dx = (c.transform.position.x - x) * Dir;
            if (dx > 0 && dx < 3.3f) return true;
        }
        var g = MMGame.I;
        if (g == null) return false;
        // zolaqlı keçiddə oyunçu və ya piyada
        if (g.Player != null && InFrontOnCrosswalk(g.Player.transform.position, x)) return true;
        foreach (var w in MMWalker.All)
            if (w.Visible && InFrontOnCrosswalk(w.transform.position, x)) return true;
        return false;
    }

    void FixedUpdate()
    {
        var g = MMGame.I;
        bool frozen = g != null && g.WorldPaused;
        float target = frozen || ShouldStop() ? 0f : Speed;
        curSpeed = Mathf.MoveTowards(curSpeed, target, (target > curSpeed ? 4f : 14f) * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + new Vector2(Dir * curSpeed * Time.fixedDeltaTime, 0));
        if ((transform.position.x - EndX) * Dir > 0) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
    void OnTriggerStay2D(Collider2D other) { TryHit(other); }

    void TryHit(Collider2D other)
    {
        if (curSpeed < 0.6f) return;
        var p = other.GetComponentInParent<MMPlayer>();
        if (p != null) p.HitByCar(transform.position);
    }
}
