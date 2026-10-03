"""Primitivas para componer pixel art como grillas de texto (formato de PixelArtGenerator)."""
import math, os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_MemosIsland", "Art", "Source")

class Canvas:
    def __init__(s, w, h, fill='.'):
        s.w, s.h = w, h
        s.g = [[fill] * w for _ in range(h)]
    def set(s, x, y, c):
        if 0 <= x < s.w and 0 <= y < s.h:
            s.g[y][x] = c
    def get(s, x, y):
        return s.g[y][x] if 0 <= x < s.w and 0 <= y < s.h else '.'
    def rows(s):
        return [''.join(r) for r in s.g]

def wrap_set(cv, x, y, c):
    cv.set(x % cv.w, y % cv.h, c)

def stamp(cv, pattern, x0, y0, wrap=True):
    for dy, row in enumerate(pattern):
        for dx, c in enumerate(row):
            if c != '.':
                (wrap_set if wrap else cv.set)(cv, x0 + dx, y0 + dy, c)

def block(name, w, h, frames, extra=(), header=()):
    out = [f"sprite {name}", f"size {w} {h}", *header]
    for f in frames:
        if isinstance(f, str):
            out.append(f)          # instrucciones como "flip 0"
        else:
            out.append("frame")
            out += f
    out += list(extra)
    out.append("end")
    return "\n".join(out) + "\n\n"

# ------------------------------------------------------------------ formas sombreadas
def ellipse_mask(cx, cy, rx, ry):
    def m(x, y):
        return ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1
    return m

def shade_ellipse(cv, cx, cy, rx, ry, ramp, line=None, clip=None, light=(.35, -.94), hi=.55, lo=-.3):
    """ramp = (sombra, base, luz). Luz desde arriba a la izquierda."""
    m = ellipse_mask(cx, cy, rx, ry)
    pts = []
    for y in range(cv.h):
        for x in range(cv.w):
            if m(x, y) and (clip is None or clip(x, y)):
                pts.append((x, y))
    S = set(pts)
    for x, y in pts:
        nx, ny = (x + .5 - cx) / rx, (y + .5 - cy) / ry
        sc = nx * light[0] + ny * light[1]
        c = ramp[2] if sc > hi else ramp[0] if sc < lo else ramp[1]
        if line:
            edge = any((x + dx, y + dy) not in S and cv.get(x + dx, y + dy) != '.'
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            if edge:
                c = line
        cv.set(x, y, c)
    return S

def poly_fill(cv, pts, c):
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    for y in range(min(ys), max(ys) + 1):
        for x in range(min(xs), max(xs) + 1):
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]; xj, yj = pts[j]
                if ((yi > y + .5) != (yj > y + .5)) and (x + .5 < (xj - xi) * (y + .5 - yi) / (yj - yi) + xi):
                    inside = not inside
                j = i
            if inside:
                cv.set(x, y, c)

def flame(cv, chain, ramp=('2', '3', '4', 'p')):
    """chain = [(x, y, r)] círculos de la base a la punta. Capas de afuera hacia adentro."""
    for layer, scale in enumerate((1.0, .72, .45, .2)):
        for (x0, y0, r) in chain:
            rr = r * scale
            for y in range(cv.h):
                for x in range(cv.w):
                    if (x + .5 - x0) ** 2 + (y + .5 - y0) ** 2 <= rr * rr:
                        cv.set(x, y, ramp[layer])

def lerp_chain(a, b, n):
    return [(a[0] + (b[0] - a[0]) * t / (n - 1), a[1] + (b[1] - a[1]) * t / (n - 1),
             a[2] + (b[2] - a[2]) * t / (n - 1)) for t in range(n)]

