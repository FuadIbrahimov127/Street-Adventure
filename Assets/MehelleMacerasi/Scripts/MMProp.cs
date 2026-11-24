using UnityEngine;

// Evdəki əşyalarla danışmaq: samovar (çay içirsən), sandıq (bir dəfəlik hədiyyə), televizor, adi yazılar.
public enum MMPropKind { Talk, Samovar, Chest, TV }

public class MMProp : MMInteractable
{
    public MMPropKind Kind;
    public string Id;
    public string Title;
    [TextArea] public string[] Lines;
    public Sprite Portrait;

    static readonly string[][] TvShows =
    {
        new[] { "Televizorda futbol gedir...", "Qoool! Hamı ayağa qalxıb sevinir!" },
        new[] { "Hava proqnozu: sabah Bakıda xəzri əsəcək.", "Papağını bərk tut!" },
        new[] { "Cizgi filmi gedir: Cırtdan yenə divi aldadır!", "Sara nənə deyir: bu nağılı uşaq vaxtı mən də sevirdim." },
        new[] { "Xəbərlər: məhəllədə bir uşaq hamıya kömək edir...", "Dayan, bu ki sənsən!" },
    };

    public override void Interact(MMPlayer player)
    {
        var g = MMGame.I;
        player.FaceTowards(transform.position);
        string[] lines = Lines;
        System.Action done = null;
        switch (Kind)
        {
            case MMPropKind.Samovar:
                if (g.Hearts < g.MaxHearts)
                {
                    lines = new[] { "Samovar qaynayır, ətrafa çay ətri yayılıb.", "Armudu stəkanda isti çay içdin: +1 ürək!" };
                    done = () => { g.Hearts = Mathf.Min(g.MaxHearts, g.Hearts + 1); g.UI.Refresh(); MMAudio.Play("lucky"); };
                }
                else
                    lines = new[] { "Samovar qaynayır...", "Ürəklərin doludur. Çayı sonra içərsən." };
                break;
            case MMPropKind.Chest:
                if (!g.Collected.Contains(Id))
                {
                    lines = new[] { "Köhnə taxta sandıqdır. Qapağını yavaşca açdın...", "İçində köhnə qəpiklər var! Sara nənə deyir: götür, sənindir. +5 manat" };
                    done = () => { g.Collected.Add(Id); g.AddCoins(5); MMAudio.Play("lucky"); g.SaveGame(); };
                }
                else
                    lines = new[] { "Sandıqda nənənin köhnə şəkilləri və yun corablar var." };
                break;
            case MMPropKind.TV:
                lines = TvShows[Random.Range(0, TvShows.Length)];
                break;
        }
        MMAudio.Play("talk");
        g.UI.ShowDialogue(Title, lines, done, Portrait);
    }
}
