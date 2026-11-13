// Trackside dressing, as at a club track: old tyres packed on edge along the inside of some bends, and flags of two
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
      // ---- tyres: packed side by side on edge, against the inside barrier of every other bend
      var xf = new List<Transform3D>();
      for (int b = 0; b < c.Bends.Count; b += 2) { var bend = c.Bends[b]; double run = 0; int last = -1;
        for (int k = bend.a; k <= bend.b; k++) { int i = ((k % n) + n) % n; if (t.E[i] > 0.004) continue; if (last >= 0) run += t.Ds; last = i; if (run < 0.031 && xf.Count > 0 && k != bend.a) continue; run = 0;
          double off = bend.side * (t.Hw + 0.055), x = t.X[i] + t.NX[i] * off, z = t.Z[i] + t.NZ[i] * off; if (Clear(x, z) < t.Hw + 0.045) continue;   // (where the inside of the bend is too tight for them they are left out)
          var ty = new Vector3((float)t.TX[i], 0, (float)t.TZ[i]); var nx = new Vector3((float)t.NX[i], 0, (float)t.NZ[i]);
          xf.Add(new Transform3D(new Basis(nx, ty, nx.Cross(ty)), new Vector3((float)x, 0.032f, (float)z))); } }
      if (xf.Count > 0) { var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = new TorusMesh { InnerRadius = 0.017f, OuterRadius = 0.032f, Rings = 14, RingSegments = 6 }, InstanceCount = xf.Count };
        for (int i = 0; i < xf.Count; i++) mm.SetInstanceTransform(i, xf[i]);
        root.AddChild(new MultiMeshInstance3D { Multimesh = mm, MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.03f, 0.03f, 0.032f), Roughness = 0.85f }, Name = "Tyres" }); }
      // ---- flags: round the lap at even steps, on whichever side of the lane has room, the far side from the driver by choice
      var pole = new StandardMaterial3D { AlbedoColor = new Color(0.86f, 0.86f, 0.84f), Roughness = 0.5f };
      StandardMaterial3D Cloth(string tex) => new StandardMaterial3D { AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/" + tex + ".png"), Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor, AlphaScissorThreshold = 0.5f, CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = 0.8f, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
      var cloth = new[] { Cloth("feather_red"), Cloth("banner_white"), Cloth("feather_white"), Cloth("banner_red") }; int made = 0, steps = 14;
      for (int q = 0; q < steps; q++) { int i = (q * n / steps + n / (2 * steps)) % n; if (t.E[i] > 0.004) continue; Vector3 best = Vector3.Zero; double bestD = -1;
        foreach (int sg in new[] { -1, 1 }) { double off = sg * (t.Hw + 0.16), x = t.X[i] + t.NX[i] * off, z = t.Z[i] + t.NZ[i] * off;
          if (Clear(x, z) < t.Hw + 0.13 || Math.Abs(x) > t.HallX - 0.25 || Math.Abs(z) > t.HallZ - 0.25) continue; double d = new Vector3((float)x, 0, (float)z).DistanceTo(driver); if (d < 1.6) continue; if (d > bestD) { bestD = d; best = new Vector3((float)x, 0, (float)z); } }
        if (bestD < 0) continue; int kind = made % 4; made++; var at = new Node3D { Position = best }; at.LookAt(new Vector3(driver.X, 0, driver.Z), Vector3.Up, true); root.AddChild(at);   // each faces the driver, so it can be read
        void Box(Vector3 size, Vector3 pos) => at.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = pole, Position = pos });
        void Quad(Vector2 size, Vector3 pos, Material m) => at.AddChild(new MeshInstance3D { Mesh = new QuadMesh { Size = size }, MaterialOverride = m, Position = pos, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        if (kind % 2 == 0) { // a feather flag: a thin pole 45 cm tall on a small foot, the cloth down one side of it
          Box(new Vector3(0.05f, 0.006f, 0.05f), new Vector3(0, 0.003f, 0)); Box(new Vector3(0.004f, 0.45f, 0.004f), new Vector3(0, 0.225f, 0)); Quad(new Vector2(0.095f, 0.38f), new Vector3(0.0495f, 0.255f, 0.001f), cloth[kind]); }
        else { // a banner: two posts and two rails, the cloth hung between them
          foreach (float sx in new[] { -0.045f, 0.045f }) { Box(new Vector3(0.006f, 0.24f, 0.006f), new Vector3(sx, 0.12f, 0)); Box(new Vector3(0.03f, 0.005f, 0.04f), new Vector3(sx, 0.0025f, 0)); }
          Box(new Vector3(0.096f, 0.005f, 0.005f), new Vector3(0, 0.237f, 0)); Box(new Vector3(0.096f, 0.005f, 0.005f), new Vector3(0, 0.045f, 0)); Quad(new Vector2(0.066f, 0.185f), new Vector3(0, 0.141f, 0.001f), cloth[kind]); } }
      return root;
    }
  }
}
