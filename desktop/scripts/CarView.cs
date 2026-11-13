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
    public int Body; public float RoofY = 0.133f, HalfW = 0.095f, HalfL = 0.21f;
    public static readonly string[] Bonnets = { "BODY COLOUR", "CARBON" }; /* one livery template: a solid colour, the bonnet in it or in carbon, and the rims in their own colour */
    public static string HideTest; // test: hide shell parts whose name contains this
    // options carried over from the web version: the body shell on or off, neon under the car, tinted or clear windows
    readonly System.Collections.Generic.List<BaseMaterial3D> _gaps = new(); readonly System.Collections.Generic.List<Vector3> _oldStick = new(); Node3D _shell, _neon; StandardMaterial3D _neonBar, _neonFloor; OmniLight3D _neonLight; bool _clear, _onBoard;
    public static readonly (string name, Color col)[] Neons = { ("OFF", Colors.Black), ("PINK", new Color(1f, 0.25f, 0.71f)), ("ICE BLUE", new Color(0.25f, 0.8f, 1f)), ("GREEN", new Color(0.3f, 1f, 0.45f)), ("AMBER", new Color(1f, 0.62f, 0.12f)), ("PURPLE", new Color(0.62f, 0.3f, 1f)), ("WHITE", new Color(1f, 0.97f, 0.9f)) };
    public Node3D Fpv; // the on-board camera's seat: inside the cabin, on the centre line, behind the windscreen; it leans with the body
    public Node3D StickCam; Node3D _stick; // a second FPV camera on a short stick: fixed to the roof, reaching back, looking forward over the car
    public static int StickSide = 1; public static float StickUp = 0, StickFwd = 0.03f; /* door sticker placement (test) */
    public static float StickY = 0.195f, StickZ = -0.5f, StickTilt = 7f; // 19.5 cm up: it has to pass under the bridge (23 cm)
    public static float FpvX = 0, FpvY = 0.102f, FpvZ = 0.006f, FpvTilt = 5f;
    static readonly string[] Names = { "WheelFa", "WheelFb", "WheelRa", "WheelRb" };

    public override void _Ready() {
      var model = GD.Load<PackedScene>("res://assets/" + Bodies[Body % Bodies.Length].file + ".glb").Instantiate<Node3D>(); AddChild(model);
      _body = new Node3D { Name = "Body" }; AddChild(_body);
      foreach (var n in new[] { "Shell", "Chassis", "Mounts" }) { var p = model.FindChild(n, true, false) as Node3D; if (p == null) { GD.PrintErr("missing ", n); continue; } var xf = p.Transform; p.GetParent().RemoveChild(p); _body.AddChild(p); p.Transform = xf; if (n == "Shell") _shell = p; }
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
        var bb = paintMi.GetAabb(); float zTail = bb.Position.Z, hw = bb.Size.X / 2 * 0.9f; var wing = new Node3D { Name = "Wing" }; _shell.AddChild(wing);
        var black = new StandardMaterial3D { AlbedoColor = new Color(0.012f, 0.012f, 0.014f), Roughness = 0.28f }; float zFoot = zTail + 0.04f, deck = 0;
        foreach (int sd in new[] { -1, 1 }) { float d = Hit(paintG, new Vector3(sd * 0.04f, 0.4f, zFoot), Vector3.Down); if (d > 0) deck = Math.Max(deck, 0.4f - d); }
        if (deck < 0.03f) deck = bb.Position.Y + bb.Size.Y * 0.7f; float bladeY = Math.Max(deck + 0.034f, bb.Position.Y + bb.Size.Y - 0.004f), bladeZ = zTail + 0.016f;
        void Box(Vector3 size, Vector3 pos, Vector3 rotDeg) => wing.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = black, Position = pos, RotationDegrees = rotDeg });
        Box(new Vector3(hw * 2, 0.0028f, 0.036f), new Vector3(0, bladeY, bladeZ), new Vector3(9, 0, 0));                      // the blade, its trailing edge kicked up
        Box(new Vector3(hw * 2, 0.0022f, 0.008f), new Vector3(0, bladeY + 0.0042f, bladeZ - 0.019f), new Vector3(42, 0, 0));    // the lip along the trailing edge
        foreach (int sd in new[] { -1, 1 }) {
          Box(new Vector3(0.0022f, 0.03f, 0.05f), new Vector3(sd * hw, bladeY - 0.004f, bladeZ - 0.002f), Vector3.Zero);          // end plates
          Vector3 foot = new Vector3(sd * 0.04f, deck - 0.002f, zFoot), head = new Vector3(sd * 0.04f, bladeY - 0.001f, bladeZ + 0.004f), dv = head - foot;
          Box(new Vector3(0.0022f, dv.Length(), 0.02f), (foot + head) / 2, new Vector3(Mathf.RadToDeg(Mathf.Atan2(dv.Z, dv.Y)), 0, 0)); } }  // stays
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
    public void SetPaint(Color a, bool carbon) {
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
