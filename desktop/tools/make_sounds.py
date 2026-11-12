#!/usr/bin/env python3
"""Builds the desktop version's sounds into assets/snd (24 kHz mono WAV).
   usage: make_sounds.py <out dir>

   The running sounds start from the web version's first recorded loops (tools/sound_src: a real RC drift car on a hard
   floor, driving steadily, drifting, and the tyre-scrub band), with the motor taken out:
     - the motor's whine is the narrow peaks in the spectrum; each peak is cut down to the level of the noise around it
     - what is left (tyres, drivetrain rumble, the floor) is re-laid as a 4 second texture of overlapping grains, so
       there is no 1.3 s loop to hear and nothing that rises and falls in pitch
   The shell sounds are made here: a thin polycarbonate body is a drum. It has a few broad resonances and it buzzes and
   ticks against its posts when the chassis shakes; the hard tyres click over every tile joint."""
import sys, os, wave, numpy as np
from scipy.signal import stft, istft, butter, sosfilt, lfilter
from scipy.ndimage import median_filter
out = sys.argv[1]; here = os.path.dirname(os.path.abspath(__file__)); SR = 24000
rng = np.random.default_rng(11)
def load(name):
    w = wave.open(os.path.join(here, 'sound_src', name + '.wav')); a = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float32) / 32768
    assert w.getframerate() == SR; return a
def save(name, a, peak=0.7):
    a = a / (np.abs(a).max() + 1e-9) * peak
    w = wave.open(os.path.join(out, name + '.wav'), 'wb'); w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes((a * 32767).astype(np.int16).tobytes()); w.close()
def band(a, lo, hi, order=2):
    if lo and hi: sos = butter(order, [lo, hi], 'bandpass', fs=SR, output='sos')
    elif lo: sos = butter(order, lo, 'highpass', fs=SR, output='sos')
    else: sos = butter(order, hi, 'lowpass', fs=SR, output='sos')
    return sosfilt(sos, a)

def dewhine(a):
    f, t, Z = stft(a, SR, nperseg=1024, noverlap=768); mag = np.abs(Z)
    avg = mag.mean(axis=1); floor = median_filter(avg, 41, mode='nearest')          # the noise level round each frequency
    gain = np.minimum(1, (floor * 1.15) / (avg + 1e-9))                               # peaks standing above it are the motor
    _, y = istft(Z * gain[:, None], SR, nperseg=1024, noverlap=768); return y[:len(a)]
def texture(a, secs=4.0, grain=0.14):
    n = int(secs * SR); g = int(grain * SR); win = np.hanning(g); y = np.zeros(n + g); wsum = np.zeros(n + g)
    for pos in range(0, n, g // 4):
        s = rng.integers(0, len(a) - g); y[pos:pos + g] += a[s:s + g] * win * rng.uniform(0.8, 1.2); wsum[pos:pos + g] += win
    y[:g] += y[n:n + g]; wsum[:g] += wsum[n:n + g]                                   # the tail wraps round: a seamless loop
    return y[:n] / np.maximum(wsum[:n], 1e-3)

save('roll', band(texture(dewhine(load('cruise'))), 90, 5200))
save('slide', band(texture(dewhine(load('drift'))), 120, 7000))
save('scrub', band(texture(dewhine(load('scrub'))), 900, 9000))

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
