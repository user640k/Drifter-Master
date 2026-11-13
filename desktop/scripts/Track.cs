// The track: a closed centreline sampled at N points, a lane half-width and a height per sample (the climb and the
// bridge). Same data and queries as the browser version (nearestT, groundAt, contactsFor), with no engine types,
// so the physics can be tested outside Godot.
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DriftMaster {
  public sealed class Track {
    public int N;
    public double[] X, Z, E, TX, TZ, NX, NZ;
    public double Hw, Ds, HallX, HallZ;
    public double[] Start = new double[3];
    public double[][] RaceLine;
    public string Name = "", Wear = "floor_wear"; public bool Boards; /* the floor sheet's name; white boards all round in place of foam blocks */
    public int Idx; // where the car last was on the lap: the hint that keeps a search on its own level at the crossing
    public const double CarR = 0.098;

    // z0: moved along z so the layout sits in the middle of its own small hall
    public static Track FromJson(string json, double z0, double hallX, double hallZ) {
      using var doc = JsonDocument.Parse(json); var r = doc.RootElement; var t = new Track();
      double[] arr(string k) { var a = r.GetProperty(k); var o = new double[a.GetArrayLength()]; int i = 0; foreach (var v in a.EnumerateArray()) o[i++] = v.GetDouble(); return o; }
      t.N = r.GetProperty("n").GetInt32(); t.Hw = r.GetProperty("hw").GetDouble(); t.Ds = r.GetProperty("ds").GetDouble();
      t.X = arr("x"); t.Z = arr("z"); t.E = arr("e"); var s = arr("start");
      for (int i = 0; i < t.N; i++) t.Z[i] -= z0;
      t.Start = new[] { s[0], s[1] - z0, s[2] };
      var rl = r.GetProperty("raceLine"); t.RaceLine = new double[rl.GetArrayLength()][]; int q = 0;
      foreach (var p in rl.EnumerateArray()) t.RaceLine[q++] = new[] { p[0].GetDouble(), p[1].GetDouble() - z0 };
      int n = t.N; t.TX = new double[n]; t.TZ = new double[n]; t.NX = new double[n]; t.NZ = new double[n];
      for (int i = 0; i < n; i++) {
        int a = (i - 1 + n) % n, b = (i + 1) % n; double dx = t.X[b] - t.X[a], dz = t.Z[b] - t.Z[a], l = Math.Sqrt(dx * dx + dz * dz);
        t.TX[i] = dx / l; t.TZ[i] = dz / l; t.NX[i] = t.TZ[i]; t.NZ[i] = -t.TX[i];
      }
      t.HallX = hallX; t.HallZ = hallZ; return t;
    }

    public int NearestT(double x, double z, int hint) {
      int bi = 0; double bd = 1e18; int from = hint < 0 ? 0 : hint - 30, to = hint < 0 ? N - 1 : hint + 30;
      for (int k = from; k <= to; k++) { int i = ((k % N) + N) % N; double dx = x - X[i], dz = z - Z[i], d = dx * dx + dz * dz; if (d < bd) { bd = d; bi = i; } }
      return bi;
    }

    // floor height under a point: follows the lap near the hint, so the road can pass under its own bridge
    public double GroundAt(double x, double z, int hint) {
      int i = NearestT(x, z, hint); double along = (x - X[i]) * TX[i] + (z - Z[i]) * TZ[i];
      int k = along >= 0 ? (i + 1) % N : (i - 1 + N) % N;
      return E[i] + (E[k] - E[i]) * Math.Min(1, Math.Abs(along) / Ds);
    }
    public double GroundAt(double x, double z) => GroundAt(x, z, Idx);
    /// what marks the lane edge here: a low PVC board (true) or a foam block. Boards are hard and springy; foam gives.
    public bool BoardAt(int i) => Boards || E[i] > 0.004;

    // ---- the bump map: what the floor's surface really does under each wheel, in metres. Every track has its own,
    // made once from its name, so a bump is always in the same place. Felt through the springs; the shell shivers on it.
    //   everywhere: each tile sits a hair high or low, the joints are fine grooves, the slab under them undulates
    //   here and there: shallow humps and dips, the lines where the sheets of the sub-floor meet (a small step and a
    //     ridge right across the hall), a few tile corners that have lifted
    //   along the lane: the kerbs on the inside of tight bends (1.2 mm proud, with a ripple from block to block), and on
    //     a raised road the joints between its deck boards
    const double TileS = 0.3048, Joint = 0.0004, JointW = 0.004, StepH = 0.00015, Und = 0.0008, Wl = 2.1;
    double[][] _humps, _seams, _corners; double _ph1, _ph2; int _seed; bool[] _kerb; double[] _kerbSide;
    public const double KerbH = 0.0012, DeckJoint = 1.22;
    static double Hash2(int i, int j, int seed) { unchecked { int h = i * 374761393 + j * 668265263 + seed * 1442695041; h = (h ^ (int)((uint)h >> 13)) * 1274126177; return (uint)(h ^ (int)((uint)h >> 16)) / 4294967296.0; } }
    void MakeBumps() {
      _seed = 41; foreach (char ch in Name) _seed = unchecked(_seed * 31 + ch); var r = new Random(_seed);
      double R(double a, double b) => a + r.NextDouble() * (b - a);
      _ph1 = R(0, 6.28); _ph2 = R(0, 6.28);
      _humps = new double[16][]; for (int i = 0; i < 16; i++) _humps[i] = new[] { R(-HallX + 0.3, HallX - 0.3), R(-HallZ + 0.3, HallZ - 0.3), R(0.08, 0.3), (r.NextDouble() < 0.5 ? -1 : 1) * R(0.0004, 0.001) };
      // sub-floor sheets are 1.22 x 2.44 m: joints across the hall every 1.22 m one way, every 2.44 m the other
      var sm = new List<double[]>(); double x0 = R(-0.6, 0.6), z0 = R(-1.2, 1.2);
      for (double x = x0 - 6.1; x < HallX; x += 1.22) if (x > -HallX) sm.Add(new[] { 0, x, R(-0.0004, 0.0004), R(0.0001, 0.00035) });
      for (double z = z0 - 4.88; z < HallZ; z += 2.44) if (z > -HallZ) sm.Add(new[] { 1, z, R(-0.0004, 0.0004), R(0.0001, 0.00035) });
      _seams = sm.ToArray();
      _corners = new double[12][]; for (int i = 0; i < 12; i++) _corners[i] = new[] { Math.Round(R(-HallX, HallX) / TileS) * TileS, Math.Round(R(-HallZ, HallZ) / TileS) * TileS, R(0.0004, 0.0009) };
      // kerbs: where the lane bends tighter than about 0.9 m, along its inner edge (as painted on the floor sheet)
      _kerb = new bool[N]; _kerbSide = new double[N];
      for (int i = 0; i < N; i++) { int a = (i - 1 + N) % N, b = (i + 1) % N; double dh = Math.Atan2(TX[b], TZ[b]) - Math.Atan2(TX[a], TZ[a]); dh = Math.Atan2(Math.Sin(dh), Math.Cos(dh)); double k = dh / (2 * Ds);
        _kerb[i] = Boards && Math.Abs(k) > 1 / 0.9 && E[i] < 0.004; _kerbSide[i] = k > 0 ? 1 : -1; }
    }
    /// the floor away from the lane's own features (also what the floor's reflections are bent by)
    public double BumpBase(double x, double z, bool tiles = true) {
      if (_humps == null) MakeBumps();
      double h = 0;
      if (tiles) {
        double u = x / TileS, w = z / TileS; int iu = (int)Math.Floor(u), iw = (int)Math.Floor(w);
        h = (Hash2(iu, iw, _seed) - 0.5) * 2 * StepH;
        double du = Math.Min(u - iu, iu + 1 - u) * TileS, dw = Math.Min(w - iw, iw + 1 - w) * TileS, dj = Math.Min(du, dw), hw = JointW / 2;
        if (dj < hw) h -= Joint * (0.5 + 0.5 * Math.Cos(Math.PI * dj / hw));
      }
      double kk = 2 * Math.PI / Wl; h += Und * (0.6 * Math.Sin(x * kk + _ph1) * Math.Sin(z * kk / 1.3 + _ph2) + 0.4 * Math.Sin((x + z) * kk / 0.7 + _ph1));
      foreach (var p in _humps) { double d2 = ((x - p[0]) * (x - p[0]) + (z - p[1]) * (z - p[1])) / (p[2] * p[2]); if (d2 < 4) h += p[3] * Math.Exp(-d2 * 2); }
      foreach (var s in _seams) { double d = (s[0] == 0 ? x : z) - s[1]; h += s[2] * 0.5 * Math.Tanh(d / 0.004); if (Math.Abs(d) < 0.03) h += s[3] * Math.Exp(-d * d / (0.008 * 0.008)); }
      foreach (var c in _corners) { double d2 = (x - c[0]) * (x - c[0]) + (z - c[1]) * (z - c[1]); if (d2 < 0.01) h += c[2] * Math.Exp(-d2 / (0.03 * 0.03)); }
      return h;
    }
    /// the whole bump map at a point, near the car's place on the lap
    public double Bump(double x, double z) {
      double h = BumpBase(x, z);
      int i = NearestT(x, z, Idx); double off = (x - X[i]) * NX[i] + (z - Z[i]) * NZ[i], along = (x - X[i]) * TX[i] + (z - Z[i]) * TZ[i];
      if (_kerb[i]) { // the kerb: a strip 7 cm wide just inside the board, ramped at its edge, rippled block to block
        double into = off * _kerbSide[i] - (Hw - 0.075);
        if (into > -0.01) h += KerbH * Math.Min(1, (into + 0.01) / 0.01) * (1 + 0.15 * Math.Sin((i * Ds + along) * 2 * Math.PI / 0.09));
      }
      if (E[i] > 0.01) { // deck boards: a ridge where two meet, and the boards drum a little between their supports
        double s = i * Ds + along, d = s - Math.Round(s / DeckJoint) * DeckJoint; if (Math.Abs(d) < 0.02) h += 0.0005 * Math.Exp(-d * d / (0.006 * 0.006));
        h += 0.00025 * Math.Sin(s * 2 * Math.PI / 0.41);
      }
      return h;
    }

    // contacts for one of the car's two circles: (nx, nz) points back into free space, pen is the overlap
    public void ContactsFor(double cx, double cz, List<(double nx, double nz, double pen)> hits) {
      int i = NearestT(cx, cz, Idx); Idx = i;
      double off = (cx - X[i]) * NX[i] + (cz - Z[i]) * NZ[i], edge = Hw - CarR;
      if (Math.Abs(off) > edge) { double sg = Math.Sign(off); hits.Add((-NX[i] * sg, -NZ[i] * sg, Math.Abs(off) - edge)); }
      double ix = HallX - 0.04, iz = HallZ - 0.04;
      if (cx < -ix + CarR) hits.Add((1, 0, -ix + CarR - cx));
      if (cx > ix - CarR) hits.Add((-1, 0, cx - (ix - CarR)));
      if (cz < -iz + CarR) hits.Add((0, 1, -iz + CarR - cz));
      if (cz > iz - CarR) hits.Add((0, -1, cz - (iz - CarR)));
    }
  }
}
