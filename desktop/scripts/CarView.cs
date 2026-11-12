// The car on screen: the sedan model (exported from the browser version), posed from the physics each frame.
// Model axes: +z forward, +y up, +x left. Body roll, pitch and heave move the shell and chassis, not the wheels.
using Godot;
using System;

namespace DriftMaster {
  public partial class CarView : Node3D {
    MeshInstance3D _blob; Node3D _body; readonly Node3D[] _wheel = new Node3D[4], _spin = new Node3D[4]; readonly Quaternion[] _camber = new Quaternion[4]; readonly Transform3D[] _wheelBase = new Transform3D[4]; readonly float[] _spinSign = new float[4];
    public StandardMaterial3D Paint; StandardMaterial3D _glass;
    public Node3D Fpv; // the on-board camera's seat: inside the cabin, on the centre line, behind the windscreen; it leans with the body
    public static float FpvX = 0, FpvY = 0.1f, FpvZ = -0.016f, FpvTilt = 6f;
    static readonly string[] Names = { "WheelFa", "WheelFb", "WheelRa", "WheelRb" };

    public override void _Ready() {
      var model = GD.Load<PackedScene>("res://assets/sedan.glb").Instantiate<Node3D>(); AddChild(model);
      _body = new Node3D { Name = "Body" }; AddChild(_body);
      foreach (var n in new[] { "Shell", "Chassis", "Mounts" }) { var p = model.FindChild(n, true, false) as Node3D; if (p == null) { GD.PrintErr("missing ", n); continue; } var xf = p.Transform; p.GetParent().RemoveChild(p); _body.AddChild(p); p.Transform = xf; }
      for (int i = 0; i < 4; i++) {
        _wheel[i] = model.FindChild(Names[i], true, false) as Node3D; _spin[i] = model.FindChild(Names[i] + "_Spin", true, false) as Node3D;
        if (_spin[i] != null) _camber[i] = _spin[i].Quaternion;
        if (_wheel[i] != null) { _wheelBase[i] = _wheel[i].Transform; _spinSign[i] = Mathf.Sign(_wheelBase[i].Basis.Z.X); } // the wheel's own z is its axle
      }
      // a soft dark patch under the car: cheap contact shadow that reads on any graphics card
      { var img = Image.CreateEmpty(64, 32, false, Image.Format.Rgba8);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 64; x++) { float u = (x - 31.5f) / 31.5f, v = (y - 15.5f) / 15.5f, d = Mathf.Pow(Mathf.Pow(Mathf.Abs(u), 3) + Mathf.Pow(Mathf.Abs(v), 3), 1 / 3f); img.SetPixel(x, y, new Color(0, 0, 0, 0.62f * Mathf.SmoothStep(1, 0.45f, d))); }
        var bm = new StandardMaterial3D { AlbedoTexture = ImageTexture.CreateFromImage(img), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, NoDepthTest = false, DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled, RenderPriority = -1 };
        _blob = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(0.27f, 0.5f) }, MaterialOverride = bm, Position = new Vector3(0, 0.003f, 0), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }; AddChild(_blob); }
      // car paint: a deep colour under a clear coat; glass: dark tint
      Fpv = new Node3D { Name = "Fpv", Position = new Vector3(FpvX, FpvY, FpvZ), Rotation = new Vector3(Mathf.DegToRad(FpvTilt), Mathf.Pi, 0) }; _body.AddChild(Fpv);
      Paint = new StandardMaterial3D { CullMode = BaseMaterial3D.CullModeEnum.Disabled, AlbedoColor = new Color(0.09f, 0.094f, 0.106f), Roughness = 0.42f, Metallic = 0.25f, ClearcoatEnabled = true, Clearcoat = 0.8f, ClearcoatRoughness = 0.12f };
      var glass = _glass = new StandardMaterial3D { AlbedoColor = new Color(0.05f, 0.065f, 0.085f, 0.8f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 0.08f, Metallic = 0.3f, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
      foreach (var node in FindChildren("*", "MeshInstance3D", true, false)) {
        var mi = (MeshInstance3D)node; string nm = mi.Name.ToString();
        if (nm.EndsWith("_paint")) mi.MaterialOverride = Paint; else if (nm.Contains("_glass_")) mi.MaterialOverride = glass;
        else if (nm.StartsWith("Shell_") && mi.Mesh != null) for (int si = 0; si < mi.Mesh.GetSurfaceCount(); si++) if (mi.Mesh.SurfaceGetMaterial(si) is BaseMaterial3D bm0) { var d = (BaseMaterial3D)bm0.Duplicate(); d.CullMode = BaseMaterial3D.CullModeEnum.Disabled; mi.SetSurfaceOverrideMaterial(si, d); } // the shell is seen from inside too
        if (mi != _blob) mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
      }
    }

    // from the driver's seat the tint is light enough to see the track through
    public void SetOnBoard(bool on) { _glass.AlbedoColor = new Color(0.05f, 0.065f, 0.085f, on ? 0.16f : 0.8f); }

    public void Pose(CarPhysics c, Track t) {
      // nose up or down with the slope under the two axles
      double fx = Math.Sin(c.Hd), fz = Math.Cos(c.Hd);
      double gf = t.GroundAt(c.X + fx * 0.1285, c.Z + fz * 0.1285), gr = t.GroundAt(c.X - fx * 0.1285, c.Z - fz * 0.1285);
      float slope = c.Air ? 0 : (float)Math.Atan2(gf - gr, 0.257);
      float y = c.Air ? (float)c.Y : (float)((gf + gr) / 2);
      Transform = new Transform3D(new Basis(Vector3.Up, (float)c.Hd) * new Basis(Vector3.Right, -slope), new Vector3((float)c.X, y, (float)c.Z));
      _body.Transform = new Transform3D(new Basis(Vector3.Right, -(float)c.Pitch) * new Basis(Vector3.Back, (float)c.Roll * 1.6f), new Vector3(0, (float)c.Heave, 0));
      _blob.Visible = !c.Air;
      for (int i = 0; i < 4; i++) {
        if (_wheel[i] == null || _spin[i] == null) continue;
        bool front = i < 2; _wheel[i].Transform = new Transform3D(new Basis(Vector3.Up, front ? (float)c.Steer : 0) * _wheelBase[i].Basis, _wheelBase[i].Origin);
        _spin[i].Quaternion = _camber[i] * new Quaternion(Vector3.Back, _spinSign[i] * (float)((front ? c.SpinF : c.Spin) % (Math.PI * 2)));
      }
    }
  }
}
