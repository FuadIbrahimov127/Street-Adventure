using UnityEngine;

// Sara nənənin pişiyi Mestan: tapılanda oyunçunun arxasınca gəzir.
public class MMCat : MMInteractable
{
    public SpriteRenderer Body;
    public Sprite[] Idle = new Sprite[2], Walk = new Sprite[2];
    public Sprite Portrait;
    public float Speed = 4.2f;

    enum Mode { Hiding, Following, Home }
    Mode mode = Mode.Hiding;
    float animT;
    Vector3 home;

    public override bool CanInteract => mode == Mode.Hiding;
    public int ModeIndex => (int)mode;
    public Vector3 SavePosition => mode == Mode.Home ? home : transform.position;

    public override void Interact(MMPlayer player)
    {
        var g = MMGame.I;
        string[] lines = g.CatQuestStarted
            ? new[] { "Miyav!", "Bu Mestandır! Narıncı, zolaqlı... Sara nənənin pişiyi.", "Mestan səni izləyir. Onu Sara nənəyə apar!" }
            : new[] { "Miyav!", "Narıncı, zolaqlı bir pişik. Boynunda \"Mestan\" yazılıb.", "Deyəsən evini itirib. Səni izləyir!" };
        MMAudio.Play("meow");
        g.UI.ShowDialogue("Mestan", lines, () =>
        {
            mode = Mode.Following;
            g.CatFound = true;
            g.UI.Refresh();
            SetHighlight(false);
            g.SaveGame();
        }, Portrait != null ? Portrait : Idle[0]);
    }

    public void SitAt(Vector3 pos)
    {
        mode = Mode.Home;
        home = pos;
    }

    // yaddaşdan bərpa
    public void Restore(int m, Vector3 pos)
    {
        mode = (Mode)Mathf.Clamp(m, 0, 2);
        if (mode == Mode.Home) { home = pos; transform.position = pos; }
        else if (mode == Mode.Following) transform.position = pos;
    }

    void Update()
    {
        var g = MMGame.I;
        bool moving = false;
        if (g != null && g.State == MMState.Playing)
        {
            Vector3 target = transform.position;
            if (mode == Mode.Following && g.Player != null)
            {
                Vector3 p = g.Player.transform.position;
                Vector3 behind = p - (Vector3)(g.Player.Facing * 0.9f);
                if ((behind - transform.position).sqrMagnitude > 0.35f) target = behind;
                if ((p - transform.position).sqrMagnitude > 64f) transform.position = behind; // çox geri qalıbsa
            }
            else if (mode == Mode.Home) target = home;

            Vector3 d = target - transform.position;
            if (d.sqrMagnitude > 0.01f)
            {
                float sp = mode == Mode.Following && d.magnitude > 2f ? Speed * 1.4f : Speed;
                sp *= g.SpeedMultiplier;
                transform.position = Vector3.MoveTowards(transform.position, target, sp * Time.deltaTime);
                if (Mathf.Abs(d.x) > 0.02f) Body.flipX = d.x < 0;
                moving = true;
            }
        }
        animT += Time.deltaTime * (moving ? 8f : 2f);
        var set = moving ? Walk : Idle;
        if (set[0] != null) Body.sprite = set[Mathf.FloorToInt(animT) % 2];
    }
}
