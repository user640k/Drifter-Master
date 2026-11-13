#!/usr/bin/env python3
"""Builds the desktop version's sounds into assets/snd (24 kHz mono WAV).
   usage: make_sounds.py <out dir>

   The running sounds are cut from a reference recording (tools/sound_src/reference.wav: the sound of the owner's video of
   1/10 drift cars on a P-tile track, the floor the game has; for non-commercial use):
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
out = sys.argv[1]; here = os.path.dirname(os.path.abspath(__file__)); SR = 44100
rng = np.random.default_rng(11)
def save(name, a, peak=0.7):
    a = a / (np.abs(a).max() + 1e-9) * peak
    w = wave.open(os.path.join(out, name + '.wav'), 'wb'); w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((a * 32767).astype(np.int16).tobytes()); w.close()
def band(a, lo, hi, order=2):
    if lo and hi: sos = butter(order, [lo, hi], 'bandpass', fs=SR, output='sos')
    elif lo: sos = butter(order, lo, 'highpass', fs=SR, output='sos')
    else: sos = butter(order, hi, 'lowpass', fs=SR, output='sos')
    return sosfilt(sos, a)

w = wave.open(os.path.join(here, 'sound_src', 'reference.wav')); assert w.getframerate() == SR and w.getnchannels() == 1
ref = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float64) / 32768
# The running sounds are long, unbroken stretches of the recording, played as they are: nothing is cut into grains and
# nothing is filtered out, so the tyres' own texture and the motor's own note stay exactly as recorded. (Cutting it
# into shuffled pieces and removing the motor's lines measured the same but sounded rough.) Only the slow swell of the
# car coming and going is levelled, as the game sets the loudness by distance itself, and the ends are cross-faded.
def stretch(t0, t1, xf=0.6):
    seg = ref[int(t0 * SR):int(t1 * SR)]; env = np.sqrt(uniform_filter1d(seg ** 2, int(0.35 * SR))) + 1e-5; seg = seg / env
    k = int(xf * SR); n = len(seg) - k; th = np.linspace(0, np.pi / 2, k); out = seg[:n].copy(); out[:k] = seg[:k] * np.sin(th) + seg[n:n + k] * np.cos(th); return out
roar = stretch(5.7, 9.3)
save('roar', roar)                                           # a car passing close, sliding
save('roll', stretch(10.2, 15.2))                            # steady running further off
save('hiss', band(roar, 2000, None))                         # wheelspin: the top of the slide

# the motor's whistle: one second at 1 kHz with a little second harmonic and a slight waver; the game sets its pitch
t = np.arange(SR) / SR; wav = np.cumsum(rng.normal(0, 1, SR)); wav = uniform_filter1d(wav - np.linspace(wav[0], wav[-1], SR), 400); wav = wav / (np.abs(wav).max() + 1e-9)
ph = 2 * np.pi * 1000 * t + 0.6 * wav
save('whine', np.sin(ph) + 0.3 * np.sin(2 * ph) + 0.55 * np.sin(3 * ph) + 0.12 * np.sin(4 * ph), 0.5)  # the third harmonic is the high whistle

# ---- the shell: resonances of a 1/10 polycarbonate body (hollow, papery, a little boxy)
MODES = [(172, 9), (248, 11), (395, 12), (610, 14), (905, 12), (1340, 10), (2100, 8)]
def resonate(x, modes=MODES, weights=None):
    y = np.zeros_like(x)
    for i, (f0, q) in enumerate(modes):
        r = np.exp(-np.pi * f0 / q / SR); b = [1 - r]; a_ = [1, -2 * r * np.cos(2 * np.pi * f0 / SR), r * r]
        y += lfilter(b, a_, x) * (weights[i] if weights else 1 / (1 + i * 0.25))
    return y
# buzz: the body shivering on the chassis: noise whose loudness wanders, plus small sharp knocks against the posts
n = 4 * SR; x = rng.normal(0, 1, n)
env = np.interp(np.arange(n), np.linspace(0, n, 14), rng.uniform(0.75, 1, 14)); x *= env
for _ in range(24):      # a few soft knocks against the posts (it was a constant rattle, far rougher than a real car sounds from the stand)
    p = rng.integers(0, n - 400); x[p:p + 60] += rng.normal(0, 1, 60) * rng.uniform(1.5, 4) * np.hanning(60)
sh = resonate(band(x, 80, 4000)); sh = np.concatenate([sh[SR // 2:], sh[:SR // 2]])
fade = 2400; sh[:fade] = sh[:fade] * np.linspace(0, 1, fade) + sh[-fade:] * np.linspace(1, 0, fade); save('shell', sh[:-fade])

# tick: a hard tyre crossing a tile joint: a tiny click, and the body answering it
def knock(dur, click_hz, body, decay):
    m = int(dur * SR); t = np.arange(m) / SR; x = np.zeros(m); x[:24] = rng.normal(0, 1, 24) * np.hanning(24)
    y = band(x, click_hz, None) * 0.9 + resonate(x) * body
    return y * np.exp(-t / decay)
save('tick', knock(0.09, 2500, 2.2, 0.022), 0.9)
# hit: the car into foam or a board: a dull thud from the barrier, the shell clacking and ringing on
m = int(0.45 * SR); t = np.arange(m) / SR
thud = np.sin(2 * np.pi * (150 * np.exp(-t * 9) + 55) * t) * np.exp(-t / 0.06)
x = np.zeros(m); x[:90] = rng.normal(0, 1, 90) * np.hanning(90)
clack = resonate(x, weights=[1, 1, 0.9, 0.8, 0.7, 0.5, 0.3]) * np.exp(-t / 0.07) + band(x, 1500, 6000) * 0.5 * np.exp(-t / 0.01)
save('hit', thud * 0.9 + clack / (np.abs(clack).max() + 1e-9) * 0.8)
print('ok')
