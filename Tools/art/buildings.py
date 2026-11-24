# Binalar: hər bina xəritədəki ölçüsündə bütöv sprite kimi çəkilir.
# Hər funksiya (gündüz şəkli, gecə pəncərə işığı) qaytarır.
from px import Img, rgb, mix, rng, darker, lighter
import pal as P
from font import draw_text, text_width


class B:
    def __init__(self, w, h):
        self.img = Img(w, h)
        self.glow = Img(w, h)
        self.w, self.h = w, h


# ---------------- ümumi hissələr ----------------
def shingles(b, x0, y0, x1, y1, R, seed=0, row_h=5):
    """kirəmit dam: sıra-sıra yarımdairəvi kirəmitlər, yuxarı açıq, aşağı tünd"""
    im = b.img
    r = rng(seed)
    nrows = (y1 - y0 + 1 + row_h - 1) // row_h
    for k in range(nrows):
        ry = y0 + k * row_h
        t = k / max(1, nrows - 1)
        base = mix(R[4], R[3], t)
        dark = mix(R[2], R[1], t)
        hi = mix(R[5], R[4], t)
        off = 3 if k % 2 else 0
        for y in range(ry, min(ry + row_h, y1 + 1)):
            for x in range(x0, x1 + 1):
                im.px(x, y, base)
        # kirəmit aralığı və alt kölgə
        for x in range(x0, x1 + 1):
            u = (x - x0 + off) % 6
            yb = min(ry + row_h - 1, y1)
            im.px(x, yb, dark)
            if u == 0:
                for y in range(ry, yb):
                    im.px(x, y, mix(base, dark, 0.6))
            if u in (1, 2) and ry <= y1:
                im.px(x, ry, hi)
            if u in (2, 3, 4) and yb - 1 >= ry:
                im.px(x, yb - 1, mix(base, dark, 0.35))
        if r.random() < 0.6:
            xx = r.randrange(x0 + 2, x1 - 2)
            im.px(xx, ry + 1, R[5])


def siding(b, x0, y0, x1, y1, Cr, board=4):
    """taxta üzlük: üfüqi lövhələr"""
    im = b.img
    for y in range(y0, y1 + 1):
        k = (y - y0) % board
        c = Cr[4]
        if k == 0:
            c = Cr[5]
        elif k == board - 1:
            c = Cr[2]
        elif k == board - 2:
            c = Cr[3]
        im.hline(x0, x1, y, c)


def plaster(b, x0, y0, x1, y1, Cr, seed=1):
    im = b.img
    r = rng(seed)
    im.rect(x0, y0, x1, y1, Cr[4])
    for _ in range((x1 - x0) * (y1 - y0) // 14):
        im.px(r.randint(x0, x1), r.randint(y0, y1), mix(Cr[4], Cr[3], 0.5))
    for _ in range((x1 - x0) * (y1 - y0) // 30):
        im.px(r.randint(x0, x1), r.randint(y0, y1), mix(Cr[4], Cr[5], 0.6))


def stone_base(b, x0, x1, y0, y1):
    s = P.STONE
    im = b.img
    im.rect(x0, y0, x1, y1, s[3])
    im.hline(x0, x1, y0, s[5])
    im.hline(x0, x1, y1, s[1])
    for x in range(x0, x1 + 1, 7):
        im.vline(x, y0 + 1, y1 - 1, s[2])


def eave_shadow(b, x0, x1, y, depth=3):
    for k in range(depth):
        a = int(120 * (1 - k / depth))
        b.img.hline(x0, x1, y + k, (40, 24, 50, a))


def window(b, x, y, w, h, frame=None, curtains=None, box=None, shutters=None, glow=True, mullion=True):
    """pəncərə: çərçivə, şüşədə əks, istəyə görə pərdə, gül qutusu, qapaqlar"""
    im = b.img
    G = P.GLASS
    F = frame or P.WHITE
    im.rect(x, y, x + w - 1, y + h - 1, F[4])
    im.hline(x, x + w - 1, y, F[5])
    im.vline(x + w - 1, y, y + h - 1, F[2])
    im.hline(x, x + w - 1, y + h - 1, F[2])
    gx0, gy0, gx1, gy1 = x + 1, y + 1, x + w - 2, y + h - 2
    for yy in range(gy0, gy1 + 1):
        t = (yy - gy0) / max(1, gy1 - gy0)
        im.hline(gx0, gx1, yy, mix(G[2], G[4], t * 0.8))
    # diaqonal parıltı
    for k in range(-2, 2):
        for i in range(gx1 - gx0 + 1):
            xx, yy = gx0 + i, gy0 + i + k * 3 + 1
            if gy0 <= yy <= gy1 and k in (-1, 0):
                im.px(xx, yy, mix(G[5], G[6], 0.5) if k == 0 else G[4])
    im.hline(gx0, gx1, gy0, G[1])
    if curtains:
        cc = curtains
        for yy in range(gy0, gy1 + 1):
            im.px(gx0, yy, cc[3])
            im.px(gx1, yy, cc[2])
            if yy < gy0 + (gy1 - gy0) // 2:
                im.px(gx0 + 1, yy, cc[4])
                im.px(gx1 - 1, yy, cc[3])
        im.hline(gx0, gx1, gy0, cc[2])
    if mullion and w >= 9:
        mx = x + w // 2
        im.vline(mx, gy0, gy1, F[3])
    if mullion and h >= 9:
        im.hline(gx0, gx1, y + h // 2, F[3])
    # altlıq
    im.hline(x - 1, x + w, y + h, F[5])
    im.hline(x - 1, x + w, y + h + 1, (40, 24, 50, 90))
    if shutters:
        S = shutters
        for sx in (x - 3, x + w):
            im.rect(sx, y, sx + 2, y + h - 1, S[3])
            for yy in range(y + 1, y + h - 1, 2):
                im.hline(sx, sx + 2, yy, S[2])
            im.vline(sx, y, y + h - 1, S[4])
    if box:
        by = y + h + 1
        im.rect(x - 1, by, x + w, by + 2, P.WOOD[3])
        im.hline(x - 1, x + w, by + 2, P.WOOD[1])
        r = rng(x * 7 + y)
        for xx in range(x - 1, x + w + 1):
            im.px(xx, by - 1, P.LEAF[4] if r.random() < 0.6 else P.LEAF[3])
            if r.random() < 0.45:
                im.px(xx, by - 2, box[r.randrange(3, 5)])
            if r.random() < 0.3:
                im.px(xx, by - 1, box[4])
    if glow:
        g = b.glow
        for yy in range(gy0, gy1 + 1):
            t = (yy - gy0) / max(1, gy1 - gy0)
            for xx in range(gx0, gx1 + 1):
                c = mix(P.NIGHT_GLOW[3], P.NIGHT_GLOW[1], t)
                if mullion and w >= 9 and xx == x + w // 2:
                    continue
                if mullion and h >= 9 and yy == y + h // 2:
                    continue
                g.px(xx, yy, (c[0], c[1], c[2], 235))


def door(b, x, y, w, h, Wd=None, glass=False, frame=None):
    im = b.img
    Wd = Wd or P.WOOD
    F = frame or P.WHITE
    im.rect(x - 1, y - 1, x + w, y + h - 1, F[4])
    im.hline(x - 1, x + w, y - 1, F[5])
    im.vline(x + w, y - 1, y + h - 1, F[2])
    if glass:
        G = P.GLASS
        im.rect(x, y, x + w - 1, y + h - 1, P.METAL[3])
        im.rect(x + 1, y + 1, x + w - 2, y + h - 1, G[3])
        for i in range(h):
            if 0 <= i - 2 < w - 2:
                im.px(x + 1 + i - 2, y + 1 + i, G[5])
        im.vline(x + w // 2, y + 1, y + h - 1, P.METAL[2])
        im.hline(x + 1, x + w - 2, y + 1, G[1])
        g = b.glow
        for yy in range(y + 1, y + h):
            for xx in range(x + 1, x + w - 1):
                if xx != x + w // 2:
                    g.px(xx, yy, (*rgb(P.NIGHT_GLOW[2])[:3], 220))
    else:
        im.rect(x, y, x + w - 1, y + h - 1, Wd[3])
        for xx in range(x + 1, x + w - 1, 3):
            im.vline(xx, y + 1, y + h - 1, Wd[2])
        im.rect(x + 2, y + 2, x + w - 3, y + h // 2 - 1, Wd[2])
        im.rect(x + 3, y + 3, x + w - 4, y + h // 2 - 2, Wd[4])
        im.hline(x, x + w - 1, y, Wd[4])
        im.vline(x + w - 1, y, y + h - 1, Wd[1])
        im.px(x + w - 3, y + h // 2 + 1, P.GOLD[4])
        im.px(x + w - 3, y + h // 2 + 2, P.GOLD[2])
    # pillə
    s = P.STONE
    im.rect(x - 2, y + h - 1, x + w + 1, y + h - 1, s[5])


def sign(b, text, cx, y, bg, fg, pad=3, border=None):
    im = b.img
    tw = text_width(text)
    x0 = cx - tw // 2 - pad
    x1 = x0 + tw + pad * 2 - 1
    y1 = y + 5 + 3
    bd = border or P.WOOD
    im.rect(x0 - 1, y - 1, x1 + 1, y1 + 1, bd[1])
    im.rect(x0, y, x1, y1, bg[3])
    im.hline(x0, x1, y, bg[4])
    im.hline(x0, x1, y1, bg[2])
    draw_text(im, text, x0 + pad, y + 2, fg, shadow=bg[1])
    im.hline(x0 - 1, x1 + 1, y1 + 2, (40, 24, 50, 90))
    return x0, x1


def potted_plant(b, x, y, flower):
    im = b.img
    T = P.ORANGE
    im.rect(x, y + 3, x + 4, y + 6, T[2])
    im.hline(x, x + 4, y + 3, T[3])
    im.vline(x + 4, y + 3, y + 6, T[1])
    L = P.LEAF
    for (dx, dy, c) in [(1, 2, L[4]), (2, 1, L[5]), (3, 2, L[4]), (0, 1, L[3]), (4, 1, L[3]), (2, 0, flower[4]), (1, 0, flower[3]), (3, 1, flower[4]), (2, 2, L[3])]:
        im.px(x + dx, y + dy, c)


# ---------------- 1) Sara nənənin evi ----------------
def house(w=112, h=80, seed=3):
    b = B(w, h)
    im = b.img
    R = P.ROOF_RED
    roof_b = 42
    # divar (krem taxta)
    siding(b, 2, roof_b, w - 3, h - 5, P.CREAM)
    im.vline(2, roof_b, h - 5, P.CREAM[2])
    im.vline(w - 3, roof_b, h - 5, P.CREAM[1])
    # künc dirəkləri
    for xx in (2, w - 5):
        im.rect(xx, roof_b, xx + 2, h - 5, P.WOOD[4])
        im.vline(xx + 2, roof_b, h - 5, P.WOOD[2])
    stone_base(b, 1, w - 2, h - 4, h - 1)
    # dam: yuxarıda daralan (trapesiya) forma
    for y in range(0, roof_b + 1):
        inset = max(0, 6 - y // 2) if y < 12 else 0
        im.hline(inset, w - 1 - inset, y, R[3])
    shingles(b, 0, 4, w - 1, roof_b - 3, R, seed)
    for y in range(0, 4):
        inset = max(0, 6 - y // 2) + 2
        im.hline(inset, w - 1 - inset, y, R[2] if y == 0 else mix(R[2], R[3], 0.5))
    im.hline(4, w - 5, 0, R[1])
    # dam silsiləsi (ridge)
    im.hline(6, w - 7, 2, R[5])
    # damın yan kənarları
    for y in range(4, roof_b):
        im.px(0, y, R[1])
        im.px(w - 1, y, R[0])
    # karniz taxtası
    im.rect(0, roof_b - 2, w - 1, roof_b, P.WOOD[3])
    im.hline(0, w - 1, roof_b - 2, P.WOOD[5])
    im.hline(0, w - 1, roof_b, P.WOOD[1])
    eave_shadow(b, 2, w - 3, roof_b + 1, 3)
    # baca
    cx = 78
    im.rect(cx, 0, cx + 9, 14, P.BRICK[3])
    for yy in range(1, 14, 3):
        im.hline(cx, cx + 9, yy, P.BRICK[2])
        for xx in range(cx + (0 if (yy // 3) % 2 else 2), cx + 10, 4):
            im.px(xx, yy + 1, P.BRICK[2])
    im.rect(cx - 1, 0, cx + 10, 2, P.BRICK[4])
    im.hline(cx - 1, cx + 10, 0, P.BRICK[5])
    im.vline(cx + 9, 3, 14, P.BRICK[1])
    im.hline(cx, cx + 9, 15, (40, 20, 30, 120))
    # dam pəncərəsi (mansard)
    dx = 24
    im.rect(dx - 2, 10, dx + 13, 12, R[1])
    window(b, dx, 13, 12, 12, frame=P.WHITE, curtains=None)
    im.poly([(dx - 3, 13), (dx + 6, 6), (dx + 15, 13)], R[2])
    im.line(dx - 3, 13, dx + 6, 6, R[5])
    im.line(dx + 6, 6, dx + 15, 13, R[1])
    # pəncərələr: qapının iki tərəfində
    door_x = 6 * 16 - 3 * 16 - 7 + 16 // 2   # xəritədə qapı sütunu (3-cü sütun) ortası
    door_x = 3 * 16 + 3
    for wx in (14, 74):
        window(b, wx, roof_b + 8, 18, 15, frame=P.WHITE, curtains=P.PINK, box=P.RED, shutters=P.GREEN)
    door(b, door_x, h - 26, 10, 22, Wd=P.WOOD)
    # qapı üstündə kiçik lampa
    im.px(door_x + 5, h - 30, P.GOLD[4])
    im.px(door_x + 5, h - 29, P.GOLD[3])
    b.glow.px(door_x + 5, h - 30, (255, 220, 140, 255))
    potted_plant(b, door_x - 9, h - 11, P.PINK)
    potted_plant(b, door_x + 15, h - 11, P.YELLOW)
    return b


# ---------------- 2) Yaşayış binası ----------------
def apartment(w=160, h=96, seed=5):
    b = B(w, h)
    im = b.img
    S = P.ROOF_SLATE
    Cr = P.CREAM
    top = 12
    # düz dam (yuxarıdan görünən)
    im.rect(0, 0, w - 1, top, S[3])
    for y in range(1, top):
        for x in range(1, w - 1):
            if (x + y * 3) % 11 == 0:
                im.px(x, y, S[4])
    im.rect(0, 0, w - 1, 1, S[5])
    im.vline(0, 0, top, S[4])
    im.vline(w - 1, 0, top, S[1])
    # damda: kondisioner, peyk antenası, su çəni
    im.rect(14, 3, 25, 9, P.WHITE[3]); im.hline(14, 25, 3, P.WHITE[5]); im.rect(16, 5, 21, 8, P.WHITE[2])
    im.circle(122, 6, 4, P.WHITE[4]); im.circle(121, 5, 2, P.WHITE[5]); im.vline(124, 6, 10, P.METAL[3])
    im.rect(60, 2, 75, 9, P.BLUE[3]); im.hline(60, 75, 2, P.BLUE[4]); im.vline(75, 2, 9, P.BLUE[1])
    for xx in (62, 73):
        im.vline(xx, 10, 11, P.METAL[2])
    # parapet
    im.rect(0, top, w - 1, top + 3, Cr[2])
    im.hline(0, w - 1, top, Cr[5])
    im.hline(0, w - 1, top + 3, Cr[1])
    eave_shadow(b, 1, w - 2, top + 4, 2)
    # fasad
    plaster(b, 1, top + 4, w - 2, h - 5, P.CREAM, seed)
    im.vline(0, top + 4, h - 5, Cr[2])
    im.vline(w - 1, top + 4, h - 5, Cr[1])
    # mərtəbə xətləri
    floors = [top + 6, top + 30, top + 54]
    for fy in floors[1:]:
        im.hline(1, w - 2, fy - 3, Cr[5])
        im.hline(1, w - 2, fy - 2, Cr[2])
    r = rng(seed)
    door_col = 5
    for fi, fy in enumerate(floors):
        for col in range(10):
            wx = col * 16 + 3
            if fi == 2 and col in (door_col - 1, door_col, door_col + 1):
                continue
            wy = fy + 2
            window(b, wx, wy, 10, 14, frame=P.WHITE, curtains=r.choice([None, None, None, None, P.YELLOW, P.PINK]))
            # balkonlar (2-ci və 1-ci mərtəbədə bəzi sütunlarda)
            if fi < 2 and col in (1, 4, 8) or (fi == 0 and col == 6):
                by = wy + 13
                im.rect(wx - 3, by, wx + 12, by + 2, P.STONE[4])
                im.hline(wx - 3, wx + 12, by + 2, P.STONE[2])
                for xx in range(wx - 3, wx + 13, 2):
                    im.vline(xx, by - 5, by - 1, P.METAL[2])
                im.hline(wx - 3, wx + 12, by - 5, P.METAL[4])
                if col == 4 and fi == 0:
                    # paltar ipi
                    im.hline(wx - 2, wx + 11, by - 9, P.WHITE[1])
                    for k, cl in enumerate([P.RED, P.BLUE, P.YELLOW, P.WHITE]):
                        xx = wx - 1 + k * 3
                        im.rect(xx, by - 8, xx + 1, by - 6, cl[3])
                if col == 8 and fi == 1:
                    potted_plant(b, wx + 2, by - 7, P.RED)
            # kondisioner blokları
            if (col, fi) in ((2, 0), (7, 1), (9, 0), (0, 1)):
                ax, ay = wx + 1, wy + 15
                if ay + 5 < h - 6:
                    im.rect(ax, ay, ax + 7, ay + 4, P.WHITE[4]); im.hline(ax, ax + 7, ay, P.WHITE[5])
                    im.vline(ax + 7, ay, ay + 4, P.WHITE[2]); im.rect(ax + 1, ay + 1, ax + 4, ay + 3, P.WHITE[2])
    # giriş: qapı + saçaq
    dx = door_col * 16 + 2
    door(b, dx, h - 26, 12, 22, Wd=P.WOOD, glass=False)
    im.rect(dx - 6, h - 32, dx + 17, h - 29, P.STONE[4])
    im.hline(dx - 6, dx + 17, h - 32, P.STONE[6])
    im.hline(dx - 6, dx + 17, h - 29, P.STONE[2])
    eave_shadow(b, dx - 5, dx + 16, h - 28, 2)
    draw_text(im, '27', dx + 3, h - 38, P.BLUE[2])
    # domofon paneli
    im.rect(dx + 14, h - 18, dx + 15, h - 14, P.METAL[3])
    stone_base(b, 0, w - 1, h - 4, h - 1)
    return b


# ---------------- 3) Poçt ----------------
def post(w=128, h=80, seed=7):
    b = B(w, h)
    im = b.img
    R = P.ROOF_BLUE
    roof_b = 34
    plaster(b, 1, roof_b, w - 2, h - 5, P.WHITE, seed)
    im.vline(1, roof_b, h - 5, P.WHITE[3])
    im.vline(w - 2, roof_b, h - 5, P.WHITE[2])
    stone_base(b, 0, w - 1, h - 4, h - 1)
    # dam (dörd tərəfə maili, kənarlarda qısalır)
    for y in range(0, roof_b + 1):
        inset = max(0, 10 - y) if y < 10 else 0
        im.hline(inset, w - 1 - inset, y, R[3])
    shingles(b, 0, 2, w - 1, roof_b - 3, R, seed, row_h=4)
    for y in range(0, 10):
        inset = max(0, 10 - y)
        im.px(inset, y, R[1])
        im.px(w - 1 - inset, y, R[0])
        if inset > 0:
            for x in range(0, inset):
                im.erase(x, y)
                im.erase(w - 1 - x, y)
    im.hline(10, w - 11, 0, R[5])
    im.rect(0, roof_b - 2, w - 1, roof_b, R[1])
    im.hline(0, w - 1, roof_b - 2, R[4])
    eave_shadow(b, 1, w - 2, roof_b + 1, 3)
    # lövhə
    sign(b, 'POÇT', w // 2, roof_b + 5, P.BLUE, P.YELLOW[4], border=P.BLUE)
    # pəncərələr və qapı (qapı xəritədə 5-ci sütunda)
    dx = 5 * 16 + 2
    for wx in (10, 34, 106):
        window(b, wx, roof_b + 18, 14, 16, frame=P.BLUE, mullion=True)
    door(b, dx, h - 27, 12, 23, glass=True, frame=P.BLUE)
    # poçt qutusu
    bx = dx - 14
    im.rect(bx, h - 18, bx + 7, h - 9, P.BLUE[3])
    im.hline(bx, bx + 7, h - 18, P.BLUE[4])
    im.vline(bx + 7, h - 18, h - 9, P.BLUE[1])
    im.hline(bx + 1, bx + 6, h - 15, P.BLUE[1])
    im.px(bx + 3, h - 12, P.YELLOW[4]); im.px(bx + 4, h - 12, P.YELLOW[4])
    im.vline(bx + 3, h - 8, h - 5, P.METAL[2]); im.vline(bx + 4, h - 8, h - 5, P.METAL[1])
    return b


# ---------------- 4) Market ----------------
def market(w=128, h=80, seed=9):
    b = B(w, h)
    im = b.img
    top = 10
    S = P.ROOF_SLATE
    im.rect(0, 0, w - 1, top, S[2])
    im.hline(0, w - 1, 0, S[4])
    for x in range(2, w - 2, 9):
        im.vline(x, 2, top - 1, S[3])
    # parapet + lövhə zolağı
    im.rect(0, top, w - 1, top + 14, P.RED[2])
    im.hline(0, w - 1, top, P.RED[4])
    im.hline(0, w - 1, top + 14, P.RED[0])
    sign(b, 'MARKET', w // 2, top + 3, P.WHITE, P.RED[2], border=P.RED)
    # fasad
    plaster(b, 1, top + 15, w - 2, h - 5, P.WHITE, seed)
    stone_base(b, 0, w - 1, h - 4, h - 1)
    # zolaqlı çardaq (tente)
    ay = top + 18
    for x in range(0, w):
        stripe = (x // 8) % 2 == 0
        for y in range(ay, ay + 9):
            c = P.RED[3] if stripe else P.WHITE[5]
            if y == ay:
                c = P.RED[4] if stripe else '#ffffff'
            im.px(x, y, c)
        # dalğalı alt kənar
        k = x % 8
        if k in (1, 2, 3, 4, 5, 6):
            im.px(x, ay + 9, P.RED[2] if stripe else P.WHITE[3])
        if k in (2, 3, 4, 5):
            im.px(x, ay + 10, P.RED[1] if stripe else P.WHITE[2])
    eave_shadow(b, 1, w - 2, ay + 11, 3)
    # vitrin pəncərələri (malları görünən)
    dx = 6 * 16 + 1
    wy = ay + 14
    for wx, ww in ((6, 40), (52, 38)):
        window(b, wx, wy, ww, h - wy - 8, frame=P.METAL, mullion=False)
        r = rng(wx)
        # rəflərdə mallar
        for sy in (wy + 6, wy + 14):
            if sy + 3 < h - 9:
                im.hline(wx + 2, wx + ww - 3, sy + 3, P.WOOD[3])
                for xx in range(wx + 2, wx + ww - 3, 3):
                    c = r.choice([P.RED, P.YELLOW, P.GREEN, P.ORANGE, P.BLUE, P.PINK])
                    im.rect(xx, sy, xx + 1, sy + 2, c[3])
                    im.px(xx, sy, c[4])
    door(b, dx, h - 27, 14, 23, glass=True, frame=P.METAL)
    # qapının yanında "AÇIQ" lövhəciyi əvəzinə kiçik yaşıl işıq
    im.px(dx + 17, h - 24, P.GREEN[4]); b.glow.px(dx + 17, h - 24, (120, 255, 140, 255))
    return b


# ---------------- 5) Çayxana ----------------
def teahouse(w=128, h=80, seed=11):
    b = B(w, h)
    im = b.img
    R = P.ROOF_GREEN
    roof_b = 30
    siding(b, 1, roof_b, w - 2, h - 5, P.WOOD, board=4)
    # taxta fasad üzərində dirəklər
    for xx in (1, 40, 86, w - 4):
        im.rect(xx, roof_b, xx + 2, h - 5, P.WOOD[2])
        im.vline(xx, roof_b, h - 5, P.WOOD[4])
    stone_base(b, 0, w - 1, h - 4, h - 1)
    for y in range(0, roof_b + 1):
        inset = max(0, 8 - y) if y < 8 else 0
        im.hline(inset, w - 1 - inset, y, R[3])
    shingles(b, 0, 2, w - 1, roof_b - 3, R, seed, row_h=4)
    for y in range(0, 8):
        inset = max(0, 8 - y)
        for x in range(0, inset):
            im.erase(x, y)
            im.erase(w - 1 - x, y)
        im.px(inset, y, R[1]); im.px(w - 1 - inset, y, R[0])
    im.hline(8, w - 9, 0, R[5])
    im.rect(0, roof_b - 2, w - 1, roof_b, P.WOOD[2])
    im.hline(0, w - 1, roof_b - 2, P.WOOD[4])
    eave_shadow(b, 1, w - 2, roof_b + 1, 3)
    sign(b, 'ÇAYXANA', w // 2, roof_b + 4, P.GREEN, P.YELLOW[4], border=P.WOOD)
    # tağlı pəncərələr
    dx = 4 * 16 + 1
    for wx in (10, 46, 92):
        if abs(wx - dx) < 20:
            continue
        window(b, wx, roof_b + 19, 22, 18, frame=P.WOOD, curtains=P.RED, mullion=True)
        im.poly([(wx, roof_b + 19), (wx + 11, roof_b + 15), (wx + 22, roof_b + 19)], P.WOOD[3])
    door(b, dx, h - 27, 14, 23, glass=True, frame=P.WOOD)
    # yaşıl tente qapı üstündə
    for x in range(dx - 6, dx + 21):
        for y in range(h - 33, h - 29):
            im.px(x, y, P.GREEN[3] if ((x - dx) // 4) % 2 == 0 else P.WHITE[5])
        if (x - dx) % 4 in (1, 2):
            im.px(x, h - 29, P.GREEN[2])
    eave_shadow(b, dx - 6, dx + 20, h - 28, 2)
    # samovar lövhəsi (kiçik qızıl samovar)
    sx = 30
    im.rect(sx, h - 22, sx + 5, h - 14, P.GOLD[3]); im.vline(sx + 5, h - 22, h - 14, P.GOLD[1])
    im.hline(sx - 1, sx + 6, h - 14, P.GOLD[2]); im.rect(sx + 1, h - 24, sx + 4, h - 23, P.GOLD[4])
    im.px(sx + 2, h - 26, P.WHITE[4]); im.px(sx + 3, h - 27, P.WHITE[3])
    return b


ALL = {
    'bld_house': house,
    'bld_apartment': apartment,
    'bld_post': post,
    'bld_market': market,
    'bld_teahouse': teahouse,
}
