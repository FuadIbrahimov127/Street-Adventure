using System.Collections.Generic;

// Əşyaların siyahısı: adı, təsviri, necə işlədildiyi və bir xanada neçə dənə ola biləcəyi.
public enum MMItemUse { Eat, Drink, Throw, Bread, Water, Photo, Light, Passive, Quest, Umbrella, Seed, Crop, Hoe }

public class MMItemDef
{
    public string Id, Name, Desc, Icon;
    public MMItemUse Use;
    public int MaxStack = 1;
}

public static class MMItems
{
    public static readonly Dictionary<string, MMItemDef> All = new Dictionary<string, MMItemDef>();

    static void Def(string id, string name, string icon, MMItemUse use, int stack, string desc)
    {
        All[id] = new MMItemDef { Id = id, Name = name, Icon = icon, Use = use, MaxStack = stack, Desc = desc };
    }

    static MMItems()
    {
        Def("icecream", "Dondurma", "icecream_icon", MMItemUse.Eat, 9, "Ye: 20 saniyə daha sürətli qaçırsan.");
        Def("juice", "Nar şirəsi", "juice_icon", MMItemUse.Drink, 9, "İç: +1 ürək.");
        Def("ball", "Top", "ball_icon", MMItemUse.Throw, 1, "At: top diyirlənir, it qaçıb gətirir. Üstünə yeriyəndə təpiklə vurursan, E ilə geri götürürsən.");
        Def("bread", "Çörək", "bread_icon", MMItemUse.Bread, 9, "At: göyərçinlər qaçıb gəlib dənləyir.");
        Def("can", "Su qabı", "can_icon", MMItemUse.Water, 1, "Fəvvarənin yanında işlət: dolur. Quru gül ləkinin yanında işlət: güllər açılır.");
        Def("camera", "Fotoaparat", "camera_icon", MMItemUse.Photo, 1, "Şəkil çək: şəkil kompüterdə \"Şəkillər\" qovluğuna yazılır.");
        Def("flashlight", "Fənər", "flashlight_icon", MMItemUse.Light, 1, "Yandır/söndür: gecə qabağına işıq salır.");
        Def("umbrella", "Çətir", "umbrella_icon", MMItemUse.Umbrella, 1, "Aç/bağla: yağışda islanmırsan. Yağışda çətirlə gəzmək nailiyyət verir!");
        Def("charm", "Göz muncuğu", "charm_icon", MMItemUse.Passive, 1, "Özü işləyir: maşın dəysə, səni bir dəfə qoruyur.");
        Def("shoes", "Qaçış ayaqqabısı", "shoes_icon", MMItemUse.Passive, 1, "Özü işləyir: həmişəlik daha sürətli yeriyirsən.");
        Def("letter", "Məktub", "letter_icon", MMItemUse.Quest, 3, "Poçtalyon Elçinin itirdiyi məktub. Ona apar.");
        Def("tea", "Armudu stəkanda çay", "tea_icon", MMItemUse.Quest, 1, "Parkdakı babalara apar, soyumasın!");
        Def("spade", "Bel", "spade_icon", MMItemUse.Hoe, 1, "Tarlada torpağı bellə [F]. Sonra sula və toxum ək.");
        Def("invite", "Dəvətnamə", "invite_icon", MMItemUse.Quest, 4, "Leyla xalanın şənlik dəvətnaməsi. Qonşulara payla: danışanda özü verilir.");
        Def("seed_tomato", "Pomidor toxumu", "seed_tomato", MMItemUse.Seed, 9, "Bağça ləkinin yanında [F]: ək. 3 günə yetişir, hər gün sula.");
        Def("seed_carrot", "Yerkökü toxumu", "seed_carrot", MMItemUse.Seed, 9, "Bağça ləkinin yanında [F]: ək. 2 günə yetişir.");
        Def("seed_melon", "Qarpız toxumu", "seed_melon", MMItemUse.Seed, 9, "Bağça ləkinin yanında [F]: ək. 4 günə yetişir, ən bahalı məhsuldur!");
        Def("tomato", "Pomidor", "tomato_icon", MMItemUse.Crop, 9, "Satış qutusunda sat (5 manat) və ya Rəşad əmiyə apar.");
        Def("carrot", "Yerkökü", "carrot_icon", MMItemUse.Crop, 9, "Satış qutusunda sat (3 manat). Qışda qar adamına burun olur!");
        Def("melon", "Qarpız", "melon_icon", MMItemUse.Crop, 9, "Satış qutusunda sat: 12 manat!");
    }

    public static MMItemDef Get(string id)
    {
        return id != null && All.TryGetValue(id, out var d) ? d : null;
    }
}
