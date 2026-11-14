// Trackside dressing, as at a club track: a few stacks of old tyres, the raised kerbs, and flags of two
// kinds round the outside of the lane (tall feather flags on poles, small banners hung in frames), red and white, with
// DRIFT MASTER on them. All of it stands off the lane, behind the barriers: there to look at, not to hit.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public static class Decor {
    public static Node3D Build(Track t, Course c, Vector3 driver) {
      var root = new Node3D { Name = "Decor" }; int n = t.N;
      // how far a point is from the middle of the nearest piece of lane
      double Clear(double x, double z) { double m = 1e9; for (int i = 0; i < n; i += 2) { double dx = x - t.X[i], dz = z - t.Z[i]; m = Math.Min(m, dx * dx + dz * dz); } return Math.Sqrt(m); }
      // ---- tyres: a few stacks and no more. Old tyres lying flat, one on another, three stacks together on the inside of each
      // of the two tightest bends, just behind the barrier.
      var xf = new List<Transform3D>(); var rnd = new Random(41);
      var spots = new List<(int apex, double side, double tight)>(); int w = Math.Max(2, (int)Math.Round(0.2 / t.Ds));
      foreach (var bend in c.Bends) { int ia = ((bend.apex - w) % n + n) % n, ib = (bend.apex + w) % n; double h = Math.Atan2(t.TX[ib], t.TZ[ib]) - Math.Atan2(t.TX[ia], t.TZ[ia]); h = Math.Abs(Math.Atan2(Math.Sin(h), Math.Cos(h))) / (2 * w * t.Ds);
        double off = bend.side * (t.Hw + 0.14); if (t.E[bend.apex] <= 0.004 && Clear(t.X[bend.apex] + t.NX[bend.apex] * off, t.Z[bend.apex] + t.NZ[bend.apex] * off) > t.Hw + 0.1) spots.Add((bend.apex, bend.side, h)); }
      spots.Sort((a, b) => b.tight.CompareTo(a.tight));
      for (int r = 0; r < Math.Min(2, spots.Count); r++) { var (apex, side, _) = spots[r];
        var o = new Vector3((float)(t.X[apex] + t.NX[apex] * side * (t.Hw + 0.1)), 0, (float)(t.Z[apex] + t.NZ[apex] * side * (t.Hw + 0.1)));
        var tan = new Vector3((float)t.TX[apex], 0, (float)t.TZ[apex]); var nor = new Vector3((float)(t.NX[apex] * side), 0, (float)(t.NZ[apex] * side));
        foreach (var (a, b, high) in new[] { (0f, 0f, 4), (0.07f, 0.012f, 3), (-0.068f, 0.02f, 3) }) for (int k = 0; k < high; k++) {
          var at = o + tan * a + nor * b + new Vector3((float)(rnd.NextDouble() - 0.5) * 0.006f, 0.013f + k * 0.026f, (float)(rnd.NextDouble() - 0.5) * 0.006f);   // each sits a little off the one below
          xf.Add(new Transform3D(new Basis(Vector3.Up, (float)rnd.NextDouble() * 6.28f).Scaled(new Vector3(1, 0.026f / 0.015f, 1)), at)); } }
      if (xf.Count > 0) { var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = new TorusMesh { InnerRadius = 0.017f, OuterRadius = 0.032f, Rings = 14, RingSegments = 6 }, InstanceCount = xf.Count };
        for (int i = 0; i < xf.Count; i++) mm.SetInstanceTransform(i, xf[i]);
        root.AddChild(new MultiMeshInstance3D { Multimesh = mm, MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.03f, 0.03f, 0.032f), Roughness = 0.85f }, Name = "Tyres" }); }
      // ---- kerbs: the red and white strips stand up off the floor (5 mm, ramped on the lane's side), where the floor sheet
      // paints them and the physics feels them
      { var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles); int step = Math.Max(1, (int)Math.Round(0.09 / t.Ds)); bool any = false; float kh = (float)Track.KerbH;
        var prof = new[] { (t.Hw - 0.079, 0.0004f), (t.Hw - 0.062, kh), (t.Hw - 0.004, kh), (t.Hw - 0.003, 0.0004f) };
        for (int i = 0; i < n; i++) { int j = (i + 1) % n; if (!t.KerbAt(i) || !t.KerbAt(j) || t.KerbSide(i) != t.KerbSide(j)) continue; any = true;
          var col = (i / step) % 2 == 0 ? new Color(0.78f, 0.08f, 0.07f) : new Color(0.9f, 0.9f, 0.88f); double sd = t.KerbSide(i);
          Vector3 P(int q, int k) => new Vector3((float)(t.X[q] + t.NX[q] * sd * prof[k].Item1), prof[k].Item2, (float)(t.Z[q] + t.NZ[q] * sd * prof[k].Item1));
          for (int k = 0; k < 3; k++) { Vector3 a = P(i, k), b = P(j, k), cc = P(j, k + 1), d = P(i, k + 1); var nrm = (b - a).Cross(d - a).Normalized(); if (nrm.Y < 0) nrm = -nrm;
            foreach (var v in nrm.Dot((b - a).Cross(cc - a)) < 0 ? new[] { a, b, cc, a, cc, d } : new[] { a, cc, b, a, d, cc }) { st.SetColor(col); st.SetNormal(nrm); st.AddVertex(v); } } }
        if (any) root.AddChild(new MeshInstance3D { Mesh = st.Commit(), Name = "Kerbs", CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, MaterialOverride = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 0.45f, CullMode = BaseMaterial3D.CullModeEnum.Disabled } }); }
      // ---- flags: round the lap at even steps, on whichever side of the lane has room, the far side from the driver by choice
      var pole = new StandardMaterial3D { AlbedoColor = new Color(0.86f, 0.86f, 0.84f), Roughness = 0.5f };
      StandardMaterial3D Cloth(string tex) => new StandardMaterial3D { AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/" + tex + ".png"), Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor, AlphaScissorThreshold = 0.5f, CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = 0.8f, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
      var cloth = new[] { Cloth("feather_red"), Cloth("banner_white"), Cloth("feather_white"), Cloth("banner_red") }; int made = 0, steps = 14;
      var colour = new Dictionary<int, StandardMaterial3D> { { 2, Cloth("feather_blue") }, { 0, Cloth("feather_yellow") }, { 6, Cloth("feather_purple") } };   // three of the feather flags fly other colours
      for (int q = 0; q < steps; q++) { int i = (q * n / steps + n / (2 * steps)) % n; if (t.E[i] > 0.004) continue; Vector3 best = Vector3.Zero; double bestD = -1;
        foreach (int sg in new[] { -1, 1 }) { double off = sg * (t.Hw + 0.16), x = t.X[i] + t.NX[i] * off, z = t.Z[i] + t.NZ[i] * off;
          if (Clear(x, z) < t.Hw + 0.13 || Math.Abs(x) > t.HallX - 0.25 || Math.Abs(z) > t.HallZ - 0.25) continue; double d = new Vector3((float)x, 0, (float)z).DistanceTo(driver); if (d < 1.6) continue; if (d > bestD) { bestD = d; best = new Vector3((float)x, 0, (float)z); } }
        if (bestD < 0) continue; int kind = made % 4; made++; var at = new Node3D { Position = best }; root.AddChild(at); at.LookAtFromPosition(best, new Vector3(driver.X, 0, driver.Z), Vector3.Up, true);   // each faces the driver, so it can be read
        void Box(Vector3 size, Vector3 pos) => at.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = pole, Position = pos });
        void Quad(Vector2 size, Vector3 pos, Material m) => at.AddChild(new MeshInstance3D { Mesh = new QuadMesh { Size = size }, MaterialOverride = m, Position = pos, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        if (kind % 2 == 0) { // a feather flag: a thin pole 45 cm tall on a small foot, the cloth down one side of it
          Box(new Vector3(0.05f, 0.006f, 0.05f), new Vector3(0, 0.003f, 0)); Box(new Vector3(0.004f, 0.45f, 0.004f), new Vector3(0, 0.225f, 0)); Quad(new Vector2(0.095f, 0.38f), new Vector3(0.0495f, 0.255f, 0.001f), colour.TryGetValue(made - 1, out var own) ? own : cloth[kind]); }
        else { // a banner: two posts and two rails, the cloth hung between them
          foreach (float sx in new[] { -0.045f, 0.045f }) { Box(new Vector3(0.006f, 0.24f, 0.006f), new Vector3(sx, 0.12f, 0)); Box(new Vector3(0.03f, 0.005f, 0.04f), new Vector3(sx, 0.0025f, 0)); }
          Box(new Vector3(0.096f, 0.005f, 0.005f), new Vector3(0, 0.237f, 0)); Box(new Vector3(0.096f, 0.005f, 0.005f), new Vector3(0, 0.045f, 0)); Quad(new Vector2(0.066f, 0.185f), new Vector3(0, 0.141f, 0.001f), cloth[kind]); } }
      Merge.Static(root);
      return root;
    }
  }
}
