using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Satıcı: Aysel xalanın marketi (Kind = "market") və ya Turalın toxum piştaxtası (Kind = "seeds").
public class MMShop : MMInteractable
{
    public string DisplayName = "Aysel xala";
    public string Kind = "market";
    public string Title = "MARKET";
    public SpriteRenderer Body;
    public Sprite[] Down = new Sprite[3], Side = new Sprite[3];

    public class Item
    {
        public string Id, Name, Desc;
        public int Price;
        public int Count = 1;      // bir alışda neçə dənə
        public Sprite Icon;
    }

    public readonly List<Item> Items = new List<Item>();
    int selected;
    int openedFrame;
    string message;

    void Awake()
    {
        if (Kind == "seeds")
        {
            Items.Add(new Item { Id = "seed_tomato", Name = "Pomidor toxumu (3 dənə)", Price = 2, Count = 3, Desc = "3 günə yetişir. Pomidor 5 manata satılır." });
            Items.Add(new Item { Id = "seed_carrot", Name = "Yerkökü toxumu (3 dənə)", Price = 1, Count = 3, Desc = "2 günə yetişir. Yerkökü 3 manata satılır." });
            Items.Add(new Item { Id = "seed_melon", Name = "Qarpız toxumu (3 dənə)", Price = 6, Count = 3, Desc = "4 günə yetişir. Qarpız 12 manata satılır!" });
            Items.Add(new Item { Id = "carrot", Name = "Yerkökü", Price = 2, Desc = "Bağçadan təzə yerkökü. Qışda qar adamına burun olur!" });
            Items.Add(new Item { Id = "spade", Name = "Bel", Price = 3, Desc = "Tarlada torpağı bellə [F]. Bellənmiş torpağı sula, sonra ək." });
            return;
        }
        Items.Add(new Item { Id = "icecream", Name = "Dondurma", Price = 2, Desc = "Çantana qoyulur. [F] ilə yeyəndə 20 saniyə sürətli qaçırsan." });
        Items.Add(new Item { Id = "juice", Name = "Nar şirəsi", Price = 3, Desc = "Çantana qoyulur. [F] ilə içəndə +1 ürək." });
        Items.Add(new Item { Id = "bread", Name = "Çörək", Price = 1, Desc = "Göyərçinlərə at, qaçıb gəlib dənləyəcəklər." });
        Items.Add(new Item { Id = "can", Name = "Su qabı", Price = 4, Desc = "Fəvvarədə doldur, quru gül ləklərini sula: güllər açılır." });
        Items.Add(new Item { Id = "umbrella", Name = "Çətir", Price = 4, Desc = "Yağışda islanmırsan. [F] ilə aç və bağla." });
        Items.Add(new Item { Id = "charm", Name = "Göz muncuğu", Price = 5, Desc = "Maşın dəysə, səni bir dəfə qoruyur." });
        Items.Add(new Item { Id = "flashlight", Name = "Fənər", Price = 6, Desc = "Gecə qaranlıqda qabağına işıq salır." });
        Items.Add(new Item { Id = "shoes", Name = "Qaçış ayaqqabısı", Price = 12, Desc = "Həmişəlik daha sürətli yeriyirsən." });
    }

    public override void Interact(MMPlayer player)
    {
        var g = MMGame.I;
        FaceTo(player.transform.position);
        player.FaceTowards(transform.position);
        g.State = MMState.Shop;
        openedFrame = Time.frameCount;
        selected = 0;
        if (Kind == "seeds")
            message = MMSeasons.I != null && MMSeasons.I.IsWinter ? "Qışda əkmək olmur, amma yazda lazım olacaq!" : "Toxum lazımdır? Seç, götür!";
        else if (MMStory.TryDeliverInvite(g, "aysel"))
            message = "Dəvətnamə? Şənliyə mütləq gələcəm, şirniyyat da gətirəcəm!";
        else
            message = MMDayNight.I != null && MMDayNight.I.IsNight
                ? "Gecən xeyrə qalsın, bala! Hələ açığıq. Nə lazımdır?"
                : "Xoş gəldin, bala! Nə istəyirsən?";
        g.UI.ShowShop(true);
        Render();
        MMAudio.Play("talk");
    }

    void FaceTo(Vector2 p)
    {
        if (Body == null || Down[0] == null) return;
        Vector2 d = p - (Vector2)transform.position;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) { Body.sprite = Side[0]; Body.flipX = d.x < 0; }
        else { Body.sprite = Down[0]; Body.flipX = false; }
    }

    void Update()
    {
        var g = MMGame.I;
        if (g == null || g.State != MMState.Shop || Time.frameCount == openedFrame) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) { selected = (selected + Items.Count - 1) % Items.Count; MMAudio.Play("blip"); Render(); }
        else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) { selected = (selected + 1) % Items.Count; MMAudio.Play("blip"); Render(); }
        else if (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) Buy(Items[selected]);
        else if (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) Close();
    }

    void Close()
    {
        var g = MMGame.I;
        g.UI.ShowShop(false);
        g.State = MMState.Playing;
        g.UI.ClosedFrame = Time.frameCount;
        g.UI.Refresh();
        if (Body != null && Down[0] != null) { Body.sprite = Down[0]; Body.flipX = false; }
    }

    public string StatusOf(Item it)
    {
        var g = MMGame.I;
        var inv = MMInventory.I;
        if (it.Id == "charm" && g.HasCharm) return "səndə var";
        if (it.Id == "shoes" && g.HasSneakers) return "alınıb";
        if ((it.Id == "can" || it.Id == "flashlight" || it.Id == "umbrella" || it.Id == "spade") && inv != null && inv.Count(it.Id) > 0) return "səndə var";
        return null;
    }

    void Buy(Item it)
    {
        var g = MMGame.I;
        var inv = MMInventory.I;
        if (StatusOf(it) != null)
        {
            message = "Bu səndə artıq var, bala.";
            MMAudio.Play("blip");
            Render();
            return;
        }
        if (g.Coins < it.Price)
        {
            message = $"Pulun çatmır: {it.Price} manat lazımdır, səndə {g.Coins} var.";
            MMAudio.Play("hit");
            Render();
            return;
        }
        switch (it.Id)
        {
            case "charm":
                g.HasCharm = true;
                message = "Göz muncuğu səni pis gözdən qorusun!";
                break;
            case "shoes":
                g.HasSneakers = true;
                message = "Təzə ayaqqabılar! İndi küləkdən də sürətlisən.";
                break;
            default:
                if (inv == null || !inv.Add(it.Id, it.Count))
                {
                    message = "Çantan doludur! Əvvəlcə bir şey işlət.";
                    MMAudio.Play("hit");
                    Render();
                    return;
                }
                if (it.Id == "can") inv.Water = MMInventory.MaxWater;
                message = it.Id == "icecream" ? "Buyur, dondurman! Çantana qoydum." :
                          it.Id == "juice" ? "Nuş olsun! Nar şirəsi çantandadır." :
                          it.Id == "bread" ? "Təzə çörək! Göyərçinlər sevinəcək." :
                          it.Id == "can" ? "Su qabı, içi də doludur. Gülləri sula!" :
                          it.Id == "umbrella" ? "Gözəl qırmızı çətir! Yağışda [F] ilə aç." :
                          it.Id.StartsWith("seed_") ? "Toxumlar çantandadır! Toxumu seç, ləkin yanında [F] bas." :
                          it.Id == "carrot" ? "Təzə yerkökü! Qar adamı üçün saxla." :
                          it.Id == "spade" ? "Bel səndədir! Tarlada [F] ilə torpağı bellə." :
                          "Fənər səndədir. Gecə [F] ilə yandır.";
                break;
        }
        g.Coins -= it.Price;
        MMAudio.Play("lucky");
        MMAchievements.Add("shop");
        g.UI.Refresh();
        g.SaveGame();
        Render();
    }

    void Render()
    {
        MMGame.I.UI.RenderShop(this, selected, message);
    }
}
