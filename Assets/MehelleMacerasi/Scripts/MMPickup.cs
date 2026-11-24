using UnityEngine;

public enum MMPickupKind { Coin, Letter, Book }

// Yerdən götürülən əşyalar: pul və məktublar. Id yaddaş üçündür (götürülənlər bir daha çıxmır).
public class MMPickup : MonoBehaviour
{
    public MMPickupKind Kind;
    public string Id;
    public int Value = 1;
    public Transform Visual;
    float t;
    Vector3 basePos;

    void Start()
    {
        // başqa zonada olanda yaddaşdan yüklənibsə, artıq götürülüb
        var gm = MMGame.I;
        if (gm != null && !string.IsNullOrEmpty(Id) && gm.Collected.Contains(Id)) { Destroy(gameObject); return; }
        t = Random.value * 6f;
        if (Visual != null) basePos = Visual.localPosition;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (Visual != null) Visual.localPosition = basePos + new Vector3(0, Mathf.Round(Mathf.Sin(t * 3f) * 1.5f) / 16f, 0);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<MMPlayer>();
        var g = MMGame.I;
        if (p == null || g == null || g.State != MMState.Playing) return;
        if (!string.IsNullOrEmpty(Id)) g.Collected.Add(Id);
        if (Kind == MMPickupKind.Coin)
        {
            g.AddCoins(Value);
            MMAudio.Play("coin");
        }
        else if (Kind == MMPickupKind.Book)
        {
            MMAudio.Play("letter");
            int n = MMStory.BooksFound(g);
            if (!g.Collected.Contains("q_book"))
                g.UI.Toast($"Kitab tapdın! Üstündə möhür var: \"Kitabxana\". ({n}/{MMStory.BooksTotal})");
            else
                g.UI.Toast(n >= MMStory.BooksTotal ? "Üçüncü kitabı da tapdın! Zəhra xanıma apar." : $"Kitab tapdın! ({n}/{MMStory.BooksTotal})");
            g.UI.Refresh();
            g.SaveGame();
        }
        else
        {
            g.LettersFound++;
            g.UI.Refresh();
            MMAudio.Play("letter");
            if (g.LettersFound >= MMGame.LettersTotal) g.UI.Toast("Bütün məktubları tapdın! Elçinə apar.");
            else g.UI.Toast($"Məktub tapdın! ({g.LettersFound}/{MMGame.LettersTotal})");
            g.SaveGame();
        }
        Destroy(gameObject);
    }
}
