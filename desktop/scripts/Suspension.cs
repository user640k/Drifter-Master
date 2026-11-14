// The suspension you can see working. The chassis model came with its shocks and lower arms fixed in one pose; here
// they are cut out of it and built again as moving parts (as the web version has them): at each corner a lower arm
// hinged on the chassis that follows its wheel up and down, and a coil-over shock from the tower to the arm whose
// shaft slides and whose spring squeezes. The antenna is taken off the chassis too, and whips with the car.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public sealed class Suspension {
    const float ArmIn = 0.03f, ArmY = 0.02f, TowerY = 0.06f, R = 0.0062f, WheelX = 0.0845f, WR = 0.032f, ArmLen = WheelX - ArmIn - 0.009f, AxleZ = 0.1285f, BodyL = (TowerY - ArmY) * 0.55f;
    sealed class Corner { public Node3D Arm, Shock; public MeshInstance3D Shaft, Cup, Eye, Spring; public float S, Z; public Vector3 Wheel; }
    bool _vis = true; public bool Visible { get => _vis; set { _vis = value; foreach (var c in _c) if (c != null) { c.Arm.Visible = value; c.Shock.Visible = value; } } }
    readonly Corner[] _c = new Corner[4]; readonly Node3D _chassis; MeshInstance3D _ant; Transform3D _antHome; Vector3 _antBase; float _ax, _avx, _az, _avz; ulong _last;
    static readonly Dictionary<Mesh, Mesh> Cut = new(); static Mesh _spring, _armR, _armL;

    // is this point part of a baked shock or lower arm? (u: distance out from the centre line; dz: along the car from the axle)
    static bool Baked(Vector3 v) {
      float u = Mathf.Abs(v.X), dz = Mathf.Abs(Mathf.Abs(v.Z) - AxleZ);
      if (dz <= 0.0098f && u > 0.034f && u < 0.079f && v.Y > 0.019f && v.Y < 0.067f) { // the shock: near the line from the tower mount to the arm mount
        Vector2 a = new(0.048f, 0.06f), b = new(0.0624f, 0.027f), p = new(u, v.Y), ab = b - a; float t = Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), -0.12f, 1.15f);
        if ((p - (a + ab * t)).Length() <= 0.0108f) return true; }
      return dz <= 0.0215f && u > 0.0283f && u < 0.0805f && v.Y > 0.0165f && v.Y < 0.031f;   // the arm and its hinge pin
    }

    public static Suspension Build(Node3D chassis, Vector3[] wheels) {
      if (!(chassis.FindChild("Chassis_m_26272b", true, false) is MeshInstance3D)) return null;   // (no springs in this model: leave it alone)
      var s = new Suspension(chassis, wheels); return s;
    }

    Suspension(Node3D chassis, Vector3[] wheels) {
      _chassis = chassis;
      Material M(string hex, Color fallback) => (chassis.FindChild("Chassis_m_" + hex, true, false) as MeshInstance3D)?.GetActiveMaterial(0) ?? new StandardMaterial3D { AlbedoColor = fallback };
      Material black = M("151517", new Color(0.08f, 0.08f, 0.09f)), red = M("c8202a", new Color(0.78f, 0.13f, 0.16f)), coil = M("26272b", new Color(0.15f, 0.15f, 0.17f)), steel = M("c8ccd2", new Color(0.78f, 0.8f, 0.82f)), nylon = M("1d1d20", new Color(0.11f, 0.11f, 0.13f));
      Material nylon2 = nylon is BaseMaterial3D bm ? (Material)bm.Duplicate() : nylon; if (nylon2 is BaseMaterial3D b2) b2.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
      // cut the fixed shocks and arms out of the chassis meshes (once for each mesh: every car of this body shares the result)
      foreach (var node in chassis.FindChildren("*", "MeshInstance3D", true, false)) { var mi = (MeshInstance3D)node; if (!(mi.Mesh is ArrayMesh am) || am.GetSurfaceCount() != 1) continue;
        if (mi.Name.ToString().EndsWith("101010")) { _ant = mi; continue; }
        if (!Cut.TryGetValue(am, out var cut)) { var arr = am.SurfaceGetArrays(0); var v = (Vector3[])arr[(int)Mesh.ArrayType.Vertex]; var idx = arr[(int)Mesh.ArrayType.Index].VariantType == Variant.Type.Nil ? null : (int[])arr[(int)Mesh.ArrayType.Index];
          if (idx == null) { idx = new int[v.Length]; for (int i = 0; i < idx.Length; i++) idx[i] = i; }
          var keep = new List<int>(idx.Length); for (int i = 0; i + 2 < idx.Length; i += 3) if (!(Baked(v[idx[i]]) && Baked(v[idx[i + 1]]) && Baked(v[idx[i + 2]]))) { keep.Add(idx[i]); keep.Add(idx[i + 1]); keep.Add(idx[i + 2]); }
          if (keep.Count == idx.Length) cut = am; else { var nm = new ArrayMesh(); if (keep.Count > 0) { arr[(int)Mesh.ArrayType.Index] = keep.ToArray(); nm.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr); nm.SurfaceSetMaterial(0, am.SurfaceGetMaterial(0)); } cut = nm; }
          Cut[am] = cut; }
        if (cut != am) { var mat = mi.GetActiveMaterial(0); mi.Mesh = cut; if (cut.GetSurfaceCount() == 0) mi.Visible = false; else if (mi.MaterialOverride == null && cut.SurfaceGetMaterial(0) == null) mi.MaterialOverride = mat; } }
      if (_ant != null) { _antHome = _ant.Transform; var bb = _ant.GetAabb(); _antBase = new Vector3(bb.Position.X + bb.Size.X / 2, bb.Position.Y, bb.Position.Z + bb.Size.Z / 2); }
      _spring ??= SpringMesh(); _armR ??= ArmMesh(1); _armL ??= ArmMesh(-1);
      MeshInstance3D Cyl(Node3D p, float rTop, float rBot, float h, Material m, float y, int seg = 14) { var mi = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = rTop, BottomRadius = rBot, Height = h, RadialSegments = seg, Rings = 1 }, MaterialOverride = m, Position = new Vector3(0, y, 0) }; p.AddChild(mi); return mi; }
      for (int i = 0; i < 4; i++) { var c = new Corner { S = Mathf.Sign(wheels[i].X), Z = Mathf.Sign(wheels[i].Z) * AxleZ, Wheel = wheels[i] }; _c[i] = c;
        c.Arm = new Node3D { Position = new Vector3(c.S * ArmIn, ArmY, c.Z), Name = "Arm" + i }; chassis.AddChild(c.Arm);
        c.Arm.AddChild(new MeshInstance3D { Mesh = c.S > 0 ? _armR : _armL, MaterialOverride = nylon2 });
        c.Arm.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.0016f, BottomRadius = 0.0016f, Height = 0.04f, RadialSegments = 8, Rings = 1 }, MaterialOverride = steel, Rotation = new Vector3(Mathf.Pi / 2, 0, 0) });   // hinge pin
        c.Shock = new Node3D { Name = "Shock" + i }; chassis.AddChild(c.Shock);
        Cyl(c.Shock, R * 0.7f, R * 0.7f, 0.004f, steel, -0.002f, 10);                      // top eyelet
        Cyl(c.Shock, R * 1.15f, R * 1.15f, 0.004f, red, -0.006f);                          // threaded cap
        Cyl(c.Shock, R, R, BodyL, black, -0.006f - BodyL / 2);                             // body
        Cyl(c.Shock, R * 1.25f, R * 1.25f, 0.002f, red, -0.006f - BodyL * 0.35f);          // preload collar
        c.Shaft = Cyl(c.Shock, R * 0.28f, R * 0.28f, 1, steel, 0, 8); c.Cup = Cyl(c.Shock, R * 1.3f, R * 1.1f, 0.0022f, red, 0); c.Eye = Cyl(c.Shock, R * 0.55f, R * 0.55f, 0.005f, nylon, 0, 10);
        c.Spring = new MeshInstance3D { Mesh = _spring, MaterialOverride = coil }; c.Shock.AddChild(c.Spring); }
      foreach (var c in _c) foreach (var n in new[] { c.Arm, c.Shock }) foreach (var ch in n.GetChildren()) if (ch is MeshInstance3D m) m.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;   // (too small to throw a shadow worth drawing)
      Update(Transform3D.Identity, 0, 0);
    }

    // the coil, one unit long, hanging down from y = 0: scaled along y to the length it has to fill
    static Mesh SpringMesh() { const int turns = 9, per = 16, sides = 5; float rc = R * 1.3f, rw = R * 0.16f; var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles);
      Vector3 P(int i, int k) { float a = i / (float)per * Mathf.Tau, b = k / (float)sides * Mathf.Tau; var c = new Vector3(Mathf.Cos(a) * rc, -i / (float)(turns * per), Mathf.Sin(a) * rc); var rad = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
        return c + rad * (Mathf.Cos(b) * rw) + Vector3.Up * (Mathf.Sin(b) * rw * 55f); }   // (the wire is drawn 55 times too tall here: the mesh is squeezed to about 1/55 of this when it is used)
      Vector3 Nrm(int i, int k) { float a = i / (float)per * Mathf.Tau, b = k / (float)sides * Mathf.Tau; return (new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * Mathf.Cos(b) + Vector3.Up * Mathf.Sin(b)).Normalized(); }
      for (int i = 0; i < turns * per; i++) for (int k = 0; k < sides; k++) { int k2 = (k + 1) % sides;
          foreach (var (ii, kk) in new[] { (i, k), (i + 1, k), (i + 1, k2), (i, k), (i + 1, k2), (i, k2) }) { st.SetNormal(Nrm(ii, kk)); st.AddVertex(P(ii, kk)); } }
      return st.Commit(); }

    // the lower arm: a flat wishbone, 4 mm thick, from the hinge (x = 0) out to the ball joint
    static Mesh ArmMesh(int s) { var pts = new[] { new Vector2(0, -0.017f), new Vector2(ArmLen, -0.004f), new Vector2(ArmLen, 0.004f), new Vector2(0, 0.017f), new Vector2(0.008f, 0.002f), new Vector2(0.008f, -0.002f) };
      var tri = Geometry2D.TriangulatePolygon(pts); var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles);
      Vector3 V(int i, float y) => new Vector3(pts[i].X * s, y, pts[i].Y);
      foreach (float y in new[] { 0.002f, -0.002f }) { st.SetNormal(y > 0 ? Vector3.Up : Vector3.Down); foreach (int i in tri) st.AddVertex(V(i, y)); }
      for (int i = 0; i < pts.Length; i++) { int j = (i + 1) % pts.Length; Vector3 a = V(i, 0.002f), b = V(j, 0.002f), c = V(j, -0.002f), d = V(i, -0.002f); st.SetNormal((b - a).Cross(d - a).Normalized() * -s); foreach (var p in new[] { a, b, c, a, c, d }) st.AddVertex(p); }
      return st.Commit(); }

    /// body: the body's place over the wheels (from the car's own frame to the chassis'); ax, ay: the car's acceleration, forward and sideways
    public void Update(Transform3D chassisFromCar, float ax, float ay) {
      foreach (var c in _c) { if (c == null) continue;
        float wy = (chassisFromCar * c.Wheel).Y, th = Mathf.Asin(Mathf.Clamp((wy - WR) / ArmLen, -0.5f, 0.5f)), d = ArmLen * 0.72f;
        c.Arm.Rotation = new Vector3(0, 0, c.S * th);
        Vector3 a = new(c.S * (ArmIn + 0.018f), TowerY, c.Z), b = new(c.S * (ArmIn + d * Mathf.Cos(th)), ArmY + d * Mathf.Sin(th) + 0.004f, c.Z); float L = (b - a).Length();
        c.Shock.Transform = new Transform3D(new Basis(new Quaternion(Vector3.Down, (b - a) / L)), a);
        float bodyEnd = -0.006f - BodyL, len = L + bodyEnd - 0.003f, top0 = -0.006f - BodyL * 0.35f, cupY = -L + 0.006f;
        c.Shaft.Position = new Vector3(0, bodyEnd - len / 2, 0); c.Shaft.Scale = new Vector3(1, Mathf.Max(0.001f, len), 1); c.Cup.Position = new Vector3(0, cupY, 0); c.Eye.Position = new Vector3(0, -L, 0);
        c.Spring.Position = new Vector3(0, top0 - 0.001f, 0); c.Spring.Scale = new Vector3(1, Mathf.Max(0.005f, top0 - 0.001f - cupY), 1); }
      if (_ant != null) { // the antenna: a thin whip sprung at its foot, left behind by every change of speed and thrown outward in a bend
        ulong now = Time.GetTicksMsec(); float dt = _last == 0 ? 0 : Mathf.Min(0.05f, (now - _last) / 1000f); _last = now;
        for (float t = 0; t < dt; t += 0.004f) { float h = Mathf.Min(0.004f, dt - t); const float w = 38f, z = 0.12f;
          _avx += (w * w * (-ax * 0.012f - _ax) - 2 * z * w * _avx) * h; _ax += _avx * h; _avz += (w * w * (ay * 0.012f - _az) - 2 * z * w * _avz) * h; _az += _avz * h; }
        _ax = Mathf.Clamp(_ax, -0.35f, 0.35f); _az = Mathf.Clamp(_az, -0.35f, 0.35f);
        _ant.Transform = new Transform3D(Basis.Identity, _antBase) * new Transform3D(new Basis(Vector3.Right, _ax) * new Basis(Vector3.Back, _az), Vector3.Zero) * new Transform3D(Basis.Identity, -_antBase) * _antHome; }
    }
  }
}
