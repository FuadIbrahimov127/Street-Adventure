using UnityEngine;

// Mestanın pişik balası: məhəllədə gizlənib. Tapanda [E] ilə götürürsən.
public class MMKitten : MMInteractable
{
    public string Id;
    public SpriteRenderer Body;
    float t, flipT;
    Vector3 basePos;

    void Start()
    {
        if (Body != null) basePos = Body.transform.localPosition;
        flipT = Random.Range(1f, 4f);
    }

    void Update()
    {
        var g = MMGame.I;
        if (g != null && g.Collected.Contains(Id)) { gameObject.SetActive(false); return; }
        if (Body == null) return;
        t += Time.deltaTime;
        Body.transform.localPosition = basePos + new Vector3(0, Mathf.Sin(t * 3f) > 0.7f ? 1f / 16f : 0f, 0);
        flipT -= Time.deltaTime;
        if (flipT <= 0) { flipT = Random.Range(1.5f, 4f); Body.flipX = !Body.flipX; }
    }

    public override void Interact(MMPlayer player)
    {
        var g = MMGame.I;
        g.Collected.Add(Id);
        MMAudio.Play("meow");
        int n = MMStory.KittensFound(g);
        if (!g.Collected.Contains("q_kit"))
            g.UI.Toast($"Balaca pişik! Bəlkə Mestanın balasıdır? Götürdün ({n}/3).");
        else if (n >= 3)
            g.UI.Toast("Üçüncü balanı da tapdın! İndi hamısını Sara nənəyə apar.");
        else
            g.UI.Toast($"Pişik balasını tapdın! ({n}/3)");
        g.UI.Refresh();
        g.SaveGame();
        gameObject.SetActive(false);
    }
}
