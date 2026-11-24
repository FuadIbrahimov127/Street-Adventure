using UnityEngine;
using UnityEngine.InputSystem;

// Qışda qartopu atmaq: [G] və ya boş xana seçilibsə [F].
// Piyadalar, qonşular, göyərçinlər və pişik qartopuna cavab verir.
public class MMSnowballs : MonoBehaviour
{
    public Sprite BallSprite, SplatSprite;
    float cooldown;

    static readonly string[] NpcLines =
    {
        "Ay-ay, soyuqdur! Hahaha!",
        "Hə? İndi görərsən, mən də atacağam!",
        "Qış gözəldir, deyilmi?",
        "Dayan, dayan! Əlcəyimi geyinim!",
    };

    void Update()
    {
        var g = MMGame.I;
        if (g == null || g.State != MMState.Playing) return;
        cooldown -= Time.deltaTime;
        var kb = Keyboard.current;
        if (kb == null || g.Player == null || cooldown > 0) return;
        bool winterOut = MMSeasons.I != null && MMSeasons.I.IsWinter && MMSeasons.I.SnowLevel > 0.5f && g.CurrentRoom == null;
        if (!winterOut) return;
        var inv = MMInventory.I;
        bool emptyHand = inv != null && inv.Slots[inv.Selected].Empty;
        bool fire = kb.gKey.wasPressedThisFrame || (kb.fKey.wasPressedThisFrame && emptyHand && Time.frameCount != g.UI.ClosedFrame);
        if (fire) Throw(g);
    }

    void Throw(MMGame g)
    {
        cooldown = 0.35f;
        var go = new GameObject("Snowball");
        Vector2 f = g.Player.Facing;
        go.transform.position = g.Player.transform.position + (Vector3)(f * 0.4f);
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        vis.transform.localPosition = new Vector3(0, 0.55f, 0);
        var sr = vis.AddComponent<SpriteRenderer>();
        sr.sprite = BallSprite; sr.sortingOrder = 20;
        var b = go.AddComponent<MMSnowball>();
        b.Dir = f; b.Visual = vis.transform; b.SplatSprite = SplatSprite;
        MMAudio.Play("throw");
    }

    // qartopu bir şeyə dəydimi? (dəyibsə, reaksiya da burada)
    public static bool TryHit(Vector3 p, Vector2 dir)
    {
        var g = MMGame.I;
        foreach (var w in MMWalker.All)
        {
            if (!w.Visible || ((Vector2)(w.transform.position + new Vector3(0, 0.7f, 0) - p)).sqrMagnitude > 0.36f) continue;
            w.HitBySnow();
            Scored();
            return true;
        }
        foreach (var n in MMNpc.All)
        {
            if (((Vector2)(n.transform.position + new Vector3(0, 0.7f, 0) - p)).sqrMagnitude > 0.4f) continue;
            if (g != null) g.UI.Toast(n.DisplayName + ": " + NpcLines[Random.Range(0, NpcLines.Length)]);
            Scored();
            return true;
        }
        foreach (var pg in MMPigeon.All)
        {
            if (((Vector2)(pg.transform.position + new Vector3(0, 0.3f, 0) - p)).sqrMagnitude > 0.5f) continue;
            if (!pg.OnGround) continue;
            pg.Flee(p - (Vector3)dir);
            Scored();
            return true;
        }
        var cat = Object.FindAnyObjectByType<MMCat>();
        if (cat != null && ((Vector2)(cat.transform.position + new Vector3(0, 0.3f, 0) - p)).sqrMagnitude < 0.4f)
        {
            MMAudio.Play("meow");
            Scored();
            return true;
        }
        return false;
    }

    static void Scored()
    {
        MMAchievements.Add("snowhits");
    }
}
