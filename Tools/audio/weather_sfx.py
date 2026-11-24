# Yağış səsi (dövr edən) və göy gurultusu.
import numpy as np, wave
SR = 22050
rng = np.random.default_rng(11)

def write(name, x, gain=0.8):
    x = x / (np.max(np.abs(x)) + 1e-9) * gain
    d = (x * 32767).astype(np.int16)
    with wave.open(name, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(d.tobytes())
    print(name, len(d) / SR)

def lowpass(x, a):
    y = np.zeros_like(x); acc = 0.0
    for i in range(len(x)):
        acc += a * (x[i] - acc); y[i] = acc
    return y

# yağış: yumşaq küy + damcı tıqqıltıları, 8 saniyə, tikişsiz
n = SR * 8
noise = rng.uniform(-1, 1, n)
hiss = noise - lowpass(noise, 0.05)          # yüksək tezlikli xışıltı
body = lowpass(noise, 0.12) * 0.6
rain = hiss * 0.35 + body * 0.5
for _ in range(900):
    p = rng.integers(0, n - 400)
    L = rng.integers(60, 300)
    t = np.arange(L) / SR
    f = rng.uniform(1800, 5200)
    rain[p:p + L] += np.sin(2 * np.pi * f * t) * np.exp(-t * 90) * rng.uniform(0.05, 0.25)
fade = 2000
rain[:fade] = rain[:fade] * np.linspace(0, 1, fade) + rain[-fade:] * np.linspace(1, 0, fade)
rain = rain[:-fade]
write('rain_loop.wav', rain, 0.55)

# göy gurultusu: aşağı tezlikli gurultu, 3.5 saniyə
n = int(SR * 3.5)
t = np.arange(n) / SR
nz = rng.uniform(-1, 1, n)
rumble = lowpass(lowpass(nz, 0.02), 0.03)
env = np.exp(-t * 0.9) * (1 - np.exp(-t * 25))
crack = (nz - lowpass(nz, 0.2)) * np.exp(-t * 14) * 0.25
thunder = rumble * env * 6 + crack
for k in range(3):
    s = int(rng.uniform(0.2, 1.6) * SR)
    thunder[s:] += rumble[:n - s] * env[:n - s] * 3
write('thunder.wav', thunder, 0.9)
