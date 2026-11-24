# Zəmin plitələri (16x16): ot, torpaq cığır, səki, asfalt, hasar kolu + kənar örtükləri.
from px import Img, rgb, mix, rng, darker, lighter
import pal as P

T = 16


class W(Img):
    """kənarları bir-birinə tikişsiz bağlanan plitə (koordinatlar dövri)"""
    def wpx(self, x, y, c):
        self.px(x % self.w, y % self.h, c)


def _tuft(t, x, y, g, r):
    # ot dəstəsi (Stardew kimi): tünd dib, ortada orta, uclarda parlaq yarpaqcıqlar
    shape = r.choice([
        ["..l.l..", ".lm.ml.", "..mdm..", "...d..."],
        [".l...l.", ".m.l.m.", "..mdm..", "..ddd.."],
        ["...l...", ".l.m.l.", ".mmdmm.", "..ddd.."],
    ])
    cols = {'d': g[2], 'm': g[3] if r.random() < 0.3 else mix(g[2], g[3], 0.5), 'l': g[4]}
    for j, row in enumerate(shape):
        for i, ch in enumerate(row):
            if ch != '.':
                t.wpx(x + i - 3, y + j - 3, cols[ch])


def grass(seed, flowers=0, dense=False):
    g = P.GRASS
    t = W(T, T)
    base = mix(g[3], g[4], 0.18)
    t.rect(0, 0, 15, 15, base)
    r = rng(seed)
    # yumşaq açıq ləkələr
    for _ in range(2):
        cx, cy = r.randrange(16), r.randrange(16)
        for dy in range(-2, 3):
            for dx in range(-3, 4):
                if dx * dx / 9 + dy * dy / 4 <= 1 and r.random() < 0.75:
                    t.wpx(cx + dx, cy + dy, mix(base, g[4], 0.4))
    # ot dəstələri (bir-birinə yaxın düşməsin deyə şəbəkə üzrə)
    cells = [(x, y) for y in range(0, 16, 6) for x in range(0, 16, 6)]
    r.shuffle(cells)
    for (cx, cy) in cells[: (6 if dense else 4)]:
        _tuft(t, cx + r.randrange(0, 5), cy + r.randrange(0, 5), g, r)
    # tək ot ucları
    for _ in range(4):
        x, y = r.randrange(16), r.randrange(16)
        t.wpx(x, y, g[4])
        t.wpx(x, y + 1, mix(base, g[2], 0.35))
    for _ in range(flowers):
        x, y = r.randrange(1, 15), r.randrange(2, 14)
        kind = r.choice(['w', 'y', 'p', 'b'])
        petal = {'w': '#f6f2ff', 'y': P.YELLOW[4], 'p': P.PINK[4], 'b': P.BLUE[4]}[kind]
        core = {'w': P.YELLOW[3], 'y': P.ORANGE[3], 'p': P.YELLOW[4], 'b': '#ffffff'}[kind]
        t.wpx(x, y + 1, g[2])
        t.wpx(x, y + 2, g[2])
        t.pts([((x - 1) % 16, y), ((x + 1) % 16, y), (x, (y - 1) % 16)], petal)
        t.wpx(x, y, core)
    return t


def dirt(seed):
    d = P.DIRT
    t = W(T, T)
    t.rect(0, 0, 15, 15, d[4])
    r = rng(seed)
    for _ in range(10):
        x, y = r.randrange(16), r.randrange(16)
        t.wpx(x, y, d[3])
        t.wpx(x + 1, y, d[3]) if r.random() < 0.5 else None
    # xırda daşlar: tünd alt, açıq üst
    for _ in range(4):
        x, y = r.randrange(16), r.randrange(16)
        t.wpx(x, y, d[5])
        t.wpx(x + 1, y, d[5])
        t.wpx(x, y + 1, d[2])
        t.wpx(x + 1, y + 1, d[2])
    for _ in range(5):
        t.wpx(r.randrange(16), r.randrange(16), d[5])
    return t


def pavers(seed, tone=0):
    """səki: Bakı küçələri kimi açıq daş plitələr, hər plitə öz işıq/kölgəsi ilə"""
    s = P.STONE
    base = s[5] if tone == 0 else s[4]
    t = W(T, T)
    t.rect(0, 0, 15, 15, s[3])                # aralıq (tikiş) rəngi
    r = rng(seed)
    rows = [(0, 7, 0), (8, 15, 4)]            # (y0, y1, x sürüşmə)
    for y0, y1, off in rows:
        for bx in range(0, 16, 8):
            x0 = bx + off
            cval = mix(base, s[6], r.random() * 0.25) if r.random() < 0.6 else mix(base, s[4], r.random() * 0.4)
            for y in range(y0, y1):
                for x in range(x0, x0 + 7):
                    t.wpx(x, y, cval)
            # yuxarı-sol işıq, aşağı-sağ kölgə
            for x in range(x0, x0 + 7):
                t.wpx(x, y0, lighter(cval, 0.35))
                t.wpx(x, y1 - 1, mix(cval, s[3], 0.45))
            for y in range(y0, y1):
                t.wpx(x0, y, mix(cval, s[6], 0.3))
            # xırda ləkələr
            for _ in range(2):
                t.wpx(x0 + r.randrange(1, 6), y0 + r.randrange(1, 6), mix(cval, s[3], 0.3))
    return t


def asphalt(seed, kind='plain'):
    a = P.ASPHALT
    t = W(T, T)
    t.rect(0, 0, 15, 15, a[2])
    r = rng(seed)
    for _ in range(26):
        t.wpx(r.randrange(16), r.randrange(16), a[3] if r.random() < 0.6 else a[1])
    for _ in range(4):
        t.wpx(r.randrange(16), r.randrange(16), a[4])
    if kind == 'top':
        # yuxarıda bordür kölgəsi, aşağıda ara xətt (kəsik-kəsik)
        t.hline(0, 15, 0, a[0])
        t.hline(0, 15, 1, mix(a[0], a[2], 0.5))
        for x in range(2, 10):
            t.wpx(x, 15, '#e8e2c8')
    elif kind == 'bottom':
        for x in range(2, 10):
            t.wpx(x, 0, '#e8e2c8')
        t.hline(0, 15, 15, a[1])
    return t


def crosswalk(seed, half):
    t = asphalt(seed, 'top' if half == 'top' else 'bottom')
    r = rng(seed + 9)
    # zolaqlar maşın istiqamətinə paraleldir (piyada onların üstündən keçir)
    ys = [(3, 5), (8, 10), (13, 15)] if half == 'top' else [(2, 4), (7, 9), (12, 13)]
    for y0, y1 in ys:
        for y in range(y0, y1 + 1):
            for x in range(16):
                c = '#ece6d6' if r.random() > 0.06 else '#a8a49c'
                if y == y0:
                    c = '#fbf8ee'
                t.px(x, y, c)
        for x in range(16):
            t.px(x, y1 + 1, mix('#3a3846', '#24222e', 0.5)) if y1 + 1 < 16 else None
    return t


def hedge(seed, front=False):
    """sərhəd kolu: iri yarpaq topaları, yuxarıdan işıq; aşağısı açıqdırsa ön üz"""
    L = P.LEAF
    t = W(T, T)
    r = rng(seed)
    t.rect(0, 0, 15, 15, L[2])
    centers = [(x + r.randrange(-1, 2), y + r.randrange(-1, 2)) for y in (2, 8, 14) for x in (0, 5, 10, 15)]
    centers += [(x + 2 + r.randrange(-1, 2), y + 3) for y in (2, 8) for x in (0, 5, 10)]
    for cx, cy in centers:
        rad = 3 if r.random() < 0.6 else 2.5
        for dy in range(-4, 5):
            for dx in range(-4, 5):
                d = (dx * dx + dy * dy) ** 0.5
                if d <= rad:
                    if dy < 0 and d > rad - 1.3:
                        col = L[5]
                    elif dy <= 0:
                        col = L[4]
                    elif d > rad - 1.1:
                        col = L[2]
                    else:
                        col = L[3]
                    t.wpx(cx + dx, cy + dy, col)
        t.wpx(cx - 1, cy - 2, L[6])
    if front:
        for y in range(11, 16):
            for x in range(16):
                k = (y - 11) / 5
                c = mix(L[2], L[0], k * 0.8)
                if (x * 3 + y * 5 + seed) % 7 == 0:
                    c = mix(c, L[3], 0.6)
                t.px(x, y, c)
        for x in range(16):
            if (x + seed) % 4 != 0:
                t.px(x, 11, L[3])
            t.px(x, 15, L[0])
    return t


# ---------- kənar örtükləri (şəffaf fon üzərində) ----------
def grass_edge(side, seed):
    """cığır/səki plitəsinin üstünə: o tərəfdə ot sərhədi (dişli kənar)"""
    g = P.GRASS
    t = Img(T, T)
    r = rng(seed)
    depth = [2 + (1 if r.random() < 0.5 else 0) for _ in range(16)]
    for i in range(16):
        if r.random() < 0.25:
            depth[i] += 1
    for i in range(16):
        dpt = depth[i]
        for k in range(dpt + 1):
            if side == 'n':
                x, y = i, k
            elif side == 's':
                x, y = i, 15 - k
            elif side == 'w':
                x, y = k, i
            else:
                x, y = 15 - k, i
            if k < dpt:
                c = g[3] if k < dpt - 1 else g[4]
                t.px(x, y, c)
            else:
                # torpağa düşən incə kölgə
                t.px(x, y, (40, 30, 40, 60))
    # ot uclarında parlaq piksellər
    for i in range(0, 16, 3):
        j = (i + r.randrange(2)) % 16
        if side == 'n':
            t.px(j, 0, g[4])
        elif side == 's':
            t.px(j, 15, g[2])
    return t


def grass_corner(corner, seed):
    """içəri künc: diaqonal qonşu ot olanda kiçik ot parçası"""
    g = P.GRASS
    t = Img(T, T)
    pts = [(0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (0, 2)]
    for x, y in pts:
        if corner == 'ne':
            x = 15 - x
        elif corner == 'sw':
            y = 15 - y
        elif corner == 'se':
            x, y = 15 - x, 15 - y
        t.px(x, y, g[3])
    return t


def curb(side):
    """səki ilə yol arasında daş bordür"""
    s = P.STONE
    t = Img(T, T)
    if side == 's':
        t.hline(0, 15, 13, s[6])
        t.hline(0, 15, 14, s[4])
        t.hline(0, 15, 15, s[2])
        for x in range(0, 16, 8):
            t.px(x, 14, s[3])
            t.px(x, 15, s[1])
    else:
        t.hline(0, 15, 0, s[2])
        t.hline(0, 15, 1, s[6])
        t.hline(0, 15, 2, s[4])
        for x in range(4, 16, 8):
            t.px(x, 1, s[4])
    return t


def ao_shadow(strength=70):
    """divar/kol altındakı yerə düşən kölgə (yuxarı kənarda)"""
    t = Img(T, T)
    for y in range(5):
        a = int(strength * (1 - y / 5) ** 1.5)
        for x in range(16):
            t.px(x, y, (30, 22, 50, a))
    return t
