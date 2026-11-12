#!/usr/bin/env python3
"""Builds the desktop version's sounds into assets/snd (24 kHz mono WAV).
   usage: make_sounds.py <out dir>

   The running sounds are cut from a reference recording (tools/sound_src/reference.wav: the sound of a video of a
   1/10 drift car sliding on a polished floor in a big room; used with permission for non-commercial use):
     - the recording is split by loudness: its loud quarter is the car sliding, its quiet quarter rolling and coasting
     - the motor's whistle glides up and down in it, which would chirp at random once cut up, so the narrow lines that
       stand well above the noise are taken out of each instant; the game adds its own whistle, pitched by wheel speed
     - each kind is re-laid as a 5 second texture of overlapping grains, which loops without a seam
   The recording carries its own room, so the game adds very little echo on top.
   The shell sounds are made here: a thin polycarbonate body is a drum, with a few broad resonances; it buzzes on its
   posts, and the hard tyres click over tile joints."""
import sys, os, wave, numpy as np
from scipy.signal import stft, istft, butter, sosfilt, lfilter
from scipy.ndimage import median_filter, uniform_filter1d
out = sys.argv[1]; here = os.path.dirname(os.path.abspath(__file__)); SR = 24000
rng = np.random.default_rng(11)
def save(name, a, peak=0.7):
    a = a / (np.abs(a).max() + 1e-9) * peak
    w = wave.open(os.path.join(out, name + '.wav'), 'wb'); w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((a * 32767).astype(np.int16).tobytes()); w.close()
def band(a, lo, hi, order=2):
    if lo and hi: sos = butter(order, [lo, hi], 'bandpass', fs=SR, output='sos')
    elif lo: sos = butter(order, lo, 'highpass', fs=SR, output='sos')
    else: sos = butter(order, hi, 'lowpass', fs=SR, output='sos')
    return sosfilt(sos, a)

w = wave.open(os.path.join(here, 'sound_src', 'reference.wav')); assert w.getframerate() == SR
ref = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float64) / 32768
# take the motor's lines out, instant by instant
f, t, Z = stft(ref, SR, nperseg=1024, noverlap=768); mag = np.abs(Z)
floor = median_filter(uniform_filter1d(mag, 5, axis=1), (31, 1), mode='nearest')
gain = np.minimum(1, 2.0 * floor / (mag + 1e-9)); gain[f < 700] = 1                     # only lines well above the noise, and only where the motor sings
_, clean = istft(Z * gain, SR, nperseg=1024, noverlap=768); clean = clean[:len(ref)]
# loudness, a tenth of a second at a time
hop = SR // 10; lv = np.sqrt((clean[:len(clean) // hop * hop].reshape(-1, hop) ** 2).mean(1)); q1, q3 = np.percentile(lv, [30, 70])
def texture(pick, secs=5.0, grain=0.2):
    starts = [i * hop for i in range(len(lv) - 3) if all(pick(lv[i + k]) for k in range(3))]   # places where 0.3 s in a row is of this kind
    n = int(secs * SR); g = int(grain * SR); win = np.hanning(g); y = np.zeros(n + g); ws = np.zeros(n + g)
    for pos in range(0, n, g // 4):
        s0 = starts[rng.integers(0, len(starts))] + rng.integers(0, hop); seg = clean[s0:s0 + g]
        seg = seg / (np.sqrt((seg ** 2).mean()) + 1e-9)                                  # even loudness: the game sets the level
        y[pos:pos + g] += seg * win; ws[pos:pos + g] += win
    y[:g] += y[n:n + g]; ws[:g] += ws[n:n + g]; return y[:n] / np.maximum(ws[:n], 1e-3)
roar = texture(lambda v: v >= q3)
save('roar', roar)                                           # sliding
save('roll', texture(lambda v: v <= q1))                     # rolling and coasting
save('hiss', band(roar, 2000, None))                         # wheelspin: the top of the slide

# the motor's whistle: one second at 1 kHz with a little second harmonic and a slight waver; the game sets its pitch
t = np.arange(SR) / SR; wav = np.cumsum(rng.normal(0, 1, SR)); wav = uniform_filter1d(wav - np.linspace(wav[0], wav[-1], SR), 400); wav = wav / (np.abs(wav).max() + 1e-9)
ph = 2 * np.pi * 1000 * t + 0.6 * wav
save('whine', np.sin(ph) + 0.28 * np.sin(2 * ph) + 0.1 * np.sin(3 * ph), 0.5)

# ---- the shell: resonances of a 1/10 polycarbonate body (hollow, papery, a little boxy)
MODES = [(172, 9), (248, 11), (395, 12), (610, 14), (905, 12), (1340, 10), (2100, 8)]
def resonate(x, modes=MODES, weights=None):
    y = np.zeros_like(x)
    for i, (f0, q) in enumerate(modes):
        r = np.exp(-np.pi * f0 / q / SR); b = [1 - r]; a_ = [1, -2 * r * np.cos(2 * np.pi * f0 / SR), r * r]
        y += lfilter(b, a_, x) * (weights[i] if weights else 1 / (1 + i * 0.25))
    return y
# buzz: the body shivering on the chassis: noise whose loudness wanders, plus small sharp knocks against the posts
n = 4 * SR; x = rng.normal(0, 1, n) * (0.35 + 0.65 * np.clip(np.cumsum(rng.normal(0, 1, n)) % 3000 / 3000, 0, 1) ** 0.3)
env = np.interp(np.arange(n), np.linspace(0, n, 90), rng.uniform(0.25, 1, 90)); x *= env
for _ in range(110):
    p = rng.integers(0, n - 400); x[p:p + 40] += rng.normal(0, 1, 40) * rng.uniform(3, 14) * np.hanning(40)
sh = resonate(band(x, 80, 4000)); sh = np.concatenate([sh[SR // 2:], sh[:SR // 2]])
fade = 2400; sh[:fade] = sh[:fade] * np.linspace(0, 1, fade) + sh[-fade:] * np.linspace(1, 0, fade); save('shell', sh[:-fade])

# tick: a hard tyre crossing a tile joint: a tiny click, and the body answering it
def knock(dur, click_hz, body, decay):
    m = int(dur * SR); t = np.arange(m) / SR; x = np.zeros(m); x[:24] = rng.normal(0, 1, 24) * np.hanning(24)
    y = band(x, click_hz, None) * 0.9 + resonate(x) * body
    return y * np.exp(-t / decay)
save('tick', knock(0.11, 2500, 4.0, 0.035), 0.9)
# hit: the car into foam or a board: a dull thud from the barrier, the shell clacking and ringing on
m = int(0.45 * SR); t = np.arange(m) / SR
thud = np.sin(2 * np.pi * (150 * np.exp(-t * 9) + 55) * t) * np.exp(-t / 0.06)
x = np.zeros(m); x[:90] = rng.normal(0, 1, 90) * np.hanning(90)
clack = resonate(x, weights=[1, 1, 0.9, 0.8, 0.7, 0.5, 0.3]) * np.exp(-t / 0.07) + band(x, 1500, 6000) * 0.5 * np.exp(-t / 0.01)
save('hit', thud * 0.9 + clack / (np.abs(clack).max() + 1e-9) * 0.8)
print('ok')
