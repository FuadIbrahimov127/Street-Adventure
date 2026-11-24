using System.Collections.Generic;
using UnityEngine;

// Səs effektləri kodla yaradılır; fon musiqisi (gündüz/gecə) fayldan gəlir və yumşaq keçidlə dəyişir.
public class MMAudio : MonoBehaviour
{
    static MMAudio inst;
    AudioSource src;
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    const int SR = 22050;

    [Header("Music")]
    public AudioClip MusicDay, MusicNight;
    [Range(0, 1)] public float MusicVolume = 0.32f;
    AudioSource dayMusic, nightMusic;
    public AudioClip RainClip, ThunderClip;
    AudioSource rainSource;
    float rainLevel;
    bool nightMusicOn;

    void Awake()
    {
        inst = this;
        src = gameObject.GetComponent<AudioSource>();
        if (src == null) src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        dayMusic = MakeMusicSource(MusicDay);
        nightMusic = MakeMusicSource(MusicNight);
        MusicVolume = 0.46f * musicLevel;
        src.volume = sfxLevel;
        if (dayMusic != null) dayMusic.volume = MusicVolume;
        if (RainClip != null)
        {
            rainSource = gameObject.AddComponent<AudioSource>();
            rainSource.clip = RainClip; rainSource.loop = true; rainSource.playOnAwake = false; rainSource.volume = 0f;
            rainSource.Play();
        }
        if (ThunderClip != null) clips["thunder"] = ThunderClip;
        clips["flap"] = Noise(0.12f, 0.08f);
        clips["munch"] = Noise(0.09f, 0.18f);
        clips["door"] = Tones(new[] { 294f, 392f }, 0.06f, 0.16f, square: false);
        clips["throw"] = Sweep(320f, 900f, 0.12f, 0.16f);
        clips["bounce"] = Sweep(520f, 260f, 0.07f, 0.16f);
        clips["water"] = Mix(Noise(0.35f, 0.1f), Sweep(1600f, 700f, 0.3f, 0.05f));
        clips["shutter"] = Mix(Noise(0.06f, 0.3f), Tones(new[] { 2200f }, 0.03f, 0.12f, square: true));
        clips["woof"] = Mix(Sweep(420f, 260f, 0.12f, 0.22f), Noise(0.08f, 0.08f));
        clips["coin"] = Tones(new[] { 988f, 1319f }, 0.07f, 0.25f, square: true);
        clips["letter"] = Tones(new[] { 660f, 880f, 1100f }, 0.06f, 0.22f, square: true);
        clips["talk"] = Tones(new[] { 520f }, 0.05f, 0.12f, square: true);
        clips["blip"] = Tones(new[] { 700f }, 0.025f, 0.08f, square: true);
        clips["quest_new"] = Tones(new[] { 523f, 659f, 784f }, 0.08f, 0.22f, square: false);
        clips["quest"] = Tones(new[] { 523f, 659f, 784f, 1047f }, 0.09f, 0.25f, square: false);
        clips["win"] = Tones(new[] { 523f, 659f, 784f, 1047f, 784f, 1047f, 1319f }, 0.12f, 0.25f, square: false);
        clips["start"] = Tones(new[] { 392f, 523f, 659f }, 0.08f, 0.2f, square: false);
        clips["lose"] = Tones(new[] { 392f, 330f, 262f, 196f }, 0.14f, 0.25f, square: false);
        clips["meow"] = Sweep(900f, 600f, 0.25f, 0.18f);
        clips["hit"] = Noise(0.22f, 0.35f);
        clips["step"] = Noise(0.03f, 0.04f);
        clips["splash"] = Mix(Noise(0.35f, 0.25f), Sweep(1200f, 300f, 0.3f, 0.12f));
        clips["lucky"] = Tones(new[] { 784f, 988f, 1175f, 1568f }, 0.07f, 0.22f, square: true);
        clips["dig"] = Mix(Noise(0.1f, 0.28f), Sweep(240f, 120f, 0.09f, 0.12f));
        clips["snow"] = Mix(Noise(0.14f, 0.18f), Sweep(500f, 200f, 0.1f, 0.06f));
        clips["achieve"] = Tones(new[] { 659f, 784f, 1047f, 1319f, 1047f, 1568f }, 0.075f, 0.2f, square: true);
        clips["charm"] = Tones(new[] { 1319f, 988f, 1319f }, 0.08f, 0.22f, square: false);
    }

    public static void Play(string name)
    {
        if (inst == null || !inst.clips.TryGetValue(name, out var c)) return;
        inst.src.PlayOneShot(c);
    }

    AudioSource MakeMusicSource(AudioClip clip)
    {
        if (clip == null) return null;
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.playOnAwake = false;
        s.volume = 0f;
        s.Play();
        return s;
    }

    // ayarlardan: musiqi və səs effektlərinin səviyyəsi (0..1)
    static float musicLevel = 0.7f, sfxLevel = 0.8f;
    public static void SetVolumes(float music, float sfx)
    {
        musicLevel = music; sfxLevel = sfx;
        if (inst == null) return;
        inst.MusicVolume = 0.46f * music;
        if (inst.src != null) inst.src.volume = sfx;
    }

    // yağış səsinin gücü (0..1)
    public static void SetRain(float level)
    {
        if (inst != null) inst.rainLevel = level;
    }

    // gecə/gündüz musiqisini seç (keçid Update-də yavaş-yavaş olur)
    public static void SetNightMusic(bool night)
    {
        if (inst != null) inst.nightMusicOn = night;
    }

    void Update()
    {
        float k = Time.unscaledDeltaTime / 3f;   // 3 saniyəlik keçid
        if (dayMusic != null) dayMusic.volume = Mathf.MoveTowards(dayMusic.volume, nightMusicOn ? 0f : MusicVolume, k * 0.46f);
        if (nightMusic != null) nightMusic.volume = Mathf.MoveTowards(nightMusic.volume, nightMusicOn ? MusicVolume : 0f, k * 0.46f);
        if (rainSource != null) rainSource.volume = Mathf.MoveTowards(rainSource.volume, rainLevel * 0.55f * sfxLevel, Time.unscaledDeltaTime * 0.5f);
    }

    static AudioClip Tones(float[] freqs, float dur, float vol, bool square)
    {
        int per = (int)(SR * dur);
        var data = new float[per * freqs.Length];
        for (int i = 0; i < freqs.Length; i++)
            for (int s = 0; s < per; s++)
            {
                float t = (float)s / SR;
                float env = Mathf.Clamp01(1f - (float)s / per) * Mathf.Clamp01(s / 80f);
                float v = Mathf.Sin(2 * Mathf.PI * freqs[i] * t);
                if (square) v = v >= 0 ? 0.6f : -0.6f;
                data[i * per + s] = v * env * vol;
            }
        var clip = AudioClip.Create("tone", data.Length, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip Sweep(float f0, float f1, float dur, float vol)
    {
        int n = (int)(SR * dur);
        var data = new float[n];
        float ph = 0;
        for (int s = 0; s < n; s++)
        {
            float k = (float)s / n;
            float f = Mathf.Lerp(f0, f1, k) * (1f + 0.15f * Mathf.Sin(k * 20f));
            ph += 2 * Mathf.PI * f / SR;
            data[s] = Mathf.Sin(ph) * Mathf.Sin(Mathf.PI * k) * vol;
        }
        var clip = AudioClip.Create("sweep", n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip Mix(AudioClip a, AudioClip b)
    {
        int n = Mathf.Max(a.samples, b.samples);
        var da = new float[a.samples]; a.GetData(da, 0);
        var db = new float[b.samples]; b.GetData(db, 0);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = (i < da.Length ? da[i] : 0) + (i < db.Length ? db[i] : 0);
        var clip = AudioClip.Create("mix", n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip Noise(float dur, float vol)
    {
        int n = (int)(SR * dur);
        var data = new float[n];
        var r = new System.Random(7);
        for (int s = 0; s < n; s++) data[s] = ((float)r.NextDouble() * 2f - 1f) * (1f - (float)s / n) * vol;
        var clip = AudioClip.Create("noise", n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }
}
