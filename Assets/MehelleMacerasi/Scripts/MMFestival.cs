using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Şənlik finalı: axşam düşür, qonşular fəvvarənin ətrafına toplaşır, bayraqcıqlar, işıqlar, konfeti.
public class MMFestival : MonoBehaviour
{
    public static MMFestival I;
    public Transform PlayerSpot;
    public Transform[] Spots;              // qonşuların durduğu yerlər
    public Sprite[] Confetti;
    bool running;

    static readonly MMNpcId[] Guests = { MMNpcId.Sara, MMNpcId.Elcin, MMNpcId.Reshad, MMNpcId.Nigar, MMNpcId.Kamal, MMNpcId.Leyla, MMNpcId.Tural };

    void Awake() { I = this; }

    public void Begin()
    {
        if (running) return;
        running = true;
        StartCoroutine(Routine());
    }

    IEnumerator Routine()
    {
        var g = MMGame.I;
        yield return new WaitForSeconds(2.2f);
        while (g.State != MMState.Playing) yield return null;

        g.State = MMState.Transition;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.6f) { g.UI.SetFade(t); yield return null; }
        g.UI.SetFade(1f);

        g.Collected.Add("festival");
        g.SetZone(MMZone.Main);
        g.SetRoom(null);
        if (PlayerSpot != null) { g.Player.Teleport(PlayerSpot.position); g.Player.SetFacing(Vector2.up); }
        if (g.DayNight != null) g.DayNight.SetTime(19 * 60 + 30);
        int i = 0;
        foreach (var id in Guests)
        {
            if (Spots == null || i >= Spots.Length) break;
            foreach (var n in MMNpc.All)
                if (n.Id == id) { n.transform.position = Spots[i].position; i++; break; }
        }

        yield return new WaitForSecondsRealtime(0.4f);
        for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.8f) { g.UI.SetFade(t); yield return null; }
        g.UI.SetFade(0f);
        g.State = MMState.Playing;
        g.UI.ClosedFrame = Time.frameCount;
        g.UI.Refresh();
        MMAudio.Play("win");
        g.UI.Toast("Şənlik başladı! Bütün qonşular fəvvarənin yanına toplaşıb!");
        StartCoroutine(ConfettiRain(g.Player.transform.position));

        yield return new WaitForSeconds(5.5f);
        while (g.State != MMState.Playing) yield return null;
        MMAchievements.Unlock("festival");
        g.SaveGame();
        g.State = MMState.Won;
        var dn = g.DayNight;
        string stats = $"Pulların: {g.Coins} manat     Gün: {(dn != null ? dn.Day : 1)}     Nailiyyətlər: {MMAchievements.UnlockedCount}/{MMAchievements.Table.Length}";
        g.UI.ShowWinCustom("MƏHƏLLƏ ŞƏNLİYİ!",
            "Dəvətnamələr çatdı, plov dəmləndi, pişik balaları evdədir.\nBütün məhəllə sənə təşəkkür edir!",
            stats, "[Enter] Şənlikdə qal və gəzməyə davam et");
        MMAudio.Play("win");
        running = false;
    }

    // kiçik konfeti partlayışı (məs. futbolda qol)
    public void Burst(Vector3 center, int count)
    {
        StartCoroutine(ConfettiRain(center, count));
    }

    // ekranın yuxarısından rəngli konfeti yağır
    IEnumerator ConfettiRain(Vector3 center, int count = 90)
    {
        if (Confetti == null || Confetti.Length == 0) yield break;
        var bits = new List<(Transform tr, Vector2 v, float sway)>();
        for (int k = 0; k < count; k++)
        {
            var go = new GameObject("Confetti");
            go.transform.position = center + new Vector3(Random.Range(-9f, 9f), Random.Range(3f, 9f), 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Confetti[k % Confetti.Length]; sr.sortingOrder = 40;
            bits.Add((go.transform, new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(-2.6f, -1.6f)), Random.Range(0f, 6f)));
        }
        for (float t = 0; t < 6f; t += Time.deltaTime)
        {
            foreach (var b in bits)
            {
                if (b.tr == null) continue;
                b.tr.position += new Vector3(b.v.x + Mathf.Sin(t * 3f + b.sway) * 0.8f, b.v.y, 0) * Time.deltaTime;
                b.tr.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 5f + b.sway) * 40f);
            }
            yield return null;
        }
        foreach (var b in bits) if (b.tr != null) Destroy(b.tr.gameObject);
    }
}
