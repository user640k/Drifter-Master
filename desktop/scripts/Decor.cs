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
      // ---- CLUB 1: a tyre arch over the right-hand side of the lap, its face to the driver: half a giant tyre standing
      // across the lane, feet outside the boards, DRIFT MASTER round its sidewall, a yellow panel underneath
      if (t.Name == "CLUB 1") { const float R = 0.62f, a = 0.11f, b = 0.085f, ky = 0.8f; const int N = 64, K = 24; var c0 = new Vector3(4.1f, 0, -0.4f);
        var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles); float Sp(float x) => Mathf.Sign(x) * Mathf.Pow(Mathf.Abs(x), 0.55f);
        for (int i = 0; i <= N; i++) for (int k = 0; k <= K; k++) { float u = i / (float)N, v = k / (float)K, th = Mathf.Pi * (1 - u), ph = (v - 0.5f) * Mathf.Tau, rad = R + b * Sp(Mathf.Sin(ph));
            st.SetUV(new Vector2(u, v)); st.AddVertex(c0 + new Vector3(Mathf.Cos(th) * rad, Mathf.Sin(th) * rad * ky, a * Sp(Mathf.Cos(ph)))); }
        for (int i = 0; i < N; i++) for (int k = 0; k < K; k++) { int p = i * (K + 1) + k, q = p + K + 1; st.AddIndex(p); st.AddIndex(q); st.AddIndex(p + 1); st.AddIndex(p + 1); st.AddIndex(q); st.AddIndex(q + 1); }
        st.GenerateNormals();
        var rubber = new StandardMaterial3D { AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/arch.jpg"), RoughnessTexture = GD.Load<Texture2D>("res://assets/tex/arch_rough.jpg"), Roughness = 1f, CullMode = BaseMaterial3D.CullModeEnum.Disabled, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
        root.AddChild(new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = rubber, Name = "Arch" }); }
      // ---- lights out: the LED strips, as glowing ribbons where tools/make_led.py puts their light, and small lights
      // along the lane that reach only the cars (the floor's light is baked; the cars have to be lit as they pass)
      if (Hall.Dark) { var ls = new SurfaceTool(); ls.Begin(Mesh.PrimitiveType.Triangles);
        Color ICE = new(0.35f, 0.85f, 1f), PINK = new(1f, 0.25f, 0.68f), RED = new(1f, 0.12f, 0.08f), WHITE = new(1f, 0.97f, 0.9f), AMB = new(1f, 0.64f, 0.12f), BLUE = new(0.15f, 0.5f, 1f);
        void Rib(IList<Vector3> p, Color col, float w, Vector3? wdir = null) { for (int i = 0; i + 1 < p.Count; i++) { Vector3 a = p[i], b = p[i + 1], dir = (b - a).Normalized(), sd = (wdir ?? new Vector3(dir.Z, 0, -dir.X)) * (w / 2);
            foreach (var v in new[] { a - sd, b - sd, b + sd, a - sd, b + sd, a + sd }) { ls.SetColor(col); ls.AddVertex(v); } } }
        Vector3 E(int i, double side, double off, float y) => new((float)(t.X[i] + t.NX[i] * side * off), y + (float)t.E[i], (float)(t.Z[i] + t.NZ[i] * side * off));
        foreach (var (side, col) in new[] { (1.0, ICE), (-1.0, PINK) }) { var p = new List<Vector3>(); for (int i = 0; i <= n; i++) p.Add(E(i % n, side, t.Hw + (t.Boards ? 0.006 : 0.025), t.Boards ? 0.0515f : 0.0615f)); Rib(p, col, 0.008f); }   // on top of the boards
        if (t.Boards) { var run = new List<Vector3>(); for (int i = 0; i <= n; i++) { int q = i % n; if (i < n && t.KerbAt(q)) run.Add(E(q, t.KerbSide(q), t.Hw - 0.081, (float)Track.KerbH * 0.5f)); else { if (run.Count > 2) Rib(run, RED, 0.006f); run.Clear(); } } }   // under the lip of each kerb
        Rib(new[] { E(0, -1, t.Hw - 0.01, 0.003f), E(0, 1, t.Hw - 0.01, 0.003f) }, WHITE, 0.012f);
        if (t.Name == "CLUB 1") {
          foreach (float back in new[] { 0.4f, 0.62f, 0.84f }) { float x = (float)t.X[0] - back, z = (float)t.Z[0]; Rib(new[] { new Vector3(x + 0.13f, 0.003f, z - 0.24f), new Vector3(x, 0.003f, z), new Vector3(x + 0.13f, 0.003f, z + 0.24f) }, AMB, 0.008f); }
          var arc = new List<Vector3>(); for (int k = 0; k <= 40; k++) { float th = Mathf.DegToRad(10 + 160f * k / 40); arc.Add(new Vector3(4.1f + Mathf.Cos(th) * 0.531f, Mathf.Sin(th) * 0.531f * 0.8f, -0.4f)); } Rib(arc, WHITE, 0.03f, Vector3.Back);   // round the inside of the arch
          foreach (float z in new[] { -2.1f, -1.7f, -1.3f, -0.9f }) Rib(new[] { new Vector3(2.78f, 0.003f, z), new Vector3(3.42f, 0.003f, z) }, new Color(0.85f, 0.93f, 1f), 0.008f);
          Rib(new[] { new Vector3(3.42f, 0.003f, -2.1f), new Vector3(3.42f, 0.003f, -0.9f) }, new Color(0.85f, 0.93f, 1f), 0.008f);
          var ring = new List<Vector3>(); for (int k = 0; k <= 36; k++) ring.Add(new Vector3(-1.75f + 0.2f * Mathf.Cos(k / 36f * Mathf.Tau), 0.003f, -0.845f + 0.2f * Mathf.Sin(k / 36f * Mathf.Tau))); Rib(ring, AMB, 0.01f);
          Rib(new[] { new Vector3(-0.7f, 0.302f, driver.Z - 0.455f), new Vector3(0.6f, 0.302f, driver.Z - 0.455f) }, AMB, 0.008f); }
        // the strips' light on what stands near them: down the faces of the barriers, and low on the walls, in the colour of
        // the nearest strip and fainter with distance (drawn as light added over the surface, brightest next to the strip)
        var ws = new SurfaceTool(); ws.Begin(Mesh.PrimitiveType.Triangles);
        void Wash(Vector3 a, Vector3 b, float h, Color ca0, Color cb0, Color ca1, Color cb1) { Vector3 up = Vector3.Up * h; foreach (var (v, c) in new[] { (a, ca0), (b, cb0), (b + up, cb1), (a, ca0), (b + up, cb1), (a + up, ca1) }) { ws.SetColor(c); ws.AddVertex(v); } }
        foreach (var (side, col) in new[] { (1.0, ICE), (-1.0, PINK) }) foreach (double off in new[] { t.Hw - 0.0008, t.Hw + (t.Boards ? 0.0128 : 0.0508) }) for (int i = 0; i < n; i += 2) { int j = (i + 2) % n; float y0 = 0.001f, y1 = 0.001f;
              Wash(E(i, side, off, y0), E(j, side, off, y1), t.Boards ? 0.049f : 0.059f, col * 0.05f, col * 0.05f, col * 0.42f, col * 0.42f); }
        (Color c, float k) Near(float x, float z) { double best = 1e9; int bi = 0; for (int i = 0; i < n; i += 3) { double dx = x - t.X[i], dz = z - t.Z[i], d = dx * dx + dz * dz; if (d < best) { best = d; bi = i; } }
          double offs = (x - t.X[bi]) * t.NX[bi] + (z - t.Z[bi]) * t.NZ[bi]; float dist = (float)Math.Max(0, Math.Sqrt(best) - t.Hw), k = Mathf.Clamp(1 - (dist - 0.1f) / 2.2f, 0, 1); return (offs > 0 ? ICE : PINK, k * k * k * 0.2f); }
        float hx = (float)t.HallX - 0.012f, hz = (float)t.HallZ - 0.012f; const float step = 0.25f;
        void WallRun(Vector3 from, Vector3 to) { int m = Mathf.Max(1, Mathf.RoundToInt((to - from).Length() / step)); for (int s = 0; s < m; s++) { Vector3 a = from.Lerp(to, s / (float)m), b = from.Lerp(to, (s + 1) / (float)m); var (ca, ka) = Near(a.X, a.Z); var (cb, kb) = Near(b.X, b.Z);
            if (ka + kb > 0.004f) Wash(a + Vector3.Up * 0.1f, b + Vector3.Up * 0.1f, 1.0f, ca * ka, cb * kb, Colors.Black, Colors.Black); } }
        WallRun(new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz)); WallRun(new Vector3(-hx, 0, -hz), new Vector3(-hx, 0, hz)); WallRun(new Vector3(hx, 0, -hz + Room.NookW), new Vector3(hx, 0, hz));
        root.AddChild(new MeshInstance3D { Mesh = ws.Commit(), Name = "LedWash", CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, VertexColorUseAsAlbedo = true, BlendMode = BaseMaterial3D.BlendModeEnum.Add, CullMode = BaseMaterial3D.CullModeEnum.Disabled } });
        root.AddChild(new MeshInstance3D { Mesh = ls.Commit(), Name = "Leds", CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, VertexColorUseAsAlbedo = true, CullMode = BaseMaterial3D.CullModeEnum.Disabled } });
        if (t.Name == "CLUB 1") root.AddChild(new OmniLight3D { Position = new Vector3(3.1f, 0.5f, -1.5f), LightColor = new Color(0.85f, 0.93f, 1f), LightEnergy = 0.5f, OmniRange = 1.6f, ShadowEnabled = false, LightCullMask = 2 }); }   // the cars in the pits, under the bays' strips
      Merge.Static(root);
      return root;
    }
  }
}
