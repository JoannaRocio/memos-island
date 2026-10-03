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


# ================================================================== PISTA DE CARRERA
track = "# Pista de carrera (vista de perfil): suelo de cada terreno (16x16), bandera de tramo y meta.\n\n"

# (id, base, superficie, luz, detalles)
TERRAIN_LOOK = [
    ("pradera", '6', '5', 'c', 'm'), ("bosque", 'm', '6', '5', 'j'), ("arena", '4', 'p', 'c', 'o'),
    ("barro", 'i', 'o', 'p', 'j'), ("rio", '9', 'a', 'c', '8'), ("hielo", 'a', 'b', 'c', '9'),
    ("nieve", 'c', 'c', 'c', 'd'), ("ceniza", 'f', 'e', 'd', '2'), ("montana", 'e', 'd', 'c', 'f'),
    ("cueva", 'f', 'e', 'd', '0'), ("tormenta", '8', '9', 'b', '0'), ("aire", 'c', 'b', 'c', 'a'),
]
for i, (tid, base, top, light, detail) in enumerate(TERRAIN_LOOK):
    cv = Canvas(16, 16, base)
    rnd = random.Random(100 + i)
    for x in range(16):
        cv.set(x, 0, top); cv.set(x, 1, top)
        if rnd.random() < .35: cv.set(x, 0, light)
        if rnd.random() < .3: cv.set(x, 2, top)
    for _ in range(9):
        x, y = rnd.randrange(16), rnd.randrange(3, 16)
        cv.set(x, y, detail)
        if rnd.random() < .5: cv.set((x + 1) % 16, y, detail)
    if tid in ("rio", "tormenta"):
        for y in (5, 10, 14):
            off = rnd.randrange(16)
            for k in range(4): cv.set((off + k) % 16, y, top)
    if tid == "ceniza":
        for _ in range(4): cv.set(rnd.randrange(16), rnd.randrange(3, 16), '3')
    if tid == "aire":
        for y in (6, 11):
            off = rnd.randrange(16)
            for k in range(6): cv.set((off + k) % 16, y, 'd')
    track += block(f"track_{tid}", 16, 16, [cv.rows()], header=["pivot 0 1"])

flag = [("..f" + ("22222" if y < 5 else ".....") + "........")[:16] for y in range(24)]
flag = [r[:2] + 'f' + r[3:] for r in flag]
track += block("track_flag", 16, 24, [flag], header=["pivot 0.15 0", "outline 0"])

finish = Canvas(16, 16)
for y in range(16):
    for x in range(16):
        finish.set(x, y, 'c' if ((x // 4) + (y // 4)) % 2 == 0 else '0')
track += block("track_finish", 16, 16, [finish.rows()], header=["pivot 0 1"])
open(os.path.join(OUT, "Track.txt"), "w", encoding="utf-8").write(track)


# ================================================================== INTERIOR DEL REFUGIO
inside = "# Interior del refugio: piso, paredes, ventana, alfombra, felpudo de salida.\n\n"

floor = Canvas(16, 16, 'o')
for y in range(16):
    if y % 4 == 3:
        for x in range(16): floor.set(x, y, 'i')
    elif y % 4 == 0:
        for x in range(16): floor.set(x, y, 'p' if x % 7 else 'o')
for (x, y) in [(5, 1), (5, 2), (12, 5), (12, 6), (2, 9), (2, 10), (9, 13), (9, 14)]:
    floor.set(x, y, 'i')
inside += block("in_floor", 16, 16, [floor.rows()], extra=["tile"])

wall = Canvas(16, 16, 'p')
for (x, y) in [(3, 3), (11, 3), (7, 7), (3, 11), (11, 11)]:
    wall.set(x, y, 'h'); wall.set(x + 1, y, 'h'); wall.set(x, y + 1, 'h')
for x in range(16):
    wall.set(x, 13, 'i'); wall.set(x, 14, 'j'); wall.set(x, 15, 'j')
    wall.set(x, 12, 'o')
inside += block("in_wall", 16, 16, [wall.rows()], extra=["tile solid"])

top = Canvas(16, 16, 'j')
for x in range(16):
    top.set(x, 0, 'i'); top.set(x, 15, 'f')
for y in (5, 10):
    for x in range(16): top.set(x, y, 'i')
inside += block("in_wall_top", 16, 16, [top.rows()], extra=["tile solid"])

win = Canvas(16, 16)
win.g = [r[:] for r in wall.g]
for y in range(2, 11):
    for x in range(3, 13):
        win.set(x, y, 'i')
for y in range(3, 10):
    for x in range(4, 12):
        win.set(x, y, 'a' if y > 5 else 'b')
for y in range(3, 10): win.set(8, y, 'i')
for x in range(4, 12): win.set(x, 6, 'i')
win.set(5, 4, 'c'); win.set(10, 7, 'c')
inside += block("in_window", 16, 16, [win.rows()], extra=["tile solid"])

rug = Canvas(16, 16, '2')
for y in range(16):
    for x in range(16):
        if (x + y) % 8 == 0 or (x - y) % 8 == 0: rug.set(x, y, 'n')
        if (x + y) % 8 == 4 and (x - y) % 8 == 4: rug.set(x, y, '4')
inside += block("in_rug", 16, 16, [rug.rows()], extra=["tile"])

mat = Canvas(16, 16)
mat.g = [r[:] for r in floor.g]
for y in range(3, 14):
    for x in range(2, 14):
        mat.set(x, y, 'n' if 3 < y < 13 and 2 < x < 13 else 'j')
for x in range(5, 11, 2): mat.set(x, 8, '4')
inside += block("in_doormat", 16, 16, [mat.rows()], extra=["tile"])
open(os.path.join(OUT, "Inside.txt"), "w", encoding="utf-8").write(inside)

# Muebles (pivote abajo al centro)
furniture = "# Muebles del refugio. Pivote abajo al centro.\n\n"

bed = Canvas(32, 32)
for y in range(4, 31):
    for x in range(3, 29):
        bed.set(x, y, 'j' if x in (3, 28) or y in (4, 30) else 'i')
for y in range(6, 13):
    for x in range(6, 26):
        bed.set(x, y, 'c' if y < 11 else 'd')
for y in range(13, 29):
    for x in range(5, 27):
        bed.set(x, y, '9' if (x + y) % 6 else 'a')
for x in range(5, 27): bed.set(x, 13, 'a')
furniture += block("bed", 32, 32, [bed.rows()], header=["pivot 0.5 0", "outline 0"])

cushion = Canvas(16, 16)
shade_ellipse(cushion, 8, 10, 7, 4.5, ('2', 'n', '3'))
shade_ellipse(cushion, 8, 9.5, 4.5, 2.5, ('n', '3', '4'))
furniture += block("memo_bed", 16, 16, [cushion.rows()], header=["pivot 0.5 0", "outline 0"])

def bowl(full):
    cv = Canvas(16, 16)
    shade_ellipse(cv, 8, 11, 6.5, 3.5, ('e', 'd', 'c'))
    shade_ellipse(cv, 8, 9.5, 5, 2, ('n', '3', '4') if full else ('f', 'f', 'e'))
    if full:
        for (x, y) in [(6, 8), (9, 8), (8, 9)]: cv.set(x, y, '2')
    return cv
furniture += block("bowl_full", 16, 16, [bowl(True).rows()], header=["pivot 0.5 0", "outline 0"])
furniture += block("bowl_empty", 16, 16, [bowl(False).rows()], header=["pivot 0.5 0", "outline 0"])

table = Canvas(32, 24)
for y in range(4, 14):
    for x in range(2, 30):
        table.set(x, y, 'o' if y > 5 else 'p')
for y in range(14, 23):
    for x in (4, 5, 26, 27): table.set(x, y, 'j')
for x in range(2, 30): table.set(x, 13, 'i')
furniture += block("table", 32, 24, [table.rows()], header=["pivot 0.5 0", "outline 0"])

plant = Canvas(16, 24)
for (cx, cy, r) in [(8, 8, 5), (5, 11, 3.5), (11, 11, 3.5), (8, 4, 3)]:
    shade_ellipse(plant, cx, cy, r, r, ('m', '6', '5'))
for y in range(15, 23):
    for x in range(4, 12):
        plant.set(x, y, 'n' if y > 15 else '3')
furniture += block("plant", 16, 24, [plant.rows()], header=["pivot 0.5 0", "outline 0"])
open(os.path.join(OUT, "Furniture.txt"), "w", encoding="utf-8").write(furniture)


# ================================================================== ACCESORIOS (Fase 5)
accessories = "# Accesorios estéticos que se ven sobre los Memos. Cabeza: pivote abajo al centro. Cuello: pivote al centro.\n\n"

def acc_canvas(rows):
    w = max(len(r) for r in rows)
    return [r.ljust(w, '.') for r in rows], w, len(rows)

HEAD = {
    "acc_gorrito_rojo": ["...44...", "..2222..", ".223322.", ".222222.", "pppppppp"],
    "acc_gorrito_azul": ["...cc...", "..9999..", ".99aa99.", ".999999.", "cccccccc"],
    "acc_mono_rosa": ["22...22.", "2n2.2n2.", ".22222..", "2n2.2n2.", "22...22."],
    "acc_corona_flores": [".2.4.c.2.", "626565626", "666666666"],
    "acc_sombrero_paja": ["...oooo...", "..o4444o..", "..o2222o..", "oooooooooo", ".4444444.."],
    "acc_estrellita": ["..4..", ".444.", "44p44", ".444.", "4...4"],
}
NECK = {
    "acc_bufanda_roja": ["2222222222", "2323232322", ".......22.", ".......32.", ".......22."],
    "acc_bufanda_verde": ["6666666666", "6565656566", ".......66.", ".......56.", ".......66."],
    "acc_panuelo_azul": ["9999999999", ".9aa9aa99.", "..9a9a99..", "...999...", "....9....."],
    "acc_cascabel": ["2222222222", "....44....", "...4p44...", "...4444...", "....00...."],
}
for name, rows in HEAD.items():
    rows, w, h = acc_canvas(rows)
    accessories += block(name, w, h, [rows], header=["pivot 0.5 0", "outline 0"])
for name, rows in NECK.items():
    rows, w, h = acc_canvas(rows)
    accessories += block(name, w, h, [rows], header=["pivot 0.5 0.5", "outline 0"])
open(os.path.join(OUT, "Accessories.txt"), "w", encoding="utf-8").write(accessories)

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

bubble = ["..cccccccccc..",
          ".cccccccccccc.",
          "cccccccccccccc",
          "cccccccccccccc",
          "cccccccccccccc",
          "cccccccccccccc",
          "cccccccccccccc",
          "cccccccccccccc",
          ".cccccccccccc.",
          "..cccccccccc..",
          "......ccc.....",
          ".......c......"]
ui += block("ui_bubble", 14, 12, [bubble], header=["pivot 0.5 0", "outline 0"])
open(os.path.join(OUT, "UI.txt"), "w", encoding="utf-8").write(ui)

print("ok")
