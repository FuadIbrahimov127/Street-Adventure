using System.Collections.Generic;
using UnityEngine;

// Qısa müddətli effekt (məs. su damcıları): yavaş-yavaş yox olur.
public class MMFade : MonoBehaviour
{
    public float Life = 0.6f;
    float t;
    SpriteRenderer sr;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        t += Time.deltaTime;
        transform.position += new Vector3(0, -0.4f * Time.deltaTime, 0);
        if (sr != null) { var c = sr.color; c.a = 1f - t / Life; sr.color = c; }
        if (t >= Life) Destroy(gameObject);
    }
}
