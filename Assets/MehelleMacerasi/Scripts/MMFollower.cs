using UnityEngine;

// Ev heyvanı (it): sahibinin arxasınca gəzir. Yerdə top görəndə qaçıb götürür və oyunçuya gətirir.
public class MMFollower : MonoBehaviour
{
    public MMWalker Owner;
    public SpriteRenderer Body;
    public Sprite[] Idle = new Sprite[2], Walk = new Sprite[2];
    public float Speed = 2.4f;
    public float FetchRange = 11f;

    enum Mode { Follow, ToBall, Return }
    Mode mode = Mode.Follow;
    MMBall ball;
    float animT;
    Vector2 lastDir = Vector2.right;
    float barkT;
    Vector2 droppedAt = new Vector2(-999, -999);   // topu sonuncu dəfə qoyduğu yer
    float cooldown;

    void Update()
    {
        if (Owner == null || Body == null) return;
        var g = MMGame.I;
        bool visible = Owner.Visible;
        Body.enabled = visible;
        var sh = transform.Find("Shadow");
        if (sh != null) sh.gameObject.SetActive(visible);
        if (!visible)
        {
            if (ball != null && ball.Carried) ball.Drop(transform.position);
            ball = null; mode = Mode.Follow;
            transform.position = Owner.transform.position;
            return;
        }
        bool paused = g != null && g.WorldPaused;
        Vector2 pos = transform.position;
        Vector2 target = pos;
        float speed = Speed;

        switch (mode)
        {
            case Mode.Follow:
            {
                // yerdə dayanmış top varsa, qaç götür
                foreach (var b in MMBall.Balls)
                {
                    if (cooldown <= 0 && b.Resting && ((Vector2)b.transform.position - pos).magnitude < FetchRange &&
                        ((Vector2)b.transform.position - droppedAt).magnitude > 1.5f)
                    {
                        ball = b; mode = Mode.ToBall;
                        if (barkT <= 0) { MMAudio.Play("woof"); barkT = 2f; }
                        break;
                    }
                }
                Vector2 ov = Owner.Velocity;
                if (ov.sqrMagnitude > 0.01f) lastDir = ov.normalized;
                target = (Vector2)Owner.transform.position - lastDir * 0.9f + new Vector2(-lastDir.y, lastDir.x) * 0.25f;
                break;
            }
            case Mode.ToBall:
                if (ball == null || ball.Carried) { ball = null; mode = Mode.Follow; break; }
                target = ball.transform.position;
                speed = Speed * 2f;
                if ((target - pos).magnitude < 0.35f) { ball.Carry(transform); mode = Mode.Return; }
                break;
            case Mode.Return:
                if (ball == null) { mode = Mode.Follow; break; }
                Vector2 pp = g != null && g.Player != null ? (Vector2)g.Player.transform.position : (Vector2)Owner.transform.position;
                target = pp + (pos - pp).normalized * 0.9f;
                speed = Speed * 2f;
                if ((pp - pos).magnitude < 1.2f)
                {
                    droppedAt = pos + (pp - pos).normalized * 0.5f;
                    ball.Drop(droppedAt);
                    cooldown = 2f;
                    ball = null; mode = Mode.Follow;
                    MMAudio.Play("woof");
                    if (g != null) g.UI.Toast("Hav-hav! İt topu sənə gətirdi.");
                    MMAchievements.Unlock("dog");
                }
                break;
        }
        barkT -= Time.deltaTime;
        cooldown -= Time.deltaTime;

        Vector2 d = target - pos;
        bool moving = d.magnitude > 0.08f && !paused;
        if (moving)
        {
            float sp = mode == Mode.Follow && d.magnitude > 2f ? Speed * 1.6f : speed;
            transform.position = Vector2.MoveTowards(pos, target, sp * Time.deltaTime);
            if (Mathf.Abs(d.x) > 0.02f) Body.flipX = d.x < 0;
        }
        animT += Time.deltaTime * (moving ? 9f : 3f);
        var set = moving ? Walk : Idle;
        if (set[0] != null) Body.sprite = set[Mathf.FloorToInt(animT) % 2];
    }
}
