using UnityEngine;

// Sadə kadr animasiyası (pul, fəvvarə, nərd masası).
public class MMSpriteAnim : MonoBehaviour
{
    public SpriteRenderer Target;
    public Sprite[] Frames;
    public float Fps = 6f;
    public bool RandomStart = true;
    float t;

    void Start()
    {
        if (Target == null) Target = GetComponent<SpriteRenderer>();
        if (RandomStart) t = Random.value * 10f;
    }

    void Update()
    {
        if (Frames == null || Frames.Length == 0 || Target == null) return;
        t += Time.deltaTime * Fps;
        Target.sprite = Frames[Mathf.FloorToInt(t) % Frames.Length];
    }
}
