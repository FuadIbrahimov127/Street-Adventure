# Bütün səhnənin Python önizləməsi (gündüz və gecə).
import re, numpy as np
from PIL import Image
import ground_preview as GP
import build as BL

ART = BL.flat()
def S(name): return ART[name][1].pil()

G = GP.G; H = GP.H; W = GP.Wd
src = open(__import__('os').path.join(__import__('os').path.dirname(__file__), '..', '..', 'Assets', 'MehelleMacerasi', 'Editor', 'MMLevelData.cs'), encoding='utf-8').read()
O = GP.arr('Objects')

def render(night=False):
    base = GP.render(1)
    layer = Image.new('RGBA', base.size)
    sprites = []  # (sort_y, img, x, y)
    # binalar
    seen = set()
    bmap = {'H': 'bld_house', 'A': 'bld_apartment', 'P': 'bld_post', 'M': 'bld_market', 'C': 'bld_teahouse'}
    for r in range(H):
        for c in range(W):
            ch = G[r][c]
            if ch in bmap and (c, r) not in seen:
                c1 = c
                while c1 + 1 < W and G[r][c1 + 1] == ch: c1 += 1
                r1 = r
                while r1 + 1 < H and G[r1 + 1][c] == ch: r1 += 1
                for rr in range(r, r1 + 1):
                    for cc in range(c, c1 + 1): seen.add((cc, rr))
                im = S(bmap[ch])
                sprites.append(((r1 + 1) * 16, im, c * 16, (r1 + 1) * 16 - im.height, bmap[ch]))
    def put(name, cx, by, sort=None):
        im = S(name)
        sprites.append((sort if sort is not None else by, im, int(cx - im.width / 2), int(by - im.height), name))
    for r in range(H):
        for c in range(W):
            ch = O[r][c]
            cx, by = c * 16 + 8, r * 16 + 15
            h = (c * 7 + r * 13) % 5
            if ch == 'T': put(['tree_oak', 'tree_oak2', 'tree_blossom', 'tree_oak', 'tree_oak2'][h], cx, by + 2)
            elif ch == 'Y': put(['tree_pomegranate', 'tree_cypress'][h % 2], cx, by + 2)
            elif ch == 'L': put('lamp_on' if night else 'lamp', cx, by + 1)
            elif ch == 'n': put('bench', cx + 8, by)
            elif ch == 'u': put('bin', cx, by)
            elif ch == 'o': put('bush', cx, by + 1)
            elif ch == 'f': put(['bush_flowers', 'bush_flowers2'][h % 2], cx, by + 1)
            elif ch == 'F': put('fountain_0', cx, by + 6)
            elif ch == 'N': put('nerd_table_0', cx, by + 2)
            elif ch == 'c': put('coin_0', cx, by - 2)
            elif ch == 'm': put('letter', cx, by - 2)
            elif ch == 'k': put('cat_idle_0', cx, by)
            elif ch == '@': put('player_down_0', cx, by)
            elif ch in '12356':
                nm = {'1': 'sara', '2': 'elcin', '3': 'reshad', '5': 'nigar', '6': 'kamal'}[ch]
                put(nm + '_down_0', cx, by)
    put('aysel_down_0', 39 * 16 + 8, 7 * 16 + 15)
    put('fruit_stand', 37 * 16 + 8, 7 * 16 + 14)
    # maşınlar və piyadalar
    put('car_taxi', 8 * 16, 10 * 16 + 14); put('car_red', 26 * 16, 10 * 16 + 14)
    im = S('car_blue').transpose(Image.FLIP_LEFT_RIGHT); sprites.append((11 * 16 + 15, im, 17 * 16, 11 * 16 + 15 - 24, 'car'))
    put('car_yellow', 41 * 16, 11 * 16 + 15)
    put('ped_b_side_1', 20 * 16, 8 * 16 + 15); put('ped_a_down_1', 33 * 16, 13 * 16 + 12); put('ped_c_side_2', 14 * 16, 13 * 16 + 14)
    put('ped_d_down_0', 30 * 16 + 8, 18 * 16 + 15); put('ped_e_side_1', 24 * 16, 24 * 16 + 15); put('dog_walk_0', 26 * 16, 24 * 16 + 15)
    for i, (x, y) in enumerate([(15, 21), (16, 22), (14, 22)]):
        put(f'pigeon_{i % 2}', x * 16, y * 16)
    sprites.sort(key=lambda s: s[0])
    for sy, im, x, y, name in sprites:
        if not name.startswith('bld') and not name.startswith('car') and name not in ('coin_0', 'letter'):
            sh = ART['shadow'][1].pil()
            w = max(10, min(40, im.width - 4))
            sh = sh.resize((w, 6), Image.NEAREST)
            layer.alpha_composite(sh, (x + (im.width - w) // 2, y + im.height - 4)) if True else None
        layer.alpha_composite(im, (x, y))
    out = base.copy(); out.alpha_composite(layer)
    if night:
        a = np.array(out).astype(float)
        a[:, :, :3] *= np.array([0.30, 0.34, 0.58])
        out = Image.fromarray(np.clip(a, 0, 255).astype('uint8'), 'RGBA')
        # pəncərə işıqları və fənər işığı
        for sy, im, x, y, name in sprites:
            if name.startswith('bld'):
                out.alpha_composite(S(name + '_glow'), (x, y))
            if name == 'lamp_on':
                gl = S('glow').resize((80, 40), Image.NEAREST)
                out.alpha_composite(gl, (x + 7 - 40, y + im.height - 20))
                out.alpha_composite(im.crop((0, 0, im.width, 13)), (x, y))
    return out

if __name__ == '__main__':
    d = render(False); n = render(True)
    d.save('out/scene_day.png'); n.save('out/scene_night.png')
    d.crop((0, 0, 28 * 16, 18 * 16)).resize((28 * 16 * 3, 18 * 16 * 3), Image.NEAREST).save('out/scene_day_zoom.png')
    n.crop((16 * 16, 0, 44 * 16, 18 * 16)).resize((28 * 16 * 3, 18 * 16 * 3), Image.NEAREST).save('out/scene_night_zoom.png')
    d.crop((0, 13 * 16, 28 * 16, 31 * 16)).resize((28 * 16 * 3, 18 * 16 * 3), Image.NEAREST).save('out/scene_park_zoom.png')
    print('ok')
