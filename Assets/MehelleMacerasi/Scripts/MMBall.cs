using System.Collections.Generic;
using UnityEngine;

// Atılmış top: diyirlənir, divarlardan sıçrayır, üstünə yeriyəndə təpiklənir, E ilə geri götürülür.
public class MMBall : MMInteractable
{
    public static readonly List<MMBall> Balls = new List<MMBall>();
    public bool Carried { get; private set; }
    Rigidbody2D rb;
    Transform carrier;
    float bounceSoundT;

    public bool Resting => !Carried && rb != null && rb.linearVelocity.sqrMagnitude < 0.05f;
    public override bool CanInteract => !Carried && rb != null && rb.linearVelocity.sqrMagnitude < 1.5f;

    protected override void OnEnable() { base.OnEnable(); Balls.Add(this); }
    protected override void OnDisable() { base.OnDisable(); Balls.Remove(this); }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // səhnədə qoyulmuş top üçün sıçrama materialı (fayl kimi saxlanmır)
        var col = GetComponent<Collider2D>();
        if (col != null && col.sharedMaterial == null) col.sharedMaterial = new PhysicsMaterial2D("BallBounce") { bounciness = 0.65f, friction = 0f };
    }

    public override void Interact(MMPlayer player)
    {
        MMInventory.I.Add("ball");
        MMAudio.Play("coin");
        MMGame.I.UI.Toast("Topu götürdün.");
        Destroy(gameObject);
    }

    // it ağzında aparır
    public void Carry(Transform t)
    {
        Carried = true;
        carrier = t;
        rb.simulated = false;
        SetHighlight(false);
    }

    public void Drop(Vector3 p)
    {
        Carried = false;
        carrier = null;
        transform.position = p;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
    }

    void Update()
    {
        if (Carried && carrier != null)
        {
            var sr = carrier.GetComponent<SpriteRenderer>();
            float side = sr != null && sr.flipX ? -0.45f : 0.45f;
            transform.position = carrier.position + new Vector3(side, 0.25f, 0);
        }
        bounceSoundT -= Time.deltaTime;
        var g = MMGame.I;
        if (rb != null) rb.simulated = !Carried && (g == null || !g.WorldPaused);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        var p = c.collider.GetComponentInParent<MMPlayer>();
        if (p != null)
        {
            // təpik: oyunçunun baxdığı istiqamətdə
            rb.linearVelocity = p.Facing * 7.5f + Random.insideUnitCircle * 0.6f;
            MMAudio.Play("bounce");
            return;
        }
        if (c.relativeVelocity.magnitude > 2f && bounceSoundT <= 0) { MMAudio.Play("bounce"); bounceSoundT = 0.15f; }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var car = other.GetComponentInParent<MMCar>();
        if (car != null && car.CurrentSpeed > 0.5f && !Carried)
        {
            rb.linearVelocity = new Vector2(car.Dir * 9f, Random.Range(-2.5f, 2.5f));
            MMAudio.Play("bounce");
        }
    }
}
