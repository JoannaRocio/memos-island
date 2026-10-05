"""Música de Memos Island (Fase 9B, versión relajante).

Instrumentos suaves hechos por código, en Python puro:
- pluck: cuerda pulsada (Karplus-Strong), para arpas y bajos.
- pad: colchón de acordes con varias voces apenas desafinadas y entrada lenta.
- epiano: piano eléctrico (seno con un armónico que se apaga).
- flute: flauta con soplo y vibrato.
- bell: campanita de cajita de música (parcial inarmónico).
Todo pasa por una reverberación y un filtro suave. Los loops se renderizan dos veces y se usa la segunda vuelta,
así la cola de la reverberación del final se oye al empezar y el loop no corta.

Notación: "nota:duración" en semicorcheas (16 = un compás de 4/4), "r:4" = silencio.
"""
import math
import random

RATE = 22050

NOTE = {"c": 0, "c#": 1, "db": 1, "d": 2, "d#": 3, "eb": 3, "e": 4, "f": 5, "f#": 6, "gb": 6, "g": 7, "g#": 8,
        "ab": 8, "a": 9, "a#": 10, "bb": 10, "b": 11}


def midi(name):
    return 12 * (int(name[-1]) + 1) + NOTE[name[:-1]]


def hz(m):
    return 440.0 * 2 ** ((m - 69) / 12)


# ------------------------------------------------------------------ Instrumentos (devuelven una lista de muestras)

def pluck(f, length, bright=0.5, seed=0):
    """Karplus-Strong: un ruido corto que recorre una línea de retardo y se va suavizando: suena a cuerda."""
    rnd = random.Random(seed)
    period = max(2, int(RATE / f))
    line = [rnd.uniform(-1, 1) for _ in range(period)]
    # Arranque más redondo (menos "chirrido").
    for _ in range(int(3 - 2 * bright)):
        line = [(line[i] + line[i - 1]) * 0.5 for i in range(period)]
    n = int(length * RATE)
    out = []
    decay = 0.996 - 0.004 * (1 - bright)
    i = 0
    for _ in range(n):
        a = line[i]
        b = line[(i + 1) % period]
        line[i] = (a + b) * 0.5 * decay
        out.append(a)
        i = (i + 1) % period
    fade = int(0.03 * RATE)
    for k in range(min(fade, n)):
        out[n - 1 - k] *= k / fade
    return out


def epiano(f, length):
    n = int(length * RATE)
    out = []
    for i in range(n):
        t = i / RATE
        env = math.exp(-t * 2.2) * min(1.0, t / 0.008)
        trem = 1 + 0.06 * math.sin(2 * math.pi * 4.5 * t)
        v = math.sin(2 * math.pi * f * t) + 0.25 * math.exp(-t * 6) * math.sin(2 * math.pi * 2 * f * t)
        out.append(v * env * trem)
    _tail(out, 0.06)
    return out


def flute(f, length):
    n = int(length * RATE)
    rnd = random.Random(int(f))
    out = []
    for i in range(n):
        t = i / RATE
        env = min(1.0, t / 0.09) * (1 - 0.15 * min(1.0, t / 1.5))
        vib = 1 + 0.006 * math.sin(2 * math.pi * 5.0 * t) * min(1.0, t / 0.4)
        ph = 2 * math.pi * f * vib * t
        v = math.sin(ph) + 0.12 * math.sin(2 * ph) + 0.03 * rnd.uniform(-1, 1) * math.exp(-t * 20)
        out.append(v * env)
    _tail(out, 0.12)
    return out


def bell(f, length):
    n = int(length * RATE)
    out = []
    for i in range(n):
        t = i / RATE
        env = math.exp(-t * 2.8) * min(1.0, t / 0.003)
        v = math.sin(2 * math.pi * f * t) + 0.35 * math.exp(-t * 5) * math.sin(2 * math.pi * 2.76 * f * t) \
            + 0.15 * math.exp(-t * 9) * math.sin(2 * math.pi * 5.4 * f * t)
        out.append(v * env)
    _tail(out, 0.05)
    return out


def pad(freqs, length):
    """Colchón: cada nota con dos voces apenas desafinadas (triángulo suavizado), entrada y salida lentas."""
    n = int(length * RATE)
    out = [0.0] * n
    for f in freqs:
        for detune in (-0.004, 0.004):
            ff = f * (1 + detune)
            for i in range(n):
                t = i / RATE
                ph = (ff * t) % 1.0
                tri = 4 * abs(ph - 0.5) - 1
                out[i] += 0.6 * tri + 0.4 * math.sin(2 * math.pi * ff * t)
    attack, release = int(0.7 * RATE), int(0.9 * RATE)
    for i in range(n):
        env = min(1.0, i / attack) * min(1.0, (n - i) / release)
        out[i] *= env / max(1, len(freqs))
    return out


def _tail(out, seconds):
    k = int(seconds * RATE)
    n = len(out)
    for i in range(min(k, n)):
        out[n - 1 - i] *= i / k


INSTRUMENTS = {"pluck": pluck, "epiano": epiano, "flute": flute, "bell": bell}


# ------------------------------------------------------------------ Acordes

SHAPES = {"maj7": [0, 4, 7, 11], "m7": [0, 3, 7, 10], "7": [0, 4, 7, 10], "6": [0, 4, 7, 9], "m": [0, 3, 7],
          "": [0, 4, 7], "sus2": [0, 2, 7], "sus4": [0, 5, 7], "add9": [0, 4, 7, 14], "m9": [0, 3, 7, 10, 14],
          "m6": [0, 3, 7, 9]}


def chord(name):
    """'F#m7' → (fundamental midi en octava 3, intervalos)."""
    root = name[:2] if len(name) > 1 and name[1] in "#b" else name[:1]
    rest = name[len(root):]
    return midi(root.lower() + "3"), SHAPES[rest]


# ------------------------------------------------------------------ Efectos de mezcla

def lowpass(buf, cutoff):
    a = 1 - math.exp(-2 * math.pi * cutoff / RATE)
    y = 0.0
    out = []
    for x in buf:
        y += a * (x - y)
        out.append(y)
    return out


def reverb(buf, mix=0.28, room=0.82):
    """Schroeder: 4 filtros peine en paralelo + 2 pasa-todo."""
    combs = [1116, 1188, 1277, 1356]
    combs = [int(c * RATE / 44100) for c in combs]
    wet = [0.0] * len(buf)
    for d in combs:
        line = [0.0] * d
        idx = 0
        damp = 0.0
        for i, x in enumerate(buf):
            y = line[idx]
            damp = y * 0.6 + damp * 0.4
            line[idx] = x + damp * room
            idx = (idx + 1) % d
            wet[i] += y * 0.25
    for d in (int(556 * RATE / 44100), int(225 * RATE / 44100)):
        line = [0.0] * d
        idx = 0
        for i in range(len(wet)):
            b = line[idx]
            y = -wet[i] + b
            line[idx] = wet[i] + b * 0.5
            idx = (idx + 1) % d
            wet[i] = y
    return [x * (1 - mix) + w * mix for x, w in zip(buf, wet)]


# ------------------------------------------------------------------ Armado de una canción

def place(buf, samples, start, gain):
    for i, s in enumerate(samples):
        j = start + i
        if j >= len(buf):
            break
        buf[j] += s * gain


def song(bpm, chords, melody, lead="flute", lead_gain=0.32, pad_gain=0.22, arp="up", arp_gain=0.16,
         arp_octave=0, bass_gain=0.22, beat=None, beat_gain=0.10, brightness=3200):
    sixteenth = 60.0 / bpm / 4
    bar = 16 * sixteenth
    bars = len(chords)
    loop_len = int(bars * bar * RATE)
    buf = [0.0] * (loop_len * 2)

    for rep in range(2):
        base = rep * loop_len
        for b, name in enumerate(chords):
            root, shape = chord(name)
            start = base + int(b * bar * RATE)
            # Colchón de acordes (todo el compás, una octava arriba del bajo).
            if pad_gain:
                place(buf, pad([hz(root + 12 + s) for s in shape[:4]], bar * 1.05), start, pad_gain)
            # Bajo punteado: fundamental en el 1 y la quinta en el 3.
            if bass_gain:
                place(buf, pluck(hz(root - 12), bar * 0.5, bright=0.2, seed=b), start, bass_gain)
                place(buf, pluck(hz(root - 5), bar * 0.5, bright=0.2, seed=b + 50), start + int(bar * 0.5 * RATE), bass_gain * 0.8)
            # Arpegio de arpa en corcheas (sube y baja por el acorde).
            if arp:
                tones = [root + 12 + arp_octave * 12 + s for s in shape] + [root + 24 + arp_octave * 12 + shape[0]]
                order = tones + tones[-2:0:-1] if arp == "updown" else tones + tones
                for k in range(8):
                    m = order[k % len(order)]
                    place(buf, pluck(hz(m), 1.4, bright=0.6, seed=b * 8 + k),
                          start + int(k * 2 * sixteenth * RATE), arp_gain)
            # Pulso suave (solo en las carreras): bombo blandito y un shaker.
            if beat:
                rnd = random.Random(b)
                for s in range(16):
                    hit = beat.get(s)
                    if not hit:
                        continue
                    st = start + int(s * sixteenth * RATE)
                    if hit == "k":
                        n = int(0.18 * RATE)
                        kick = [math.sin(2 * math.pi * (55 + 90 * math.exp(-i / RATE * 30)) * i / RATE) * math.exp(-i / RATE * 14)
                                for i in range(n)]
                        place(buf, kick, st, beat_gain * 1.6)
                    else:
                        n = int(0.05 * RATE)
                        shk = lowpass([rnd.uniform(-1, 1) * math.exp(-i / RATE * 60) for i in range(n)], 5000)
                        place(buf, shk, st, beat_gain)
        # Melodía.
        pos = base
        play = INSTRUMENTS[lead]
        for token in melody.split():
            name, units = token.split(":")
            length = int(units) * sixteenth
            if name != "r":
                place(buf, play(hz(midi(name)), length + (0.4 if lead in ("bell", "epiano") else 0.05)), pos, lead_gain)
            pos += int(length * RATE)

    buf = lowpass(buf, brightness)
    buf = reverb(buf)
    out = buf[loop_len:]  # la segunda vuelta ya trae la cola de la reverberación de la primera
    # Unión del loop sin "clic": los últimos 30 ms se funden con lo que en el render continuo viene justo antes
    # del comienzo de la segunda vuelta; así el final empalma exacto con el arranque.
    k = int(0.03 * RATE)
    for i in range(k):
        a = (i + 1) / k
        out[-k + i] = out[-k + i] * (1 - a) + buf[loop_len - k + i] * a
    return out


SOFT_BEAT = {0: "k", 4: "s", 6: "s", 8: "k", 12: "s", 14: "s"}
WALK_BEAT = {0: "k", 8: "s", 12: "s"}

MUSIC = {
    # Refugio: casa, tarde tranquila. Flauta lenta sobre arpa y colchón.
    "refugio": lambda: song(68, ["Fmaj7", "Am7", "Bbmaj7", "C6", "Dm7", "Am7", "Gm7", "Csus4"],
                            "a5:8 c6:4 a5:4  g5:12 e5:4  f5:8 d5:4 f5:4  e5:16 "
                            "d5:4 f5:4 a5:8  c6:8 e5:8  f5:4 g5:4 a5:4 d6:4  c6:16",
                            lead="flute", arp="updown", brightness=2600),
    # Pueblo Puerto: soleado y amable. Piano eléctrico y arpa.
    "pueblo": lambda: song(86, ["Cmaj7", "Am7", "Dm7", "G7", "Em7", "Am7", "Fmaj7", "G6"],
                           "e5:4 g5:4 b5:4 g5:4  c6:8 a5:8  f5:4 a5:4 c6:4 a5:4  b5:8 g5:8 "
                           "g5:4 b5:4 d6:4 b5:4  c6:4 e6:4 a5:8  a5:4 c6:4 e6:4 c6:4  d6:12 r:4",
                           lead="epiano", lead_gain=0.30, arp="up", brightness=3000),
    # Rutas y zonas: paseo al aire libre, con un pulso muy suave.
    "ruta": lambda: song(92, ["Gmaj7", "D", "Em7", "Cmaj7", "Am7", "D", "Cmaj7", "Dsus2"],
                         "b5:8 d6:4 b5:4  a5:8 f#5:8  g5:4 b5:4 e6:8  e6:4 d6:4 b5:8 "
                         "c6:4 e6:4 a5:8  f#5:8 a5:8  g5:4 b5:4 e6:4 d6:4  d6:16",
                         lead="flute", arp="updown", beat=WALK_BEAT, beat_gain=0.05, brightness=3000),
    # La cajita de música del abuelo (título y momentos emotivos).
    "cajita": lambda: song(72, ["Emaj7", "C#m7", "Amaj7", "B6", "Emaj7", "G#m7", "Amaj7", "Bsus4"],
                           "g#5:4 b5:4 e6:4 d#6:4  c#6:4 b5:8 g#5:4  a5:4 c#6:4 e6:4 g#6:4  f#6:12 r:4 "
                           "e6:4 g#6:4 b6:4 g#6:4  f#6:4 e6:4 d#6:4 b5:4  c#6:4 e6:4 d#6:4 f#6:4  e6:16",
                           lead="bell", lead_gain=0.30, arp=None, bass_gain=0.12, pad_gain=0.20, brightness=3600),
    # Ápice: misterio en calma (no da miedo: inquieta).
    "apice": lambda: song(60, ["Dm9", "Bbmaj7", "Gm7", "A7", "Dm9", "Bbmaj7", "Gm6", "Asus4"],
                          "a4:8 f4:8  d5:12 r:4  d5:4 c5:4 a#4:8  a4:16 "
                          "f5:8 e5:8  d5:12 r:4  g4:4 a#4:4 d5:8  c#5:16",
                          lead="flute", lead_gain=0.28, arp="up", arp_gain=0.10, arp_octave=-1, brightness=1800),
    # Carrera: con ritmo, pero con sonidos suaves.
    "carrera": lambda: song(126, ["Am7", "Fmaj7", "Cmaj7", "G", "Am7", "Fmaj7", "G", "Esus4"],
                            "a5:2 c6:2 e6:4 a5:2 c6:2 e6:4  f6:4 e6:2 d6:2 c6:4 a5:4  g5:2 c6:2 e6:4 g6:4 e6:4  d6:4 b5:4 g5:8 "
                            "a5:2 c6:2 e6:4 a6:4 g6:4  f6:4 e6:2 f6:2 a6:8  g6:4 f6:4 e6:4 d6:4  e6:8 b5:8",
                            lead="epiano", lead_gain=0.28, arp="up", beat=SOFT_BEAT, beat_gain=0.09, brightness=3800),
    # Copa: luminosa y emocionante, sin ser estridente.
    "copa": lambda: song(108, ["Dmaj7", "Gmaj7", "A", "Dmaj7", "Bm7", "Gmaj7", "A", "Dmaj7"],
                         "d5:4 f#5:2 a5:2 d6:8  b5:4 d6:4 g6:8  a5:2 a5:2 c#6:4 e6:4 a6:4  f#6:8 d6:8 "
                         "f#6:4 e6:4 d6:4 b5:4  g6:4 f#6:4 e6:4 d6:4  e6:4 a6:4 g6:4 e6:4  d6:16",
                         lead="flute", lead_gain=0.30, arp="updown", beat=WALK_BEAT, beat_gain=0.08, brightness=3600),
}
