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

    // small flaws in the floor, felt through the springs: each tile sits a hair high or low, the joints are fine
    // grooves, the slab under them undulates, and there are a few shallow humps and dips
    const double TileS = 0.3048, Joint = 0.0004, JointW = 0.004, StepH = 0.00015, Und = 0.0008, Wl = 2.1;
    static readonly double[][] Patches = MakePatches();
    static double[][] MakePatches() { var r = new Random(41); var p = new double[9][]; for (int i = 0; i < 9; i++) p[i] = new[] { -4.3 + r.NextDouble() * 8.6, -2.3 + r.NextDouble() * 4.6, 0.08 + r.NextDouble() * 0.14, (r.NextDouble() < 0.5 ? -1 : 1) * (0.0005 + r.NextDouble() * 0.0007) }; return p; }
    static double Hash2(int i, int j) { unchecked { int h = i * 374761393 + j * 668265263 + 41 * 1442695041; h = (h ^ (int)((uint)h >> 13)) * 1274126177; return (uint)(h ^ (int)((uint)h >> 16)) / 4294967296.0; } }
    public static double MicroH(double x, double z) {
      double u = x / TileS, w = z / TileS; int iu = (int)Math.Floor(u), iw = (int)Math.Floor(w);
      double h = (Hash2(iu, iw) - 0.5) * 2 * StepH;
      double du = Math.Min(u - iu, iu + 1 - u) * TileS, dw = Math.Min(w - iw, iw + 1 - w) * TileS, dj = Math.Min(du, dw), hw = JointW / 2;
      if (dj < hw) h -= Joint * (0.5 + 0.5 * Math.Cos(Math.PI * dj / hw));
      double k = 2 * Math.PI / Wl; h += Und * (0.6 * Math.Sin(x * k + 1.3) * Math.Sin(z * k / 1.3 + 0.4) + 0.4 * Math.Sin((x + z) * k / 0.7));
      foreach (var p in Patches) { double d2 = ((x - p[0]) * (x - p[0]) + (z - p[1]) * (z - p[1])) / (p[2] * p[2]); if (d2 < 4) h += p[3] * Math.Exp(-d2 * 2); }
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
