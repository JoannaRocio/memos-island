"""Audio provisorio de Memos Island (Fase 9B), generado por código como el pixel art.

La música (relajante: arpa, colchón, piano eléctrico, flauta y campanitas, con reverberación) está en music.py;
acá están los efectos (un sintetizador chiptune mínimo) y la escritura de los WAV. Escribe WAV (mono, 16 bits, 22050 Hz) en
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
    "blip": tone(740, 740, 0.03, kind="sine", vol=0.30),
    "cursor": tone(1046, 1046, 0.04, kind="tri", vol=0.35),
    "confirm": notes_sfx(["e6", "b6"], 0.06, kind="tri", vol=0.45),
    "cancel": notes_sfx(["b5", "e5"], 0.06, kind="tri", vol=0.45),
    "step": noise(0.05, vol=0.25, decay=70, seed=3),
    "bubble": tone(600, 1200, 0.08, kind="sine", vol=0.5),
    "get": notes_sfx(["c6", "e6", "g6", "c7"], 0.08, kind="sine", vol=0.45, decay=4),
    "save": notes_sfx(["g5", "c6", "e6"], 0.08, kind="tri", vol=0.6),
    "race_beep": tone(660, 660, 0.14, duty=0.5, vol=0.5),          # 3, 2, 1…
    "race_start": tone(1320, 1320, 0.45, duty=0.5, vol=0.5),       # ¡YA!
    "race_win": notes_sfx(["c6", "e6", "g6", "c7", "g6", "c7"], 0.12, kind="tri", vol=0.5),
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
    import music
    for name, make in music.MUSIC.items():
        write("Music", name, make(), peak=0.7)
        print("  tema listo:", name)
    for name, samples in SFX.items():
        write("Sfx", name, samples)
    for i in range(1, 21):
        write("Sfx", f"cry_{i:02d}", cry(i))
    print(f"Listo: {len(music.MUSIC)} temas y {len(SFX) + 20} efectos en Resources/Audio.")
