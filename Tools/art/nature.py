# Təbiət: ağaclar, kollar. Yarpaq topaları 3D kimi işıqlandırılır (işıq yuxarı-soldan).
import math
from px import Img, rgb, mix, rng, darker, lighter, shadow_ellipse
import pal as P

LIGHT = (-0.55, -0.65, 0.52)


def _norm(v):
    l = math.sqrt(sum(c * c for c in v))
    return tuple(c / l for c in v)


LIGHT = _norm(LIGHT)


def canopy(im, blobs, ramp, seed=0, texture=True, fruit=None, fruit_n=0):
    """blobs: [(cx, cy, r)], arxadan önə. Hər piksel ən öndəki topanın normalına görə rənglənir."""
    r = rng(seed)
    n = len(ramp)
    for bi, (cx, cy, rad) in enumerate(blobs):
        depth = bi / max(1, len(blobs) - 1)
        for y in range(int(cy - rad - 1), int(cy + rad + 2)):
            for x in range(int(cx - rad - 1), int(cx + rad + 2)):
                nx, ny = (x + 0.5 - cx) / rad, (y + 0.5 - cy) / rad
                d2 = nx * nx + ny * ny
                if d2 > 1:
                    continue
                nz = math.sqrt(1 - d2)
                lam = nx * LIGHT[0] + ny * LIGHT[1] + nz * LIGHT[2]
                v = 0.15 + 0.85 * max(0, lam)
                v = v * (0.75 + 0.25 * depth)
                idx = 1 + v * (n - 2.2)
                # yarpaq teksturu: kiçik qruplar şəklində kölgə
                if texture:
                    h = ((x * 73856093) ^ (y * 19349663) ^ ((x + y // 2) // 2 * 83492791) ^ (seed * 2654435761)) % 17
                    hl = (((x - 1) * 73856093) ^ (y * 19349663) ^ ((x - 1 + y // 2) // 2 * 83492791) ^ (seed * 2654435761)) % 17
                    if h < 2 or hl < 2:
                        idx -= 1.0
                    elif h == 9 and v > 0.45:
                        idx += 0.8
                idx = int(max(1, min(n - 1, round(idx))))
                im.px(x, y, ramp[idx])
    # meyvələr
    for _ in range(fruit_n):
        cx, cy, rad = blobs[r.randrange(len(blobs))]
        a = r.random() * math.tau
        dd = r.random() * rad * 0.75
        fx, fy = int(cx + math.cos(a) * dd), int(cy + math.sin(a) * dd)
        if im.alpha(fx, fy) and im.alpha(fx + 1, fy + 1):
            im.px(fx, fy, fruit[3]); im.px(fx + 1, fy, fruit[2])
            im.px(fx, fy + 1, fruit[2]); im.px(fx + 1, fy + 1, fruit[1])
            im.px(fx, fy, fruit[4])


def trunk(im, cx, y_top, y_bot, w, B=None):
    B = B or P.BARK
    for y in range(y_top, y_bot + 1):
        flare = 0
        if y > y_bot - 3:
            flare = (y - (y_bot - 3))
        x0, x1 = cx - w // 2 - flare, cx + (w - 1) // 2 + flare
        for x in range(x0, x1 + 1):
            t = (x - x0) / max(1, x1 - x0)
            c = B[4] if t < 0.3 else B[3] if t < 0.7 else B[2]
            if (y * 3 + x) % 7 == 0:
                c = B[2]
            im.px(x, y, c)
    # köklər
    im.px(cx - w // 2 - 3, y_bot, B[2])
    im.px(cx + w // 2 + 3, y_bot, B[1])


def tree_oak(seed=1):
    im = Img(48, 64)
    trunk(im, 24, 34, 61, 8)
    # gövdə üzərində kölgə (yarpaqlar altında)
    for y in range(34, 40):
        for x in range(19, 30):
            if im.alpha(x, y):
                im.px(x, y, (30, 20, 30, 90))
    r = rng(seed)
    blobs = [(13, 30, 9), (35, 30, 9), (24, 33, 10), (10, 21, 9), (38, 21, 9),
             (17, 13, 10), (31, 13, 10), (24, 22, 12), (24, 9, 8)]
    blobs = [(x + r.randint(-1, 1), y + r.randint(-1, 1), rad) for x, y, rad in blobs]
    canopy(im, blobs, P.LEAF, seed)
    im.outline(col=P.LEAF[0])
    return im


def tree_pomegranate(seed=2):
    """nar ağacı: kiçik, yumru, qırmızı meyvəli"""
    im = Img(40, 52)
    trunk(im, 20, 30, 49, 6, P.BARK)
    im.line(20, 34, 13, 26, P.BARK[3]); im.line(21, 34, 28, 27, P.BARK[2])
    r = rng(seed)
    blobs = [(11, 24, 8), (29, 24, 8), (20, 27, 8), (13, 14, 9), (27, 14, 9), (20, 18, 10), (20, 8, 7)]
    canopy(im, blobs, P.LEAF, seed + 3, fruit=P.RED, fruit_n=11)
    im.outline(col=P.LEAF[0])
    return im


def tree_cypress(seed=3):
    """sərv: hündür, ensiz"""
    im = Img(24, 64)
    trunk(im, 12, 54, 61, 4)
    blobs = []
    for i in range(9):
        y = 54 - i * 6
        rad = 7.5 - abs(i - 3) * 0.55 if i >= 3 else 6.5 + i * 0.4
        rad = max(3.5, rad - (0 if i < 6 else (i - 5) * 1.0))
        blobs.append((12 + (1 if i % 2 else -1) * 0.5, y, rad))
    canopy(im, blobs, ['#10261e', '#173624', '#20482c', '#2c5e34', '#3b763c', '#4f9044', '#6aaa4e'], seed)
    im.outline(col='#0c1e18')
    return im


def tree_blossom(seed=4):
    """çiçəkli ağac (çəhrayı)"""
    im = Img(48, 60)
    trunk(im, 24, 32, 57, 7)
    r = rng(seed)
    blobs = [(13, 28, 9), (35, 28, 9), (24, 31, 9), (11, 19, 9), (37, 19, 9), (18, 11, 9), (30, 11, 9), (24, 20, 11)]
    canopy(im, blobs, P.LEAF_PINK, seed)
    for _ in range(26):
        x, y = r.randrange(6, 42), r.randrange(4, 36)
        if im.alpha(x, y):
            im.px(x, y, '#ffffff' if r.random() < 0.5 else P.LEAF_PINK[6])
    im.outline(col=P.LEAF_PINK[0])
    return im


def bush(seed=5, flowers=None, w=20, h=16):
    im = Img(w, h)
    r = rng(seed)
    blobs = [(5, 10, 5), (15, 10, 5), (10, 11, 6), (7, 6, 5), (13, 6, 5)]
    canopy(im, blobs, P.LEAF, seed)
    if flowers:
        for _ in range(9):
            x, y = r.randrange(2, w - 2), r.randrange(2, h - 3)
            if im.alpha(x, y) and im.alpha(x + 1, y):
                im.px(x, y, flowers[4]); im.px(x + 1, y, flowers[3]); im.px(x, y + 1, flowers[3])
                im.px(x, y - 1, flowers[5])
    im.outline(col=P.LEAF[0])
    return im


def flowerbed(seed=6, w=16, h=12):
    """kiçik gül ləki (yerə qoyulur)"""
    im = Img(w, h)
    r = rng(seed)
    for _ in range(14):
        x, y = r.randrange(1, w - 1), r.randrange(3, h - 1)
        col = r.choice([P.RED, P.YELLOW, P.PINK, P.WHITE, P.PURPLE])
        im.vline(x, y, y + 2, P.LEAF[3])
        im.px(x - 1, y + 1, P.LEAF[4])
        im.px(x, y - 1, col[4]); im.px(x - 1, y, col[3]); im.px(x + 1, y, col[3]); im.px(x, y, P.YELLOW[4])
    im.outline(col=P.LEAF[1])
    return im


ALL = {
    'tree_oak': tree_oak,
    'tree_pomegranate': tree_pomegranate,
    'tree_cypress': tree_cypress,
    'tree_blossom': tree_blossom,
}
