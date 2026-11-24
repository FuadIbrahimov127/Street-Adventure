using UnityEngine;

// Arzu fəvvarəsi: 1 manat at, şansını yoxla.
public class MMFountain : MMInteractable
{
    public const string Name = "Arzu fəvvarəsi";
    public Sprite Portrait;

    public override void Interact(MMPlayer player)
    {
        var g = MMGame.I;
        player.FaceTowards(transform.position);
        if (g.Coins < 1)
        {
            g.UI.ShowDialogue(Name, new[] { "Deyirlər, bu fəvvarəyə pul atıb arzu tutanların bəxti gətirir.", "Amma sənin pulun yoxdur. Küçələrdə pul axtar!" }, null, Portrait);
            return;
        }
        g.Coins -= 1;
        g.Wishes++;
        MMAchievements.Add("wishes");
        g.UI.Refresh();
        MMAudio.Play("splash");

        string result;
        System.Action reward = null;
        float r = Random.value;
        if (r < 0.04f)
        {
            result = "Suyun dibində qızıl sikkə parıldayır! Bu gün çox şanslısan: +10 manat!";
            reward = () => { g.AddCoins(10); MMAudio.Play("lucky"); };
        }
        else if (r < 0.24f)
        {
            if (g.HasCharm)
            {
                result = "Yenə mavi göz muncuğu çıxdı, amma səndə artıq var. Əvəzində 2 manat götürdün.";
                reward = () => g.AddCoins(2);
            }
            else
            {
                result = "Suyun içindən mavi göz muncuğu çıxdı! Maşın dəysə, səni bir dəfə qoruyacaq.";
                reward = () => { g.HasCharm = true; g.UI.Refresh(); g.UI.Toast("Göz muncuğu qazandın!"); MMAudio.Play("lucky"); };
            }
        }
        else if (r < 0.39f)
        {
            result = "Fəvvarənin yanındakı dondurmaçı sənə dondurma hədiyyə etdi! Çantana qoydun, [F] ilə yeyə bilərsən.";
            reward = () => { if (MMInventory.I == null || !MMInventory.I.Add("icecream")) g.SpeedBoostT = 20f; g.UI.Refresh(); MMAudio.Play("lucky"); };
        }
        else if (r < 0.49f)
        {
            if (g.Hearts < g.MaxHearts)
            {
                result = "Sərin su səni təravətləndirdi. +1 ürək!";
                reward = () => { g.Hearts++; g.UI.Refresh(); MMAudio.Play("lucky"); };
            }
            else
            {
                result = "Fəvvarənin kənarında kimsə 3 manat unudub. İndi sənindir!";
                reward = () => g.AddCoins(3);
            }
        }
        else if (r < 0.69f)
        {
            result = "Atdığın manat daşa dəyib geri sıçradı! Pulun geri qayıtdı.";
            reward = () => g.AddCoins(1);
        }
        else
            result = "Su şırıldayır... Arzun yəqin ki, sonra çin olacaq.";

        System.Action done = () => { reward?.Invoke(); g.SaveGame(); };
        g.UI.ShowDialogue(Name, new[] { "Fəvvarəyə 1 manat atdın, gözlərini yumub arzu tutdun...", result }, done, Portrait);
    }
}
