"""Arte de la Fase 8B: tiles de las zonas del Acto 3 (Pantano Pantufla, Colina Escarcha, Acantilados Tormenta)
y el derrumbe que tapa el camino al Volcán Dormido. Escribe Zones.txt en Art/Source.
Uso: python make_zones.py"""
import random
from pixel_lib import *


def write(name, text):
    open(os.path.join(OUT, name), "w", encoding="utf-8").write(text)


def rect(cv, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            cv.set(x, y, c)


def speckle(base, specks, seed, n=22):
    rnd = random.Random(seed)
    cv = Canvas(16, 16, base)
    for _ in range(n):
        x, y = rnd.randrange(16), rnd.randrange(16)
        cv.set(x, y, rnd.choice(specks))
    return cv


def blades(cv, color_lo, color_hi, seed):
    rnd = random.Random(seed)
    for bx in (2, 6, 10, 13):
        h = rnd.randint(5, 8)
        for i in range(h):
            cv.set(bx + (i % 2 if i > h // 2 else 0), 14 - i, color_lo if i < h - 2 else color_hi)
    return cv


out = "# Zonas del Acto 3 (Fase 8B): barro, juncos, nieve, pasto nevado, hielo, roca, pasto de altura y derrumbe. Generado por make_zones.py.\n\n"

mud = speckle('i', 'jo', 1)
for (x, y) in [(3, 5), (10, 11), (12, 3)]:
    for dx in range(3):
        mud.set(x + dx, y, 'j')
out += block("mud", 16, 16, [mud.rows()], extra=["tile"])

reeds = speckle('i', 'jo', 2)
out += block("mud_reeds", 16, 16, [blades(reeds, '7', '6', 3).rows()], extra=["tile"])

snow = speckle('c', 'dd', 4, 14)
out += block("snow", 16, 16, [snow.rows()], extra=["tile"])

snow_grass = speckle('c', 'd', 5, 10)
out += block("snow_grass", 16, 16, [blades(snow_grass, 'd', 'e', 6).rows()], extra=["tile"])

ice = Canvas(16, 16, 'b')
for i in range(16):
    ice.set(i, (i * 5 + 3) % 16, 'c')
    ice.set((i * 7 + 1) % 16, i, 'a')
out += block("ice", 16, 16, [ice.rows()], extra=["tile"])

cliff = speckle('e', 'fd', 7, 26)
for (x, y) in [(2, 3), (9, 8), (5, 13)]:
    for dx in range(4):
        cliff.set(x + dx, y, 'f')
out += block("cliff", 16, 16, [cliff.rows()], extra=["tile"])

cliff_grass = speckle('e', 'fd', 8, 18)
out += block("cliff_grass", 16, 16, [blades(cliff_grass, '7', '5', 9).rows()], extra=["tile"])

cliff_wall = Canvas(16, 16, 'f')
for y in range(16):
    for x in range(16):
        if (x * 3 + y * 5) % 7 == 0: cliff_wall.set(x, y, 'e')
        if y in (5, 11) and x % 4 != 0: cliff_wall.set(x, y, '0')
out += block("cliff_wall", 16, 16, [cliff_wall.rows()], extra=["tile solid"])

# Derrumbe (32x24): rocas apiladas que tapan el camino.
rubble = Canvas(32, 24)
for (cx, cy, rx, ry) in [(8, 17, 7, 6), (22, 16, 8, 7), (15, 10, 7, 6), (26, 8, 5, 4), (6, 8, 5, 4)]:
    shade_ellipse(rubble, cx, cy, rx, ry, ('f', 'e', 'd'))
for (x, y) in [(12, 18), (20, 12), (9, 9)]:
    rubble.set(x, y, '0'); rubble.set(x + 1, y, '0')
out += block("rubble", 32, 24, [rubble.rows()], header=["pivot 0.5 0", "outline 0"])

# Collar roto (objeto de la historia): dos medialunas violetas con el triángulo de Ápice.
collar = Canvas(16, 16)
for (cx, x0, x1) in [(6, 1, 7), (10, 9, 15)]:
    for y in range(16):
        for x in range(x0, x1):
            d = ((x + .5 - 8) ** 2 + (y + .5 - 8) ** 2) ** .5
            if 4.2 <= d <= 6.2 and abs(x + .5 - 8) > 0.8:
                collar.set(x, y, 'k' if d > 5.2 else 'l')
for (x, y) in [(8, 1), (7, 2), (8, 2), (9, 2)]:
    collar.set(x, y, 'd')
out += block("icon_collar_roto", 16, 16, [collar.rows()], header=["outline 0"])

# Cierre (Fase 8C): el Director Sílex de espaldas y una figura en las sombras con la bufanda roja del abuelo.
silex = Canvas(24, 40)
shade_ellipse(silex, 12, 7, 5, 5.5, ('d', 'c', 'c'))            # pelo gris peinado hacia atrás
rect(silex, 9, 12, 14, 13, 'h')                                  # nuca
poly_fill(silex, [(3, 39), (5, 16), (9, 13), (15, 13), (19, 16), (21, 39)], 'f')  # saco largo
poly_fill(silex, [(5, 39), (7, 18), (11, 15), (13, 15), (17, 18), (19, 39)], '0')
rect(silex, 11, 15, 12, 38, 'f')
for (x, y) in [(6, 22), (17, 22)]:
    silex.set(x, y, 'e')
out += block("silex", 24, 40, [silex.rows()], header=["pivot 0.5 0", "outline 0"])

figure = Canvas(20, 36)
shade_ellipse(figure, 10, 7, 5, 5.5, ('0', 'f', 'f'))
poly_fill(figure, [(2, 35), (4, 14), (8, 12), (12, 12), (16, 14), (18, 35)], '0')
rect(figure, 5, 12, 15, 14, '2')                                 # la bufanda roja
rect(figure, 6, 12, 8, 13, '3')
rect(figure, 13, 15, 15, 22, '2')                                # la punta que cuelga
out += block("scarf_figure", 20, 36, [figure.rows()], header=["pivot 0.5 0", "outline 0"])


write("Zones.txt", out)
print("Listo: Zones.txt")
