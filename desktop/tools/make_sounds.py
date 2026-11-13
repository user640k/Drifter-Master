#!/usr/bin/env python3
"""Builds the desktop version's sounds into assets/snd (24 kHz mono WAV).
   usage: make_sounds.py <out dir>

   The running sounds are cut from a reference recording (tools/sound_src/reference.wav: the sound of the owner's video of
   two 1/10 drift cars on a P-tile track; for non-commercial use):
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
# The tyres: a soft hiss with no pitch, the main sound of the car. It is made here from plain noise given the shape of
# the recording's own spectrum, so it has the recording's tone and none of its events: no motor notes (the narrow
# lines are smoothed out of the shape), no clicks, nothing that can be heard coming round again.
#   tyre: the tone of the cars heard across the hall (mostly 600 Hz to 1.5 kHz)
#   air:  what is added when a car is close: the top of the hiss, above 3 kHz, which the distance takes away
from scipy.signal import welch
def shape(t0, t1):
    f, P = welch(ref[int(t0 * SR):int(t1 * SR)], SR, nperseg=4096); P = median_filter(P, 81, mode='nearest'); return f, uniform_filter1d(P, 25)
def hiss(f, P, secs=6.0):
    n = int(secs * SR); X = np.fft.rfft(rng.normal(0, 1, n)); fx = np.fft.rfftfreq(n, 1 / SR); X *= np.sqrt(np.interp(fx, f, P)); X[fx < 120] = 0; return np.fft.irfft(X, n)   # a whole number of periods: it loops as it is
fF, PF = shape(0.0, 6.5); fN, PN = shape(7.0, 8.3)                # far running; the close pass (the clip's 8.5 s sound is a phone, left out)
save('tyre', hiss(fF, PF)); save('air', band(hiss(fN, PN), 3000, None))

# the motor's whistle: three seconds at a nominal 1 kHz; the game sets its pitch from the motor's speed. A real motor is not a
# pure tone: its note wavers a little, each harmonic swells and fades on its own, the rotor's once-a-turn unevenness
# beats through it (a sixth of the note), and the switching adds a hiss that rides on the note
n = 3 * SR; t = np.arange(n) / SR
def wander(rate, depth):   # a slow random drift, the same at both ends so the loop closes
    k = max(2, int(n / SR * rate)); pts = rng.normal(0, 1, k); pts = np.append(pts, pts[0]); return depth * np.interp(t, np.linspace(0, n / SR, k + 1), pts)
ph = 2 * np.pi * 1000 * t + 2 * np.pi * np.cumsum(wander(5, 4.0) + wander(23, 2.5)) / SR; ph -= (ph[-1] - 2 * np.pi * round(ph[-1] / (2 * np.pi))) * t / t[-1]   # a few Hz of waver; whole turns, so it loops
wh = sum(a * (1 + wander(r, 0.22)) * np.sin(h * ph + o) for h, a, r, o in ((1, 1.0, 3, 0), (2, 0.3, 4, 1.1), (3, 0.5, 5, 2.3), (4, 0.14, 6, 0.4), (6, 0.08, 7, 1.9)))
wh *= 1 + 0.1 * np.sin(ph / 6) + 0.05 * np.sin(ph / 3 + 0.7)                                   # once and twice a turn of the rotor
hash_ = band(rng.normal(0, 1, n), 600, 5000) * (0.5 + 0.5 * np.sin(ph)) ** 2; wh += 0.22 * hash_ / (np.abs(hash_).max() + 1e-9) * np.abs(wh).max()
save('whine', wh, 0.5)
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
