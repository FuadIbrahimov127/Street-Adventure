# Zəmin təbəqələrini xəritə üzərində sınaq üçün göstərir (Unity-dəki məntiqin Python nüsxəsi).
import re, sys
from PIL import Image
import tiles as TL

src = open(__import__('os').path.join(__import__('os').path.dirname(__file__), '..', '..', 'Assets', 'MehelleMacerasi', 'Editor', 'MMLevelData.cs'), encoding='utf-8').read()
def arr(name):
    block = src.split(f'public static readonly string[] {name}')[1].split('};')[0]
    return re.findall(r'"([^"]*)"', block)
G = arr('Ground')
H, Wd = len(G), len(G[0])

def at(c, r):
    if r < 0 or r >= H or c < 0 or c >= Wd:
        return '#'
    return G[r][c]

GRASSY = set('.,')
BUILD = set('HAPMC')

def ground_tiles():
    grass = [TL.grass(i, dense=(i % 2 == 0)) for i in range(4)]
    flowers = [TL.grass(10 + i, flowers=3) for i in range(2)]
    dirt = [TL.dirt(i) for i in range(3)]
    pav = [TL.pavers(i) for i in range(3)]
    return grass, flowers, dirt, pav

def render(scale=1):
    grass, flowers, dirt, pav = ground_tiles()
    road_t = TL.asphalt(1, 'top'); road_b = TL.asphalt(2, 'bottom')
    cw_t = TL.crosswalk(3, 'top'); cw_b = TL.crosswalk(4, 'bottom')
    hed = TL.hedge(5); hedf = TL.hedge(6, front=True)
    edges = {s: TL.grass_edge(s, 20 + i) for i, s in enumerate('nesw')}
    corners = {c: TL.grass_corner(c, 0) for c in ('nw', 'ne', 'sw', 'se')}
    curbs = {'s': TL.curb('s'), 'n': TL.curb('n')}
    ao = TL.ao_shadow()
    img = Image.new('RGBA', (Wd * 16, H * 16))
    def put(t, c, r):
        img.alpha_composite(t.pil(), (c * 16, r * 16))
    for r in range(H):
        for c in range(Wd):
            ch = G[r][c]
            h = (c * 7 + r * 13) % 11
            if ch in 'sp':
                base = (pav if ch == 's' else dirt)[h % 3]
                put(base, c, r)
                nb = {'n': at(c, r - 1), 's': at(c, r + 1), 'w': at(c - 1, r), 'e': at(c + 1, r)}
                for side, v in nb.items():
                    if v in GRASSY or (ch == 'p' and v == 's') or v in '#':
                        put(edges[side], c, r)
                    elif ch == 's' and v in 'tTbB':
                        put(curbs[side], c, r)
                diag = {'nw': (-1, -1), 'ne': (1, -1), 'sw': (-1, 1), 'se': (1, 1)}
                for k, (dx, dy) in diag.items():
                    if at(c + dx, r + dy) in GRASSY and at(c + dx, r) not in GRASSY and at(c, r + dy) not in GRASSY:
                        put(corners[k], c, r)
            elif ch == 't': put(road_t, c, r)
            elif ch == 'b': put(road_b, c, r)
            elif ch == 'T': put(cw_t, c, r)
            elif ch == 'B': put(cw_b, c, r)
            elif ch == '#':
                put(hedf if at(c, r + 1) != '#' else hed, c, r)
            elif ch == ',':
                put(flowers[h % 2], c, r)
            else:
                put(grass[h % 4], c, r)
            # kol/bina altında kölgə
            if ch not in '#HAPMC' and (at(c, r - 1) == '#' or at(c, r - 1) in BUILD):
                put(ao, c, r)
    if scale != 1:
        img = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
    return img

if __name__ == '__main__':
    im = render(1)
    im.save('out/ground_full.png')
    im.crop((0, 0, 30 * 16, 24 * 16)).resize((30 * 16 * 3, 24 * 16 * 3), Image.NEAREST).save('out/ground_zoom.png')
    print('ok')
