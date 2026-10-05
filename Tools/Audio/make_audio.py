"""Audio provisorio de Memos Island (Fase 9B), generado por código como el pixel art.

Un sintetizador chiptune mínimo (pulso, triángulo, ruido y "cajita de música") arma, desde partituras escritas acá,
la música en loop (GDD §19) y los efectos. Escribe WAV (mono, 16 bits, 22050 Hz) en
Assets/_MemosIsland/Resources/Audio/Music y .../Sfx. Python puro, sin dependencias.

Notación de las partituras: "nota:duración" en semicorcheas (16 = un compás de 4/4), por ejemplo "c5:4" o "r:2" (silencio).
Uso: python make_audio.py
"""
import math
import os
import random
import struct
import wave

RATE = 22050
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "_MemosIsland", "Resources", "Audio")

NOTE = {"c": 0, "c#": 1, "db": 1, "d": 2, "d#": 3, "eb": 3, "e": 4, "f": 5, "f#": 6, "gb": 6, "g": 7, "g#": 8,
        "ab": 8, "a": 9, "a#": 10, "bb": 10, "b": 11}


def freq(name):
    """'c#5' → Hz (a4 = 440)."""
    pitch, octave = name[:-1], int(name[-1])
    midi = 12 * (octave + 1) + NOTE[pitch]
    return 440.0 * 2 ** ((midi - 69) / 12)


# ------------------------------------------------------------------ Osciladores

def osc(kind, f, t, duty=0.5):
    phase = (f * t) % 1.0
    if kind == "pulse":
        return 1.0 if phase < duty else -1.0
    if kind == "tri":
        return 4 * abs(phase - 0.5) - 1
    if kind == "box":  # cajita de música: seno con un armónico brillante
        return 0.75 * math.sin(2 * math.pi * phase) + 0.25 * math.sin(4 * math.pi * phase)
    return 0.0


def envelope(i, n, attack, release, decay=0.0):
    a = int(attack * RATE)
    r = int(release * RATE)
    v = 1.0
    if i < a:
        v = i / max(1, a)
    elif decay > 0:
        v = math.exp(-decay * (i - a) / RATE)
    if i > n - r:
        v *= max(0.0, (n - i) / max(1, r))
    return v


def render_notes(buf, notes, bpm, kind, volume, duty=0.5, attack=0.004, release=0.03, decay=0.0, vibrato=0.0):
    """Suma una voz al buffer. notes = 'c5:4 r:2 ...' (semicorcheas)."""
    step = 60.0 / bpm / 4
    pos = 0
    for token in notes.split():
        name, units = token.split(":")
        n = int(int(units) * step * RATE)
        if name != "r":
            f = freq(name)
            for i in range(n):
                if pos + i >= len(buf):
                    break
                t = i / RATE
                ff = f * (1 + vibrato * math.sin(2 * math.pi * 5.5 * t)) if vibrato else f
                buf[pos + i] += volume * osc(kind, ff, t, duty) * envelope(i, n, attack, release, decay)
        pos += n
    return pos


CHORDS = {
    "C": "c e g", "Dm": "d f a", "Em": "e g b", "F": "f a c", "G": "g b d", "Am": "a c e", "Bdim": "b d f",
    "D": "d f# a", "E": "e g# b", "A": "a c# e", "B": "b d# f#", "Bm": "b d f#", "C#m": "c# e g#",
    "F#m": "f# a c#", "Gm": "g a# d", "Bb": "a# d f", "Eb": "d# g a#", "Cm": "c d# g", "G#m": "g# b d#",
}


def bass_line(chords, pattern):
    """Bajo: por compás, la fundamental (y la quinta) según el patrón."""
    out = []
    for ch in chords:
        notes = CHORDS[ch].split()
        root, fifth = notes[0], notes[2]
        if pattern == "pulse":      # negras alternando fundamental y quinta
            out += [f"{root}3:4", f"{fifth}3:4", f"{root}3:4", f"{fifth}3:4"]
        elif pattern == "drive":    # corcheas en la fundamental
            out += [f"{root}2:2", f"{root}3:2"] * 4
        elif pattern == "long":     # redonda
            out += [f"{root}2:16"]
        else:                       # blancas
            out += [f"{root}3:8", f"{fifth}2:8"]
    return " ".join(out)


def arp_line(chords, octave, step_units):
    out = []
    for ch in chords:
        notes = CHORDS[ch].split()
        seq = notes + [notes[1]]
        for i in range(16 // step_units):
            out.append(f"{seq[i % len(seq)]}{octave}:{step_units}")
    return " ".join(out)


def drums(buf, bpm, bars, pattern, volume):
    rnd = random.Random(7)
    step = 60.0 / bpm / 4
    for bar in range(bars):
        for s in range(16):
            start = int((bar * 16 + s) * step * RATE)
            hit = pattern.get(s)
            if not hit:
                continue
            length = {"k": 0.12, "s": 0.10, "h": 0.03}[hit]
            n = int(length * RATE)
            for i in range(n):
                if start + i >= len(buf):
                    break
                t = i / RATE
                env = math.exp(-t * {"k": 28, "s": 30, "h": 120}[hit])
                if hit == "k":   # bombo: seno que cae de tono
                    v = math.sin(2 * math.pi * (110 - 400 * t) * t)
                else:
                    v = rnd.uniform(-1, 1) * (0.8 if hit == "s" else 0.5)
                buf[start + i] += volume * v * env


def song(bpm, chords, melody, lead="pulse", lead_duty=0.5, lead_vol=0.24, bass="pulse", arp_oct=4, arp_step=2,
         arp_vol=0.08, arp_kind="pulse", drum_pattern=None, drum_vol=0.18, lead_decay=0.0, vibrato=0.0, repeats=1):
    bars = len(chords)
    total = int(bars * 16 * 60.0 / bpm / 4 * RATE) * repeats
    buf = [0.0] * total
    one = total // repeats
    for r in range(repeats):
        part = [0.0] * one
        render_notes(part, melody, bpm, lead, lead_vol, duty=lead_duty, decay=lead_decay, vibrato=vibrato,
                     release=0.05 if lead != "box" else 0.08)
        render_notes(part, bass_line(chords, bass), bpm, "tri", 0.30, release=0.02)
        if arp_step:
            render_notes(part, arp_line(chords, arp_oct, arp_step), bpm, arp_kind, arp_vol, duty=0.25,
                         decay=6.0 if arp_kind == "box" else 0.0)
        if drum_pattern:
            drums(part, bpm, bars, drum_pattern, drum_vol)
        for i, v in enumerate(part):
            buf[r * one + i] = v
    return buf


# ------------------------------------------------------------------ Escritura

def write(folder, name, samples, peak=0.85):
    os.makedirs(os.path.join(ROOT, folder), exist_ok=True)
    top = max(1e-6, max(abs(s) for s in samples))
    k = peak / top
    with wave.open(os.path.join(ROOT, folder, name + ".wav"), "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s * k)) * 32767)) for s in samples))


# ------------------------------------------------------------------ Música (GDD §19)

HATS = {0: "k", 4: "h", 8: "s", 12: "h"}
DRIVE = {0: "k", 2: "h", 4: "s", 6: "h", 8: "k", 10: "h", 12: "s", 14: "h"}
CLOCK = {0: "h", 8: "h"}

MUSIC = {
    # Pueblo Puerto: alegre, Do mayor.
    "pueblo": song(118, ["C", "Am", "F", "G", "C", "Am", "F", "G"],
                   "e5:4 g5:4 c6:4 g5:4  a5:4 g5:4 e5:8  f5:4 a5:4 d6:4 c6:4  b5:8 g5:8 "
                   "e5:4 g5:4 c6:4 e6:4  d6:4 c6:4 a5:8  f5:4 d6:4 c6:4 b5:4  c6:12 r:4",
                   drum_pattern=HATS, drum_vol=0.10),
    # Refugio: tranquilo, Fa mayor, sin batería.
    "refugio": song(84, ["F", "C", "Dm", "Bb", "F", "C", "Bb", "C"],
                    "a5:8 c6:8  g5:12 e5:4  f5:8 a5:4 d6:4  d6:8 c6:8 "
                    "a5:4 c6:4 f6:8  e6:8 c6:8  d6:4 c6:4 a5:4 g5:4  f5:16",
                    lead="tri", lead_vol=0.30, arp_step=4, arp_vol=0.07, vibrato=0.004),
    # Rutas: aventura, Sol mayor.
    "ruta": song(140, ["G", "D", "Em", "C", "G", "D", "C", "D"],
                 "d5:4 g5:4 b5:4 d6:4  a5:8 f#5:4 d5:4  e5:4 g5:4 b5:4 e6:4  c6:8 g5:8 "
                 "b5:4 d6:4 g6:4 d6:4  a5:4 f#5:4 a5:4 d6:4  c6:4 b5:4 a5:4 g5:4  a5:8 d5:8",
                 lead_duty=0.25, drum_pattern=HATS, drum_vol=0.14),
    # Carrera: rápida, La menor.
    "carrera": song(160, ["Am", "F", "C", "G", "Am", "F", "G", "E"],
                    "a5:2 c6:2 e6:4 a5:2 c6:2 e6:4  f6:4 e6:2 d6:2 c6:4 a5:4  g5:2 c6:2 e6:4 g6:4 e6:4  d6:4 b5:4 g5:8 "
                    "a5:2 c6:2 e6:4 a6:4 g6:4  f6:4 e6:2 f6:2 a6:8  g6:4 f6:4 e6:4 d6:4  e6:8 g#5:8",
                    bass="drive", lead_duty=0.25, arp_oct=5, drum_pattern=DRIVE, drum_vol=0.20),
    # Copa: épica, Re mayor.
    "copa": song(132, ["D", "G", "A", "D", "Bm", "G", "A", "D"],
                 "d5:4 f#5:2 a5:2 d6:8  b5:4 d6:4 g6:8  a5:2 a5:2 c#6:4 e6:4 a6:4  f#6:8 d6:8 "
                 "f#6:4 e6:4 d6:4 b5:4  g6:4 f#6:4 e6:4 d6:4  e6:4 a6:4 g6:4 e6:4  d6:16",
                 bass="drive", drum_pattern=DRIVE, drum_vol=0.18),
    # Ápice: inquietante, Re menor, lento, con un tic tac de reloj.
    "apice": song(76, ["Dm", "Bb", "Gm", "A", "Dm", "Bb", "Eb", "A"],
                  "d4:8 f4:4 e4:4  d4:12 r:4  g4:8 a#4:4 a4:4  a4:12 c#5:4 "
                  "d5:8 c5:4 a#4:4  a4:12 r:4  g4:4 a#4:4 d#5:8  c#5:8 a4:8",
                  lead_duty=0.125, lead_vol=0.22, bass="long", arp_step=0, drum_pattern=CLOCK, drum_vol=0.08),
    # La cajita de música del abuelo (leitmotiv; también el título).
    "cajita": song(96, ["E", "B", "C#m", "A", "E", "B", "A", "B"],
                   "g#5:4 b5:4 e6:4 d#6:4  c#6:4 b5:8 g#5:4  a5:4 c#6:4 e6:4 g#6:4  f#6:12 r:4 "
                   "e6:4 g#6:4 b6:4 g#6:4  f#6:4 e6:4 d#6:4 b5:4  c#6:4 e6:4 d#6:4 f#6:4  e6:16",
                   lead="box", lead_vol=0.32, lead_decay=3.0, bass="half", arp_oct=5, arp_step=4, arp_vol=0.10,
                   arp_kind="box"),
}

# ------------------------------------------------------------------ Efectos

def tone(f0, f1, length, kind="pulse", duty=0.5, vol=0.6, attack=0.002, decay=0.0):
    n = int(length * RATE)
    out = []
    ph = 0.0
    for i in range(n):
        f = f0 + (f1 - f0) * i / n
        ph += f / RATE
        p = ph % 1.0
        v = (1 if p < duty else -1) if kind == "pulse" else (4 * abs(p - 0.5) - 1) if kind == "tri" else math.sin(2 * math.pi * p)
        out.append(vol * v * envelope(i, n, attack, 0.01, decay))
    return out


def noise(length, vol=0.5, decay=20.0, seed=1):
    rnd = random.Random(seed)
    n = int(length * RATE)
    return [vol * rnd.uniform(-1, 1) * math.exp(-decay * i / RATE) for i in range(n)]


def seq(*parts):
    out = []
    for p in parts:
        out += p
    return out


def silence(t):
    return [0.0] * int(t * RATE)


def notes_sfx(names, each, kind="pulse", duty=0.5, vol=0.5, decay=0.0):
    return seq(*[tone(freq(n), freq(n), each, kind, duty, vol, decay=decay) for n in names])


SFX = {
    "blip": tone(880, 880, 0.025, duty=0.25, vol=0.35),
    "cursor": tone(1320, 1320, 0.035, duty=0.25, vol=0.4),
    "confirm": notes_sfx(["e6", "b6"], 0.05, duty=0.25),
    "cancel": notes_sfx(["b5", "e5"], 0.05, duty=0.25),
    "step": noise(0.05, vol=0.25, decay=70, seed=3),
    "bubble": tone(600, 1200, 0.08, kind="sine", vol=0.5),
    "get": notes_sfx(["c6", "e6", "g6", "c7"], 0.07, duty=0.25, vol=0.45),
    "save": notes_sfx(["g5", "c6", "e6"], 0.08, kind="tri", vol=0.6),
    "race_beep": tone(660, 660, 0.14, duty=0.5, vol=0.5),          # 3, 2, 1…
    "race_start": tone(1320, 1320, 0.45, duty=0.5, vol=0.5),       # ¡YA!
    "race_win": notes_sfx(["c6", "e6", "g6", "c7", "g6", "c7"], 0.11, duty=0.25, vol=0.5),
    "race_lose": notes_sfx(["g5", "e5", "c5"], 0.18, kind="tri", vol=0.6),
    "collar": seq(tone(2400, 300, 0.35, kind="sine", vol=0.4, decay=4), noise(0.4, vol=0.35, decay=9, seed=5)),
    "sparkle": notes_sfx(["e7", "b6", "g#7"], 0.05, kind="sine", vol=0.35, decay=12),
    "door": noise(0.12, vol=0.3, decay=25, seed=9),
    "thunder": noise(1.2, vol=0.6, decay=2.5, seed=11),
}


def cry(index):
    """Grito corto de cada Memo (como en Pokémon): barrido de tono propio según su número."""
    rnd = random.Random(1000 + index)
    base = 300 + rnd.random() * 700
    up = base * (1.2 + rnd.random() * 0.8)
    kind = rnd.choice(["pulse", "pulse", "tri", "sine"])
    duty = rnd.choice([0.125, 0.25, 0.5])
    a = tone(base, up, 0.09 + rnd.random() * 0.06, kind, duty, 0.5)
    b = tone(up, base * (0.7 + rnd.random() * 0.4), 0.12 + rnd.random() * 0.1, kind, duty, 0.45, decay=6)
    return seq(a, b)


if __name__ == "__main__":
    for name, samples in MUSIC.items():
        write("Music", name, samples, peak=0.8)
    for name, samples in SFX.items():
        write("Sfx", name, samples)
    for i in range(1, 21):
        write("Sfx", f"cry_{i:02d}", cry(i))
    print(f"Listo: {len(MUSIC)} temas y {len(SFX) + 20} efectos en Resources/Audio.")
