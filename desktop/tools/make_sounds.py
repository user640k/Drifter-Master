#!/usr/bin/env python3
"""Builds the desktop version's sounds into assets/snd (24 kHz mono WAV).
   usage: make_sounds.py <out dir>

   The running sounds are matched to a reference video of a 1/10 drift car sliding on a polished floor in a big room.
   Nothing of the recording itself is used, only what was measured from it (tools/sound_src/ref_*.npy):
     - its average spectrum while the car is loud (sliding) and while it is quiet (rolling, coasting): a broad roar
       centred near 500 Hz with lesser humps near 1.7, 2.3 and 3.9 kHz, almost nothing under 150 Hz or over 6 kHz
     - how its loudness flutters: about 17% from one hundredth of a second to the next, part slow, part fast
     - the motor: a thin whistle gliding between about 0.9 and 2.4 kHz with wheel speed (whine.wav, pitched in the game)
   Noise is shaped to those spectra and given that flutter. The shell sounds are made here too: a thin polycarbonate
   body is a drum, with a few broad resonances; it buzzes on its posts, and the hard tyres click over tile joints."""
import sys, os, wave, numpy as np
from scipy.signal import butter, sosfilt, lfilter
from scipy.ndimage import uniform_filter1d
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

ref_f = np.load(os.path.join(here, 'sound_src', 'ref_freq.npy'))
def shaped(env_name, secs=4.0, lo=0, hi=1e9, flutter=0.17):
    env = np.load(os.path.join(here, 'sound_src', env_name)); n = int(secs * SR)
    X = np.fft.rfft(rng.normal(0, 1, n)); f = np.fft.rfftfreq(n, 1 / SR)
    g = np.interp(f, ref_f, env); g[(f < max(lo, 60)) | (f > hi)] = 0            # the measured spectrum, laid on noise
    y = np.fft.irfft(X * g, n)                                                    # periodic: loops without a seam
    # flutter: the tyres letting go and biting. A slow part (1-4 Hz) and a quick part (16-50 Hz), wrapped so it loops too
    def wander(f0, f1):
        M = np.fft.rfft(rng.normal(0, 1, n)); mf = np.fft.rfftfreq(n, 1 / SR); M[(mf < f0) | (mf > f1)] = 0; m = np.fft.irfft(M, n); return m / m.std()
    mod = 1 + flutter * (0.62 * wander(1, 4) + 0.5 * wander(8, 16) + 0.6 * wander(16, 50))
    return y * np.clip(mod, 0.3, 2)
save('roar', shaped('ref_loud.npy'))                         # sliding: the whole loud spectrum
save('roll', shaped('ref_quiet.npy', flutter=0.1))           # rolling and coasting
save('hiss', shaped('ref_loud.npy', lo=2000, flutter=0.22))  # wheelspin: the top of it
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
