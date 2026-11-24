using UnityEngine;
using UnityEngine.InputSystem;

// Oyunçunun hərəkəti, animasiyası, danışma və maşın zərbəsi.
[RequireComponent(typeof(Rigidbody2D))]
public class MMPlayer : MonoBehaviour
{
    public float Speed = 4.5f;
    public float InteractRadius = 1.5f;
    public SpriteRenderer Body;
    public Sprite[] Down = new Sprite[3], Up = new Sprite[3], Side = new Sprite[3];

    Rigidbody2D rb;
    Vector2 move;
    Vector2 facing = Vector2.down;
    float animT;
    float invuln;
    float stepT;
    Vector2 lastSafe;
    MMInteractable current;

    public Vector2 Facing => facing;
    public bool IsMoving => move.sqrMagnitude > 0.01f;

    public void SetFacing(Vector2 f)
    {
        facing = f;
        Animate();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (Body == null) Body = GetComponent<SpriteRenderer>();
        lastSafe = transform.position;
    }

    void Update()
    {
        var g = MMGame.I;
        var kb = Keyboard.current;
        move = Vector2.zero;
        if (g != null && g.State == MMState.Playing && kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
            if (move.sqrMagnitude > 0.01f)
            {
                move.Normalize();
                facing = Mathf.Abs(move.x) > Mathf.Abs(move.y) ? new Vector2(Mathf.Sign(move.x), 0) : new Vector2(0, Mathf.Sign(move.y));
            }

            var near = MMInteractable.FindNearest(transform.position, InteractRadius);
            if (near != current)
            {
                if (current != null) current.SetHighlight(false);
                current = near;
                if (current != null) current.SetHighlight(true);
            }
            if (current != null && Time.frameCount != g.UI.ClosedFrame && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                current.Interact(this);

            if (!g.IsRoad(transform.position)) lastSafe = transform.position;
        }
        else if (current != null)
        {
            current.SetHighlight(false);
            current = null;
        }

        Animate();

        if (invuln > 0)
        {
            invuln -= Time.deltaTime;
            Body.enabled = invuln <= 0 || Mathf.FloorToInt(invuln * 12f) % 2 == 0;
        }
    }

    void FixedUpdate()
    {
        var g = MMGame.I;
        float mul = g != null ? g.SpeedMultiplier : 1f;
        rb.linearVelocity = move * Speed * mul;
    }

    void Animate()
    {
        Sprite[] set = facing.y > 0.5f ? Up : facing.y < -0.5f ? Down : Side;
        Body.flipX = facing.x < -0.5f;
        if (move.sqrMagnitude > 0.01f)
        {
            var g = MMGame.I;
            float mul = g != null ? g.SpeedMultiplier : 1f;
            animT += Time.deltaTime * 8f * mul;
            int f = Mathf.FloorToInt(animT) % 4;
            Body.sprite = set[f == 1 ? 1 : f == 3 ? 2 : 0];
            stepT += Time.deltaTime * mul;
            if (stepT > 0.3f) { stepT = 0; MMAudio.Play("step"); }
        }
        else
        {
            animT = 0;
            Body.sprite = set[0];
        }
    }

    public void FaceTowards(Vector2 target)
    {
        Vector2 d = target - (Vector2)transform.position;
        facing = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? new Vector2(Mathf.Sign(d.x), 0) : new Vector2(0, Mathf.Sign(d.y));
    }

    public void Teleport(Vector2 p)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.position = p;
        transform.position = p;
        lastSafe = p;
    }

    public void HitByCar(Vector2 carPos)
    {
        if (invuln > 0 || MMGame.I.State != MMState.Playing) return;
        invuln = 1.6f;
        MMGame.I.Damage();
        rb.position = lastSafe;
        transform.position = lastSafe;
        MMCameraFollow.Shake(0.25f);
    }
}
