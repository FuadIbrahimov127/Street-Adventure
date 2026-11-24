# v8: "Məktəb məhəlləsi" (sağdakı yeni xəritə): məktəb, kitabxana, çörəkxana, futbol meydançası, tarla.
import math
from px import Img, mix, rng, rgb
import pal as P
import buildings as BD
import interiors as INT

OUT = P.OUTLINE


# ======================= BİNALAR =======================
def _roof(b, w, roof_b, R, seed, inset_top=8):
    """bütün en boyu kirəmit dam (yuxarı künclər kəsik)"""
    im = b.img
    for y in range(0, roof_b + 1):
        inset = max(0, inset_top - y)
        im.hline(inset, w - 1 - inset, y, R[3])
    BD.shingles(b, 0, 2, w - 1, roof_b - 3, R, seed, row_h=4)
    for y in range(0, inset_top):
        inset = max(0, inset_top - y)
        for x in range(0, inset):
            im.erase(x, y)
            im.erase(w - 1 - x, y)
        im.px(inset, y, R[1]); im.px(w - 1 - inset, y, R[0])
    im.hline(inset_top, w - 1 - inset_top, 0, R[5])


def school(w=224, h=96, seed=21):
    b = BD.B(w, h)
    im = b.img
    roof_b = 28
    # divarlar: kərpic
    Br = P.BRICK
    im.rect(1, roof_b, w - 2, h - 5, Br[3])
    for y in range(roof_b, h - 5):
        for x in range(1, w - 1):
            if (y - roof_b) % 4 == 3 or ((x + (8 if ((y - roof_b) // 4) % 2 else 0)) % 16 == 0):
                im.px(x, y, Br[2])
            elif (x * 7 + y * 3) % 23 == 0:
                im.px(x, y, Br[4])
    BD.stone_base(b, 0, w - 1, h - 4, h - 1)
    _roof(b, w, roof_b, P.ROOF_SLATE, seed)
    im.rect(0, roof_b - 2, w - 1, roof_b, P.STONE[3]); im.hline(0, w - 1, roof_b - 2, P.STONE[5])
    BD.eave_shadow(b, 1, w - 2, roof_b + 1, 3)
    # ortada giriş: sütunlar və üçbucaq frontu
    cx = w // 2
    im.poly([(cx - 30, roof_b + 2), (cx, roof_b - 14), (cx + 30, roof_b + 2)], P.CREAM[4])
    im.poly([(cx - 24, roof_b), (cx, roof_b - 10), (cx + 24, roof_b)], P.CREAM[5])
    im.line(cx - 30, roof_b + 2, cx, roof_b - 14, P.CREAM[2]); im.line(cx, roof_b - 14, cx + 30, roof_b + 2, P.CREAM[2])
    # saat
    im.circle(cx, roof_b - 3, 4, P.WOOD[2]); im.circle(cx, roof_b - 3, 3, '#fff8e8')
    im.vline(cx, roof_b - 5, roof_b - 3, P.BLACKISH[2]); im.hline(cx, cx + 2, roof_b - 3, P.BLACKISH[2])
    im.rect(cx - 28, roof_b + 2, cx + 28, h - 5, P.CREAM[4])
    for sx in (cx - 26, cx - 14, cx + 11, cx + 23):
        im.rect(sx, roof_b + 4, sx + 3, h - 6, P.WHITE[5]); im.vline(sx + 3, roof_b + 4, h - 6, P.WHITE[3])
        im.hline(sx - 1, sx + 4, roof_b + 3, P.WHITE[4]); im.hline(sx - 1, sx + 4, h - 6, P.WHITE[3])
    BD.sign(b, 'MƏKTƏB', cx, roof_b + 5, P.BLUE, P.YELLOW[4], border=P.WOOD)
    BD.door(b, cx - 10, h - 27, 20, 23, glass=True, frame=P.WOOD)
    # pəncərələr: iki sıra
    for wx in list(range(8, cx - 34, 22)) + list(range(cx + 36, w - 20, 22)):
        for wy in (roof_b + 8, roof_b + 34):
            BD.window(b, wx, wy, 16, 18, frame=P.WHITE, curtains=None, mullion=True)
    # bayraq dirəyi
    im.vline(6, 4, roof_b - 2, P.METAL[4])
    for y in range(5, 13):
        im.hline(7, 18, y, P.BLUE[3] if y < 8 else P.RED[3] if y < 10 else P.GREEN[3])
    im.px(12, 9, P.WHITE[5]); im.px(13, 9, P.WHITE[5])
    return b


def library(w=128, h=80, seed=23):
    b = BD.B(w, h)
    im = b.img
    roof_b = 26
    BD.plaster(b, 1, roof_b, w - 2, h - 5, P.STONE, seed)
    BD.stone_base(b, 0, w - 1, h - 4, h - 1)
    _roof(b, w, roof_b, P.ROOF_GREEN, seed)
    im.rect(0, roof_b - 2, w - 1, roof_b, P.STONE[4]); im.hline(0, w - 1, roof_b - 2, P.STONE[6])
    BD.eave_shadow(b, 1, w - 2, roof_b + 1, 3)
    cx = 64
    # klassik sütunlar
    for sx in (6, 22, 98, 114):
        im.rect(sx, roof_b + 3, sx + 5, h - 6, P.CREAM[5]); im.vline(sx + 5, roof_b + 3, h - 6, P.CREAM[3])
        im.hline(sx - 1, sx + 6, roof_b + 2, P.CREAM[4]); im.hline(sx - 1, sx + 6, h - 6, P.CREAM[3])
    BD.sign(b, 'KİTABXANA', cx, roof_b + 4, P.PURPLE, P.YELLOW[4], border=P.WOOD)
    BD.door(b, cx - 8, h - 27, 16, 23, Wd=P.WOOD, glass=False, frame=P.CREAM)
    for wx in (32, 82):
        BD.window(b, wx, roof_b + 18, 14, 20, frame=P.CREAM, curtains=P.PURPLE, mullion=True)
        im.poly([(wx, roof_b + 18), (wx + 7, roof_b + 14), (wx + 14, roof_b + 18)], P.CREAM[4])
    # açıq kitab lövhəsi
    im.rect(cx - 7, roof_b + 14, cx + 6, roof_b + 19, '#fff8e8'); im.vline(cx, roof_b + 14, roof_b + 19, P.WOOD[2])
    for y in (roof_b + 15, roof_b + 17):
        im.hline(cx - 5, cx - 2, y, P.BLUE[3]); im.hline(cx + 2, cx + 5, y, P.BLUE[3])
    return b


def bakery(w=112, h=80, seed=25):
    b = BD.B(w, h)
    im = b.img
    roof_b = 28
    BD.plaster(b, 1, roof_b, w - 2, h - 5, P.CREAM, seed)
    BD.stone_base(b, 0, w - 1, h - 4, h - 1)
    _roof(b, w, roof_b, P.ROOF_RED, seed)
    im.rect(0, roof_b - 2, w - 1, roof_b, P.WOOD[2]); im.hline(0, w - 1, roof_b - 2, P.WOOD[4])
    BD.eave_shadow(b, 1, w - 2, roof_b + 1, 3)
    # baca və tüstü
    im.rect(86, 2, 93, 14, P.BRICK[3]); im.hline(85, 94, 2, P.BRICK[4])
    for k, (x, y) in enumerate([(88, 0), (90, -2)]):
        pass
    BD.sign(b, 'ÇÖRƏK', 56, roof_b + 4, P.ORANGE, P.WHITE[5], border=P.WOOD)
    dx = 33
    BD.door(b, dx, h - 27, 14, 23, glass=True, frame=P.WOOD)
    # vitrin: çörəklər
    wx, wy, ww, wh = 58, roof_b + 18, 44, 22
    BD.window(b, wx, wy, ww, wh, frame=P.WOOD, curtains=None, mullion=False)
    im.rect(wx + 2, wy + wh - 6, wx + ww - 3, wy + wh - 3, P.WOOD[4])
    for k in range(5):
        bx = wx + 4 + k * 8
        im.ellipse(bx + 3, wy + wh - 8, 3.5, 2, P.GOLD[3]); im.px(bx + 2, wy + wh - 9, P.GOLD[5])
    # narıncı-ağ tente
    for x in range(wx - 3, wx + ww + 3):
        for y in range(wy - 7, wy - 2):
            im.px(x, y, P.ORANGE[3] if ((x - wx) // 5) % 2 == 0 else P.WHITE[5])
        if (x - wx) % 5 in (1, 3):
            im.px(x, wy - 2, P.ORANGE[2])
    BD.eave_shadow(b, wx - 3, wx + ww + 2, wy - 1, 2)
    BD.potted_plant(b, 12, h - 12, P.PINK)
    return b


BUILDINGS = {'bld_school': school, 'bld_library': library, 'bld_bakery': bakery}
for _n, _f in BUILDINGS.items():
    BD.ALL[_n] = _f


# ======================= PLİTƏLƏR =======================
def yard_tile(seed):
    """məktəb həyəti: qırmızımtıl kərpic döşəmə"""
    im = Img(16, 16)
    r = rng(seed)
    A, Bc = '#c47a5a', '#b06a4c'
    for y in range(16):
        for x in range(16):
            bx = (x + (4 if (y // 4) % 2 else 0)) // 8
            c = A if (bx + y // 4) % 2 == 0 else Bc
            if y % 4 == 3 or (x + (4 if (y // 4) % 2 else 0)) % 8 == 7:
                c = '#8e5440'
            im.px(x, y, c)
    for _ in range(4):
        im.px(r.randint(0, 15), r.randint(0, 15), '#d89474')
    return im


def field_tile(seed):
    """tarla: bellənməmiş açıq torpaq, arabir ot"""
    im = Img(16, 16)
    r = rng(seed)
    base = ['#a07a4e', '#b08a5a', '#bc966a', '#c8a478']
    for y in range(16):
        for x in range(16):
            v = 1 + (1 if r.random() < 0.35 else 0) - (1 if r.random() < 0.15 else 0)
            im.px(x, y, base[v])
    for _ in range(3):
        x, y = r.randint(1, 14), r.randint(2, 14)
        im.px(x, y, P.GRASS[3]); im.px(x - 1, y - 1, P.GRASS[4]); im.px(x + 1, y - 1, P.GRASS[4])
    for _ in range(3):
        im.px(r.randint(0, 15), r.randint(0, 15), '#8a6a44')
    return im


# ======================= OBYEKTLƏR =======================
def pitch(cw=16, ch=9):
    """futbol meydançası: zolaqlı ot və ağ xətlər (yerə düz qoyulur)"""
    w, h = cw * 16, ch * 16
    im = Img(w, h)
    G1, G2 = '#4f9a42', '#5aa84a'
    for y in range(h):
        for x in range(w):
            im.px(x, y, G1 if (x // 16) % 2 == 0 else G2)
    Wt = '#f4f8ee'
    m = 6
    for x in range(m, w - m):
        im.px(x, m, Wt); im.px(x, h - 1 - m, Wt)
    for y in range(m, h - m):
        im.px(m, y, Wt); im.px(w - 1 - m, y, Wt); im.px(w // 2, y, Wt)
    for a in range(120):
        t = a / 120 * math.tau
        im.px(w // 2 + math.cos(t) * 18, h // 2 + math.sin(t) * 18, Wt)
    im.rect(w // 2 - 1, h // 2 - 1, w // 2, h // 2, Wt)
    # cərimə meydançaları
    for side in (0, 1):
        x0 = m if side == 0 else w - 1 - m - 28
        x1 = x0 + 28
        y0, y1 = h // 2 - 30, h // 2 + 30
        for x in range(x0, x1 + 1):
            im.px(x, y0, Wt); im.px(x, y1, Wt)
        xe = x1 if side == 0 else x0
        for y in range(y0, y1 + 1):
            im.px(xe, y, Wt)
        px_ = m + 20 if side == 0 else w - 1 - m - 20
        im.px(px_, h // 2, Wt)
    return im


def goal():
    """qapı (yan görünüş): ağ dirəklər, boz tor. Ağzı sağa baxır"""
    im = Img(18, 44)
    Wt, N = '#f4f8ee', '#b8c0c8'
    for y in range(6, 40):
        for x in range(2, 14):
            if (x + y) % 3 == 0 or (x - y) % 3 == 0:
                im.px(x, y, N)
    im.rect(13, 4, 15, 41, Wt); im.vline(15, 4, 41, '#c8d0d8')
    im.rect(1, 4, 15, 6, Wt); im.rect(1, 38, 15, 40, Wt)
    im.vline(1, 4, 40, '#c8d0d8')
    im.outline(col='#5a6070')
    return im


def swing(frame):
    im = Img(34, 30)
    M = P.METAL
    im.line(2, 29, 9, 2, M[3]); im.line(16, 29, 9, 2, M[4])
    im.line(17, 29, 24, 2, M[3]); im.line(31, 29, 24, 2, M[4])
    im.rect(8, 1, 25, 3, P.RED[3]); im.hline(8, 25, 1, P.RED[4])
    off = [-2, 2][frame]
    for sx, c in ((13, P.BLUE), (20, P.YELLOW)):
        im.line(sx, 3, sx + off, 20, M[2]); im.line(sx + 3, 3, sx + 3 + off, 20, M[2])
        im.rect(sx + off - 1, 20, sx + 4 + off, 22, c[3]); im.hline(sx + off - 1, sx + 4 + off, 20, c[4])
    im.outline(col=OUT)
    return im


def slide():
    im = Img(30, 30)
    M = P.METAL
    im.rect(2, 6, 3, 29, M[3]); im.rect(9, 6, 10, 29, M[3])
    for y in range(9, 29, 4):
        im.hline(3, 9, y, M[4])
    im.rect(1, 4, 12, 6, P.GREEN[3])
    for k in range(18):
        x, y = 11 + k, 6 + int(k * 1.15)
        im.rect(x, y, x + 1, y + 3, P.ORANGE[3] if k % 4 else P.ORANGE[4])
    im.outline(col=OUT)
    return im


def hoop():
    im = Img(18, 44)
    M = P.METAL
    im.rect(8, 10, 9, 43, M[3]); im.vline(9, 10, 43, M[2])
    im.rect(2, 0, 15, 10, '#f4f8ee'); im.rect(5, 3, 12, 8, P.RED[3]); im.rect(6, 4, 11, 7, '#f4f8ee')
    im.hline(4, 13, 11, P.ORANGE[3]); im.hline(5, 12, 12, P.ORANGE[2])
    for x in range(5, 13, 2):
        im.vline(x, 13, 16, '#e8e8f0')
    im.rect(5, 41, 12, 43, M[2])
    im.outline(col=OUT)
    return im


def fence_h():
    im = Img(16, 14)
    Wd = P.WOOD
    for x in (1, 13):
        im.rect(x, 1, x + 2, 13, Wd[3]); im.vline(x, 1, 13, Wd[4]); im.px(x + 1, 0, Wd[4])
    im.rect(0, 4, 15, 5, Wd[4]); im.rect(0, 9, 15, 10, Wd[3])
    im.outline(col=OUT)
    return im


def fence_v():
    im = Img(6, 20)
    Wd = P.WOOD
    im.rect(1, 1, 4, 19, Wd[3]); im.vline(1, 1, 19, Wd[4]); im.hline(1, 4, 1, Wd[5])
    im.outline(col=OUT)
    return im


def tandir(frame):
    """təndir: gil soba, ağzında od"""
    im = Img(26, 24)
    C = ['#6a3a24', '#8a5232', '#a86c42', '#c48a5a', '#dcaa78']
    im.ellipse(13, 15, 11, 9, C[2]); im.ellipse(11, 13, 8, 6, C[3]); im.px(8, 10, C[4]); im.px(9, 9, C[4])
    im.ellipse(13, 7, 6, 2.5, C[1]); im.ellipse(13, 7, 4.5, 1.6, '#2a1410')
    fl = [P.YELLOW[4], P.ORANGE[4], P.RED[3]]
    for k, (x, y) in enumerate([(11, 6), (13, 5), (15, 6), (12, 4), (14, 3)]):
        if (k + frame) % 2 == 0:
            im.px(x, y, fl[k % 3])
    im.rect(10, 18, 16, 21, '#2a1410'); im.rect(11, 19, 15, 21, P.ORANGE[3] if frame == 0 else P.YELLOW[3])
    im.outline(col='#3a2014')
    return im


def book(col):
    im = Img(12, 10)
    im.rect(1, 2, 10, 8, col[3]); im.hline(1, 10, 2, col[4]); im.vline(1, 2, 8, col[2])
    im.rect(2, 8, 10, 9, '#f6ead0'); im.hline(3, 8, 5, P.GOLD[4])
    im.outline(col=OUT)
    return im


def book_icon():
    im = Img(16, 16)
    im.poly([(1, 4), (8, 2), (8, 14), (1, 13)], '#f6ead0'); im.poly([(8, 2), (15, 4), (15, 13), (8, 14)], '#fffaf0')
    for y in (6, 8, 10):
        im.hline(3, 6, y, P.BLUE[3]); im.hline(10, 13, y, P.BLUE[3])
    im.vline(8, 2, 14, P.RED[3])
    im.outline(col=OUT)
    return im


def spade_icon():
    """bel"""
    im = Img(16, 16)
    Wd = P.WOOD
    for k in range(9):
        im.px(4 + k * 0.7, 1 + k, Wd[3]); im.px(5 + k * 0.7, 1 + k, Wd[4])
    im.rect(2, 0, 6, 1, Wd[2])
    M = P.METAL
    im.poly([(9, 9), (14, 11), (12, 15), (8, 14)], M[4])
    im.poly([(10, 10), (13, 11), (12, 13)], M[5])
    im.line(9, 9, 8, 14, M[2])
    im.outline(col=OUT)
    return im


def tile_cursor():
    im = Img(16, 16)
    C = (255, 250, 220, 255)
    for k in range(0, 5):
        for (x, y) in [(k, 0), (0, k), (15 - k, 0), (15, k), (k, 15), (0, 15 - k), (15 - k, 15), (15, 15 - k)]:
            im.px(x, y, C)
    for k in range(1, 4):
        for (x, y) in [(k, 1), (1, k), (14 - k, 1), (14, k), (k, 14), (1, 14 - k), (14 - k, 14), (14, 14 - k)]:
            im.px(x, y, (90, 60, 30, 200))
    return im


def dust(frame):
    im = Img(16, 10)
    C = ['#c8a478', '#a8845a', '#e0c49a']
    pts = [(3, 6, 2.2), (8, 5, 2.8), (12, 6, 2)] if frame == 0 else [(2, 4, 1.6), (8, 3, 2.2), (13, 4, 1.5)]
    for x, y, rr in pts:
        im.circle(x, y, rr, C[0]); im.px(x - 1, y - 1, C[2])
    return im


# ---------------- kitabxananın içi ----------------
def library_room():
    w, h = INT.ROOM_W * 16, INT.ROOM_H * 16
    im = Img(w, h)
    INT._planks(im, 0, INT.WALL_H * 16, w - 1, h - 1, ['#3a2018', '#5c3222', '#7a4a2e', '#96603a', '#b07c4c', '#cc9c66', '#e6c08a'], 13)
    INT._walls(im, P.GREEN, None, base=P.WOOD)
    # divarda taxta panellər
    for x in range(8, w - 8, 24):
        im.rect(x, 6, x + 18, 26, P.GREEN[2]); im.rect(x + 1, 7, x + 17, 25, P.GREEN[3])
    INT._window(im, 90, 8, 22, 16, P.PURPLE)
    # "Oxu!" posteri
    im.rect(140, 6, 160, 24, '#fff4dc'); im.rect(142, 9, 158, 21, P.ORANGE[3]); im.rect(146, 12, 154, 18, '#fff4dc'); im.vline(150, 12, 18, P.RED[3])
    return im


def lib_shelf(seed):
    im = Img(32, 40)
    Wd = P.WOOD
    im.rect(0, 0, 31, 39, Wd[2]); im.rect(2, 2, 29, 37, Wd[1])
    r = rng(seed)
    cols = [P.RED, P.BLUE, P.GREEN, P.YELLOW, P.PURPLE, P.ORANGE, P.TEAL]
    for j in range(4):
        y1 = 10 + j * 9
        im.rect(2, y1, 29, y1, Wd[3])
        x = 3
        while x < 28:
            bw = r.choice([2, 2, 3])
            bh = r.choice([6, 7, 7, 8])
            c = r.choice(cols)
            im.rect(x, y1 - bh, x + bw - 1, y1 - 1, c[3]); im.vline(x, y1 - bh, y1 - 1, c[4])
            x += bw + (1 if r.random() < 0.2 else 0)
    im.hline(0, 31, 0, Wd[4])
    im.outline(col=OUT)
    return im


def read_table():
    im = Img(36, 24)
    Wd = P.WOOD
    im.rect(0, 8, 35, 12, Wd[4]); im.hline(0, 35, 8, Wd[5])
    im.rect(2, 13, 4, 23, Wd[2]); im.rect(31, 13, 33, 23, Wd[2])
    # açıq kitab və lampa
    im.rect(6, 5, 15, 8, '#fffaf0'); im.vline(10, 5, 8, P.WOOD[2])
    im.vline(27, 2, 7, P.METAL[3]); im.rect(24, 0, 30, 2, P.GREEN[3]); im.rect(25, 7, 29, 7, P.METAL[2])
    im.rect(17, 5, 21, 7, P.RED[3]); im.rect(17, 3, 21, 4, P.BLUE[3])
    im.outline(col=OUT)
    return im


def lib_desk():
    im = Img(52, 30)
    Wd = P.WOOD
    im.rect(0, 10, 51, 13, Wd[4]); im.hline(0, 51, 10, Wd[5])
    im.rect(1, 14, 50, 29, Wd[2])
    for x in range(5, 48, 11):
        im.rect(x, 17, x + 7, 26, Wd[3])
    # kitab yığını, möhür, kompüter
    for k, c in enumerate([P.RED, P.BLUE, P.GREEN, P.YELLOW]):
        im.rect(4, 8 - k * 2, 13, 9 - k * 2, c[3])
    im.rect(30, 1, 44, 9, P.METAL[2]); im.rect(31, 2, 43, 8, P.TEAL[3]); im.rect(36, 9, 38, 10, P.METAL[2])
    im.outline(col=OUT)
    return im


def globe():
    im = Img(14, 20)
    im.circle(7, 6, 5.5, P.BLUE[3]); im.pts([(5, 4), (6, 4), (6, 5), (9, 7), (8, 8), (9, 8), (4, 8)], P.GREEN[4])
    im.vline(7, 12, 17, P.WOOD[2]); im.rect(3, 17, 11, 19, P.WOOD[3])
    im.outline(col=OUT)
    return im


ROOMS = {'room_library': library_room, 'int_libshelf': lambda: lib_shelf(3), 'int_libshelf2': lambda: lib_shelf(9),
         'int_readtable': read_table, 'int_libdesk': lib_desk, 'int_globe': globe}

CHAR_SPECS = {
    'zehra': dict(skin='light', hair=P.HAIR['grey'], style='bun', top=P.PURPLE, under=P.WHITE, dress=P.PURPLE, shoes=P.BLACKISH, glasses=True, book=P.BLUE),
    'gulnare': dict(skin='tan', style='scarf', scarf=P.WHITE, top=P.RED, dress=P.RED, apron=P.WHITE, shoes=P.WOOD),
}
