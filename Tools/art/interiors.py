# Evlərin içi: otaq fonu (döşəmə + divarlar) və mebel spritləri.
import math
from px import Img, rgb, mix, rng, darker, lighter
import pal as P

ROOM_W, ROOM_H = 12, 9          # xanalarla
WALL_H = 2                      # yuxarı divarın hündürlüyü (xana)


def _planks(im, x0, y0, x1, y1, W, seed=1):
    r = rng(seed)
    for y in range(y0, y1 + 1):
        row = (y - y0) // 6
        k = (y - y0) % 6
        off = (row * 23) % 40
        for x in range(x0, x1 + 1):
            c = W[4] if k not in (0, 5) else (W[5] if k == 0 else W[2])
            if (x + off) % 40 == 0:
                c = W[2]
            im.px(x, y, c)
        if k == 2 and r.random() < 0.9:
            for _ in range(3):
                xx = r.randint(x0, x1)
                im.px(xx, y, mix(W[4], W[3], 0.6))


def _checker(im, x0, y0, x1, y1, a, b):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            t = ((x - x0) // 16 + (y - y0) // 16) % 2
            c = a if t == 0 else b
            if (x - x0) % 16 == 0 or (y - y0) % 16 == 0:
                c = mix(c, '#000000', 0.08)
            im.px(x, y, c)


def _walls(im, wall_ramp, pattern=None, base=None):
    w, h = im.w, im.h
    top = WALL_H * 16
    # yuxarı divar
    for y in range(0, top):
        for x in range(0, w):
            c = wall_ramp[4]
            if pattern == 'floral' and (x % 8 == 3 and y % 8 == 3):
                c = wall_ramp[2]
            elif pattern == 'floral' and ((x % 8 == 2 or x % 8 == 4) and y % 8 == 3 or (y % 8 in (2, 4) and x % 8 == 3)):
                c = wall_ramp[3]
            elif pattern == 'stripes' and (x // 4) % 3 == 0:
                c = wall_ramp[3]
            im.px(x, y, c)
    for x in range(w):
        im.px(x, 0, wall_ramp[1])
        im.px(x, 1, wall_ramp[2])
    # süpürgəlik
    B = base or P.WOOD
    im.rect(0, top - 4, w - 1, top - 1, B[3])
    im.hline(0, w - 1, top - 4, B[5])
    im.hline(0, w - 1, top - 1, B[1])
    # divardan döşəməyə kölgə
    for k in range(4):
        im.hline(4, w - 5, top + k, (30, 20, 40, int(90 * (1 - k / 4))))
    # yan divarlar
    for y in range(0, h):
        for k in range(4):
            c = wall_ramp[1] if k == 0 else wall_ramp[2]
            im.px(k, y, c)
            im.px(w - 1 - k, y, c)
    # aşağı divar (qapı yeri boş)
    dx0, dx1 = w // 2 - 10, w // 2 + 9
    for x in range(w):
        if dx0 <= x <= dx1:
            continue
        for k in range(4):
            im.px(x, h - 1 - k, wall_ramp[1] if k == 0 else wall_ramp[2])
    # qapı ağzında xalçaça
    im.rect(dx0 + 2, h - 6, dx1 - 2, h - 1, P.RED[2])
    im.rect(dx0 + 3, h - 5, dx1 - 3, h - 2, P.RED[3])
    for x in range(dx0 + 4, dx1 - 3, 3):
        im.px(x, h - 4, P.YELLOW[3])


def _window(im, x, y, w, h, curtain):
    G = P.GLASS
    im.rect(x - 2, y - 2, x + w + 1, y + h + 1, P.WOOD[2])
    im.rect(x, y, x + w - 1, y + h - 1, G[4])
    for yy in range(y, y + h):
        t = (yy - y) / h
        im.hline(x, x + w - 1, yy, mix(G[5], G[3], t))
    im.vline(x + w // 2, y, y + h - 1, P.WOOD[3])
    im.line(x + 2, y + h - 3, x + 6, y + 1, G[6])
    # pərdələr
    for yy in range(y - 3, y + h + 3):
        for k in range(4):
            im.px(x - 4 + k, yy, curtain[3] if k % 2 else curtain[4])
            im.px(x + w + k, yy, curtain[3] if k % 2 else curtain[4])
    im.hline(x - 5, x + w + 4, y - 4, P.WOOD[1])


def sara_room():
    w, h = ROOM_W * 16, ROOM_H * 16
    im = Img(w, h)
    _planks(im, 0, WALL_H * 16, w - 1, h - 1, P.WOOD, 3)
    _walls(im, P.CREAM, 'floral')
    _window(im, 22, 8, 22, 16, P.PINK)
    _window(im, 148, 8, 22, 16, P.PINK)
    # divar saatı
    im.circle(96, 12, 6, P.WOOD[3]); im.circle(96, 12, 4.6, '#fff8e8')
    im.vline(96, 9, 12, P.BLACKISH[2]); im.hline(96, 98, 12, P.BLACKISH[2])
    # ailə şəkli
    im.rect(70, 8, 84, 20, P.GOLD[2]); im.rect(72, 10, 82, 18, P.SKIN['light'][3])
    im.rect(74, 12, 76, 16, P.HAIR['grey'][3]); im.rect(78, 12, 80, 16, P.HAIR['black'][3])
    # mətbəx rəfi və bankalar
    im.rect(110, 14, 136, 15, P.WOOD[3])
    for k, c in enumerate([P.RED, P.ORANGE, P.YELLOW, P.GREEN]):
        im.rect(112 + k * 6, 8, 115 + k * 6, 13, c[3]); im.hline(112 + k * 6, 115 + k * 6, 8, P.WHITE[4])
    return im


def market_room():
    w, h = ROOM_W * 16, ROOM_H * 16
    im = Img(w, h)
    _checker(im, 0, WALL_H * 16, w - 1, h - 1, '#e8e2d2', '#9cc8b8')
    _walls(im, P.MINT, 'stripes', base=P.STONE)
    # divardakı rəflər (mallar)
    r = rng(5)
    for sx in (10, 74, 138):
        for sy in (8, 20):
            im.rect(sx, sy + 6, sx + 44, sy + 7, P.WOOD[3])
            for xx in range(sx + 1, sx + 43, 4):
                c = r.choice([P.RED, P.YELLOW, P.GREEN, P.ORANGE, P.BLUE, P.PINK, P.PURPLE])
                im.rect(xx, sy, xx + 2, sy + 5, c[3]); im.hline(xx, xx + 2, sy, c[4])
    return im


# ---------------- mebel ----------------
def carpet():
    """Azərbaycan xalçası: qırmızı fon, mərkəzdə göllər, haşiyə"""
    im = Img(88, 56)
    R, B, Y, W = P.RED, P.BLUE, P.YELLOW, P.WHITE
    im.rect(0, 0, 87, 55, R[2])
    im.rect(2, 2, 85, 53, B[1])
    im.rect(4, 4, 83, 51, R[3])
    # haşiyə naxışı
    for x in range(6, 82, 6):
        im.px(x, 3, Y[3]); im.px(x + 1, 2, Y[3]); im.px(x, 52, Y[3]); im.px(x + 1, 53, Y[3])
    for y in range(6, 50, 6):
        im.px(3, y, Y[3]); im.px(84, y, Y[3])
    # göllər (romb medalyonlar)
    for cx in (24, 44, 64):
        for k in range(14, -1, -1):
            col = [B[2], Y[3], R[1], W[4], B[3]][k % 5]
            for dx in range(-k, k + 1):
                dy = k - abs(dx)
                im.px(cx + dx, 28 - dy, col)
                im.px(cx + dx, 28 + dy, col)
        im.px(cx, 28, W[5])
    # saçaqlar
    for y in range(0, 56, 2):
        im.px(0, y, W[4]); im.px(87, y, W[4])
    return im


def sofa():
    im = Img(52, 30)
    C = P.RED
    im.rect(2, 4, 49, 16, C[2]); im.hline(2, 49, 4, C[3])         # söykənək
    im.rect(4, 14, 47, 24, C[3]); im.hline(4, 47, 14, C[4])       # oturacaq
    im.rect(0, 8, 6, 26, C[2]); im.rect(45, 8, 51, 26, C[1])      # qoltuqlar
    im.hline(0, 6, 8, C[3]); im.hline(45, 51, 8, C[2])
    for x in (16, 34):
        im.vline(x, 15, 23, C[2])
    # yastıqlar
    im.rect(8, 7, 17, 14, P.YELLOW[3]); im.hline(8, 17, 7, P.YELLOW[4])
    im.rect(34, 7, 43, 14, P.TEAL[3]); im.hline(34, 43, 7, P.TEAL[4])
    for x in (3, 48):
        im.vline(x, 26, 29, P.WOOD[1])
    im.outline(col=P.OUTLINE)
    return im


def samovar_table():
    im = Img(24, 36)
    Wd, Gd = P.WOOD, P.GOLD
    im.rect(1, 20, 22, 23, Wd[4]); im.hline(1, 22, 20, Wd[5]); im.hline(1, 22, 23, Wd[2])
    for x in (3, 20):
        im.vline(x, 24, 35, Wd[2])
    # samovar
    im.rect(7, 6, 16, 19, Gd[3]); im.vline(7, 6, 19, Gd[4]); im.vline(16, 6, 19, Gd[1])
    im.hline(6, 17, 19, Gd[2]); im.hline(8, 15, 12, Gd[2])
    im.rect(9, 3, 14, 5, Gd[3]); im.rect(10, 0, 13, 2, P.WHITE[4])   # çaydan
    im.px(18, 12, Gd[2]); im.px(19, 13, Gd[2])   # kran
    im.px(5, 9, Gd[2]); im.px(18, 9, Gd[2])
    im.px(9, 8, '#ffffff')
    # armudu stəkan
    im.rect(2, 16, 4, 19, '#b8401c'); im.px(2, 15, '#ffffff')
    im.outline(col=P.OUTLINE)
    return im


def tv():
    """köhnə televizor tumbada"""
    im = Img(30, 34)
    Wd = P.WOOD
    im.rect(1, 20, 28, 33, Wd[3]); im.hline(1, 28, 20, Wd[5]); im.vline(28, 20, 33, Wd[1])
    im.rect(4, 24, 13, 31, Wd[2]); im.rect(16, 24, 25, 31, Wd[2])
    im.px(12, 27, P.GOLD[4]); im.px(17, 27, P.GOLD[4])
    im.rect(3, 2, 26, 19, P.METAL[2]); im.hline(3, 26, 2, P.METAL[4])
    im.rect(5, 4, 20, 17, P.GREEN[2])
    for y in range(4, 18, 2):
        im.hline(5, 20, y, P.GREEN[3])
    im.px(7, 6, '#ffffff'); im.px(8, 6, P.GREEN[5])
    im.px(23, 6, P.RED[3]); im.px(23, 9, P.METAL[4]); im.px(23, 12, P.METAL[4])
    im.line(10, 1, 7, -2, P.METAL[3]); im.line(18, 1, 21, -2, P.METAL[3])
    im.outline(col=P.OUTLINE)
    return im


def chest():
    """köhnə taxta sandıq"""
    im = Img(30, 22)
    Wd, M = P.WOOD, P.GOLD
    im.rect(1, 8, 28, 21, Wd[3]); im.hline(1, 28, 8, Wd[4]); im.vline(28, 8, 21, Wd[1])
    im.rect(1, 2, 28, 8, Wd[4]); im.hline(2, 27, 1, Wd[5]); im.hline(1, 28, 8, Wd[2])
    for x in (6, 23):
        im.vline(x, 1, 21, M[2]); im.vline(x + 1, 1, 21, M[3])
    im.rect(13, 7, 16, 11, M[3]); im.px(14, 9, P.BLACKISH[1])
    im.outline(col=P.OUTLINE)
    return im


def cat_bed():
    im = Img(22, 12)
    im.ellipse(11, 7, 10.5, 4.8, P.WOOD[2])
    im.ellipse(11, 6.5, 8.5, 3.2, P.PINK[3])
    im.ellipse(11, 6, 7, 2.2, P.PINK[4])
    im.outline(col=P.OUTLINE)
    return im


def bookshelf():
    im = Img(32, 42)
    Wd = P.WOOD
    im.rect(0, 0, 31, 41, Wd[2]); im.rect(2, 2, 29, 39, Wd[1])
    r = rng(9)
    for sy in (2, 15, 28):
        im.rect(2, sy + 11, 29, sy + 12, Wd[3])
        x = 3
        while x < 28:
            bw = r.choice([2, 3, 3, 4])
            c = r.choice([P.RED, P.BLUE, P.GREEN, P.YELLOW, P.PURPLE, P.ORANGE])
            hh = r.randint(7, 10)
            im.rect(x, sy + 11 - hh, min(28, x + bw - 1), sy + 10, c[3])
            im.vline(x, sy + 11 - hh, sy + 10, c[4])
            x += bw + (1 if r.random() < 0.3 else 0)
    im.outline(col=P.OUTLINE)
    return im


def plant():
    im = Img(18, 30)
    T = P.ORANGE
    im.rect(4, 21, 13, 29, T[2]); im.hline(3, 14, 21, T[3]); im.vline(13, 21, 29, T[1])
    L = P.LEAF
    for (cx, cy, r) in [(9, 9, 5), (5, 14, 4), (13, 14, 4), (9, 16, 4), (8, 4, 3)]:
        im.circle(cx, cy, r, L[4], fn=lambda x, y, nx, ny: L[5] if ny < -0.3 else L[4] if ny < 0.4 else L[3])
    im.vline(9, 17, 21, P.BARK[3])
    im.outline(col=L[0])
    return im


def shelf_unit():
    """marketdə ortada duran iki tərəfli rəf"""
    im = Img(50, 36)
    M = P.METAL
    im.rect(1, 4, 48, 33, M[4]); im.vline(1, 4, 33, M[5]); im.vline(48, 4, 33, M[2])
    r = rng(12)
    for sy in (6, 16, 26):
        im.hline(2, 47, sy + 8, M[2])
        for x in range(3, 46, 4):
            c = r.choice([P.RED, P.YELLOW, P.GREEN, P.ORANGE, P.BLUE, P.PINK, P.WHITE])
            im.rect(x, sy + 1, x + 2, sy + 7, c[3]); im.hline(x, x + 2, sy + 1, c[4])
    im.rect(0, 2, 49, 4, M[3]); im.hline(0, 49, 2, M[5])
    im.outline(col=P.OUTLINE)
    return im


def fridge():
    im = Img(28, 44)
    M, G = P.WHITE, P.GLASS
    im.rect(1, 1, 26, 42, M[4]); im.vline(26, 1, 42, M[2]); im.hline(1, 26, 1, M[5])
    im.rect(4, 5, 23, 38, G[3])
    r = rng(4)
    for sy in (7, 17, 27):
        im.hline(4, 23, sy + 9, M[3])
        for x in range(5, 22, 3):
            c = r.choice([P.RED, P.ORANGE, P.GREEN, P.BLUE])
            im.rect(x, sy + 2, x + 1, sy + 8, c[3]); im.px(x, sy + 1, M[4])
    im.line(6, 36, 12, 8, G[5])
    im.rect(2, 2, 25, 4, P.BLUE[3])
    im.outline(col=P.OUTLINE)
    return im


def counter():
    """kassa piştaxtası"""
    im = Img(56, 30)
    Wd = P.WOOD
    im.rect(0, 10, 55, 29, Wd[3]); im.hline(0, 55, 10, Wd[5]); im.hline(0, 55, 12, Wd[4])
    for x in range(4, 54, 10):
        im.rect(x, 15, x + 6, 26, Wd[2])
    # kassa aparatı
    im.rect(34, 0, 50, 10, P.METAL[2]); im.rect(36, 2, 48, 5, P.GREEN[4]); im.hline(34, 50, 0, P.METAL[4])
    for x in range(37, 48, 3):
        im.px(x, 7, P.METAL[5]); im.px(x, 9, P.METAL[5])
    # kiçik konfet qabı
    im.rect(8, 5, 16, 10, '#e8f0f8'); im.px(10, 7, P.RED[3]); im.px(13, 6, P.YELLOW[4]); im.px(11, 8, P.GREEN[4])
    im.outline(col=P.OUTLINE)
    return im


def crates():
    im = Img(34, 22)
    Wd = P.WOOD
    for k, fr in enumerate([P.GREEN, P.YELLOW]):
        x0 = 1 + k * 16
        im.rect(x0, 8, x0 + 14, 21, Wd[4]); im.hline(x0, x0 + 14, 8, Wd[5]); im.hline(x0, x0 + 14, 14, Wd[3])
        for j in range(3):
            for i in range(4):
                fx, fy = x0 + 1 + i * 3 + (j % 2), 2 + j * 2
                im.rect(fx, fy, fx + 1, fy + 1, fr[3]); im.px(fx, fy, fr[4])
    im.outline(col=P.OUTLINE)
    return im


ALL = {
    'room_sara': sara_room, 'room_market': market_room,
    'int_carpet': carpet, 'int_sofa': sofa, 'int_samovar': samovar_table, 'int_tv': tv,
    'int_chest': chest, 'int_catbed': cat_bed, 'int_bookshelf': bookshelf, 'int_plant': plant,
    'int_shelf': shelf_unit, 'int_fridge': fridge, 'int_counter': counter, 'int_crates': crates,
}
