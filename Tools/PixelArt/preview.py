"""Hoja de contacto ampliada de los sprites (sin dependencias): python preview.py archivo.txt [escala] [salida.png]"""
import sys, zlib, struct

PAL = {'0': "1a1c2c", '1': "5d275d", '2': "b13e53", '3': "ef7d57", '4': "ffcd75", '5': "a7f070", '6': "38b764",
       '7': "257179", '8': "29366f", '9': "3b5dc9", 'a': "41a6f6", 'b': "73eff7", 'c': "f4f4f4", 'd': "94b0c2",
       'e': "566c86", 'f': "333c57", 'g': "f6d2b0", 'h': "d99a74", 'i': "8f5a3c", 'j': "5a3328", 'k': "9b3cff",
       'l': "d8a6ff", 'm': "1e5a3c", 'n': "c46a3a", 'o': "d9a066", 'p': "fff1c9"}
BG = (0x88, 0x88, 0x99)

def parse(path):
    lines = open(path, encoding="utf-8").read().splitlines()
    sprites, cur, i = [], None, 0
    while i < len(lines):
        p = lines[i].split()
        if not p or p[0].startswith('#'):
            i += 1; continue
        if p[0] == 'sprite': cur = {'name': p[1], 'frames': [], 'outline': None}
        elif p[0] == 'size': cur['w'], cur['h'] = int(p[1]), int(p[2])
        elif p[0] == 'outline': cur['outline'] = p[1]
        elif p[0] == 'frame':
            f = [lines[i + 1 + k].strip().ljust(cur['w'], '.')[:cur['w']] for k in range(cur['h'])]
            cur['frames'].append(f); i += cur['h']
        elif p[0] == 'flip': cur['frames'].append([r[::-1] for r in cur['frames'][int(p[1])]])
        elif p[0] == 'copyfrom': cur['frames'] += [list(f) for s in sprites if s['name'] == p[1] for f in s['frames']]
        elif p[0] == 'mirror': cur['frames'] = [[r[::-1] for r in f] for f in cur['frames']]
        elif p[0] == 'end': sprites.append(cur)
        i += 1
    return sprites

def outline(f, c):
    h, w = len(f), len(f[0]); g = [list(r) for r in f]
    T = lambda ch: ch in '. '
    for y in range(h):
        for x in range(w):
            if T(f[y][x]) and any(0 <= x + dx < w and 0 <= y + dy < h and not T(f[y + dy][x + dx])
                                  for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                g[y][x] = c
    return [''.join(r) for r in g]

def main():
    src = sys.argv[1]; sc = int(sys.argv[2]) if len(sys.argv) > 2 else 6
    out = sys.argv[3] if len(sys.argv) > 3 else "preview.png"
    frames = []
    for s in parse(src):
        for f in s['frames']:
            frames.append(outline(f, s['outline']) if s['outline'] else f)
    pad = 4
    W = sum(len(f[0]) * sc + pad for f in frames) + pad
    H = max(len(f) for f in frames) * sc + 2 * pad
    img = [[BG] * W for _ in range(H)]
    x0 = pad
    for f in frames:
        for y, row in enumerate(f):
            for x, ch in enumerate(row):
                if ch in PAL:
                    col = tuple(int(PAL[ch][k:k + 2], 16) for k in (0, 2, 4))
                    for yy in range(sc):
                        for xx in range(sc):
                            img[pad + y * sc + yy][x0 + x * sc + xx] = col
        x0 += len(f[0]) * sc + pad
    raw = b''.join(b'\x00' + bytes(c for px in row for c in px) for row in img)
    def chunk(t, d): return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 2, 0, 0, 0)) + \
          chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    open(out, 'wb').write(png)
    print(out, W, H)

main()
