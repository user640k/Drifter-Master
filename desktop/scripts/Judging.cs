// Drift judging for solo training, after the usual qualifying format: a run judged on line, angle, style and speed.
// No engine types here: it only watches the car.
//   Line: the course has inner clipping points (the nose must pass close) and outer zones (the tail must run close to
//     the outside barrier), set out from the track's own bends.
//   Angle: how much slip angle the car carries through the bends.     Style: one continuous, fluid drift.
//   Speed: the average over the run.
//   Deductions: straightening up in a bend, tapping the barriers (the first is cheap, the rest are not).
//     A spin, or stopping on the course, scores the run zero.
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public sealed class Course {
    public struct Mark { public int I, I0, I1; public bool Outer; public double X, Z, Y, Side; }
    public readonly List<Mark> Marks = new(); public readonly List<(int a, int apex, int b, double side, int mark)> Bends = new(); /* every bend: where it starts, its tightest point, where it ends, which way its inside lies, its mark (-1: none) */ public bool[] Bend;   // Bend[i]: the track is turning here
    public const double ClipIn = 0.03, ZoneHalf = 0.4;

    public static Course Build(Track t) {
      var c = new Course(); int n = t.N; var k = new double[n]; c.Bend = new bool[n]; int w = Math.Max(2, (int)Math.Round(0.2 / t.Ds));
      for (int i = 0; i < n; i++) { int a = ((i - w) % n + n) % n, b = (i + w) % n; double h = Math.Atan2(t.TX[b], t.TZ[b]) - Math.Atan2(t.TX[a], t.TZ[a]); h = Math.Atan2(Math.Sin(h), Math.Cos(h)); k[i] = h / (2 * w * t.Ds); c.Bend[i] = Math.Abs(k[i]) > 0.55; }
      // bends: runs of one-way turning at least half a metre long; each gives one mark at its tightest point
      var bends = new List<(int apex, double k)>(); int skip = (int)(1.0 / t.Ds), s0 = t.NearestT(t.Start[0], t.Start[1], -1);
      for (int i = 0; i < n;) {
        if (!c.Bend[i]) { i++; continue; } int j = i, best = i; double sg = Math.Sign(k[i]);
        while (j < n && c.Bend[j] && Math.Sign(k[j]) == sg) { if (Math.Abs(k[j]) > Math.Abs(k[best])) best = j; j++; }
        int fromStart = ((best - s0) % n + n) % n;
        if ((j - i) * t.Ds >= 0.5) { int pa = ((best - 1) % n + n) % n, pb = (best + 1) % n; double sd = Math.Sign((t.TX[pb] - t.TX[pa]) * t.NX[best] + (t.TZ[pb] - t.TZ[pa]) * t.NZ[best]); if (sd == 0) sd = 1;
          c.Bends.Add((i, best, j, sd, -1)); if (fromStart > skip && fromStart < n - skip / 2) bends.Add((best, k[best])); }   // marks: not in the launch, not at the line
        i = j;
      }
      bends.Sort((a, b) => (((a.apex - s0) % n + n) % n).CompareTo(((b.apex - s0) % n + n) % n));
      for (int q = 0; q < bends.Count; q++) {
        int i = bends[q].apex; bool outer = q % 2 == 1; int a = ((i - 1) % n + n) % n, b = (i + 1) % n;
        double side = Math.Sign((t.TX[b] - t.TX[a]) * t.NX[i] + (t.TZ[b] - t.TZ[a]) * t.NZ[i]); if (side == 0) side = 1;   // which way the inside of the bend lies along the normal
        double off = outer ? -side * t.Hw : side * (t.Hw - ClipIn); int zh = (int)Math.Round(ZoneHalf / t.Ds);
        for (int bq = 0; bq < c.Bends.Count; bq++) if (c.Bends[bq].apex == i) { var bb = c.Bends[bq]; c.Bends[bq] = (bb.a, bb.apex, bb.b, bb.side, c.Marks.Count); }
        c.Marks.Add(new Mark { I = i, I0 = i - zh, I1 = i + zh, Outer = outer, Side = side, X = t.X[i] + t.NX[i] * off, Z = t.Z[i] + t.NZ[i] * off, Y = t.E[i] });
      }
      return c;
    }
  }

  public sealed class RunJudge {
    public readonly double[] Clip; readonly Course _c; readonly Track _t;
    public double Time, Dist, DriftT, BendT, AngleSum, RateSum, Progress; public int Straightens, Taps; public bool Spun, Started;
    double _flatT, _slowT, _tapT, _lastBeta; int _lastIdx = -1; bool _flatCounted;
    public RunJudge(Course c, Track t) { _c = c; _t = t; Clip = new double[c.Marks.Count]; }
    public bool Done => Progress >= _t.N;
    public void Tap(double k) { if (k > 0.2 && _tapT <= 0 && Started) { Taps++; _tapT = 0.5; } }

    public void Step(double dt, CarPhysics car, int idx) {
      double sp = Math.Sqrt(car.U * car.U + car.V * car.V), beta = car.Beta, ab = Math.Abs(beta); int n = _t.N;
      if (_lastIdx >= 0) { int d = idx - _lastIdx; if (d > n / 2) d -= n; if (d < -n / 2) d += n; Progress += d; } _lastIdx = idx;
      Time += dt; Dist += sp * dt; _tapT -= dt; bool drifting = ab > 0.17 && sp > 0.6, bend = _c.Bend[idx];
      if (drifting) { Started = true; DriftT += dt; }
      if (bend) { BendT += dt; AngleSum += Math.Min(ab, 0.95) * dt; }
      RateSum += Math.Abs(beta - _lastBeta); _lastBeta = beta;
      if (Started) {
        if (bend && !drifting && sp > 0.5) { _flatT += dt; if (_flatT > 0.7 && !_flatCounted) { Straightens++; _flatCounted = true; } } else if (drifting) { _flatT = 0; _flatCounted = false; }
        if (ab > 1.75 && sp > 0.3) Spun = true;
        if (sp < 0.2) { _slowT += dt; if (_slowT > 1.2) Spun = true; } else _slowT = 0;
      }
      // the line: nose to the inner clips, tail to the outer zones, while sliding
      double fx = Math.Sin(car.Hd), fz = Math.Cos(car.Hd);
      for (int m = 0; m < _c.Marks.Count; m++) { var mk = _c.Marks[m]; int rel = idx - mk.I; if (rel > n / 2) rel -= n; if (rel < -n / 2) rel += n; if (rel < mk.I0 - mk.I || rel > mk.I1 - mk.I || !drifting) continue;
        double v;
        if (!mk.Outer) { double dx = car.X + fx * 0.2 - mk.X, dz = car.Z + fz * 0.2 - mk.Z, d = Math.Sqrt(dx * dx + dz * dz); v = 1 - (d - 0.15) / 0.2; }
        else { double rx = car.X - fx * 0.2 - _t.X[idx], rz = car.Z - fz * 0.2 - _t.Z[idx], off = -(rx * _t.NX[idx] + rz * _t.NZ[idx]) * mk.Side, gap = _t.Hw - off; v = 1 - (gap - 0.17) / 0.22; }
        Clip[m] = Math.Max(Clip[m], Math.Clamp(v, 0, 1)); }
    }
    public double LineFrac { get { if (Clip.Length == 0) return 1; double s = 0; foreach (var v in Clip) s += v; return s / Clip.Length; } }
    public int ClipsHit { get { int k = 0; foreach (var v in Clip) if (v > 0.5) k++; return k; } }
    public double AvgAngleDeg => BendT > 0 ? AngleSum / BendT * 180 / Math.PI : 0;
    public double AvgSpeed => Time > 0 ? Dist / Time : 0;
    public struct Card { public double Line, Angle, Style, Speed, Deduct, Total; public string Notes; }
    /// the lead car's (or a solo run's) score out of 100
    public Card Solo() {
      var c = new Card(); if (Spun) { c.Notes = "SPIN OR STOP ON COURSE: ZERO"; return c; }
      c.Line = 35 * LineFrac; c.Angle = 30 * Math.Clamp(AvgAngleDeg / 38, 0, 1);
      double cont = BendT > 0 ? Math.Clamp(DriftT / Math.Max(BendT, 0.1), 0, 1) : 0, wob = Time > 0 ? RateSum / Time : 0, fluid = Math.Clamp(1 - (wob - 1.0) / 2.0, 0, 1);
      c.Style = 25 * (0.65 * cont + 0.35 * fluid); c.Speed = 10 * Math.Clamp((AvgSpeed - 1.0) / 1.4, 0, 1);
      c.Deduct = 4 * Straightens + (Taps > 0 ? 2 + 4 * (Taps - 1) : 0); var notes = new List<string>();
      if (ClipsHit < Clip.Length) notes.Add($"MISSED {Clip.Length - ClipsHit} OF {Clip.Length} CLIPS"); if (Straightens > 0) notes.Add($"STRAIGHTENED x{Straightens}"); if (Taps > 0) notes.Add($"WALL TAP x{Taps}");
      c.Total = Math.Max(0, c.Line + c.Angle + c.Style + c.Speed - c.Deduct); c.Notes = string.Join("   ", notes); return c;
    }
  }
}
