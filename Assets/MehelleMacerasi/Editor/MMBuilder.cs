using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Məhəllə Macərası səhnəsini MMLevelData xəritəsindən avtomatik qurur:
// zəmin (autotile kənarlarla), binalar, obyektlər, piyadalar, işıqlar, UI.
public static class MMBuilder
{
    const string Root = "Assets/MehelleMacerasi";
    const string ArtRoot = Root + "/Art";
    const string TileDir = Root + "/Tiles";
    const string ScenePath = "Assets/Scenes/MehelleMacerasi.unity";

    static Dictionary<string, Sprite> sprites;
    static Dictionary<string, Tile> tileCache;
    static Font font, fontBold;
    static Material unlitMat;
    static int W, H;
    static string[] ground, objects;
    static List<KeyValuePair<string, Vector2>> mapLabels;
    static HashSet<Vector2Int> blocked;   // piyadaların keçə bilmədiyi xanalar (obyektlər)
    static SpriteRenderer playerUmbrella;  // oyunçunun çətiri (SetupInventory-də təyin edilir)
    static HashSet<Vector2Int> reserved;   // gölməçə qoyulmayan xanalar (bağça, qar adamı yerləri)
    static string idPrefix = "";           // götürülən əşyaların id-si (ikinci xəritədə "z2_")
    const float Z2X = 200f;                // ikinci xəritənin (Məktəb məhəlləsi) dünyadakı yeri
    static List<KeyValuePair<string, Vector2>> mapLabels2;

    static readonly Dictionary<char, string> BuildingNames = new Dictionary<char, string>
    {
        { 'H', "Sara nənənin evi" }, { 'A', "Yaşayış binası" }, { 'P', "Poçt" }, { 'M', "Market" }, { 'C', "Çayxana" },
        { 'E', "Məktəb" }, { 'K', "Kitabxana" }, { 'R', "Çörəkxana" },
    };

    // bina simvolu -> (sprite adı, qapının soldan məsafəsi xanalarla)
    static readonly Dictionary<char, (string sprite, float door)> Buildings = new Dictionary<char, (string, float)>
    {
        { 'H', ("bld_house", 3.5f) }, { 'A', ("bld_apartment", 5.5f) }, { 'P', ("bld_post", 5.5f) },
        { 'M', ("bld_market", 6.5f) }, { 'C', ("bld_teahouse", 4.5f) },
        { 'E', ("bld_school", 7.0f) }, { 'K', ("bld_library", 4.0f) }, { 'R', ("bld_bakery", 2.5f) },
    };

    [MenuItem("Tools/Mehelle Macerasi/Build Scene")]
    public static void Build()
    {
        LoadAssets();
        ConfigureSorting();
        var scene = EnsureScene();
        foreach (var n in new[] { "MM_Level", "MM_UI", "MM_System", "MM_Interiors", "MM_Zone2", "MM_Player", "MM_Səviyyə", "MM_Sistem" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }
        tileCache = new Dictionary<string, Tile>();
        mapLabels = new List<KeyValuePair<string, Vector2>>();
        blocked = new HashSet<Vector2Int>();
        reserved = new HashSet<Vector2Int>();
        if (!AssetDatabase.IsValidFolder(TileDir)) AssetDatabase.CreateFolder(Root, "Tiles");

        ground = MMLevelData.Ground;
        objects = MMLevelData.Objects;
        H = ground.Length; W = ground[0].Length;

        var level = new GameObject("MM_Level");
        level.AddComponent<Grid>();
        var maps = new Dictionary<string, Tilemap>();
        int order = -30;
        foreach (var n in new[] { "Ground", "Road", "EdgeN", "EdgeE", "EdgeS", "EdgeW", "CornerNW", "CornerNE", "CornerSW", "CornerSE", "Curbs", "Shadows" })
            maps[n] = MakeTilemap(level, n, order++, false);
        maps["Hedges"] = MakeTilemap(level, "Hedges", order++, true);
        PaintGround(maps);
        var leavesMap = MakeTilemap(level, "SeasonLeaves", -17, false);
        var snowMap = MakeTilemap(level, "SeasonSnow", -17, false);
        PaintSeasonOverlays(leavesMap, snowMap);

        var sys = new GameObject("MM_System");
        var game = sys.AddComponent<MMGame>();
        game.RoadMap = maps["Road"];
        game.Crosswalks = FindCrosswalks();
        var dayNight = sys.AddComponent<MMDayNight>();
        dayNight.GlobalLight = EnsureGlobalLight();
        game.DayNight = dayNight;
        var seasons = sys.AddComponent<MMSeasons>();
        seasons.Tinted = new[] { maps["Ground"], maps["EdgeN"], maps["EdgeE"], maps["EdgeS"], maps["EdgeW"], maps["CornerNW"], maps["CornerNE"], maps["CornerSW"], maps["CornerSE"] };
        seasons.LeavesMaps = new[] { leavesMap }; seasons.SnowMaps = new[] { snowMap };
        seasons.Icons = Frames("season", 4);

        var objRoot = new GameObject("Objects").transform;
        objRoot.SetParent(level.transform, false);
        PlaceBuildings(objRoot);
        MMPlayer player = null;
        PlaceObjects(objRoot, ref player);
        game.Player = player;
        // oyunçu heç bir zonanın içində olmamalıdır (zona sönəndə oyunçu sönməsin)
        var actors = new GameObject("MM_Player");
        if (player != null) player.transform.SetParent(actors.transform, true);
        SetupInventory(sys);
        BuildSnowmen(objRoot);
        BuildFestival(objRoot, sys);
        MakeKitten(objRoot, "kit_black", "kitten_black", new Vector3(11.6f, H - 1 - 26 + 0.05f, 0));
        MakeKitten(objRoot, "kit_ginger", "kitten_ginger", new Vector3(39.5f, H - 1 - 7 + 0.1f, 0));
        var balls = sys.AddComponent<MMSnowballs>();
        balls.BallSprite = S("snowball"); balls.SplatSprite = S("snow_splat");
        SetupWeather(sys, objRoot);
        game.WorldSize = new Vector2(W, H);
        var interiors = new GameObject("MM_Interiors");
        BuildInteriors(interiors.transform);
        MakeLanes(level.transform);
        // sağ kənarda (sətir 21-23) ikinci xəritəyə keçid
        MakeBounds(level.transform, true, 12f, 15f);
        game.WalkGrid = BuildWalkGrid();
        SpawnWalkers(objRoot, game.WalkGrid);

        mapLabels.Add(new KeyValuePair<string, Vector2>("Park", new Vector2(4.5f, H - 15.4f)));
        mapLabels.Add(new KeyValuePair<string, Vector2>("Əsas küçə", new Vector2(W * 0.5f + 6f, H - 11f)));
        mapLabels.Add(new KeyValuePair<string, Vector2>("Məktəb məhəlləsi »", new Vector2(W - 6.5f, H - 20.6f)));

        // ---- ikinci xəritə: Məktəb məhəlləsi ----
        var zMain = new GameObject("Zone_Main"); zMain.transform.SetParent(sys.transform, false);
        var zoneMain = zMain.AddComponent<MMZone>();
        zoneMain.Id = "main"; zoneMain.Title = "Məhəllə"; zoneMain.Root = level; zoneMain.Bounds = new Rect(0, 0, W, H);
        var zSchool = new GameObject("Zone_School"); zSchool.transform.SetParent(sys.transform, false);
        var zoneSchool = zSchool.AddComponent<MMZone>();
        zoneSchool.Id = "school"; zoneSchool.Title = "Məktəb məhəlləsi"; zoneSchool.Bounds = new Rect(Z2X, 0, W, H);
        Tilemap[] z2Tinted; Tilemap z2Leaves, z2Snow;
        zoneSchool.Root = BuildZone2(sys, zoneMain, out z2Tinted, out z2Leaves, out z2Snow);
        var tinted = new List<Tilemap>(seasons.Tinted); tinted.AddRange(z2Tinted);
        seasons.Tinted = tinted.ToArray();
        seasons.LeavesMaps = new[] { leavesMap, z2Leaves }; seasons.SnowMaps = new[] { snowMap, z2Snow };
        // əsas xəritədən çıxış
        ZoneDoor(level.transform, new Vector2(W - 0.25f, 13.5f), Vector2.right, new Vector2(Z2X + 1.1f, 13.3f), zoneSchool, "Məktəb məhəlləsinə gəldin!");
        var mapCam = CreateMapCamera(sys.transform);
        game.UI = BuildUI(mapCam);

        SetupCamera(player);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log($"[Mehelle Macerasi] Scene built: {W}x{H} map, {objRoot.childCount} objects.");
    }

    // ---------------- köməkçilər ----------------
    static void LoadAssets()
    {
        sprites = new Dictionary<string, Sprite>();
        foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtRoot }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (sp != null) sprites[Path.GetFileNameWithoutExtension(p)] = sp;
        }
        font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/PixelifySans-Regular.ttf");
        fontBold = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/PixelifySans-Bold.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fontBold == null) fontBold = font;
        unlitMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        if (unlitMat == null)
            foreach (var guid in AssetDatabase.FindAssets("Sprite-Unlit-Default t:Material"))
            {
                unlitMat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (unlitMat != null) break;
            }
        if (unlitMat == null) Debug.LogWarning("[Mehelle Macerasi] Sprite-Unlit-Default material not found; glows will be lit.");
    }

    static Sprite S(string name)
    {
        if (sprites.TryGetValue(name, out var s)) return s;
        Debug.LogWarning("[Mehelle Macerasi] Sprite not found: " + name);
        return null;
    }

    // sprite varsa qaytarır, yoxdursa xəbərdarlıqsız null
    static Sprite TS(string name) => sprites.TryGetValue(name, out var s) ? s : null;

    // fəsil variantları (_s1 yay, _s2 payız, _s3 qış) varsa, MMSeasonal əlavə edir
    static void Seasonal(GameObject go, string baseName)
    {
        var v = new[] { S(baseName), TS(baseName + "_s1"), TS(baseName + "_s2"), TS(baseName + "_s3") };
        if (v[1] == null && v[2] == null && v[3] == null) return;
        var m = go.AddComponent<MMSeasonal>();
        m.Target = go.GetComponent<SpriteRenderer>();
        m.Variants = v;
    }

    static Sprite[] Frames(string prefix, int n)
    {
        var arr = new Sprite[n];
        for (int i = 0; i < n; i++) arr[i] = S(prefix + "_" + i);
        return arr;
    }

    static int Hash(int c, int r) => Mathf.Abs(c * 7 + r * 13 + c * r * 3);

    static void ConfigureSorting()
    {
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0, 1, 0);
        var r2d = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Renderer2D.asset");
        if (r2d != null)
        {
            var so = new SerializedObject(r2d);
            var mode = so.FindProperty("m_TransparencySortMode");
            var axis = so.FindProperty("m_TransparencySortAxis");
            if (mode != null) mode.intValue = (int)TransparencySortMode.CustomAxis;
            if (axis != null) axis.vector3Value = new Vector3(0, 1, 0);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(r2d);
        }
    }

    static UnityEngine.SceneManagement.Scene EnsureScene()
    {
        if (!File.Exists(ScenePath))
            AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath);
        var active = EditorSceneManager.GetActiveScene();
        if (active.path == ScenePath) return active;
        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static void AddToBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(s => s.path == ScenePath);
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    static Light2D EnsureGlobalLight()
    {
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            if (l.lightType == Light2D.LightType.Global) return l;
        var go = new GameObject("Global Light 2D");
        var gl = go.AddComponent<Light2D>();
        gl.lightType = Light2D.LightType.Global;
        gl.intensity = 1f;
        return gl;
    }

    static Tilemap MakeTilemap(GameObject grid, string name, int order, bool collider)
    {
        var go = new GameObject(name);
        go.transform.SetParent(grid.transform, false);
        var tm = go.AddComponent<Tilemap>();
        var tr = go.AddComponent<TilemapRenderer>();
        tr.sortingOrder = order;
        if (collider) go.AddComponent<TilemapCollider2D>();
        return tm;
    }

    static Tile T(string sprite, bool collide = false)
    {
        string key = sprite + (collide ? "_solid" : "");
        if (tileCache.TryGetValue(key, out var t)) return t;
        string path = $"{TileDir}/{key}.asset";
        t = AssetDatabase.LoadAssetAtPath<Tile>(path);
        bool isNew = t == null;
        if (isNew) t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = S(sprite);
        t.color = Color.white;
        t.colliderType = collide ? Tile.ColliderType.Grid : Tile.ColliderType.None;
        if (isNew) AssetDatabase.CreateAsset(t, path); else EditorUtility.SetDirty(t);
        tileCache[key] = t;
        return t;
    }

    static Vector3Int Cell(int c, int r) => new Vector3Int(c, H - 1 - r, 0);
    static char At(string[] m, int c, int r) => (r < 0 || r >= m.Length || c < 0 || c >= m[r].Length) ? '#' : m[r][c];
    static bool IsGrassy(char ch) => ch == '.' || ch == ',';
    static bool IsBuilding(char ch) => Buildings.ContainsKey(ch);

    // ---------------- zəmin ----------------
    static void PaintGround(Dictionary<string, Tilemap> maps)
    {
        var sides = new[] { ('n', 0, -1, "EdgeN"), ('e', 1, 0, "EdgeE"), ('s', 0, 1, "EdgeS"), ('w', -1, 0, "EdgeW") };
        var corners = new[] { ("nw", -1, -1, "CornerNW"), ("ne", 1, -1, "CornerNE"), ("sw", -1, 1, "CornerSW"), ("se", 1, 1, "CornerSE") };
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
            {
                char ch = ground[r][c];
                var cell = Cell(c, r);
                int h = Hash(c, r);
                switch (ch)
                {
                    case 's':
                    case 'p':
                    case 'y':
                    case 'q':
                        maps["Ground"].SetTile(cell, T((ch == 's' ? "pavers_" : ch == 'p' ? "dirt_" : ch == 'y' ? "yard_" : "field_") + (h % 3)));
                        foreach (var (side, dx, dy, map) in sides)
                        {
                            char v = At(ground, c + dx, r + dy);
                            if (IsGrassy(v) || v == '#') maps[map].SetTile(cell, T("edge_grass_" + side));
                            else if (ch == 's' && "tTbB".IndexOf(v) >= 0 && (side == 'n' || side == 's')) maps["Curbs"].SetTile(cell, T("curb_" + side));
                        }
                        foreach (var (name, dx, dy, map) in corners)
                        {
                            if (IsGrassy(At(ground, c + dx, r + dy)) && !IsGrassy(At(ground, c + dx, r)) && !IsGrassy(At(ground, c, r + dy))
                                && At(ground, c + dx, r) != '#' && At(ground, c, r + dy) != '#')
                                maps[map].SetTile(cell, T("corner_grass_" + name));
                        }
                        break;
                    case 't': maps["Road"].SetTile(cell, T("road_top")); break;
                    case 'b': maps["Road"].SetTile(cell, T("road_bottom")); break;
                    case 'T': maps["Road"].SetTile(cell, T("cross_top")); break;
                    case 'B': maps["Road"].SetTile(cell, T("cross_bottom")); break;
                    case '#':
                        maps["Ground"].SetTile(cell, T("grass_" + (h % 4)));
                        maps["Hedges"].SetTile(cell, T(At(ground, c, r + 1) != '#' ? "hedge_front" : "hedge", true));
                        break;
                    case ',':
                        maps["Ground"].SetTile(cell, T("grass_flowers_" + (h % 2)));
                        break;
                    default:
                        maps["Ground"].SetTile(cell, T("grass_" + (h % 4)));
                        break;
                }
                // kol və bina altında yerə düşən kölgə
                char up = At(ground, c, r - 1);
                if (ch != '#' && !IsBuilding(ch) && "tTbB".IndexOf(ch) < 0 && r > 0 && (up == '#' || IsBuilding(up)))
                    maps["Shadows"].SetTile(cell, T("ao_shadow"));
            }
    }

    static List<Rect> FindCrosswalks()
    {
        var list = new List<Rect>();
        for (int r = 0; r < H; r++)
        {
            int c = 0;
            while (c < W)
            {
                if (ground[r][c] == 'T')
                {
                    int c0 = c; while (c < W && ground[r][c] == 'T') c++;
                    // T (yuxarı zolaq) + B (aşağı zolaq) birlikdə
                    float yBottom = H - 1 - (r + 1);
                    list.Add(new Rect(c0, yBottom, c - c0, 2f));
                }
                else c++;
            }
        }
        return list;
    }

    // ---------------- sprite obyektləri ----------------
    static GameObject MakeSprite(Transform parent, string name, Sprite sp, Vector3 pos, int order, bool ySort)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = order;
        if (ySort) sr.spriteSortPoint = SpriteSortPoint.Pivot;
        return go;
    }

    static void AddShadow(GameObject owner, float scaleX = 1f, float scaleY = 1f)
    {
        var sh = MakeSprite(owner.transform, "Shadow", S("shadow"), owner.transform.position + new Vector3(0, 0.02f, 0), -1, false);
        sh.transform.localScale = new Vector3(scaleX, scaleY, 1);
    }

    // başın üstündə çətir (ağ sprite, rənglənir); əvvəlcə gizlidir
    static SpriteRenderer AddUmbrella(GameObject owner, Color tint)
    {
        var u = MakeSprite(owner.transform, "Umbrella", S("umbrella"), owner.transform.position + new Vector3(0.06f, 0.5f, 0), 1, false);
        var sr = u.GetComponent<SpriteRenderer>();
        sr.color = tint;
        sr.enabled = false;
        return sr;
    }

    static GameObject Child(GameObject owner, string name, Sprite sp, float y, int order, bool active)
    {
        var go = MakeSprite(owner.transform, name, sp, owner.transform.position + new Vector3(0, y, 0), order, false);
        go.SetActive(active);
        return go;
    }

    static void Box(GameObject go, float w, float h, float oy, bool trigger = false)
    {
        var b = go.AddComponent<BoxCollider2D>();
        b.size = new Vector2(w, h);
        b.offset = new Vector2(0, oy);
        b.isTrigger = trigger;
    }

    static Light2D PointLight(GameObject owner, string name, Vector3 local, float radius, Color col, float maxIntensity, float threshold)
    {
        var go = new GameObject(name);
        go.transform.SetParent(owner.transform, false);
        go.transform.localPosition = local;
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = radius * 0.15f;
        l.falloffIntensity = 0.65f;
        l.color = col;
        l.intensity = 0f;
        var nl = go.AddComponent<MMNightLight>();
        nl.Light = l;
        nl.MaxIntensity = maxIntensity;
        nl.Threshold = threshold;
        return l;
    }

    static void Block(int c, int r) => blocked.Add(new Vector2Int(c, r));

    // ---------------- binalar ----------------
    static void PlaceBuildings(Transform root)
    {
        var seen = new HashSet<Vector2Int>();
        var warm = new Color(1f, 0.8f, 0.52f);
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
            {
                char ch = ground[r][c];
                if (!IsBuilding(ch) || seen.Contains(new Vector2Int(c, r))) continue;
                int c1 = c; while (c1 + 1 < W && ground[r][c1 + 1] == ch) c1++;
                int r1 = r; while (r1 + 1 < H && ground[r1 + 1][c] == ch) r1++;
                for (int rr = r; rr <= r1; rr++) for (int cc = c; cc <= c1; cc++) seen.Add(new Vector2Int(cc, rr));
                int w = c1 - c + 1, h = r1 - r + 1;
                var info = Buildings[ch];
                var pos = new Vector3(c + w / 2f, H - 1 - r1, 0);
                var go = MakeSprite(root, "Building_" + info.sprite.Substring(4), S(info.sprite), pos, 0, true);
                Seasonal(go, info.sprite);
                if (ch == 'E')
                {
                    var sd = new GameObject("SchoolDoor"); sd.transform.SetParent(root, false);
                    sd.transform.position = new Vector3(c + info.door, H - 1 - r1 - 0.1f, 0);
                    Prop(sd, MMPropKind.Talk, "Məktəb", new[] { "Məktəbin qapısı bağlıdır. Uşaqlar tətildədir!", "Lövhədə yazılıb: \"Tezliklə açılacaq. Hamınızı gözləyirik!\"" }, S("bld_school"), 1.6f);
                }
                Box(go, w, h - 0.15f, (h - 0.15f) / 2f);
                // gecə pəncərə işıqları (işıqlandırmadan asılı olmayan material)
                var glow = MakeSprite(go.transform, "WindowLights", S(info.sprite + "_glow"), pos + new Vector3(0, -0.01f, 0), 0, true);
                var gsr = glow.GetComponent<SpriteRenderer>();
                if (unlitMat != null) gsr.sharedMaterial = unlitMat;
                var nl = glow.AddComponent<MMNightLight>();
                nl.Glow = gsr; nl.Threshold = 0.18f + (c % 3) * 0.06f; nl.Flicker = true;
                // qapı üstündə isti işıq
                PointLight(go, "DoorLight", new Vector3(c + info.door - pos.x, 1.3f, 0), 3f, warm, 0.8f, 0.3f);
                if (BuildingNames.TryGetValue(ch, out var bname))
                    mapLabels.Add(new KeyValuePair<string, Vector2>(bname, new Vector2(c + w / 2f, H - (r + r1 + 1) / 2f)));
            }
    }

    // ---------------- obyektlər ----------------
    static void PlaceObjects(Transform root, ref MMPlayer player)
    {
        var lampCol = new Color(1f, 0.82f, 0.55f);
        var rnd = new System.Random(42);
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
            {
                char ch = objects[r][c];
                if (ch == '.') continue;
                float x = c + 0.5f, y = H - 1 - r;
                Vector3 p = new Vector3(x, y, 0);
                Vector3 pc = new Vector3(x, y + 0.12f, 0);
                int h = Hash(c, r);
                GameObject go;
                switch (ch)
                {
                    case 'T':
                    case 'Y':
                    {
                        string sp = ch == 'T' ? new[] { "tree_oak", "tree_oak2", "tree_blossom", "tree_oak", "tree_oak2" }[h % 5]
                                              : new[] { "tree_pomegranate", "tree_cypress", "tree_pomegranate", "tree_blossom" }[h % 4];
                        go = MakeSprite(root, "Tree", S(sp), p, 0, true);
                        Seasonal(go, sp);
                        Box(go, 0.5f, 0.3f, 0.15f);
                        AddShadow(go, sp == "tree_cypress" ? 1.4f : 2.6f, 1.4f);
                        break;
                    }
                    case 'L':
                    {
                        go = MakeSprite(root, "StreetLamp", S("lamp"), p, 0, true);
                        Box(go, 0.3f, 0.25f, 0.12f);
                        var nl = go.AddComponent<MMNightLight>();
                        nl.Swap = go.GetComponent<SpriteRenderer>(); nl.DaySprite = S("lamp"); nl.NightSprite = S("lamp_on");
                        nl.Threshold = 0.35f;
                        PointLight(go, "LampLight", new Vector3(0, 2.2f, 0), 4.4f, lampCol, 1.0f, 0.35f);
                        Block(c, r);
                        break;
                    }
                    case 'n':
                        go = MakeSprite(root, "Bench", S("bench"), new Vector3(c + 1f, y, 0), 0, true);
                        Seasonal(go, "bench");
                        Box(go, 1.8f, 0.35f, 0.2f); AddShadow(go, 2.2f);
                        Block(c, r); Block(c + 1, r);
                        break;
                    case 'u':
                        go = MakeSprite(root, "TrashBin", S("bin"), p, 0, true); Box(go, 0.55f, 0.3f, 0.15f); AddShadow(go, 0.8f);
                        Block(c, r);
                        break;
                    case 'o':
                    case 'f':
                        string bushName = ch == 'o' ? "bush" : (h % 2 == 0 ? "bush_flowers" : "bush_flowers2");
                        go = MakeSprite(root, "Bush", S(bushName), p, 0, true);
                        Seasonal(go, bushName);
                        Box(go, 0.9f, 0.4f, 0.2f); AddShadow(go, 1.3f);
                        Block(c, r);
                        break;
                    case 'F':
                    {
                        go = MakeSprite(root, "Fountain", S("fountain_0"), p, 0, true);
                        var a = go.AddComponent<MMSpriteAnim>(); a.Frames = Frames("fountain", 3); a.Fps = 6f;
                        Box(go, 2.8f, 1.3f, 0.75f);
                        var fo = go.AddComponent<MMFountain>();
                        fo.InteractOffset = new Vector2(0, 0.75f); fo.RadiusBonus = 0.9f;
                        fo.Portrait = S("fountain_0");
                        fo.Prompt = Child(go, "Prompt", S("prompt_e"), 3.0f, 50, false);
                        mapLabels.Add(new KeyValuePair<string, Vector2>("Arzu fəvvarəsi", new Vector2(x, y - 0.6f)));
                        for (int dc = -1; dc <= 1; dc++) { Block(c + dc, r); Block(c + dc, r - 1); }
                        break;
                    }
                    case 'N':
                    {
                        go = MakeSprite(root, "BackgammonTable", S("nerd_table_0"), p, 0, true);
                        var a = go.AddComponent<MMSpriteAnim>(); a.Frames = Frames("nerd_table", 2); a.Fps = 1.3f;
                        Box(go, 3.0f, 0.8f, 0.45f); AddShadow(go, 3.2f);
                        var npc = go.AddComponent<MMNpc>();
                        npc.Id = MMNpcId.Babalar; npc.DisplayName = "Kamil baba və Rauf baba";
                        npc.Body = go.GetComponent<SpriteRenderer>();
                        npc.Portrait = S("nerd_table_0");
                        npc.InteractOffset = new Vector2(0, 0.3f); npc.RadiusBonus = 0.6f;
                        npc.Prompt = Child(go, "Prompt", S("prompt_e"), 2.4f, 50, false);
                        npc.MarkerExcl = Child(go, "MarkerExcl", S("marker_excl"), 2.3f, 50, false);
                        npc.MarkerQ = Child(go, "MarkerQ", S("marker_q"), 2.3f, 50, false);
                        for (int dc = -1; dc <= 1; dc++) Block(c + dc, r);
                        break;
                    }
                    case 'c':
                    case 'm':
                    {
                        bool coin = ch == 'c';
                        go = new GameObject(coin ? "Coin" : "Letter"); go.transform.SetParent(root, false); go.transform.position = pc;
                        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = coin ? 0.32f : 0.36f; col.offset = new Vector2(0, 0.3f);
                        var vis = MakeSprite(go.transform, "Visual", coin ? S("coin_0") : S("letter"), pc, 0, true);
                        if (coin) { var a = vis.AddComponent<MMSpriteAnim>(); a.Frames = Frames("coin", 4); a.Fps = 8f; }
                        AddShadow(go, coin ? 0.6f : 0.8f);
                        var pk = go.AddComponent<MMPickup>();
                        pk.Kind = coin ? MMPickupKind.Coin : MMPickupKind.Letter;
                        pk.Id = idPrefix + (coin ? "coin_" : "letter_") + c + "_" + r;
                        pk.Visual = vis.transform;
                        break;
                    }
                    case 'k':
                    {
                        go = MakeSprite(root, "Cat_Mestan", S("cat_idle_0"), pc, 0, true);
                        AddShadow(go, 0.8f);
                        var cat = go.AddComponent<MMCat>();
                        cat.Body = go.GetComponent<SpriteRenderer>();
                        cat.Idle = Frames("cat_idle", 2); cat.Walk = Frames("cat_walk", 2);
                        cat.Portrait = S("cat_idle_0");
                        cat.InteractOffset = new Vector2(0, 0.3f);
                        cat.Prompt = Child(go, "Prompt", S("prompt_e"), 1.1f, 50, false);
                        break;
                    }
                    case '@':
                    {
                        go = MakeSprite(root, "Player", S("player_down_0"), pc, 0, true);
                        var rb = go.AddComponent<Rigidbody2D>();
                        rb.gravityScale = 0; rb.freezeRotation = true;
                        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                        Box(go, 0.55f, 0.28f, 0.14f);
                        AddShadow(go);
                        // çətir başın üstündə görünsün deyə bütün hissələr bir qrupda sıralanır
                        go.AddComponent<SortingGroup>();
                        playerUmbrella = AddUmbrella(go, new Color(0.9f, 0.32f, 0.28f));
                        var pl = go.AddComponent<MMPlayer>();
                        pl.Body = go.GetComponent<SpriteRenderer>();
                        pl.Down = Frames("player_down", 3); pl.Up = Frames("player_up", 3); pl.Side = Frames("player_side", 3);
                        // gecə oyunçunun ətrafında zəif işıq (yaxşı görünsün deyə)
                        PointLight(go, "PlayerLight", new Vector3(0, 0.8f, 0), 2.8f, new Color(1f, 0.92f, 0.78f), 0.45f, 0.4f);
                        player = pl;
                        break;
                    }
                    case '1': case '2': case '3': case '5': case '6':
                    {
                        var info = ch == '1' ? ("sara", "Sara nənə", MMNpcId.Sara) :
                                   ch == '2' ? ("elcin", "Poçtalyon Elçin", MMNpcId.Elcin) :
                                   ch == '3' ? ("reshad", "Çayxanaçı Rəşad", MMNpcId.Reshad) :   // (indi çayxananın içində)
                                   ch == '5' ? ("nigar", "Tələbə Nigar", MMNpcId.Nigar) : ("kamal", "Qonşu Kamal", MMNpcId.Kamal);
                        go = new GameObject("NPC_" + info.Item1); go.transform.SetParent(root, false); go.transform.position = pc;
                        Box(go, 0.6f, 0.3f, 0.15f);
                        var body = MakeSprite(go.transform, "Body", S(info.Item1 + "_down_0"), pc, 0, true);
                        AddShadow(go);
                        var npc = go.AddComponent<MMNpc>();
                        npc.Id = info.Item3; npc.DisplayName = info.Item2;
                        npc.Body = body.GetComponent<SpriteRenderer>();
                        npc.Down = Frames(info.Item1 + "_down", 3); npc.Up = Frames(info.Item1 + "_up", 3); npc.Side = Frames(info.Item1 + "_side", 3);
                        npc.Portrait = S(info.Item1 + "_down_0");
                        npc.Prompt = Child(go, "Prompt", S("prompt_e"), 2.0f, 50, false);
                        npc.MarkerExcl = Child(go, "MarkerExcl", S("marker_excl"), 1.95f, 50, false);
                        npc.MarkerQ = Child(go, "MarkerQ", S("marker_q"), 1.95f, 50, false);
                        if (ch == '1')
                        {
                            var seat = new GameObject("CatSeat"); seat.transform.SetParent(go.transform, false);
                            seat.transform.position = pc + new Vector3(0.95f, -0.15f, 0);
                            npc.CatSeat = seat.transform;
                        }
                        Block(c, r);
                        break;
                    }
                    case 'S':
                    {
                        go = new GameObject("Shopkeeper_Aysel"); go.transform.SetParent(root, false); go.transform.position = pc;
                        Box(go, 0.6f, 0.3f, 0.15f);
                        var body = MakeSprite(go.transform, "Body", S("aysel_down_0"), pc, 0, true);
                        AddShadow(go);
                        var shop = go.AddComponent<MMShop>();
                        shop.Body = body.GetComponent<SpriteRenderer>();
                        shop.Down = Frames("aysel_down", 3); shop.Side = Frames("aysel_side", 3);
                        shop.Prompt = Child(go, "Prompt", S("prompt_e"), 2.0f, 50, false);
                        Block(c, r);
                        break;
                    }
                    case 'D':
                        go = MakeSprite(root, "FruitStand", S("fruit_stand"), new Vector3(c + 0.5f, y + 0.05f, 0), 0, true);
                        Seasonal(go, "fruit_stand");
                        Box(go, 2.0f, 0.45f, 0.25f); AddShadow(go, 2.4f);
                        Block(c - 1, r); Block(c, r); Block(c + 1, r);
                        break;
                    case 'g':
                    {
                        // göyərçin dəstəsi
                        for (int i = 0; i < 4; i++)
                        {
                            var off = new Vector3((float)rnd.NextDouble() * 2.4f - 1.2f, (float)rnd.NextDouble() * 1.2f - 0.6f, 0);
                            var pg = MakeSprite(root, "Pigeon", S("pigeon_0"), p + off + new Vector3(0, 0.2f, 0), 0, true);
                            var pigeon = pg.AddComponent<MMPigeon>();
                            pigeon.Body = pg.GetComponent<SpriteRenderer>();
                            pigeon.Frames = Frames("pigeon", 4);
                        }
                        break;
                    }
                    case 'w':
                    {
                        go = MakeSprite(root, "Flowerbed", S("flowerbed_dry"), p + new Vector3(0, 0.1f, 0), 0, true);
                        go.GetComponent<SpriteRenderer>().sortingOrder = -1;
                        var fb = go.AddComponent<MMFlowerbed>();
                        fb.Id = "flower_" + c + "_" + r; fb.Body = go.GetComponent<SpriteRenderer>();
                        fb.DrySprite = S("flowerbed_dry"); fb.BloomSprite = S("flowerbed");
                        break;
                    }
                    case 'b':
                    {
                        // gündüz kəpənək, gecə atəşböcəkləri
                        var tints = new[] { new Color(1f, 0.85f, 0.3f), new Color(1f, 1f, 1f), new Color(1f, 0.6f, 0.75f), new Color(0.6f, 0.8f, 1f) };
                        var bf = MakeSprite(root, "Butterfly", S("butterfly_0"), p, 5, false);
                        var bsr = bf.GetComponent<SpriteRenderer>(); bsr.color = tints[h % tints.Length];
                        var bc = bf.AddComponent<MMCritter>(); bc.Type = MMCritter.Kind.Butterfly; bc.Body = bsr; bc.Frames = Frames("butterfly", 2);
                        for (int i = 0; i < 3; i++)
                        {
                            var ff = MakeSprite(root, "Firefly", S("firefly"), p + new Vector3(i - 1, (i % 2) * 0.5f, 0), 6, false);
                            var fsr = ff.GetComponent<SpriteRenderer>();
                            if (unlitMat != null) fsr.sharedMaterial = unlitMat;
                            var fc = ff.AddComponent<MMCritter>(); fc.Type = MMCritter.Kind.Firefly; fc.Body = fsr; fc.Radius = 1.8f;
                        }
                        break;
                    }
                    case 'j':
                    {
                        // itmiş kitab (kitabxananın tapşırığı)
                        go = new GameObject("Book"); go.transform.SetParent(root, false); go.transform.position = pc;
                        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.38f; col.offset = new Vector2(0, 0.3f);
                        var vis = MakeSprite(go.transform, "Visual", S(new[] { "book_red", "book_blue", "book_green" }[h % 3]), pc, 0, true);
                        AddShadow(go, 0.8f);
                        var pk = go.AddComponent<MMPickup>();
                        pk.Kind = MMPickupKind.Book; pk.Id = idPrefix + "book_" + c + "_" + r; pk.Visual = vis.transform;
                        break;
                    }
                    case 'W':
                    {
                        go = MakeSprite(root, "Swings", S("swing_0"), p, 0, true);
                        var a = go.AddComponent<MMSpriteAnim>(); a.Frames = Frames("swing", 2); a.Fps = 1.2f;
                        Box(go, 1.9f, 0.3f, 0.15f); AddShadow(go, 2.4f);
                        Block(c, r); Block(c + 1, r);
                        break;
                    }
                    case 'z':
                        go = MakeSprite(root, "Slide", S("slide"), p, 0, true);
                        Box(go, 1.6f, 0.3f, 0.15f); AddShadow(go, 2.0f);
                        break;
                    case 'h':
                        go = MakeSprite(root, "BasketHoop", S("hoop"), p, 0, true);
                        Box(go, 0.4f, 0.25f, 0.12f); AddShadow(go, 0.8f);
                        break;
                    case 'Z':
                    {
                        go = MakeSprite(root, "Tandir", S("tandir_0"), p, 0, true);
                        var a = go.AddComponent<MMSpriteAnim>(); a.Frames = Frames("tandir", 2); a.Fps = 4f;
                        Box(go, 1.3f, 0.5f, 0.25f); AddShadow(go, 1.6f);
                        PointLight(go, "FireLight", new Vector3(0, 0.6f, 0), 2.4f, new Color(1f, 0.6f, 0.3f), 0.9f, 0.2f);
                        break;
                    }
                    case '7':
                    {
                        var npc = MakeNpc(root, "gulnare", "Çörəkçi Gülnarə xala", MMNpcId.Gulnare, pc);
                        go = npc.gameObject;
                        Block(c, r);
                        break;
                    }
                    default:
                        Debug.LogWarning($"[Mehelle Macerasi] Unknown object symbol '{ch}' ({c},{r})");
                        break;
                }
            }
    }

    // ---------------- evlərin içi ----------------
    // otaqlar xəritədən kənarda (x >= 60) qurulur; qapılar oyunçunu ora və geri aparır
    static void BuildInteriors(Transform parent)
    {
        var root = new GameObject("Interiors").transform;
        root.SetParent(parent, false);
        var warm = new Color(1f, 0.85f, 0.62f);

        // ---- Sara nənənin evi ----
        var sara = MakeRoom(root, "Room_SaraHouse", "room_sara", new Vector2(60, 2), "Sara nənənin evi", new Vector2(6.5f, H - 7.2f), warm);
        Vector2 o = new Vector2(60, 2);
        Furniture(sara, "int_carpet", o + new Vector2(6f, 2f), 0, 0, 0, false);
        Furniture(sara, "int_sofa", o + new Vector2(6f, 6.5f), 3.0f, 0.6f, 0.3f, true);
        var sam = Furniture(sara, "int_samovar", o + new Vector2(1.6f, 6.5f), 1.3f, 0.5f, 0.25f, true);
        Prop(sam, MMPropKind.Samovar, "Samovar", null, S("int_samovar"), 2.6f);
        Furniture(sara, "int_plant", o + new Vector2(3.3f, 6.6f), 0.8f, 0.4f, 0.2f, true);
        var shelf = Furniture(sara, "int_bookshelf", o + new Vector2(8.6f, 6.6f), 1.9f, 0.5f, 0.25f, true);
        Prop(shelf, MMPropKind.Talk, "Kitab rəfi", new[] { "Rəfdə köhnə kitablar var: nağıllar, şeirlər, yemək kitabı...", "Ən yuxarıda \"Kitabi-Dədə Qorqud\" durur." }, S("int_bookshelf"), 2.9f);
        var tv = Furniture(sara, "int_tv", o + new Vector2(10.7f, 6.5f), 1.7f, 0.5f, 0.25f, true);
        Prop(tv, MMPropKind.TV, "Televizor", null, S("int_tv"), 2.5f);
        var chest = Furniture(sara, "int_chest", o + new Vector2(10.4f, 1.6f), 1.7f, 0.5f, 0.25f, true);
        Prop(chest, MMPropKind.Chest, "Sandıq", null, S("int_chest"), 1.8f).Id = "chest_sara";
        var bed = Furniture(sara, "int_catbed", o + new Vector2(1.7f, 2.0f), 0, 0, 0, true);
        Prop(bed, MMPropKind.Talk, "Pişik yatağı", new[] { "Mestanın yumşaq yatağı.", "Bura onun ən sevdiyi yerdir... qaçmadığı vaxtlarda!" }, S("int_catbed"), 1.2f);
        Doors(sara, new Vector2(6.5f, H - 7.15f), o, "Sara nənənin evinə girdin. Samovarda çay qaynayır!");

        // ---- Market ----
        var market = MakeRoom(root, "Room_Market", "room_market", new Vector2(80, 2), "Market", new Vector2(42.5f, H - 7.2f), warm);
        o = new Vector2(80, 2);
        Furniture(market, "int_counter", o + new Vector2(6f, 4.8f), 3.4f, 0.8f, 0.4f, true);
        Furniture(market, "int_shelf", o + new Vector2(2.7f, 2.4f), 3.0f, 0.5f, 0.25f, true);
        Furniture(market, "int_shelf", o + new Vector2(9.3f, 2.4f), 3.0f, 0.5f, 0.25f, true);
        var fr = Furniture(market, "int_fridge", o + new Vector2(10.9f, 6.5f), 1.6f, 0.5f, 0.25f, true);
        Prop(fr, MMPropKind.Talk, "Soyuducu", new[] { "Soyuducuda soyuq içkilər və ayran var.", "Aysel xala deyir: \"Yayın istisində ayrandan yaxşısı yoxdur!\"" }, S("int_fridge"), 2.6f);
        Furniture(market, "int_crates", o + new Vector2(1.8f, 6.5f), 2.0f, 0.5f, 0.25f, true);
        Furniture(market, "int_plant", o + new Vector2(0.9f, 0.6f), 0.8f, 0.4f, 0.2f, true);
        // satıcı Aysel xala piştaxtanın arxasında
        var pc = new Vector3(o.x + 7.0f, o.y + 5.9f, 0);
        var ay = new GameObject("Shopkeeper_Aysel"); ay.transform.SetParent(market.transform, false); ay.transform.position = pc;
        var body = MakeSprite(ay.transform, "Body", S("aysel_down_0"), pc, 0, true);
        AddShadow(ay);
        var shop = ay.AddComponent<MMShop>();
        shop.Body = body.GetComponent<SpriteRenderer>();
        shop.Down = Frames("aysel_down", 3); shop.Side = Frames("aysel_side", 3);
        shop.InteractOffset = new Vector2(0, -1.0f); shop.RadiusBonus = 0.6f;
        shop.Prompt = Child(ay, "Prompt", S("prompt_e"), 2.0f, 50, false);
        Doors(market, new Vector2(42.5f, H - 7.15f), o, "Marketə xoş gəldin! Aysel xala piştaxtanın arxasındadır.");

        // ---- Sara nənənin evində pişik balaları (tapşırıqdan sonra görünür) ----
        var kh = new GameObject("KittensHome"); kh.transform.SetParent(sara.transform, false);
        var khVis = new GameObject("Kittens"); khVis.transform.SetParent(kh.transform, false);
        var so = new Vector2(60, 2);
        MakeSprite(khVis.transform, "KittenWhite", S("kitten_white"), new Vector3(so.x + 0.9f, so.y + 1.3f, 0), 0, true);
        MakeSprite(khVis.transform, "KittenBlack", S("kitten_black"), new Vector3(so.x + 2.6f, so.y + 1.2f, 0), 0, true).GetComponent<SpriteRenderer>().flipX = true;
        MakeSprite(khVis.transform, "KittenGinger", S("kitten_ginger"), new Vector3(so.x + 3.2f, so.y + 2.2f, 0), 0, true);
        var khShow = kh.AddComponent<MMShowIf>(); khShow.Key = "q_kit_done"; khShow.Target = khVis;
        khVis.SetActive(false);

        // ---- Poçt ----
        var post = MakeRoom(root, "Room_Post", "room_post", new Vector2(100, 2), "Poçt", new Vector2(30.5f, H - 7.2f), warm);
        o = new Vector2(100, 2);
        Furniture(post, "int_postcounter", o + new Vector2(6f, 4.8f), 3.6f, 0.8f, 0.4f, true);
        var mail = Furniture(post, "int_mailshelf", o + new Vector2(2.4f, 6.5f), 2.6f, 0.5f, 0.25f, true);
        Prop(mail, MMPropKind.Talk, "Poçt qutuları", new[] { "Hər qonşunun öz qutusu var.", "Sara nənənin qutusunda nəvəsindən məktub var!" }, S("int_mailshelf"), 2.5f);
        Furniture(post, "int_parcels", o + new Vector2(10.4f, 1.2f), 1.6f, 0.5f, 0.25f, true);
        Furniture(post, "int_parcels", o + new Vector2(10.6f, 6.5f), 1.6f, 0.5f, 0.25f, true);
        Furniture(post, "int_plant", o + new Vector2(0.9f, 0.6f), 0.8f, 0.4f, 0.2f, true);
        var leyla = MakeNpc(post.transform, "leyla", "Poçt müdiri Leyla", MMNpcId.Leyla, new Vector3(o.x + 7.0f, o.y + 5.9f, 0));
        leyla.InteractOffset = new Vector2(0, -1.0f); leyla.RadiusBonus = 0.6f;
        MakeKitten(post.transform, "kit_white", "kitten_white", new Vector3(o.x + 9.3f, o.y + 1.0f, 0));
        Doors(post, new Vector2(30.5f, H - 7.15f), o, "Poçta xoş gəldin! Leyla xala piştaxtanın arxasındadır.");

        // ---- Çayxana ----
        var tea = MakeRoom(root, "Room_Teahouse", "room_teahouse", new Vector2(120, 2), "Çayxana", new Vector2(40.5f, H - 21.2f), warm);
        o = new Vector2(120, 2);
        Furniture(tea, "int_teacounter", o + new Vector2(6f, 4.9f), 3.4f, 0.8f, 0.4f, true);
        var reshad = MakeNpc(tea.transform, "reshad", "Çayxanaçı Rəşad", MMNpcId.Reshad, new Vector3(o.x + 7.2f, o.y + 6.0f, 0));
        reshad.InteractOffset = new Vector2(0, -1.0f); reshad.RadiusBonus = 0.6f;
        foreach (var tp in new[] { new Vector2(2.3f, 1.4f), new Vector2(9.7f, 1.4f), new Vector2(2.3f, 4.1f), new Vector2(9.9f, 4.1f) })
            Furniture(tea, "int_teatable", o + tp, 2.0f, 0.5f, 0.25f, true);
        Furniture(tea, "int_plant", o + new Vector2(0.9f, 6.6f), 0.8f, 0.4f, 0.2f, true);
        Furniture(tea, "int_plant", o + new Vector2(11.1f, 6.6f), 0.8f, 0.4f, 0.2f, true);
        Doors(tea, new Vector2(40.5f, H - 21.15f), o, "Çayxanaya xoş gəldin! Təzə dəmlənmiş çayın ətri gəlir.");

        // ---- Sənin evin (yaşayış binasında) ----
        var home = MakeRoom(root, "Room_Home", "room_home", new Vector2(140, 2), "Sənin evin", new Vector2(18.5f, H - 7.2f), warm);
        o = new Vector2(140, 2);
        Furniture(home, "int_carpet", o + new Vector2(6f, 2.4f), 0, 0, 0, false);
        var bedGo = Furniture(home, "int_bed", o + new Vector2(1.5f, 3.6f), 1.6f, 2.4f, 1.2f, true);
        var homeBed = bedGo.AddComponent<MMBed>();
        homeBed.InteractOffset = new Vector2(0.4f, 0.2f); homeBed.RadiusBonus = 0.7f;
        homeBed.Prompt = Child(bedGo, "Prompt", S("prompt_e"), 3.1f, 50, false);
        var desk = Furniture(home, "int_desk", o + new Vector2(8.6f, 6.1f), 1.9f, 0.5f, 0.25f, true);
        Prop(desk, MMPropKind.Talk, "Yazı masası", new[] { "Dərs kitabların və rəngli karandaşların.", "Dəftərdə yazılıb: \"Bu yay bağçada qarpız yetişdirəcəm!\"" }, S("int_desk"), 2.0f);
        Furniture(home, "int_wardrobe", o + new Vector2(10.9f, 6.0f), 1.5f, 0.5f, 0.25f, true);
        Furniture(home, "int_bookshelf", o + new Vector2(2.1f, 6.6f), 1.9f, 0.5f, 0.25f, true);
        Furniture(home, "int_plant", o + new Vector2(11.1f, 0.6f), 0.8f, 0.4f, 0.2f, true);
        Doors(home, new Vector2(18.5f, H - 7.15f), o, "Evinə xoş gəldin! Yatağında yatsan, səhər açılar.");

        // ---- Kitabxana (Məktəb məhəlləsində) ----
        var lib = MakeRoom(root, "Room_Library", "room_library", new Vector2(160, 2), "Kitabxana", new Vector2(Z2X + 34f, H - 7.2f), warm);
        o = new Vector2(160, 2);
        Furniture(lib, "int_libdesk", o + new Vector2(6f, 4.8f), 3.2f, 0.8f, 0.4f, true);
        var zehra = MakeNpc(lib.transform, "zehra", "Kitabxanaçı Zəhra xanım", MMNpcId.Zehra, new Vector3(o.x + 7.2f, o.y + 5.9f, 0));
        zehra.InteractOffset = new Vector2(0, -1.0f); zehra.RadiusBonus = 0.6f;
        var sh1 = Furniture(lib, "int_libshelf", o + new Vector2(1.3f, 6.4f), 1.9f, 0.5f, 0.25f, true);
        Prop(sh1, MMPropKind.Talk, "Kitab rəfləri", new[] { "Rəflərdə nağıllar, şeirlər, ensiklopediyalar...", "Bir kitabın adı: \"Cırtdan\". Bu nağılı hamı sevir!" }, S("int_libshelf"), 2.7f);
        Furniture(lib, "int_libshelf2", o + new Vector2(3.4f, 6.4f), 1.9f, 0.5f, 0.25f, true);
        var sh3 = Furniture(lib, "int_libshelf2", o + new Vector2(11.0f, 6.4f), 1.9f, 0.5f, 0.25f, true);
        Prop(sh3, MMPropKind.Talk, "Elm rəfi", new[] { "Kosmos, dinozavrlar, robotlar haqqında kitablar.", "Bir kitabda yazılıb: \"Süni intellekt insanlara kömək edən proqramdır.\"" }, S("int_libshelf2"), 2.7f);
        Furniture(lib, "int_readtable", o + new Vector2(2.6f, 2.0f), 2.1f, 0.5f, 0.25f, true);
        Furniture(lib, "int_readtable", o + new Vector2(9.4f, 2.0f), 2.1f, 0.5f, 0.25f, true);
        var globe = Furniture(lib, "int_globe", o + new Vector2(8.3f, 6.6f), 0.6f, 0.3f, 0.15f, true);
        Prop(globe, MMPropKind.Talk, "Qlobus", new[] { "Qlobusu fırlatdın...", "Budur, Azərbaycan! Xəzər dənizinin sahilində." }, S("int_globe"), 1.5f);
        Furniture(lib, "int_plant", o + new Vector2(0.9f, 0.6f), 0.8f, 0.4f, 0.2f, true);
        Furniture(lib, "int_plant", o + new Vector2(11.1f, 0.6f), 0.8f, 0.4f, 0.2f, true);
        Doors(lib, new Vector2(Z2X + 34f, H - 7.15f), o, "Kitabxanaya xoş gəldin! Burada sakit ol, hamı kitab oxuyur.");
    }

    // ---------------- qar adamı yerləri (qışda görünür) ----------------
    static void BuildSnowmen(Transform root)
    {
        var spots = new[] { new Vector2Int(8, 30), new Vector2Int(16, 30), new Vector2Int(21, 18) };
        for (int i = 0; i < spots.Length; i++)
        {
            var cell = spots[i];
            var pos = new Vector3(cell.x + 0.5f, H - 1 - cell.y + 0.1f, 0);
            var go = MakeSprite(root, "Snowman_" + i, S("snow_pile"), pos, 0, true);
            var sm = go.AddComponent<MMSnowman>();
            sm.Index = i; sm.Body = go.GetComponent<SpriteRenderer>(); sm.Pile = S("snow_pile"); sm.Stages = Frames("snowman", 4);
            sm.Body.enabled = false;
            sm.InteractOffset = new Vector2(0, 0.3f); sm.RadiusBonus = 0.3f;
            sm.Prompt = Child(go, "Prompt", S("prompt_e"), 2.3f, 50, false);
            for (int dc = -1; dc <= 1; dc++) for (int dr = -1; dr <= 1; dr++) reserved.Add(new Vector2Int(cell.x + dc, cell.y + dr));
        }
    }

    // ---------------- şənlik (ikinci hekayənin sonu) ----------------
    static void BuildFestival(Transform root, GameObject sys)
    {
        var fest = sys.AddComponent<MMFestival>();
        var fr = new GameObject("Festival").transform; fr.SetParent(root, false);
        var decor = new GameObject("Decor"); decor.transform.SetParent(fr, false);
        var center = new Vector3(12.5f, H - 1 - 22 + 0.8f, 0);
        // iki fənərin arasında bayraqcıqlar
        var bunt = MakeSprite(decor.transform, "Bunting", S("bunting"), new Vector3(12f, H - 1 - 18 + 1.75f, 0), 25, false);
        bunt.transform.localScale = new Vector3(0.82f, 1f, 1f);
        // fəvvarənin ətrafında rəngli fənərlər
        var cols = new[] { new Color(1f, 0.4f, 0.35f), new Color(1f, 0.85f, 0.3f), new Color(0.45f, 0.9f, 0.5f), new Color(0.45f, 0.65f, 1f), new Color(1f, 0.55f, 0.85f) };
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            var p = center + new Vector3(Mathf.Cos(a) * 3.6f, Mathf.Sin(a) * 2.4f + 1.3f, 0);
            var gl = MakeSprite(decor.transform, "Lantern", S("glow"), p, 25, false);
            gl.transform.localScale = Vector3.one * 0.45f;
            var gsr = gl.GetComponent<SpriteRenderer>(); gsr.color = cols[i % cols.Length];
            if (unlitMat != null) gsr.sharedMaterial = unlitMat;
        }
        for (int i = 0; i < 4; i++)
        {
            float a = (i + 0.5f) / 4f * Mathf.PI * 2f;
            var lgo = new GameObject("FestivalLight"); lgo.transform.SetParent(decor.transform, false);
            lgo.transform.position = center + new Vector3(Mathf.Cos(a) * 3f, Mathf.Sin(a) * 2f + 0.5f, 0);
            var l = lgo.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.pointLightOuterRadius = 3.2f; l.pointLightInnerRadius = 0.4f; l.falloffIntensity = 0.6f;
            l.color = cols[i]; l.intensity = 0.8f;
        }
        var show = fr.gameObject.AddComponent<MMShowIf>(); show.Key = "festival"; show.Target = decor;
        decor.SetActive(false);
        // qonşuların və oyunçunun yerləri
        var spots = new[] { new Vector2(9.6f, 13.2f), new Vector2(15.4f, 13.2f), new Vector2(10.2f, 11.1f), new Vector2(14.8f, 11.1f),
                            new Vector2(8.8f, 15.0f), new Vector2(16.2f, 15.0f), new Vector2(12.5f, 16.3f) };
        fest.Spots = new Transform[spots.Length];
        for (int i = 0; i < spots.Length; i++)
        {
            var s = new GameObject("Spot_" + i); s.transform.SetParent(fr, false); s.transform.position = spots[i];
            fest.Spots[i] = s.transform;
        }
        var ps = new GameObject("PlayerSpot"); ps.transform.SetParent(fr, false); ps.transform.position = new Vector3(12.5f, 10.4f, 0);
        fest.PlayerSpot = ps.transform;
        fest.Confetti = Frames("confetti", 5);
    }

    // payız yarpaqları və qış qarı üçün örtük plitələri (əvvəlcə görünməz)
    static void PaintSeasonOverlays(Tilemap leaves, Tilemap snow)
    {
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
            {
                char g = ground[r][c];
                if ("tbTB".IndexOf(g) >= 0) continue;
                var cell = Cell(c, r);
                snow.SetTile(cell, T("snow_" + (Hash(c, r) % 4)));
                if ((g == '.' || g == ',') && Hash(c * 3, r * 5) % 5 < 3) leaves.SetTile(cell, T("leaves_" + (Hash(c, r * 7) % 4)));
            }
        leaves.color = new Color(1, 1, 1, 0);
        snow.color = new Color(1, 1, 1, 0);
    }

    static MMNpc MakeNpc(Transform parent, string sprite, string display, MMNpcId id, Vector3 pc)
    {
        var go = new GameObject("NPC_" + sprite); go.transform.SetParent(parent, false); go.transform.position = pc;
        Box(go, 0.6f, 0.3f, 0.15f);
        var body = MakeSprite(go.transform, "Body", S(sprite + "_down_0"), pc, 0, true);
        AddShadow(go);
        var npc = go.AddComponent<MMNpc>();
        npc.Id = id; npc.DisplayName = display;
        npc.Body = body.GetComponent<SpriteRenderer>();
        npc.Down = Frames(sprite + "_down", 3); npc.Up = Frames(sprite + "_up", 3); npc.Side = Frames(sprite + "_side", 3);
        npc.Portrait = S(sprite + "_down_0");
        npc.Prompt = Child(go, "Prompt", S("prompt_e"), 2.0f, 50, false);
        npc.MarkerExcl = Child(go, "MarkerExcl", S("marker_excl"), 1.95f, 50, false);
        npc.MarkerQ = Child(go, "MarkerQ", S("marker_q"), 1.95f, 50, false);
        return npc;
    }

    static void MakeKitten(Transform parent, string id, string sprite, Vector3 pos)
    {
        var go = new GameObject("Kitten_" + id); go.transform.SetParent(parent, false); go.transform.position = pos;
        var body = MakeSprite(go.transform, "Body", S(sprite), pos, 0, true);
        AddShadow(go, 0.6f);
        var k = go.AddComponent<MMKitten>();
        k.Id = id; k.Body = body.GetComponent<SpriteRenderer>();
        k.InteractOffset = new Vector2(0, 0.25f); k.RadiusBonus = 0.25f;
        k.Prompt = Child(go, "Prompt", S("prompt_e"), 0.95f, 50, false);
    }

    static MMRoom MakeRoom(Transform parent, string name, string bg, Vector2 origin, string title, Vector2 mapPos, Color light)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = origin;
        var room = go.AddComponent<MMRoom>();
        room.Id = name; room.Title = title;
        room.Bounds = new Rect(origin.x, origin.y - 0.6f, 12f, 9.6f);
        room.MapPosition = mapPos;
        var b = MakeSprite(go.transform, "Background", S(bg), new Vector3(origin.x + 6f, origin.y, 0), -25, false);
        // divarlar: yuxarı, yanlar, aşağıda qapı boşluğu ilə
        var e = go.AddComponent<EdgeCollider2D>();
        e.points = new[] { new Vector2(5.4f, 0), new Vector2(0, 0), new Vector2(0, 7.1f), new Vector2(12, 7.1f), new Vector2(12, 0), new Vector2(6.6f, 0),
                           new Vector2(6.6f, -0.6f), new Vector2(5.4f, -0.6f), new Vector2(5.4f, 0) };
        // otaq işığı: gecə içəri isti və işıqlı olsun
        var lgo = new GameObject("RoomLight"); lgo.transform.SetParent(go.transform, false); lgo.transform.localPosition = new Vector3(6f, 4.5f, 0);
        var l = lgo.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.pointLightOuterRadius = 9f; l.pointLightInnerRadius = 3f; l.falloffIntensity = 0.4f; l.color = light; l.intensity = 0f;
        var nl = lgo.AddComponent<MMNightLight>(); nl.Light = l; nl.MaxIntensity = 0.85f; nl.Threshold = 0f;
        return room;
    }

    static GameObject Furniture(MMRoom room, string sprite, Vector2 pos, float cw, float ch, float coy, bool ySort)
    {
        var go = MakeSprite(room.transform, sprite.Replace("int_", "Furniture_"), S(sprite), pos, ySort ? 0 : -24, ySort);
        if (cw > 0) Box(go, cw, ch, coy);
        return go;
    }

    static MMProp Prop(GameObject go, MMPropKind kind, string title, string[] lines, Sprite portrait, float promptY)
    {
        var p = go.AddComponent<MMProp>();
        p.Kind = kind; p.Title = title; p.Lines = lines; p.Portrait = portrait;
        p.InteractOffset = new Vector2(0, 0.2f); p.RadiusBonus = 0.4f;
        p.Prompt = Child(go, "Prompt", S("prompt_e"), promptY, 50, false);
        return p;
    }

    // küçədəki qapı (içəri) + otaqdakı çıxış (bayıra)
    static void Doors(MMRoom room, Vector2 outsideDoor, Vector2 o, string enterMsg)
    {
        var dIn = new GameObject("Door_Enter_" + room.Id);
        dIn.transform.SetParent(room.transform.parent, false);
        dIn.transform.position = outsideDoor;
        var c1 = dIn.AddComponent<BoxCollider2D>(); c1.isTrigger = true; c1.size = new Vector2(1.0f, 0.5f);
        var d1 = dIn.AddComponent<MMDoor>();
        d1.Target = o + new Vector2(6f, 0.7f); d1.EnterDirection = Vector2.up; d1.FaceAfter = Vector2.up; d1.TargetRoom = room; d1.Message = enterMsg;

        var dOut = new GameObject("Door_Exit_" + room.Id);
        dOut.transform.SetParent(room.transform, false);
        dOut.transform.position = o + new Vector2(6f, -0.3f);
        var c2 = dOut.AddComponent<BoxCollider2D>(); c2.isTrigger = true; c2.size = new Vector2(1.1f, 0.5f);
        var d2 = dOut.AddComponent<MMDoor>();
        d2.Target = outsideDoor + new Vector2(0, -0.75f); d2.EnterDirection = Vector2.down; d2.FaceAfter = Vector2.down; d2.TargetRoom = null;
    }

    // ---------------- piyadalar ----------------
    static string[] BuildWalkGrid()
    {
        var rows = new string[H];
        for (int r = 0; r < H; r++)
        {
            var sb = new System.Text.StringBuilder(W);
            for (int c = 0; c < W; c++)
            {
                char ch = ground[r][c];
                char o = ch == 's' ? 's' : ch == 'p' ? 'p' : (ch == 'T' || ch == 'B') ? 'x' : '.';
                if (blocked.Contains(new Vector2Int(c, r))) o = '.';
                sb.Append(o);
            }
            rows[r] = sb.ToString();
        }
        return rows;
    }

    static void SpawnWalkers(Transform root, string[] grid)
    {
        var specs = new[] { ("ped_a", 1.5f, false, false), ("ped_b", 1.3f, false, true), ("ped_c", 1.8f, false, false),
                            ("ped_d", 0.9f, false, false), ("ped_e", 2.3f, true, false), ("ped_a", 1.4f, true, false) };
        var rnd = new System.Random(7);
        var cells = new List<Vector2Int>();
        for (int r = 0; r < H; r++) for (int c = 0; c < W; c++) if (grid[r][c] == 's' || grid[r][c] == 'p') cells.Add(new Vector2Int(c, r));
        for (int i = 0; i < specs.Length; i++)
        {
            var (name, speed, owl, dog) = specs[i];
            var cell = cells[rnd.Next(cells.Count)];
            var pos = new Vector3(cell.x + 0.5f, H - 1 - cell.y + 0.3f, 0);
            var go = new GameObject("Walker_" + name); go.transform.SetParent(root, false); go.transform.position = pos;
            var body = MakeSprite(go.transform, "Body", S(name + "_down_0"), pos, 0, true);
            AddShadow(go);
            go.AddComponent<SortingGroup>();
            var umbrellaColors = new[] { new Color(0.35f, 0.55f, 0.95f), new Color(0.98f, 0.82f, 0.3f), new Color(0.45f, 0.75f, 0.4f),
                                         new Color(0.7f, 0.45f, 0.85f), new Color(0.95f, 0.55f, 0.7f), new Color(0.3f, 0.75f, 0.75f) };
            var w = go.AddComponent<MMWalker>();
            w.Body = body.GetComponent<SpriteRenderer>();
            w.Umbrella = AddUmbrella(go, umbrellaColors[i % umbrellaColors.Length]);
            w.Down = Frames(name + "_down", 3); w.Up = Frames(name + "_up", 3); w.Side = Frames(name + "_side", 3);
            w.Speed = speed; w.NightOwl = owl; w.Seed = 100 + i * 17;
            if (dog)
            {
                var d = MakeSprite(root, "Dog", S("dog_idle_0"), pos + new Vector3(-0.8f, 0, 0), 0, true);
                AddShadow(d, 0.9f);
                var f = d.AddComponent<MMFollower>();
                f.Owner = w; f.Body = d.GetComponent<SpriteRenderer>();
                f.Idle = Frames("dog_idle", 2); f.Walk = Frames("dog_walk", 2);
            }
        }
    }

    static void MakeLanes(Transform parent)
    {
        var lanes = new GameObject("RoadLanes").transform;
        lanes.SetParent(parent, false);
        var cars = new[] { S("car_red"), S("car_blue"), S("car_white"), S("car_taxi"), S("car_green"), S("car_yellow"), S("car_taxi") };
        for (int r = 0; r < H; r++)
        {
            bool top = ground[r].IndexOf('t') >= 0, bottom = ground[r].IndexOf('b') >= 0;
            if (!top && !bottom) continue;
            var go = new GameObject(top ? "Lane_West" : "Lane_East");
            go.transform.SetParent(lanes, false);
            go.transform.position = new Vector3(0, H - 1 - r, 0);
            var lane = go.AddComponent<MMLane>();
            lane.Dir = top ? -1 : 1;
            lane.StartX = top ? W + 3 : -3;
            lane.EndX = top ? -3 : W + 3;
            lane.CarSprites = cars;
            lane.Shadow = S("shadow");
            lane.HeadlightSprite = S("car_light");
            lane.UnlitMaterial = unlitMat;
        }
    }

    // xəritənin sərhədi; gapRight=true: sağda, false: solda y0..y1 arasında keçid boşluğu
    static void MakeBounds(Transform parent, bool gapRight, float y0, float y1)
    {
        var go = new GameObject("Bounds");
        go.transform.SetParent(parent, false);
        var e1 = go.AddComponent<EdgeCollider2D>();
        var e2 = go.AddComponent<EdgeCollider2D>();
        var e3 = go.AddComponent<EdgeCollider2D>();
        if (gapRight)
        {
            e1.points = new[] { new Vector2(W, y0), new Vector2(W, 0), new Vector2(0, 0), new Vector2(0, H), new Vector2(W, H), new Vector2(W, y1) };
            e2.points = new[] { new Vector2(W, y0), new Vector2(W + 0.6f, y0), new Vector2(W + 0.6f, y1), new Vector2(W, y1) };
        }
        else
        {
            e1.points = new[] { new Vector2(0, y0), new Vector2(0, 0), new Vector2(W, 0), new Vector2(W, H), new Vector2(0, H), new Vector2(0, y1) };
            e2.points = new[] { new Vector2(0, y0), new Vector2(-0.6f, y0), new Vector2(-0.6f, y1), new Vector2(0, y1) };
        }
        Object.DestroyImmediate(e3);
    }

    // başqa xəritəyə keçid (oyunçu kənara doğru yeriyəndə)
    static void ZoneDoor(Transform parent, Vector2 pos, Vector2 dir, Vector2 target, MMZone zone, string msg)
    {
        var go = new GameObject("ZoneExit_" + zone.Id);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(0.5f, 3f);
        var d = go.AddComponent<MMDoor>();
        d.Target = target; d.EnterDirection = dir; d.FaceAfter = dir; d.TargetRoom = null; d.TargetZone = zone; d.Message = msg;
    }

    // ---------------- ikinci xəritə: Məktəb məhəlləsi ----------------
    // Əsas xəritənin funksiyaları ilə qurulur (qlobal massivlər müvəqqəti dəyişir), sonra Z2X qədər sağa sürüşdürülür.
    static GameObject BuildZone2(GameObject sys, MMZone zoneMain, out Tilemap[] tinted, out Tilemap leaves, out Tilemap snow)
    {
        var saveG = ground; var saveO = objects; var saveLabels = mapLabels; var saveBlocked = blocked; var saveReserved = reserved;
        ground = MMLevelData.Ground2; objects = MMLevelData.Objects2;
        mapLabels2 = new List<KeyValuePair<string, Vector2>>();
        mapLabels = mapLabels2; blocked = new HashSet<Vector2Int>(); reserved = new HashSet<Vector2Int>();
        idPrefix = "z2_";

        var root = new GameObject("MM_Zone2");
        var grid = new GameObject("Grid"); grid.transform.SetParent(root.transform, false);
        grid.AddComponent<Grid>();
        var maps = new Dictionary<string, Tilemap>();
        int order = -30;
        foreach (var n in new[] { "Ground", "Road", "EdgeN", "EdgeE", "EdgeS", "EdgeW", "CornerNW", "CornerNE", "CornerSW", "CornerSE", "Curbs", "Shadows" })
            maps[n] = MakeTilemap(grid, n, order++, false);
        maps["Hedges"] = MakeTilemap(grid, "Hedges", order++, true);
        PaintGround(maps);
        leaves = MakeTilemap(grid, "SeasonLeaves", -17, false);
        snow = MakeTilemap(grid, "SeasonSnow", -17, false);
        PaintSeasonOverlays(leaves, snow);
        tinted = new[] { maps["Ground"], maps["EdgeN"], maps["EdgeE"], maps["EdgeS"], maps["EdgeW"], maps["CornerNW"], maps["CornerNE"], maps["CornerSW"], maps["CornerSE"] };

        var objRoot = new GameObject("Objects").transform;
        objRoot.SetParent(root.transform, false);
        PlaceBuildings(objRoot);
        MMPlayer none = null;
        PlaceObjects(objRoot, ref none);
        BuildPitch(objRoot);
        BuildFarm(objRoot, sys);
        MakeBounds(root.transform, false, 12f, 15f);
        ZoneDoor(root.transform, new Vector2(0.25f, 13.5f), Vector2.left, new Vector2(W - 1.1f, 13.3f), zoneMain, "Böyük məhəlləyə qayıtdın.");
        mapLabels2.Add(new KeyValuePair<string, Vector2>("Məktəb həyəti", new Vector2(22.5f, H - 1.2f)));
        mapLabels2.Add(new KeyValuePair<string, Vector2>("« Məhəllə", new Vector2(4.0f, H - 20.6f)));

        // hamısını sağa sürüşdür
        root.transform.position = new Vector3(Z2X, 0, 0);

        ground = saveG; objects = saveO; mapLabels = saveLabels; blocked = saveBlocked; reserved = saveReserved;
        idPrefix = "";
        return root;
    }

    // futbol meydançası (sütun 2-17, sətir 11-19), iki qapı və top
    static void BuildPitch(Transform root)
    {
        var pr = new GameObject("Football").transform; pr.SetParent(root, false);
        float x0 = 2f, x1 = 18f, yB = H - 1 - 19, yT = H - 1 - 11 + 1;
        float cx = (x0 + x1) / 2f, cy = (yB + yT) / 2f;
        MakeSprite(pr, "Pitch", S("pitch"), new Vector3(cx, yB, 0), -16, false);
        var center = new GameObject("PitchCenter"); center.transform.SetParent(pr, false); center.transform.position = new Vector3(cx, cy - 0.2f, 0);
        for (int side = 0; side < 2; side++)
        {
            bool left = side == 0;
            var gpos = new Vector3(left ? x0 + 0.6f : x1 - 0.6f, cy - 1.375f, 0);
            var go = MakeSprite(pr, left ? "GoalLeft" : "GoalRight", S("goal"), gpos, 0, true);
            go.GetComponent<SpriteRenderer>().flipX = !left;
            float back = left ? -0.5f : 0.5f, front = left ? 0.55f : -0.55f;
            var e = go.AddComponent<EdgeCollider2D>();
            e.points = new[] { new Vector2(front, 2.55f), new Vector2(back, 2.55f), new Vector2(back, 0.2f), new Vector2(front, 0.2f) };
            var goal = go.AddComponent<MMGoal>();
            goal.Mouth = left ? new Rect(-0.5f, 0.25f, 1.0f, 2.25f) : new Rect(-0.5f, 0.25f, 1.0f, 2.25f);
            goal.Center = center.transform;
        }
        // meydançadakı top
        var ball = MakeSprite(pr, "PitchBall", S("ball"), center.transform.position, 0, true);
        AddShadow(ball, 0.6f, 0.8f);
        var rb = ball.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0; rb.linearDamping = 1.4f; rb.freezeRotation = true; rb.mass = 0.2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = ball.AddComponent<CircleCollider2D>(); col.radius = 0.18f; col.offset = new Vector2(0, 0.18f);
        var mb = ball.AddComponent<MMBall>();
        mb.InteractOffset = new Vector2(0, 0.2f);
        mb.Prompt = Child(ball, "Prompt", S("prompt_e"), 0.9f, 50, false);
        mapLabels.Add(new KeyValuePair<string, Vector2>("Futbol meydançası", new Vector2(cx, cy + 0.2f)));
    }

    // tarla: 'q' xanaları bellənir; ətrafında hasar, qarşısında Tural, toxum piştaxtası, satış qutusu və kran
    static void BuildFarm(Transform root, GameObject sys)
    {
        var fr = new GameObject("Farm").transform; fr.SetParent(root, false);
        var cells = new List<Vector2Int>();
        int cMin = W, cMax = 0, rMin = H, rMax = 0;
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                if (ground[r][c] == 'q')
                {
                    cells.Add(new Vector2Int(Mathf.RoundToInt(Z2X) + c, H - 1 - r));
                    cMin = Mathf.Min(cMin, c); cMax = Mathf.Max(cMax, c); rMin = Mathf.Min(rMin, r); rMax = Mathf.Max(rMax, r);
                }
        var gdn = sys.AddComponent<MMGarden>();
        gdn.SoilDry = S("soil"); gdn.SoilWet = S("soil_wet");
        gdn.Tomato = Frames("tomato", 4); gdn.Carrot = Frames("carrot", 4); gdn.Melon = Frames("melon", 4);
        gdn.PromptSprite = S("prompt_e"); gdn.CursorSprite = S("tile_cursor"); gdn.Dust = Frames("dust", 2);
        gdn.Tillable = cells.ToArray();
        var plots = new GameObject("Plots"); plots.transform.SetParent(fr, false);
        gdn.PlotRoot = plots.transform;

        // hasar: yuxarıda ortada giriş boşluğu
        int fc0 = cMin - 1, fc1 = cMax + 1, fr0 = rMin - 1, fr1 = rMax + 1;
        int gate0 = (fc0 + fc1) / 2 - 1, gate1 = gate0 + 1;
        var fence = new GameObject("Fence").transform; fence.SetParent(fr, false);
        for (int c = fc0; c <= fc1; c++)
            foreach (int r in new[] { fr0, fr1 })
            {
                if (r == fr0 && c >= gate0 && c <= gate1) continue;
                var f = MakeSprite(fence, "FenceH", S("fence_h"), new Vector3(c + 0.5f, H - 1 - r + 0.1f, 0), 0, true);
                Box(f, 1f, 0.25f, 0.15f);
            }
        for (int r = fr0 + 1; r < fr1; r++)
            foreach (int c in new[] { fc0, fc1 })
            {
                var f = MakeSprite(fence, "FenceV", S("fence_v"), new Vector3(c + 0.5f, H - 1 - r, 0), 0, true);
                Box(f, 0.3f, 1f, 0.5f);
            }
        // tarlanın qarşısı (hasarın üstündəki sətir)
        float yFront = H - 1 - (fr0 - 1) + 0.1f;
        var box = MakeSprite(fr, "SellBox", S("sell_box"), new Vector3(fc0 + 2.5f, yFront, 0), 0, true);
        Box(box, 1.3f, 0.5f, 0.25f); AddShadow(box, 1.4f);
        var sb = box.AddComponent<MMSellBox>();
        sb.InteractOffset = new Vector2(0, 0.3f); sb.RadiusBonus = 0.4f;
        sb.Prompt = Child(box, "Prompt", S("prompt_e"), 1.8f, 50, false);
        var tap = MakeSprite(fr, "WaterTap", S("water_tap"), new Vector3(gate0 - 1.2f, yFront - 0.1f, 0), 0, true);
        Box(tap, 0.6f, 0.3f, 0.15f); AddShadow(tap, 0.8f);
        gdn.Tap = tap.transform;
        MakeNpc(fr, "tural", "Bağban Tural", MMNpcId.Tural, new Vector3(gate1 + 2.0f, yFront + 0.3f, 0));
        var stand = MakeSprite(fr, "SeedStand", S("seed_stand"), new Vector3(gate1 + 4.6f, yFront, 0), 0, true);
        Box(stand, 1.9f, 0.45f, 0.25f); AddShadow(stand, 2.2f);
        var shop = stand.AddComponent<MMShop>();
        shop.Kind = "seeds"; shop.Title = "TURALIN TOXUMLARI"; shop.DisplayName = "Tural";
        shop.InteractOffset = new Vector2(0, 0.3f); shop.RadiusBonus = 0.5f;
        shop.Prompt = Child(stand, "Prompt", S("prompt_e"), 2.0f, 50, false);
        mapLabels.Add(new KeyValuePair<string, Vector2>("Tarla", new Vector2((cMin + cMax + 1) / 2f, H - (rMin + rMax + 1) / 2f)));
    }

    static Camera CreateMapCamera(Transform parent)
    {
        string rtPath = Root + "/MapRT.renderTexture";
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
        if (rt == null)
        {
            rt = new RenderTexture(W * 16, H * 16, 16);
            rt.filterMode = FilterMode.Point;
            AssetDatabase.CreateAsset(rt, rtPath);
        }
        else if (rt.width != W * 16 || rt.height != H * 16)
        {
            rt.Release(); rt.width = W * 16; rt.height = H * 16; EditorUtility.SetDirty(rt);
        }
        var go = new GameObject("MapCamera");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(W / 2f, H / 2f, -10f);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = H / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(27, 27, 42, 255);
        cam.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
        cam.targetTexture = rt;
        cam.enabled = false;
        return cam;
    }

    static void SetupCamera(MMPlayer player)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var cgo = new GameObject("Main Camera"); cgo.tag = "MainCamera";
            cam = cgo.AddComponent<Camera>(); cgo.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 5.625f;
        cam.backgroundColor = new Color32(27, 27, 42, 255);
        cam.clearFlags = CameraClearFlags.SolidColor;
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cam.gameObject);
        var ppc = cam.GetComponent<PixelPerfectCamera>();
        if (ppc != null) Object.DestroyImmediate(ppc);
        var follow = cam.GetComponent<MMCameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<MMCameraFollow>();
        follow.Target = player != null ? player.transform : null;
        follow.MapMin = Vector2.zero; follow.MapMax = new Vector2(W, H);
        var audio = cam.GetComponent<MMAudio>();
        if (audio == null) audio = cam.gameObject.AddComponent<MMAudio>();
        audio.MusicDay = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/music_day.wav");
        audio.MusicNight = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/music_night.wav");
        audio.RainClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/rain_loop.wav");
        audio.ThunderClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/thunder.wav");
        if (player != null) cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10);
        EditorUtility.SetDirty(cam.gameObject);
    }

    // ---------------- çanta ----------------
    static void SetupInventory(GameObject sys)
    {
        var inv = sys.AddComponent<MMInventory>();
        var names = new List<string>(); var sps = new List<Sprite>();
        foreach (var def in MMItems.All.Values)
            if (!names.Contains(def.Icon)) { names.Add(def.Icon); sps.Add(S(def.Icon)); }
        inv.IconNames = names.ToArray(); inv.IconSprites = sps.ToArray();
        inv.BallSprite = S("ball"); inv.CrumbsSprite = S("crumbs"); inv.DropsSprite = S("water_drops");
        inv.ShadowSprite = S("shadow"); inv.PromptSprite = S("prompt_e");
        // fənərin işığı (oyunçunun qabağına düşür)
        var fl = new GameObject("Flashlight");
        fl.transform.SetParent(sys.transform, false);
        var l = fl.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.pointLightOuterRadius = 3.6f; l.pointLightInnerRadius = 0.6f; l.falloffIntensity = 0.6f;
        l.color = new Color(1f, 0.95f, 0.8f); l.intensity = 0f; l.enabled = false;
        inv.Flashlight = l;
        inv.PlayerUmbrella = playerUmbrella;
    }

    // hava: yağış sistemi və gölməçələr
    static void SetupWeather(GameObject sys, Transform root)
    {
        var wt = sys.AddComponent<MMWeather>();
        wt.DropSprite = S("raindrop");
        wt.SplashFrames = Frames("rain_splash", 2);
        wt.RippleFrames = Frames("ripple", 3);
        wt.SnowSprite = S("snowflake");
        var parent = new GameObject("Puddles").transform;
        parent.SetParent(root, false);
        var rnd = new System.Random(42);
        var cands = new List<Vector2Int>();
        for (int r = 1; r < H - 1; r++)
            for (int c = 1; c < W - 1; c++)
            {
                char g = ground[r][c];
                if (g != 's' && g != 'p' && g != '.' && g != ',') continue;
                if (objects[r][c] != '.' || blocked.Contains(new Vector2Int(c, r)) || reserved.Contains(new Vector2Int(c, r))) continue;
                // qonşu xanalarda da obyekt olmasın
                bool free = true;
                for (int dr = -1; dr <= 1 && free; dr++)
                    for (int dc = -1; dc <= 1 && free; dc++)
                        if (objects[r + dr][c + dc] != '.' || blocked.Contains(new Vector2Int(c + dc, r + dr))) free = false;
                if (free) cands.Add(new Vector2Int(c, r));
            }
        var chosen = new List<Vector2Int>();
        int paths = 0, grass = 0;
        for (int tries = 0; tries < 4000 && chosen.Count < 26 && cands.Count > 0; tries++)
        {
            var p = cands[rnd.Next(cands.Count)];
            bool isPath = ground[p.y][p.x] == 's' || ground[p.y][p.x] == 'p';
            if (isPath ? paths >= 18 : grass >= 8) continue;
            bool far = true;
            foreach (var q in chosen) if (Mathf.Abs(q.x - p.x) + Mathf.Abs(q.y - p.y) < 5) { far = false; break; }
            if (!far) continue;
            chosen.Add(p);
            if (isPath) paths++; else grass++;
        }
        var list = new List<SpriteRenderer>();
        for (int i = 0; i < chosen.Count; i++)
        {
            var p = chosen[i];
            var pos = new Vector3(p.x + 0.5f + (float)(rnd.NextDouble() - 0.5) * 0.4f, H - 1 - p.y + 0.25f, 0);
            var go = MakeSprite(parent, "Puddle", S(i % 3 == 0 ? "puddle_1" : "puddle_0"), pos, -16, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.flipX = rnd.Next(2) == 0;
            sr.color = new Color(1, 1, 1, 0); sr.enabled = false;
            var rip = MakeSprite(go.transform, "Ripple", S("ripple_0"), pos + new Vector3(0, 0.12f, 0), -15, false);
            rip.GetComponent<SpriteRenderer>().enabled = false;
            list.Add(sr);
        }
        wt.Puddles = list.ToArray();
    }

    static void EnsureEventSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null) es = new GameObject("EventSystem").AddComponent<EventSystem>();
        var old = es.GetComponent<StandaloneInputModule>();
        if (old != null) Object.DestroyImmediate(old);
        if (es.GetComponent<InputSystemUIInputModule>() == null) es.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    static MMSlotUI SlotUI(Transform parent, string name, int index, bool inBag, Vector2 aMin, Vector2 pivot, Vector2 pos, float size)
    {
        var bg = Img(parent, name, S("slot_9s"), aMin, aMin, pivot, pos, new Vector2(size, size), true);
        bg.raycastTarget = true;
        var slot = bg.gameObject.AddComponent<MMSlotUI>();
        slot.Index = index; slot.InBag = inBag; slot.Background = bg;
        slot.Icon = Img(bg.transform, "Icon", null, C, C, C, Vector2.zero, new Vector2(size * 0.66f, size * 0.66f));
        slot.Icon.enabled = false;
        slot.CountText = Txt(bg.transform, "Count", "", 19, Cream, BR, BR, BR, new Vector2(-5, 2), new Vector2(size, 24), TextAnchor.LowerRight, true, true);
        return slot;
    }

    static void BuildInventoryUI(MMUI ui, Transform canvas, GameObject hud)
    {
        ui.SlotNormal = S("slot_9s"); ui.SlotSelected = S("slot_sel_9s");
        const float sz = 62f, gap = 6f;
        float rowW = 7 * sz + 6 * gap;

        // aşağıdakı 7 xanalı əşya paneli
        var hb = Img(canvas, "Hotbar", S("panel_9s"), BC, BC, BC, new Vector2(0, 8), new Vector2(rowW + 30, sz + 28), true);
        ui.HotbarRoot = hb.gameObject;
        ui.HotbarSlots = new MMSlotUI[7];
        for (int i = 0; i < 7; i++)
        {
            ui.HotbarSlots[i] = SlotUI(hb.transform, "Slot" + (i + 1), i, false, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(15 + i * (sz + gap), 0), sz);
            Txt(ui.HotbarSlots[i].transform, "Key", (i + 1).ToString(), 16, Muted, TL, TL, TL, new Vector2(6, -2), new Vector2(20, 20), TextAnchor.UpperLeft, false, true);
        }
        ui.ItemNameText = Txt(canvas, "ItemName", "", 22, Cream, BC, BC, BC, new Vector2(0, sz + 44), new Vector2(500, 32), TextAnchor.MiddleCenter, true, true);

        // çanta pəncərəsi
        var bag = Overlay(canvas, "Bag", new Color(0.05f, 0.04f, 0.07f, 0.6f));
        var box = Img(bag.transform, "Box", S("dialog_9s"), C, C, C, Vector2.zero, new Vector2(rowW + 90, 470), true);
        box.transform.SetSiblingIndex(0);
        Txt(box.transform, "Title", "ÇANTA", 33, Brown, TC, TC, TC, new Vector2(0, -20), new Vector2(500, 44), TextAnchor.MiddleCenter, true, false);
        Txt(box.transform, "HotbarLabel", "Əşya paneli (1-7)", 19, Brown, TL, TL, TL, new Vector2(45, -66), new Vector2(300, 24), TextAnchor.MiddleLeft, false, false);
        ui.BagSlots = new MMSlotUI[MMInventory.Size];
        for (int i = 0; i < MMInventory.Size; i++)
        {
            int row = i / 7, col = i % 7;
            float y = -92 - row * (sz + gap) - (row > 0 ? 14 : 0);
            ui.BagSlots[i] = SlotUI(box.transform, "BagSlot" + i, i, true, TL, TL, new Vector2(45 + col * (sz + gap), y), sz);
        }
        ui.BagInfoName = Txt(box.transform, "InfoName", "", 22, Ink, TL, TL, TL, new Vector2(45, -330), new Vector2(rowW, 30), TextAnchor.MiddleLeft, true, false);
        ui.BagInfoDesc = Txt(box.transform, "InfoDesc", "", 22, Ink, TL, TL, TL, new Vector2(45, -362), new Vector2(rowW, 60), TextAnchor.UpperLeft, false, false);
        Txt(box.transform, "Hint", "Siçanla sürüklə: yerini dəyiş      [I] bağla", 19, Brown, BC, BC, BC, new Vector2(0, 20), new Vector2(rowW, 28), TextAnchor.MiddleCenter, true, false);
        ui.BagPanel = bag;
        bag.SetActive(false);

        // sürüklənən ikon və fotoaparat parıltısı
        ui.DragIcon = Img(canvas, "DragIcon", null, C, C, C, Vector2.zero, new Vector2(44, 44));
        ui.DragIcon.gameObject.SetActive(false);
        var flash = Overlay(canvas, "PhotoFlash", new Color(1, 1, 1, 0));
        ui.FlashImage = flash.GetComponent<Image>();
        flash.SetActive(false);
        var fade = Overlay(canvas, "DoorFade", new Color(0.03f, 0.02f, 0.05f, 0));
        ui.FadeImage = fade.GetComponent<Image>();
        fade.SetActive(false);
    }

    // ---------------- UI köməkçiləri ----------------
    static readonly Vector2 TL = new Vector2(0, 1), TR = new Vector2(1, 1), BL = new Vector2(0, 0), BR = new Vector2(1, 0),
                            C = new Vector2(0.5f, 0.5f), TC = new Vector2(0.5f, 1), BC = new Vector2(0.5f, 0);
    static readonly Color Cream = new Color32(255, 244, 220, 255), Gold = new Color32(255, 214, 92, 255),
                          Ink = new Color32(70, 42, 26, 255), Brown = new Color32(122, 74, 40, 255), Muted = new Color32(214, 200, 180, 255);

    static RectTransform R(GameObject go, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    // tam uzanan element: kənarlardan boşluqlarla
    static void Inset(RectTransform rt, float left, float right, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = C;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    // shadowCol: null = kölgəsiz (məs. kağız üzərində)
    static Text Txt(Transform parent, string name, string text, int size, Color col, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 sz, TextAnchor align, bool bold = false, bool shadow = true)
    {
        var go = new GameObject(name);
        R(go, parent, aMin, aMax, pivot, pos, sz);
        var t = go.AddComponent<Text>();
        t.font = bold ? fontBold : font; t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
        t.fontStyle = FontStyle.Normal;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        if (shadow)
        {
            var o = go.AddComponent<Shadow>(); o.effectColor = new Color(0.1f, 0.06f, 0.08f, 0.9f); o.effectDistance = new Vector2(2, -2);
        }
        return t;
    }

    static Image Img(Transform parent, string name, Sprite sp, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size, bool sliced = false)
    {
        var go = new GameObject(name);
        R(go, parent, aMin, aMax, pivot, pos, size);
        var im = go.AddComponent<Image>();
        im.sprite = sp;
        im.preserveAspect = !sliced;
        // 9-slice kənarları: 1 sprite pikseli = 3 UI vahidi
        if (sliced) { im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 2.0833f; }
        im.raycastTarget = false;
        return im;
    }

    static GameObject Overlay(Transform parent, string name, Color col)
    {
        var im = Img(parent, name, null, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero);
        im.color = col; im.preserveAspect = false;
        return im.gameObject;
    }

    // ---------------- Nailiyyətlər ----------------
    static void BuildAchievementsUI(GameObject uiRoot, Transform t)
    {
        var ach = uiRoot.AddComponent<MMAchievements>();
        ach.MedalSprite = S("medal"); ach.MedalLockedSprite = S("medal_locked");
        var table = MMAchievements.Table;
        ach.IconSprites = new Sprite[table.Length];
        for (int i = 0; i < table.Length; i++) ach.IconSprites[i] = S(table[i].Icon);

        // pəncərə: 4 sütun x 3 sətir
        var ao = Overlay(t, "Achievements", new Color(0.05f, 0.04f, 0.07f, 0.78f));
        ach.Panel = ao;
        var box = Img(ao.transform, "Box", S("dialog_9s"), C, C, C, Vector2.zero, new Vector2(1060, 802), true);
        ach.FitRoot = box.rectTransform;
        ach.HeaderText = Txt(box.transform, "Title", "Nailiyyətlər", 33, Brown, TC, TC, TC, new Vector2(0, -20), new Vector2(900, 44), TextAnchor.MiddleCenter, true, false);
        int n = table.Length;
        ach.Medals = new Image[n]; ach.Icons = new Image[n]; ach.Titles = new Text[n]; ach.Descs = new Text[n];
        const float cw = 240f, ch = 126f, gx = 12f, gy = 8f;
        float x0 = -(4 * cw + 3 * gx) / 2f;
        for (int i = 0; i < n; i++)
        {
            int col = i % 4, row = i / 4;
            var cell = Img(box.transform, "Cell" + (i + 1), S("slot_9s"), TC, TC, TL, new Vector2(x0 + col * (cw + gx), -74 - row * (ch + gy)), new Vector2(cw, ch), true);
            ach.Medals[i] = Img(cell.transform, "Medal", S("medal_locked"), TL, TL, TL, new Vector2(8, -10), new Vector2(58, 63));
            ach.Icons[i] = Img(ach.Medals[i].transform, "Icon", ach.IconSprites[i], C, C, C, new Vector2(0, 4), new Vector2(30, 30));
            ach.Titles[i] = Txt(cell.transform, "Title", table[i].Title, 17, Ink, TL, TL, TL, new Vector2(72, -12), new Vector2(162, 24), TextAnchor.UpperLeft, true, false);
            ach.Titles[i].horizontalOverflow = HorizontalWrapMode.Overflow;
            ach.Descs[i] = Txt(cell.transform, "Desc", "", 16, Ink, TL, TL, TL, new Vector2(72, -34), new Vector2(160, 90), TextAnchor.UpperLeft, false, false);
            ach.Descs[i].lineSpacing = 1.0f;
        }
        Txt(box.transform, "Hint", "[N] və ya [Esc]  bağla        Yeni oyunda nailiyyətlər qalır", 19, Brown, BC, BC, BC, new Vector2(0, 18), new Vector2(980, 28), TextAnchor.MiddleCenter, true, false);
        ao.SetActive(false);

        // yuxarıda çıxan bildiriş
        var pop = Img(t, "AchievementPopup", S("dialog_9s"), TC, TC, TC, new Vector2(0, 130), new Vector2(400, 92), true);
        ach.Popup = pop.rectTransform;
        var medal = Img(pop.transform, "Medal", S("medal"), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(60, 65));
        ach.PopupIcon = Img(medal.transform, "Icon", null, C, C, C, new Vector2(0, 4), new Vector2(32, 32));
        ach.PopupTitle = Txt(pop.transform, "Text", "", 22, Ink, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true, false);
        Inset(ach.PopupTitle.rectTransform, 92, 16, 10, 10);
        pop.gameObject.SetActive(false);
    }

    // ---------------- UI ----------------
    static MMUI BuildUI(Camera mapCam)
    {
        var go = new GameObject("MM_UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        var ui = go.AddComponent<MMUI>();
        var t = go.transform;

        // ---- HUD ----
        var hud = new GameObject("HUD"); R(hud, t, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero);
        ui.HudRoot = hud;
        var status = Img(hud.transform, "StatusPanel", S("panel_9s"), TL, TL, TL, new Vector2(12, -12), new Vector2(214, 108), true);
        ui.HeartFull = S("heart_full"); ui.HeartEmpty = S("heart_empty");
        ui.HeartImages = new Image[3];
        for (int i = 0; i < 3; i++)
            ui.HeartImages[i] = Img(status.transform, "Heart" + (i + 1), ui.HeartFull, TL, TL, TL, new Vector2(22 + i * 46, -18), new Vector2(42, 39));
        Img(status.transform, "CoinIcon", S("coin_icon"), TL, TL, TL, new Vector2(24, -62), new Vector2(34, 34));
        ui.CoinText = Txt(status.transform, "Coins", "0", 33, Cream, TL, TL, TL, new Vector2(66, -58), new Vector2(130, 42), TextAnchor.MiddleLeft, true);

        var items = new GameObject("Items"); R(items, hud.transform, TL, TL, TL, new Vector2(18, -126), new Vector2(400, 48));
        Image ItemIcon(string n, string sp)
        {
            var im = Img(items.transform, n, S(sp), TL, TL, TL, Vector2.zero, new Vector2(44, 44));
            im.gameObject.SetActive(false);
            return im;
        }
        ui.CatIcon = ItemIcon("CatIcon", "cat_icon");
        ui.TeaIcon = ItemIcon("TeaIcon", "tea_icon");
        ui.CharmIcon = ItemIcon("CharmIcon", "charm_icon");
        ui.ShoesIcon = ItemIcon("ShoesIcon", "shoes_icon");
        ui.SpeedIcon = ItemIcon("SpeedIcon", "icecream_icon");
        ui.SpeedText = Txt(ui.SpeedIcon.transform, "Seconds", "", 22, Cream, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(50, 30), TextAnchor.MiddleLeft, true);

        var clock = Img(hud.transform, "ClockPanel", S("panel_9s"), TR, TR, TR, new Vector2(-12, -12), new Vector2(196, 98), true);
        ui.ClockIcon = Img(clock.transform, "Icon", S("sun_icon"), TL, TL, TL, new Vector2(18, -12), new Vector2(40, 40));
        ui.SunSprite = S("sun_icon"); ui.MoonSprite = S("moon_icon"); ui.SnowSprite = S("season_3");
        ui.ClockText = Txt(clock.transform, "Time", "08:00", 33, Cream, TL, TL, TL, new Vector2(70, -10), new Vector2(110, 44), TextAnchor.MiddleLeft, true);
        // fəsil və gün
        ui.SeasonIcon = Img(clock.transform, "SeasonIcon", S("season_0"), TL, TL, TL, new Vector2(22, -58), new Vector2(28, 28));
        ui.SeasonText = Txt(clock.transform, "Season", "Yaz, 1-ci gün", 19, Cream, TL, TL, TL, new Vector2(58, -56), new Vector2(136, 32), TextAnchor.MiddleLeft, false, true);

        var qp = Img(hud.transform, "QuestPanel", S("panel_9s"), TR, TR, TR, new Vector2(-12, -116), new Vector2(420, 148), true);
        ui.QuestPanel = qp.gameObject;
        ui.QuestText = Txt(qp.transform, "QuestText", "", 22, Cream, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero, TextAnchor.UpperLeft, false, true);
        Inset(ui.QuestText.rectTransform, 22, 16, 16, 12);
        ui.QuestText.lineSpacing = 1.05f;
        Txt(hud.transform, "KeyHints", "[I] Çanta   [F] İşlət   [G] Qartopu   [Esc] Fasilə\n[J] Tapşırıqlar   [M] Xəritə   [N] Nailiyyətlər", 17, Cream, BL, BL, BL, new Vector2(16, 10), new Vector2(480, 50), TextAnchor.LowerLeft, false, true);

        var toast = Img(t, "ToastBox", S("dialog_9s"), TC, TC, TC, new Vector2(0, -96), new Vector2(600, 56), true);
        ui.ToastBox = toast.rectTransform;
        ui.RainSprite = S("rain_icon");
        ui.ToastText = Txt(toast.transform, "ToastText", "", 22, Ink, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true, false);
        ui.ToastText.horizontalOverflow = HorizontalWrapMode.Overflow;
        toast.gameObject.SetActive(false);

        BuildInventoryUI(ui, t, hud);

        // ---- Dialoq ----
        var dp = Img(t, "Dialogue", S("dialog_9s"), BL, BR, BC, new Vector2(0, 16), new Vector2(-160, 196), true);
        ui.DialoguePanel = dp.gameObject;
        var pf = Img(dp.transform, "PortraitFrame", S("slot_9s"), TL, TL, TL, new Vector2(22, -22), new Vector2(152, 152), true);
        ui.PortraitFrame = pf.gameObject;
        ui.DialoguePortrait = Img(pf.transform, "Portrait", null, C, C, C, new Vector2(0, -4), new Vector2(128, 128));
        var nameTag = Img(dp.transform, "NameTag", S("panel_9s"), TL, TL, BL, new Vector2(190, -4), new Vector2(320, 48), true);
        ui.DialogueName = Txt(nameTag.transform, "Name", "", 22, Gold, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter, true);
        ui.DialogueBody = Txt(dp.transform, "Body", "", 22, Ink, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero, TextAnchor.UpperLeft, false, false);
        Inset(ui.DialogueBody.rectTransform, 196, 34, 34, 44);
        ui.DialogueBody.lineSpacing = 1.15f;
        ui.DialogueHint = Txt(dp.transform, "Hint", "", 22, Brown, BR, BR, BR, new Vector2(-30, 16), new Vector2(240, 30), TextAnchor.LowerRight, true, false);
        dp.gameObject.SetActive(false);

        // ---- Xəritə ----
        var mapPanel = Overlay(t, "MapScreen", new Color(0.07f, 0.06f, 0.09f, 1f));
        var mapTitle = Txt(mapPanel.transform, "Title", "XƏRİTƏ", 44, Gold, TC, TC, TC, new Vector2(0, -8), new Vector2(900, 56), TextAnchor.MiddleCenter, true);
        var mapSize = new Vector2(W * 16, H * 16);
        var fitGo = new GameObject("Container");
        var fit = R(fitGo, mapPanel.transform, C, C, C, new Vector2(0, -6), mapSize + new Vector2(36, 36));
        Img(fitGo.transform, "Frame", S("panel_9s"), Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero, true);
        var rawGo = new GameObject("View");
        R(rawGo, fitGo.transform, C, C, C, Vector2.zero, mapSize);
        var raw = rawGo.AddComponent<RawImage>();
        raw.texture = mapCam.targetTexture;
        var labelSets = new List<GameObject>();
        foreach (var set in new[] { mapLabels, mapLabels2 ?? new List<KeyValuePair<string, Vector2>>() })
        {
            var lgo = new GameObject(labelSets.Count == 0 ? "Labels_main" : "Labels_school");
            R(lgo, rawGo.transform, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero);
            foreach (var kv in set)
            {
                var a = new Vector2(kv.Value.x / W, kv.Value.y / H);
                Txt(lgo.transform, "Label_" + kv.Key, kv.Key, 22, Cream, a, a, C, Vector2.zero, new Vector2(260, 30), TextAnchor.MiddleCenter, true);
            }
            labelSets.Add(lgo);
        }
        var you = Img(rawGo.transform, "You", S("map_you"), C, C, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(34, 30));
        var tmpl = Img(rawGo.transform, "MarkerTemplate", S("marker_excl"), C, C, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(30, 40));
        tmpl.gameObject.SetActive(false);
        Txt(mapPanel.transform, "Hint", "[M] bağla        !  yeni tapşırıq        ?  tapşırığı təhvil ver        sarı ox  sən", 22, Muted,
            BC, BC, BC, new Vector2(0, 8), new Vector2(1200, 32), TextAnchor.MiddleCenter);
        var map = mapPanel.AddComponent<MMMap>();
        map.MapCamera = mapCam; map.PlayerMarker = you.rectTransform; map.MarkerTemplate = tmpl; map.FitRoot = fit;
        map.ExclSprite = S("marker_excl"); map.QSprite = S("marker_q"); map.WorldSize = new Vector2(W, H);
        map.TitleText = mapTitle;
        map.LabelZoneIds = new[] { "main", "school" };
        map.LabelSets = labelSets.ToArray();
        ui.Map = map;
        mapPanel.SetActive(false);

        // ---- Tapşırıq jurnalı ----
        var jo = Overlay(t, "Journal", new Color(0.05f, 0.04f, 0.07f, 0.78f));
        var jp = Img(jo.transform, "Paper", S("dialog_9s"), C, C, C, Vector2.zero, new Vector2(1000, 640), true);
        Txt(jp.transform, "Title", "TAPŞIRIQ JURNALI", 33, Brown, TC, TC, TC, new Vector2(0, -22), new Vector2(900, 44), TextAnchor.MiddleCenter, true, false);
        ui.JournalText = Txt(jp.transform, "Text", "", 22, Ink, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero, TextAnchor.UpperLeft, false, false);
        Inset(ui.JournalText.rectTransform, 48, 48, 78, 56);
        ui.JournalText.lineSpacing = 1.05f;
        Txt(jp.transform, "Hint", "[J] və ya [E]  bağla", 22, Brown, BR, BR, BR, new Vector2(-34, 20), new Vector2(360, 30), TextAnchor.LowerRight, true, false);
        ui.JournalPanel = jo;
        jo.SetActive(false);

        // ---- Mağaza ----
        var so = Overlay(t, "Shop", new Color(0.05f, 0.04f, 0.07f, 0.6f));
        var box = Img(so.transform, "Box", S("dialog_9s"), C, C, C, Vector2.zero, new Vector2(760, 680), true);
        box.transform.SetSiblingIndex(0);
        ui.ShopTitle = Txt(box.transform, "Title", "MARKET", 33, Brown, TC, TC, TC, new Vector2(0, -20), new Vector2(700, 44), TextAnchor.MiddleCenter, true, false);
        ui.ShopCoins = Txt(box.transform, "Coins", "", 22, Ink, TR, TR, TR, new Vector2(-36, -28), new Vector2(240, 30), TextAnchor.MiddleRight, true, false);
        ui.RowNormal = S("slot_9s"); ui.RowSelected = S("slot_sel_9s");
        const int shopRows = 8;
        ui.ShopRows = new Image[shopRows]; ui.ShopIcons = new Image[shopRows]; ui.ShopNames = new Text[shopRows]; ui.ShopPrices = new Text[shopRows];
        for (int i = 0; i < shopRows; i++)
        {
            var row = Img(box.transform, "Row" + (i + 1), ui.RowNormal, TC, TC, TC, new Vector2(0, -68 - i * 54), new Vector2(680, 50), true);
            ui.ShopRows[i] = row;
            ui.ShopIcons[i] = Img(row.transform, "Icon", null, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(36, 36));
            ui.ShopNames[i] = Txt(row.transform, "Name", "", 22, Ink, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(78, 0), new Vector2(460, 34), TextAnchor.MiddleLeft, true, false);
            ui.ShopPrices[i] = Txt(row.transform, "Price", "", 22, Ink, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(140, 34), TextAnchor.MiddleRight, true, false);
        }
        ui.ShopDesc = Txt(box.transform, "Description", "", 22, Ink, TC, TC, TC, new Vector2(0, -506), new Vector2(660, 52), TextAnchor.UpperLeft, false, false);
        ui.ShopMessage = Txt(box.transform, "Message", "", 22, new Color32(150, 52, 40, 255), TC, TC, TC, new Vector2(0, -562), new Vector2(660, 52), TextAnchor.UpperLeft, false, false);
        Txt(box.transform, "Hint", "[W/S] seç      [E] al      [Esc] çıx", 22, Brown, BC, BC, BC, new Vector2(0, 22), new Vector2(660, 30), TextAnchor.MiddleCenter, true, false);
        ui.ShopPanel = so;
        so.SetActive(false);

        // ---- Fasilə menyusu (Esc) ----
        var po = Overlay(t, "Pause", new Color(0.05f, 0.04f, 0.07f, 0.65f));
        var pbox = Img(po.transform, "Box", S("dialog_9s"), C, C, C, Vector2.zero, new Vector2(640, 626), true);
        Txt(pbox.transform, "Title", "FASİLƏ", 33, Brown, TC, TC, TC, new Vector2(0, -22), new Vector2(560, 44), TextAnchor.MiddleCenter, true, false);
        var pm = go.AddComponent<MMPauseMenu>();
        pm.Panel = po; pm.RowNormal = S("slot_9s"); pm.RowSelected = S("slot_sel_9s");
        const int pauseRows = 8;
        pm.Rows = new Image[pauseRows]; pm.Labels = new Text[pauseRows];
        for (int i = 0; i < pauseRows; i++)
        {
            var row = Img(pbox.transform, "Row" + (i + 1), S("slot_9s"), TC, TC, TC, new Vector2(0, -72 - i * 62), new Vector2(560, 54), true);
            row.raycastTarget = true;
            var pr = row.gameObject.AddComponent<MMPauseRow>(); pr.Menu = pm; pr.Index = i;
            pm.Rows[i] = row;
            pm.Labels[i] = Txt(row.transform, "Label", "", 22, Ink, Vector2.zero, Vector2.one, C, Vector2.zero, Vector2.zero, TextAnchor.MiddleLeft, true, false);
            Inset(pm.Labels[i].rectTransform, 24, 16, 0, 0);
        }
        Txt(pbox.transform, "Hint", "[W/S] seç     [A/D] dəyiş     [E] təsdiq     [Esc] bağla", 19, Brown, BC, BC, BC, new Vector2(0, 20), new Vector2(580, 28), TextAnchor.MiddleCenter, true, false);
        po.SetActive(false);

        BuildAchievementsUI(go, t);

        // ---- Menyu ----
        var menu = Overlay(t, "Menu", new Color(0.05f, 0.05f, 0.1f, 0.55f));
        ui.MenuPanel = menu;
        var mbox = Img(menu.transform, "Box", S("panel_9s"), C, C, C, new Vector2(0, 10), new Vector2(900, 520), true);
        mbox.color = new Color(1, 1, 1, 0.95f);
        Txt(mbox.transform, "Title", "MƏHƏLLƏ MACƏRASI", 66, Gold, TC, TC, TC, new Vector2(0, -34), new Vector2(860, 84), TextAnchor.MiddleCenter, true);
        Txt(mbox.transform, "Subtitle", "Qonşulara kömək et, məhəllənin qəhrəmanı ol!", 22, Cream, TC, TC, TC, new Vector2(0, -118), new Vector2(860, 34), TextAnchor.MiddleCenter);
        Img(mbox.transform, "Hero", S("player_down_0"), C, C, C, new Vector2(-150, 6), new Vector2(80, 160));
        Img(mbox.transform, "Mestan", S("cat_idle_0"), C, C, C, new Vector2(-82, -30), new Vector2(80, 80));
        Img(mbox.transform, "Sara", S("sara_down_0"), C, C, C, new Vector2(40, 6), new Vector2(80, 160));
        Img(mbox.transform, "Aysel", S("aysel_down_0"), C, C, C, new Vector2(140, 6), new Vector2(80, 160));
        Txt(mbox.transform, "Controls", "Yerimək: W A S D    Danışmaq: E    İşlətmək: F    Çanta: I    Xəritə: M", 22, Muted, BC, BC, BC, new Vector2(0, 92), new Vector2(860, 32), TextAnchor.MiddleCenter);
        ui.MenuHint = Txt(mbox.transform, "Start", "[Enter] Oyuna başla", 33, Cream, BC, BC, BC, new Vector2(0, 34), new Vector2(860, 48), TextAnchor.MiddleCenter, true);

        // ---- Qələbə ----
        var win = Overlay(t, "Victory", new Color(0.05f, 0.09f, 0.07f, 0.8f));
        ui.WinPanel = win;
        ui.WinTitle = Txt(win.transform, "Title", "TƏBRİKLƏR!", 66, Gold, C, C, C, new Vector2(0, 130), new Vector2(1100, 90), TextAnchor.MiddleCenter, true);
        ui.WinText = Txt(win.transform, "Text", "Sən məhəllənin əsl qəhrəmanısan!\nMestan evdədir, məktublar çatdı, babalar çay içir.", 33, Cream, C, C, C, new Vector2(0, 26), new Vector2(1100, 100), TextAnchor.MiddleCenter);
        ui.WinStats = Txt(win.transform, "Stats", "", 33, new Color32(143, 224, 122, 255), C, C, C, new Vector2(0, -66), new Vector2(1100, 48), TextAnchor.MiddleCenter, true);
        ui.WinHint = Txt(win.transform, "Again", "Yenidən oynamaq üçün ENTER bas", 22, Muted, C, C, C, new Vector2(0, -150), new Vector2(1100, 34), TextAnchor.MiddleCenter);
        win.SetActive(false);

        // ---- Oyun bitdi ----
        var over = Overlay(t, "GameOver", new Color(0.12f, 0.05f, 0.06f, 0.82f));
        ui.GameOverPanel = over;
        Txt(over.transform, "Title", "AY-AY-AY!", 66, new Color32(255, 122, 107, 255), C, C, C, new Vector2(0, 100), new Vector2(1100, 90), TextAnchor.MiddleCenter, true);
        Txt(over.transform, "Text", "Maşınlar çox sürətlidir. Yolu zolaqlı keçiddən keç!", 33, Cream, C, C, C, new Vector2(0, 10), new Vector2(1100, 50), TextAnchor.MiddleCenter);
        ui.GameOverHint = Txt(over.transform, "Hint", "[Enter] Yenidən başla", 33, Gold, C, C, C, new Vector2(0, -80), new Vector2(1100, 48), TextAnchor.MiddleCenter, true);
        over.SetActive(false);

        return ui;
    }
}
