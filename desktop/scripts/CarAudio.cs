// The car's sound: the browser version's, carried over as it is. There it is a Web Audio graph; here the same graph is
// computed sample by sample and fed to the sound card.
//
//   motor (synthesised) ─┐
//   recorded loops ──────┼─ car bus ─ air filter (duller with distance) ─ panner (quieter with distance, placed
//   knocks ──────────────┘   left or right) ─ master 0.9 ─ compressor ─ volume
//
// The motor is a sensored brushless motor and its drivetrain:
//   - the commutation whine: a sawtooth and two sines (f, f, 2f) through a resonant peak that follows the pitch
//   - the gear mesh: two sines a little above it (1.62 f), 9 cents apart, so they beat slowly
//   - the drivetrain growl: a low sawtooth under a 260 Hz low-pass, up under load
//   - the ESC's drive tone: a thin 2.6 kHz sine at low speed on part throttle
//   - boost and turbo: past 85 % revs the resonance sharpens, and turbo adds a square-wave rasp at half the pitch
// Its pitch is the motor's speed as a share of its free speed (190 Hz at rest to about 2.4 kHz), and the motor's speed
// leads the car's: it flares the instant the throttle goes on and winds down more slowly than it spins up.
// The loops are cut from a recording of a real RC drift car (steady driving, hard drifting, and the tyre-scrub band of
// the drifting section), softened and kept under the synthesiser.
// Heard from the pilot's place.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public partial class CarAudio : Node {
    public float Volume = 0.8f; public bool Muted, Motor = true; public static string Solo;
    public double MotorVFree = 10.5, MotorFreeRev = 382, MotorPitch = 1.3; public int Esc = 2;   // pitch: the motor's own character (10.5T 1.4, 13.5T drift 1.3); esc: 0 blinky, 1 boost, 2 boost and turbo

    const int SR = 44100, Block = 32;
    AudioStreamPlayer _player; AudioStreamGeneratorPlayback _pb; readonly Random _rng = new(7);

    // ---- the Web Audio pieces
    sealed class Biquad {
      double b0 = 1, b1, b2, a1, a2, z1, z2;
      void Set(double B0, double B1, double B2, double A0, double A1, double A2) { b0 = B0 / A0; b1 = B1 / A0; b2 = B2 / A0; a1 = A1 / A0; a2 = A2 / A0; }
      static double W(double f) => 2 * Math.PI * Math.Clamp(f, 10, SR * 0.49) / SR;
      public void Lowpass(double f, double qDb) { double w = W(f), c = Math.Cos(w), al = Math.Sin(w) / (2 * Math.Pow(10, qDb / 20)); Set((1 - c) / 2, 1 - c, (1 - c) / 2, 1 + al, -2 * c, 1 - al); }
      public void Peaking(double f, double q, double gDb) { double w = W(f), c = Math.Cos(w), A = Math.Pow(10, gDb / 40), al = Math.Sin(w) / (2 * q); Set(1 + al * A, -2 * c, 1 - al * A, 1 + al / A, -2 * c, 1 - al / A); }
      public void Bandpass(double f, double q) { double w = W(f), c = Math.Cos(w), al = Math.Sin(w) / (2 * q); Set(al, 0, -al, 1 + al, -2 * c, 1 - al); }
      public double Run(double x) { double y = b0 * x + z1; z1 = b1 * x - a1 * y + z2; z2 = b2 * x - a2 * y; return y; }
    }
    /// a parameter moved the way setTargetAtTime moves it: towards its target with a time constant
    struct Par { public double V, T, Tau; public Par(double v, double tau) { V = T = v; Tau = tau; } public void To(double target, double tau) { T = target; Tau = tau; } public void Step(double dt) { V += (T - V) * (1 - Math.Exp(-dt / Math.Max(Tau, 1e-4))); } }
    sealed class Osc { public double Ph; public int Type; /* 0 sine, 1 sawtooth, 2 square */
      static double Blep(double t, double dt) { if (t < dt) { t /= dt; return t + t - t * t - 1; } if (t > 1 - dt) { t = (t - 1) / dt; return t * t + t + t + 1; } return 0; }
      public double Run(double f) { double dt = f / SR, t = Ph, y; if (Type == 0) y = Math.Sin(2 * Math.PI * t); else if (Type == 1) y = 2 * t - 1 - Blep(t, dt); else { y = (t < 0.5 ? 1 : -1) + Blep(t, dt) - Blep((t + 0.5) % 1, dt); }
        Ph += dt; if (Ph >= 1) Ph -= Math.Floor(Ph); return y; } }
    sealed class Loop { public float[] D = Array.Empty<float>(); public double Pos, SrcRate = 24000; public Par Gain = new(0, 0.08), Rate = new(1, 0.05);
      public double Run() { if (D.Length < 2) return 0; int i = (int)Pos; double f = Pos - i; double y = D[i] + (D[(i + 1) % D.Length] - D[i]) * f; Pos += Rate.V * SrcRate / SR; if (Pos >= D.Length) Pos -= D.Length; return y * Gain.V; } }
    sealed class Knock { public double T, K, Ph, NoisePos; public Biquad F = new(); }

    readonly Osc _w0 = new() { Type = 1 }, _w1 = new(), _w2 = new(), _meshA = new(), _meshB = new(), _growl = new() { Type = 1 }, _squeal = new(), _rasp = new() { Type = 2 }; double _lfoPh;
    readonly Biquad _res = new(), _mf = new(), _growlF = new(), _raspF = new(), _loopF = new(), _scrubF = new(), _airL = new(), _airR = new();
    Par _f = new(200, 0.02), _resF = new(800, 0.03), _resG = new(7, 0.05), _meshF = new(324, 0.02), _growlHz = new(28, 0.03), _gMeshA = new(0, 0.05), _gMeshB = new(0, 0.05), _gGrowl = new(0, 0.06), _mfF = new(2000, 0.04), _mg = new(0, 0.15),
      _gW0 = new(0.22, 0.05), _gSqueal = new(0, 0.05), _raspF2 = new(100, 0.02), _gRasp = new(0, 0.05), _airF = new(9000, 0.05), _dist = new(1, 0.02), _pan = new(0.5, 0.02), _out = new(0, 0.03);
    readonly Loop _cruise = new(), _drift = new(), _scrub = new(); readonly List<Knock> _knocks = new(); readonly float[] _noise = new float[SR]; double _spool, _lastKnock = -1, _time, _env; bool _ready;

    public override void _Ready() {
      for (int i = 0; i < _noise.Length; i++) _noise[i] = (float)(_rng.NextDouble() * 2 - 1);
      foreach (var (name, l) in new[] { ("cruise", _cruise), ("drift", _drift), ("scrub", _scrub) }) {
        if (GD.Load<AudioStream>("res://assets/snd/web_" + name + ".wav") is AudioStreamWav w && w.Format == AudioStreamWav.FormatEnum.Format16Bits) { var b = w.Data; int ch = w.Stereo ? 2 : 1, n = b.Length / 2 / ch; l.D = new float[n]; for (int i = 0; i < n; i++) l.D[i] = BitConverter.ToInt16(b, i * 2 * ch) / 32768f; l.SrcRate = w.MixRate; l.Pos = _rng.NextDouble() * n; }
        else GD.PrintErr("sound: loop not loaded: ", name); }
      _growlF.Lowpass(260, 1); _raspF.Bandpass(3200, 1.2); _loopF.Lowpass(3200, 0.5); _scrubF.Lowpass(2400, 0.5); _ready = true;
      _player = new AudioStreamPlayer { Stream = new AudioStreamGenerator { MixRate = SR, BufferLength = 0.08f }, Bus = "Master" }; AddChild(_player); _player.Play(); _pb = (AudioStreamGeneratorPlayback)_player.GetStreamPlayback();
    }
    /// a knock (0..1): the car into a barrier, or landing
    public void Bump(double k) { if (_time - _lastKnock < 0.12) return; _lastKnock = _time; var kn = new Knock { K = k, NoisePos = _rng.NextDouble() * 0.5 * SR }; kn.F.Lowpass(700, 1); lock (_knocks) _knocks.Add(kn); }
    static double Smooth(double a, double b, double x) { double t = Math.Clamp((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); }

    /// once a frame: the car's state sets where every part of the sound is heading (the browser version's update and setPose)
    public void Step(CarPhysics c, Transform3D car, Transform3D ear, bool paused, float dt) {
      bool active = !paused; double rpm = c.W / MotorVFree, thr = c.Thr, slide = c.Slide;
      double qT = Math.Min(1.35, Math.Abs(rpm));
      _spool += (qT + (active ? thr * 0.12 * (1 - qT) : 0) - _spool) * (1 - Math.Exp(-dt / (qT > _spool ? 0.05 : 0.22)));
      double q = Math.Max(0, _spool); bool on = active && thr > 0.02, alive = active && q > 0.015;
      double f = 190 + q * 1900 * (0.9 + 0.1 * MotorPitch);
      _f.To(f, 0.02); _resF.To(f * 1.05, 0.03); _meshF.To(f * 1.62, 0.02); _growlHz.To(28 + q * 140, 0.03);
      double load = on ? thr : 0;
      _gMeshA.To(alive ? 0.025 + load * 0.035 : 0, 0.05); _gMeshB.To(alive ? 0.02 + load * 0.025 : 0, 0.05);
      _gGrowl.To(alive ? (0.05 + load * 0.16) * Math.Min(1, q * 3) : 0, 0.06);
      _mfF.To(1400 + q * 3800 + load * 900, 0.04);
      _mg.To(on ? 0.05 + thr * 0.12 + q * 0.08 : alive ? 0.012 + q * 0.06 : 0, on ? 0.03 : 0.15);   // on the throttle the whole motor is loud; off it, it coasts down quieter and duller
      _gW0.To(on ? 0.16 + thr * 0.12 : 0.07, 0.05);
      _gSqueal.To(on ? Math.Clamp(0.06 * thr * (1 - q * 3.5), 0, 0.05) : 0, 0.05);
      double top = Math.Clamp((q - 0.85) / 0.3, 0, 1) * (Esc > 0 ? 1 : 0) * (on ? thr : 0);
      _resG.To(7 + top * 5, 0.05); _raspF2.To(f * 0.5, 0.02); _gRasp.To(Esc == 2 ? top * 0.05 : 0, 0.05);
      double drifting = Math.Clamp(slide * 1.3, 0, 1), rate = (0.72 + q * 0.7) * 1.06 * (0.9 + 0.1 * MotorPitch);
      double level = on ? (0.3 + thr * 0.5 + q * 0.25) * 0.3 : alive ? q * 0.07 : 0;                  // the recording sits under the synthesised whine
      _cruise.Gain.To(level * (1 - drifting), 0.08); _cruise.Rate.To(rate, 0.05); _drift.Gain.To(level * drifting, 0.08); _drift.Rate.To(rate * 0.98, 0.05);
      _scrub.Gain.To(active ? Math.Clamp(slide * 0.3, 0, 0.3) : 0, 0.08); _scrub.Rate.To(0.9 + Math.Min(0.3, q * 0.25), 0.05);
      // where the car is from the listener: further away is quieter (inverse law: reference 1 m, roll-off 0.75), duller, and to its side
      var src = car.Origin + new Vector3(0, 0.05f, 0); var rel = src - ear.Origin; double d = rel.Length();
      _airF.To(Math.Clamp(10000 / (1 + d * 0.55), 1200, 10000), 0.05); _dist.To(1.0 / (1 + 0.75 * (Math.Max(d, 1.0) - 1.0)), 0.02);
      double x = rel.Dot(ear.Basis.X), z = rel.Dot(-ear.Basis.Z), az = Math.Atan2(x, z) * 180 / Math.PI; if (az > 90) az = 180 - az; else if (az < -90) az = -180 - az;
      _pan.To((az + 90) / 180, 0.02);
      _out.To(Muted ? 0 : Volume * Volume, 0.03);
      Fill();
    }
    public override void _Process(double delta) { Fill(); }

    void Fill() {
      if (_pb == null || !_ready) return; int frames = _pb.GetFramesAvailable(); if (frames <= 0) return; var buf = new Vector2[frames]; double bdt = (double)Block / SR;
      for (int i = 0; i < frames; i++) {
        if (i % Block == 0) { // parameters move, filters follow them
          _f.Step(bdt); _resF.Step(bdt); _resG.Step(bdt); _meshF.Step(bdt); _growlHz.Step(bdt); _gMeshA.Step(bdt); _gMeshB.Step(bdt); _gGrowl.Step(bdt); _mfF.Step(bdt); _mg.Step(bdt); _gW0.Step(bdt); _gSqueal.Step(bdt); _raspF2.Step(bdt); _gRasp.Step(bdt);
          _airF.Step(bdt); _dist.Step(bdt); _pan.Step(bdt); _out.Step(bdt); foreach (var l in new[] { _cruise, _drift, _scrub }) { l.Gain.Step(bdt); l.Rate.Step(bdt); }
          _res.Peaking(_resF.V, 2.2, _resG.V); _mf.Lowpass(_mfF.V, 0.7); _airL.Lowpass(_airF.V, 0.5);
        }
        _lfoPh += 5.3 / SR; if (_lfoPh >= 1) _lfoPh -= 1; double det = Math.Pow(2, 4 * Math.Sin(2 * Math.PI * _lfoPh) / 1200);   // every voice wavers four cents, 5.3 times a second
        double f = _f.V * det;
        double whine = _w0.Run(f) * _gW0.V + _w1.Run(f) * 0.20 + _w2.Run(f * 2) * 0.06;
        double mesh = _meshA.Run(_meshF.V * det) * _gMeshA.V + _meshB.Run(_meshF.V * det * 1.005213) * _gMeshB.V;
        double motor = (_mf.Run(_res.Run(whine) + mesh) + _growlF.Run(_growl.Run(_growlHz.V * det) * _gGrowl.V) + _squeal.Run(2600 * det) * _gSqueal.V + _raspF.Run(_rasp.Run(_raspF2.V * det) * _gRasp.V)) * _mg.V;
        double loops = _loopF.Run(_cruise.Run() + _drift.Run()) + _scrubF.Run(_scrub.Run());
        double knocks = 0;
        if (_knocks.Count > 0) lock (_knocks) { for (int k = _knocks.Count - 1; k >= 0; k--) { var kn = _knocks[k]; double t = kn.T; kn.T += 1.0 / SR; if (t > 0.2) { _knocks.RemoveAt(k); continue; }
              double hz = t < 0.15 ? 180 * Math.Pow(50.0 / 180, t / 0.15) : 50, g0 = 0.3 * kn.K + 0.04, g = g0 * Math.Pow(0.001 / g0, Math.Min(1, t / 0.18)); kn.Ph += hz / SR; knocks += Math.Sin(2 * Math.PI * kn.Ph) * g;   // a thump sliding down from 180 to 50 Hz
              if (t < 0.1) { double n0 = Math.Max(1e-6, 0.18 * kn.K), ng = n0 * Math.Pow(0.001 / n0, Math.Min(1, t / 0.09)); knocks += kn.F.Run(_noise[(int)(kn.NoisePos + t * SR) % _noise.Length]) * ng; } } }                  // and a short burst of low noise
        double carBus = Solo == "motor" ? motor : Solo == "loops" ? loops : Solo == "knocks" ? knocks : motor + loops + knocks;
        double y = _airL.Run(carBus) * _dist.V * 0.9, p = _pan.V * Math.PI / 2, l0 = y * Math.Cos(p), r0 = y * Math.Sin(p);
        // the compressor: threshold -18 dB, ratio 3, a 30 dB soft knee, 3 ms attack, 250 ms release, with the make-up gain a browser adds
        double pk = Math.Max(Math.Abs(l0), Math.Abs(r0)); _env += (pk - _env) * (pk > _env ? 1 - Math.Exp(-1.0 / (0.003 * SR)) : 1 - Math.Exp(-1.0 / (0.25 * SR)));
        double xdb = 20 * Math.Log10(Math.Max(_env, 1e-6)), ydb = xdb < -18 ? xdb : xdb < 12 ? xdb + (1.0 / 3 - 1) * (xdb + 18) * (xdb + 18) / 60 : 12 - 10 + (xdb - 12) / 3, gain = Math.Pow(10, (ydb - xdb + 2.16) / 20) * _out.V;
        buf[i] = new Vector2((float)(l0 * gain), (float)(r0 * gain)); _time += 1.0 / SR;
      }
      _pb.PushBuffer(buf);
    }
  }
}
