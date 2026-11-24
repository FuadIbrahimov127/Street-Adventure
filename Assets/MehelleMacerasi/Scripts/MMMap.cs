using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// M düyməsi ilə açılan canlı xəritə: ayrıca kamera oyunçunun olduğu xəritəni (zonanı) şəklə çəkir.
public class MMMap : MonoBehaviour
{
    public Camera MapCamera;
    public RectTransform PlayerMarker;
    public Image MarkerTemplate;
    public Sprite ExclSprite, QSprite;
    public Vector2 WorldSize = new Vector2(48, 36);
    public RectTransform FitRoot;   // xəritə çərçivəsi: ekrana sığması üçün miqyaslanır
    public Text TitleText;
    public string[] LabelZoneIds;   // hər zonanın yer adları ayrıca qrupdadır
    public GameObject[] LabelSets;
    Vector2 origin;
    readonly List<Image> pool = new List<Image>();
    float t;

    public void SetOpen(bool on)
    {
        gameObject.SetActive(on);
        if (MapCamera != null) MapCamera.enabled = on;
        if (!on) return;
        var g = MMGame.I;
        var zone = g != null ? g.CurrentZone : null;
        origin = zone != null ? zone.Origin : Vector2.zero;
        if (MapCamera != null) MapCamera.transform.position = new Vector3(origin.x + WorldSize.x / 2f, origin.y + WorldSize.y / 2f, -10f);
        if (LabelSets != null)
            for (int i = 0; i < LabelSets.Length; i++)
                if (LabelSets[i] != null) LabelSets[i].SetActive(zone != null && i < LabelZoneIds.Length && LabelZoneIds[i] == zone.Id);
        if (TitleText != null) TitleText.text = zone != null && zone.Id != "main" ? "XƏRİTƏ: " + zone.Title : "XƏRİTƏ";
        Update();
    }

    void Place(RectTransform rt, Vector3 world, float yOffset)
    {
        var n = new Vector2(Mathf.Clamp01((world.x - origin.x) / WorldSize.x), Mathf.Clamp01((world.y - origin.y + yOffset) / WorldSize.y));
        rt.anchorMin = n; rt.anchorMax = n;
        rt.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        var g = MMGame.I;
        if (g == null) return;
        t += Time.unscaledDeltaTime;
        if (FitRoot != null)
        {
            var pr = ((RectTransform)transform).rect;
            float k = Mathf.Min((pr.height - 124f) / FitRoot.rect.height, (pr.width - 60f) / FitRoot.rect.width);
            k = Mathf.Clamp(k, 0.3f, 1.25f);
            FitRoot.localScale = new Vector3(k, k, 1f);
        }
        if (g.Player != null && PlayerMarker != null)
        {
            Place(PlayerMarker, g.PlayerMapPosition, 1.7f);
            PlayerMarker.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(t * 7f));
        }
        int i = 0;
        foreach (var npc in MMNpc.All)
        {
            Sprite s = null;
            if (npc.MarkerExcl != null && npc.MarkerExcl.activeSelf) s = ExclSprite;
            else if (npc.MarkerQ != null && npc.MarkerQ.activeSelf) s = QSprite;
            if (s == null || MarkerTemplate == null) continue;
            // evin içindəki qonşu xəritədə evin qapısında göstərilir
            Vector3 wp = npc.transform.position;
            var room = MMRoom.Find(wp);
            if (room != null) wp = room.MapPosition;
            // yalnız bu xəritədəki qonşular
            if (g.CurrentZone != null && !g.CurrentZone.Bounds.Contains(wp)) continue;
            if (i >= pool.Count) pool.Add(Instantiate(MarkerTemplate, MarkerTemplate.transform.parent));
            var m = pool[i++];
            m.gameObject.SetActive(true);
            m.sprite = s;
            Place(m.rectTransform, wp, 2.1f);
        }
        for (; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
    }
}
