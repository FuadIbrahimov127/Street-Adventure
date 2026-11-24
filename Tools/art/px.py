# Piksel çəkmə köməkçisi: RGBA kətan, sadə fiqurlar, kontur, kölgə, dither.
import random
import numpy as np
from PIL import Image


def rgb(h):
    """'#rrggbb' və ya (r,g,b[,a]) -> (r,g,b,a)"""
    if h is None:
        return None
    if isinstance(h, tuple):
        return h if len(h) == 4 else (h[0], h[1], h[2], 255)
    h = h.lstrip('#')
    a = int(h[6:8], 16) if len(h) == 8 else 255
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def mix(a, b, t):
    a, b = rgb(a), rgb(b)
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(4))


def darker(c, k=0.75, hue_to='#2a2050'):
    """kölgə rəngi: qaraya yox, soyuq bənövşəyiyə doğru"""
    return mix(c, hue_to, 1 - k)


def lighter(c, k=0.3, hue_to='#fff4c8'):
    """işıq rəngi: isti sarıya doğru"""
    return mix(c, hue_to, k)


class Img:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.a = np.zeros((h, w, 4), dtype=np.uint8)

    # --- əsas ---
    def copy(self):
        c = Img(self.w, self.h)
        c.a = self.a.copy()
        return c

    def inb(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h

    def px(self, x, y, c):
        if c is None:
            return
        x, y = int(x), int(y)
        if not self.inb(x, y):
            return
        c = rgb(c)
        if c[3] == 255:
            self.a[y, x] = c
        else:
            self.blend_px(x, y, c)

    def blend_px(self, x, y, c):
        src = np.array(c, dtype=np.float32)
        dst = self.a[y, x].astype(np.float32)
        sa = src[3] / 255.0
        da = dst[3] / 255.0
        oa = sa + da * (1 - sa)
        if oa <= 0:
            return
        col = (src[:3] * sa + dst[:3] * da * (1 - sa)) / oa
        self.a[y, x] = [int(col[0]), int(col[1]), int(col[2]), int(oa * 255)]

    def get(self, x, y):
        x, y = int(x), int(y)
        if not self.inb(x, y):
            return (0, 0, 0, 0)
        return tuple(int(v) for v in self.a[y, x])

    def alpha(self, x, y):
        return self.get(x, y)[3]

    def erase(self, x, y):
        if self.inb(x, y):
            self.a[int(y), int(x)] = 0

    # --- fiqurlar ---
    def rect(self, x0, y0, x1, y1, c):
        """daxil olmaqla (inclusive)"""
        c = rgb(c)
        x0, x1 = max(0, int(x0)), min(self.w - 1, int(x1))
        y0, y1 = max(0, int(y0)), min(self.h - 1, int(y1))
        if x0 > x1 or y0 > y1:
            return
        if c[3] == 255:
            self.a[y0:y1 + 1, x0:x1 + 1] = c
        else:
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1):
                    self.blend_px(x, y, c)

    def hline(self, x0, x1, y, c):
        self.rect(min(x0, x1), y, max(x0, x1), y, c)

    def vline(self, x, y0, y1, c):
        self.rect(x, min(y0, y1), x, max(y0, y1), c)

    def line(self, x0, y0, x1, y1, c):
        x0, y0, x1, y1 = int(x0), int(y0), int(x1), int(y1)
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            self.px(x0, y0, c)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def pts(self, pts, c):
        for x, y in pts:
            self.px(x, y, c)

    def ellipse(self, cx, cy, rx, ry, c, fn=None):
        """dolu ellips; fn(x,y,nx,ny)->rəng ilə hər pikseli fərdi boyamaq olar"""
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                nx, ny = (x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry
                if nx * nx + ny * ny <= 1.0:
                    self.px(x, y, fn(x, y, nx, ny) if fn else c)

    def circle(self, cx, cy, r, c, fn=None):
        self.ellipse(cx, cy, r, r, c, fn)

    def poly(self, pts, c):
        """dolu çoxbucaqlı (scanline)"""
        ys = [p[1] for p in pts]
        for y in range(int(min(ys)), int(max(ys)) + 1):
            xs = []
            n = len(pts)
            for i in range(n):
                (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]
                if (y0 <= y + 0.5 < y1) or (y1 <= y + 0.5 < y0):
                    xs.append(x0 + (y + 0.5 - y0) * (x1 - x0) / (y1 - y0))
            xs.sort()
            for i in range(0, len(xs) - 1, 2):
                for x in range(int(round(xs[i])), int(round(xs[i + 1]))):
                    self.px(x, y, c)

    # --- effektlər ---
    def paste(self, o, ox, oy, alpha_mul=1.0):
        for y in range(o.h):
            for x in range(o.w):
                c = o.a[y, x]
                if c[3] == 0:
                    continue
                cc = (int(c[0]), int(c[1]), int(c[2]), int(c[3] * alpha_mul))
                self.px(ox + x, oy + y, cc)

    def flipx(self):
        c = self.copy()
        c.a = self.a[:, ::-1].copy()
        return c

    def shifted(self, dx, dy):
        c = Img(self.w, self.h)
        c.paste(self, dx, dy)
        return c

    def mask(self):
        return self.a[:, :, 3] > 0

    def outline(self, col=None, k=0.45, diag=False, only_bottom=False):
        """Xarici kontur. col=None olarsa qonşu pikselin tünd çaları (Stardew kimi rəngli kontur)."""
        m = self.mask()
        out = self.a.copy()
        nb = [(1, 0), (-1, 0), (0, 1), (0, -1)]
        if diag:
            nb += [(1, 1), (1, -1), (-1, 1), (-1, -1)]
        for y in range(self.h):
            for x in range(self.w):
                if m[y, x]:
                    continue
                best = None
                for dx, dy in nb:
                    xx, yy = x + dx, y + dy
                    if 0 <= xx < self.w and 0 <= yy < self.h and m[yy, xx] and self.a[yy, xx, 3] > 200:
                        best = self.a[yy, xx]
                        break
                if best is not None:
                    if only_bottom and not (y > 0 and m[y - 1, x]):
                        continue
                    if col is not None:
                        out[y, x] = rgb(col)
                    else:
                        out[y, x] = darker(tuple(int(v) for v in best), k, '#1a1030')
        self.a = out

    def inner_shade(self, light=(-1, -1), hi=None, lo=None):
        """forma kənarlarına işıq/kölgə: yuxarı-sol işıq, aşağı-sağ kölgə"""
        m = self.mask()
        out = self.a.copy()
        for y in range(self.h):
            for x in range(self.w):
                if not m[y, x]:
                    continue
                c = tuple(int(v) for v in self.a[y, x])
                lx, ly = x + light[0], y + light[1]
                dx, dy = x - light[0], y - light[1]
                edge_l = not (0 <= lx < self.w and 0 <= ly < self.h and m[ly, lx])
                edge_d = not (0 <= dx < self.w and 0 <= dy < self.h and m[dy, dx])
                if edge_l and hi is not False:
                    out[y, x] = rgb(hi) if hi else lighter(c, 0.25)
                elif edge_d and lo is not False:
                    out[y, x] = rgb(lo) if lo else darker(c, 0.8)
        self.a = out

    def recolor(self, src, dst):
        s, d = rgb(src), rgb(dst)
        m = np.all(self.a == np.array(s, dtype=np.uint8), axis=2)
        self.a[m] = d

    def tint_all(self, c, t):
        for y in range(self.h):
            for x in range(self.w):
                if self.a[y, x, 3]:
                    cc = mix(tuple(int(v) for v in self.a[y, x]), c, t)
                    self.a[y, x] = (cc[0], cc[1], cc[2], self.a[y, x, 3])

    def pil(self):
        return Image.fromarray(self.a, 'RGBA')

    def save(self, path, scale=1):
        im = self.pil()
        if scale != 1:
            im = im.resize((self.w * scale, self.h * scale), Image.NEAREST)
        im.save(path)


def shadow_ellipse(w, h, alpha=70, col='#1c1838'):
    """yumşaq kölgə: mərkəzdə tünd, kənarda açıq"""
    im = Img(w, h)
    c = rgb(col)
    for y in range(h):
        for x in range(w):
            nx, ny = (x + 0.5 - w / 2) / (w / 2), (y + 0.5 - h / 2) / (h / 2)
            d = nx * nx + ny * ny
            if d <= 1:
                a = alpha if d < 0.55 else int(alpha * 0.6)
                im.a[y, x] = (c[0], c[1], c[2], a)
    return im


def rng(seed):
    return random.Random(seed)
