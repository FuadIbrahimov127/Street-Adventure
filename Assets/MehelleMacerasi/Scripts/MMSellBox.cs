using UnityEngine;

// Satış qutusu: çantadakı bütün tərəvəzləri satır (Stardew kimi).
public class MMSellBox : MMInteractable
{
    public override void Interact(MMPlayer player)
    {
        var g = MMGame.I;
        var inv = MMInventory.I;
        var gdn = MMGarden.I;
        if (inv == null || gdn == null) return;
        int total = 0, count = 0, kept = 0;
        string summary = "";
        foreach (var d in gdn.Crops.Values)
        {
            int n = inv.Count(d.Id);
            // Rəşadın tapşırığı üçün pomidorları saxla
            if (d.Id == "tomato") { kept = Mathf.Min(n, MMStory.ReservedTomatoes(g)); n -= kept; }
            if (n <= 0) continue;
            inv.Remove(d.Id, n);
            total += n * d.Price; count += n;
            summary += (summary.Length > 0 ? ", " : "") + n + " " + d.Name.ToLower();
        }
        if (count == 0)
        {
            g.UI.Toast(kept > 0 ? "Pomidorları Rəşad üçün saxladım. Satmağa başqa məhsul yoxdur." : "Satmaq üçün bağçadan məhsul yığ: pomidor, yerkökü, qarpız.");
            return;
        }
        g.AddCoins(total);
        MMAchievements.Add("sold", count);
        MMAudio.Play("coin");
        g.UI.Toast($"Satdın: {summary}. +{total} manat!" + (kept > 0 ? $" ({kept} pomidor Rəşad üçün saxlandı)" : ""));
        g.SaveGame();
    }
}
