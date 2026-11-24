using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Gecə yanan hər şey: fənərlər, pəncərələr, maşın faraları, oyunçunun ətrafındakı zəif işıq.
public class MMNightLight : MonoBehaviour
{
    public static readonly List<MMNightLight> All = new List<MMNightLight>();

    public SpriteRenderer Glow;          // gecə görünən parıltı (alfa ilə)
    public Light2D Light;                // nöqtəvi işıq
    public float MaxIntensity = 1f;
    public SpriteRenderer Swap;          // gündüz/gecə şəkli dəyişən renderer (məs. fənər başlığı)
    public Sprite DaySprite, NightSprite;
    [Range(0, 1)] public float Threshold = 0.35f;   // gecənin hansı həddində yanır
    public bool Flicker;                 // pəncərələrdə yüngül titrəmə

    float level;
    float seed;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        seed = Random.value * 10f;
        Apply(MMDayNight.I != null ? MMDayNight.I.Night : 0f);
    }

    void Update()
    {
        if (!Flicker || level <= 0.01f) return;
        float f = 0.92f + 0.08f * Mathf.PerlinNoise(Time.time * 0.8f, seed);
        if (Glow != null) { var c = Glow.color; c.a = level * f; Glow.color = c; }
    }

    public void Apply(float night)
    {
        level = Mathf.Clamp01((night - Threshold) / 0.2f);
        if (Glow != null)
        {
            var c = Glow.color; c.a = level; Glow.color = c;
            Glow.enabled = level > 0.01f;
        }
        if (Light != null)
        {
            Light.intensity = MaxIntensity * level;
            Light.enabled = level > 0.01f;
        }
        if (Swap != null && NightSprite != null) Swap.sprite = level > 0.5f ? NightSprite : DaySprite;
    }
}
