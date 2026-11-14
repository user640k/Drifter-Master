// The car on screen: the sedan model (exported from the browser version), posed from the physics each frame.
// Model axes: +z forward, +y up, +x left. Body roll, pitch and heave move the shell and chassis, not the wheels.
using Godot;
using System;

namespace DriftMaster {
  public partial class CarView : Node3D {
    MeshInstance3D _blob, _neonFloorNode; Node3D _body; Transform3D? _shellHome; readonly Node3D[] _wheel = new Node3D[4], _spin = new Node3D[4]; readonly Quaternion[] _camber = new Quaternion[4]; readonly float[] _leanIn = new float[4]; readonly Transform3D[] _wheelBase = new Transform3D[4]; readonly float[] _spinSign = new float[4];
    public ShaderMaterial Paint; ShaderMaterial _glass; StandardMaterial3D _rimMat; MeshInstance3D _banner;
    /// the shells: name, model, its mass (kg) and how high its own centre of gravity sits (m), and where the cabin is (z)
    public static readonly (string name, string file, double kg, double cgUp, float cabZ)[] Bodies = {
      ("SEDAN", "sedan", 0.130, 0.068, 0.006f), ("COUPE", "coupe", 0.115, 0.060, -0.012f), ("PICKUP", "pickup", 0.165, 0.074, 0.03f), ("TUNER", "tuner", 0.120, 0.060, 0.02f), ("VAN", "zuk", 0.175, 0.090, 0.10f) };
    public float RideOffset; /* ride height above or below 7 mm (m) */
    public bool Detail = true; int _layTick; MeshInstance3D _proxy; StandardMaterial3D _proxyMat;
    // which camera sees what: the car's own meshes are on layer 2 (every camera but the television's), a plain block in
    // the body colour on layer 3 (the television only). What is under the shell casts no shadow while the shell is on.
    void Lay() {
      if (_proxy == null && !Ghost) { _proxyMat = new StandardMaterial3D { AlbedoColor = new Color(0.5f, 0.5f, 0.5f), Roughness = 0.5f }; _proxy = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(HalfW * 2, 0.09f, HalfL * 2) }, Position = new Vector3(0, 0.06f, 0), MaterialOverride = _proxyMat, Layers = 4, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Name = "TvProxy" }; AddChild(_proxy); }
      bool under = _shell != null && _shell.Visible;
      foreach (var node in FindChildren("*", "MeshInstance3D", true, false)) { var mi = (MeshInstance3D)node; if (mi == _proxy) continue; if (mi.Layers != 2) mi.Layers = 2;
        if (!Show && ((_chassis != null && _chassis.IsAncestorOf(mi)) || (_mounts != null && _mounts.IsAncestorOf(mi)))) { var want = under ? GeometryInstance3D.ShadowCastingSetting.Off : GeometryInstance3D.ShadowCastingSetting.On; if (mi.CastShadow != want) mi.CastShadow = want; } }
    }
    Node3D _mounts, _chassis; Suspension _susp; public bool Show; /* a car on show: it never moves, so it gets no working suspension */
    public bool Ghost; /* a see-through replay of the car: one pale tint, no shadow, no stickers */
    public int Body; public float RoofY = 0.133f, HalfW = 0.095f, HalfL = 0.21f;
    public static readonly string[] Bonnets = { "BODY COLOUR", "CARBON" }; /* one livery template: a solid colour, the bonnet in it or in carbon, and the rims in their own colour */
    public static string HideTest; // test: hide shell parts whose name contains this
    // options carried over from the web version: the body shell on or off, neon under the car, tinted or clear windows
    readonly System.Collections.Generic.List<BaseMaterial3D> _gaps = new(); readonly System.Collections.Generic.List<Vector3> _oldStick = new(); Node3D _shell, _neon; StandardMaterial3D _neonBar, _neonFloor; OmniLight3D _neonLight; bool _clear, _onBoard;
    public static readonly (string name, Color col)[] Neons = { ("OFF", Colors.Black), ("PINK", new Color(1f, 0.25f, 0.71f)), ("ICE BLUE", new Color(0.25f, 0.8f, 1f)), ("GREEN", new Color(0.3f, 1f, 0.45f)), ("AMBER", new Color(1f, 0.62f, 0.12f)), ("PURPLE", new Color(0.62f, 0.3f, 1f)), ("WHITE", new Color(1f, 0.97f, 0.9f)) };
    public Node3D Fpv; // the on-board camera's seat: inside the cabin, on the centre line, behind the windscreen; it leans with the body
    public Node3D StickCam; Node3D _stick; // a second FPV camera on a short stick: fixed to the roof, reaching back, looking forward over the car
    public static int StickSide = 1; public static float StickUp = 0, StickFwd = 0.03f; /* door sticker placement (test) */
    public static float StickY = 0.215f, StickZ = -0.46f, StickTilt = 10f; // 21.5 cm up (it has to pass under the bridge, 23 cm), 46 cm back, looking down 10 degrees: the car in the lower half, the track ahead above it
    public static float FpvX = 0, FpvY = 0.102f, FpvZ = 0.006f, FpvTilt = 5f;
    static readonly string[] Names = { "WheelFa", "WheelFb", "WheelRa", "WheelRb" };

    public override void _Ready() {
      var model = GD.Load<PackedScene>("res://assets/" + Bodies[Body % Bodies.Length].file + ".glb").Instantiate<Node3D>(); AddChild(model);
      _body = new Node3D { Name = "Body" }; AddChild(_body);
      foreach (var n in new[] { "Shell", "Chassis", "Mounts" }) { var p = model.FindChild(n, true, false) as Node3D; if (p == null) { GD.PrintErr("missing ", n); continue; } var xf = p.Transform; p.GetParent().RemoveChild(p); _body.AddChild(p); p.Transform = xf; if (n == "Shell") _shell = p; if (n == "Mounts") _mounts = p; if (n == "Chassis") _chassis = p; }
      for (int i = 0; i < 4; i++) {
        _wheel[i] = model.FindChild(Names[i], true, false) as Node3D; _spin[i] = model.FindChild(Names[i] + "_Spin", true, false) as Node3D;
        if (_spin[i] != null) { _camber[i] = _spin[i].Quaternion; _leanIn[i] = Mathf.Sign(_camber[i].X) == 0 ? 1 : Mathf.Sign(_camber[i].X); _camber[i] = Quaternion.Identity; } // the model came with 9 degrees of negative camber: which way is 'in' for this wheel is taken from it, then the wheel is stood upright
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
      var glass = _glass = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/glass.gdshader") }; MeshInstance3D glassMi = null;
      _rimMat = new StandardMaterial3D { AlbedoColor = new Color(0.02f, 0.02f, 0.022f), Roughness = 0.3f };
      bool van = Bodies[Body % Bodies.Length].file == "zuk";
      foreach (var node in FindChildren("*", "MeshInstance3D", true, false)) {
        var mi = (MeshInstance3D)node; string nm = mi.Name.ToString();
        if (HideTest != null && nm.StartsWith("Shell_") && nm.Contains(HideTest)) mi.Visible = false;
        // the web version's door stickers (three upright strips, each in its own frame) give way to cut-out ones: note where
        // on the door they sat, and how the door leans there, then hide them
        if (nm.StartsWith("Shell_m_ffffff_t") && mi.Mesh != null) { var sb = mi.GetAabb(); if (sb.Size.Y > 0.025f && sb.Size.Z < 0.014f) {
            foreach (var v in (Vector3[])mi.Mesh.SurfaceGetArrays(0)[(int)Mesh.ArrayType.Vertex]) _oldStick.Add(mi.Transform * v); mi.Visible = false; continue; } }
        // the rear window sticker: the web version's logo gives way to SEND IT, on the same patch
        if (nm.StartsWith("Shell_m_ffffff_t") && mi.Mesh != null) { var rb = mi.GetAabb(); var rc = rb.GetCenter(); if (rc.Z < -0.05f && Mathf.Abs(rc.X) < 0.01f && rb.Size.X > 0.05f && rb.Size.X < 0.09f && rb.Size.Y < 0.03f) {
            mi.MaterialOverride = new StandardMaterial3D { AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/rear_sticker.png"), AlbedoColor = new Color(0.98f, 0.96f, 0.9f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = 0.42f,
              EmissionEnabled = true, Emission = new Color(0.98f, 0.96f, 0.9f), EmissionEnergyMultiplier = 0.5f, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic }; continue; } }
        if (nm.EndsWith("_paint")) { mi.MaterialOverride = Paint; if (van) { Paint.SetShaderParameter("detail", GD.Load<Texture2D>("res://assets/tex/van_detail.png")); Paint.SetShaderParameter("use_detail", 1f); }
          // the painted shell's size: the livery is laid out on it, and the cameras and the handling take the roof height
          var bb = mi.GetAabb(); Paint.SetShaderParameter("dim", new Vector3(bb.Size.X / 2, bb.Size.Y, bb.Size.Z / 2)); Paint.SetShaderParameter("origin", new Vector3(bb.Position.X + bb.Size.X / 2, bb.Position.Y, bb.Position.Z + bb.Size.Z / 2));
          var sx = _shell != null ? _shell.Scale : Vector3.One; float sy = _shell != null ? _shell.Position.Y : 0;
          RoofY = (bb.Position.Y + bb.Size.Y) * sx.Y + sy; HalfW = bb.Size.X / 2 * sx.X; HalfL = bb.Size.Z / 2 * sx.Z; }
        else if (nm.Contains("_glass_")) { mi.MaterialOverride = glass; glassMi = mi; }
        else if (van && nm.StartsWith("Shell_m_ffffff_t") && mi.Mesh.SurfaceGetMaterial(0) is BaseMaterial3D vm && vm.AlbedoTexture != null && mi.GetAabb().Size.Z > 0.05f) {
          if (vm.Transparency != BaseMaterial3D.TransparencyEnum.Disabled) { mi.MaterialOverride = glass; glassMi = mi; }   // the van's windows come as a printed sheet: they are glass here, like the others
          else mi.MaterialOverride = Paint; }
        else if (nm.Contains("_Spin_m_18181b")) mi.MaterialOverride = _rimMat;   /* the rims: their colour is the driver's choice */
        else if (mi.Mesh != null && mi != _blob && mi != _neonFloorNode && !_stick.IsAncestorOf(mi) && !_neon.IsAncestorOf(mi))
          for (int si = 0; si < mi.Mesh.GetSurfaceCount(); si++) if (mi.Mesh.SurfaceGetMaterial(si) is BaseMaterial3D bm0) {
            var d = (BaseMaterial3D)bm0.Duplicate();
            // The model's plain colours were written out as screen (sRGB) values, but the engine reads them as light (linear)
            // values, which made every one too pale: near-black seals and panel gaps came out copper, black tyres grey.
            if (d.AlbedoTexture == null) { d.AlbedoColor = new Color(d.AlbedoColor.SrgbToLinear(), d.AlbedoColor.A); if (d.EmissionEnabled) d.Emission = d.Emission.SrgbToLinear(); }
            if (nm.StartsWith("Shell_")) d.CullMode = BaseMaterial3D.CullModeEnum.Disabled; // the shell is seen from inside too
            if (nm.StartsWith("Wheel") && nm.Contains("_m_ffffff_t")) d.AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/tyre_letter.png");   // the sidewall lettering: our own
            if (nm.Contains("27110a")) { d.Roughness = 0.7f; d.Metallic = 0; _gaps.Add(d); }   // panel gaps, vents and window surrounds: matt, and shaded from the paint (SetPaint)
            mi.SetSurfaceOverrideMaterial(si, d); }
        if (mi != _blob && mi != _neonFloorNode && !_stick.IsAncestorOf(mi) && !_neon.IsAncestorOf(mi)) mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
      }
      // Stickers. Each is a thin sheet laid on the shell from outside, point by point, so it follows the surface; its own
      // satin finish and the relief of its cut edges set it apart from the glossy shell under it.
      (Vector3[] v, int[] i) Tris(MeshInstance3D m) { var vv = new System.Collections.Generic.List<Vector3>(); var ii = new System.Collections.Generic.List<int>(); if (m == null || m.Mesh == null) return (vv.ToArray(), ii.ToArray());
        for (int sf = 0; sf < m.Mesh.GetSurfaceCount(); sf++) { var arr = m.Mesh.SurfaceGetArrays(sf); var av = (Vector3[])arr[(int)Mesh.ArrayType.Vertex]; var ai = (int[])arr[(int)Mesh.ArrayType.Index]; int b0 = vv.Count; foreach (var q in av) vv.Add(m.Transform * q);
          if (ai != null && ai.Length > 0) foreach (int q in ai) ii.Add(b0 + q); else for (int q = 0; q < av.Length; q++) ii.Add(b0 + q); }
        return (vv.ToArray(), ii.ToArray()); }
      float Hit((Vector3[] v, int[] i) g, Vector3 o, Vector3 d) { // nearest point of a mesh along a ray, as a distance (or -1)
        float best = -1; var pv = g.v; var pi = g.i;
        for (int k = 0; k + 2 < pi.Length; k += 3) { Vector3 a = pv[pi[k]], e1 = pv[pi[k + 1]] - a, e2 = pv[pi[k + 2]] - a, h = d.Cross(e2); float det = e1.Dot(h); if (det > -1e-12f && det < 1e-12f) continue;
          float f = 1 / det; var sv = o - a; float u = f * sv.Dot(h); if (u < 0 || u > 1) continue; var q = sv.Cross(e1); float v = f * d.Dot(q); if (v < 0 || u + v > 1) continue;
          float tt = f * e2.Dot(q); if (tt > 1e-5f && (best < 0 || tt < best)) best = tt; }
        return best; }
      StandardMaterial3D Vinyl(string tex) => new StandardMaterial3D { AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/" + tex + ".png"), NormalEnabled = true, NormalTexture = GD.Load<Texture2D>("res://assets/tex/" + tex + "_n.png"), NormalScale = 0.6f, AlbedoColor = new Color(0.98f, 0.96f, 0.9f), EmissionEnabled = true, Emission = new Color(0.98f, 0.96f, 0.9f), EmissionOperator = BaseMaterial3D.EmissionOperatorEnum.Multiply, EmissionTexture = GD.Load<Texture2D>("res://assets/tex/" + tex + ".png"), EmissionEnergyMultiplier = 0.6f, /* milky white: a soft warm white that does not go grey in the shade */
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha, CullMode = BaseMaterial3D.CullModeEnum.Disabled, Roughness = 0.42f, MetallicSpecular = 0.4f, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
      // a patch c +- along*hl, +- up*hh dropped onto a mesh along -n, held a hair off it; hold: how far it may leave the height at its middle
      void Patch(SurfaceTool st, (Vector3[] v, int[] i) g, Vector3 c, Vector3 along, Vector3 up, Vector3 n, float hl, float hh, int NU, int NV, bool flip, float hold, float lift) {
        var grid = new Vector3[NU + 1, NV + 1]; float d0 = Hit(g, c + n * 0.04f, -n); if (d0 <= 0 || d0 > 0.07f) d0 = 0.04f;
        for (int iu = 0; iu <= NU; iu++) for (int iv = 0; iv <= NV; iv++) { var flat = c + along * ((iu / (float)NU * 2 - 1) * hl) + up * ((1 - iv / (float)NV * 2) * hh); float d = Hit(g, flat + n * 0.04f, -n); if (d <= 0 || d > 0.07f) d = d0; d = Mathf.Clamp(d, d0 - hold, d0 + hold); grid[iu, iv] = flat + n * (0.04f - d + lift); }
        for (int iu = 0; iu < NU; iu++) for (int iv = 0; iv < NV; iv++) foreach (var (a, b) in new[] { (0, 0), (1, 0), (1, 1), (0, 0), (1, 1), (0, 1) }) {
          float uu = (iu + a) / (float)NU; st.SetNormal(n); st.SetUV(new Vector2(flip ? 1 - uu : uu, (iv + b) / (float)NV)); st.AddVertex(grid[iu + a, iv + b]); } }
      MeshInstance3D paintMi = null; foreach (var node in _shell.FindChildren("*_paint", "MeshInstance3D", true, false)) { paintMi = (MeshInstance3D)node; break; }
      var paintG = Tris(paintMi);
      // door stickers: four separate cut-vinyl stickers with no backing (DRIFT MASTER, Send it, 640kFPV, SEND IT), stacked
      // in a column down the driver's door under the mirror, laid on the door's own lean. White with a thin black contour.
      if (_oldStick.Count > 0 && _shell != null) {
        var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles);
        foreach (int sg in new[] { StickSide }) {
          var pts = _oldStick.FindAll(v => v.X * sg > 0); if (pts.Count < 6) pts = _oldStick.FindAll(v => v.X * sg < 0).ConvertAll(v => new Vector3(-v.X, v.Y, v.Z)); if (pts.Count < 6) continue;
          Vector3 m = Vector3.Zero; foreach (var v in pts) m += v; m /= pts.Count; float sxy = 0, syy = 0, sxz = 0, szz = 0;
          foreach (var v in pts) { sxy += (v.X - m.X) * (v.Y - m.Y); syy += (v.Y - m.Y) * (v.Y - m.Y); sxz += (v.X - m.X) * (v.Z - m.Z); szz += (v.Z - m.Z) * (v.Z - m.Z); }
          float lean = syy > 1e-9f ? sxy / syy : 0, sweep = szz > 1e-9f ? sxz / szz : 0;           // how the door's surface runs up and along
          var up = new Vector3(lean, 1, 0).Normalized(); var along = new Vector3(sweep, 0, 1).Normalized(); var n = along.Cross(up).Normalized(); if (n.X * sg < 0) n = -n;
          int bi = Body % Bodies.Length; var c = m + new Vector3(0, StickUp - (bi == 2 ? 0.006f : bi == 3 ? 0.008f : 0), StickFwd - (bi == 3 ? 0.034f : 0)); /* the pickup's mirror hangs lower; the tuner's sits on the door itself */
          Patch(st, paintG, c, along, up, n, 0.0135f, 0.019f, 6, 9, sg > 0, 0.009f, 0.0012f);   // seen from outside, the nose is to the left on the car's left side
        }
        st.GenerateTangents();
        _shell.AddChild(new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = Vinyl("side_stickers"), Name = "DoorStickers", CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
      }
      // the windscreen: found in the glass (the panes that face forward, in the front half), for the sun strip across its top
      // and the DRIFT MASTER banner on that strip
      { var gl = Tris(glassMi); float zmid = 0, n0 = 0; foreach (var v in gl.v) { zmid += v.Z; n0++; } zmid /= Math.Max(1, n0);
        var wn = Vector3.Zero; var wv = new System.Collections.Generic.List<Vector3>();
        for (int k = 0; k + 2 < gl.i.Length; k += 3) { Vector3 a = gl.v[gl.i[k]], b = gl.v[gl.i[k + 1]], cc = gl.v[gl.i[k + 2]]; var nn = (b - a).Cross(cc - a); float ar = nn.Length(); if (ar < 1e-12f) continue; nn /= ar; if (nn.Z < 0) nn = -nn;
          if ((a.Z + b.Z + cc.Z) / 3 < zmid || nn.Z < 0.25f || Mathf.Abs(nn.X) > 0.5f) continue; wn += nn * ar; wv.Add(a); wv.Add(b); wv.Add(cc); }
        if (wv.Count > 0) { wn = wn.Normalized(); wn.X = 0; wn = wn.Normalized(); float top = float.MinValue, bot = float.MaxValue; foreach (var v in wv) { top = Math.Max(top, v.Y); bot = Math.Min(bot, v.Y); }
          float zt = 0, zb = 0, nt = 0, nb = 0, wTop = 0; foreach (var v in wv) { if (v.Y > top - 0.003f) { zt += v.Z; nt++; } if (v.Y < bot + 0.003f) { zb += v.Z; nb++; } if (v.Y > top - 0.012f) wTop = Math.Max(wTop, Mathf.Abs(v.X)); }
          Vector3 T = new Vector3(0, top, zt / nt), B = new Vector3(0, bot, zb / nb), up = (T - B).Normalized(); float L = (T - B).Length(), bh = Math.Min(0.015f, L * 0.26f);
          glass.SetShaderParameter("strip_c", T - up * bh); glass.SetShaderParameter("strip_v", up); glass.SetShaderParameter("strip_n", wn); glass.SetShaderParameter("strip_on", 1f);
          var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles); float bw = Math.Min(0.052f, wTop * 0.72f), bhh = Math.Min(bh * 0.33f, bw * 0.125f);
          Patch(st, gl, T - up * (bh * 0.52f), new Vector3(1, 0, 0), up, wn, bw, bhh, 16, 2, false, 0.012f, 0.0007f); st.GenerateTangents();
          _banner = new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = Vinyl("banner"), Name = "Banner", CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }; _shell.AddChild(_banner);
          foreach (var node in _shell.FindChildren("Shell_m_ffffff_t*", "MeshInstance3D", true, false)) { var mi = (MeshInstance3D)node; var sb = mi.GetAabb(); var ce = sb.GetCenter(); // the web version's banner goes
            if (sb.Size.X > 0.08f && sb.Size.Y < 0.02f && ce.Y > bot && ce.Z > B.Z - L && ce.Z < B.Z + 0.01f && Mathf.Abs(ce.X) < 0.01f) mi.Visible = false; } } }
      // the coupe carries a GT wing: a blade on two swept stays over the tail, with end plates
      if (Body % Bodies.Length == 1 && paintMi != null) {
        var bb = paintMi.GetAabb(); float zTail = bb.Position.Z, hw = bb.Size.X / 2 * 0.74f; var wing = new Node3D { Name = "Wing" }; _shell.AddChild(wing);
        var black = new StandardMaterial3D { AlbedoColor = new Color(0.012f, 0.012f, 0.014f), Roughness = 0.28f }; float zFoot = zTail + 0.04f, deck = 0;
        foreach (int sd in new[] { -1, 1 }) { float d = Hit(paintG, new Vector3(sd * 0.034f, 0.4f, zFoot), Vector3.Down); if (d > 0) deck = Math.Max(deck, 0.4f - d); }
        if (deck < 0.03f) deck = bb.Position.Y + bb.Size.Y * 0.7f; float bladeY = deck + 0.024f, bladeZ = zTail + 0.02f;   /* a modest wing: three quarters of the body's width, a couple of centimetres off the deck */
        void Box(Vector3 size, Vector3 pos, Vector3 rotDeg) => wing.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = black, Position = pos, RotationDegrees = rotDeg });
        Box(new Vector3(hw * 2, 0.0024f, 0.028f), new Vector3(0, bladeY, bladeZ), new Vector3(9, 0, 0));                      // the blade, its trailing edge kicked up
        Box(new Vector3(hw * 2, 0.002f, 0.006f), new Vector3(0, bladeY + 0.0034f, bladeZ - 0.015f), new Vector3(42, 0, 0));    // the lip along the trailing edge
        foreach (int sd in new[] { -1, 1 }) {
          Box(new Vector3(0.002f, 0.02f, 0.036f), new Vector3(sd * hw, bladeY - 0.003f, bladeZ - 0.002f), Vector3.Zero);          // end plates
          Vector3 foot = new Vector3(sd * 0.034f, deck - 0.002f, zFoot), head = new Vector3(sd * 0.034f, bladeY - 0.001f, bladeZ + 0.004f), dv = head - foot;
          Box(new Vector3(0.0022f, dv.Length(), 0.02f), (foot + head) / 2, new Vector3(Mathf.RadToDeg(Mathf.Atan2(dv.Z, dv.Y)), 0, 0)); } }  // stays
      if (Ghost) { var gm = new StandardMaterial3D { AlbedoColor = new Color(0.55f, 0.85f, 1f, 0.3f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, CullMode = BaseMaterial3D.CullModeEnum.Back };
        foreach (var node in FindChildren("*", "MeshInstance3D", true, false)) { var mi = (MeshInstance3D)node; mi.MaterialOverride = gm; for (int si = 0; mi.Mesh != null && si < mi.Mesh.GetSurfaceCount(); si++) mi.SetSurfaceOverrideMaterial(si, null); mi.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
          string gn = mi.Name.ToString(); if (mi == _blob || gn == "DoorStickers" || gn == "Banner" || gn.Contains("_glass_") || gn.StartsWith("Shell_m_ffffff_t") || gn.StartsWith("Chassis") || gn.StartsWith("Mounts")) mi.Visible = false; }
        _neon.Visible = false; _stick.Visible = false; }
      // the cameras ride where this shell's cabin and roof are
      float cab = Bodies[Body % Bodies.Length].cabZ;
      Fpv.Position = new Vector3(FpvX, RoofY - 0.031f, cab); StickCam.Position = new Vector3(0, Math.Max(StickY, RoofY + 0.045f), StickZ);
      foreach (var ch in _stick.GetChildren()) ch.QueueFree();
      { var carbon = new StandardMaterial3D { AlbedoColor = new Color(0.05f, 0.05f, 0.055f), Roughness = 0.45f };
        Vector3 foot = new Vector3(0, 0.05f, -HalfL + 0.004f), end = StickCam.Position + new Vector3(0, -0.022f, 0.012f), d = end - foot;   // clamped to the rear bumper, so the rod crosses the floor behind the car and not the car
        var y = d.Normalized(); var x = y.Cross(Vector3.Right).Normalized(); var z = x.Cross(y);
        _stick.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.002f, BottomRadius = 0.002f, Height = d.Length(), RadialSegments = 8, Rings = 1 }, MaterialOverride = carbon, Transform = new Transform3D(new Basis(x, y, z), (foot + end) / 2), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        _stick.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.03f, 0.012f, 0.008f) }, MaterialOverride = carbon, Position = foot, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }); }
    }

    // from the driver's seat the tint is light enough to see the track through
    public void SetPaint(Color a, bool carbon) { if (_proxyMat == null) Lay(); if (_proxyMat != null) _proxyMat.AlbedoColor = a; SetPaintInner(a, carbon); }
    void SetPaintInner(Color a, bool carbon) {
      // shut lines are shadow, not ink: a much darker shade of the body colour, so they do not look drawn on
      foreach (var g in _gaps) g.AlbedoColor = new Color(a.R * 0.3f + 0.006f, a.G * 0.3f + 0.006f, a.B * 0.3f + 0.006f); Paint.SetShaderParameter("col_a", a); Paint.SetShaderParameter("carbon", carbon && HasBonnet ? 1f : 0f); }
    public bool HasBonnet => Bodies[Body % Bodies.Length].file != "zuk";   /* the van has a flat nose */
    /// rim colour; metal: an anodised or polished finish, else paint
    public void SetRims(Color c, bool metal) { _rimMat.AlbedoColor = c; _rimMat.Metallic = metal ? 0.85f : 0f; _rimMat.Roughness = metal ? 0.28f : 0.32f; }
    /// camber in degrees, front and rear: 0 upright, negative leans the tops of the wheels in, positive out
    public void SetCamber(float front, float rear) { for (int i = 0; i < 4; i++) _camber[i] = new Quaternion(Vector3.Right, _leanIn[i] * Mathf.DegToRad(-(i < 2 ? front : rear))); }
    public void ShowStick(bool on) { _stick.Visible = on; }
    public void SetOnBoard(bool on) { _onBoard = on; if (_banner != null) _banner.Visible = !on; Glass(); } /* from inside, the banner would be read backwards across the view */
    // tinted: dark, as on most shells; clear: the chassis and the cabin show through
    public void SetWindows(bool clear) { _clear = clear; Glass(); }
    void Glass() { _glass.SetShaderParameter("tint", _clear ? new Color(0.75f, 0.82f, 0.88f, 0.1f) : new Color(0.05f, 0.065f, 0.085f, _onBoard ? 0.16f : 0.8f)); _glass.SetShaderParameter("metal", _clear ? 0.1f : 0.3f); _glass.SetShaderParameter("strip_show", _onBoard ? 0f : 1f); }
    // shell off: the bare chassis, as on a setup stand (the body posts stay)
    /// a car on show that will never move: what is under the shell is not drawn, and it casts no shadow but the soft one under it
    /// (its meshes are left as they are: they carry the model's levels of detail, which a merged mesh would lose)
    public void Freeze() { foreach (var n in new[] { "Chassis", "Mounts" }) if (_body.FindChild(n, true, false) is Node3D p) p.Visible = false;
      foreach (var node in FindChildren("*", "MeshInstance3D", true, false)) ((MeshInstance3D)node).CastShadow = GeometryInstance3D.ShadowCastingSetting.Off; }
    readonly System.Collections.Generic.List<(Node3D h, Vector3 onShell, Vector3 onKit)> _halos = new(); readonly System.Collections.Generic.List<(MeshInstance3D plain, MeshInstance3D lit)> _lit = new();
    /// the halos sit where the light leaves the car: at the shell's lamps, or at the LEDs themselves when the shell is off
    void PlaceHalos() { bool shell = _shell != null && _shell.Visible; foreach (var (h, a, b) in _halos) h.Position = shell ? a : b; }
    Node3D _lamps, _lampKit, _lampFloor; static ImageTexture _glowTex, _streakTex; public Vector3 Eye; /* where the picture is taken from (set by the game each frame) */
    readonly System.Collections.Generic.List<(MeshInstance3D streak, Node3D halo, Color col)> _refl = new();
    static ImageTexture Tex(int w, int h, Func<float, float, float> f) { var img = Image.CreateEmpty(w, h, false, Image.Format.Rgb8); for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { float v = Mathf.Clamp(f((x + 0.5f) / w, (y + 0.5f) / h), 0, 1); img.SetPixel(x, y, new Color(v, v, v)); } return ImageTexture.CreateFromImage(img); }
    /// headlights, as RC drift cars carry them: LEDs in holders on a bar across the front of the chassis (red ones on a bar
    /// at the back), shining out through the shell's lamps. Each lamp is a small lit lens that faces the way it shines (so
    /// it is bright from in front and nothing from the side or above), and each has its reflection in the glossy floor: a
    /// streak lying flat on the floor, between the lamp and whoever is looking, where a mirror would show it. None of it
    /// is a real light: it is drawn, not computed, so it costs next to nothing.
    public bool HasLamps { get { string f = Bodies[Body % Bodies.Length].file; return f != "zuk" && f != "pickup"; } }   /* the van and the pickup have no working lamps */
    public void SetLamps(bool on) {
      if (!HasLamps) return;
      if (_lamps == null) { if (!on || Ghost) return;
        _glowTex ??= Tex(64, 64, (u, v) => { float r = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2; return Mathf.Pow(Mathf.Max(0, 1 - r), 3f) * 0.7f + Mathf.Exp(-r * r * 30); });
        _streakTex ??= Tex(32, 128, (u, v) => { float du = (u - 0.5f) / 0.3f, a = Mathf.Exp(-du * du * 2.5f), along = Mathf.Min(1, v / 0.035f) * Mathf.Exp(-v * 2.8f) * Mathf.Min(1, (1 - v) / 0.12f); return a * along; });   // brightest by the car, dying away towards the eye
        StandardMaterial3D Add(Texture2D t, Color c) => new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoTexture = t, AlbedoColor = c, BlendMode = BaseMaterial3D.BlendModeEnum.Add, CullMode = BaseMaterial3D.CullModeEnum.Back, DisableReceiveShadows = true };
        float k = Hall.Dark ? 1f : 0.5f; Color cool = new(0.78f, 0.9f, 1f), red = new(1f, 0.1f, 0.06f);
        // the kit on the chassis: a bar on two posts at each end, an LED in a holder at each end of the bar
        _lampKit = new Node3D { Name = "LampKit" }; (_chassis ?? _body).AddChild(_lampKit);
        var alu = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.1f, 0.11f), Roughness = 0.4f, Metallic = 0.6f };
        foreach (var (z, c, x) in new[] { (0.166f, new Color(0.9f, 0.96f, 1f), 0.052f), (-0.168f, red, 0.05f) }) { float dir = Mathf.Sign(z);
          _lampKit.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(x * 2 + 0.02f, 0.004f, 0.006f) }, MaterialOverride = alu, Position = new Vector3(0, 0.047f, z) });
          foreach (int sx in new[] { -1, 1 }) {
            _lampKit.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.004f, 0.04f, 0.004f) }, MaterialOverride = alu, Position = new Vector3(sx * 0.028f, 0.027f, z) });
            _lampKit.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.0065f, BottomRadius = 0.0055f, Height = 0.011f, RadialSegments = 12, Rings = 1 }, MaterialOverride = alu, Position = new Vector3(sx * x, 0.047f, z + dir * 0.004f), Rotation = new Vector3(Mathf.Pi / 2 * dir, 0, 0) });
            _lampKit.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.0052f, BottomRadius = 0.0052f, Height = 0.001f, RadialSegments = 12, Rings = 1 }, Position = new Vector3(sx * x, 0.047f, z + dir * 0.0098f), Rotation = new Vector3(Mathf.Pi / 2, 0, 0), MaterialOverride = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = c } }); } }
        foreach (var n in _lampKit.GetChildren()) ((MeshInstance3D)n).CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        // the shell's own lamps light up: the moulded headlamps (white parts at the nose) and tail lamps (red parts, or white
        // ones at the tail) are drawn again in a material that gives off light. Nothing is added in the air round them.
        _lamps = new Node3D { Name = "Lamps" }; _body.AddChild(_lamps); _lampFloor = new Node3D { Name = "LampFloor" }; AddChild(_lampFloor);
        var sum = new Vector3[4]; var cnt = new int[4];   // where the lamps are: front left, front right, rear left, rear right (in the body's frame)
        Transform3D Rel(Node3D n) { var x = n.Transform; for (var q = n.GetParent(); q != _body && q is Node3D q3; q = q3.GetParent()) x = q3.Transform * x; return x; }
        if (_shell != null) foreach (var node in _shell.FindChildren("*", "MeshInstance3D", true, false)) { var mi = (MeshInstance3D)node; string nm = mi.Name.ToString(); bool white = nm.EndsWith("_m_e6e6e6"), redPart = nm.EndsWith("_m_b0121a"); if (!(white || redPart) || !(mi.Mesh is ArrayMesh am) || am.GetSurfaceCount() != 1) continue;
            var arr = am.SurfaceGetArrays(0); var v = (Vector3[])arr[(int)Mesh.ArrayType.Vertex]; var idx = arr[(int)Mesh.ArrayType.Index].VariantType == Variant.Type.Nil ? null : (int[])arr[(int)Mesh.ArrayType.Index]; if (idx == null) { idx = new int[v.Length]; for (int q = 0; q < idx.Length; q++) idx[q] = q; }
            var xf = Rel(mi); var parts = new[] { new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>() };
            for (int q = 0; q + 2 < idx.Length; q += 3) { var c = xf * ((v[idx[q]] + v[idx[q + 1]] + v[idx[q + 2]]) / 3); int rear = c.Z < 0 ? 1 : 0; if (redPart && rear == 0) continue; parts[rear].Add(idx[q]); parts[rear].Add(idx[q + 1]); parts[rear].Add(idx[q + 2]); int slot = rear * 2 + (c.X < 0 ? 1 : 0); sum[slot] += c; cnt[slot]++; }
            bool first = true;
            for (int rear = 0; rear < 2; rear++) { if (parts[rear].Count == 0) continue; var nmsh = new ArrayMesh(); arr[(int)Mesh.ArrayType.Index] = parts[rear].ToArray(); nmsh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr);
              var c = rear == 1 ? red : cool; var lit = new MeshInstance3D { Mesh = nmsh, Transform = mi.Transform, Name = "Lit" + rear, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, MaterialOverride = new StandardMaterial3D { AlbedoColor = c * 0.6f, EmissionEnabled = true, Emission = c, EmissionEnergyMultiplier = Hall.Dark ? 2.6f : 1.4f, Roughness = 0.3f } };
              mi.GetParent().AddChild(lit); _lit.Add((first ? mi : null, lit)); first = false; } }   // (the plain part is hidden while its lit copy is shown)
        // each lamp's reflection on the floor, under the lamp itself
        void Lamp(int slot, Vector3 fallback, Vector3 onKit, Color c, bool rear) {
          if (cnt[slot] == 0) return;   // (a body with no moulded lamp there shows no light and so no reflection)
          var at = cnt[slot] > 0 ? sum[slot] / cnt[slot] : fallback; at.Z = rear ? Mathf.Min(at.Z, -HalfL + 0.004f) : Mathf.Max(at.Z, HalfL - 0.004f);
          var h = new Node3D { Rotation = new Vector3(0, rear ? Mathf.Pi : 0, 0) }; _lamps.AddChild(h); _halos.Add((h, at, onKit));
          var st = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(1, 1) }, MaterialOverride = Add(_streakTex, c), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }; ((StandardMaterial3D)st.MaterialOverride).CullMode = BaseMaterial3D.CullModeEnum.Disabled; _lampFloor.AddChild(st); _refl.Add((st, h, c * (rear ? 0.7f : 0.55f) * (0.5f + 0.5f * k))); }
        foreach (int side in new[] { 0, 1 }) { int sx = side == 0 ? 1 : -1; Lamp(side, new Vector3(sx * HalfW * 0.62f, 0.05f, HalfL), new Vector3(sx * 0.052f, 0.044f, 0.179f), cool, false); Lamp(2 + side, new Vector3(sx * HalfW * 0.64f, 0.055f, -HalfL), new Vector3(sx * 0.05f, 0.044f, -0.181f), red, true); } }
      foreach (var (plain, lit) in _lit) { if (plain != null) plain.Visible = !on; if (lit != null) lit.Visible = on; }
      _lamps.Visible = on; _lampKit.Visible = on; _lampFloor.Visible = on; PlaceHalos();
    }
    // each lamp's reflection: flat on the floor at the point where a mirror would show the lamp to the eye, drawn out
    // into a streak along the line of sight (the tile is glossy, not a mirror). Only for lamps that face the eye.
    void PlaceReflections() {
      var inv = GlobalTransform.AffineInverse(); var eye = inv * Eye;
      foreach (var (st, h, col) in _refl) { var lamp = inv * h.GlobalPosition; var fwd = (inv.Basis * h.GlobalBasis.Z).Normalized(); var to = eye - lamp; float facing = fwd.Dot(to.Normalized());
        var flat = new Vector3(to.X, 0, to.Z); float dist = flat.Length(), eh = Mathf.Max(0.02f, eye.Y), lh = Mathf.Max(0.005f, lamp.Y);
        if (facing < 0.12f || dist < 0.05f) { st.Visible = false; continue; } st.Visible = true;
        var dir = flat / dist; float d = dist * lh / (eh + lh), len = Mathf.Clamp(d * 1.5f, 0.07f, 0.42f), wid = 0.028f + d * 0.06f;
        var side = new Vector3(dir.Z, 0, -dir.X);   // the plane's own x lies across the streak, its z along it, towards the eye
        st.Transform = new Transform3D(new Basis(side * wid, Vector3.Up, dir * len), new Vector3(lamp.X, 0.0028f, lamp.Z) + dir * (len * 0.5f + 0.012f));   // (it begins by the car and runs towards the eye)
        if (st.MaterialOverride is StandardMaterial3D m) m.AlbedoColor = col * Mathf.Clamp(facing * 1.3f, 0, 1); }
    }
    public void SetShell(bool on) { if (_shell != null) _shell.Visible = on; PlaceHalos(); }
    /// how the shell is held on: body posts with clips (they stand through the bonnet and boot), or hidden magnets (nothing shows)
    public void SetMounts(bool magnets) { if (_mounts != null) _mounts.Visible = !magnets; }
    /// Neon, as fitted to RC drift cars: LED strips under the edges of the chassis and across its ends. The light pools on
    /// the floor close round the car's outline, fills the wheel arches, catches the bottom of the body, and lights the
    /// cabin from inside so the windows glow.
    public void SetNeon(int i) {
      var c = Neons[i % Neons.Length].col; bool on = i % Neons.Length != 0 && !Ghost; _neon.Visible = on; _neonFloorNode.Visible = on;
      Paint.SetShaderParameter("neon", on ? c : Colors.Black); _glass.SetShaderParameter("inner_glow", on ? c : Colors.Black); if (!on) return;
      foreach (var ch in _neon.GetChildren()) ch.QueueFree();
      float hw = HalfW, hl = HalfL; _neonBar.AlbedoColor = c.Lerp(Colors.White, 0.55f);
      void Strip(Vector3 size, Vector3 pos) => _neon.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = _neonBar, Position = pos, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
      foreach (int sd in new[] { -1, 1 }) Strip(new Vector3(0.004f, 0.003f, hl * 1.0f), new Vector3(sd * (hw - 0.022f), 0.013f, 0));            // along each sill, between the wheels
      Strip(new Vector3(hw * 1.2f, 0.003f, 0.004f), new Vector3(0, 0.014f, hl - 0.03f)); Strip(new Vector3(hw * 1.2f, 0.003f, 0.004f), new Vector3(0, 0.014f, -hl + 0.03f));   // across the nose and the tail
      void Lamp(Vector3 pos, float range, float energy) => _neon.AddChild(new OmniLight3D { Position = pos, OmniRange = range, OmniAttenuation = 1.1f, LightEnergy = energy, LightColor = c, ShadowEnabled = false, LightSpecular = 0.04f });   // (next to no highlight of its own: on the glossy floor it drew a bright streak; the pool is the floor's light)
      Lamp(new Vector3(0, 0.014f, 0), 0.24f, 2.2f);                                                           // under the chassis
      Lamp(new Vector3(0, 0.04f, 0.1285f), 0.2f, 1.8f); Lamp(new Vector3(0, 0.04f, -0.1285f), 0.2f, 1.8f);    // in the wheel arches, front and rear
      Lamp(new Vector3(0, RoofY - 0.05f, Bodies[Body % Bodies.Length].cabZ - 0.03f), 0.17f, 1.1f);            // inside the cabin
      // the pool on the floor: brightest right at the car's edge, gone within a hand's width of it
      float W = hw * 2 + 0.22f, L = hl * 2 + 0.22f; const int TW = 96, TH = 160; var img = Image.CreateEmpty(TW, TH, false, Image.Format.Rgba8);
      for (int y = 0; y < TH; y++) for (int x = 0; x < TW; x++) { float px = ((x + 0.5f) / TW - 0.5f) * W, pz = ((y + 0.5f) / TH - 0.5f) * L, r = 0.035f;
          float qx = Mathf.Abs(px) - (hw - 0.01f - r), qz = Mathf.Abs(pz) - (hl - 0.02f - r), d = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qz, 0)).Length() + Mathf.Min(Mathf.Max(qx, qz), 0) - r;   // distance outside the car's rounded outline (m)
          float a = d <= 0 ? 0.8f : 0.95f * Mathf.Exp(-d / 0.032f); img.SetPixel(x, y, new Color(1, 1, 1, a)); }
      _neonFloor.AlbedoTexture = ImageTexture.CreateFromImage(img); _neonFloor.AlbedoColor = new Color(c.R, c.G, c.B, 0.8f); ((PlaneMesh)_neonFloorNode.Mesh).Size = new Vector2(W, L);
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
      _body.Transform = new Transform3D(new Basis(Vector3.Right, -(float)c.Pitch) * new Basis(Vector3.Back, (float)c.Roll * 1.6f), new Vector3(0, (float)c.Heave + RideOffset, 0));
      if (_susp == null && _chassis != null && !Ghost && !Show && _wheel[0] != null && _wheel[3] != null) { var w = new Vector3[4]; for (int i = 0; i < 4; i++) w[i] = _wheelBase[i].Origin; _susp = Suspension.Build(_chassis, w); if (_susp == null) _chassis = null; }
      if (_susp != null && _susp.Visible != Detail) _susp.Visible = Detail;
      if (_layTick++ % 45 == 0) Lay();
      if (Detail) _susp?.Update((_body.Transform * _chassis.Transform).AffineInverse(), (float)c.Ax, (float)c.Ay);
      if (_lampFloor != null && _lamps.Visible) { _lampFloor.Visible = !c.Air; if (!c.Air) PlaceReflections(); }
      _blob.Visible = !c.Air && !Ghost; if (_neonFloorNode.Visible != (_neon.Visible && !c.Air)) _neonFloorNode.Visible = _neon.Visible && !c.Air;
      for (int i = 0; i < 4; i++) {
        if (_wheel[i] == null || _spin[i] == null) continue;
        bool front = i < 2; _wheel[i].Transform = new Transform3D(new Basis(Vector3.Up, front ? (float)c.Steer : 0) * _wheelBase[i].Basis, _wheelBase[i].Origin);
        _spin[i].Quaternion = _camber[i] * new Quaternion(Vector3.Back, _spinSign[i] * (float)((front ? c.SpinF : c.Spin) % (Math.PI * 2)));
      }
    }
  }
}
