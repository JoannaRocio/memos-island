"""Arte de la Fase 7 (vecinos): los 9 vecinos en el mundo (16x32, a partir del sprite del jugador con otros
colores y detalles), sus retratos de 48x48, muebles y carteles del pueblo, y la lluvia.
Escribe Neighbors.txt, Portraits.txt, Town.txt y Weather.txt en Art/Source.
Uso: python make_neighbors.py"""
import random
from pixel_lib import *


def write(name, text):
    open(os.path.join(OUT, name), "w", encoding="utf-8").write(text)


def ell(cv, cx, cy, rx, ry, ramp, **kw):
    return shade_ellipse(cv, cx, cy, rx, ry, ramp, **kw)


def px(cv, pts, c):
    for (x, y) in pts:
        cv.set(x, y, c)


def rect(cv, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            cv.set(x, y, c)


# ================================================================== Sprites del jugador (base)

def read_player_frames():
    """{"down": [frame, frame], "up": [...], "right": [f, f, f]} desde Characters.txt."""
    lines = open(os.path.join(OUT, "Characters.txt"), encoding="utf-8").read().splitlines()
    frames, name, cur = {}, None, None
    for ln in lines:
        if ln.startswith("sprite "):
            name = ln.split()[1]
            frames[name] = []
            cur = None
        elif ln == "frame":
            cur = []
            frames[name].append(cur)
        elif cur is not None and len(ln) == 16 and set(ln) <= set(".0123456789abcdefghijklmnop"):
            cur.append(ln)
    return {d: [[list(r) for r in f] for f in frames[f"player_{d}"]] for d in ("down", "up", "right")}


BASE = read_player_frames()

# Colores del jugador: pelo i/j, piel g/h, bufanda 2, remera a/9, pantalón 8, zapatos f.
NEIGHBORS = {
    #           pelo       piel       bufanda  remera     pantalón zapatos  detalles
    "deny":  dict(hair="43", skin="gh", scarf="c", shirt="67", pants="i", shoes="j", features=["mustache"]),
    "fer":   dict(hair="f0", skin="hn", scarf="f", shirt="ef", pants="f", shoes="0", features=["bandana"]),
    "anni":  dict(hair="32", skin="gh", scarf="b", shirt="cd", pants="9", shoes="e", features=["long"]),
    "lalo":  dict(hair="j0", skin="gh", scarf="4", shirt="32", pants="8", shoes="2", features=["spiky"]),
    "luca":  dict(hair="p4", skin="gh", scarf="c", shirt="f0", pants="f", shoes="0", features=[]),
    "zorak": dict(hair="de", skin="hn", scarf="7", shirt="ij", pants="f", shoes="j", features=["beard"]),
    "jojo":  dict(hair="n2", skin="gh", scarf="5", shirt="56", pants="a", shoes="2", features=["kid"]),
    "marga": dict(hair="cd", skin="gh", scarf="4", shirt="21", pants="1", shoes="0", features=["bun"]),
    "vera":  dict(hair="98", skin="gh", scarf="2", shirt="de", pants="f", shoes="0", features=["cap"]),
}


def recolor(frame, spec):
    m = {'i': spec["hair"][0], 'j': spec["hair"][1], 'g': spec["skin"][0], 'h': spec["skin"][1],
         '2': spec["scarf"], 'a': spec["shirt"][0], '9': spec["shirt"][1], '8': spec["pants"], 'f': spec["shoes"]}
    return [[m.get(c, c) for c in row] for row in frame]


def span(row):
    xs = [x for x, c in enumerate(row) if c != '.']
    return (xs[0], xs[-1]) if xs else (None, None)


def apply_features(frame, d, spec, original):
    hair1, hair2 = spec["hair"]
    skin = set(spec["skin"])
    f = frame
    for feat in spec["features"]:
        if feat == "long":
            if d == "up":
                for y in range(19, 23):
                    for x in range(3, 13): f[y][x] = hair1 if y < 21 else hair2
            else:
                for y in range(14, 22):
                    x0, x1 = span(f[y])
                    if x0 is None: continue
                    f[y][max(0, x0 - 1)] = hair2
                    if d == "down": f[y][min(15, x1 + 1)] = hair2
        elif feat == "spiky":
            x0, x1 = span(f[10])
            for x in range(x0, x1 + 1, 2): f[9][x] = hair1
            f[8][x0 + 1] = hair1
        elif feat == "bun":
            x0, x1 = span(f[10])
            c = (x0 + x1) // 2
            for x in range(c - 1, c + 3): f[9][x] = hair1
            for x in range(c, c + 2): f[8][x] = hair1
        elif feat == "cap":
            for y in range(10, 14):
                for x in range(16):
                    if original[y][x] in "ij": f[y][x] = 'c' if y < 13 else 'd'
            if d == "down": f[11][7] = f[11][8] = '4'
            if d == "right":
                for x in range(10, 14): f[13][x] = '8'
        elif feat == "bandana":
            for y in (12, 13):
                for x in range(16):
                    if original[y][x] in "ij": f[y][x] = '2'
        elif feat == "beard":
            xs = range(4, 12) if d == "down" else range(8, 13)
            if d != "up":
                for y in (19, 20, 21):
                    for x in xs:
                        if f[y][x] in skin or f[y][x] == '3' or y == 21 and f[y][x] != '.': f[y][x] = 'd'
        elif feat == "mustache" and d == "down":
            for x in range(6, 10): f[19][x] = hair2
        elif feat == "kid":
            del f[27]
            del f[23]
            f.insert(0, list('.' * 16))
            f.insert(0, list('.' * 16))
    return f


neighbors_txt = "# Vecinos en el mundo (16x32): el sprite del jugador con otros colores y detalles. Generado por make_neighbors.py.\n\n"
for nid, spec in NEIGHBORS.items():
    for d in ("down", "up", "right"):
        frames = []
        for fr in BASE[d]:
            frames.append(["".join(r) for r in apply_features(recolor(fr, spec), d, spec, fr)])
        extra = ["flip 1"] if d in ("down", "up") else []
        neighbors_txt += block(f"{nid}_{d}", 16, 32, frames, header=["pivot 0.5 0", "outline 0"], extra=extra)
    neighbors_txt += f"sprite {nid}_left\nsize 16 32\npivot 0.5 0\noutline 0\ncopyfrom {nid}_right\nmirror\nend\n\n"
write("Neighbors.txt", neighbors_txt)

# ================================================================== Retratos 48x48

LIGHT = {'g': 'p', 'h': 'g'}
SHIRT_LIGHT = {'6': '5', 'e': 'd', 'c': 'c', '3': '4', 'f': 'e', 'i': 'o', '5': 'p', '2': '3', 'd': 'c'}


def portrait(nid, spec):
    cv = Canvas(48, 48)
    hair1, hair2 = spec["hair"]
    skin, skin_d = spec["skin"]
    sh1, sh2 = spec["shirt"]
    feats = spec["features"]
    kid = "kid" in feats
    hy = 23 if kid else 21                     # centro de la cabeza
    hr = (13, 13) if kid else (12, 13)

    if "long" in feats:                         # pelo largo, detrás de todo
        ell(cv, 24, hy + 8, 15.5, 19, (hair2, hair1, hair1))
    # Hombros y cuello
    ell(cv, 24, 52, 21 if not kid else 17, 15, (sh2, sh1, SHIRT_LIGHT.get(sh1, sh1)), hi=.75, lo=-.45)
    rect(cv, 19, hy + 10, 28, hy + 15, skin_d)
    if spec["scarf"] not in (sh1, sh2):        # cuello de la ropa / pañuelo
        ell(cv, 24, hy + 17, 9, 3, (spec["scarf"], spec["scarf"], spec["scarf"]))
    # Cabeza
    ell(cv, 24, hy, hr[0], hr[1], (skin_d, skin, LIGHT.get(skin, skin)), hi=.8, lo=-.6)
    # Pelo de arriba (con flequillo) y patillas
    top = hy - 4
    ell(cv, 24, hy - 4, hr[0] + 1.5, hr[1] - 2.5, (hair2, hair1, hair1), hi=.9, lo=-.5,
        clip=lambda x, y: y < top + 1 or (y < top + 6 and (x < 15 or x > 32)) or (y == top + 1 and x % 4 != 0))
    if "spiky" in feats:
        for x0 in (12, 18, 24, 30):
            poly_fill(cv, [(x0, top - 6), (x0 + 3, top - 13), (x0 + 6, top - 6)], hair1)
    if "bun" in feats:
        ell(cv, 24, hy - 15, 5.5, 4.5, (hair2, hair1, 'c'))
    if "cap" in feats:
        rect(cv, 11, hy - 14, 36, hy - 8, 'c'); rect(cv, 11, hy - 8, 36, hy - 8, 'd')
        rect(cv, 9, hy - 7, 39, hy - 6, '8')
        rect(cv, 22, hy - 13, 25, hy - 11, '4')
    if "bandana" in feats:
        rect(cv, 11, hy - 8, 36, hy - 6, '2'); px(cv, [(37, hy - 6), (38, hy - 5), (38, hy - 4)], '2')

    # Cara: cejas, ojos con brillo, mejillas y boca
    ey = hy + 2
    for ex in (17, 28):
        rect(cv, ex, ey - 2, ex + 3, ey - 2, hair2 if hair1 not in "cd" else 'e')
        rect(cv, ex + 1, ey, ex + 2, ey + 2, '0')
        cv.set(ex + 1, ey, 'c')
    if "glasses" in feats:
        for ex in (15, 26):
            for x in range(ex, ex + 8):
                cv.set(x, ey - 2, 'f'); cv.set(x, ey + 4, 'f')
            for y in range(ey - 2, ey + 5):
                cv.set(ex, y, 'f'); cv.set(ex + 7, y, 'f')
    px(cv, [(14, ey + 5), (15, ey + 5), (32, ey + 5), (33, ey + 5)], '3')
    my = ey + 7
    if "beard" in feats:
        ell(cv, 24, my + 1, 10.5, 7, ('e', 'd', 'c'), clip=lambda x, y: y >= my - 3)
        px(cv, [(22, my), (23, my), (24, my), (25, my)], 'f')
    else:
        px(cv, [(21, my), (22, my + 1), (23, my + 1), (24, my + 1), (25, my + 1), (26, my)], '1')
    if "mustache" in feats:
        rect(cv, 20, my - 2, 27, my - 2, 'i'); cv.set(19, my - 1, 'i'); cv.set(28, my - 1, 'i')
    return cv


portraits_txt = "# Retratos de los vecinos (48x48) para los diálogos. Generado por make_neighbors.py.\n\n"
for nid, spec in NEIGHBORS.items():
    spec_p = dict(spec)
    if nid == "luca":
        spec_p["features"] = spec["features"] + ["glasses"]
    pcv = portrait(nid, spec_p)
    portraits_txt += block(f"portrait_{nid}", 48, 48, [pcv.rows()], header=["outline 0"])
    # Cabecita de 16x16 para listas (el tablón): la cara del retrato, a la mitad.
    head = Canvas(16, 16)
    for y in range(16):
        for x in range(16):
            head.set(x, y, pcv.get(8 + 2 * x, 2 + 2 * y))
    portraits_txt += block(f"head_{nid}", 16, 16, [head.rows()], header=["outline 0"])
write("Portraits.txt", portraits_txt)

# ================================================================== Muebles y carteles del pueblo

town = "# Pueblo (Fase 7): mostrador, estantes, yunque, barril, camilla, banquito, tablón y carteles. Generado por make_neighbors.py.\n\n"

counter = Canvas(16, 16)
rect(counter, 0, 2, 15, 6, 'o'); rect(counter, 0, 2, 15, 2, 'p')
rect(counter, 0, 7, 15, 15, 'i'); rect(counter, 0, 7, 15, 7, 'j')
for x in (3, 11): rect(counter, x, 9, x, 14, 'j')
town += block("counter", 16, 16, [counter.rows()], extra=["tile solid"])

shelf = Canvas(32, 32)
rect(shelf, 1, 2, 30, 30, 'j'); rect(shelf, 2, 3, 29, 29, 'i')
rnd = random.Random(7)
for sy in (10, 19, 28):
    rect(shelf, 2, sy, 29, sy, 'o')
    x = 4
    while x < 27:
        c = rnd.choice("2349a6c")
        h = rnd.randint(3, 5)
        rect(shelf, x, sy - h, x + 2, sy - 1, c)
        shelf.set(x, sy - h, 'c' if c != 'c' else 'd')
        x += rnd.randint(4, 5)
town += block("shelf", 32, 32, [shelf.rows()], header=["pivot 0.5 0", "outline 0"])

anvil = Canvas(16, 16)
rect(anvil, 2, 6, 13, 8, 'e'); rect(anvil, 2, 6, 13, 6, 'd'); rect(anvil, 0, 6, 2, 7, 'e')
rect(anvil, 5, 9, 10, 11, 'f'); rect(anvil, 3, 12, 12, 14, 'f')
town += block("anvil", 16, 16, [anvil.rows()], header=["pivot 0.5 0", "outline 0"])

barrel = Canvas(16, 16)
ell(barrel, 8, 8.5, 6, 7, ('j', 'i', 'o'))
for y in (4, 12): rect(barrel, 2, y, 13, y, 'f')
town += block("barrel", 16, 16, [barrel.rows()], header=["pivot 0.5 0", "outline 0"])

bed = Canvas(32, 16)
rect(bed, 1, 4, 30, 11, 'c'); rect(bed, 1, 10, 30, 11, 'd'); rect(bed, 2, 4, 8, 8, 'b')
for x in (1, 30): rect(bed, x, 11, x, 15, 'e')
town += block("clinic_bed", 32, 16, [bed.rows()], header=["pivot 0.5 0", "outline 0"])

stool = Canvas(16, 16)
ell(stool, 8, 6, 5, 2.5, ('i', 'o', 'p'))
for x in (5, 10): rect(stool, x, 8, x, 14, 'j')
town += block("stool", 16, 16, [stool.rows()], header=["pivot 0.5 0", "outline 0"])

board = Canvas(32, 32)
for x in (4, 27): rect(board, x, 14, x + 1, 31, 'j')
rect(board, 1, 2, 30, 20, 'j'); rect(board, 2, 3, 29, 19, 'o')
for (x, y, w, h) in [(4, 5, 7, 8), (13, 4, 6, 6), (21, 6, 7, 9), (12, 12, 7, 6)]:
    rect(board, x, y, x + w - 1, y + h - 1, 'c')
    for ly in range(y + 2, y + h - 1, 2): rect(board, x + 1, ly, x + w - 2, ly, 'd')
    board.set(x + w // 2, y, '2')
town += block("board", 32, 32, [board.rows()], header=["pivot 0.5 0", "outline 0"])


def hanging_sign(draw):
    cv = Canvas(16, 16)
    rect(cv, 1, 2, 14, 13, 'o'); rect(cv, 1, 2, 14, 2, 'p'); rect(cv, 1, 13, 14, 13, 'i')
    px(cv, [(4, 0), (4, 1), (11, 0), (11, 1)], 'f')
    draw(cv)
    return cv


def sign_shop(cv):
    rect(cv, 5, 6, 10, 11, '6'); rect(cv, 6, 4, 9, 5, 'j'); px(cv, [(6, 5), (9, 5)], 'o')


def sign_forge(cv):
    rect(cv, 4, 6, 11, 7, 'e'); rect(cv, 6, 8, 9, 9, 'f'); rect(cv, 5, 10, 10, 11, 'f')


def sign_clinic(cv):
    rect(cv, 3, 4, 12, 12, 'c'); rect(cv, 7, 5, 8, 11, '2'); rect(cv, 4, 7, 11, 8, '2')


def sign_tavern(cv):
    rect(cv, 5, 5, 9, 11, '4'); rect(cv, 5, 5, 9, 6, 'c'); rect(cv, 10, 7, 11, 9, '4')


def sign_town(cv):
    poly_fill(cv, [(7, 3), (10, 9), (4, 9)], '4'); poly_fill(cv, [(4, 6), (11, 6), (7, 12)], '4')


def sign_stadium(cv):
    rect(cv, 5, 4, 10, 8, '4'); rect(cv, 7, 9, 8, 10, '3'); rect(cv, 5, 11, 10, 11, '3')
    px(cv, [(4, 5), (11, 5)], '4')


def sign_apice(cv):
    poly_fill(cv, [(7, 3), (12, 12), (3, 12)], 'f'); poly_fill(cv, [(7, 6), (10, 11), (5, 11)], 'd')


for name, fn in [("sign_shop", sign_shop), ("sign_forge", sign_forge), ("sign_clinic", sign_clinic),
                 ("sign_tavern", sign_tavern), ("sign_town", sign_town), ("sign_stadium", sign_stadium),
                 ("sign_apice", sign_apice)]:
    town += block(name, 16, 16, [hanging_sign(fn).rows()], header=["pivot 0.5 0.5", "outline 0"])
write("Town.txt", town)

# ================================================================== Lluvia

rain = Canvas(256, 144)
rnd = random.Random(3)
for _ in range(230):
    x, y = rnd.randrange(256), rnd.randrange(144)
    c = 'd' if rnd.random() < .7 else 'c'
    for i in range(3):
        rain.set((x - i) % 256, (y + i * 2) % 144, c)
        rain.set((x - i) % 256, (y + i * 2 + 1) % 144, c)
write("Weather.txt", "# Lluvia (lámina de 256x144 que se repite al caer). Generado por make_neighbors.py.\n\n"
      + block("rain", 256, 144, [rain.rows()]))
print("Listo: Neighbors.txt, Portraits.txt, Town.txt, Weather.txt")
