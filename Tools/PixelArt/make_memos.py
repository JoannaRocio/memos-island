"""Compone los 20 Memos (vista lateral de 64x64 mirando a la derecha, 2 cuadros de carrera) y escribe
Art/Source/Memos.txt. Cada Memo tiene variante brillante (paleta propia) y con collar (desaturado).
Los tiernos son redondos con ojos grandes; Karman, Draken y Randy son angulosos y oscuros.
Uso: python make_memos.py"""
import math
from pixel_lib import *

GROUND = 61

# Rampas (sombra, base, luz)
FIRE = ('2', '3', '4'); CREAM = ('4', 'p', 'p')
PLANT = ('m', '6', '5'); LEAF_LIGHT = ('6', '5', 'p')
WATER = ('8', '9', 'a'); FOAM = ('a', 'b', 'c')
EARTH = ('j', 'i', 'o'); SAND = ('i', 'o', 'p')
ELEC = ('o', '4', 'p')
SNOW = ('d', 'c', 'c')
SHADOW = ('1', 'e', 'd')
METAL = ('f', 'e', 'd')
NAVY = ('0', '8', 'e')
DARK = ('0', 'f', 'e')

# Con collar: todo pierde el color (el collar violeta se dibuja aparte, en la Fase 3/8).
COLLAR = "variant concollar " + " ".join(f"{a}>{b}" for a, b in [
    ('2', 'f'), ('3', 'e'), ('4', 'd'), ('5', 'd'), ('6', 'e'), ('7', 'f'), ('9', 'e'), ('a', 'd'), ('b', 'd'),
    ('n', 'f'), ('o', 'd'), ('p', 'd'), ('h', 'd'), ('g', 'c'), ('i', 'e'), ('j', 'f'), ('m', 'f'), ('1', 'f'),
    ('8', 'f')])


def shiny(*pairs):
    return "variant brillante " + " ".join(pairs)


def ell(cv, cx, cy, rx, ry, ramp, line=None, clip=None, **kw):
    return shade_ellipse(cv, cx, cy, rx, ry, ramp, line=line, clip=clip, **kw)


def px(cv, pts, c, dy=0):
    for (x, y) in pts:
        cv.set(x, y + dy, c)


def line(cv, x0, y0, x1, y1, c):
    n = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(n + 1):
        cv.set(round(x0 + (x1 - x0) * i / n), round(y0 + (y1 - y0) * i / n), c)


def eye(cv, cx, cy, rx=3.5, ry=5, iris='8', dy=0):
    """Ojo tierno: grande, negro, con dos brillos."""
    for y in range(int(cy - ry) - 1, int(cy + ry) + 2):
        for x in range(int(cx - rx) - 1, int(cx + rx) + 2):
            if ((x + .5 - cx) / rx) ** 2 + ((y + .5 - cy) / ry) ** 2 <= 1:
                cv.set(x, y + dy, '0')
    top = int(cy - ry) + 1
    hx = int(cx + rx * 0.2)
    px(cv, [(hx, top), (hx + 1, top), (hx, top + 1), (hx + 1, top + 1)], 'c', dy)
    cv.set(int(cx - rx * 0.4), int(cy + ry * 0.45) + dy, 'c')
    if iris:
        px(cv, [(int(cx - rx + 1), int(cy)), (int(cx - rx + 1), int(cy) + 1)], iris, dy)


def eye_cool(cv, x, y, glow, dy=0):
    """Ojo afilado (legendarios): almendra inclinada con brillo de color."""
    px(cv, [(x + 1, y), (x + 2, y), (x + 3, y), (x + 4, y), (x + 5, y + 1)], '0', dy)
    px(cv, [(x, y + 1), (x + 6, y + 2)], '0', dy)
    px(cv, [(x + 1, y + 1), (x + 2, y + 1), (x + 3, y + 1), (x + 4, y + 1),
            (x + 1, y + 2), (x + 2, y + 2), (x + 5, y + 2)], glow, dy)
    px(cv, [(x + 3, y + 2), (x + 4, y + 2)], '0', dy)
    px(cv, [(x + 1, y + 3), (x + 2, y + 3), (x + 3, y + 3), (x + 4, y + 3), (x + 5, y + 3)], '0', dy)
    cv.set(x + 1, y + 1 + dy, 'c')


def legs(cv, phase, back_x, front_x, near, far, rx=3.8, ry=6.5, foot=None, line_c='j'):
    """Patas de cuadrúpedo: las lejanas más oscuras; en el cuadro 1 se abren al correr."""
    cy = GROUND - ry + 0.5
    if phase == 0:
        fars, nears = (back_x + 3, front_x + 3), (back_x, front_x)
    else:
        fars, nears = (back_x + 6, front_x - 3), (back_x - 4, front_x + 4)
    for x in fars:
        ell(cv, x, cy - 1, rx - 0.3, ry, far)
    for x in nears:
        ell(cv, x, cy, rx, ry, near, line=line_c)
        if foot:
            for dx in range(-int(rx) + 1, int(rx)):
                cv.set(int(x) + dx, GROUND, foot)


def tongue(x0, y0, r0, x1, y1, r1, wob, n=10, sway=0):
    pts = []
    for t in range(n):
        u = t / (n - 1)
        pts.append((x0 + (x1 - x0) * u + math.sin(u * 3.2 + sway) * wob,
                    y0 + (y1 - y0) * u, r0 + (r1 - r0) * u ** 1.3))
    return pts


def leaf(cv, base, tip, width, ramp=PLANT):
    """Hoja: lente entre base y punta, con nervadura."""
    (bx, by), (tx, ty) = base, tip
    length = math.hypot(tx - bx, ty - by)
    for y in range(64):
        for x in range(64):
            u = ((x + .5 - bx) * (tx - bx) + (y + .5 - by) * (ty - by)) / (length ** 2)
            if not 0 <= u <= 1:
                continue
            d = abs((x + .5 - bx) * (ty - by) - (y + .5 - by) * (tx - bx)) / length
            if d <= width * math.sin(math.pi * u):
                side = (x + .5 - bx) * (ty - by) - (y + .5 - by) * (tx - bx)
                cv.set(x, y, ramp[2] if side < 0 else ramp[1])
    line(cv, bx, by, round(bx + (tx - bx) * .8), round(by + (ty - by) * .8), ramp[0])


# ================================================================== INICIALES

def tostin(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    s = 1 if phase else 0
    flame(cv, tongue(17, 41 + b, 6.5, 4 - s, 26 + b, 2.2, 1.5, sway=s)
              + tongue(14, 34 + b, 4.5, 9 + s, 15 + b, 1.3, 2.0, sway=s)
              + tongue(18, 36 + b, 3.5, 18, 24 + b, 1.0, 1.0, 6, sway=s))
    legs(cv, phase, 22, 38, FIRE, ('1', '2', '2'), foot='2')
    ell(cv, 29, 44 + b, 14, 10.5, FIRE, line='j')
    ell(cv, 33, 49 + b, 9, 5.5, CREAM, clip=ellipse_mask(29, 44 + b, 13, 9.5))
    poly_fill(cv, [(33, 18 + b), (34, 4 + b), (44, 14 + b)], '3')
    poly_fill(cv, [(35, 15 + b), (36, 8 + b), (41, 14 + b)], '2')
    poly_fill(cv, [(45, 13 + b), (52, 3 + b), (55, 16 + b)], '3')
    poly_fill(cv, [(47, 13 + b), (51, 7 + b), (53, 14 + b)], '2')
    ell(cv, 44, 27 + b, 14, 13, FIRE, line='j')
    flame(cv, lerp_chain((44, 15 + b, 3.2), (42, 7 + b, 1.2), 5))
    ell(cv, 53, 33 + b, 7, 5.5, CREAM)
    px(cv, [(58, 30), (59, 30), (58, 31), (59, 31)], '0', b)
    eye(cv, 48.5, 25, 3.5, 5, dy=b)
    px(cv, [(52, 37), (53, 38), (54, 38), (55, 38), (56, 37)], 'j', b)
    px(cv, [(54, 39), (55, 39)], '2', b)
    px(cv, [(40, 32), (41, 32), (42, 32), (41, 33)], 'n', b)
    return cv


def brason(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    s = 1 if phase else 0
    flame(cv, tongue(15, 42 + b, 8, 3 - s, 22 + b, 2.5, 1.8, sway=s)
              + tongue(12, 34 + b, 5.5, 7 + s, 9 + b, 1.4, 2.2, sway=s)
              + tongue(17, 37 + b, 4, 17, 20 + b, 1.2, 1.0, 6, sway=s))
    legs(cv, phase, 20, 39, FIRE, ('1', '2', '2'), rx=4.6, ry=7, foot='j')
    ell(cv, 28, 42 + b, 16, 11.5, FIRE, line='j')
    ell(cv, 32, 48 + b, 10, 6, CREAM, clip=ellipse_mask(28, 42 + b, 15, 10.5))
    # placas de roca en el lomo
    for (x0, x1, top) in [(15, 23, 26), (22, 31, 23), (30, 38, 26)]:
        poly_fill(cv, [(x0, 35 + b), ((x0 + x1) // 2, top + b), (x1, 35 + b)], 'e')
        line(cv, x0 + 1, 34 + b, (x0 + x1) // 2, top + 1 + b, 'd')
        line(cv, (x0 + x1) // 2 + 1, top + 2 + b, x1 - 1, 34 + b, 'f')
    poly_fill(cv, [(37, 17 + b), (37, 2 + b), (46, 13 + b)], '3')
    poly_fill(cv, [(39, 14 + b), (39, 6 + b), (43, 13 + b)], '2')
    poly_fill(cv, [(48, 12 + b), (56, 1 + b), (58, 15 + b)], '3')
    poly_fill(cv, [(50, 12 + b), (55, 5 + b), (56, 13 + b)], '2')
    ell(cv, 47, 25 + b, 13.5, 12.5, FIRE, line='j')
    flame(cv, lerp_chain((47, 13 + b, 4), (44, 3 + b, 1.4), 6))
    ell(cv, 56, 31 + b, 7, 5.5, CREAM)
    px(cv, [(61, 28), (62, 28), (61, 29), (62, 29)], '0', b)
    eye(cv, 52, 24, 3, 4.2, dy=b)
    line(cv, 48, 18 + b, 55, 20 + b, 'j')
    px(cv, [(55, 35), (56, 36), (57, 36), (58, 36), (59, 35)], 'j', b)
    px(cv, [(57, 37)], 'c', b)
    return cv


def brotito(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    leaf(cv, (17, 46 + b), (5, 37 + b - phase), 3.2)
    legs(cv, phase, 23, 36, PLANT, ('m', 'm', '6'), rx=3.5, ry=5, foot='m', line_c='m')
    ell(cv, 28, 46 + b, 13, 9, PLANT, line='m')
    ell(cv, 32, 50 + b, 8, 4.5, ('5', 'p', 'p'), clip=ellipse_mask(28, 46 + b, 12, 8))
    ell(cv, 43, 31 + b, 14, 13, PLANT, line='m')
    ell(cv, 51, 36 + b, 6.5, 5, ('6', '5', '5'))
    # brote en la cabeza: tallo y dos hojitas (se mueven al correr)
    line(cv, 42, 18 + b, 42, 12 + b, 'm')
    leaf(cv, (42, 13 + b), (33, 8 + b + phase), 2.6, LEAF_LIGHT)
    leaf(cv, (42, 12 + b), (51, 6 + b - phase), 2.6, LEAF_LIGHT)
    eye(cv, 48, 30, 3.5, 5, iris='m', dy=b)
    px(cv, [(52, 38), (53, 39), (54, 39), (55, 38)], 'm', b)
    px(cv, [(44, 36), (45, 36), (46, 36)], '3', b)
    return cv


def ramazon(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    leaf(cv, (14, 40 + b), (3, 33 + b - phase), 3.5)
    legs(cv, phase, 20, 39, EARTH, ('j', 'j', 'i'), rx=4.6, ry=7, foot='j')
    ell(cv, 28, 42 + b, 16, 11.5, EARTH, line='j')
    ell(cv, 32, 48 + b, 10, 5.5, ('i', 'o', 'p'), clip=ellipse_mask(28, 42 + b, 15, 10.5))
    # manto de musgo sobre el lomo
    ell(cv, 27, 33 + b, 14, 6.5, PLANT, clip=ellipse_mask(28, 42 + b, 16.5, 12))
    for x in range(15, 40, 4):
        cv.set(x, 38 + b, '6'); cv.set(x + 1, 39 + b, 'm')
    # astas de ramas con hojas y una flor
    for (x0, y0, x1, y1) in [(44, 15, 37, 4), (41, 10, 34, 8), (50, 15, 56, 4), (53, 9, 60, 8)]:
        line(cv, x0, y0 + b, x1, y1 + b, 'j')
    for (x, y) in [(36, 4), (33, 8), (57, 4), (60, 9)]:
        ell(cv, x, y + b, 2.6, 2.2, LEAF_LIGHT)
    px(cv, [(56, 3), (57, 2), (58, 3), (57, 4)], '2', b)
    cv.set(57, 3 + b, '4')
    ell(cv, 47, 26 + b, 12.5, 11.5, PLANT, line='m')
    ell(cv, 55, 31 + b, 6.5, 5, ('6', '5', '5'))
    eye(cv, 51, 25, 3, 4.2, iris='m', dy=b)
    px(cv, [(55, 35), (56, 36), (57, 36), (58, 35)], 'm', b)
    return cv


def charquito(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    f = 1 if phase else 0
    # cola de pez
    poly_fill(cv, [(17, 45 + b), (7, 34 + b - f), (3, 37 + b), (9, 46 + b), (3, 54 + b), (8, 56 + b + f), (17, 49 + b)], '9')
    line(cv, 15, 45 + b, 7, 36 + b - f, 'a')
    line(cv, 15, 49 + b, 7, 54 + b, '8')
    legs(cv, phase, 23, 37, WATER, ('8', '8', '9'), rx=4.2, ry=4.2, line_c='8')
    ell(cv, 28, 46 + b, 14, 9.5, WATER, line='8')
    ell(cv, 32, 51 + b, 9, 5, FOAM, clip=ellipse_mask(28, 46 + b, 13, 8.5))
    poly_fill(cv, [(37, 22 + b), (42, 11 + b), (48, 21 + b)], 'a')
    line(cv, 41, 13 + b, 42, 12 + b, 'b')
    ell(cv, 44, 31 + b, 13, 12, WATER, line='8')
    ell(cv, 53, 36 + b, 6.5, 4.5, FOAM)
    eye(cv, 49, 29, 3.5, 5, iris='9', dy=b)
    # gotita sobre la cabeza
    px(cv, [(51, 8), (50, 9), (51, 9), (52, 9), (50, 10), (51, 10), (52, 10), (51, 11)], 'b', b - f)
    px(cv, [(51, 9)], 'c', b - f)
    # sonrisa pícara con lengua
    px(cv, [(52, 38), (53, 39), (54, 40), (55, 40), (56, 39), (57, 38)], '8', b)
    px(cv, [(54, 39), (55, 39)], '2', b)
    return cv


def chapuzon(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    f = 1 if phase else 0
    poly_fill(cv, [(15, 42 + b), (4, 28 + b - f), (1, 31 + b), (7, 43 + b), (1, 56 + b), (5, 58 + b + f), (15, 47 + b)], '9')
    line(cv, 13, 42 + b, 4, 30 + b - f, 'a')
    # aleta-ala sobre el lomo
    poly_fill(cv, [(28, 34 + b), (16, 17 + b - f), (12, 19 + b), (19, 35 + b)], 'a')
    line(cv, 25, 33 + b, 16, 19 + b - f, 'b')
    legs(cv, phase, 21, 39, WATER, ('8', '8', '9'), rx=4.4, ry=5, line_c='8')
    ell(cv, 29, 42 + b, 17, 10, WATER, line='8')
    ell(cv, 33, 47 + b, 11, 5, FOAM, clip=ellipse_mask(29, 42 + b, 16, 9))
    for x in range(41, 56, 3):
        poly_fill(cv, [(x, 18 + b), (x + 2, 12 + b - (x % 2)), (x + 3, 18 + b)], 'b')
    ell(cv, 48, 27 + b, 12, 11, WATER, line='8')
    ell(cv, 57, 32 + b, 6, 4.5, FOAM)
    eye(cv, 52, 25, 3, 4.2, iris='9', dy=b)
    line(cv, 49, 19 + b, 55, 20 + b, '8')
    px(cv, [(56, 35), (57, 36), (58, 36), (59, 35)], '8', b)
    return cv


# ================================================================== LEGENDARIOS

def wolf(phase, body, mane, eye_glow, tail_color, accent_line):
    """Lobo anguloso (Karman, Ferrolobo)."""
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    f = 1 if phase else 0
    # cola en zigzag (rayo)
    poly_fill(cv, [(14, 37 + b), (5, 30 + b - f), (10, 29 + b), (2, 19 + b - f), (14, 29 + b), (9, 30 + b), (16, 40 + b)], tail_color)
    legs(cv, phase, 18, 40, body, (body[0], body[0], body[1]), rx=3, ry=8.5, foot=body[0], line_c=body[0])
    ell(cv, 28, 40 + b, 17, 8.5, body, line=body[0])
    # melena de púas sobre cuello y lomo
    for i, x in enumerate(range(18, 44, 5)):
        top = 22 - i if x < 40 else 18
        poly_fill(cv, [(x - 3, 35 + b), (x - 1 - f, top + 6 + b), (x + 3, 34 + b)], mane[0])
        line(cv, x - 2, 33 + b, x - 1 - f, top + 7 + b, mane[1])
    # orejas altas
    poly_fill(cv, [(41, 20 + b), (43, 5 + b), (48, 18 + b)], body[1])
    poly_fill(cv, [(43, 17 + b), (44, 9 + b), (46, 17 + b)], mane[0])
    poly_fill(cv, [(47, 18 + b), (52, 4 + b), (54, 19 + b)], body[1])
    poly_fill(cv, [(49, 17 + b), (52, 8 + b), (53, 17 + b)], mane[0])
    ell(cv, 46, 26 + b, 9.5, 7.5, body, line=body[0])
    poly_fill(cv, [(50, 23 + b), (62, 27 + b), (61, 31 + b), (50, 32 + b)], body[1])
    line(cv, 51, 23 + b, 61, 27 + b, body[2])
    px(cv, [(61, 26), (62, 26), (61, 27), (62, 27)], '0', b)
    eye_cool(cv, 46, 22, eye_glow, dy=b)
    line(cv, 52, 31 + b, 60, 31 + b, '0')
    px(cv, [(54, 32), (57, 32)], 'c', b)
    line(cv, 30, 46 + b, 40, 44 + b, accent_line)
    line(cv, 22, 44 + b, 28, 47 + b, accent_line)
    return cv


def karman(phase):
    cv = wolf(phase, NAVY, ('4', 'p'), '4', '4', '9')
    for (x, y) in [(6, 15), (3, 25), (58, 12), (36, 14)]:
        cv.set(x, y - phase, 'p')
    return cv


def draken(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    f = 2 if phase else 0
    # ala de murciélago (atrás)
    wing = [(31, 34 + b), (21, 8 + b + f), (15, 4 + b + f), (11, 12 + b + f), (4, 15 + b + f),
            (8, 23 + b + f), (1, 27 + b + f), (13, 33 + b)]
    poly_fill(cv, wing, '1')
    for (x, y) in [(15, 4), (4, 15), (1, 27)]:
        line(cv, 31, 34 + b, x, y + b + f, 'f')
    # cola larga con punta de fuego
    for i, (x, y, r) in enumerate(lerp_chain((18, 47, 4.5), (4, 54, 1.5), 7)):
        ell(cv, x, y + b, r, r, DARK)
    flame(cv, lerp_chain((4, 54 + b, 2.6), (1, 48 + b, 1), 4), ramp=('1', '2', '3', '4'))
    legs(cv, phase, 22, 39, DARK, ('0', '0', 'f'), rx=4.2, ry=6, foot='c', line_c='1')
    ell(cv, 29, 43 + b, 15, 10, DARK, line='1')
    # panza de brasas encendidas
    ell(cv, 33, 48 + b, 10, 5, ('2', '3', '4'), clip=ellipse_mask(29, 43 + b, 14, 9))
    for x in range(26, 41, 3):
        cv.set(x, 47 + b, '2')
    # cuernos
    poly_fill(cv, [(42, 20 + b), (33, 7 + b), (45, 16 + b)], 'e')
    line(cv, 42, 19 + b, 34, 8 + b, 'd')
    poly_fill(cv, [(46, 18 + b), (43, 3 + b), (50, 16 + b)], 'e')
    line(cv, 46, 17 + b, 43, 4 + b, 'd')
    ell(cv, 48, 26 + b, 10, 8, DARK, line='1')
    poly_fill(cv, [(53, 23 + b), (63, 26 + b), (62, 31 + b), (52, 32 + b)], 'f')
    line(cv, 54, 23 + b, 62, 26 + b, 'e')
    px(cv, [(61, 26), (61, 27)], '2', b)
    eye_cool(cv, 47, 22, '2', dy=b)
    line(cv, 53, 31 + b, 61, 31 + b, '3')
    for (x, y) in [(24, 38), (30, 36), (36, 40), (21, 43)]:
        px(cv, [(x, y), (x + 1, y + 1)], '3', b)
    return cv


def randy(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    f = 2 if phase else 0
    poly_fill(cv, [(14, 43 + b), (3, 27 + b + f), (7, 43 + b), (2, 59 + b - f), (14, 47 + b)], '8')
    line(cv, 12, 43 + b, 4, 29 + b + f, '9')
    poly_fill(cv, [(25, 36 + b), (33, 16 + b), (40, 36 + b)], '9')
    line(cv, 26, 35 + b, 33, 17 + b, 'a')
    px(cv, [(33, 15), (32, 16), (34, 16), (33, 17)], 'c', b)
    ell(cv, 34, 44 + b, 23, 10.5, WATER, line='8')
    ell(cv, 37, 51 + b, 18, 5, SNOW, clip=ellipse_mask(34, 44 + b, 22, 9.5))
    poly_fill(cv, [(34, 50 + b), (27, 60 + b), (41, 53 + b)], '8')
    for (x, y) in [(20, 38), (44, 36), (48, 38)]:
        poly_fill(cv, [(x, y + b), (x + 2, y - 4 + b), (x + 4, y + b)], 'b')
        cv.set(x + 2, y - 3 + b, 'c')
    eye_cool(cv, 46, 40, 'b', dy=b)
    line(cv, 45, 38 + b, 52, 39 + b, '8')
    # sonrisa burlona con dientes
    line(cv, 47, 48 + b, 58, 46 + b, '0')
    for x in range(49, 58, 2):
        cv.set(x, 47 + b, 'c')
    line(cv, 22, 46 + b, 26, 48 + b, '8'); line(cv, 23, 43 + b, 27, 45 + b, '8')
    return cv


# ================================================================== SALVAJES

def plumin(phase):
    cv = Canvas(64, 64)
    up = -3 if phase else 0
    poly_fill(cv, [(19, 44 + up), (8, 38 + up), (10, 44 + up), (7, 49 + up), (19, 48 + up)], 'a')
    line(cv, 18, 45 + up, 10, 41 + up, 'b')
    px(cv, [(28, 58), (29, 59), (34, 58), (35, 59)], 'o', up // 2)
    ell(cv, 31, 44 + up, 14, 13, SNOW, line='d')
    ell(cv, 27, 46 + up, 7, 5.5, ('9', 'a', 'b'), line='9')
    for i in range(3):
        line(cv, 23 + i * 3, 48 + up, 26 + i * 3, 50 + up, '9')
    # copete
    for (x0, x1, y1) in [(30, 27, 25), (32, 32, 23), (34, 38, 25)]:
        line(cv, x0, 32 + up, x1, y1 + up, 'a')
        cv.set(x1, y1 + up, 'b')
    eye(cv, 38, 40, 2.8, 4, iris='9', dy=up)
    poly_fill(cv, [(43, 42 + up), (48, 44 + up), (43, 46 + up)], '4')
    line(cv, 44, 45 + up, 47, 44 + up, 'o')
    px(cv, [(37, 47), (38, 47), (39, 47)], 'h', up)
    return cv


def topin(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    poly_fill(cv, [(16, 50 + b), (9, 47 + b), (16, 46 + b)], 'i')
    ell(cv, 24 + phase, 57, 5, 3.5, SAND, line='j')
    ell(cv, 31, 47 + b, 15, 11.5, EARTH, line='j')
    ell(cv, 34, 52 + b, 9, 5, SAND, clip=ellipse_mask(31, 47 + b, 14, 10.5))
    ell(cv, 47, 46 + b, 7.5, 6.5, SAND, line='j')
    ell(cv, 54, 44 + b, 3, 2.5, ('2', 'h', 'g'))
    # ojitos cerrados y felices
    px(cv, [(41, 41), (42, 40), (43, 40), (44, 41)], '0', b)
    px(cv, [(44, 49), (45, 50), (46, 50)], 'j', b)
    # garras grandes
    ell(cv, 44 - phase * 2, 57, 6, 4, ('o', 'p', 'p'), line='i')
    for dx in (0, 3, 6):
        line(cv, 46 + dx - phase * 2, 57, 47 + dx - phase * 2, 60, 'c')
    return cv


def zumbi(phase):
    cv = Canvas(64, 64)
    up = -2 if phase else 0
    poly_fill(cv, [(14, 44 + up), (8, 46 + up), (14, 48 + up)], 'f')
    abd = ellipse_mask(24, 44 + up, 11, 9)
    ell(cv, 24, 44 + up, 11, 9, ELEC, line='o')
    for x0 in (17, 22, 27):
        for x in (x0, x0 + 1):
            for y in range(30, 56):
                if abd(x, y):
                    cv.set(x, y, 'j')
    for (x, dy) in [(30, 0), (34, 1), (38, 0)]:
        line(cv, x, 51 + up, x - 1, 57 + up, 'j')
    ell(cv, 40, 39 + up, 10, 9.5, ELEC, line='o')
    # alas que aletean
    wy = 22 if phase else 26
    ell(cv, 28, wy, 9, 5, SNOW, line='d')
    ell(cv, 35, wy - 3, 7, 4, ('d', 'c', 'b'), line='d')
    line(cv, 42, 30 + up, 46, 21 + up, 'j'); ell(cv, 46, 20 + up, 1.8, 1.8, ('j', 'j', 'j'))
    line(cv, 45, 31 + up, 51, 24 + up, 'j'); ell(cv, 51, 23 + up, 1.8, 1.8, ('j', 'j', 'j'))
    eye(cv, 44, 38, 3, 4.5, iris='j', dy=up)
    px(cv, [(46, 45), (47, 46), (48, 45)], 'j', up)
    px(cv, [(38, 44), (39, 44)], 'n', up)
    return cv


def chispin(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    f = 1 if phase else 0
    poly_fill(cv, [(18, 45 + b), (8, 39 + b - f), (13, 37 + b), (4, 25 + b - f), (17, 35 + b), (12, 37 + b), (20, 43 + b)], '4')
    line(cv, 16, 43 + b, 8, 38 + b - f, 'p')
    legs(cv, phase, 23, 36, ELEC, ('o', 'o', '4'), rx=3.4, ry=5, foot='o', line_c='o')
    ell(cv, 28, 46 + b, 12, 9, ELEC, line='o')
    ell(cv, 31, 50 + b, 7, 4.5, ('4', 'p', 'p'), clip=ellipse_mask(28, 46 + b, 11, 8))
    # orejas-antena con punta metálica
    for (x0, x1, y1) in [(37, 33, 14), (45, 49, 13)]:
        line(cv, x0, 25 + b, x1, y1 + b, 'o')
        line(cv, x0 + 1, 25 + b, x1 + 1, y1 + b, '4')
        ell(cv, x1 + .5, y1 - 1 + b, 2, 2, METAL)
    ell(cv, 42, 34 + b, 12, 11, ELEC, line='o')
    ell(cv, 50, 38 + b, 5.5, 4.5, ('4', 'p', 'p'))
    ell(cv, 41, 38 + b, 2.5, 2, ('2', 'n', 'n'))
    eye(cv, 46, 32, 3.2, 4.5, iris='j', dy=b)
    px(cv, [(52, 40), (53, 41), (54, 41), (55, 40)], 'j', b)
    for (x, y) in [(6, 22), (3, 32), (12, 30)]:
        cv.set(x, y - f, 'b')
    return cv


def copito(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    legs(cv, phase, 24, 36, SNOW, ('d', 'd', 'c'), rx=3.6, ry=4.5, line_c='d')
    ell(cv, 30, 45 + b, 14, 12, SNOW, line='d')
    ell(cv, 22, 40 + b, 5, 4, ('d', 'c', 'c'))
    # carámbanos en la cabeza
    for (x, h) in [(26, 6), (30, 8), (34, 6), (38, 5)]:
        poly_fill(cv, [(x - 2, 34 + b), (x, 34 - h + b), (x + 2, 34 + b)], 'b')
        cv.set(x, 34 - h + 2 + b, 'c')
    # copo de nieve
    for (dx, dy) in [(0, 0), (-1, 0), (1, 0), (0, -1), (0, 1), (-2, -2), (2, 2), (2, -2), (-2, 2)]:
        cv.set(42 + dx, 29 + dy + b, 'a')
    eye(cv, 39, 42, 3, 4.5, iris='9', dy=b)
    px(cv, [(43, 49), (44, 50), (45, 49)], 'e', b)
    px(cv, [(36, 48), (37, 48), (47, 46), (48, 46)], 'a', b)
    return cv


def pantuflo(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    ell(cv, 31 + phase, 61, 22, 1.6, ('9', 'a', 'a'))
    ell(cv, 28, 51 + b, 20, 8, EARTH, line='j')
    ell(cv, 46, 48 + b, 11, 10, EARTH, line='j')
    # boca de la pantufla, mullida
    ell(cv, 24, 44 + b, 12, 4.5, ('o', 'p', 'p'), line='o')
    ell(cv, 24, 45 + b, 9, 2.5, ('j', 'j', 'i'))
    ell(cv, 49, 38 + b - phase, 4, 4, ('o', 'p', 'p'))
    # ojos dormidos y sonrisa
    px(cv, [(46, 46), (47, 47), (48, 47), (49, 46)], '0', b)
    px(cv, [(52, 51), (53, 52), (54, 52), (55, 51)], 'j', b)
    px(cv, [(43, 51), (44, 51)], 'h', b)
    for (x, y) in [(16, 57), (37, 58)]:
        px(cv, [(x, y), (x, y + 1)], '9', b)
    return cv


def bostezo(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    # cola con luna
    for (x, y, r) in lerp_chain((18, 48, 3), (8, 34, 1.6), 6):
        ell(cv, x, y + b, r, r, SHADOW)
    poly_fill(cv, [(5, 28 + b), (9, 25 + b), (11, 30 + b), (8, 33 + b), (10, 29 + b)], '4')
    legs(cv, phase, 24, 36, SHADOW, ('1', '1', 'e'), rx=3.5, ry=4.5, line_c='1')
    ell(cv, 29, 46 + b, 13, 10, SHADOW, line='1')
    ell(cv, 41, 37 + b, 12, 11, SHADOW, line='1')
    # orejas caídas
    ell(cv, 33, 37 + b, 3, 7, ('1', '1', 'e'), line='1')
    # ojo entrecerrado y bostezo
    line(cv, 43, 34 + b, 48, 34 + b, '0')
    px(cv, [(43, 35), (48, 35)], '0', b)
    line(cv, 43, 32 + b, 48, 32 + b, '1')
    ell(cv, 49.5, 41 + b, 3, 3.5, ('0', '2', '2'))
    for (x, y, c) in [(54, 25, 'd'), (55, 25, 'd'), (55, 26, 'd'), (54, 27, 'd'), (55, 27, 'd'),
                      (58, 20, 'd'), (59, 20, 'd'), (59, 21, 'd'), (58, 22, 'd'), (59, 22, 'd')]:
        cv.set(x, y - phase, c)
    return cv


def farolito(phase):
    cv = Canvas(64, 64)
    up = -2 if phase else 0
    wy = 25 if phase else 29
    ell(cv, 29, wy, 8, 4.5, SNOW, line='d')
    ell(cv, 34, wy - 3, 6, 3.5, ('d', 'c', 'b'), line='d')
    ell(cv, 23, 45 + up, 11, 10, ('4', 'p', 'c'), line='o')
    ell(cv, 21, 47 + up, 6, 5, ('p', 'c', 'c'))
    for x in (17, 22, 27):
        line(cv, x, 36 + up, x, 54 + up, 'o')
    ell(cv, 39, 36 + up, 9, 8.5, ('o', '4', 'p'), line='o')
    line(cv, 42, 28 + up, 45, 20 + up, 'j'); ell(cv, 45.5, 19 + up, 2, 2, ('4', 'p', 'c'))
    line(cv, 44, 29 + up, 50, 23 + up, 'j'); ell(cv, 50.5, 22 + up, 2, 2, ('4', 'p', 'c'))
    eye(cv, 42, 35, 2.8, 4, iris='j', dy=up)
    px(cv, [(45, 41), (46, 42), (47, 41)], 'j', up)
    for (x, y) in [(8, 40), (10, 52), (14, 33)]:
        cv.set(x, y + up, 'p')
    return cv


# ================================================================== EXCLUSIVOS (Capitana Vera)

def tuerquita(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    for i in range(4):
        line(cv, 17 - i * 2, 44 + b + (i % 2) * 3, 15 - i * 2, 47 + b - (i % 2) * 3, 'd')
    legs(cv, phase, 23, 36, METAL, ('f', 'f', 'e'), rx=3.5, ry=5, foot='f', line_c='f')
    ell(cv, 28, 46 + b, 12, 9, METAL, line='f')
    for x in (22, 28, 34):
        cv.set(x, 42 + b, 'c')
    ell(cv, 42, 35 + b, 11, 10, METAL, line='f')
    # tuerca hexagonal sobre la cabeza
    hexa = [(36, 19), (39, 13), (45, 13), (48, 19), (45, 25), (39, 25)]
    poly_fill(cv, [(x, y + b) for x, y in hexa], 'd')
    line(cv, 39, 14 + b, 45, 14 + b, 'c')
    line(cv, 40, 24 + b, 45, 24 + b, 'e')
    ell(cv, 42, 19 + b, 2.2, 2.2, ('f', 'f', 'f'))
    ell(cv, 49, 39 + b, 5, 4, ('e', 'd', 'c'))
    eye(cv, 45, 33, 3, 4.2, iris='a', dy=b)
    px(cv, [(50, 42), (51, 43), (52, 43), (53, 42)], 'f', b)
    return cv


def ferrolobo(phase):
    return wolf(phase, METAL, ('1', 'e'), '2', 'e', '1')


def imanta(phase):
    cv = Canvas(64, 64)
    b = -1 if phase else 0
    poly_fill(cv, [(16, 42 + b), (8, 37 + b), (12, 35 + b), (6, 29 + b), (16, 36 + b)], '4')
    legs(cv, phase, 21, 38, METAL, ('f', 'f', 'e'), rx=3.6, ry=7, foot='f', line_c='f')
    ell(cv, 28, 43 + b, 15, 10, METAL, line='f')
    # imán en U sobre la cabeza (rojo con puntas plateadas)
    for (x0, x1) in [(40, 43), (50, 53)]:
        for x in range(x0, x1):
            for y in range(7, 19):
                cv.set(x, y + b, '3' if x == x0 else '2')
    for x in range(40, 53):
        for y in range(16, 20):
            cv.set(x, y + b, '2')
    line(cv, 41, 19 + b, 51, 19 + b, '1')
    for (x0, x1) in [(40, 43), (50, 53)]:
        for x in range(x0, x1):
            for y in range(7, 10):
                cv.set(x, y + b, 'c' if y < 9 else 'd')
    ell(cv, 46, 27 + b, 11, 10, METAL, line='f')
    ell(cv, 54, 32 + b, 6, 4.5, ('e', 'd', 'c'))
    eye(cv, 50, 25, 3, 4.3, iris='2', dy=b)
    px(cv, [(55, 35), (56, 36), (57, 36), (58, 35)], 'f', b)
    for (x, y) in [(36, 10), (56, 9), (60, 15)]:
        cv.set(x, y - phase, 'b')
    return cv


# ================================================================== SALIDA

MEMOS = [
    ("tostin", tostin, shiny("3>a", "2>9", "4>b", "p>c", "n>8")),
    ("brason", brason, shiny("3>a", "2>9", "4>b", "p>c", "e>1")),
    ("brotito", brotito, shiny("6>4", "5>p", "m>o", "3>2")),
    ("ramazon", ramazon, shiny("6>4", "5>p", "m>o", "i>e", "j>f", "o>d")),
    ("charquito", charquito, shiny("9>n", "8>2", "a>3", "b>4")),
    ("chapuzon", chapuzon, shiny("9>n", "8>2", "a>3", "b>4")),
    ("karman", karman, shiny("4>b", "p>c", "8>1", "9>a")),
    ("draken", draken, shiny("2>9", "3>a", "4>b", "1>8")),
    ("randy", randy, shiny("9>e", "8>f", "a>d", "b>4")),
    ("plumin", plumin, shiny("a>4", "9>o", "b>p")),
    ("topin", topin, shiny("i>e", "j>f", "o>d", "p>c")),
    ("zumbi", zumbi, shiny("4>5", "o>6", "p>c", "j>m")),
    ("chispin", chispin, shiny("4>b", "o>a", "p>c", "n>9", "2>8")),
    ("copito", copito, shiny("a>2", "b>3", "9>1")),
    ("pantuflo", pantuflo, shiny("i>9", "j>8", "o>a", "p>c")),
    ("bostezo", bostezo, shiny("1>m", "e>6", "d>5", "4>c")),
    ("farolito", farolito, shiny("4>b", "p>c", "o>a")),
    ("tuerquita", tuerquita, shiny("e>o", "d>4", "f>i")),
    ("ferrolobo", ferrolobo, shiny("e>o", "d>4", "f>i")),
    ("imanta", imanta, shiny("e>o", "d>4", "f>i")),
]

TOSTIN_WORLD_0 = [
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
TOSTIN_WORLD_1 = TOSTIN_WORLD_0[:2] + [
    "..3......3..4...",
    "..33....333343..",
    "..33333333342...",
] + TOSTIN_WORLD_0[5:13] + [
    "...333..333.....",
    "...222..222.....",
    "................",
]

if __name__ == "__main__":
    out = "# Memos. Carrera/MemoBox: 64x64 de perfil (pivote abajo). Mundo: 16x16 de frente.\n" \
          "# Generado por Tools/PixelArt/make_memos.py: no editar a mano.\n\n"
    out += block("tostin_world", 16, 16, [TOSTIN_WORLD_0, TOSTIN_WORLD_1],
                 header=["pivot 0.5 0", "outline 0"], extra=[MEMOS[0][2], COLLAR])
    for name, fn, shiny_variant in MEMOS:
        out += block(f"{name}_race", 64, 64, [fn(0).rows(), fn(1).rows()],
                     header=["pivot 0.5 0", "outline 0"], extra=[shiny_variant, COLLAR])
    open(os.path.join(OUT, "Memos.txt"), "w", encoding="utf-8").write(out)
    print(f"ok: {len(MEMOS)} Memos")
