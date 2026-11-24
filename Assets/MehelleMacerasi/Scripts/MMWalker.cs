using System.Collections.Generic;
using UnityEngine;

// Küçədə gəzən piyadalar: səki və cığırlarla gəzir (BFS yol tapma),
// yolu yalnız zolaqlı keçiddən keçir və maşın gəlirsə gözləyir. Gecə evə gedirlər.
public class MMWalker : MonoBehaviour
{
    public static readonly List<MMWalker> All = new List<MMWalker>();

    public SpriteRenderer Body;
    public SpriteRenderer Umbrella;     // yağışda açılır
    public Sprite[] Down = new Sprite[3], Up = new Sprite[3], Side = new Sprite[3];
    public float Speed = 1.5f;
    public bool NightOwl;          // gecə də gəzir
    public int Seed = 1;

    readonly List<Vector2Int> path = new List<Vector2Int>();
    int pathIdx;
    float waitT;
    float animT;
    float hopT;
    Vector2 facing = Vector2.down;
    System.Random rnd;
    bool hidden;
    SpriteRenderer[] renderers;

    public Vector2 Velocity { get; private set; }
    public bool Visible => !hidden;

    static string[] grid;
    static int gw, gh;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        rnd = new System.Random(Seed);
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        waitT = (float)rnd.NextDouble() * 2f;
    }

    static bool EnsureGrid()
    {
        var g = MMGame.I;
        if (g == null || g.WalkGrid == null || g.WalkGrid.Length == 0) return false;
        if (grid != g.WalkGrid) { grid = g.WalkGrid; gh = grid.Length; gw = grid[0].Length; }
        return true;
    }

    static bool Walkable(int c, int r) => c >= 0 && r >= 0 && c < gw && r < gh && grid[r][c] != '.';
    static bool IsCross(int c, int r) => Walkable(c, r) && grid[r][c] == 'x';
    static Vector2 CellCenter(Vector2Int cell) => new Vector2(cell.x + 0.5f, gh - 1 - cell.y + 0.3f);
    static Vector2Int CellOf(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), gh - 1 - Mathf.FloorToInt(p.y));

    void Update()
    {
        var g = MMGame.I;
        if (g == null || !EnsureGrid()) return;
        // qartopu dəyəndə hoppanır
        if (hopT > 0 && Body != null)
        {
            hopT -= Time.deltaTime;
            float k = Mathf.Clamp01(hopT / 0.35f);
            Body.transform.localPosition = new Vector3(0, hopT > 0 ? Mathf.Sin(k * Mathf.PI) * 0.3f : 0f, 0);
        }

        // gecə evə gedirlər (görünməz olurlar), səhər qayıdırlar
        bool sleep = !NightOwl && MMDayNight.I != null && MMDayNight.I.Night > 0.85f;
        if (sleep != hidden)
        {
            hidden = sleep;
            foreach (var r in renderers) if (r != null) r.enabled = !hidden;
        }
        if (Umbrella != null) Umbrella.enabled = !hidden && MMWeather.I != null && MMWeather.I.IsRaining;
        if (hidden || g.WorldPaused)
        {
            Velocity = Vector2.zero;
            Animate(false);
            return;
        }
        if (waitT > 0)
        {
            waitT -= Time.deltaTime;
            Velocity = Vector2.zero;
            Animate(false);
            return;
        }
        if (pathIdx >= path.Count)
        {
            PickTarget();
            if (path.Count == 0) { waitT = 1f; return; }
        }

        var cell = path[pathIdx];
        var cur = CellOf(transform.position);
        // keçidə girməzdən əvvəl maşınlara bax
        if (IsCross(cell.x, cell.y) && !IsCross(cur.x, cur.y) && CarComing(cell.x + 0.5f))
        {
            Velocity = Vector2.zero;
            Animate(false);
            return;
        }

        Vector2 target = CellCenter(cell);
        Vector2 pos = transform.position;
        Vector2 d = target - pos;
        float step = Speed * Time.deltaTime;
        if (d.magnitude <= step)
        {
            transform.position = target;
            pathIdx++;
            if (pathIdx >= path.Count) waitT = 1f + (float)rnd.NextDouble() * 4f;
        }
        else
        {
            Vector2 v = d.normalized;
            transform.position = pos + v * step;
            Velocity = v * Speed;
            facing = Mathf.Abs(v.x) > Mathf.Abs(v.y) ? new Vector2(Mathf.Sign(v.x), 0) : new Vector2(0, Mathf.Sign(v.y));
        }
        Animate(true);
    }

    // qartopu dəydi: dayanır və hoppanır
    public void HitBySnow()
    {
        waitT = Mathf.Max(waitT, 1.2f);
        hopT = 0.35f;
        MMAudio.Play("bounce");
    }

    static bool CarComing(float x)
    {
        foreach (var c in MMCar.All)
        {
            float dx = (x - c.transform.position.x) * c.Dir;
            if (dx > -1.6f && dx < 6f) return true;
        }
        return false;
    }

    void PickTarget()
    {
        path.Clear();
        pathIdx = 0;
        var start = CellOf(transform.position);
        if (!Walkable(start.x, start.y)) start = NearestWalkable(start);
        for (int tries = 0; tries < 12; tries++)
        {
            var t = new Vector2Int(rnd.Next(gw), rnd.Next(gh));
            if (!Walkable(t.x, t.y) || IsCross(t.x, t.y)) continue;
            if (Mathf.Abs(t.x - start.x) + Mathf.Abs(t.y - start.y) < 6) continue;
            if (Bfs(start, t)) return;
        }
    }

    Vector2Int NearestWalkable(Vector2Int c)
    {
        for (int r = 1; r < 6; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (Walkable(c.x + dx, c.y + dy)) return new Vector2Int(c.x + dx, c.y + dy);
        return c;
    }

    bool Bfs(Vector2Int s, Vector2Int t)
    {
        var prev = new Dictionary<Vector2Int, Vector2Int>();
        var q = new Queue<Vector2Int>();
        q.Enqueue(s);
        prev[s] = s;
        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            if (c == t) break;
            // qonşuları təsadüfi sırada yoxla ki, yollar müxtəlif olsun
            int o = rnd.Next(4);
            for (int k = 0; k < 4; k++)
            {
                var n = c + dirs[(k + o) % 4];
                if (!Walkable(n.x, n.y) || prev.ContainsKey(n)) continue;
                prev[n] = c;
                q.Enqueue(n);
            }
        }
        if (!prev.ContainsKey(t)) return false;
        var cur = t;
        while (cur != s) { path.Add(cur); cur = prev[cur]; }
        path.Reverse();
        return path.Count > 0;
    }

    void Animate(bool moving)
    {
        if (Body == null || Down[0] == null) return;
        Sprite[] set = facing.y > 0.5f ? Up : facing.y < -0.5f ? Down : Side;
        Body.flipX = facing.x < -0.5f;
        if (moving)
        {
            animT += Time.deltaTime * 6f;
            int f = Mathf.FloorToInt(animT) % 4;
            Body.sprite = set[f == 1 ? 1 : f == 3 ? 2 : 0];
        }
        else
        {
            animT = 0;
            Body.sprite = set[0];
        }
    }
}
