"""Arte de la Fase 6 (vida en la isla): tierra arada, cultivos, íconos de objetos, máquinas del refugio,
puestos del pueblo y la cueva. Escribe Farm.txt, Icons.txt, Machines.txt y Cave.txt en Art/Source.
Uso: python make_island_life.py"""
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


# ================================================================== GRANJA
farm = "# Granja: tierra arada (seca/regada) y cultivos en 3 etapas (brote, creciendo, listo).\n\n"


def soil(wet):
    cv = Canvas(16, 16, 'j' if wet else 'i')
    for y in range(1, 16, 4):
        for x in range(16):
            cv.set(x, y, 'f' if wet else 'j')
            cv.set(x, y + 1, 'i' if wet else 'o')
    rnd = random.Random(4 if wet else 3)
    for _ in range(6):
        cv.set(rnd.randrange(16), rnd.randrange(16), '0' if wet else 'p')
    return cv


farm += block("soil_dry", 16, 16, [soil(False).rows()], extra=["tile"])
farm += block("soil_wet", 16, 16, [soil(True).rows()], extra=["tile"])


def sprout():
    cv = Canvas(16, 16)
    line_pts = [(8, 13), (8, 12), (8, 11)]
    px(cv, line_pts, '6')
    px(cv, [(6, 10), (7, 10), (6, 9), (9, 10), (10, 10), (10, 9)], '5')
    return cv


def leaves(cv, cx, top, color=('m', '6', '5')):
    for (dx, h) in [(-3, 5), (0, 7), (3, 5)]:
        ell(cv, cx + dx, top + 7 - h / 2, 1.6, h / 2, color)


def crop(kind, stage):
    if stage == 0:
        return sprout()
    cv = Canvas(16, 16)
    if kind in ("nabo", "zanahoria"):
        leaves(cv, 8, 2 if stage == 2 else 5)
        if stage == 2:
            top = ('c', 'c', 'c') if kind == "nabo" else ('2', '3', '4')
            ell(cv, 8, 13, 3.5, 2.5, top)
            if kind == "nabo":
                px(cv, [(6, 12), (7, 12), (8, 11)], '1')
    elif kind in ("frutilla", "bayamemo"):
        ell(cv, 8, 10, 6 if stage == 2 else 4, 4.5 if stage == 2 else 3.5, ('m', '6', '5'))
        if stage == 2:
            berry = ('2', '2', '3') if kind == "frutilla" else ('8', '9', 'a')
            for (x, y) in [(5, 8), (10, 9), (7, 12), (11, 12), (4, 11)]:
                ell(cv, x + .5, y + .5, 1.3, 1.3, berry)
                if kind == "bayamemo":
                    cv.set(x, y - 1, 'c')
    else:  # zapallo
        for x in range(2, 15):
            cv.set(x, 13 + (x % 3 == 0), '6')
        leaves(cv, 5, 6)
        if stage == 2:
            ell(cv, 10, 11, 4.5, 3.5, ('n', '3', '4'))
            px(cv, [(10, 7), (10, 8)], 'm')
            px(cv, [(8, 9), (8, 10), (8, 11), (8, 12)], 'n')
    return cv


CROPS = ["nabo", "zanahoria", "frutilla", "zapallo", "bayamemo"]
for kind in CROPS:
    for stage in range(3):
        farm += block(f"crop_{kind}_{stage}", 16, 16, [crop(kind, stage).rows()], header=["pivot 0.5 0", "outline 0"])
write("Farm.txt", farm)

# ================================================================== ÍCONOS
icons = "# Íconos de objetos (16x16, pivote al centro).\n\n"


def icon(name, cv):
    global icons
    icons += block(f"icon_{name}", 16, 16, [cv.rows()], header=["outline 0"])


def rock(c1=('e', 'd', 'c'), specks=None):
    cv = Canvas(16, 16)
    ell(cv, 8, 9, 6, 4.5, c1)
    if specks:
        for (x, y) in [(6, 8), (9, 7), (10, 10), (5, 10)]:
            cv.set(x, y, specks[0]); cv.set(x + 1, y, specks[1])
    return cv


def crystal(c1, c2, c3):
    cv = Canvas(16, 16)
    poly_fill(cv, [(8, 2), (12, 7), (8, 14), (4, 7)], c2)
    poly_fill(cv, [(8, 2), (12, 7), (8, 8)], c3)
    poly_fill(cv, [(4, 7), (8, 14), (8, 8)], c1)
    return cv


def ingot(c1, c2, c3):
    cv = Canvas(16, 16)
    poly_fill(cv, [(3, 11), (5, 6), (13, 6), (14, 11)], c2)
    rect(cv, 5, 6, 12, 7, c3)
    rect(cv, 3, 11, 14, 12, c1)
    return cv


def bag(dot):
    cv = Canvas(16, 16)
    ell(cv, 8, 10, 5, 4.5, ('i', 'o', 'p'))
    rect(cv, 6, 3, 10, 5, 'o')
    px(cv, [(6, 5), (10, 5)], 'j')
    ell(cv, 8, 10, 2, 2, (dot, dot, dot))
    return cv


def bowl(fill):
    cv = Canvas(16, 16)
    ell(cv, 8, 10, 6, 3.5, ('e', 'd', 'c'))
    ell(cv, 8, 8.5, 5, 2, fill)
    return cv


for kind, dot in zip(CROPS, ['c', '3', '2', 'n', '9']):
    icon(f"semilla_{kind}", bag(dot))
for kind in CROPS:
    cv = crop(kind, 2)
    icon(kind, cv)

icon("comida_memo", bowl(('i', 'o', '4')))
icon("pure_zapallo", bowl(('n', '3', '4')))
cake = Canvas(16, 16)
rect(cake, 3, 8, 12, 12, 'p'); rect(cake, 3, 7, 12, 7, 'c'); rect(cake, 3, 12, 12, 12, 'o')
for x in (4, 7, 10): ell(cake, x + 1, 6, 1.3, 1.3, ('2', '2', '3'))
icon("pastel_frutilla", cake)
jar = Canvas(16, 16)
rect(jar, 5, 6, 11, 13, '2'); rect(jar, 5, 4, 11, 5, 'c'); px(jar, [(6, 7), (6, 8)], '3')
icon("mermelada", jar)

icon("piedra", rock())
icon("mineral_cobre", rock(specks=('n', '3')))
icon("mineral_hierro", rock(('f', 'e', 'd'), specks=('c', 'd')))
icon("cuarzo", crystal('d', 'c', 'c'))
icon("amatista", crystal('1', 'e', 'd'))
icon("topacio", crystal('o', '4', 'p'))
icon("esmeralda", crystal('m', '6', '5'))
icon("lingote_cobre", ingot('2', 'n', '3'))
icon("lingote_hierro", ingot('f', 'e', 'd'))

flower = Canvas(16, 16)
px(flower, [(8, 9), (8, 10), (8, 11), (8, 12), (8, 13)], '6')
for (x, y) in [(8, 4), (5, 6), (11, 6), (6, 9), (10, 9)]:
    ell(flower, x + .5, y + .5, 2, 2, ('2', '2', '3'))
ell(flower, 8.5, 7, 1.5, 1.5, ('4', '4', 'p'))
icon("flor", flower)
feather = Canvas(16, 16)
for i in range(10):
    ell(feather, 4 + i, 12 - i, 1.6, 1.6, ('d', 'c', 'c'))
px(feather, [(3 + i, 13 - i) for i in range(11)], 'a')
icon("pluma", feather)
alga = Canvas(16, 16)
for (x0, h) in [(5, 9), (8, 11), (11, 8)]:
    for i in range(h):
        alga.set(x0 + (i // 3) % 2, 14 - i, '7' if i % 2 else '6')
icon("alga", alga)
shell = Canvas(16, 16)
ell(shell, 8, 9, 6, 5, ('h', 'g', 'p'))
for x in (5, 8, 11): px(shell, [(x, 6), (x, 8), (x, 10), (x, 12)], 'h')
icon("concha", shell)
herb = Canvas(16, 16)
for (dx, h) in [(-3, 6), (0, 9), (3, 6)]:
    ell(herb, 8 + dx, 13 - h / 2, 1.8, h / 2, ('m', '6', '5'))
icon("hierba", herb)
berry = Canvas(16, 16)
for (x, y) in [(6, 7), (10, 7), (8, 10), (5, 11), (11, 11)]:
    ell(berry, x, y, 2, 2, ('1', '2', '3'))
px(berry, [(8, 3), (8, 4), (9, 3)], '6')
icon("fruto_silvestre", berry)
cloth = Canvas(16, 16)
poly_fill(cloth, [(3, 4), (13, 3), (12, 13), (2, 12)], '9')
for y in range(5, 12, 3): px(cloth, [(x, y) for x in range(4, 12)], 'a')
icon("tela", cloth)
leather = Canvas(16, 16)
poly_fill(leather, [(4, 3), (12, 4), (13, 12), (8, 14), (3, 12)], 'i')
px(leather, [(6, 6), (9, 8), (7, 10)], 'j')
icon("cuero", leather)
ball = Canvas(16, 16)
ell(ball, 8, 8, 5.5, 5.5, ('2', '2', '3'))
px(ball, [(x, 8) for x in range(3, 14)], 'c')
icon("pelota", ball)

# Equipo y amuletos (antes no tenían ícono)
shoe = Canvas(16, 16)
for i in range(9):
    a = 3.1416 * i / 8
    ell(shoe, 8 + 5 * math.cos(a), 6 + 5 * math.sin(a), 1.4, 1.4, ('f', 'e', 'd'))
icon("herraduras", shoe)
fin = Canvas(16, 16)
poly_fill(fin, [(3, 12), (13, 3), (11, 13)], '9'); px(fin, [(5, 11), (8, 8), (11, 5)], 'a')
icon("aletas", fin)
boot = Canvas(16, 16)
rect(boot, 5, 3, 9, 10, 'j'); rect(boot, 5, 10, 13, 12, 'j'); px(boot, [(6, 13), (9, 13), (12, 13)], 'd')
icon("botas_clavos", boot)
wing = Canvas(16, 16)
for i, (x, y) in enumerate([(4, 10), (6, 7), (9, 5), (12, 4)]):
    ell(wing, x, y, 3, 2, ('d', 'c', 'c'))
icon("alas_planeo", wing)
weight = Canvas(16, 16)
rect(weight, 3, 7, 13, 9, 'e'); ell(weight, 3.5, 8.5, 2, 3.5, ('0', 'f', 'e')); ell(weight, 12.5, 8.5, 2, 3.5, ('0', 'f', 'e'))
icon("pesas", weight)
pack = Canvas(16, 16)
ell(pack, 8, 9, 5, 5.5, ('8', '9', 'a')); rect(pack, 6, 3, 10, 4, 'j'); ell(pack, 8, 10, 2, 1.5, ('a', 'b', 'c'))
icon("mochila_agua", pack)


def amulet(c):
    cv = Canvas(16, 16)
    px(cv, [(x, 3) for x in range(5, 12)], 'i')
    ell(cv, 8, 9, 4.5, 4.5, c)
    cv.set(7, 7, 'c')
    return cv


icon("amuleto_relevo", amulet(('2', '3', '4')))
icon("piedra_carga", amulet(('o', '4', 'p')))
bell = Canvas(16, 16)
poly_fill(bell, [(8, 3), (12, 11), (4, 11)], '4'); rect(bell, 3, 11, 13, 12, 'o'); ell(bell, 8, 13.5, 1.5, 1.5, ('j', 'j', 'j'))
icon("cascabel_calma", bell)
bow = Canvas(16, 16)
poly_fill(bow, [(8, 8), (2, 4), (2, 12)], '2'); poly_fill(bow, [(8, 8), (14, 4), (14, 12)], '2'); ell(bow, 8, 8, 1.6, 1.6, ('n', 'n', '3'))
icon("mono_amistad", bow)
icon("gema", crystal('8', 'a', 'b'))
write("Icons.txt", icons)

# ================================================================== MÁQUINAS Y PUESTOS
machines = "# Máquinas del refugio y puestos del pueblo. Pivote abajo al centro.\n\n"

smelter = Canvas(32, 32)
for y in range(6, 31):
    for x in range(5, 27):
        smelter.set(x, y, 'e' if (x + y // 4) % 5 else 'f')
rect(smelter, 3, 4, 28, 7, 'f')
rect(smelter, 11, 15, 20, 26, '0')
ell(smelter, 15.5, 24, 4, 2.5, ('2', '3', '4'))
rect(smelter, 13, 1, 18, 4, 'f')
machines += block("smelter", 32, 32, [smelter.rows()], header=["pivot 0.5 0", "outline 0"])

bench = Canvas(32, 24)
rect(bench, 2, 6, 29, 11, 'o'); rect(bench, 2, 6, 29, 6, 'p'); rect(bench, 2, 11, 29, 11, 'i')
for x in (4, 5, 26, 27): rect(bench, x, 12, x, 22, 'j')
rect(bench, 7, 3, 9, 5, 'e'); rect(bench, 13, 4, 19, 5, 'i'); px(bench, [(22, 2), (23, 3), (24, 4)], 'd')
machines += block("workbench", 32, 24, [bench.rows()], header=["pivot 0.5 0", "outline 0"])

proc = Canvas(16, 24)
rect(proc, 2, 4, 13, 22, 'e'); rect(proc, 4, 6, 11, 12, 'f'); ell(proc, 7.5, 9, 2.5, 2.5, ('o', '4', 'p'))
rect(proc, 4, 15, 11, 17, '2'); px(proc, [(5, 19), (7, 19), (9, 19)], 'b')
machines += block("processor", 16, 24, [proc.rows()], header=["pivot 0.5 0", "outline 0"])

box = Canvas(16, 16)
rect(box, 1, 4, 14, 14, 'o'); rect(box, 1, 4, 14, 5, 'i'); rect(box, 1, 9, 14, 9, 'i')
px(box, [(7, 7), (8, 7)], '4')
machines += block("shipping_box", 16, 16, [box.rows()], header=["pivot 0.5 0", "outline 0"])


def stall(c1, c2):
    cv = Canvas(32, 32)
    for x in range(1, 31):
        for y in range(4, 11):
            cv.set(x, y, c1 if (x // 4) % 2 == 0 else c2)
    for x in range(1, 31, 4):
        cv.set(x + 1, 11, c1); cv.set(x + 2, 11, c1)
    for x in (3, 28): rect(cv, x, 11, x, 30, 'j')
    rect(cv, 2, 20, 29, 23, 'o'); rect(cv, 2, 20, 29, 20, 'p')
    for (x, c) in [(6, '2'), (10, '3'), (14, '6'), (19, '9'), (23, '4')]:
        ell(cv, x, 18.5, 1.8, 1.6, (c, c, 'c'))
    return cv


machines += block("stall_deny", 32, 32, [stall('2', 'c').rows()], header=["pivot 0.5 0", "outline 0"])
machines += block("stall_fer", 32, 32, [stall('f', 'e').rows()], header=["pivot 0.5 0", "outline 0"])
write("Machines.txt", machines)

# ================================================================== CUEVA
cave = "# Cueva de Zorak: piso, paredes, piso áspero (encuentros), rocas, escaleras y entrada.\n\n"
cfloor = Canvas(16, 16, 'f')
rnd = random.Random(21)
for _ in range(14):
    x, y = rnd.randrange(16), rnd.randrange(16)
    cfloor.set(x, y, 'e' if rnd.random() < .6 else '0')
cave += block("cave_floor", 16, 16, [cfloor.rows()], extra=["tile"])
rough = Canvas(16, 16)
rough.g = [r[:] for r in cfloor.g]
for (x, y) in [(2, 3), (9, 2), (5, 9), (12, 11), (3, 13), (11, 6)]:
    px(rough, [(x, y), (x + 1, y), (x, y + 1)], 'e')
    rough.set(x + 1, y - 1, 'd')
cave += block("cave_rough", 16, 16, [rough.rows()], extra=["tile"])
cwall = Canvas(16, 16, '0')
for y in range(16):
    for x in range(16):
        if (x * 7 + y * 3) % 11 == 0: cwall.set(x, y, 'f')
for x in range(16): cwall.set(x, 15, 'f')
cave += block("cave_wall", 16, 16, [cwall.rows()], extra=["tile solid"])


def rock_prop(specks):
    cv = Canvas(16, 16)
    ell(cv, 8, 10, 6.5, 5, ('f', 'e', 'd'))
    for i, (x, y) in enumerate([(5, 8), (9, 7), (10, 11), (6, 11)]):
        if specks:
            cv.set(x, y, specks[i % len(specks)]); cv.set(x + 1, y, specks[(i + 1) % len(specks)])
    return cv


for name, specks in [("rock_stone", None), ("rock_copper", 'n3'), ("rock_iron", 'cd'),
                     ("rock_quartz", 'cb'), ("rock_gem", 'a46')]:
    cave += block(name, 16, 16, [rock_prop(specks).rows()], header=["pivot 0.5 0", "outline 0"])

ladder_down = Canvas(16, 16)
ell(ladder_down, 8, 8, 6.5, 5.5, ('0', '0', 'f'))
for y in (5, 8, 11): px(ladder_down, [(x, y) for x in range(5, 12)], 'i')
for x in (5, 11): px(ladder_down, [(x, y) for y in range(3, 14)], 'j')
cave += block("ladder_down", 16, 16, [ladder_down.rows()], extra=["tile"])
ladder_up = Canvas(16, 16, 'f')
for y in (2, 6, 10, 14): px(ladder_up, [(x, y) for x in range(4, 12)], 'o')
for x in (4, 11): px(ladder_up, [(x, y) for y in range(0, 16)], 'i')
cave += block("ladder_up", 16, 16, [ladder_up.rows()], extra=["tile"])

entrance = Canvas(32, 32)
ell(entrance, 16, 20, 15, 13, ('f', 'e', 'd'))
ell(entrance, 16, 24, 8, 8, ('0', '0', '0'))
for (x, y) in [(5, 12), (24, 10), (9, 6), (21, 16)]:
    px(entrance, [(x, y), (x + 1, y)], 'c')
rect(entrance, 8, 30, 24, 31, 'f')
cave += block("cave_entrance", 32, 32, [entrance.rows()], header=["pivot 0.5 0", "outline 0"])
write("Cave.txt", cave)
print("ok")
