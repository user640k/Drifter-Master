// A car carried round the middle of the lane on a clean drift line: at a speed that suits each bend, its nose turned
// into the bend by a drift angle that grows with how tight the bend is, the front wheels turned back along the way it
// is going. Nothing is simulated, so it never touches a barrier. Used for the opponent (a pace car to follow) and for
// filming.
using System;

namespace DriftMaster {
  public sealed class LineDriver {
    public double S = -1, V = 2, B; double[] _k;   // place on the lap (in samples), speed, drift angle

    /// pace: 1 for its natural speed; less to make it wait
    public void Step(double dt, Track t, CarPhysics c, double pace = 1) {
      int n = t.N; if (_k == null) { var raw = new double[n]; for (int i = 0; i < n; i++) { int a = (i - 1 + n) % n, b = (i + 1) % n; double dh = Math.Atan2(t.TX[b], t.TZ[b]) - Math.Atan2(t.TX[a], t.TZ[a]); raw[i] = Math.Atan2(Math.Sin(dh), Math.Cos(dh)) / (2 * t.Ds); }
        _k = new double[n]; for (int i = 0; i < n; i++) { double sum = 0; for (int k = -8; k <= 8; k++) sum += raw[((i + k) % n + n) % n]; _k[i] = sum / 17; } }
      if (S < 0) S = t.NearestT(c.X, c.Z, -1);
      double K(double s) { int i = ((int)Math.Floor(s) % n + n) % n; return _k[i]; }
      double ahead = 0.4 / t.Ds, kA = K(S + ahead), kMax = 0; for (double q = 0; q < 1.0 / t.Ds; q += 2) kMax = Math.Max(kMax, Math.Abs(K(S + q)));
      double vT = Math.Clamp(Math.Sqrt(2.4 / Math.Max(kMax, 0.05)), 1.25, 2.9) * pace, bT = Math.Sign(kA) * 0.62 * Math.Tanh(Math.Abs(kA) / 0.9);
      V += (vT - V) * (1 - Math.Exp(-dt / 0.45)); B += (bT - B) * (1 - Math.Exp(-dt / 0.3)); S += V * dt / t.Ds;
      int i0 = ((int)Math.Floor(S) % n + n) % n, i1 = (i0 + 1) % n; double f = S - Math.Floor(S), kH = K(S);
      double tx = t.TX[i0] + (t.TX[i1] - t.TX[i0]) * f, tz = t.TZ[i0] + (t.TZ[i1] - t.TZ[i0]) * f, ht = Math.Atan2(tx, tz);
      c.X = t.X[i0] + (t.X[i1] - t.X[i0]) * f; c.Z = t.Z[i0] + (t.Z[i1] - t.Z[i0]) * f; c.Y = 0; c.Air = false; t.Idx = i0;
      c.Hd = ht + B; c.Steer = Math.Clamp(-B * 0.92 + kH * 0.257, -0.62, 0.62); c.Roll = Math.Sign(kH) * Math.Min(0.05, V * V * Math.Abs(kH) * 0.008); c.Pitch = 0; c.Heave = 0;
      c.Spin += V / 0.032 * dt * (1.25 + Math.Abs(B)); c.SpinF += V / 0.032 * dt; c.Vx = V * tx; c.Vz = V * tz; c.U = V * Math.Cos(B); c.V = -V * Math.Sin(B); c.Beta = B;
    }
  }
}
