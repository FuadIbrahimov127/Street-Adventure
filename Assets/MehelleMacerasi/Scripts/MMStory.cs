using System.Collections.Generic;
using UnityEngine;

// İkinci hekayə: "Məhəllə şənliyi".
// Birinci hissə bitəndən sonra başlayır. Vəziyyət Collected siyahısında açarlarla saxlanılır:
//   ch2 - hekayə başladı; q_inv / q_veg / q_kit - tapşırıqlar başladı; ..._done - bitdi;
//   inv_<kim> - dəvətnamə verildi; kit_<rəng> - pişik balası tapıldı; festival - şənlik oldu.
public static class MMStory
{
    public const int InvitesTotal = 4, TomatoesNeeded = 3, KittensTotal = 3, BooksTotal = 3;
    static readonly string[] InviteNames = { "sara", "aysel", "nigar", "kamal" };
    static readonly string[] KittenIds = { "kit_white", "kit_black", "kit_ginger" };

    const string Done = "#8fe07a";
    const string Todo = "#fff4dc";

    public static bool Has(MMGame g, string key) => g != null && g.Collected.Contains(key);
    public static bool Ch2(MMGame g) => Has(g, "ch2");

    public static int InvitesDelivered(MMGame g)
    {
        int n = 0;
        foreach (var k in InviteNames) if (Has(g, "inv_" + k)) n++;
        return n;
    }

    public static int KittensFound(MMGame g)
    {
        int n = 0;
        foreach (var k in KittenIds) if (Has(g, k)) n++;
        return n;
    }

    // itmiş kitablar (pickup id-ləri "book_..." və ya "z2_book_...")
    public static int BooksFound(MMGame g)
    {
        int n = 0;
        foreach (var k in g.Collected) if (k.StartsWith("book_") || k.StartsWith("z2_book_")) n++;
        return n;
    }

    static int Tomatoes => MMInventory.I != null ? MMInventory.I.Count("tomato") : 0;

    // satış qutusu Rəşad üçün pomidor saxlasın
    public static int ReservedTomatoes(MMGame g) => Ch2(g) && Has(g, "q_veg") && !Has(g, "q_veg_done") ? TomatoesNeeded : 0;

    static bool CanDeliver(MMGame g, string who) =>
        Ch2(g) && Has(g, "q_inv") && !Has(g, "inv_" + who) && MMInventory.I != null && MMInventory.I.Count("invite") > 0;

    // qonşuya dəvətnamə ver (dialoq başlayanda çağırılır)
    public static bool TryDeliverInvite(MMGame g, string who)
    {
        if (!CanDeliver(g, who)) return false;
        MMInventory.I.Remove("invite");
        g.Collected.Add("inv_" + who);
        MMAudio.Play("letter");
        g.UI.Refresh();
        return true;
    }

    // tapşırıq bitdi: hamısı bitibsə, şənlik başlayır
    static void QuestDone(MMGame g, string msg)
    {
        g.QuestDone(msg);
        if (Has(g, "q_inv_done") && Has(g, "q_veg_done") && Has(g, "q_kit_done") && !Has(g, "festival") && MMFestival.I != null)
            MMFestival.I.Begin();
    }

    // ---------------- dialoqlar ----------------
    // true qaytarırsa, NPC-nin adi dialoqu əvəzinə bu göstərilir
    public static bool Dialogue(MMNpc npc, MMGame g, ref string[] lines, ref System.Action done)
    {
        var inv = MMInventory.I;
        bool ch2 = Ch2(g), fest = Has(g, "festival");
        switch (npc.Id)
        {
            case MMNpcId.Leyla:
                if (!ch2)
                    lines = new[] { "Salam! Mən Leylayam, poçtun müdiriyəm.",
                                    g.LetterQuestStarted && !g.LettersReturned ? "Elçin yenə məktublarını itirib? O, həmişə tələsir!" : "Məktub göndərmək, bağlama almaq istəsən, bura gəl!" };
                else if (!Has(g, "q_inv"))
                {
                    lines = new[] { "Ay bala, yaxşı ki, gəldin! Böyük xəbər var:",
                                    "Fəvvarənin yanında MƏHƏLLƏ ŞƏNLİYİ olacaq!",
                                    "Bu 4 dəvətnaməni payla: Sara nənəyə, Aysel xalaya, Nigara və Kamal əmiyə.",
                                    "Bir də... Rəşad əmi şənlik üçün plov bişirir, ona kömək lazımdır.",
                                    "Sara nənə də nəyəsə narahatdır, ona da baş çək." };
                    done += () =>
                    {
                        if (inv == null || !inv.Add("invite", InvitesTotal)) { g.UI.Toast("Çantan doludur! Yer aç və Leyla xala ilə yenidən danış."); return; }
                        g.Collected.Add("q_inv");
                        g.OnQuestAccepted("invites");
                    };
                }
                else if (!Has(g, "q_inv_done"))
                {
                    int n = InvitesDelivered(g);
                    if (n >= InvitesTotal)
                    {
                        lines = new[] { "Hamısı gələcək? Əla! Çox sağ ol, bala!", "Bu 6 manat sənin zəhmət haqqındır." };
                        done += () => { g.Collected.Add("q_inv_done"); g.AddCoins(6); QuestDone(g, "Tapşırıq tamamlandı: dəvətnamələr çatdı! +6 manat"); };
                    }
                    else
                        lines = new[] { $"Dəvətnamələri payladın? ({n}/{InvitesTotal})", "Sara nənə, Aysel xala, Nigar və Kamal əmi gözləyir." };
                }
                else
                    lines = fest ? new[] { "Şənlik əla keçdi, elə deyilmi?", "Gələn il daha böyüyünü edəcəyik!" }
                                 : new[] { "Hazırlıq gedir! Bayraqcıqları özüm asacağam.", "Rəşad əmiyə və Sara nənəyə də kömək etdin?" };
                return true;

            case MMNpcId.Tural:
                if (!Has(g, "gift_tural"))
                {
                    bool needCan = inv != null && inv.Count("can") == 0;
                    var l = new List<string>
                    {
                        "Salam! Mən Turalam. Bu tarlaya mən baxıram.",
                        "Əvvəlcə bel ilə torpağı belləyirsən, sonra sulayırsan, sonra toxum əkirsən.",
                        "Gecə yatanda bitkilər böyüyür. Yağış yağsa, özü sulanır!",
                        "Al, bu bel və toxumlar sənə hədiyyə! Beli seç və tarlada [F] bas.",
                    };
                    if (needCan) l.Add("Su qabın yoxdur? Al, köhnə su qabımı da sənə verirəm!");
                    l.Add("Su qabını bu kranda doldur. Yetişəndə [E] ilə yığ, satış qutusunda sat!");
                    lines = l.ToArray();
                    done += () =>
                    {
                        if (inv == null) return;
                        g.Collected.Add("gift_tural");
                        if (inv.Count("spade") == 0) inv.Add("spade");
                        inv.Add("seed_tomato", 3);
                        inv.Add("seed_carrot", 2);
                        if (needCan && inv.Add("can")) inv.Water = MMInventory.MaxWater;
                        MMAudio.Play("lucky");
                        g.UI.Toast("Tural sənə bel və toxum verdi! Çantana bax: [I]");
                        g.UI.Refresh();
                        g.SaveGame();
                    };
                }
                else if (ch2 && Has(g, "q_veg") && !Has(g, "q_veg_done"))
                    lines = new[] { "Rəşad əmiyə pomidor lazımdır? Pomidor 3 günə yetişir.", "Toxum lazımdırsa, piştaxtamdan al. Hər gün sulamağı unutma!" };
                else
                {
                    string[][] tips =
                    {
                        new[] { "Pomidor 3 günə, yerkökü 2 günə, qarpız 4 günə yetişir.", "Qarpız ən bahalısıdır: 12 manat!" },
                        new[] { "Yorulmusan? Böyük məhəllədə, yaşayış binasındakı evinə gir və yat.", "Səhər oyananda bitkilər böyümüş olacaq!" },
                        new[] { "Gülnarə xala tərəvəzi satış qutusundan baha alır.", "Təndirin yanında olur, çörəkxananın qarşısında." },
                        new[] { "Qışda torpaq donur, bitkilər böyümür.", "Amma yerkökünü saxla: qar adamına burun lazım olacaq!" },
                    };
                    lines = tips[Random.Range(0, tips.Length)];
                }
                return true;

            case MMNpcId.Reshad:
                if (!ch2) return false;
                if (fest) lines = new[] { "Plovumu bəyəndin? Hamı iki boşqab yedi!" };
                else if (!Has(g, "q_veg"))
                {
                    lines = new[] { "Xoş gəldin! Eşitmisən? Şənlik olacaq!",
                                    "Böyük qazanda plov bişirəcəyəm, yanında da salat.",
                                    "Amma salat üçün təzə pomidor lazımdır. Bağçadan mənə 3 pomidor gətirərsən?",
                                    "Tarla Məktəb məhəlləsindədir: çayxananın yanındakı səki ilə sağa get. Tural sənə kömək edər." };
                    done += () => { g.Collected.Add("q_veg"); g.OnQuestAccepted("veg"); };
                }
                else if (!Has(g, "q_veg_done"))
                {
                    int n = Tomatoes;
                    if (n >= TomatoesNeeded)
                    {
                        lines = new[] { "Pomidorlar! Nə gözəl, qıpqırmızıdır!", "Salat əla alınacaq. Bu 8 manat sənindir!" };
                        done += () => { inv.Remove("tomato", TomatoesNeeded); g.Collected.Add("q_veg_done"); g.AddCoins(8); QuestDone(g, "Tapşırıq tamamlandı: salat üçün pomidor! +8 manat"); };
                    }
                    else
                        lines = new[] { $"Hələ {n}/{TomatoesNeeded} pomidor. Tarlada bellə, pomidor ək, hər gün sula.", "Evində yatanda bitkilər böyüyür, unutma!" };
                }
                else lines = new[] { "Plov dəmlənir... Şənlikdə görüşərik!" };
                return true;

            case MMNpcId.Sara:
            {
                if (!ch2) return false;
                var l = new List<string>();
                if (TryDeliverInvite(g, "sara")) { l.Add("Dəvətnamə? Məhəllə şənliyi? Əlbəttə gələcəm!"); l.Add("Şəkərbura bişirib gətirərəm."); }
                if (Has(g, "q_kit_done"))
                {
                    if (l.Count == 0) l.Add(fest ? "Nə gözəl şənlikdir! Balalar da evdə şirin yuxudadır." : "Balalar Mestanın yanında yatır. Evə gir, bax!");
                }
                else if (!Has(g, "q_kit"))
                {
                    l.Add("Ay bala, bir xəbər: Mestan ana olub, 3 balası var!");
                    l.Add("Amma bu dəcəllər səbətdən çıxıb, məhəlləyə səpələniblər...");
                    l.Add("Biri ağ, biri qara, biri də sarı. Tapıb gətirə bilərsən?");
                    done += () => { g.Collected.Add("q_kit"); g.OnQuestAccepted("kittens"); };
                }
                else
                {
                    int n = KittensFound(g);
                    if (n >= KittensTotal)
                    {
                        l.Add("Balalarım! Üçü də sağ-salamat!");
                        l.Add("Çox sağ ol, bala. Bu 6 manat sənə, özünə şirniyyat al.");
                        done += () => { g.Collected.Add("q_kit_done"); g.AddCoins(6); QuestDone(g, "Tapşırıq tamamlandı: pişik balaları evdədir! +6 manat"); };
                    }
                    else
                    {
                        l.Add($"Balaları tapdın? ({n}/{KittensTotal})");
                        l.Add("Ağ bala qutularla oynamağı sevir, bəlkə poçtdadır?");
                        l.Add("Qara bala skamyaların yanında, sarı bala isə meyvə piştaxtasının yanında gəzir.");
                    }
                }
                lines = l.ToArray();
                return true;
            }

            case MMNpcId.Nigar:
                if (TryDeliverInvite(g, "nigar")) { lines = new[] { "Şənlik? Nə gözəl! Gitaramı da gətirəcəm!", "Hamıya mahnı oxuyarıq." }; return true; }
                if (fest) { lines = new[] { "Bu şənliyi heç vaxt unutmayacam!", "Gitara çalmaq çox xoş idi." }; return true; }
                return false;

            case MMNpcId.Kamal:
                if (TryDeliverInvite(g, "kamal")) { lines = new[] { "Şənlik? Əla fikirdir!", "Uşaqlar üçün futbol turniri də təşkil edərik!" }; return true; }
                if (fest) { lines = new[] { "Futbol turnirində qızıl medalı sən qazandın!" }; return true; }
                return false;

            case MMNpcId.Zehra:
            {
                int n = BooksFound(g);
                if (!Has(g, "q_book"))
                {
                    lines = new[] { "Salam, bala! Mən Zəhra xanımam, kitabxanaçıyam.",
                                    "Uşaqlar kitabları götürüb qaytarmayıblar... 3 kitab itib!",
                                    "Biri məktəbin həyətində, biri tarlanın yanında ola bilər.",
                                    "Üçüncüsünü kimsə böyük məhəllənin parkında oxuyurdu. Tapıb gətirərsən?" };
                    if (n > 0) lines = new[] { lines[0], lines[1], $"Ooo, səndə artıq {n} kitab var! Qalanlarını da tapa bilərsən?" };
                    done += () => { g.Collected.Add("q_book"); g.OnQuestAccepted("books"); };
                }
                else if (!Has(g, "q_book_done"))
                {
                    if (n >= BooksTotal)
                    {
                        lines = new[] { "Kitablarım! Üçü də burda, səhifələri də cırılmayıb!", "Çox sağ ol. Bu 5 manat sənin üçün. Bir də yadda saxla: kitab ən yaxşı dostdur!" };
                        done += () => { g.Collected.Add("q_book_done"); g.AddCoins(5); MMAchievements.Unlock("books"); g.QuestDone("Tapşırıq tamamlandı: kitablar kitabxanaya qayıtdı! +5 manat"); };
                    }
                    else
                        lines = new[] { $"Hələ {n}/{BooksTotal} kitab tapmısan.", "Məktəbin həyətinə, tarlanın ətrafına və böyük məhəllənin parkına bax." };
                }
                else
                    lines = new[] { "Rəflərə bax, sevdiyin kitabı seç!", "\"Kitabi-Dədə Qorqud\" da var, nağıllar da, kosmos haqqında kitablar da." };
                return true;
            }

            case MMNpcId.Gulnare:
            {
                var gdn = MMGarden.I;
                int total = 0, count = 0;
                if (inv != null && gdn != null)
                    foreach (var d in gdn.Crops.Values)
                    {
                        int k = inv.Count(d.Id) - (d.Id == "tomato" ? Mathf.Min(inv.Count("tomato"), ReservedTomatoes(g)) : 0);
                        if (k > 0) { count += k; total += k * (d.Price + 1); }
                    }
                if (count > 0)
                {
                    int pay = total;
                    lines = new[] { "Təzə tərəvəz! Qutab və salat üçün lap yaxşıdır.", $"Hamısını alıram: {pay} manat. Satış qutusundan bir az baha verirəm!" };
                    done += () =>
                    {
                        foreach (var d in gdn.Crops.Values)
                        {
                            int k = inv.Count(d.Id) - (d.Id == "tomato" ? Mathf.Min(inv.Count("tomato"), ReservedTomatoes(g)) : 0);
                            if (k > 0) inv.Remove(d.Id, k);
                        }
                        g.AddCoins(pay);
                        MMAchievements.Add("sold", count);
                        MMAchievements.Unlock("baker");
                        MMAudio.Play("coin");
                        g.UI.Toast($"Gülnarə xalaya tərəvəz satdın: +{pay} manat!");
                        g.SaveGame();
                    };
                }
                else
                {
                    var dn = MMDayNight.I;
                    string key = "bread_day_" + (dn != null ? dn.Day : 1);
                    if (!Has(g, key))
                    {
                        lines = new[] { "Salam, bala! Mən Gülnarə xalayam. Təndir indicə qızıb!", "Al, isti təndir çörəyi. Göyərçinlərə də bir az verərsən." };
                        done += () => { if (inv != null && inv.Add("bread", 2)) { g.Collected.Add(key); MMAudio.Play("lucky"); g.UI.Toast("Gülnarə xala sənə 2 isti çörək verdi!"); g.UI.Refresh(); } };
                    }
                    else
                        lines = new[] { "Təndirdə çörək bişirmək babamdan qalma sənətdir.", "Tarlada tərəvəz yetişdirsən, mənə gətir. Yaxşı qiymətə alaram!" };
                }
                return true;
            }

            case MMNpcId.Elcin:
                if (ch2 && !Has(g, "q_inv")) { lines = new[] { "Salam! Leyla xala səni axtarırdı.", "Poçtun içinə gir, böyük xəbəri var!" }; return true; }
                return false;

            case MMNpcId.Babalar:
                if (fest) { lines = new[] { "Kamil baba: Şənlikdə nərd turnirini mən uddum!", "Rauf baba: Hələ görərik, gələn il..." }; return true; }
                if (ch2 && g.TeaDelivered) { lines = new[] { "Kamil baba: Şənlik olacaq? Nərd turniri də keçirək!", "Rauf baba: Bu dəfə uduzmayacağam!" }; return true; }
                return false;
        }
        return false;
    }

    // ---------------- işarələr (! və ?) ----------------
    public static void Markers(MMNpc npc, MMGame g, ref bool excl, ref bool q)
    {
        bool ch2 = Ch2(g);
        switch (npc.Id)
        {
            case MMNpcId.Tural:
                excl = !Has(g, "gift_tural"); q = false;
                break;
            case MMNpcId.Zehra:
                excl = !Has(g, "q_book");
                q = Has(g, "q_book") && !Has(g, "q_book_done") && BooksFound(g) >= BooksTotal;
                break;
            case MMNpcId.Gulnare:
                excl = false;
                q = false;
                break;
            case MMNpcId.Leyla:
                excl = ch2 && !Has(g, "q_inv");
                q = ch2 && Has(g, "q_inv") && !Has(g, "q_inv_done") && InvitesDelivered(g) >= InvitesTotal;
                break;
            case MMNpcId.Reshad:
                if (!ch2) break;
                excl = !Has(g, "q_veg");
                q = Has(g, "q_veg") && !Has(g, "q_veg_done") && Tomatoes >= TomatoesNeeded;
                break;
            case MMNpcId.Sara:
                if (!ch2) break;
                excl = !Has(g, "q_kit");
                q = (Has(g, "q_kit") && !Has(g, "q_kit_done") && KittensFound(g) >= KittensTotal) || CanDeliver(g, "sara");
                break;
            case MMNpcId.Nigar:
                if (ch2) { excl = false; q = CanDeliver(g, "nigar"); }
                break;
            case MMNpcId.Kamal:
                if (ch2) { excl = false; q = CanDeliver(g, "kamal"); }
                break;
            case MMNpcId.Elcin:
                if (ch2) { excl = !Has(g, "q_inv"); q = false; }
                break;
        }
    }

    // ---------------- jurnal və HUD ----------------
    public static List<MMQuestInfo> Quests(MMGame g)
    {
        var list = new List<MMQuestInfo>();
        if (!Ch2(g)) return list;
        int inv = InvitesDelivered(g), kit = KittensFound(g), tom = Mathf.Min(Tomatoes, TomatoesNeeded);

        var q1 = new MMQuestInfo
        {
            Id = "invites", Title = "Şənlik dəvətnamələri", Giver = "Poçt müdiri Leyla",
            Description = "Fəvvarənin yanında məhəllə şənliyi olacaq! Leyla xala 4 dəvətnamə verdi: Sara nənəyə, Aysel xalaya (marketdə), Nigara və Kamal əmiyə payla.",
            Started = Has(g, "q_inv"), Done = Has(g, "q_inv_done")
        };
        q1.Steps.Add(new MMQuestStep("Leyla xala ilə danış (poçtun içində)", Has(g, "q_inv")));
        q1.Steps.Add(new MMQuestStep($"Dəvətnamələri payla ({inv}/{InvitesTotal})", inv >= InvitesTotal));
        q1.Steps.Add(new MMQuestStep("Leyla xalaya qayıt", Has(g, "q_inv_done")));
        list.Add(q1);

        var q2 = new MMQuestInfo
        {
            Id = "veg", Title = "Şənlik süfrəsi", Giver = "Çayxanaçı Rəşad",
            Description = "Rəşad əmi şənlik üçün plov və salat bişirir. Salata 3 təzə pomidor lazımdır. Tarla Məktəb məhəlləsindədir (çayxananın yanındakı səki ilə sağa): bel ilə bellə, sula, toxum ək, evində yat ki, bitkilər böyüsün.",
            Started = Has(g, "q_veg"), Done = Has(g, "q_veg_done")
        };
        q2.Steps.Add(new MMQuestStep("Rəşadla danış (çayxananın içində)", Has(g, "q_veg")));
        q2.Steps.Add(new MMQuestStep($"Tarlada 3 pomidor yetişdir ({(Has(g, "q_veg_done") ? TomatoesNeeded : tom)}/{TomatoesNeeded})", Has(g, "q_veg_done") || tom >= TomatoesNeeded));
        q2.Steps.Add(new MMQuestStep("Pomidorları Rəşada apar", Has(g, "q_veg_done")));
        list.Add(q2);

        var q3 = new MMQuestInfo
        {
            Id = "kittens", Title = "Mestanın balaları", Giver = "Sara nənə",
            Description = "Mestanın 3 balası (ağ, qara və sarı) məhəlləyə səpələnib. Ağ bala qutuları sevir, qara bala skamyaları, sarı bala meyvə piştaxtasını.",
            Started = Has(g, "q_kit"), Done = Has(g, "q_kit_done")
        };
        q3.Steps.Add(new MMQuestStep("Sara nənə ilə danış", Has(g, "q_kit")));
        q3.Steps.Add(new MMQuestStep($"Pişik balalarını tap ({kit}/{KittensTotal})", kit >= KittensTotal));
        q3.Steps.Add(new MMQuestStep("Balaları Sara nənəyə apar", Has(g, "q_kit_done")));
        list.Add(q3);
        return list;
    }

    static string Line(string col, string t) => $"<color={col}>• {t}</color>\n";

    public static string Panel(MMGame g)
    {
        if (Has(g, "festival"))
            return "Şənlik oldu!\n" + Line(Todo, "Bağçanı becər, pul qazan") + Line(Todo, "Fəsilləri gör: qışda qar adamı!") + Line(Todo, "Nailiyyətləri topla [N]");
        string s = "Məhəllə şənliyi\n";
        int inv = InvitesDelivered(g), kit = KittensFound(g), tom = Mathf.Min(Tomatoes, TomatoesNeeded);
        if (!Has(g, "q_inv")) s += Line(Todo, "Leyla xala ilə danış (poçtda)");
        else if (Has(g, "q_inv_done")) s += Line(Done, "Dəvətnamələr çatdı");
        else if (inv < InvitesTotal) s += Line(Todo, $"Dəvətnamələri payla ({inv}/{InvitesTotal})");
        else s += Line(Todo, "Leyla xalaya qayıt");

        if (!Has(g, "q_veg")) s += Line(Todo, "Rəşadla danış (çayxanada)");
        else if (Has(g, "q_veg_done")) s += Line(Done, "Pomidorlar çatdı");
        else if (tom < TomatoesNeeded) s += Line(Todo, $"Pomidor yetişdir ({tom}/{TomatoesNeeded})");
        else s += Line(Todo, "Pomidorları Rəşada apar");

        if (!Has(g, "q_kit")) s += Line(Todo, "Sara nənə ilə danış");
        else if (Has(g, "q_kit_done")) s += Line(Done, "Balalar evdədir");
        else if (kit < KittensTotal) s += Line(Todo, $"Pişik balalarını tap ({kit}/{KittensTotal})");
        else s += Line(Todo, "Balaları Sara nənəyə apar");
        return s;
    }
}
