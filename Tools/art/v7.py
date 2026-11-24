# v7: yeni otaqlar (poçt, çayxana, sənin evin), yeni qonşular, bağça, fəsillər və qış.
import colorsys
import math
import numpy as np
from px import Img, mix, rng, rgb
import pal as P
import interiors as INT

OUT = P.OUTLINE


# ======================= OTAQLAR =======================
def post_room():
    w, h = INT.ROOM_W * 16, INT.ROOM_H * 16
    im = Img(w, h)
    INT._checker(im, 0, INT.WALL_H * 16, w - 1, h - 1, '#ece6d6', P.PASTEL_BLUE[3])
    INT._walls(im, P.PASTEL_BLUE, 'stripes', base=P.STONE)
    INT._window(im, 18, 8, 22, 16, P.YELLOW)
    INT._window(im, 152, 8, 22, 16, P.YELLOW)
    # divarda poçt lövhəsi: zərf şəkli
    im.rect(80, 5, 112, 24, P.BLUE[2]); im.rect(81, 6, 111, 23, P.BLUE[3])
    im.rect(87, 9, 105, 20, '#fff8e8')
    im.line(87, 9, 96, 15, P.RED[3]); im.line(105, 9, 96, 15, P.RED[3])
    # divar saatı
    im.circle(132, 12, 5, P.STONE[2]); im.circle(132, 12, 3.8, '#fff8e8')
    im.vline(132, 9, 12, P.BLACKISH[2]); im.hline(132, 134, 12, P.BLACKISH[2])
    return im


def teahouse_room():
    w, h = INT.ROOM_W * 16, INT.ROOM_H * 16
    im = Img(w, h)
    INT._planks(im, 0, INT.WALL_H * 16, w - 1, h - 1, P.WOOD, 7)
    INT._walls(im, P.ORANGE, None, base=P.WOOD)
    # divarda böyük xalça (qırmızı, qızılı naxış)
    x0, y0, x1, y1 = 58, 4, 134, 26
    im.rect(x0, y0, x1, y1, P.RED[1])
    im.rect(x0 + 2, y0 + 2, x1 - 2, y1 - 2, P.RED[2])
    im.rect(x0 + 5, y0 + 5, x1 - 5, y1 - 5, P.RED[3])
    for x in range(x0 + 2, x1 - 1, 4):
        im.px(x, y0 + 3, P.GOLD[4]); im.px(x, y1 - 3, P.GOLD[4])
    cx, cy = (x0 + x1) // 2, (y0 + y1) // 2
    for k, (dx, c) in enumerate([(0, P.GOLD[4]), (-24, P.BLUE[3]), (24, P.BLUE[3])]):
        for r in range(6, 0, -1):
            col = c if r % 2 == 0 else P.RED[1]
            for d in range(-r, r + 1):
                im.px(cx + dx + d, cy - (r - abs(d)), col); im.px(cx + dx + d, cy + (r - abs(d)), col)
    for y in range(y1 + 1, y1 + 4):
        for x in range(x0 + 1, x1, 3):
            im.px(x, y, P.CREAM[4])
    INT._window(im, 16, 8, 22, 16, P.RED)
    INT._window(im, 154, 8, 22, 16, P.RED)
    return im


def home_room():
    w, h = INT.ROOM_W * 16, INT.ROOM_H * 16
    im = Img(w, h)
    INT._planks(im, 0, INT.WALL_H * 16, w - 1, h - 1, ['#4a2a1c', '#6e3e26', '#94583a', '#b8774e', '#d29a68', '#e8bc8a', '#f6dcb0'], 11)
    INT._walls(im, P.MINT, 'floral', base=P.WOOD)
    INT._window(im, 60, 8, 22, 16, P.BLUE)
    # futbol posteri
    im.rect(104, 6, 124, 24, P.WHITE[5]); im.rect(105, 7, 123, 23, P.GREEN[3])
    im.hline(105, 123, 15, P.WHITE[5]); im.circle(114, 15, 3, P.WHITE[5]); im.circle(114, 15, 2, P.GREEN[3])
    im.circle(110, 11, 2, '#ffffff'); im.px(110, 11, P.BLACKISH[2])
    # divar saatı
    im.circle(140, 12, 5, P.RED[3]); im.circle(140, 12, 3.8, '#fff8e8')
    im.vline(140, 9, 12, P.BLACKISH[2]); im.hline(140, 142, 12, P.BLACKISH[2])
    return im


# ---------------- mebel ----------------
def post_counter():
    im = Img(60, 30)
    Wd = P.WOOD
    im.rect(0, 8, 59, 11, Wd[4]); im.hline(0, 59, 8, Wd[5])
    im.rect(1, 12, 58, 29, P.BLUE[3]); im.hline(1, 58, 12, P.BLUE[2])
    for x in range(8, 56, 12):
        im.rect(x, 16, x + 8, 26, P.BLUE[2]); im.rect(x + 1, 17, x + 7, 25, P.BLUE[3])
    # tərəzi
    im.rect(6, 4, 16, 7, P.METAL[4]); im.hline(6, 16, 4, P.METAL[5]); im.rect(10, 1, 12, 3, P.METAL[3])
    # möhür və mürəkkəb
    im.rect(24, 5, 27, 7, P.RED[3]); im.rect(25, 2, 26, 4, Wd[3])
    # bağlamalar
    im.rect(38, 1, 47, 7, P.WOOD[5]); im.vline(42, 1, 7, P.WOOD[3]); im.hline(38, 47, 4, P.WOOD[3])
    im.rect(49, 3, 55, 7, '#fff8e8'); im.line(49, 3, 52, 5, P.RED[3]); im.line(55, 3, 52, 5, P.RED[3])
    im.outline(col=OUT)
    return im


def mail_shelf():
    im = Img(44, 34)
    Wd = P.WOOD
    im.rect(0, 0, 43, 33, Wd[2]); im.rect(1, 1, 42, 32, Wd[3])
    r = rng(17)
    for j in range(3):
        for i in range(5):
            x, y = 3 + i * 8, 3 + j * 10
            im.rect(x, y, x + 6, y + 7, Wd[1])
            if r.random() < 0.75:
                c = r.choice(['#fff8e8', '#f6e6c0', P.PASTEL_BLUE[4], P.PINK[5]])
                im.rect(x + 1, y + 3, x + 5, y + 7, c); im.px(x + 3, y + 4, P.RED[3])
    im.outline(col=OUT)
    return im


def parcels():
    im = Img(28, 26)
    def box(x0, y0, x1, y1, c):
        im.rect(x0, y0, x1, y1, c[4]); im.hline(x0, x1, y0, c[5]); im.vline(x1, y0, y1, c[3])
        im.vline((x0 + x1) // 2, y0, y1, c[2]); im.hline(x0, x1, y0 + 2, c[3])
    box(1, 13, 14, 25, P.WOOD); box(14, 15, 26, 25, P.WOOD); box(5, 3, 19, 13, P.WOOD)
    im.rect(8, 6, 12, 9, '#fff8e8')
    im.outline(col=OUT)
    return im


def tea_table():
    im = Img(36, 24)
    # döşəkçələr
    for x0 in (0, 27):
        im.rect(x0, 12, x0 + 8, 21, P.RED[3]); im.hline(x0, x0 + 8, 12, P.RED[4]); im.hline(x0, x0 + 8, 21, P.RED[2])
        im.px(x0 + 4, 16, P.GOLD[4])
    Wd = P.WOOD
    im.rect(7, 9, 28, 13, Wd[4]); im.hline(7, 28, 9, Wd[5]); im.hline(7, 28, 13, Wd[2])
    im.rect(9, 14, 10, 19, Wd[2]); im.rect(25, 14, 26, 19, Wd[2])
    # armudu stəkanlar və çaydan
    for x in (11, 22):
        im.rect(x, 4, x + 2, 8, '#c0502a'); im.px(x, 4, '#e88a5a'); im.hline(x - 1, x + 3, 8, P.GOLD[3])
    im.rect(15, 3, 20, 8, P.WHITE[4]); im.hline(15, 20, 3, P.WHITE[5]); im.px(21, 5, P.WHITE[3]); im.px(17, 2, P.WHITE[3])
    im.outline(col=OUT)
    return im


def tea_counter():
    im = Img(56, 36)
    Wd = P.WOOD
    im.rect(0, 14, 55, 17, Wd[4]); im.hline(0, 55, 14, Wd[5])
    im.rect(1, 18, 54, 35, Wd[2])
    for x in range(4, 52, 10):
        im.rect(x, 21, x + 6, 32, Wd[3]); im.hline(x, x + 6, 21, Wd[4])
    # böyük samovar
    G = P.GOLD
    im.ellipse(14, 8, 7, 6, G[3]); im.ellipse(13, 7, 5, 4, G[4]); im.px(11, 5, G[6])
    im.rect(12, 0, 16, 2, G[2]); im.rect(9, 12, 19, 13, G[2]); im.px(21, 9, G[2]); im.px(22, 10, G[2])
    im.rect(13, 1, 15, 2, '#f0e8e0')
    # stəkanlar sırası
    for x in range(28, 52, 5):
        im.rect(x, 9, x + 2, 13, '#c0502a'); im.px(x, 9, '#e88a5a'); im.hline(x - 1, x + 3, 13, G[3])
    im.outline(col=OUT)
    return im


def bed():
    im = Img(28, 44)
    Wd = P.WOOD
    im.rect(0, 0, 27, 6, Wd[3]); im.hline(0, 27, 0, Wd[5]); im.rect(2, 2, 25, 4, Wd[4])
    im.rect(1, 6, 26, 41, Wd[2])
    im.rect(3, 7, 24, 15, '#fbf6ee'); im.hline(3, 24, 15, '#d8d0c8'); im.rect(5, 9, 22, 13, '#ffffff')
    B = P.BLUE
    im.rect(2, 16, 25, 40, B[3]); im.hline(2, 25, 16, B[4]); im.hline(2, 25, 17, B[4])
    for y in range(20, 40, 5):
        for x in range(4, 24, 5):
            im.px(x, y, B[5]); im.px(x + 1, y + 1, B[2])
    im.rect(1, 41, 26, 43, Wd[1])
    im.outline(col=OUT)
    return im


def desk():
    im = Img(32, 28)
    Wd = P.WOOD
    im.rect(0, 10, 31, 13, Wd[4]); im.hline(0, 31, 10, Wd[5])
    im.rect(1, 14, 30, 27, Wd[2]); im.rect(18, 15, 29, 26, Wd[3]); im.px(23, 20, P.GOLD[4])
    im.rect(2, 14, 4, 27, Wd[1])
    # masa lampası
    im.vline(6, 4, 9, P.METAL[3]); im.rect(3, 0, 9, 3, P.YELLOW[3]); im.hline(3, 9, 3, P.YELLOW[2]); im.rect(4, 9, 8, 9, P.METAL[2])
    # kitablar və dəftər
    im.rect(14, 6, 19, 9, P.RED[3]); im.rect(14, 4, 19, 5, P.GREEN[3]); im.rect(22, 7, 29, 9, '#fff8e8'); im.hline(23, 28, 8, P.BLUE[3])
    im.outline(col=OUT)
    return im


def wardrobe():
    im = Img(26, 40)
    Wd = P.WOOD
    im.rect(0, 0, 25, 39, Wd[2]); im.rect(1, 1, 24, 38, Wd[3]); im.hline(1, 24, 1, Wd[5])
    im.rect(3, 4, 11, 34, Wd[4]); im.rect(14, 4, 22, 34, Wd[4])
    im.px(11, 19, P.GOLD[4]); im.px(14, 19, P.GOLD[4])
    im.rect(1, 36, 24, 39, Wd[1])
    im.outline(col=OUT)
    return im


ROOMS = {
    'room_post': post_room, 'room_teahouse': teahouse_room, 'room_home': home_room,
    'int_postcounter': post_counter, 'int_mailshelf': mail_shelf, 'int_parcels': parcels,
    'int_teatable': tea_table, 'int_teacounter': tea_counter,
    'int_bed': bed, 'int_desk': desk, 'int_wardrobe': wardrobe,
}

# yeni qonşular
CHAR_SPECS = {
    'leyla': dict(skin='light', hair=P.HAIR['black'], style='bun', top=P.BLUE, collar=P.WHITE[5], pants=P.BLACKISH, shoes=P.BLACKISH, glasses=True),
    'tural': dict(skin='tan', hair=P.HAIR['brown'], style='messy', top=P.GREEN, under=P.WHITE, pants=P.BLUE, shorts=True, shoes=P.RED, hat=('band', P.YELLOW)),
}


# ======================= BAĞÇA =======================
SOIL = ['#3a2418', '#52321e', '#6c4428', '#865834', '#a06e44']
SOIL_WET = ['#24160e', '#341f14', '#46291a', '#5a3622', '#6e452e']


def soil(wet):
    S = SOIL_WET if wet else SOIL
    im = Img(16, 16)
    r = rng(31 if wet else 37)
    im.rect(1, 1, 14, 14, S[2])
    for y in (3, 7, 11):
        im.hline(2, 13, y, S[1]); im.hline(2, 13, y + 1, S[3])
    for _ in range(10):
        im.px(r.randint(2, 13), r.randint(2, 13), S[4] if r.random() < 0.5 else S[1])
    im.hline(1, 14, 1, S[3]); im.hline(1, 14, 14, S[0]); im.vline(1, 1, 14, S[1]); im.vline(14, 1, 14, S[1])
    if wet:
        for x, y in [(4, 5), (10, 9), (7, 12)]:
            im.px(x, y, '#7a6a8a')
    return im


def _leaf_pair(im, x, y, L, big=False):
    im.px(x, y, L[3]); im.px(x - 1, y - 1, L[4]); im.px(x + 1, y - 1, L[4])
    if big:
        im.px(x - 2, y - 1, L[3]); im.px(x + 2, y - 1, L[3]); im.px(x - 2, y - 2, L[5]); im.px(x + 2, y - 2, L[5])


def crop(kind, stage):
    im = Img(16, 20)
    L = P.LEAF
    if stage == 0:
        im.vline(8, 15, 17, L[2]); _leaf_pair(im, 8, 15, L)
        im.outline(col=L[0])
        return im
    if kind == 'tomato':
        hgt = [0, 7, 11, 13][stage]
        top = 18 - hgt
        if stage >= 2:
            im.vline(12, top - 1, 18, P.WOOD[3]); im.px(12, top - 2, P.WOOD[4])
        im.vline(8, top, 18, L[2])
        for k, y in enumerate(range(top + 1, 18, 3)):
            _leaf_pair(im, 8, y, L, big=stage >= 2)
        if stage == 2:
            for x, y in [(6, top + 3), (10, top + 6)]:
                im.px(x, y, P.YELLOW[4])
            im.px(7, top + 8, L[5]); im.px(10, top + 9, L[5])
        if stage == 3:
            for x, y in [(5, top + 4), (10, top + 3), (6, top + 9), (11, top + 8), (8, top + 6)]:
                im.rect(x, y, x + 1, y + 1, P.RED[3]); im.px(x, y, P.RED[5])
    elif kind == 'carrot':
        n = [0, 3, 5, 7][stage]
        for i in range(n):
            a = (i - (n - 1) / 2) * 0.35
            ln = 5 + stage * 2 - abs(i - n // 2)
            for t in range(ln):
                x = 8 + math.sin(a) * t * 0.9
                y = 17 - t
                im.px(x, y, L[3] if t < ln - 2 else L[5])
        if stage == 3:
            im.rect(7, 16, 9, 18, P.ORANGE[3]); im.px(7, 16, P.ORANGE[5]); im.hline(7, 9, 18, P.ORANGE[2])
    else:  # qarpız
        for k in range(stage + 1):
            x0 = 3 + k * 2
            im.hline(x0, 13 - k, 17 - k % 2, L[2])
        for x, y in [(4, 15), (12, 16), (7, 14)][:stage + 1]:
            _leaf_pair(im, x, y, L, big=True)
        if stage >= 2:
            rx, ry = (2.5, 2) if stage == 2 else (5, 3.5)
            cx, cy = 9, 15 if stage == 2 else 14
            im.ellipse(cx, cy, rx, ry, P.GREEN[2])
            for x in range(int(cx - rx), int(cx + rx) + 1, 2):
                for y in range(int(cy - ry), int(cy + ry) + 1):
                    if im.alpha(x, y):
                        im.px(x, y, P.GREEN[4])
            im.px(cx - 1, cy - int(ry) + 1, P.GREEN[5])
    im.outline(col=L[0])
    return im


def crop_icon(kind):
    im = Img(16, 16)
    if kind == 'tomato':
        im.circle(8, 9, 5.5, P.RED[3]); im.circle(7, 8, 3.5, P.RED[4]); im.px(6, 6, P.RED[5]); im.px(5, 7, '#ffd0c0')
        im.pts([(6, 3), (8, 4), (10, 3), (7, 4), (9, 4), (8, 2)], P.LEAF[4])
    elif kind == 'carrot':
        for i in range(10):
            w = max(0, 2.6 - i * 0.26)
            x, y = 5 + i * 0.75, 4 + i
            im.rect(x - w, y, x + w, y, P.ORANGE[3] if i % 3 else P.ORANGE[2])
        im.px(4, 4, P.ORANGE[5]); im.px(5, 5, P.ORANGE[4])
        im.pts([(3, 2), (4, 1), (5, 2), (6, 1), (2, 3), (6, 3)], P.LEAF[4])
    else:
        im.ellipse(8, 9, 6.5, 5, P.GREEN[2])
        for x in range(2, 15, 3):
            for y in range(3, 15):
                if im.alpha(x, y):
                    im.px(x, y, P.GREEN[4])
        im.px(5, 6, P.GREEN[5]); im.px(6, 5, P.GREEN[5])
    im.outline(col=OUT)
    return im


def seed_packet(kind):
    im = Img(16, 16)
    C = {'tomato': P.RED, 'carrot': P.ORANGE, 'melon': P.GREEN}[kind]
    im.rect(3, 1, 12, 14, '#f6ead0'); im.hline(3, 12, 1, C[3]); im.hline(3, 12, 2, C[3]); im.hline(3, 12, 14, '#d6bc94')
    ic = crop_icon(kind)
    for y in range(16):
        for x in range(16):
            if ic.a[y, x, 3] > 0 and ic.a[y, x, 0:3].tolist() != list(rgb(OUT)[:3]):
                tx, ty = 4 + x * 8 // 16, 4 + y * 9 // 16
                im.px(tx, ty, tuple(int(v) for v in ic.a[y, x]))
    for x in (5, 8, 11):
        im.px(x, 13, '#8a6a4a')
    im.outline(col=OUT)
    return im


def sell_box():
    im = Img(24, 22)
    Wd = P.WOOD
    im.rect(1, 7, 22, 21, Wd[3])
    for y in range(9, 21, 4):
        im.hline(1, 22, y, Wd[2])
    im.vline(1, 7, 21, Wd[2]); im.vline(22, 7, 21, Wd[1])
    im.rect(0, 2, 23, 7, Wd[4]); im.hline(0, 23, 2, Wd[5]); im.hline(0, 23, 7, Wd[2])
    im.rect(9, 10, 14, 13, P.GOLD[3]); im.px(10, 11, P.GOLD[5])
    # manat işarəsi əvəzinə sikkə
    im.circle(12, 16, 2.5, P.GOLD[4]); im.px(12, 16, P.GOLD[2])
    im.outline(col=OUT)
    return im


def water_tap():
    im = Img(14, 24)
    M = P.BLUE
    im.rect(5, 4, 8, 21, M[3]); im.vline(5, 4, 21, M[4]); im.vline(8, 4, 21, M[2])
    im.rect(4, 2, 9, 4, M[2]); im.hline(4, 9, 2, M[4])
    im.rect(9, 7, 12, 8, M[3]); im.px(12, 9, M[2])
    im.rect(10, 0, 11, 1, P.RED[3]); im.vline(10, 1, 3, P.METAL[3])
    im.rect(3, 21, 10, 23, P.STONE[3]); im.hline(3, 10, 21, P.STONE[4])
    # vedrə
    im.rect(9, 14, 13, 20, P.METAL[4]); im.hline(9, 13, 14, P.METAL[5]); im.vline(13, 14, 20, P.METAL[3])
    im.rect(10, 15, 12, 15, P.WATER[4])
    im.outline(col=OUT)
    return im


def seed_stand():
    im = Img(32, 26)
    Wd = P.WOOD
    im.rect(0, 10, 31, 13, Wd[4]); im.hline(0, 31, 10, Wd[5])
    im.rect(2, 14, 4, 25, Wd[2]); im.rect(27, 14, 29, 25, Wd[2]); im.rect(2, 18, 29, 19, Wd[2])
    for k, c in enumerate([P.RED, P.ORANGE, P.GREEN, P.RED, P.ORANGE]):
        x = 2 + k * 6
        im.rect(x, 4, x + 4, 9, '#f6ead0'); im.rect(x, 4, x + 4, 5, c[3]); im.px(x + 2, 7, c[3])
    # kiçik lövhə
    im.vline(30, 0, 9, Wd[2]); im.rect(24, 0, 31, 4, P.GREEN[3]); im.px(27, 2, P.GREEN[5]); im.px(28, 1, P.GREEN[5])
    im.outline(col=OUT)
    return im


def kitten(col, frame=0):
    """pişik balası: kiçik oturan pişik"""
    im = Img(12, 12)
    C = col
    im.rect(3, 6, 8, 10, C[3]); im.hline(3, 8, 10, C[2]); im.vline(8, 6, 10, C[2])
    im.rect(4, 7, 6, 10, C[4])
    im.rect(2, 2, 7, 6, C[3]); im.hline(2, 7, 2, C[4])
    im.pts([(2, 1), (3, 0), (6, 0), (7, 1)], C[3]); im.px(3, 1, P.PINK[4]); im.px(6, 1, P.PINK[4])
    im.px(3, 4, '#24161e'); im.px(6, 4, '#24161e'); im.px(4, 5, P.PINK[3]); im.px(5, 5, P.PINK[3])
    tail = [(9, 10), (10, 9), (10, 8)] if frame == 0 else [(9, 10), (10, 10), (11, 9)]
    for x, y in tail:
        im.px(x, y, C[3])
    im.outline(col='#3a1a10')
    return im


KITTENS = {
    'kitten_white': ['#6a6474', '#9a96a2', '#c8c4cc', '#ecebf0', '#ffffff'],
    'kitten_black': ['#121018', '#1e1a26', '#2e2838', '#423a4e', '#5c526a'],
    'kitten_ginger': P.ORANGE,
}


# ======================= FƏSİLLƏR =======================
AUTUMN_A = ['#4a1a12', '#7a2a16', '#a8401a', '#d0621e', '#ea8a2a', '#f6b24a', '#fcd77a', '#fff0b0']
AUTUMN_B = ['#4a2a10', '#7a4a14', '#a87018', '#d09a22', '#eabe36', '#f6da5c', '#fcec96', '#fff8cc']
WINTER = ['#2a3448', '#3e4c66', '#5a6c88', '#8496b4', '#b2c2da', '#d6e2f0', '#f0f6ff', '#ffffff']
SUMMER_LEAF = P.LEAF


def _is_leaf(r, g, b):
    h, l, s = colorsys.rgb_to_hls(r / 255, g / 255, b / 255)
    hue = h * 360
    green = 60 <= hue <= 175 and s > 0.18 and l > 0.06
    pink = (hue >= 290 or hue <= 15) and s > 0.2 and l > 0.25    # çiçəkli ağac
    return green, pink, l


def season_variant(src, season, seed=0, keep_fruit=True):
    """ağac və kollar: yarpaqların rəngini fəslə görə dəyiş. season: 1 yay, 2 payız, 3 qış"""
    im = src.copy()
    r = rng(seed + season * 13)
    h, w = im.a.shape[:2]
    ramp = SUMMER_LEAF if season == 1 else (AUTUMN_A if seed % 2 == 0 else AUTUMN_B) if season == 2 else WINTER
    for y in range(h):
        for x in range(w):
            R, G, B, A = [int(v) for v in im.a[y, x]]
            if A == 0:
                continue
            green, pink, l = _is_leaf(R, G, B)
            if season == 1 and not pink:
                continue
            if not (green or pink):
                continue
            k = min(len(ramp) - 1, max(0, int(l * (len(ramp) + 1.2))))
            if season == 2 and r.random() < 0.12:
                k = min(len(ramp) - 1, k + 1)
            if season == 3:
                # qış: yuxarı baxan yerlər ağ qar, qalanı boz-mavi
                above = im.a[y - 1, x, 3] if y > 0 else 0
                k = 7 if above == 0 else min(6, k + 2)
            c = rgb(ramp[k])
            im.a[y, x] = (c[0], c[1], c[2], A)
    return im


def snowcap(src, depth=2):
    """yuxarıya baxan bütün kənarlara qar qoyur (binalar, skamyalar)"""
    im = src.copy()
    h, w = im.a.shape[:2]
    a = src.a[:, :, 3]
    snow = ['#ffffff', '#eef4ff', '#d4e0f0']
    for x in range(w):
        for y in range(h):
            if a[y, x] == 0:
                continue
            if y == 0 or a[y - 1, x] == 0:
                for d in range(depth + 1):
                    yy = y + d
                    if yy < h and a[yy, x] > 0:
                        c = rgb(snow[min(d, 2)])
                        im.a[yy, x] = (c[0], c[1], c[2], 255)
                # qarın kənarında kiçik kontur
                if y > 0:
                    im.a[y - 1, x] = (200, 214, 236, 255) if x % 5 == 0 else im.a[y - 1, x]
    return im


def snow_tile(seed):
    im = Img(16, 16)
    r = rng(seed)
    base = ['#dfe8f4', '#e9f0f8', '#f4f8fd', '#ffffff']
    for y in range(16):
        for x in range(16):
            v = 2 + (1 if r.random() < 0.25 else 0) - (1 if r.random() < 0.15 else 0)
            im.px(x, y, base[v])
    for _ in range(3):
        x, y = r.randint(1, 13), r.randint(2, 13)
        im.hline(x, x + 2, y, '#d2deee')
        im.px(x + 1, y - 1, '#ffffff')
    im.px(r.randint(0, 15), r.randint(0, 15), '#c6e2ff')
    return im


def leaves_tile(seed):
    im = Img(16, 16)
    r = rng(seed)
    cols = [AUTUMN_A[4], AUTUMN_A[3], AUTUMN_B[4], AUTUMN_B[5], AUTUMN_A[5]]
    for _ in range(6):
        x, y = r.randint(1, 14), r.randint(1, 14)
        c = r.choice(cols)
        im.px(x, y, c); im.px(x + 1, y, c); im.px(x, y + 1, mix(c, '#000000', 0.25))
    return im


def snowflake():
    im = Img(3, 3)
    im.px(1, 0, '#ffffff'); im.px(0, 1, '#ffffff'); im.px(1, 1, '#ffffff'); im.px(2, 1, '#ffffff'); im.px(1, 2, '#ffffff')
    return im


def snowball():
    im = Img(7, 7)
    im.circle(3, 3, 3, '#e2ecf8'); im.circle(2.5, 2.5, 2, '#ffffff'); im.px(4, 5, '#b8c8de')
    im.outline(col='#7a8aa6')
    return im


def snow_splat():
    im = Img(12, 7)
    for x, y in [(1, 4), (3, 2), (5, 5), (8, 3), (10, 5), (6, 1)]:
        im.px(x, y, '#ffffff'); im.px(x + 1, y, '#e2ecf8')
    im.rect(4, 3, 7, 4, '#ffffff')
    return im


def snow_pile():
    im = Img(16, 9)
    im.ellipse(8, 6, 7, 3.5, '#e2ecf8'); im.ellipse(7, 5, 5, 2.5, '#ffffff'); im.px(11, 7, '#c6d4e8')
    im.outline(col='#8a9ab6')
    return im


def snowman(stage):
    im = Img(20, 32)
    S = ['#c6d4e8', '#e2ecf8', '#ffffff']

    def ball(cx, cy, r):
        im.circle(cx, cy, r, S[0]); im.circle(cx - 0.6, cy - 0.6, r - 1.2, S[1]); im.circle(cx - 1.4, cy - 1.4, max(1, r - 3), S[2])

    ball(10, 26, 6.5)
    if stage >= 1:
        ball(10, 17, 4.8)
        for y in (15, 18):
            im.px(10, y, '#2a2430')
    if stage >= 2:
        ball(10, 10, 3.8)
        im.px(9, 9, '#2a2430'); im.px(12, 9, '#2a2430')
        # budaq əllər
        im.line(5, 16, 1, 13, P.WOOD[2]); im.px(1, 12, P.WOOD[2])
        im.line(15, 16, 19, 13, P.WOOD[2]); im.px(19, 12, P.WOOD[2])
    if stage >= 3:
        # yerkökü burun, şərf, papaq
        im.hline(11, 14, 10, P.ORANGE[3]); im.px(11, 11, P.ORANGE[2])
        im.hline(6, 14, 13, P.RED[3]); im.hline(7, 13, 14, P.RED[2]); im.vline(13, 14, 17, P.RED[3])
        im.rect(7, 2, 13, 6, P.BLACKISH[2]); im.hline(5, 15, 6, P.BLACKISH[1]); im.hline(7, 13, 5, P.RED[3])
    im.outline(col='#7a8aa6')
    return im


def season_icon(season):
    im = Img(16, 16)
    if season == 0:      # yaz: çiçək
        for a in range(5):
            t = a / 5 * math.tau
            im.circle(8 + math.cos(t) * 3.6, 7 + math.sin(t) * 3.6, 2.2, P.PINK[4])
        im.circle(8, 7, 2, P.YELLOW[4]); im.vline(8, 11, 15, P.LEAF[3]); im.px(9, 13, P.LEAF[4]); im.px(10, 12, P.LEAF[4])
    elif season == 1:    # yay: günəş
        im.circle(8, 8, 4, P.YELLOW[4]); im.circle(7, 7, 2.5, P.YELLOW[5])
        for a in range(8):
            t = a / 8 * math.tau
            im.px(8 + math.cos(t) * 6.4, 8 + math.sin(t) * 6.4, P.ORANGE[4])
    elif season == 2:    # payız: yarpaq
        im.poly([(8, 1), (13, 5), (12, 11), (8, 14), (4, 11), (3, 5)], AUTUMN_A[4])
        im.poly([(8, 3), (11, 6), (8, 12), (5, 6)], AUTUMN_A[5])
        im.vline(8, 3, 15, AUTUMN_A[2])
    else:                # qış: qar dənəsi (qalın xətlərlə)
        C = '#e6f2ff'
        im.rect(7, 1, 8, 14, C); im.rect(1, 7, 14, 8, C)
        for k in range(-5, 6):
            im.px(7.5 + k, 7.5 + k, '#ffffff'); im.px(7.5 + k, 7.5 - k, '#ffffff')
        for (x, y) in [(5, 2), (10, 2), (5, 13), (10, 13), (2, 5), (2, 10), (13, 5), (13, 10)]:
            im.px(x, y, C)
        im.rect(6, 6, 9, 9, '#ffffff')
        im.outline(col=P.BLUE[2])
        return im
    im.outline(col=OUT)
    return im


def invite_icon():
    im = Img(16, 16)
    im.rect(1, 4, 14, 13, '#fff4dc'); im.hline(1, 14, 13, '#d6bc94')
    im.line(1, 4, 7, 9, P.PINK[3]); im.line(14, 4, 8, 9, P.PINK[3])
    im.circle(7.5, 9, 2, P.RED[3]); im.px(7, 8, P.RED[5])
    im.pts([(11, 1), (12, 2), (13, 1), (12, 0)], P.YELLOW[4])
    im.outline(col=OUT)
    return im


def bunting():
    """bayram bayraqcıqları (şənlik üçün)"""
    im = Img(64, 14)
    for x in range(64):
        y = 2 + int(3 * math.sin(x / 63 * math.pi))
        im.px(x, y, P.WOOD[2])
    cols = [P.RED, P.YELLOW, P.GREEN, P.BLUE, P.PINK, P.ORANGE]
    for i, x in enumerate(range(3, 62, 7)):
        y = 2 + int(3 * math.sin(x / 63 * math.pi)) + 1
        c = cols[i % len(cols)]
        for k in range(6):
            im.hline(x - (5 - k) // 2, x + (5 - k) // 2, y + k, c[3] if k < 4 else c[2])
        im.px(x - 1, y + 1, c[5])
    return im


def confetti(seed):
    im = Img(3, 3)
    c = [P.RED, P.YELLOW, P.GREEN, P.BLUE, P.PINK][seed % 5]
    im.px(0, 0, c[4]); im.px(1, 0, c[3]); im.px(1, 1, c[3]) if seed % 2 else im.px(0, 1, c[4])
    return im


def roof_snow(name):
    """binalar: dam örtüyü (shingles ilə çəkilən hissə) qarla örtülür, kənarlara da qar"""
    import buildings as BD
    rects = []
    orig = BD.shingles

    def rec(b, x0, y0, x1, y1, R, *a, **k):
        rects.append((x0, y0, x1, y1, R))
        return orig(b, x0, y0, x1, y1, R, *a, **k)

    BD.shingles = rec
    try:
        src = BD.ALL[name]().img
    finally:
        BD.shingles = orig
    im = snowcap(src, 2)
    h, w = im.a.shape[:2]
    for (x0, y0, x1, y1, R) in rects:
        pal = [np.array(rgb(c)[:3], dtype=np.float32) for c in R]
        for y in range(max(0, int(y0)), min(h, int(y1) + 1)):
            for x in range(max(0, int(x0)), min(w, int(x1) + 1)):
                Rr, G, B, A = [int(v) for v in src.a[y, x]]
                if A == 0:
                    continue
                d = min(float(np.abs(np.array([Rr, G, B], dtype=np.float32) - c).sum()) for c in pal)
                if d > 60:
                    continue
                l = (0.3 * Rr + 0.59 * G + 0.11 * B) / 255
                k = 7 if l > 0.5 else 6 if l > 0.36 else 5 if l > 0.24 else 4
                c = rgb(WINTER[k])
                im.a[y, x] = (c[0], c[1], c[2], 255)
    return im
