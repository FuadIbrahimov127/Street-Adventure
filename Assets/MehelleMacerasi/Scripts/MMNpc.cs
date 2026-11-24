using System.Collections.Generic;
using UnityEngine;

public enum MMNpcId { Sara, Elcin, Reshad, Babalar, Nigar, Kamal, Leyla, Tural, Zehra, Gulnare }

// Qonşular: tapşırıq verir, dialoq göstərir, oyunçuya tərəf dönür.
public class MMNpc : MMInteractable
{
    public MMNpcId Id;
    public string DisplayName;
    public SpriteRenderer Body;
    public Sprite[] Down = new Sprite[3], Up = new Sprite[3], Side = new Sprite[3];
    public GameObject MarkerExcl, MarkerQ;
    public Transform CatSeat;   // Sara nənə üçün: pişiyin oturacağı yer
    public Sprite Portrait;     // dialoq pəncərəsində göstərilən şəkil

    public static readonly List<MMNpc> All = new List<MMNpc>();

    float bobT;
    Vector3 basePos;

    protected override void OnEnable() { base.OnEnable(); All.Add(this); }
    protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

    void Start()
    {
        bobT = Random.value * 3f;
        if (Body != null) basePos = Body.transform.localPosition;
    }

    void Update()
    {
        if (Body != null && Id != MMNpcId.Babalar)
        {
            bobT += Time.deltaTime;
            Body.transform.localPosition = basePos + new Vector3(0, (Mathf.Sin(bobT * 2.2f) > 0.6f ? 1f / 16f : 0f), 0);
        }
        UpdateMarker();
    }

    void UpdateMarker()
    {
        var g = MMGame.I;
        if (g == null) return;
        bool excl = false, q = false;
        switch (Id)
        {
            case MMNpcId.Sara: excl = !g.CatQuestStarted && !g.CatFound; q = g.CatFound && !g.CatReturned; break;
            case MMNpcId.Elcin: excl = !g.LetterQuestStarted && g.LettersFound < MMGame.LettersTotal; q = g.LettersFound >= MMGame.LettersTotal && !g.LettersReturned; break;
            case MMNpcId.Babalar: excl = !g.TeaQuestStarted; q = g.HasTea; break;
            case MMNpcId.Reshad: q = g.TeaQuestStarted && !g.HasTea && !g.TeaDelivered; break;
        }
        MMStory.Markers(this, g, ref excl, ref q);
        bool show = g.State != MMState.Menu && g.State != MMState.Won && g.State != MMState.GameOver;
        if (MarkerExcl) MarkerExcl.SetActive(show && excl);
        if (MarkerQ) MarkerQ.SetActive(show && q && !excl);
    }

    void FaceTo(Vector2 p)
    {
        if (Body == null || Id == MMNpcId.Babalar || Down[0] == null) return;
        Vector2 d = p - (Vector2)transform.position;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) { Body.sprite = Side[0]; Body.flipX = d.x < 0; }
        else { Body.sprite = d.y > 0 ? Up[0] : Down[0]; Body.flipX = false; }
    }

    void FaceDown()
    {
        if (Body == null || Id == MMNpcId.Babalar || Down[0] == null) return;
        Body.sprite = Down[0]; Body.flipX = false;
    }

    // hədiyyə: bir dəfə verilir (açar Collected siyahısında saxlanılır)
    static void Gift(MMGame g, string key, string item, int count, string toast)
    {
        if (g.Collected.Contains(key) || MMInventory.I == null) return;
        g.Collected.Add(key);
        MMInventory.I.Add(item, count);
        MMAudio.Play("lucky");
        if (toast != null) g.UI.Toast(toast);
        g.SaveGame();
    }

    public override void Interact(MMPlayer player)
    {
        var g = MMGame.I;
        FaceTo(player.transform.position);
        player.FaceTowards(transform.position);
        string[] lines = null;
        System.Action done = FaceDown;
        // ikinci hekayənin dialoqları (və yeni qonşular) əvvəlcə yoxlanılır
        if (!MMStory.Dialogue(this, g, ref lines, ref done))
        switch (Id)
        {
            case MMNpcId.Sara:
                if (g.CatFound && !g.CatReturned)
                {
                    lines = new[] { "Ay Mestan! Hara qaçmışdın, dələduz?", "Çox sağ ol, bala! Bu 5 manat da sənə, özünə dondurma al." };
                    done += () => { g.CatQuestStarted = true; g.CatReturned = true; g.AddCoins(5); var cat = FindAnyObjectByType<MMCat>(); if (cat) cat.SitAt(CatSeat ? CatSeat.position : transform.position + new Vector3(0.9f, -0.2f, 0)); g.QuestDone("Tapşırıq tamamlandı: Mestan evdədir! +5 manat"); };
                }
                else if (!g.CatQuestStarted)
                {
                    lines = new[] { "Ay bala, salam! Yaxşı ki, gəldin.", "Pişiyim Mestan yenə evdən qaçıb...", "Yəqin parka tərəf gedib. Onu tapıb mənə gətirə bilərsən?", "Al, bu çörəkləri də götür. Yolda göyərçinlərə at, sevinsinlər." };
                    done += () => { g.CatQuestStarted = true; Gift(g, "gift_sara", "bread", 3, null); g.OnQuestAccepted("cat"); };
                }
                else if (!g.CatReturned)
                    lines = new[] { "Mestanı tapdın? Narıncı, zolaqlı pişikdir.", "Parka bax, kolların arasında gizlənməyi sevir." };
                else
                    lines = new[] { "Mestan indi evdədir, çox sağ ol!", "Babalara salam de, parkda nərd oynayırlar." };
                break;

            case MMNpcId.Elcin:
                if (g.LettersFound >= MMGame.LettersTotal && !g.LettersReturned)
                {
                    lines = new[] { "Bunlar mənim məktublarımdır! Hər üçünü tapdın? Əla!", "İndi hamısını vaxtında çatdıracağam. Al, bu 5 manat sənindir!" };
                    done += () => { g.LetterQuestStarted = true; g.LettersReturned = true; g.AddCoins(5); g.QuestDone("Tapşırıq tamamlandı: məktublar çatdı! +5 manat"); };
                }
                else if (!g.LetterQuestStarted)
                {
                    lines = new[] { "Salam! Mən Elçinəm, məhəllənin poçtalyonu.", "Bu gün tələsirdim, çantamdan 3 məktub düşüb!", "Küçələrdə görsən, mənə gətir, olarmı?" };
                    done += () => { g.LetterQuestStarted = true; g.OnQuestAccepted("letters"); };
                }
                else if (!g.LettersReturned)
                    lines = new[] { $"Hələ {g.LettersFound}/{MMGame.LettersTotal} məktub tapmısan.", "Birini yolun o tayında, çayxananın yanında itirmişəm deyəsən.", "Biri də parkda olmalıdır..." };
                else
                    lines = new[] { "Məktublar sahiblərinə çatdı. Sən əsl köməkçisən!" };
                break;

            case MMNpcId.Babalar:
                if (!g.TeaQuestStarted)
                {
                    lines = new[] { "Kamil baba: Şeş-beş! Bu oyunu mən udacağam!", "Rauf baba: Hələ görərik... Ay bala, salam!", "Kamil baba: Bir stəkan çay olsaydı, lap əla olardı.", "Rauf baba: Çayxanadan bizə armudu stəkanda çay gətirərsən?" };
                    done += () => { g.TeaQuestStarted = true; g.OnQuestAccepted("tea"); };
                }
                else if (g.HasTea)
                {
                    lines = new[] { "Kamil baba: Ooo, təzə dəmlənmiş çay!", "Rauf baba: Sağ ol, bala! Bu 4 manat da sənə, çayın pulu.", "Kamil baba: Gəl, sənə də nərd öyrədərik!" };
                    done += () => { g.HasTea = false; g.TeaDelivered = true; g.AddCoins(4); g.QuestDone("Tapşırıq tamamlandı: babalar çay içir! +4 manat"); };
                }
                else if (!g.TeaDelivered)
                    lines = new[] { "Kamil baba: Çayxana yolun o tayındadır, sağ tərəfdə.", "Rauf baba: Yolu keçəndə diqqətli ol, maşınlara bax!" };
                else
                    lines = new[] { "Kamil baba: Bu dəfə qalib mən olacağam!", "Rauf baba: Sən hələ zəri at, sonra danışarıq..." };
                break;

            case MMNpcId.Reshad:
                if (g.HasTea)
                    lines = new[] { "Çayı soyutma, babalar gözləyir!" };
                else if (g.TeaDelivered)
                    lines = new[] { "Babalar razı qaldı? Bizim çay bütün məhəllədə məşhurdur!" };
                else if (g.TeaQuestStarted)
                {
                    if (g.Coins >= MMGame.TeaPrice)
                    {
                        lines = new[] { "Babalara çay? Əlbəttə!", $"Buyur, {MMGame.TeaPrice} manat. Ehtiyatla apar, isti olur!" };
                        done += () => { g.Coins -= MMGame.TeaPrice; g.HasTea = true; g.UI.Refresh(); g.UI.Toast("Armudu stəkanda çay aldın!"); MMAudio.Play("coin"); };
                    }
                    else
                        lines = new[] { $"Babalara çay? Bir stəkan {MMGame.TeaPrice} manatdır.", $"Səndə {g.Coins} manat var. Küçələrdə itmiş pullar olur, axtar!" };
                }
                else
                    lines = new[] { "Xoş gəldin, çayxanamıza!", "Təzə dəmlənmiş çayımız var, limonla, mürəbbə ilə..." };
                break;

            case MMNpcId.Nigar:
                if (!g.Collected.Contains("gift_nigar"))
                {
                    lines = new[] { "Salam! Mən Nigaram, universitetdə oxuyuram.", "Axşamlar parkda kitab oxuyanda bu fənəri işlədirəm.", "Al, səndə olsun! Gecə qaranlıqda [F] ilə yandır." };
                    done += () => Gift(g, "gift_nigar", "flashlight", 1, "Nigar sənə fənər verdi! Çantana bax: [I]");
                }
                else
                    lines = new[] { "Fəvvarənin yanında oturub kitab oxumağı çox sevirəm.", "Bir məsləhət: yolu yalnız zolaqlı keçiddən keç, orada maşınlar dayanır!" };
                break;

            default:
                if (!g.Collected.Contains("gift_kamal"))
                {
                    lines = new[] { "Salam, qonşu! Mən Kamal əmiyəm.", "Uşaq vaxtı bu topla bütün günü futbol oynayardıq.", "Al, indi sənindir! [F] ilə at, parkdakı it qaçıb gətirəcək." };
                    done += () => Gift(g, "gift_kamal", "ball", 1, "Kamal əmi sənə top verdi! Çantana bax: [I]");
                }
                else
                    lines = new[] { "Salam, qonşu!", "Deyirlər küçələrdə itmiş pullar var...", "Gözünü dörd aç, bəlkə tapdın! Hətta yolun ortasında da ola bilər, amma ehtiyatlı ol." };
                break;
        }
        MMAudio.Play("talk");
        g.UI.ShowDialogue(DisplayName, lines, done, Portrait != null ? Portrait : Down[0]);
    }
}
