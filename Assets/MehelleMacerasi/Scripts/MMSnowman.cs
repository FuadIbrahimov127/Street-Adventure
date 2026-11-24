using System.Collections.Generic;
using UnityEngine;

// Qar adamı yeri: qışda qar yığını görünür. [E] ilə üç qartopu yuvarla, sonra yerkökü burun tax.
public class MMSnowman : MMInteractable
{
    public static readonly List<MMSnowman> All = new List<MMSnowman>();
    public int Index;
    public int Stage = -1;                 // -1 qar yığını, 0..2 qartopları, 3 hazır
    public SpriteRenderer Body;
    public Sprite Pile;
    public Sprite[] Stages = new Sprite[4];

    protected override void OnEnable() { base.OnEnable(); All.Add(this); }
    protected override void OnDisable() { base.OnDisable(); All.Remove(this); }

    static bool Snowy => MMSeasons.I != null && MMSeasons.I.IsWinter && MMSeasons.I.SnowLevel > 0.5f;
    public override bool CanInteract => Snowy && Stage < 3;

    void Update()
    {
        // yaz gələndə qar adamı əriyir
        if (MMSeasons.I != null && !MMSeasons.I.IsWinter && Stage >= 0) Stage = -1;
        if (Body == null) return;
        Body.enabled = Snowy;
        Body.sprite = Stage < 0 ? Pile : Stages[Mathf.Clamp(Stage, 0, 3)];
    }

    public override void Interact(MMPlayer player)
    {
        var g = MMGame.I;
        if (Stage < 2)
        {
            Stage++;
            MMAudio.Play("snow");
            g.UI.Toast(Stage == 0 ? "Böyük qartopu yuvarladın! Bu, qar adamının ayaqlarıdır." :
                       Stage == 1 ? "İkinci qartopu: qar adamının gövdəsi!" :
                       "Başı da hazırdır! Amma burnu yoxdur: yerkökü lazımdır.");
            return;
        }
        var inv = MMInventory.I;
        if (inv != null && inv.Count("carrot") > 0)
        {
            inv.Remove("carrot");
            Stage = 3;
            MMAudio.Play("win");
            MMAchievements.Unlock("snowman");
            g.UI.Toast("Yerkökü burun, qırmızı şərf, qara papaq... Qar adamı hazırdır!");
            g.UI.Refresh();
            g.SaveGame();
        }
        else
            g.UI.Toast("Qar adamına burun lazımdır: yerkökü! Bağçada yetişdir və ya Turaldan al.");
    }
}
