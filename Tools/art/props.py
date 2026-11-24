# Küçə əşyaları, maşınlar, heyvanlar, götürülən əşyalar və UI ikonları.
import math
from px import Img, rgb, mix, rng, darker, lighter, shadow_ellipse
import pal as P
from font import draw_text


# ---------------- küçə əşyaları ----------------
def lamp(on=False):
    """klassik Bakı küçə fənəri (dəmir dirək, fənər başlığı)"""
    im = Img(14, 42)
    M = P.METAL
    cx = 7
    # oturacaq
    im.rect(cx - 3, 38, cx + 3, 41, M[2]); im.hline(cx - 3, cx + 3, 38, M[4]); im.vline(cx + 3, 38, 41, M[1])
    im.rect(cx - 2, 35, cx + 2, 37, M[2]); im.hline(cx - 2, cx + 2, 35, M[3])
    # dirək
    for y in range(12, 35):
        im.px(cx - 1, y, M[4]); im.px(cx, y, M[2]); im.px(cx + 1, y, M[1])
    for y in (20, 28):
        im.hline(cx - 2, cx + 2, y, M[3])
    # fənər
    glass = P.NIGHT_GLOW if on else ['#3a4a5a', '#56708a', '#86a4bc', '#c4dcea']
    im.rect(cx - 4, 2, cx + 4, 11, M[1])
    im.rect(cx - 3, 4, cx + 3, 10, glass[2])
    im.vline(cx - 3, 4, 10, glass[3]); im.vline(cx + 3, 4, 10, glass[1])
    im.vline(cx, 4, 10, M[2])
    if on:
        im.rect(cx - 2, 6, cx + 2, 9, glass[3])
    im.poly([(cx - 5, 3), (cx, -1), (cx + 6, 3)], M[2])
    im.hline(cx - 5, cx + 5, 2, M[3])
    im.px(cx, 0, M[4])
    im.hline(cx - 3, cx + 3, 11, M[2])
    im.outline(col=P.OUTLINE)
    return im


def bench():
    """park skamyası: taxta oturacaq və söykənəcək, dəmir ayaqlar"""
    im = Img(32, 22)
    Wd, M = P.WOOD, P.METAL
    # söykənəcək
    for k, y in enumerate((3, 6, 9)):
        im.rect(2, y, 29, y + 1, Wd[4] if k == 0 else Wd[3])
        im.hline(2, 29, y + 1, Wd[2])
    # oturacaq
    for k, y in enumerate((12, 14, 16)):
        im.rect(1, y, 30, y + 1, Wd[5] if k == 0 else Wd[4])
        im.hline(1, 30, y + 1, Wd[2])
    # yan dəmirlər
    for x in (2, 28):
        im.vline(x, 2, 20, M[2]); im.vline(x + 1, 2, 20, M[1])
        im.px(x, 2, M[4])
    im.hline(1, 4, 11, M[2]); im.hline(27, 30, 11, M[2])
    im.outline(col=P.OUTLINE)
    return im


def trash_bin():
    im = Img(12, 17)
    G = P.GREEN
    im.rect(1, 4, 10, 15, G[2])
    for x in range(2, 10, 2):
        im.vline(x, 5, 14, G[3])
    im.vline(1, 4, 15, G[3]); im.vline(10, 4, 15, G[1])
    im.rect(0, 2, 11, 4, G[3]); im.hline(0, 11, 2, G[4]); im.hline(0, 11, 4, G[1])
    im.rect(4, 0, 7, 1, P.METAL[3])
    im.hline(2, 9, 15, G[0])
    im.outline(col=P.OUTLINE)
    return im


def fountain(frame):
    """daş hovuzlu fəvvarə, 3 kadr su animasiyası"""
    im = Img(48, 44)
    S = P.STONE
    Wt = P.WATER
    # hovuz (ellips)
    im.ellipse(24, 32, 23, 11, S[2])
    im.ellipse(24, 31, 23, 10.5, S[4])
    im.ellipse(24, 30, 22, 9.5, S[5])
    # su
    im.ellipse(24, 31, 19, 7.5, Wt[2])
    im.ellipse(24, 30.5, 18, 6.5, Wt[3])
    r = rng(frame)
    for k in range(10):
        a = (k / 10) * math.tau + frame * 0.6
        rx, ry = 11 + (k % 3) * 2.5, 4 + (k % 3)
        x, y = int(24 + math.cos(a) * rx), int(30.5 + math.sin(a) * ry)
        im.px(x, y, Wt[5]); im.px(x + 1, y, Wt[4])
    # dalğa halqası
    rr = 5 + frame * 3
    for k in range(24):
        a = k / 24 * math.tau
        x, y = int(24 + math.cos(a) * rr), int(30 + math.sin(a) * rr * 0.4)
        im.px(x, y, Wt[5])
    # hovuz kənarının ön hissəsi
    for x in range(1, 47):
        nx = (x + 0.5 - 24) / 23
        if abs(nx) < 1:
            y = int(32 + 11 * math.sqrt(1 - nx * nx))
            im.px(x, y - 1, S[3]); im.px(x, y, S[1])
    # mərkəzi sütun
    im.rect(21, 14, 26, 31, S[4]); im.vline(21, 14, 31, S[5]); im.vline(26, 14, 31, S[2])
    im.ellipse(23.5, 15, 7, 2.5, S[5]); im.ellipse(23.5, 15.5, 5, 1.5, Wt[3])
    im.hline(20, 27, 31, S[2])
    # fışqıran su
    h = [8, 10, 9][frame]
    for y in range(15 - h, 15):
        im.px(23, y, Wt[5]); im.px(24, y, Wt[6])
    top = 15 - h
    spread = [(-1, 0), (-2, 1), (-3, 2), (-4, 4), (-5, 6), (-6, 9), (-6, 12)]
    for dx, dy in spread:
        im.px(23 + dx, top + dy + frame % 2, Wt[5])
        im.px(24 - dx, top + dy + (frame + 1) % 2, Wt[5])
    for k in range(5):
        im.px(r.randrange(15, 33), r.randrange(top, 18), Wt[6])
    im.outline(col=P.OUTLINE)
    return im


def car(Rmp, kind='sedan'):
    """yandan görünüş, sağa baxır; 40x24"""
    im = Img(40, 24)
    M = P.METAL
    G = P.GLASS
    body_top = 11 if kind != 'taxi' else 9
    # gövdə
    for y in range(body_top + 3, 20):
        x0 = 1 if y > body_top + 4 else 3
        im.hline(x0, 38 - (0 if y > body_top + 4 else 1), y, Rmp[3])
    im.hline(2, 38, body_top + 3, Rmp[4])
    im.hline(1, 38, 19, Rmp[1])
    im.hline(1, 38, 18, Rmp[2])
    # kabin
    if kind == 'taxi':
        # London tipli bənövşəyi Bakı taksisi: hündür kabin
        im.rect(6, body_top - 6, 31, body_top + 3, Rmp[3])
        im.hline(7, 30, body_top - 6, Rmp[4])
        im.rect(8, body_top - 4, 17, body_top + 2, G[3]); im.rect(19, body_top - 4, 29, body_top + 2, G[3])
        im.line(9, body_top + 1, 13, body_top - 3, G[5]); im.line(20, body_top + 1, 24, body_top - 3, G[5])
        im.rect(15, body_top - 9, 22, body_top - 7, P.YELLOW[4]); im.hline(15, 22, body_top - 7, P.YELLOW[2])
    else:
        pts = [(8, body_top + 3), (12, body_top - 4), (27, body_top - 4), (32, body_top + 3)]
        im.poly(pts, Rmp[3])
        im.hline(12, 27, body_top - 4, Rmp[4])
        im.poly([(10, body_top + 3), (13, body_top - 2), (19, body_top - 2), (19, body_top + 3)], G[3])
        im.poly([(21, body_top + 3), (21, body_top - 2), (26, body_top - 2), (30, body_top + 3)], G[3])
        im.line(12, body_top + 2, 15, body_top - 1, G[5]); im.line(22, body_top + 2, 24, body_top, G[5])
        if kind == 'van':
            im.rect(19, body_top - 4, 37, body_top + 3, Rmp[3]); im.hline(19, 37, body_top - 4, Rmp[4])
    # qapı xətti və tutacaq
    im.vline(20, body_top + 4, 18, Rmp[2])
    im.hline(23, 25, body_top + 6, M[4])
    im.hline(14, 16, body_top + 6, M[4])
    # faralar
    im.rect(37, body_top + 5, 38, body_top + 7, P.YELLOW[5])
    im.rect(1, body_top + 5, 2, body_top + 7, P.RED[3])
    im.rect(36, 16, 39, 17, M[4]); im.rect(0, 16, 3, 17, M[4])  # bamperlər
    # təkərlər
    for wx in (9, 30):
        im.circle(wx + 0.5, 19.5, 4, M[0])
        im.circle(wx + 0.5, 19.5, 2.2, M[3])
        im.px(wx, 19, M[5])
    im.outline(col=P.OUTLINE)
    return im


def car_light():
    """gecə farasının işıq konusu (sağa)"""
    im = Img(28, 14)
    for x in range(28):
        spread = 1 + x * 0.22
        for y in range(14):
            d = abs(y - 6.5)
            if d <= spread:
                a = int(110 * (1 - x / 28) * (1 - d / (spread + 1)))
                im.px(x, y, (255, 236, 170, max(0, a)))
    return im


def shadow():
    return shadow_ellipse(16, 6, 80)


# ---------------- götürülən əşyalar ----------------
def coin(frame):
    im = Img(12, 12)
    Gd = P.GOLD
    ws = [5, 3, 1, 3][frame]
    cx = 6
    for y in range(1, 11):
        dy = (y - 5.5) / 5
        half = int(round(ws * math.sqrt(max(0, 1 - dy * dy)) + 0.2))
        for x in range(cx - half, cx + half):
            t = (x - (cx - half)) / max(1, 2 * half - 1)
            c = Gd[5] if t < 0.3 else Gd[4] if t < 0.75 else Gd[3]
            im.px(x, y, c)
    if ws >= 3:
        im.vline(cx - 1, 4, 7, Gd[2]) if ws == 5 else None
        im.px(cx - 2, 3, '#ffffff')
    im.outline(col=Gd[1])
    return im


def letter():
    im = Img(13, 10)
    W_ = P.WHITE
    im.rect(1, 1, 11, 8, W_[5])
    im.hline(1, 11, 8, W_[3]); im.vline(11, 1, 8, W_[3])
    im.line(1, 1, 6, 5, W_[2]); im.line(11, 1, 6, 5, W_[2])
    im.px(6, 5, P.RED[3]); im.px(5, 5, P.RED[3]); im.px(6, 6, P.RED[2]); im.px(7, 5, P.RED[2])
    im.outline(col=P.OUTLINE)
    return im


def tea():
    """armudu stəkanda çay, nəlbəki ilə"""
    im = Img(12, 14)
    im.ellipse(6, 12, 5.5, 1.6, P.WHITE[4])
    im.hline(1, 11, 12, P.WHITE[3])
    rows = {2: (4, 7), 3: (4, 7), 4: (5, 6), 5: (5, 6), 6: (4, 7), 7: (3, 8), 8: (3, 8), 9: (3, 8), 10: (4, 7)}
    for y, (a, b) in rows.items():
        for x in range(a, b + 1):
            c = '#b8401c' if y > 3 else '#e8d8c8'
            if x == a:
                c = '#e86a30' if y > 3 else '#ffffff'
            im.px(x, y, c)
    im.hline(4, 7, 1, '#f4f0ff')
    im.px(4, 7, '#ffd0a0')
    im.outline(col=P.OUTLINE)
    return im


def prompt_e():
    im = Img(12, 12)
    im.rect(1, 1, 10, 10, P.WHITE[3])
    im.rect(1, 1, 10, 8, P.WHITE[5])
    im.hline(2, 9, 1, '#ffffff')
    for x, y in [(4, 3), (5, 3), (6, 3), (7, 3), (4, 4), (4, 5), (5, 5), (6, 5), (4, 6), (4, 7), (5, 7), (6, 7), (7, 7)]:
        im.px(x, y, P.BLACKISH[2])
    im.outline(col=P.OUTLINE)
    return im


def marker(sym):
    im = Img(12, 16)
    col = P.YELLOW if sym == '!' else P.BLUE
    im.rect(1, 1, 10, 11, col[4])
    im.hline(1, 10, 1, col[5]); im.hline(1, 10, 11, col[2]); im.vline(10, 1, 11, col[3])
    im.erase(1, 1); im.erase(10, 1); im.erase(1, 11); im.erase(10, 11)
    im.pts([(5, 12), (6, 12), (5, 13)], col[3])
    d = P.BLACKISH[1]
    if sym == '!':
        im.vline(5, 3, 7, d); im.vline(6, 3, 7, d); im.px(5, 9, d); im.px(6, 9, d)
    else:
        im.pts([(4, 4), (5, 3), (6, 3), (7, 4), (7, 5), (6, 6), (5, 7), (5, 9)], d)
    im.outline(col=P.OUTLINE)
    return im


# ---------------- nərd masası və babalar ----------------
def old_man(face_right, cap, top, hand_up, sk=None):
    im = Img(16, 24)
    sk = sk or P.SKIN['old']
    # oturmuş bədən (yan görünüş)
    im.rect(5, 12, 10, 18, top[3]); im.vline(5, 12, 18, top[4]); im.vline(10, 12, 18, top[2])
    # ayaqlar (oturub)
    im.rect(6, 18, 12, 19, P.STONE[2]); im.rect(11, 19, 12, 22, P.STONE[2]); im.hline(11, 13, 23, P.BLACKISH[2])
    # baş
    for y, (a, b) in {4: (6, 10), 5: (5, 11), 6: (5, 12), 7: (5, 12), 8: (5, 11), 9: (6, 10), 10: (6, 10)}.items():
        im.hline(a, b, y, sk[3]); im.px(b, y, sk[2])
    im.px(10, 6, '#24161e'); im.px(12, 7, sk[2])
    im.hline(9, 12, 8, P.HAIR['grey'][3])   # bığ
    im.rect(4, 5, 6, 9, P.HAIR['grey'][3])
    # kepka
    im.rect(4, 2, 11, 4, cap[3]); im.hline(5, 10, 2, cap[4]); im.hline(9, 13, 4, cap[1])
    # qol
    if hand_up:
        im.pts([(9, 12), (10, 11), (11, 10)], top[2]); im.px(12, 9, sk[3])
    else:
        im.pts([(8, 13), (9, 14), (10, 15)], top[2]); im.px(11, 15, sk[3])
    im.outline(col=P.OUTLINE)
    return im if face_right else im.flipx()


def nerd_scene(frame):
    im = Img(56, 34)
    Wd = P.WOOD
    # taburelər
    for sx in (3, 45):
        im.rect(sx, 27, sx + 7, 28, Wd[3]); im.vline(sx + 1, 29, 33, Wd[2]); im.vline(sx + 6, 29, 33, Wd[1])
    # masa
    im.rect(15, 18, 40, 21, Wd[4]); im.hline(15, 40, 18, Wd[5]); im.hline(15, 40, 21, Wd[2])
    im.vline(17, 22, 33, Wd[2]); im.vline(38, 22, 33, Wd[1]); im.vline(18, 22, 33, Wd[3])
    # nərd taxtası
    im.rect(18, 13, 37, 18, Wd[2]); im.rect(19, 14, 36, 17, '#f0d8a8')
    im.vline(27, 13, 18, Wd[1]); im.vline(28, 13, 18, Wd[1])
    for i in range(4):
        im.px(20 + i * 2, 15, P.RED[3]); im.px(30 + i * 2, 16, P.BLACKISH[3])
    im.px(22, 16, '#ffffff'); im.px(33, 15, '#ffffff'); im.px(24, 16, P.BLACKISH[3]); im.px(31, 15, '#ffffff')
    # zərlər
    if frame == 1:
        im.rect(25, 9, 26, 10, '#ffffff'); im.rect(29, 10, 30, 11, '#ffffff')
    else:
        im.rect(25, 15, 26, 16, '#ffffff'); im.rect(29, 15, 30, 16, '#ffffff')
    a = old_man(True, P.STONE, P.BLUE, hand_up=(frame == 1))
    b = old_man(False, P.WOOD, P.GREEN, hand_up=(frame == 0))
    im.paste(a, 1, 8)
    im.paste(b, 39, 8)
    im.outline(col=P.OUTLINE)
    return im


# ---------------- heyvanlar ----------------
def cat(frame, mode):
    """Mestan: narıncı zolaqlı pişik. mode: 'idle' (oturub) | 'walk'"""
    im = Img(16, 16)
    O = P.ORANGE
    if mode == 'idle':
        im.rect(5, 8, 10, 14, O[3]); im.vline(10, 8, 14, O[2]); im.hline(5, 10, 14, O[2])
        im.rect(6, 10, 8, 14, '#fff0dc')
        for y in (9, 11):
            im.px(9, y, O[1]); im.px(10, y, O[1])
        # baş
        im.rect(4, 3, 10, 8, O[3]); im.hline(4, 10, 3, O[4]); im.vline(10, 3, 8, O[2])
        im.pts([(4, 2), (5, 1), (9, 1), (10, 2)], O[3]); im.px(5, 2, P.PINK[4]); im.px(9, 2, P.PINK[4])
        im.px(6, 5, '#24161e'); im.px(8, 5, '#24161e'); im.px(7, 6, P.PINK[3])
        im.px(6, 3, O[1]); im.px(7, 3, O[1]); im.px(8, 3, O[1])
        # quyruq
        tail = [(11, 14), (12, 13), (13, 12), (13, 11)] if frame == 0 else [(11, 14), (12, 14), (13, 13), (14, 12)]
        for x, y in tail:
            im.px(x, y, O[3])
        im.px(*tail[-1], O[1])
    else:
        lift = frame
        im.rect(3, 7, 11, 11, O[3]); im.hline(3, 11, 7, O[4]); im.hline(3, 11, 11, O[2])
        for x in (5, 8):
            im.vline(x, 7, 9, O[1])
        im.rect(10, 4, 14, 8, O[3]); im.hline(10, 14, 4, O[4])
        im.px(10, 3, O[3]); im.px(14, 3, O[3]); im.px(13, 6, '#24161e'); im.px(14, 7, P.PINK[3])
        legs = [(4, 12), (5, 13), (10, 12), (9, 13)] if lift == 0 else [(3, 12), (6, 13), (11, 12), (8, 13)]
        for x, y in legs:
            im.vline(x, 12, y, O[2])
        im.pts([(2, 7), (1, 6), (1, 5), (2, 4)] if lift == 0 else [(2, 7), (1, 6), (0, 6), (0, 5)], O[3])
    im.outline(col='#3a1a10')
    return im


def dog(frame, mode):
    """kiçik qəhvəyi it (yandan, sağa)"""
    im = Img(16, 14)
    Bn = P.WOOD
    im.rect(3, 6, 11, 9, Bn[4]); im.hline(3, 11, 6, Bn[5]); im.hline(3, 11, 9, Bn[3])
    im.rect(10, 2, 14, 6, Bn[4]); im.hline(10, 14, 2, Bn[5]); im.px(15, 5, Bn[2]); im.px(15, 4, P.BLACKISH[1])
    im.px(13, 3, '#24161e'); im.rect(10, 2, 11, 5, Bn[2])
    if mode == 'walk':
        legs = [(4, 12), (6, 11), (9, 12), (11, 11)] if frame == 0 else [(4, 11), (6, 12), (9, 11), (11, 12)]
    else:
        legs = [(4, 12), (6, 12), (9, 12), (11, 12)]
    for x, y in legs:
        im.vline(x, 10, y, Bn[3])
    tail = [(2, 5), (1, 4)] if frame == 0 else [(2, 5), (2, 3)]
    im.pts(tail, Bn[4])
    im.hline(11, 12, 7, P.RED[3])  # boyunbağı
    im.outline(col='#2a160e')
    return im


def pigeon(frame):
    """göyərçin: 0 dayanır, 1 dənləyir, 2-3 uçur"""
    im = Img(10, 10)
    G = P.STONE
    if frame < 2:
        im.rect(2, 4, 6, 7, G[3]); im.hline(2, 6, 4, G[4]); im.px(1, 5, G[2])
        hy = 2 if frame == 0 else 5
        im.rect(6, hy, 8, hy + 2, G[3]); im.px(7, hy + 1, '#24161e'); im.px(9, hy + 1, P.ORANGE[3])
        im.px(6, hy + 2, P.TEAL[4]) if frame == 0 else None
        im.pts([(3, 8), (5, 8)], P.ORANGE[2])
        im.px(4, 5, G[2]); im.px(5, 6, G[2])
    else:
        im.rect(3, 4, 7, 6, G[3]); im.px(8, 4, G[3]); im.px(8, 5, '#24161e')
        if frame == 2:
            im.pts([(2, 1), (3, 2), (4, 3), (6, 3), (7, 2), (8, 1)], G[4])
        else:
            im.pts([(1, 6), (2, 6), (3, 6), (7, 7), (8, 7), (9, 7)], G[4])
    im.outline(col=P.OUTLINE)
    return im


def butterfly(frame, col):
    im = Img(7, 6)
    if frame == 0:
        im.pts([(0, 0), (1, 0), (0, 1), (1, 1), (2, 2), (5, 0), (6, 0), (5, 1), (6, 1), (4, 2), (1, 3), (5, 3)], col[4])
        im.vline(3, 1, 4, P.BLACKISH[2])
    else:
        im.pts([(2, 1), (2, 2), (4, 1), (4, 2), (2, 3), (4, 3)], col[3])
        im.vline(3, 1, 4, P.BLACKISH[2])
    return im


def firefly():
    im = Img(5, 5)
    for y in range(5):
        for x in range(5):
            d = math.hypot(x - 2, y - 2)
            if d <= 2.2:
                a = int(255 * max(0, 1 - d / 2.4))
                im.px(x, y, (230, 255, 140, a))
    im.px(2, 2, '#ffffff')
    return im


def glow(size=32, col=(255, 210, 120)):
    """yumşaq işıq ləkəsi (gecə fənərləri altında yerə düşür)"""
    im = Img(size, size // 2)
    for y in range(size // 2):
        for x in range(size):
            nx, ny = (x + 0.5 - size / 2) / (size / 2), (y + 0.5 - size / 4) / (size / 4)
            d = nx * nx + ny * ny
            if d < 1:
                im.px(x, y, (*col, int(90 * (1 - d) ** 1.5)))
    return im


# ---------------- UI ikonları (16x16) ----------------
def heart(full):
    im = Img(13, 12)
    shape = ["..###...###..", ".#####.#####.", "#############", "#############", "#############", ".###########.", "..#########..", "...#######...", "....#####....", ".....###.....", "......#......"]
    R = P.RED if full else P.STONE
    for y, row in enumerate(shape):
        for x, ch in enumerate(row):
            if ch == '#':
                c = R[3]
                if y > 5 or x > 9:
                    c = R[2]
                im.px(x, y, c)
    im.pts([(2, 1), (3, 1), (1, 2), (1, 3)], R[5] if full else R[4])
    im.px(2, 2, '#ffffff' if full else R[5])
    im.outline(col=P.OUTLINE)
    return im


def coin_icon():
    im = Img(14, 14)
    Gd = P.GOLD
    im.circle(7, 7, 6, Gd[3])
    im.circle(6.5, 6.5, 5, Gd[4])
    im.circle(7, 7, 3.5, Gd[3])
    draw_text(im, 'M', 5, 5, Gd[1]) if False else None
    im.pts([(5, 5), (5, 6), (5, 7), (5, 8), (9, 5), (9, 6), (9, 7), (9, 8), (6, 6), (8, 6), (7, 7)], Gd[1])
    im.px(4, 3, '#ffffff')
    im.outline(col=Gd[0])
    return im


def charm_icon():
    """göz muncuğu (nazar)"""
    im = Img(14, 14)
    im.circle(7, 7, 6.5, P.BLUE[2])
    im.circle(7, 7, 5, P.BLUE[3])
    im.circle(7, 7, 3.6, '#ffffff')
    im.circle(7, 7, 2.4, P.BLUE[4])
    im.circle(7, 7, 1.3, P.BLACKISH[1])
    im.px(5, 4, '#ffffff'); im.px(3, 5, P.BLUE[5])
    im.outline(col=P.BLUE[0])
    return im


def icecream_icon():
    im = Img(12, 16)
    for y in range(8, 15):
        half = max(0, (15 - y) // 2)
        im.hline(6 - half, 5 + half, y, P.WOOD[4] if (y % 2) else P.WOOD[3])
    im.circle(6, 6, 4.2, P.PINK[4])
    im.circle(5.5, 5, 2.4, P.PINK[5])
    im.circle(6, 2.5, 2, '#fff4e8')
    im.px(7, 0, P.RED[3]); im.px(6, 1, P.RED[3])
    im.outline(col=P.OUTLINE)
    return im


def juice_icon():
    """nar şirəsi stəkanı"""
    im = Img(12, 16)
    im.rect(2, 3, 9, 14, '#e4eef8')
    im.rect(3, 6, 8, 13, P.RED[2]); im.vline(3, 6, 13, P.RED[3]); im.hline(3, 8, 6, P.RED[4])
    im.vline(7, 0, 8, P.YELLOW[4]); im.px(8, 0, P.YELLOW[4])
    im.hline(2, 9, 14, P.WHITE[3])
    im.outline(col=P.OUTLINE)
    return im


def shoes_icon():
    im = Img(16, 12)
    for ox in (0, 7):
        im.rect(ox + 1, 4, ox + 7, 8, P.RED[3]); im.rect(ox + 1, 2, ox + 4, 4, P.RED[3])
        im.hline(ox + 1, ox + 8, 9, '#ffffff'); im.hline(ox + 1, ox + 8, 10, P.WHITE[3])
        im.px(ox + 3, 4, '#ffffff'); im.px(ox + 4, 5, '#ffffff'); im.hline(ox + 1, ox + 4, 2, P.RED[4])
    im.outline(col=P.OUTLINE)
    return im


def sun_icon():
    im = Img(16, 16)
    im.circle(8, 8, 4.2, P.YELLOW[4]); im.circle(7.5, 7.5, 2.5, P.YELLOW[5])
    for a in range(8):
        ang = a / 8 * math.tau
        im.px(8 + math.cos(ang) * 6.5, 8 + math.sin(ang) * 6.5, P.ORANGE[4])
    im.outline(col=P.ORANGE[1])
    return im


def moon_icon():
    im = Img(16, 16)
    im.circle(8, 8, 5.5, '#f4eccc')
    im.circle(10.5, 6.5, 4.5, (0, 0, 0, 0))
    for y in range(16):
        for x in range(16):
            if math.hypot(x + 0.5 - 10.5, y + 0.5 - 6.5) < 4.5:
                im.erase(x, y)
    im.px(5, 9, '#d8cca0'); im.px(6, 11, '#d8cca0')
    im.outline(col=P.BLUE[1])
    return im


def map_you():
    im = Img(14, 12)
    pts = [(1, 1), (12, 1), (7, 10), (6, 10)]
    im.poly([(1, 1), (13, 1), (7, 10)], P.YELLOW[4])
    im.hline(2, 11, 2, '#ffffff')
    im.outline(col=P.OUTLINE)
    return im


def cat_icon():
    return cat(0, 'idle')


def nine_slice(kind):
    """UI çərçivələri (24x24, kənar 8 piksel): taxta panel və perqament dialoq"""
    im = Img(24, 24)
    Wd = P.WOOD if kind != 'slot_sel' else P.GOLD
    if kind == 'dialog':
        fill = ['#c89a5c', '#e2bc80', '#f2d8a4', '#fbecc8']
    elif kind == 'panel':
        fill = ['#2e1c18', '#3e2620', '#4a2e24', '#5a382a']
    else:
        fill = ['#e2bc80', '#f6dca8', '#fff0cc', '#fffae8']
    # xarici taxta çərçivə
    im.rect(0, 0, 23, 23, P.OUTLINE)
    im.rect(1, 1, 22, 22, Wd[2])
    im.rect(2, 2, 21, 21, Wd[4])
    im.hline(2, 21, 2, Wd[5]); im.vline(2, 2, 21, Wd[5])
    im.hline(2, 21, 21, Wd[2]); im.vline(21, 2, 21, Wd[2])
    im.rect(4, 4, 19, 19, Wd[1])
    im.rect(5, 5, 18, 18, fill[2])
    im.hline(5, 18, 5, fill[1]); im.vline(5, 5, 18, fill[1])
    im.hline(5, 18, 6, fill[3] if kind != 'panel' else fill[1])
    # künc perçinləri
    for x, y in ((2, 2), (20, 2), (2, 20), (20, 20)):
        im.px(x, y, P.GOLD[4]); im.px(x + 1, y + 1, P.GOLD[2])
    for c in ((0, 0), (23, 0), (0, 23), (23, 23)):
        im.erase(*c)
    return im


def fruit_stand():
    """market qarşısında meyvə piştaxtası: taxta yeşiklərdə alma, portağal, nar"""
    im = Img(34, 22)
    Wd = P.WOOD
    # ayaqlar və rəf
    im.rect(1, 12, 32, 15, Wd[3]); im.hline(1, 32, 12, Wd[5]); im.hline(1, 32, 15, Wd[1])
    for x in (2, 31):
        im.vline(x, 16, 21, Wd[2])
    im.hline(2, 31, 19, Wd[2])
    # yeşiklər
    fruits = [P.RED, P.ORANGE, ['#3a0a18', '#6a1428', '#9a1e34', '#c8304a', '#e85068', '#ff8a9a']]
    for k, fr in enumerate(fruits):
        x0 = 2 + k * 10
        im.rect(x0, 6, x0 + 9, 12, Wd[4]); im.hline(x0, x0 + 9, 6, Wd[5]); im.vline(x0 + 9, 6, 12, Wd[2])
        im.hline(x0, x0 + 9, 9, Wd[3])
        for j in range(4):
            for i in range(3):
                fx, fy = x0 + 1 + i * 3, 2 + (j % 2) * 2 + (1 if j > 1 else 0)
                if j > 1:
                    fx += 1
                im.rect(fx, fy, fx + 1, fy + 1, fr[3]); im.px(fx, fy, fr[4]); im.px(fx + 1, fy + 1, fr[2])
    # kiçik qiymət lövhəsi
    im.rect(13, 14, 20, 17, '#fff4dc'); im.hline(14, 19, 15, P.BLACKISH[3])
    im.outline(col=P.OUTLINE)
    return im


# ---------------- çanta əşyaları (v3) ----------------
def ball_icon():
    """qırmızı-ağ top"""
    im = Img(16, 16)
    im.circle(8, 8, 6.5, P.RED[3])
    im.circle(7.5, 7.5, 5.2, P.RED[4])
    for y in range(16):
        for x in range(16):
            if im.alpha(x, y) and abs((x - 8) + (y - 8) * 0.4) < 1.6:
                im.px(x, y, '#ffffff')
    im.px(5, 4, '#ffffff'); im.px(6, 4, P.RED[5])
    im.outline(col=P.OUTLINE)
    return im


def ball_world():
    im = Img(8, 8)
    im.circle(4, 4, 3.4, P.RED[3])
    im.circle(3.6, 3.6, 2.4, P.RED[4])
    im.px(3, 4, '#ffffff'); im.px(4, 4, '#ffffff'); im.px(5, 3, '#ffffff'); im.px(2, 2, '#ffffff')
    im.outline(col=P.OUTLINE)
    return im


def bread_icon():
    """tandır çörəyi (oval, üstü naxışlı)"""
    im = Img(16, 16)
    im.ellipse(8, 9, 7, 4.6, P.WOOD[4])
    im.ellipse(7.5, 8.3, 6, 3.6, P.WOOD[5])
    im.ellipse(7, 7.6, 4, 2, P.WOOD[6])
    for x in (4, 7, 10):
        im.px(x, 8, P.WOOD[3]); im.px(x + 1, 9, P.WOOD[3])
    im.hline(3, 13, 12, P.WOOD[2])
    im.outline(col=P.OUTLINE)
    return im


def crumbs():
    im = Img(10, 6)
    for x, y in [(1, 3), (3, 1), (4, 4), (6, 2), (8, 4), (5, 0), (2, 5), (7, 5)]:
        im.px(x, y, P.WOOD[5])
    for x, y in [(3, 2), (6, 3), (1, 4)]:
        im.px(x, y, P.WOOD[3])
    return im


def can_icon():
    """su qabı (bağça suvarma qabı)"""
    im = Img(16, 16)
    G = P.GREEN
    im.rect(3, 6, 10, 13, G[3]); im.hline(3, 10, 6, G[4]); im.vline(10, 6, 13, G[2]); im.hline(3, 10, 13, G[1])
    im.vline(3, 6, 13, G[4])
    for i in range(5):
        im.px(11 + i, 9 - i, G[3]); im.px(11 + i, 10 - i, G[2])
    im.rect(14, 3, 15, 5, G[4])
    im.line(4, 5, 6, 2, G[2]); im.line(6, 2, 9, 2, G[2]); im.line(9, 2, 10, 5, G[2])
    im.hline(4, 9, 9, G[2])
    im.outline(col=P.OUTLINE)
    return im


def camera_icon():
    im = Img(16, 16)
    M = P.METAL
    im.rect(1, 5, 14, 13, M[2]); im.hline(1, 14, 5, M[4]); im.hline(1, 14, 13, M[1])
    im.rect(4, 3, 8, 5, M[3])
    im.circle(8, 9, 3.4, M[0]); im.circle(8, 9, 2.4, P.BLUE[3]); im.px(7, 8, '#ffffff')
    im.rect(11, 6, 13, 7, P.YELLOW[4])
    im.hline(1, 14, 7, M[3])
    im.outline(col=P.OUTLINE)
    return im


def flashlight_icon():
    im = Img(16, 16)
    M = P.METAL
    for i in range(9):
        im.px(2 + i, 12 - i, M[3]); im.px(3 + i, 12 - i, M[2]); im.px(2 + i, 13 - i, M[1])
    im.rect(10, 2, 13, 5, P.YELLOW[3]); im.rect(11, 3, 12, 4, P.YELLOW[5])
    im.px(6, 8, P.RED[3])
    im.outline(col=P.OUTLINE)
    return im


def flowerbed_dry():
    """suvarılmamış gül ləki: quru torpaq və kiçik cücərtilər"""
    im = Img(16, 12)
    D = P.DIRT
    im.ellipse(8, 7, 7.5, 4.5, D[2])
    im.ellipse(8, 6.5, 6.5, 3.5, D[3])
    for x, y in [(4, 6), (7, 5), (10, 7), (12, 5), (6, 8)]:
        im.px(x, y, D[1]); im.px(x + 1, y, D[4])
    for x in (5, 8, 11):
        im.px(x, 4, P.LEAF[4]); im.px(x, 5, P.LEAF[3]); im.px(x - 1, 4, P.LEAF[5])
    im.outline(col=D[0])
    return im


def water_drops():
    im = Img(12, 10)
    for x, y in [(2, 2), (5, 1), (8, 3), (10, 1), (4, 5), (7, 6), (1, 7), (9, 7)]:
        im.px(x, y, P.WATER[4]); im.px(x, y + 1, P.WATER[3])
    return im


# ---------------- yağış və nailiyyətlər (v6) ----------------
def raindrop():
    im = Img(2, 8)
    for y in range(8):
        a = int(60 + 150 * y / 7)
        im.px(1, y, (200, 225, 255, a))
        if y > 3:
            im.px(0, y, (160, 195, 240, a // 2))
    return im


def rain_splash(frame):
    im = Img(8, 4)
    c = (210, 230, 255, 220)
    if frame == 0:
        im.pts([(3, 3), (4, 3), (2, 2), (5, 2)], c)
    else:
        im.pts([(1, 2), (6, 2), (0, 1), (7, 1), (3, 0), (4, 0)], (210, 230, 255, 150))
    return im


def puddle(seed):
    im = Img(28, 12)
    r = rng(seed)
    W = P.WATER
    rx, ry = 13 - r.random() * 2, 5 - r.random()
    for y in range(12):
        for x in range(28):
            nx, ny = (x + 0.5 - 14) / rx, (y + 0.5 - 6) / ry
            wob = 0.12 * math.sin(x * 0.9 + seed) + 0.1 * math.sin(y * 1.7 + seed * 2)
            d = nx * nx + ny * ny + wob
            if d < 1:
                c = (52, 70, 112, 150)
                if d > 0.78:
                    c = (40, 52, 86, 170)
                im.px(x, y, c)
    # göyün əksi
    for x in range(8, 16):
        im.px(x, 4, (170, 200, 240, 140))
    for x in range(10, 13):
        im.px(x, 3, (220, 235, 255, 150))
    return im


def ripple(frame):
    im = Img(12, 6)
    rr = [1.5, 3.2, 5.0][frame]
    a = [220, 160, 90][frame]
    for k in range(28):
        t = k / 28 * math.tau
        im.px(6 + math.cos(t) * rr, 3 + math.sin(t) * rr * 0.45, (200, 225, 255, a))
    return im


def umbrella_world():
    """piyadaların başı üstündə çətir (ağ-boz: Unity-də rənglənir)"""
    im = Img(22, 18)
    for y in range(9):
        for x in range(22):
            nx, ny = (x + 0.5 - 11) / 10.5, (y + 0.5 - 9) / 8.5
            if nx * nx + ny * ny <= 1 and y < 9:
                shade = 0.55 + 0.45 * max(0.0, -nx * 0.5 - ny * 0.6 + 0.3)
                v = int(255 * min(1, shade))
                im.px(x, y, (v, v, v, 255))
    # tağlar
    for x in (5, 11, 16):
        for y in range(2, 9):
            if im.alpha(x, y):
                im.px(x, y, (150, 150, 155, 255))
    # aşağı dalğalı kənar
    for x in range(1, 21):
        if x % 5 in (1, 2, 3):
            im.px(x, 9, (120, 120, 125, 255))
    # sap
    for y in range(9, 17):
        im.px(11, y, (60, 50, 60, 255))
    im.px(10, 17, (60, 50, 60, 255)); im.px(9, 16, (60, 50, 60, 255))
    im.outline(col=(40, 36, 52, 255))
    return im


def umbrella_icon():
    im = Img(16, 16)
    R = P.RED
    for y in range(8):
        for x in range(16):
            nx, ny = (x + 0.5 - 8) / 7.5, (y + 0.5 - 8) / 7
            if nx * nx + ny * ny <= 1:
                im.px(x, y, R[4] if x < 6 else R[3] if x < 11 else R[2])
    for x in (4, 8, 12):
        im.px(x, 7, R[1])
    im.vline(8, 8, 14, P.METAL[1]); im.px(7, 15, P.METAL[1]); im.px(6, 14, P.METAL[1])
    im.outline(col=P.OUTLINE)
    return im


def rain_icon():
    im = Img(16, 16)
    C = ['#5a6a8a', '#7e8eaa', '#a8b6cc', '#d6deea']
    for (cx, cy, r) in [(5, 7, 3.5), (9, 5, 4.2), (12, 8, 3)]:
        im.circle(cx, cy, r, C[2])
    im.rect(2, 8, 14, 10, C[2])
    im.hline(3, 13, 10, C[1])
    im.px(7, 3, C[3]); im.px(8, 3, C[3])
    for x, y in [(4, 12), (8, 13), (12, 12), (6, 14), (10, 15)]:
        im.px(x, y, P.WATER[4])
    im.outline(col=P.BLUE[0])
    return im


def medal(locked):
    """nailiyyət medalı: lentli dairəvi çərçivə (ortası boş, ikon üstə qoyulur)"""
    im = Img(24, 26)
    G = P.GOLD if not locked else ['#2a2a34', '#3c3c48', '#50505e', '#686876', '#82828e', '#a0a0aa', '#c0c0c8']
    Rb = P.RED if not locked else ['#2a2a34', '#3c3c48', '#50505e', '#686876', '#82828e', '#a0a0aa']
    # lentlər
    im.poly([(6, 16), (10, 16), (8, 25), (5, 22)], Rb[3])
    im.poly([(14, 16), (18, 16), (19, 22), (16, 25)], Rb[2])
    im.circle(12, 11, 10.5, G[2])
    im.circle(12, 11, 9.5, G[4])
    im.circle(11.5, 10.5, 8, G[5])
    im.circle(12, 11, 7.6, G[3])
    im.circle(12, 11, 6.8, '#fff6dc' if not locked else '#4a4a56')
    for a in range(12):
        t = a / 12 * math.tau
        im.px(12 + math.cos(t) * 9.2, 11 + math.sin(t) * 9.2, G[6])
    im.outline(col=P.OUTLINE)
    return im


def house_icon():
    """nailiyyət ikonu: kiçik ev (qırmızı dam, sarı pəncərə)"""
    im = Img(16, 16)
    R = P.ROOF_RED; C = P.CREAM
    im.rect(3, 8, 12, 14, C[4])
    im.rect(3, 13, 12, 14, C[3])
    for y in range(1, 9):
        half = y + 0.5
        im.hline(max(0, int(8 - half)), min(15, int(7 + half)), y, R[4] if y < 4 else R[3])
    im.hline(0, 15, 8, R[2])
    im.rect(10, 2, 11, 4, P.BRICK[3])
    im.rect(5, 10, 6, 11, P.YELLOW[4])
    im.rect(9, 10, 10, 14, P.WOOD[3]); im.px(9, 12, P.GOLD[4])
    im.outline(col=P.OUTLINE)
    return im

