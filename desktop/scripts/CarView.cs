// The car on screen: the sedan model (exported from the browser version), posed from the physics each frame.
// Model axes: +z forward, +y up, +x left. Body roll, pitch and heave move the shell and chassis, not the wheels.
using Godot;
using System;

namespace DriftMaster {
  public partial class CarView : Node3D {
    MeshInstance3D _blob, _neonFloorNode; Node3D _body; Transform3D? _shellHome; readonly Node3D[] _wheel = new Node3D[4], _spin = new Node3D[4]; readonly Quaternion[] _camber = new Quaternion[4]; readonly Transform3D[] _wheelBase = new Transform3D[4]; readonly float[] _spinSign = new float[4];
    public ShaderMaterial Paint; StandardMaterial3D _glass;
    /// the shells: name, model, its mass (kg) and how high its own centre of gravity sits (m), and where the cabin is (z)
    public static readonly (string name, string file, double kg, double cgUp, float cabZ)[] Bodies = {
      ("SEDAN", "sedan", 0.130, 0.068, 0.006f), ("COUPE", "coupe", 0.115, 0.060, -0.012f), ("PICKUP", "pickup", 0.165, 0.074, 0.03f) };
    public int Body; public float RoofY = 0.133f, HalfW = 0.095f, HalfL = 0.21f;
    public static readonly string[] Liveries = { "SOLID", "BONNET", "FADE", "FLAMES", "CAMO", "STREAKS", "HOLOGRAM" };
    // options carried over from the web version: the body shell on or off, neon under the car, tinted or clear windows
    Node3D _shell, _neon; StandardMaterial3D _neonBar, _neonFloor; OmniLight3D _neonLight; bool _clear, _onBoard;
    public static readonly (string name, Color col)[] Neons = { ("OFF", Colors.Black), ("PINK", new Color(1f, 0.25f, 0.71f)), ("ICE BLUE", new Color(0.25f, 0.8f, 1f)), ("GREEN", new Color(0.3f, 1f, 0.45f)), ("AMBER", new Color(1f, 0.62f, 0.12f)), ("PURPLE", new Color(0.62f, 0.3f, 1f)), ("WHITE", new Color(1f, 0.97f, 0.9f)) };
    public Node3D Fpv; // the on-board camera's seat: inside the cabin, on the centre line, behind the windscreen; it leans with the body
    public Node3D StickCam; Node3D _stick; // a second FPV camera on a short stick: fixed to the roof, reaching back, looking forward over the car
    public static float StickY = 0.195f, StickZ = -0.5f, StickTilt = 7f; // 19.5 cm up: it has to pass under the bridge (23 cm)
    public static float FpvX = 0, FpvY = 0.102f, FpvZ = 0.006f, FpvTilt = 5f;
    static readonly string[] Names = { "WheelFa", "WheelFb", "WheelRa", "WheelRb" };

    public override void _Ready() {
      var model = GD.Load<PackedScene>("res://assets/" + Bodies[Body % Bodies.Length].file + ".glb").Instantiate<Node3D>(); AddChild(model);
      _body = new Node3D { Name = "Body" }; AddChild(_body);
      foreach (var n in new[] { "Shell", "Chassis", "Mounts" }) { var p = model.FindChild(n, true, false) as Node3D; if (p == null) { GD.PrintErr("missing ", n); continue; } var xf = p.Transform; p.GetParent().RemoveChild(p); _body.AddChild(p); p.Transform = xf; if (n == "Shell") _shell = p; }
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
      { // neon under the car: two lit tubes along the chassis, their light on the floor, and a little of it on the car
        _neon = new Node3D { Name = "Neon", Visible = false }; _body.AddChild(_neon);
        _neonBar = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        foreach (int sd in new[] { -1, 1 }) _neon.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.006f, 0.005f, 0.17f) }, MaterialOverride = _neonBar, Position = new Vector3(sd * 0.044f, 0.012f, 0), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        var img = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { float u = (x - 31.5f) / 31.5f, v = (y - 31.5f) / 31.5f, d = Mathf.Pow(Mathf.Pow(Mathf.Abs(u), 2.6f) + Mathf.Pow(Mathf.Abs(v), 2.6f), 1 / 2.6f), a = Mathf.SmoothStep(1, 0.25f, d); img.SetPixel(x, y, new Color(1, 1, 1, a * a)); }
        _neonFloor = new StandardMaterial3D { AlbedoTexture = ImageTexture.CreateFromImage(img), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha, BlendMode = BaseMaterial3D.BlendModeEnum.Add, DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled };
        var floor = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(0.42f, 0.62f) }, MaterialOverride = _neonFloor, Position = new Vector3(0, 0.0045f, 0), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Name = "NeonFloor" };
        AddChild(floor); floor.Visible = false; _neonFloorNode = floor; // on the car's root, not its body: the light lies on the floor and does not lean
        _neonLight = new OmniLight3D { Position = new Vector3(0, 0.02f, 0), OmniRange = 0.45f, OmniAttenuation = 1.2f, LightEnergy = 0.9f, ShadowEnabled = false }; _neon.AddChild(_neonLight);
      }
      // car paint: a deep colour under a clear coat; glass: dark tint
      Fpv = new Node3D { Name = "Fpv", Position = new Vector3(FpvX, FpvY, FpvZ), Rotation = new Vector3(Mathf.DegToRad(FpvTilt), Mathf.Pi, 0) }; _body.AddChild(Fpv);
      { // the stick: a little foot on the roof, a thin carbon rod up and back, the camera at its end
        _stick = new Node3D { Name = "Stick", Visible = false }; _body.AddChild(_stick);
        var carbon = new StandardMaterial3D { AlbedoColor = new Color(0.05f, 0.05f, 0.055f), Roughness = 0.45f };
        Vector3 foot = new Vector3(0, 0.131f, -0.045f), cam = new Vector3(0, StickY, StickZ), end = cam + new Vector3(0, -0.022f, 0.012f), d = end - foot; // the camera sits on top of the rod's tip
        var y = d.Normalized(); var x = y.Cross(Vector3.Right).Normalized(); var z = x.Cross(y);
        _stick.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.002f, BottomRadius = 0.002f, Height = d.Length(), RadialSegments = 8, Rings = 1 }, MaterialOverride = carbon, Transform = new Transform3D(new Basis(x, y, z), (foot + end) / 2) });
        _stick.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.03f, 0.006f, 0.03f) }, MaterialOverride = carbon, Position = foot });
        StickCam = new Node3D { Name = "StickCam", Position = cam, Rotation = new Vector3(-Mathf.DegToRad(StickTilt), Mathf.Pi, 0) }; _body.AddChild(StickCam); // looking down a little, at the car and the track ahead
      }
      Paint = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/paint.gdshader") };
      var glass = _glass = new StandardMaterial3D { AlbedoColor = new Color(0.05f, 0.065f, 0.085f, 0.8f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 0.08f, Metallic = 0.3f, CullMode = BaseMaterial3D.CullModeEnum.Disabled };
      foreach (var node in FindChildren("*", "MeshInstance3D", true, false)) {
        var mi = (MeshInstance3D)node; string nm = mi.Name.ToString();
        if (nm.EndsWith("_paint")) { mi.MaterialOverride = Paint;
          // the painted shell's size: the livery is laid out on it, and the cameras and the handling take the roof height
          var bb = mi.GetAabb(); Paint.SetShaderParameter("dim", new Vector3(bb.Size.X / 2, bb.Size.Y, bb.Size.Z / 2)); Paint.SetShaderParameter("origin", new Vector3(bb.Position.X + bb.Size.X / 2, bb.Position.Y, bb.Position.Z + bb.Size.Z / 2));
          var sx = _shell != null ? _shell.Scale : Vector3.One; float sy = _shell != null ? _shell.Position.Y : 0;
          RoofY = (bb.Position.Y + bb.Size.Y) * sx.Y + sy; HalfW = bb.Size.X / 2 * sx.X; HalfL = bb.Size.Z / 2 * sx.Z; }
        else if (nm.Contains("_glass_")) mi.MaterialOverride = glass;
        else if (nm.StartsWith("Shell_") && mi.Mesh != null) for (int si = 0; si < mi.Mesh.GetSurfaceCount(); si++) if (mi.Mesh.SurfaceGetMaterial(si) is BaseMaterial3D bm0) { var d = (BaseMaterial3D)bm0.Duplicate(); d.CullMode = BaseMaterial3D.CullModeEnum.Disabled; mi.SetSurfaceOverrideMaterial(si, d); } // the shell is seen from inside too
        if (mi != _blob && mi != _neonFloorNode && !_stick.IsAncestorOf(mi) && !_neon.IsAncestorOf(mi)) mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
      }
      // the cameras ride where this shell's cabin and roof are
      float cab = Bodies[Body % Bodies.Length].cabZ;
      Fpv.Position = new Vector3(FpvX, RoofY - 0.031f, cab); StickCam.Position = new Vector3(0, Math.Max(StickY, RoofY + 0.045f), StickZ);
      foreach (var ch in _stick.GetChildren()) ch.QueueFree();
      { var carbon = new StandardMaterial3D { AlbedoColor = new Color(0.05f, 0.05f, 0.055f), Roughness = 0.45f };
        Vector3 foot = new Vector3(0, RoofY - 0.002f, cab - 0.05f), end = StickCam.Position + new Vector3(0, -0.022f, 0.012f), d = end - foot;
        var y = d.Normalized(); var x = y.Cross(Vector3.Right).Normalized(); var z = x.Cross(y);
        _stick.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.002f, BottomRadius = 0.002f, Height = d.Length(), RadialSegments = 8, Rings = 1 }, MaterialOverride = carbon, Transform = new Transform3D(new Basis(x, y, z), (foot + end) / 2), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        _stick.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.03f, 0.006f, 0.03f) }, MaterialOverride = carbon, Position = foot, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }); }
    }

    // from the driver's seat the tint is light enough to see the track through
    public void SetPaint(Color a, Color b, Color c, int scheme) { Paint.SetShaderParameter("col_a", a); Paint.SetShaderParameter("col_b", b); Paint.SetShaderParameter("col_c", c); Paint.SetShaderParameter("scheme", scheme); }
    public void ShowStick(bool on) { _stick.Visible = on; }
    public void SetOnBoard(bool on) { _onBoard = on; Glass(); }
    // tinted: dark, as on most shells; clear: the chassis and the cabin show through
    public void SetWindows(bool clear) { _clear = clear; Glass(); }
    void Glass() { _glass.AlbedoColor = _clear ? new Color(0.75f, 0.82f, 0.88f, 0.1f) : new Color(0.05f, 0.065f, 0.085f, _onBoard ? 0.16f : 0.8f); _glass.Metallic = _clear ? 0.1f : 0.3f; }
    // shell off: the bare chassis, as on a setup stand (the body posts stay)
    public void SetShell(bool on) { if (_shell != null) _shell.Visible = on; }
    public void SetNeon(int i) {
      var c = Neons[i % Neons.Length].col; bool on = i % Neons.Length != 0; _neon.Visible = on; _neonFloorNode.Visible = on; if (!on) return;
      _neonBar.AlbedoColor = c.Lerp(Colors.White, 0.45f); _neonFloor.AlbedoColor = new Color(c.R, c.G, c.B, 0.55f); _neonLight.LightColor = c;
    }

    public void Pose(CarPhysics c, Track t) {
      // nose up or down with the slope under the two axles
      double fx = Math.Sin(c.Hd), fz = Math.Cos(c.Hd);
      double gf = t.GroundAt(c.X + fx * 0.1285, c.Z + fz * 0.1285), gr = t.GroundAt(c.X - fx * 0.1285, c.Z - fz * 0.1285);
      float slope = c.Air ? 0 : (float)Math.Atan2(gf - gr, 0.257);
      float y = c.Air ? (float)c.Y : (float)((gf + gr) / 2);
      Transform = new Transform3D(new Basis(Vector3.Up, (float)c.Hd) * new Basis(Vector3.Right, -slope), new Vector3((float)c.X, y, (float)c.Z));
      // the shell shivers on its posts over the bumps: shown three times life size, or it could not be seen (a third of a millimetre)
      if (_shell != null) { if (!_shellHome.HasValue) _shellHome = _shell.Transform; var h0 = _shellHome.Value; _shell.Transform = new Transform3D(new Basis(Vector3.Back, (float)c.ShellR * 3f) * h0.Basis, h0.Origin + new Vector3(0, (float)c.ShellZ * 3f, 0)); }
      _body.Transform = new Transform3D(new Basis(Vector3.Right, -(float)c.Pitch) * new Basis(Vector3.Back, (float)c.Roll * 1.6f), new Vector3(0, (float)c.Heave, 0));
      _blob.Visible = !c.Air; if (_neonFloorNode.Visible != (_neon.Visible && !c.Air)) _neonFloorNode.Visible = _neon.Visible && !c.Air;
      for (int i = 0; i < 4; i++) {
        if (_wheel[i] == null || _spin[i] == null) continue;
        bool front = i < 2; _wheel[i].Transform = new Transform3D(new Basis(Vector3.Up, front ? (float)c.Steer : 0) * _wheelBase[i].Basis, _wheelBase[i].Origin);
        _spin[i].Quaternion = _camber[i] * new Quaternion(Vector3.Back, _spinSign[i] * (float)((front ? c.SpinF : c.Spin) % (Math.PI * 2)));
      }
    }
  }
}
