using UnityEngine;

// Futbol qapısı: top qapının içinə girəndə "QOL!" olur, top meydançanın ortasına qayıdır.
public class MMGoal : MonoBehaviour
{
    public Rect Mouth;               // qapının içi (qapıya nisbətən)
    public Transform Center;         // meydançanın ortası
    float cooldown;

    void Update()
    {
        var g = MMGame.I;
        if (g == null || g.WorldPaused) return;
        cooldown -= Time.deltaTime;
        if (cooldown > 0) return;
        var mouth = new Rect((Vector2)transform.position + Mouth.position, Mouth.size);
        foreach (var b in MMBall.Balls)
        {
            if (b == null || b.Carried || !mouth.Contains(b.transform.position)) continue;
            cooldown = 2.5f;
            MMAudio.Play("win");
            MMAchievements.Add("goals");
            int n = MMAchievements.Get("goals");
            g.UI.Toast(n >= 3 ? "QOOOL! Sən əsl futbolçusan!" : "QOOOL! Top torun içində!");
            MMCameraFollow.Shake(0.25f);
            if (MMFestival.I != null) MMFestival.I.Burst(b.transform.position, 30);
            if (Center != null) b.Drop(Center.position);
            return;
        }
    }
}
