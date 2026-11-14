// The Warehouse's centrepiece, carried over from the web version: a stack of three full-size car tyres (660 mm across)
// with a fourth standing upright on top, and a tiny whoop (a 100 mm ducted drone) flying a loop through the upright
// tyre's hole and round the stack at about 2.6 m/s.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public sealed class Centrepiece {
    public readonly Node3D Root = new Node3D { Name = "Centrepiece" };
    readonly Node3D _drone = new Node3D(), _body = new Node3D(); readonly List<(Node3D n, int dir)> _props = new();
    readonly Vector3[] _path; readonly float[] _len; float _s; const float Speed = 2.6f;

    /// loop: how big the drone's loop is (1: as in the web version's warehouse; smaller for a smaller room)
    readonly StandardMaterial3D[] _bulbs = new StandardMaterial3D[4]; StandardMaterial3D _led; float _time;
    /// lights: lights out, so the stack is hung with a string of coloured bulbs
    public Centrepiece(Vector3 at, float loop = 1, bool lights = false) {
      // a tyre: its section turned round the axle
      var prof = new[] { (0.2f, -0.1f), (0.27f, -0.105f), (0.31f, -0.09f), (0.325f, -0.06f), (0.33f, 0f), (0.325f, 0.06f), (0.31f, 0.09f), (0.27f, 0.105f), (0.2f, 0.1f) };
      var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles); const int seg = 28;
      for (int i = 0; i < seg; i++) for (int k = 0; k + 1 < prof.Length; k++) { float a0 = i * Mathf.Tau / seg, a1 = (i + 1) * Mathf.Tau / seg;
          Vector3 P(float a, (float r, float y) p) => new Vector3(Mathf.Cos(a) * p.r, p.y, Mathf.Sin(a) * p.r);
          Vector3 p00 = P(a0, prof[k]), p10 = P(a1, prof[k]), p01 = P(a0, prof[k + 1]), p11 = P(a1, prof[k + 1]); foreach (var v in new[] { p00, p01, p10, p10, p01, p11 }) st.AddVertex(v); }
      st.GenerateNormals(); var tyre = st.Commit(); var rubber = new StandardMaterial3D { AlbedoColor = new Color(0.035f, 0.035f, 0.038f), Roughness = 0.85f, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
      for (int i = 0; i < 3; i++) Root.AddChild(new MeshInstance3D { Mesh = tyre, MaterialOverride = rubber, Position = at + new Vector3(Mathf.Sin(i * 2.1f) * 0.025f, 0.105f + i * 0.21f, Mathf.Cos(i * 1.7f) * 0.025f), Rotation = new Vector3(Mathf.Sin(i) * 0.015f, i * 0.9f, 0) });
      float hy = 0.63f + 0.33f; Root.AddChild(new MeshInstance3D { Mesh = tyre, MaterialOverride = rubber, Position = at + new Vector3(0, hy, 0), Rotation = new Vector3(0, 0, Mathf.Pi / 2) });   // upright: its hole faces along x
      if (lights) { // a string of Christmas lights: a dozen bulbs wound up round the stack and eight round the rim of the upright tyre, red, green, gold and blue, on a dark flex
        var cols = new[] { new Color(1f, 0.12f, 0.08f), new Color(0.1f, 1f, 0.25f), new Color(1f, 0.72f, 0.15f), new Color(0.15f, 0.45f, 1f) };
        for (int k = 0; k < 4; k++) _bulbs[k] = new StandardMaterial3D { AlbedoColor = cols[k] * 0.4f, EmissionEnabled = true, Emission = cols[k], EmissionEnergyMultiplier = 2.2f, Roughness = 0.3f };
        var flex = new StandardMaterial3D { AlbedoColor = new Color(0.02f, 0.05f, 0.03f), Roughness = 0.7f }; var bulb = new SphereMesh { Radius = 0.013f, Height = 0.032f, RadialSegments = 10, Rings = 5 };
        var pts = new List<Vector3>(); for (int k = 0; k < 12; k++) { float a = k * 0.85f, y = 0.05f + k * 0.05f; pts.Add(at + new Vector3(Mathf.Cos(a) * 0.342f, y, Mathf.Sin(a) * 0.342f)); }
        for (int k = 0; k < 8; k++) { float a = Mathf.Pi * 0.5f + (k + 0.5f) * Mathf.Tau / 8; pts.Add(at + new Vector3(0.11f * (k % 2 == 0 ? 1 : -1), hy + Mathf.Sin(a) * 0.345f, Mathf.Cos(a) * 0.345f)); }
        for (int k = 0; k < pts.Count; k++) { Root.AddChild(new MeshInstance3D { Mesh = bulb, MaterialOverride = _bulbs[k % 4], Position = pts[k], CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
          if (k + 1 < pts.Count && k != 11) { Vector3 a = pts[k], b = pts[k + 1], d = b - a; var y = d.Normalized(); var x = y.Cross(Vector3.Up).Length() > 0.01f ? y.Cross(Vector3.Up).Normalized() : Vector3.Right; var z = x.Cross(y);
            Root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.0025f, BottomRadius = 0.0025f, Height = d.Length(), RadialSegments = 5, Rings = 1 }, MaterialOverride = flex, Transform = new Transform3D(new Basis(x, y, z), (a + b) / 2), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }); } } }
      // the drone's loop, round the stack: straight through the hole along x, then up and round behind
      var ctl = new[] { new Vector3(-0.75f, 0, 0), new Vector3(0, 0, 0), new Vector3(0.75f, 0, 0), new Vector3(1.55f, 0.2f, -0.35f), new Vector3(2.0f, 0.42f, -1.05f), new Vector3(1.35f, 0.62f, -1.6f), new Vector3(0, 0.7f, -1.55f), new Vector3(-1.35f, 0.55f, -1.45f), new Vector3(-2.0f, 0.32f, -0.9f), new Vector3(-1.55f, 0.1f, -0.28f) };
      int n = ctl.Length; const int per = 40; _path = new Vector3[n * per]; _len = new float[n * per + 1];
      for (int i = 0; i < n; i++) for (int k = 0; k < per; k++) { float t = k / (float)per; Vector3 p0 = ctl[(i + n - 1) % n], p1 = ctl[i], p2 = ctl[(i + 1) % n], p3 = ctl[(i + 2) % n];
          _path[i * per + k] = at + new Vector3(0, hy, 0) + loop * 0.5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (3 * p1 - p0 - 3 * p2 + p3) * t * t * t); }
      for (int i = 0; i < _path.Length; i++) _len[i + 1] = _len[i] + _path[i].DistanceTo(_path[(i + 1) % _path.Length]);
      // the drone: four black ducts with blue-capped motors and clear three-blade props, a purple canopy with the camera
      // tilted up, an antenna, the battery slung underneath, a green light at the back
      Root.AddChild(_drone); _drone.AddChild(_body);
      StandardMaterial3D M(Color c, float rough = 0.5f, float metal = 0) => new StandardMaterial3D { AlbedoColor = c, Roughness = rough, Metallic = metal };
      var blk = M(new Color(0.03f, 0.03f, 0.035f)); var blue = M(new Color(0.12f, 0.66f, 0.85f), 0.25f, 0.6f); var silver = M(new Color(0.72f, 0.74f, 0.77f), 0.25f, 0.8f); var purple = M(new Color(0.6f, 0.18f, 0.78f));
      var blade = new StandardMaterial3D { AlbedoColor = new Color(0.87f, 0.91f, 0.95f, 0.45f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = 0.2f };
      void Add(Node3D p, Mesh m, Material mat, Vector3 pos, Vector3 rot = default) => p.AddChild(new MeshInstance3D { Mesh = m, MaterialOverride = mat, Position = pos, Rotation = rot, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
      foreach (var (x, z) in new[] { (0.026f, 0.026f), (-0.026f, 0.026f), (0.026f, -0.026f), (-0.026f, -0.026f) }) {
        Add(_body, new TorusMesh { InnerRadius = 0.0214f, OuterRadius = 0.0246f, Rings = 24, RingSegments = 6 }, blk, new Vector3(x, 0.002f, z));
        for (int k = 0; k < 3; k++) Add(_body, new BoxMesh { Size = new Vector3(0.023f, 0.0012f, 0.0012f) }, blk, new Vector3(x + Mathf.Cos(k * 2.1f) * 0.0115f, -0.004f, z + Mathf.Sin(k * 2.1f) * 0.0115f), new Vector3(0, -k * 2.1f, 0));
        Add(_body, new CylinderMesh { TopRadius = 0.0045f, BottomRadius = 0.0045f, Height = 0.006f, RadialSegments = 12, Rings = 1 }, silver, new Vector3(x, -0.002f, z));
        Add(_body, new CylinderMesh { TopRadius = 0.0046f, BottomRadius = 0.0046f, Height = 0.0012f, RadialSegments = 12, Rings = 1 }, blue, new Vector3(x, 0.0015f, z));
        var pr = new Node3D { Position = new Vector3(x, 0.003f, z) }; _body.AddChild(pr); _props.Add((pr, x * z > 0 ? 1 : -1));
        for (int k = 0; k < 3; k++) Add(pr, new BoxMesh { Size = new Vector3(0.019f, 0.0004f, 0.0055f) }, blade, new Vector3(Mathf.Cos(k * 2.094f) * 0.0095f, 0, Mathf.Sin(k * 2.094f) * 0.0095f), new Vector3(0, -k * 2.094f + 0.35f, 0)); }
      Add(_body, new BoxMesh { Size = new Vector3(0.03f, 0.002f, 0.03f) }, blk, Vector3.Zero, new Vector3(0, Mathf.Pi / 4, 0)); Add(_body, new BoxMesh { Size = new Vector3(0.02f, 0.003f, 0.02f) }, blk, new Vector3(0, 0.003f, 0));
      Add(_body, new BoxMesh { Size = new Vector3(0.014f, 0.008f, 0.02f) }, purple, new Vector3(0, 0.008f, 0.002f));
      Add(_body, new CylinderMesh { TopRadius = 0.004f, BottomRadius = 0.0045f, Height = 0.008f, RadialSegments = 12, Rings = 1 }, blk, new Vector3(0, 0.012f, 0.012f), new Vector3(Mathf.Pi / 2 - 0.4f, 0, 0));
      Add(_body, new CylinderMesh { TopRadius = 0.0009f, BottomRadius = 0.0009f, Height = 0.03f, RadialSegments = 6, Rings = 1 }, blk, new Vector3(0, 0.014f, -0.014f), new Vector3(-0.75f, 0, 0));
      Add(_body, new BoxMesh { Size = new Vector3(0.012f, 0.006f, 0.03f) }, M(new Color(0.9f, 0.9f, 0.88f)), new Vector3(0, -0.006f, 0));
      Add(_body, new SphereMesh { Radius = 0.0015f, Height = 0.003f, RadialSegments = 6, Rings = 4 }, new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(0.25f, 1f, 0.38f) }, new Vector3(0, 0.004f, -0.012f));
      // a little LED on its belly, cool white, that pulses as it flies: easy to follow in the dark
      _led = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(0.55f, 0.85f, 1f) };
      Add(_body, new SphereMesh { Radius = lights ? 0.006f : 0.003f, Height = lights ? 0.012f : 0.006f, RadialSegments = 8, Rings = 4 }, _led, new Vector3(0, -0.011f, 0.004f));
      Step(0);
    }

    Vector3 At(float s) { float total = _len[_path.Length]; s = ((s % total) + total) % total; int lo = 0, hi = _path.Length; while (hi - lo > 1) { int mid = (lo + hi) / 2; if (_len[mid] <= s) lo = mid; else hi = mid; }
      float f = (s - _len[lo]) / Mathf.Max(1e-6f, _len[lo + 1] - _len[lo]); return _path[lo].Lerp(_path[(lo + 1) % _path.Length], f); }

    public void Step(double dt) {
      if (dt < 0 || dt > 1) dt = 0; _s += Speed * (float)dt; Vector3 p = At(_s), a = At(_s + 0.05f), b = At(_s + 0.25f); var t = (a - p).Normalized(); var t2 = (b - a).Normalized();
      _drone.Position = p + new Vector3(0, Mathf.Sin(_s * 3.1f) * 0.006f, 0);   // a little wobble, as whoops have
      float yaw = Mathf.Atan2(t.X, t.Z), turn = Mathf.AngleDifference(yaw, Mathf.Atan2(t2.X, t2.Z));
      _drone.Rotation = new Vector3(0, yaw, 0); _body.Rotation = new Vector3(0.32f - t.Y * 0.8f, 0, -Mathf.Clamp(turn * 4.5f, -0.7f, 0.7f));   // nose down to fly forward, banked into the turns
      foreach (var (n, dir) in _props) n.RotateY(dir * (float)dt * 160);
      _time += (float)dt; for (int k = 0; k < 4; k++) if (_bulbs[k] != null) _bulbs[k].EmissionEnergyMultiplier = 1.7f + 1.0f * Mathf.Sin(_time * 1.6f + k * 1.57f);   // the bulbs breathe, each colour in its turn
      if (_led != null) { float pulse = 0.75f + 0.25f * Mathf.Sin(_time * 9f); _led.AlbedoColor = new Color(0.55f * pulse, 0.85f * pulse, 1f * pulse); }
    }
  }
}
