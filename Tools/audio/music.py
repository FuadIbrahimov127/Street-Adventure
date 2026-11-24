# Fon musiqisi: tar səsinə bənzər dartımlı simlər (Karplus-Strong), qaval ritmi.
# Gündüz: Rast ladında şən 6/8 melodiya. Gecə: Şur ladında sakit arpecio + cırcırama səsləri.
import numpy as np
import wave

SR = 22050
rng = np.random.default_rng(7)

NOTE = {'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11}


def freq(n):
    """'C5', 'Bb4', 'F#3' -> Hz"""
    name = n[0]
    acc = 0
    rest = n[1:]
    if rest and rest[0] in 'b#':
        acc = -1 if rest[0] == 'b' else 1
        rest = rest[1:]
    octv = int(rest)
    midi = 12 * (octv + 1) + NOTE[name] + acc
    return 440.0 * 2 ** ((midi - 69) / 12)


def pluck(f, dur, bright=0.5, decay=0.996, vol=1.0):
    """Karplus-Strong: tar/saz kimi dartımlı sim"""
    n = int(SR * dur)
    p = max(2, int(SR / f))
    buf = rng.uniform(-1, 1, p)
    # parlaqlıq: başlanğıc səs-küyü bir az hamarlansın
    for _ in range(int((1 - bright) * 3)):
        buf = 0.5 * (buf + np.roll(buf, 1))
    out = np.zeros(n)
    idx = 0
    for i in range(n):
        out[i] = buf[idx]
        nxt = (idx + 1) % p
        buf[idx] = decay * 0.5 * (buf[idx] + buf[nxt])
        idx = nxt
    # yumşaq başlanğıc və son
    out[:40] *= np.linspace(0, 1, 40)
    out[-200:] *= np.linspace(1, 0, 200)
    return out * vol


def tremolo_pluck(f, dur, rate=14, vol=1.0):
    """uzun notlarda tar tremolosu: tez-tez təkrarlanan dartma"""
    n = int(SR * dur)
    out = np.zeros(n)
    step = int(SR / rate)
    k = 0
    for s in range(0, n - step // 2, step):
        seg = pluck(f, min(0.35, (n - s) / SR), bright=0.6, decay=0.994, vol=vol * (0.75 if k % 2 else 1.0))
        e = min(n, s + len(seg))
        out[s:e] += seg[:e - s]
        k += 1
    return out * 0.7


def dum(vol=1.0):
    """qavalın bas zərbəsi"""
    n = int(SR * 0.35)
    t = np.arange(n) / SR
    f = 140 * np.exp(-t * 9) + 55
    ph = 2 * np.pi * np.cumsum(f) / SR
    body = np.sin(ph) * np.exp(-t * 11)
    noise = rng.uniform(-1, 1, n) * np.exp(-t * 60) * 0.25
    return (body + noise) * vol


def tek(vol=1.0):
    """qavalın kənar zərbəsi + zınqırovlar"""
    n = int(SR * 0.22)
    t = np.arange(n) / SR
    noise = rng.uniform(-1, 1, n)
    hp = noise - np.concatenate([[0], noise[:-1]])
    jingle = np.sin(2 * np.pi * 6200 * t) * rng.uniform(0.3, 1, n)
    return (hp * np.exp(-t * 45) * 0.6 + jingle * np.exp(-t * 18) * 0.12) * vol


def add(buf, sig, at):
    s = int(at * SR)
    e = min(len(buf), s + len(sig))
    if s < len(buf):
        buf[s:e] += sig[:e - s]


def reverb(x, mix=0.22):
    """sadə otaq əksi: bir neçə comb + allpass"""
    out = np.copy(x)
    for d, g in ((1557, 0.62), (1617, 0.6), (1491, 0.63), (1422, 0.64)):
        y = np.copy(x)
        for i in range(d, len(y)):
            y[i] += g * y[i - d]
        out += y * (mix / 4)
    return out


def finish_loop(buf, loop_len, name, gain=0.8):
    """quyruğu (əks-səda) başlanğıca qatıb tikişsiz dövr edən fayl yazır"""
    n = int(loop_len * SR)
    main = buf[:n].copy()
    tail = buf[n:]
    main[:len(tail)] += tail[:n]
    peak = np.max(np.abs(main)) + 1e-9
    main = main / peak
    main = np.tanh(2.2 * main) / np.tanh(2.2) * gain   # yumşaq kompressor
    data = (main * 32767).astype(np.int16)
    with wave.open(name, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print(name, f'{loop_len:.1f}s', len(data))


# ---------------- GÜNDÜZ (Rast, 6/8) ----------------
def day_theme():
    e8 = 60 / 288.0              # səkkizlik not (nöqtəli dörddəbir = 96 BPM)
    bar = 6 * e8
    # melodiya: (nota, səkkizlik sayı); '.' = pauza
    A = [('G4', 2), ('A4', 1), ('B4', 2), ('C5', 1), ('D5', 2), ('C5', 1), ('B4', 1), ('A4', 1), ('G4', 1),
         ('A4', 2), ('B4', 1), ('C5', 2), ('D5', 1), ('E5', 6)]
    A2 = [('G4', 2), ('A4', 1), ('B4', 2), ('C5', 1), ('D5', 2), ('E5', 1), ('D5', 1), ('C5', 1), ('B4', 1),
          ('A4', 2), ('G4', 1), ('A4', 2), ('B4', 1), ('G4', 6)]
    Bm = [('E5', 2), ('E5', 1), ('F5', 2), ('E5', 1), ('D5', 2), ('C5', 1), ('D5', 3),
          ('C5', 2), ('C5', 1), ('D5', 2), ('C5', 1), ('B4', 2), ('A4', 1), ('B4', 3)]
    B2 = [('A4', 2), ('B4', 1), ('C5', 2), ('D5', 1), ('E5', 2), ('D5', 1), ('C5', 1), ('B4', 1), ('A4', 1),
          ('B4', 2), ('C5', 1), ('A4', 2), ('B4', 1), ('C5', 6)]
    melody = A + A2 + Bm + B2
    chords = ['C', 'G', 'F', 'C', 'C', 'G', 'F', 'G', 'Am', 'G', 'F', 'E', 'F', 'G', 'Am', 'C']
    roots = {'C': ('C3', 'G2'), 'G': ('G2', 'D3'), 'F': ('F2', 'C3'), 'Am': ('A2', 'E3'), 'E': ('E2', 'B2')}
    arps = {'C': ['C4', 'E4', 'G4'], 'G': ['B3', 'D4', 'G4'], 'F': ['A3', 'C4', 'F4'], 'Am': ['A3', 'C4', 'E4'], 'E': ['G#3', 'B3', 'E4']}
    bars = 32
    total = bars * bar
    buf = np.zeros(int((total + 3) * SR))
    for rep in range(2):
        t = rep * 16 * bar
        for k, (n, d) in enumerate(melody):
            dur = d * e8
            v = 0.55 if rep == 0 else 0.5
            if d >= 3:
                sig = tremolo_pluck(freq(n), dur * 0.95, vol=v)
            else:
                sig = pluck(freq(n), dur + 0.25, bright=0.7, decay=0.995, vol=v)
            add(buf, sig, t)
            # ikinci dəfə: oktava yuxarıda bəzək notu
            if rep == 1 and d == 2 and k % 3 == 0:
                add(buf, pluck(freq(n) * 2, 0.2, vol=0.18), t + e8)
            t += dur
    for b in range(bars):
        ch = chords[b % 16]
        r1, r2 = roots[ch]
        t0 = b * bar
        add(buf, pluck(freq(r1), bar * 0.6, bright=0.3, decay=0.997, vol=0.55), t0)
        add(buf, pluck(freq(r2), bar * 0.5, bright=0.3, decay=0.997, vol=0.4), t0 + 3 * e8)
        # yüngül akkord dartmaları (2-ci və 5-ci səkkizlikdə)
        for j, n in enumerate(arps[ch]):
            add(buf, pluck(freq(n), 0.4, bright=0.4, vol=0.13), t0 + 1 * e8 + j * 0.012)
            add(buf, pluck(freq(n), 0.4, bright=0.4, vol=0.11), t0 + 4 * e8 + j * 0.012)
        # qaval: dum . . tek . tek | dum . tek tek . tek
        pat = [('d', 0), ('t', 3), ('t', 5)] if b % 2 == 0 else [('d', 0), ('t', 2), ('t', 3), ('t', 5)]
        if b >= 4:
            for kind, pos in pat:
                add(buf, dum(0.5) if kind == 'd' else tek(0.32), t0 + pos * e8)
    buf = reverb(buf, 0.25)
    finish_loop(buf, total, 'music_day.wav', 0.75)


# ---------------- GECƏ (Şur, sakit) ----------------
def night_theme():
    q = 60 / 66.0
    bar = 4 * q
    chords = ['Dm', 'Bb', 'C', 'Dm', 'Gm', 'Bb', 'A', 'Dm']
    arp = {'Dm': ['D3', 'A3', 'D4', 'F4', 'A4', 'F4', 'D4', 'A3'],
           'Bb': ['Bb2', 'F3', 'Bb3', 'D4', 'F4', 'D4', 'Bb3', 'F3'],
           'C': ['C3', 'G3', 'C4', 'E4', 'G4', 'E4', 'C4', 'G3'],
           'Gm': ['G2', 'D3', 'G3', 'Bb3', 'D4', 'Bb3', 'G3', 'D3'],
           'A': ['A2', 'E3', 'A3', 'C#4', 'E4', 'C#4', 'A3', 'E3']}
    mel = {1: [('A4', 2), ('G4', 1), ('F4', 1)], 3: [('D5', 3), ('C5', 1)], 5: [('Bb4', 2), ('A4', 2)], 7: [('E4', 2), ('D4', 2)],
           9: [('F4', 2), ('G4', 1), ('A4', 1)], 11: [('C5', 2), ('Bb4', 1), ('A4', 1)], 13: [('G4', 2), ('F4', 2)], 15: [('E4', 1), ('F4', 1), ('D4', 2)]}
    bars = 16
    total = bars * bar
    buf = np.zeros(int((total + 4) * SR))
    for b in range(bars):
        ch = chords[b % 8]
        t0 = b * bar
        for i, n in enumerate(arp[ch]):
            add(buf, pluck(freq(n), 1.2, bright=0.25, decay=0.998, vol=0.32 if i % 4 else 0.42), t0 + i * q / 2)
        # yumşaq fon akkordu (sinus)
        tt = np.arange(int(bar * SR)) / SR
        env = np.sin(np.pi * tt / bar) ** 2
        pad = sum(np.sin(2 * np.pi * freq(n) * tt) for n in arp[ch][1:4]) * env * 0.05
        add(buf, pad, t0)
        if b in mel:
            t = t0
            for n, d in mel[b]:
                add(buf, tremolo_pluck(freq(n), d * q * 0.9, rate=11, vol=0.32) if d >= 2 else pluck(freq(n), d * q + 0.4, vol=0.34), t)
                t += d * q
    # cırcıramalar
    n = len(buf)
    t = np.arange(n) / SR
    cr = np.zeros(n)
    for k in range(3):
        f0 = 4300 + k * 450
        period = 0.9 + k * 0.37
        phase = (t + k * 0.21) % period
        gate = ((phase < 0.16) & ((phase * 60).astype(int) % 2 == 0)).astype(float)
        cr += np.sin(2 * np.pi * f0 * t) * gate * 0.025
    buf += cr
    buf = reverb(buf, 0.35)
    finish_loop(buf, total, 'music_night.wav', 0.6)


if __name__ == '__main__':
    day_theme()
    night_theme()
