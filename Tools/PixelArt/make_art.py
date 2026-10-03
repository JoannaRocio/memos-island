"""Compone pixel art con formas sombreadas (tiles, props e interfaz) y escribe las grillas de texto
de Art/Source que lee PixelArtGenerator. Los Memos los genera make_memos.py y Characters.txt se dibuja a mano.
Uso: python make_art.py"""
import math, random, os

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_MemosIsland", "Art", "Source")
os.makedirs(OUT, exist_ok=True)

from pixel_lib import *

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


# Arena de playa
sand = Canvas(16, 16, '4')
rnd = random.Random(5)
for (x, y) in [(2, 3), (9, 1), (13, 7), (5, 10), (11, 13), (1, 14)]:
    wrap_set(sand, x, y, 'o'); wrap_set(sand, x + 1, y, 'o')
for _ in range(7):
    wrap_set(sand, rnd.randrange(16), rnd.randrange(16), 'p')
tiles += block("sand", 16, 16, [sand.rows()], extra=["tile"])

# Tablas del muelle (sobre el agua, se camina)
dock = Canvas(16, 16, 'o')
for y in range(16):
    if y % 4 == 3:
        for x in range(16): dock.set(x, y, 'j')
    elif y % 4 == 0:
        for x in range(16): dock.set(x, y, 'p')
    dock.set(0, y, 'i'); dock.set(15, y, 'i')
for y in (1, 5, 9, 13):
    dock.set(2, y, 'f'); dock.set(13, y, 'f')
tiles += block("dock", 16, 16, [dock.rows()], extra=["tile"])

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

def pad(rows, w, h):
    rows = [r.ljust(w, '.')[:w] for r in rows]
    return ['.' * w] * (h - len(rows)) + rows

sign = pad([
    "..oooooooooooo..",
    "..oppppppppppo..",
    "..ojjjoojjjjoo..",
    "..oooooooooooo..",
    "..ojjjjoojjjoo..",
    "..oooooooooooo..",
    "..iiiiiiiiiiii..",
    "......ij........",
    "......ij........",
    "......ij........",
    "......ij........",
    "......jj........",
    "................",
], 16, 16)
props += block("sign", 16, 16, [sign], header=["pivot 0.5 0", "outline 0"])

lamp = pad([
    ".....ffffff.....",
    "....ffeeeeff....",
    ".....f4pp4f.....",
    ".....f4pp4f.....",
    ".....f4444f.....",
    ".....ffffff.....",
] + [".......ef......."] * 16 + [
    "......ffff......",
    ".....feeeff.....",
    ".....ffffff.....",
    "................",
], 16, 32)
props += block("lamp", 16, 32, [lamp], header=["pivot 0.5 0", "outline 0"])

open(os.path.join(OUT, "Props.txt"), "w", encoding="utf-8").write(props)

# ================================================================== UI
ui = "# Interfaz: caja de texto estilo GBA (9 cortes) y pixel blanco para fundidos.\n\n"
corner = ["..888888",
          ".8aaaaaa",
          "8aaaaaaa",
          "8aa99999",
          "8aa9cccc",
          "8aa9cccc",
          "8aa9cccc",
          "8aa9cccc"]
edge_col = [r[7] for r in corner]
top = [corner[y] + edge_col[y] * 8 + corner[y][::-1] for y in range(8)]
mid = [corner[7] + 'c' * 8 + corner[7][::-1]] * 8
box = top + mid + top[::-1]
ui += block("ui_box", 24, 24, [box], header=["border 8 8 8 8"])
ui += block("ui_pixel", 2, 2, [["cc", "cc"]])
open(os.path.join(OUT, "UI.txt"), "w", encoding="utf-8").write(ui)

print("ok")
