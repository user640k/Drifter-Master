// Drift judging for solo training, after the solo qualifying (tanso) format: a run out of 100, plus up to 5 bonus.
// No engine types here: it only watches the car.
//   Line 40: every inner clipping point (nose close to the cone) and outer zone (tail close to the outside barrier), taken
//     on a natural line. Reaching for a clip (a sudden swerve to touch it) halves that clip and costs style.
//   Angle 40: depth (25: how much angle the car carries through the bends) and consistency (15: holding it, not flattening).
//   Style 20: fluidity (7: transitions that are quick, clean and made once), commitment (7: staying on the throttle through
//     the bends, carrying speed through the outer zones, running close to the edge there), initiation (6: early and sharp).
//   Bonus, up to 5: initiating before the initiation line (0.8 m ahead of the first bend), and doing it hard.
//   Speed earns nothing by itself; it shows only as momentum in the outer zones.
//   Deductions: straightening up in a bend, tapping the barriers (the first is cheap, the rest are not). A spin, or
//     stopping on the course, scores the run zero.
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
      // at most six marks, at the corners that turn the car the most (a weaving run has many small bends: they are driven, not judged one by one)
      if (bends.Count > 6) { double Turn(int apex) { double sum = 0; for (int d = 0; d < n; d++) { int q = (apex + d) % n; if (!c.Bend[q] || Math.Sign(k[q]) != Math.Sign(k[apex])) break; sum += Math.Abs(k[q]) * t.Ds; } for (int d = 1; d < n; d++) { int q = ((apex - d) % n + n) % n; if (!c.Bend[q] || Math.Sign(k[q]) != Math.Sign(k[apex])) break; sum += Math.Abs(k[q]) * t.Ds; } return sum; }
        bends.Sort((a, b) => Turn(b.apex).CompareTo(Turn(a.apex))); bends.RemoveRange(6, bends.Count - 6); }
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
    public double Time, Dist, DriftT, BendT, AngleSum, AngleSq, RateSum, Progress; public int Straightens, Taps, Reaches, Transitions; public bool Spun, Started;
    public double ThrBendT, ZoneT, ZoneSpeed, TransScore, InitLead = double.NaN, InitRate; readonly bool[] _reached; double _reachT, _transT = -1, _lastVdir; int _transFrom, _firstBend = -1, _startIdx = -1; double _sinceFlip = 9;
    double _flatT, _slowT, _tapT, _lastBeta; int _lastIdx = -1; bool _flatCounted;
    public RunJudge(Course c, Track t) { _c = c; _t = t; Clip = new double[c.Marks.Count]; _reached = new bool[c.Marks.Count]; }
    public bool Done => Progress >= _t.N;
    public void Tap(double k) { if (k > 0.2 && _tapT <= 0 && Started) { Taps++; _tapT = 0.5; } }

    public void Step(double dt, CarPhysics car, int idx) {
      double sp = Math.Sqrt(car.U * car.U + car.V * car.V), beta = car.Beta, ab = Math.Abs(beta); int n = _t.N;
      if (_lastIdx >= 0) { int d = idx - _lastIdx; if (d > n / 2) d -= n; if (d < -n / 2) d += n; Progress += d; } _lastIdx = idx;
      Time += dt; Dist += sp * dt; _tapT -= dt; bool drifting = ab > 0.17 && sp > 0.6, bend = _c.Bend[idx];
      if (drifting) { Started = true; DriftT += dt; }
      if (bend) { BendT += dt; double aa = Math.Min(ab, 0.95); AngleSum += aa * dt; AngleSq += aa * aa * dt; if (car.Thr > 0.3) ThrBendT += dt; }
      // the first bend of the run, for the initiation: how far before it the tail first went out, and how sharply
      if (_startIdx < 0) { _startIdx = idx; int bestD = int.MaxValue; foreach (var b in _c.Bends) { int d = ((b.a - idx) % n + n) % n; if (d > 12 && d < bestD) { bestD = d; _firstBend = b.a; } } }
      if (double.IsNaN(InitLead) && ab > 0.26 && sp > 0.6 && _firstBend >= 0) { int d = _firstBend - idx; if (d > n / 2) d -= n; if (d < -n / 2) d += n; InitLead = d * _t.Ds; InitRate = Math.Abs(beta - _lastBeta) / dt; }
      // transitions: the tail swapping from one side to the other. Timed from 15 degrees one way to 15 the other
      _sinceFlip += dt;
      if (ab > 0.26 && sp > 0.6) { int sg = Math.Sign(beta); if (_transFrom != 0 && sg != _transFrom) { double took = _transT >= 0 ? Time - _transT : 0.5; Transitions++; TransScore += Math.Clamp(1 - (took - 0.35) / 0.9, 0, 1) * (_sinceFlip < 1.0 ? 0.4 : 1); _sinceFlip = 0; } _transFrom = sg; _transT = Time; }
      // reaching: the car's path bending far tighter than the lane does, close to an inner clip: a late swerve at the cone
      double vdir = Math.Atan2(car.Vx, car.Vz), dv = vdir - _lastVdir; dv = Math.Atan2(Math.Sin(dv), Math.Cos(dv)); _lastVdir = vdir; double pathK = sp > 0.5 ? Math.Abs(dv) / dt / sp : 0;
      RateSum += Math.Abs(beta - _lastBeta); _lastBeta = beta;
      if (Started) {
        if (bend && !drifting && sp > 0.5) { _flatT += dt; if (_flatT > 0.7 && !_flatCounted) { Straightens++; _flatCounted = true; } } else if (drifting) { _flatT = 0; _flatCounted = false; }
        if (ab > 1.75 && sp > 0.3) Spun = true;
        if (sp < 0.2) { _slowT += dt; if (_slowT > 1.2) Spun = true; } else _slowT = 0;
      }
      // the line: nose to the inner clips, tail to the outer zones, while sliding
      double fx = Math.Sin(car.Hd), fz = Math.Cos(car.Hd); if (pathK <= 7.0) _reachT = 0;
      for (int m = 0; m < _c.Marks.Count; m++) { var mk = _c.Marks[m]; int rel = idx - mk.I; if (rel > n / 2) rel -= n; if (rel < -n / 2) rel += n; if (rel < mk.I0 - mk.I || rel > mk.I1 - mk.I || !drifting) continue;
        double v;
        if (!mk.Outer) { double dx = car.X + fx * 0.2 - mk.X, dz = car.Z + fz * 0.2 - mk.Z, d = Math.Sqrt(dx * dx + dz * dz); v = 1 - (d - 0.15) / 0.2;
          if (Math.Abs(rel) * _t.Ds < 0.45 && pathK > 7.0) { _reachT += dt; if (_reachT > 0.1 && !_reached[m]) { _reached[m] = true; Reaches++; } } }
        else { double rx = car.X - fx * 0.2 - _t.X[idx], rz = car.Z - fz * 0.2 - _t.Z[idx], off = -(rx * _t.NX[idx] + rz * _t.NZ[idx]) * mk.Side, gap = _t.Hw - off; v = 1 - (gap - 0.17) / 0.22; ZoneT += dt; ZoneSpeed += sp * dt; }
        Clip[m] = Math.Max(Clip[m], Math.Clamp(v, 0, 1)); }
    }
    public double LineFrac { get { if (Clip.Length == 0) return 1; double s = 0; for (int m = 0; m < Clip.Length; m++) s += Clip[m] * (_reached[m] ? 0.5 : 1); return s / Clip.Length; } }
    public int ClipsHit { get { int k = 0; foreach (var v in Clip) if (v > 0.5) k++; return k; } }
    public double AvgAngleDeg => BendT > 0 ? AngleSum / BendT * 180 / Math.PI : 0;
    public double AvgSpeed => Time > 0 ? Dist / Time : 0;
    public struct Card { public double Line, Angle, Style, Bonus, Deduct, Total, Depth, Consistency, Fluidity, Commitment, Initiation; public string Notes; }
    /// the run's score: line 40, angle 40, style 20, bonus up to 5
    public Card Solo() {
      var c = new Card(); if (Spun) { c.Notes = "SPIN OR STOP ON COURSE: ZERO"; return c; }
      c.Line = 40 * LineFrac;
      double mean = BendT > 0 ? AngleSum / BendT : 0, var_ = BendT > 0 ? Math.Max(0, AngleSq / BendT - mean * mean) : 0, cv = mean > 0.05 ? Math.Sqrt(var_) / mean : 1;
      c.Depth = 25 * Math.Clamp(mean / 0.66, 0, 1);                         // 38 degrees and more: full marks
      c.Consistency = 15 * Math.Clamp(1 - (cv - 0.3) / 0.55, 0, 1) * Math.Clamp(mean / 0.3, 0, 1);   // steady angle (and some angle to be steady about)
      c.Angle = c.Depth + c.Consistency;
      double wob = Time > 0 ? RateSum / Time : 0, smooth = Math.Clamp(1 - (wob - 1.0) / 2.0, 0, 1), trans = Transitions > 0 ? TransScore / Transitions : smooth;
      c.Fluidity = 7 * (0.6 * trans + 0.4 * smooth);
      double onThr = BendT > 0 ? ThrBendT / BendT : 0, cont = BendT > 0 ? Math.Clamp(DriftT / Math.Max(BendT, 0.1), 0, 1) : 0, zoneV = ZoneT > 0 && AvgSpeed > 0 ? Math.Clamp((ZoneSpeed / ZoneT) / AvgSpeed, 0, 1.1) / 1.1 : 0, outer = 0; int no = 0;
      for (int m = 0; m < Clip.Length; m++) if (_c.Marks[m].Outer) { outer += Clip[m]; no++; } if (no > 0) outer /= no; else outer = cont;
      c.Commitment = 7 * (0.35 * Math.Clamp((onThr - 0.4) / 0.5, 0, 1) + 0.25 * cont + 0.2 * zoneV + 0.2 * outer);
      bool init = !double.IsNaN(InitLead); double early = init ? Math.Clamp((InitLead + 0.3) / 0.8, 0, 1) : 0, sharp = init ? Math.Clamp(InitRate / 3.0, 0, 1) : 0;   // tail out by 0.5 m before the bend: full marks for timing
      c.Initiation = 6 * (0.6 * early + 0.4 * sharp);
      c.Style = Math.Max(0, c.Fluidity + c.Commitment + c.Initiation - 2 * Reaches);
      c.Bonus = init && InitLead > 0.8 && InitLead <= 3.0 ? Math.Min(5, 5 * (0.6 * Math.Clamp((InitLead - 0.8) / 1.0, 0, 1) + 0.4 * sharp)) : 0;   // before the initiation line (but not a slide carried from far back along a long run-in)
      c.Deduct = 4 * Straightens + (Taps > 0 ? 2 + 4 * (Taps - 1) : 0); var notes = new List<string>();
      if (ClipsHit < Clip.Length) notes.Add($"MISSED {Clip.Length - ClipsHit} OF {Clip.Length} CLIPS"); if (Reaches > 0) notes.Add($"REACHED FOR A CLIP x{Reaches}"); if (Straightens > 0) notes.Add($"STRAIGHTENED x{Straightens}"); if (Taps > 0) notes.Add($"WALL TAP x{Taps}"); if (c.Bonus > 0.05) notes.Add("EARLY INITIATION");
      c.Total = Math.Max(0, c.Line + c.Angle + c.Style + c.Bonus - c.Deduct); c.Notes = string.Join("   ", notes); return c;
    }
  }
}
