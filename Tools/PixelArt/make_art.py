"""Compone pixel art con formas sombreadas (tiles, árbol, Tostín) y escribe las grillas de texto
de Art/Source que lee PixelArtGenerator. Characters.txt se dibuja a mano y este script no lo toca.
Uso: python make_art.py"""
import math, random, os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_MemosIsland", "Art", "Source")
os.makedirs(OUT, exist_ok=True)

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

# ================================================================== TILES
random.seed(7)
tiles = "# Tiles del mundo (16x16). Se generan como Tile en Art/Tiles.\n\n"

def grass_base(seed):
    rnd = random.Random(seed)
    cv = Canvas(16, 16, '6')
    tuft = ["5.5", ".m."]
    for (x, y) in [(2, 2), (10, 5), (5, 9), (12, 12), (1, 13)]:
        stamp(cv, tuft, x, y)
    for _ in range(4):
        wrap_set(cv, rnd.randrange(16), rnd.randrange(16), '5')
    return cv

g = grass_base(1)
tiles += block("grass", 16, 16, [g.rows()], extra=["tile"])

# Pasto alto (encuentros): matas en V, dos filas desfasadas
tall = Canvas(16, 16, 'm')
blade = ["5...5",
         "65.56",
         "66566",
         "m666m",
         ".mmm."]
for (x, y) in [(0, 0), (8, 0), (4, 8), (12, 8)]:
    stamp(tall, blade, x - 2, y + 1)
for (x, y) in [(3, 6), (11, 6), (7, 14), (15, 14)]:
    wrap_set(tall, x, y, '6')
tall2 = Canvas(16, 16, 'm')
blade2 = [".5..5",
          ".6556",
          "66566",
          "m666m",
          ".mmm."]
for (x, y) in [(0, 0), (8, 0), (4, 8), (12, 8)]:
    stamp(tall2, blade2, x - 2, y + 1)
for (x, y) in [(3, 6), (11, 6), (7, 14), (15, 14)]:
    wrap_set(tall2, x, y, '6')
tiles += block("tall_grass", 16, 16, [tall.rows(), tall2.rows()], extra=["tile 1.5"])

# Flores sobre pasto (dos colores) con leve balanceo
def flowers(sway):
    cv = grass_base(2)
    red = [".2.", "242", ".2."] if not sway else [".2.", "242", ".2."]
    white = [".c.", "c4c", ".c."]
    for (x, y, p) in [(2, 3, red), (10, 2, white), (6, 10, white), (12, 11, red)]:
        stamp(cv, p, x + (1 if sway and p is red else 0), y)
        wrap_set(cv, x + 1, y + 3, 'm')
    return cv
tiles += block("flowers", 16, 16, [flowers(False).rows(), flowers(True).rows()], extra=["tile 1"])

# Camino de tierra
path = Canvas(16, 16, 'o')
rnd = random.Random(3)
for (x, y) in [(3, 2), (11, 4), (6, 9), (13, 12), (1, 13), (8, 14)]:
    wrap_set(path, x, y, 'i'); wrap_set(path, x + 1, y, 'i')
    wrap_set(path, x, y - 1, 'p')
for _ in range(6):
    wrap_set(path, rnd.randrange(16), rnd.randrange(16), '4')
tiles += block("path", 16, 16, [path.rows()], extra=["tile"])

# Agua con olas que se desplazan
def water(off):
    cv = Canvas(16, 16, '9')
    for (x, y) in [(1, 2), (9, 6), (4, 11), (12, 14)]:
        for i in range(4):
            wrap_set(cv, x + off + i, y, 'a')
        wrap_set(cv, x + off + 1, y - 1, 'b')
        wrap_set(cv, x + off + 2, y - 1, 'b')
    wrap_set(cv, 7 + off, 3, 'c')
    wrap_set(cv, 14 + off, 9, 'c')
    return cv
tiles += block("water", 16, 16, [water(0).rows(), water(2).rows(), water(4).rows(), water(2).rows()],
               extra=["tile 2 solid"])

# Cerca de madera
fence = [
    "................",
    "................",
    "..jj........jj..",
    ".joij......joij.",
    ".jooj......jooj.",
    "jjooojjjjjjjooojj"[:16],
    "ioooooooooooooooi"[:16],
    "iiiiiiiiiiiiiiii",
    "jjooojjjjjjjooojj"[:16],
    "ioooooooooooooooi"[:16],
    "iiiiiiiiiiiiiiii",
    ".jioj......jioj.",
    ".jioj......jioj.",
    ".jiij......jiij.",
    "..jj........jj..",
    "................",
]
tiles += block("fence", 16, 16, [fence], extra=["tile solid"])

# Pared de casa (tablas crema)
wall = Canvas(16, 16, 'p')
for y in (3, 7, 11, 15):
    for x in range(16):
        wall.set(x, y, 'o')
for y in (0, 4, 8, 12):
    for x in range(16):
        wall.set(x, y, 'c')
for (x, y) in [(5, 1), (5, 2), (12, 5), (12, 6), (3, 9), (3, 10), (10, 13), (10, 14)]:
    wall.set(x, y, 'o')
tiles += block("house_wall", 16, 16, [wall.rows()], extra=["tile solid"])

# Ventana sobre la pared
win = Canvas(16, 16)
win.g = [r[:] for r in wall.g]
for y in range(2, 13):
    for x in range(2, 14):
        win.set(x, y, 'j')
for y in range(3, 12):
    for x in range(3, 13):
        win.set(x, y, 'a' if (x + y) % 9 else 'c')
for y in range(3, 12):
    win.set(8, y, 'j')
for x in range(3, 13):
    win.set(x, 7, 'j')
for (x, y) in [(4, 4), (5, 4), (4, 5), (9, 8), (10, 8), (9, 9)]:
    win.set(x, y, 'b')
for x in range(1, 15):
    win.set(x, 13, 'i'); win.set(x, 14, 'j')
tiles += block("house_window", 16, 16, [win.rows()], extra=["tile solid"])

# Puerta
door = Canvas(16, 16)
door.g = [r[:] for r in wall.g]
for y in range(1, 16):
    for x in range(3, 13):
        door.set(x, y, 'j')
for y in range(2, 16):
    for x in range(4, 12):
        door.set(x, y, 'i' if x not in (7, 8) else 'j')
for x in range(4, 12):
    door.set(x, 2, 'o')
door.set(10, 9, '4'); door.set(10, 10, 'n')
tiles += block("house_door", 16, 16, [door.rows()], extra=["tile"])

# Techo de tejas (escamas)
roof = Canvas(16, 16, 'n')
for row in range(4):
    y = row * 4
    off = 0 if row % 2 == 0 else 4
    for x in range(16):
        roof.set(x, y + 3, '2')
    for k in range(2):
        cx = off + k * 8
        for dx in range(1, 7):
            wrap_set(roof, cx + dx, y, '3' if dx in (2, 3) else 'n')
        wrap_set(roof, cx, y + 1, '2'); wrap_set(roof, cx, y + 2, '2')
        wrap_set(roof, cx + 2, y + 1, '3')
tiles += block("house_roof", 16, 16, [roof.rows()], extra=["tile solid"])

# Borde inferior del techo (alero con sombra)
eave = Canvas(16, 16)
eave.g = [r[:] for r in roof.g]
for y in range(10, 16):
    for x in range(16):
        eave.set(x, y, '1' if y < 12 else ('j' if y < 13 else 'i'))
for x in range(0, 16, 4):
    eave.set(x, 13, 'j'); eave.set(x, 14, 'j'); eave.set(x, 15, 'j')
for y in range(13, 16):
    for x in range(16):
        if eave.get(x, y) == 'i' and y == 13:
            eave.set(x, y, 'o')
tiles += block("house_roof_edge", 16, 16, [eave.rows()], extra=["tile solid"])

open(os.path.join(OUT, "Tiles.txt"), "w", encoding="utf-8").write(tiles)

# ================================================================== PROPS
props = "# Objetos grandes del mundo. Pivote abajo al centro.\n\n"

def tree():
    cv = Canvas(32, 32)
    # tronco
    for y in range(20, 30):
        for x in range(13, 19):
            cv.set(x, y, 'i' if x < 16 else 'j')
    for x in range(12, 20):
        cv.set(x, 29, 'j')
    cv.set(14, 24, 'j'); cv.set(14, 25, 'j')
    # copa: racimo de círculos, del fondo al frente
    blobs = [(9, 18, 6, 5), (23, 18, 6, 5), (16, 19, 7, 5),
             (10, 12, 6, 5), (22, 12, 6, 5), (16, 8, 7, 6), (16, 14, 6, 5)]
    for (cx, cy, rx, ry) in blobs:
        shade_ellipse(cv, cx, cy, rx, ry, ('m', '6', '5'), line='m', light=(-.5, -.85), hi=.35, lo=-.35)
    rnd = random.Random(11)
    # textura de hojas
    for _ in range(40):
        x, y = rnd.randrange(32), rnd.randrange(32)
        c = cv.get(x, y)
        if c == '6' and cv.get(x + 1, y) == '6':
            cv.set(x, y, '5' if y < 14 else 'm')
    # manzanitas
    for (x, y) in [(11, 12), (20, 9), (22, 18)]:
        cv.set(x, y, '2'); cv.set(x + 1, y, '2'); cv.set(x, y + 1, '2'); cv.set(x + 1, y + 1, '1')
        cv.set(x, y, '3')
    return cv

props += block("tree", 32, 32, [tree().rows()], header=["pivot 0.5 0", "outline 0"])
open(os.path.join(OUT, "Props.txt"), "w", encoding="utf-8").write(props)

# ================================================================== MEMOS
memos = "# Memos. Mundo 16x16 (pivote abajo) y carrera 64x64 (pivote abajo).\n" \
        "# Variantes: brillante (shiny) y concollar (desaturado; el collar se dibuja aparte).\n\n"

SHINY = "variant brillante 3>a 2>9 4>b p>c n>8"
COLLAR = "variant concollar 3>e 2>f 4>d p>d n>f c>e"

tostin_world_0 = [
    "................",
    "................",
    "..3......3.4....",
    "..33....33343...",
    "..333333333424..",
    "..3333333333....",
    "..30c3330c33....",
    "..3003330033....",
    "..2344444432....",
    "..334404433.....",
    "...3333333......",
    "..334444433.....",
    "..334444433.....",
    "..333...333.....",
    "..222...222.....",
    "................",
]
tostin_world_1 = tostin_world_0[:2] + [
    "..3......3..4...",
    "..33....333343..",
    "..33333333342...",
] + tostin_world_0[5:13] + [
    "...333..333.....",
    "...222..222.....",
    "................",
]
memos += block("tostin_world", 16, 16, [tostin_world_0, tostin_world_1],
               header=["pivot 0.5 0", "outline 0"], extra=[SHINY, COLLAR])

def tostin_race(phase):
    """Vista lateral mirando a la derecha. phase 0/1 = patas alternadas al correr."""
    cv = Canvas(64, 64)
    bob = -1 if phase == 1 else 0
    # cola de fuego (atrás)
    sway = 1 if phase else 0
    def tongue(x0, y0, r0, x1, y1, r1, wob, n=10):
        pts = []
        for t in range(n):
            u = t / (n - 1)
            pts.append((x0 + (x1 - x0) * u + math.sin(u * 3.2 + sway) * wob,
                        y0 + (y1 - y0) * u, r0 + (r1 - r0) * u ** 1.3))
        return pts
    flame(cv, tongue(17, 41 + bob, 6.5, 4 - sway, 26 + bob, 2.2, 1.5)
              + tongue(14, 34 + bob, 4.5, 9 + sway, 15 + bob, 1.3, 2.0)
              + tongue(18, 36 + bob, 3.5, 18, 24 + bob, 1.0, 1.0, 6))
    # patas lejanas (más oscuras)
    far = ('1', '2', '2')
    if phase == 0:
        shade_ellipse(cv, 21, 53, 3.5, 6, far); shade_ellipse(cv, 38, 54, 3.5, 6, far)
    else:
        shade_ellipse(cv, 25, 52, 3.5, 6, far); shade_ellipse(cv, 34, 53, 3.5, 6, far)
    # cuerpo
    body = ('2', '3', '4')
    shade_ellipse(cv, 29, 44 + bob, 14, 10.5, body, line='j')
    # panza
    bm = ellipse_mask(29, 44 + bob, 13, 9.5)
    shade_ellipse(cv, 33, 49 + bob, 9, 5.5, ('4', 'p', 'p'), clip=bm)
    # patas cercanas
    near = ('2', '3', '4')
    if phase == 0:
        shade_ellipse(cv, 25, 55, 4, 6.5, near, line='j'); shade_ellipse(cv, 41, 55, 4, 6.5, near, line='j')
    else:
        shade_ellipse(cv, 20, 54, 4, 6.5, near, line='j'); shade_ellipse(cv, 44, 54, 4, 6.5, near, line='j')
    for (fx) in ((25, 41) if phase == 0 else (20, 44)):
        for dx in (-2, -1, 0, 1, 2):
            cv.set(fx + dx, 61, '2')
    # orejas
    poly_fill(cv, [(33, 18 + bob), (34, 4 + bob), (44, 14 + bob)], '3')
    poly_fill(cv, [(35, 15 + bob), (36, 8 + bob), (41, 14 + bob)], '2')
    poly_fill(cv, [(45, 13 + bob), (52, 3 + bob), (55, 16 + bob)], '3')
    poly_fill(cv, [(47, 13 + bob), (51, 7 + bob), (53, 14 + bob)], '2')
    # cabeza
    shade_ellipse(cv, 44, 27 + bob, 14, 13, body, line='j')
    # mechón de fuego
    flame(cv, lerp_chain((44, 15 + bob, 3.2), (42, 7 + bob, 1.2), 5))
    # hocico
    shade_ellipse(cv, 53, 33 + bob, 7, 5.5, ('4', 'p', 'p'))
    # nariz
    for (x, y) in [(58, 30), (59, 30), (58, 31), (59, 31)]:
        cv.set(x, y + bob, '0')
    cv.set(58, 30 + bob, 'e')
    # ojo grande
    for y in range(20, 30):
        for x in range(45, 52):
            if ((x + .5 - 48.5) / 3.5) ** 2 + ((y + .5 - 25) / 5) ** 2 <= 1:
                cv.set(x, y + bob, '0')
    for (x, y) in [(49, 21), (50, 21), (49, 22), (50, 22), (47, 27)]:
        cv.set(x, y + bob, 'c')
    for (x, y) in [(46, 25), (46, 26)]:
        cv.set(x, y + bob, '8')
    # boca sonriente
    for (x, y) in [(52, 37), (53, 38), (54, 38), (55, 38), (56, 37)]:
        cv.set(x, y + bob, 'j')
    cv.set(54, 39 + bob, '2'); cv.set(55, 39 + bob, '2')
    # cachete
    for (x, y) in [(40, 32), (41, 32), (42, 32), (41, 33)]:
        cv.set(x, y + bob, 'n')
    return cv

memos += block("tostin_race", 64, 64, [tostin_race(0).rows(), tostin_race(1).rows()],
               header=["pivot 0.5 0", "outline 0"], extra=[SHINY, COLLAR])
open(os.path.join(OUT, "Memos.txt"), "w", encoding="utf-8").write(memos)
print("ok")
