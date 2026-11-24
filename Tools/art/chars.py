# Personajlar: 16x32, Stardew kimi iri başlı mütənasiblik, 3 istiqamət x 3 kadr.
# d: 'down' | 'up' | 'side' (sağa baxır; sola üçün Unity-də flipX), f: 0 dayanma, 1 addım A, 2 addım B
from px import Img, rgb, mix, darker, lighter
import pal as P

EYE = '#24161e'


def _skin(spec):
    return P.SKIN[spec.get('skin', 'light')]


def _legs(im, spec, d, f):
    pants = spec.get('pants', P.BLUE)
    shoes = spec.get('shoes', P.BLACKISH)
    dress = spec.get('dress')
    sk = _skin(spec)
    if d in ('down', 'up'):
        if dress:
            # uzun ətək: ayaqları örtür
            for y in range(25, 30):
                w = 0 if y < 27 else 1
                im.hline(4 - w, 11 + w, y, dress[3])
                im.px(11 + w, y, dress[2])
                im.px(4 - w, y, dress[4])
            im.hline(3, 12, 29, dress[2])
            lx, rx = (4, 9)
            ly = 31 if f != 2 else 30
            ry = 31 if f != 1 else 30
            im.hline(lx + 1, lx + 2, ly - 1, shoes[2]); im.hline(lx + 1, lx + 2, ly, shoes[1])
            im.hline(rx, rx + 1, ry - 1, shoes[2]); im.hline(rx, rx + 1, ry, shoes[1])
            return
        short = spec.get('shorts', False)
        im.hline(5, 10, 27, pants[3])
        im.px(10, 27, pants[2])
        # sol ayaq (ekranda sol)
        lb = 30 if f != 2 else 29   # addımda qalxan ayaq qısalır
        rb = 30 if f != 1 else 29
        for y in range(28, lb):
            c = sk[3] if short and y > 28 else pants[3]
            im.hline(5, 6, y, c)
            im.px(6, y, sk[2] if short and y > 28 else pants[2])
        for y in range(28, rb):
            c = sk[3] if short and y > 28 else pants[3]
            im.hline(9, 10, y, c)
            im.px(10, y, sk[2] if short and y > 28 else pants[2])
        im.hline(4, 6, lb, shoes[3]); im.hline(4, 6, lb + 1, shoes[1]); im.px(4, lb, shoes[4])
        im.hline(9, 11, rb, shoes[3]); im.hline(9, 11, rb + 1, shoes[1]); im.px(11, rb, shoes[2])
    else:
        if dress:
            for y in range(25, 30):
                w = 0 if y < 27 else 1
                im.hline(5 - w, 10 + w, y, dress[3])
                im.px(10 + w, y, dress[2])
            im.hline(4, 11, 29, dress[2])
            if f == 0:
                im.hline(6, 9, 30, shoes[2]); im.hline(6, 9, 31, shoes[1])
            else:
                fx, bx = (9, 4) if f == 1 else (8, 5)
                im.hline(fx, fx + 2, 31, shoes[1]); im.hline(fx, fx + 1, 30, shoes[2])
                im.hline(bx, bx + 2, 31, shoes[1]); im.hline(bx + 1, bx + 2, 30, shoes[2])
            return
        short = spec.get('shorts', False)
        im.hline(6, 9, 27, pants[3])
        im.px(9, 27, pants[2])
        if f == 0:
            for y in range(28, 30):
                im.hline(6, 9, y, sk[3] if short else pants[3])
                im.px(9, y, sk[2] if short else pants[2])
                im.px(7, y, mix(pants[2], pants[3], 0.5) if not short else sk[2])
            im.hline(6, 10, 30, shoes[3]); im.hline(6, 10, 31, shoes[1]); im.px(10, 30, shoes[4])
        else:
            # irəli və geri ayaq
            front = [(8, 28), (9, 29), (10, 29)] if f == 1 else [(8, 28), (9, 28), (9, 29)]
            back = [(6, 28), (5, 29), (5, 29)] if f == 1 else [(7, 28), (6, 29), (6, 29)]
            for x, y in back:
                im.px(x, y, sk[2] if short else pants[2])
                im.px(x + 1, y, sk[2] if short else pants[2])
            for x, y in front:
                im.px(x, y, sk[3] if short else pants[3])
            if f == 1:
                im.hline(9, 12, 30, shoes[3]); im.hline(9, 12, 31, shoes[1]); im.px(12, 30, shoes[4])
                im.hline(3, 6, 30, shoes[2]); im.hline(3, 6, 31, shoes[0])
            else:
                im.hline(8, 11, 30, shoes[3]); im.hline(8, 11, 31, shoes[1]); im.px(11, 30, shoes[4])
                im.hline(4, 7, 30, shoes[2]); im.hline(4, 7, 31, shoes[0])


def _torso(im, spec, d, f):
    top = spec['top']
    sk = _skin(spec)
    if d in ('down', 'up'):
        im.rect(4, 20, 11, 26, top[3])
        im.vline(11, 20, 26, top[2])
        im.vline(10, 21, 26, mix(top[2], top[3], 0.5))
        im.hline(4, 11, 20, top[4])
        im.hline(5, 10, 26, top[2])
        if d == 'down':
            under = spec.get('under')
            if under:
                im.rect(7, 20, 8, 24, under[4])
                im.px(8, 21, under[3])
            if spec.get('collar'):
                im.pts([(6, 20), (9, 20)], spec['collar'])
            if spec.get('tie'):
                im.vline(7, 21, 24, spec['tie'][3]); im.px(8, 21, spec['tie'][2])
            if spec.get('vest'):
                v = spec['vest']
                for y in range(20, 27):
                    im.px(5, y, v[3]); im.px(4, y, v[3])
                    im.px(10, y, v[2]); im.px(11, y, v[2])
                    if y > 21:
                        im.px(6, y, v[3]); im.px(9, y, v[2])
            if spec.get('apron'):
                a = spec['apron']
                im.rect(5, 22, 10, 28, a[4]); im.vline(10, 22, 28, a[3]); im.hline(5, 10, 22, a[5])
            if spec.get('belt'):
                im.hline(4, 11, 26, spec['belt'][1]); im.px(7, 26, P.GOLD[4])
            if spec.get('strap'):
                s = spec['strap']
                for k in range(7):
                    im.px(4 + k, 20 + k, s[2])
                    im.px(5 + k, 20 + k, s[3])
            if spec.get('stripe'):
                im.hline(4, 11, 23, spec['stripe'][4])
            # boyun
            im.hline(7, 8, 20, sk[2])
        else:
            if spec.get('backpack'):
                bp = spec['backpack']
                im.rect(5, 21, 10, 26, bp[3]); im.hline(5, 10, 21, bp[4]); im.vline(10, 21, 26, bp[2])
                im.hline(6, 9, 24, bp[2]); im.px(7, 25, P.GOLD[4])
            if spec.get('apron'):
                im.hline(5, 10, 23, spec['apron'][4])
    else:
        im.rect(5, 20, 10, 26, top[3])
        im.vline(5, 20, 26, top[4])
        im.vline(10, 20, 26, top[2])
        im.hline(5, 10, 26, top[2])
        if spec.get('apron'):
            a = spec['apron']
            im.rect(9, 22, 11, 28, a[4]); im.vline(11, 22, 28, a[3])
        if spec.get('vest'):
            im.rect(5, 21, 7, 26, spec['vest'][3])
        if spec.get('backpack'):
            bp = spec['backpack']
            im.rect(3, 21, 5, 26, bp[3]); im.vline(3, 21, 26, bp[2]); im.hline(3, 5, 21, bp[4])
        if spec.get('strap'):
            s = spec['strap']
            im.rect(4, 24, 6, 27, s[3]); im.hline(4, 6, 24, s[4]); im.vline(6, 24, 27, s[2])
            im.line(6, 20, 9, 24, s[2])
        im.px(8, 20, sk[2])


def _arms(im, spec, d, f):
    top = spec['top']
    sk = _skin(spec)
    sleeve = spec.get('sleeve', top)
    if d in ('down', 'up'):
        # qol yelləməsi: əl 1 piksel yuxarı/aşağı
        ly = 0 if f == 0 else (1 if f == 1 else -1)
        ry = -ly
        for side, x, dy in (('l', 3, ly), ('r', 12, ry)):
            for y in range(21 + dy, 25 + dy):
                im.px(x, y, sleeve[3] if side == 'l' else sleeve[2])
            im.px(x, 20 + max(dy, 0), sleeve[4] if side == 'l' else sleeve[3])
            im.px(x, 25 + dy, sk[3] if side == 'l' else sk[2])
            im.px(x, 26 + dy, sk[2] if side == 'l' else sk[1])
        if spec.get('cane') and d == 'down':
            c = P.WOOD
            im.vline(13, 25, 31, c[2]); im.px(13, 24, c[3]); im.px(12, 24, c[3])
        if spec.get('book') and d == 'down':
            b = spec['book']
            im.rect(2, 23 + ly, 4, 26 + ly, b[3]); im.vline(2, 23 + ly, 26 + ly, b[4])
    else:
        # yan görünüş: bir qol önə/arxaya
        if f == 0:
            pts = [(7, 21), (7, 22), (7, 23), (7, 24)]
            hand = (7, 25)
        elif f == 1:
            pts = [(8, 21), (8, 22), (9, 23), (9, 24)]
            hand = (10, 24)
        else:
            pts = [(7, 21), (6, 22), (6, 23), (5, 24)]
            hand = (5, 25)
        for x, y in pts:
            im.px(x, y, sleeve[2])
            im.px(x + 1, y, sleeve[3]) if x + 1 <= 10 else None
        im.px(*hand, sk[3])
        if spec.get('cane'):
            c = P.WOOD
            im.vline(hand[0] + 1, hand[1], 31, c[2])


def _head(im, spec, d, f):
    sk = _skin(spec)
    rows = {10: (4, 11), 11: (3, 12), 12: (3, 12), 13: (3, 12), 14: (3, 12), 15: (3, 12), 16: (3, 12), 17: (3, 12), 18: (4, 11), 19: (5, 10)}
    if d == 'side':
        rows = {10: (5, 11), 11: (4, 12), 12: (4, 12), 13: (4, 12), 14: (4, 13), 15: (4, 13), 16: (4, 12), 17: (4, 12), 18: (5, 11), 19: (6, 10)}
    for y, (a, b) in rows.items():
        im.hline(a, b, y, sk[3])
        im.px(b, y, sk[2])
        if d != 'side':
            im.px(a, y, mix(sk[3], sk[4], 0.4))
    im.hline(5, 10, 19, sk[2])
    if d == 'down':
        # gözlər
        ey = 14
        for ex in (5, 10):
            im.px(ex, ey, EYE)
            im.px(ex, ey + 1, EYE)
        if spec.get('eye_hi', True):
            im.px(5, ey, '#3e2a36')
            im.px(10, ey, '#3e2a36')
        # yanaq qızartısı
        blush = mix(sk[3], P.PINK[3], 0.45)
        im.px(4, 16, blush)
        im.px(11, 16, blush)
        im.hline(7, 8, 17, sk[1])
        if spec.get('glasses'):
            g = P.METAL[2]
            for ex in (4, 9):
                im.hline(ex, ex + 2, 13, g)
                im.px(ex, 14, P.METAL[4]); im.px(ex + 2, 14, P.METAL[4])
            im.px(7, 13, g); im.px(8, 13, g)
        if spec.get('mustache'):
            m = spec['mustache']
            im.hline(5, 10, 17, m[2]); im.px(6, 16, m[2]); im.px(9, 16, m[2])
            im.hline(7, 8, 18, sk[1])
        if spec.get('wrinkles'):
            im.px(4, 13, sk[2]); im.px(11, 13, sk[2])
    elif d == 'side':
        im.px(10, 14, EYE)
        im.px(10, 15, EYE)
        im.px(13, 15, sk[2])            # burun
        im.px(11, 17, sk[1])            # ağız
        im.px(9, 16, mix(sk[3], P.PINK[3], 0.45))
        if spec.get('glasses'):
            g = P.METAL[1]
            im.rect(9, 13, 11, 15, g); im.px(10, 14, EYE); im.hline(4, 9, 13, g)
        if spec.get('mustache'):
            m = spec['mustache']
            im.hline(10, 13, 16, m[2]); im.px(12, 17, m[1])
        # qulaq
        im.px(7, 14, sk[2]); im.px(7, 15, sk[2])


def _hair(im, spec, d, f):
    H = spec.get('hair', P.HAIR['brown'])
    style = spec.get('style', 'short')
    if style == 'scarf':
        S = spec['scarf']
        if d == 'down':
            for y in range(8, 13):
                a, b = (5, 10) if y == 8 else (4, 11) if y == 9 else (3, 12)
                im.hline(a, b, y, S[3])
            im.hline(3, 12, 12, S[2])
            for y in range(12, 20):
                im.px(2, y, S[3]); im.px(13, y, S[2])
            im.px(3, 13, S[3]); im.px(12, 13, S[2])
            im.hline(5, 10, 9, S[4])
            for x in (5, 8, 11):
                im.px(x, 10, P.YELLOW[4])
            # boz saç kənarı
            im.hline(4, 11, 13, P.HAIR['grey'][3])
            im.px(4, 14, P.HAIR['grey'][2]); im.px(11, 14, P.HAIR['grey'][2])
            im.pts([(3, 20), (12, 20), (2, 20), (13, 20)], S[2])
        elif d == 'up':
            for y in range(8, 21):
                a, b = (5, 10) if y == 8 else (4, 11) if y == 9 else (3, 12) if y < 19 else (3, 12)
                im.hline(a, b, y, S[3])
                im.px(b, y, S[2])
            for x in range(4, 12, 3):
                for y in range(11, 19, 3):
                    im.px(x, y, P.YELLOW[4])
            im.hline(4, 11, 9, S[4])
            im.hline(3, 12, 20, S[2])
        else:
            for y in range(8, 21):
                a, b = (5, 10) if y == 8 else (4, 11) if y == 9 else (3, 12) if y < 13 else (3, 8)
                im.hline(a, b, y, S[3])
            im.hline(9, 12, 12, S[2])
            im.px(9, 13, P.HAIR['grey'][3])
            im.hline(5, 10, 9, S[4])
            for x, y in ((5, 11), (7, 14), (5, 17)):
                im.px(x, y, P.YELLOW[4])
        return
    if d == 'down':
        im.hline(5, 10, 8, H[3])
        im.hline(4, 11, 9, H[3])
        im.hline(3, 12, 10, H[3])
        im.hline(3, 12, 11, H[2])
        im.hline(5, 9, 9, H[4])
        im.px(6, 8, H[4])
        # kəkil
        bang = {'short': [(3, 12), (4, 12), (7, 12), (11, 12), (12, 12), (3, 13), (12, 13)],
                'long': [(3, 12), (4, 12), (5, 12), (10, 12), (11, 12), (12, 12)] + [(3, y) for y in range(13, 21)] + [(12, y) for y in range(13, 21)] + [(2, y) for y in range(14, 21)] + [(13, y) for y in range(14, 21)],
                'messy': [(3, 12), (5, 12), (6, 12), (9, 12), (12, 12), (3, 13), (12, 13), (6, 13)],
                'bun': [(3, 12), (4, 12), (11, 12), (12, 12)],
                'bald': [],
                'ponytail': [(3, 12), (4, 12), (5, 12), (10, 12), (11, 12), (12, 12), (3, 13), (12, 13)]}[style]
        for x, y in bang:
            im.px(x, y, H[2] if x > 8 else H[3])
        if style == 'bald':
            im.hline(5, 10, 8, _skin(spec)[3]); im.hline(4, 11, 9, _skin(spec)[3]); im.hline(4, 11, 10, _skin(spec)[3])
            im.hline(3, 12, 11, H[3]); im.px(3, 12, H[3]); im.px(12, 12, H[2])
        if style == 'bun':
            im.rect(6, 5, 9, 7, H[3]); im.hline(7, 8, 5, H[4]); im.px(9, 7, H[2])
        if style == 'messy':
            im.px(4, 8, H[3]); im.px(10, 7, H[3]); im.px(11, 8, H[2])
    elif d == 'up':
        for y in range(8, 20):
            a, b = (5, 10) if y == 8 else (4, 11) if y == 9 else (3, 12) if y < 18 else (4, 11)
            im.hline(a, b, y, H[3])
            im.px(b, y, H[2])
            im.px(a, y, H[4] if y < 12 else H[3])
        im.hline(5, 9, 9, H[4])
        im.px(7, 8, H[4]); im.px(10, 10, H[4])
        if style in ('short', 'messy', 'bald'):
            im.hline(4, 11, 19, _skin(spec)[2])
            im.hline(4, 11, 18, H[2])
        if style == 'bald':
            for y in range(8, 15):
                a, b = (5, 10) if y == 8 else (4, 11) if y == 9 else (3, 12)
                im.hline(a, b, y, _skin(spec)[3])
                im.px(b, y, _skin(spec)[2])
        if style == 'long':
            for y in range(18, 24):
                im.hline(4, 11, y, H[3]); im.px(11, y, H[2])
        if style == 'ponytail':
            im.rect(7, 18, 8, 24, H[3]); im.px(8, 24, H[2]); im.px(7, 18, P.RED[3]); im.px(8, 18, P.RED[3])
        if style == 'bun':
            im.rect(6, 5, 9, 8, H[3]); im.hline(7, 8, 5, H[4])
    else:
        for y in range(8, 13):
            a, b = (5, 10) if y == 8 else (4, 11) if y == 9 else (3, 12)
            im.hline(a, b, y, H[3])
        im.hline(5, 9, 9, H[4])
        im.px(12, 12, H[2]); im.px(11, 12, H[3]); im.px(12, 13, H[2])
        back = {'short': 17, 'messy': 17, 'long': 22, 'bun': 16, 'ponytail': 16, 'bald': 0}[style]
        for y in range(12, back + 1):
            im.hline(3, 6, y, H[3])
            im.px(3, y, H[2])
        if style == 'long':
            im.hline(4, 8, 22, H[2])
        if style == 'ponytail':
            im.rect(1, 13, 2, 20, H[3]); im.px(1, 20, H[2]); im.px(3, 13, P.RED[3])
        if style == 'bun':
            im.rect(1, 9, 3, 12, H[3]); im.px(1, 9, H[4])
        if style == 'messy':
            im.px(4, 7, H[3]); im.px(8, 7, H[3])
        if style == 'bald':
            sk = _skin(spec)
            for y in range(8, 12):
                a, b = (5, 10) if y == 8 else (4, 11) if y == 9 else (4, 12)
                im.hline(a, b, y, sk[3])
            im.rect(3, 12, 6, 15, H[3])


def _hat(im, spec, d, f):
    hat = spec.get('hat')
    if not hat:
        return
    kind, C = hat
    if kind == 'cap':           # poçtalyon papağı
        if d == 'down':
            im.rect(4, 7, 11, 10, C[3]); im.hline(4, 11, 7, C[4]); im.vline(11, 7, 10, C[2])
            im.hline(3, 12, 11, C[1]); im.hline(4, 11, 11, C[2])
            im.px(7, 8, P.YELLOW[4]); im.px(8, 8, P.YELLOW[3])
        elif d == 'up':
            im.rect(4, 7, 11, 11, C[3]); im.hline(4, 11, 7, C[4]); im.vline(11, 7, 11, C[2])
        else:
            im.rect(4, 7, 11, 10, C[3]); im.hline(4, 11, 7, C[4])
            im.hline(9, 14, 11, C[1]); im.px(9, 8, P.YELLOW[4])
    elif kind == 'flat':        # kepka
        if d == 'down':
            im.rect(3, 8, 12, 10, C[3]); im.hline(4, 11, 7, C[3]); im.hline(4, 11, 7, C[4])
            im.hline(3, 12, 11, C[1]); im.vline(12, 8, 10, C[2])
        elif d == 'up':
            im.rect(3, 7, 12, 11, C[3]); im.hline(4, 11, 7, C[4]); im.vline(12, 8, 11, C[2])
        else:
            im.rect(3, 7, 11, 10, C[3]); im.hline(4, 10, 7, C[4]); im.hline(9, 13, 11, C[1])
    elif kind == 'band':        # idman lenti
        y = 11 if d != 'side' else 11
        im.hline(3, 12, y, C[3]); im.hline(3, 12, y, C[3])


def make_char(spec, d, f):
    im = Img(16, 32)
    up = d == 'up'
    if up:
        _arms(im, spec, d, f)
    _legs(im, spec, d, f)
    _torso(im, spec, d, f)
    if not up:
        _arms(im, spec, d, f)
    _head(im, spec, d, f)
    _hair(im, spec, d, f)
    _hat(im, spec, d, f)
    im.outline(col=None, k=0.30)
    return im


SPECS = {
    'player': dict(skin='light', hair=P.HAIR['brown'], style='messy', top=P.TEAL, under=P.WHITE, pants=P.BLUE, shoes=P.RED, backpack=P.ORANGE),
    'sara': dict(skin='old', style='scarf', scarf=P.RED, top=P.PURPLE, dress=P.PURPLE, apron=P.WHITE, shoes=P.BLACKISH, wrinkles=True),
    'elcin': dict(skin='tan', hair=P.HAIR['black'], style='short', top=P.BLUE, collar=P.WHITE[5], pants=P.BLACKISH, shoes=P.BLACKISH, strap=P.WOOD, hat=('cap', P.BLUE)),
    'reshad': dict(skin='tan', hair=P.HAIR['black'], style='short', top=P.WHITE, vest=P.BLACKISH, pants=P.BLACKISH, shoes=P.WOOD, mustache=P.HAIR['black'], apron=P.WHITE),
    'nigar': dict(skin='light', hair=P.HAIR['black'], style='ponytail', top=P.PURPLE, under=P.YELLOW, pants=P.PASTEL_BLUE, shoes=P.WHITE, glasses=True, book=P.RED),
    'kamal': dict(skin='tan', hair=P.HAIR['grey'], style='short', top=P.BLUE, stripe=P.BLUE, pants=P.WOOD, shoes=P.BLACKISH, mustache=P.HAIR['grey'], belt=P.WOOD, hat=('flat', P.STONE)),
    'aysel': dict(skin='light', hair=P.HAIR['auburn'], style='bun', top=P.GREEN, apron=P.RED, pants=P.BLACKISH, shoes=P.BLACKISH),
    # piyadalar
    'ped_a': dict(skin='light', hair=P.HAIR['black'], style='short', top=P.STONE, under=P.WHITE, tie=P.RED, pants=P.STONE, shoes=P.BLACKISH),
    'ped_b': dict(skin='light', hair=P.HAIR['auburn'], style='long', top=P.PINK, dress=P.BLACKISH, shoes=P.RED),
    'ped_c': dict(skin='light', hair=P.HAIR['blond'], style='messy', top=P.YELLOW, pants=P.BLUE, shorts=True, shoes=P.WHITE),
    'ped_d': dict(skin='old', hair=P.HAIR['grey'], style='bald', top=P.WOOD, pants=P.STONE, shoes=P.BLACKISH, cane=True, mustache=P.HAIR['grey'], hat=('flat', P.WOOD)),
    'ped_e': dict(skin='tan', hair=P.HAIR['brown'], style='ponytail', top=P.RED, stripe=P.RED, pants=P.BLACKISH, shoes=P.WHITE, hat=('band', P.WHITE)),
}


def all_frames(name):
    out = {}
    for d in ('down', 'up', 'side'):
        for f in range(3):
            out[f'{name}_{d}_{f}'] = make_char(SPECS[name], d, f)
    return out
