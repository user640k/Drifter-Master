// The room and the track in it, built in code: a small hall just big enough for the layout, a P-tile floor, painted
// walls, a tiled ceiling with tube lights, the raised part of the track with its bridge, and the barriers.
// Style: photo textures on simple shapes (few materials, few draw calls), so it runs on integrated graphics.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public static class Hall {
    public static bool Dark; /* lights out: ceiling lights and daylight off, the track lit by its LED strips */
    public static float CeilY => Map == 1 ? 6.0f : 2.6f; public const float Tile = 1.2192f; // the floor texture holds 4 x 4 tiles of 304.8 mm

    static StandardMaterial3D Mat(string tex, Color? col = null, float rough = 0.8f, float uv = 1) {
      var m = new StandardMaterial3D { AlbedoColor = col ?? Colors.White, Roughness = rough, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
      if (tex != null) m.AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/" + tex);
      m.Uv1Scale = new Vector3(uv, uv, 1); return m;
    }
    static MeshInstance3D Box(Node3D parent, Vector3 size, Vector3 pos, Material mat, bool shadow = false) {
      var mi = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = pos, MaterialOverride = mat };
      mi.CastShadow = shadow ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off; parent.AddChild(mi); return mi;
    }
    public static ShaderMaterial FloorMat(Track t) {
      var m = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/floor.gdshader") };
      Texture2D tx(string f) => GD.Load<Texture2D>("res://assets/tex/" + f);
      m.SetShaderParameter("tile_albedo", tx("ptile_albedo.jpg")); m.SetShaderParameter("tile_rough", tx("ptile_rough.jpg"));
      m.SetShaderParameter("wear", tx(t.Wear + ".png")); m.SetShaderParameter("wear_rough", tx(t.Wear + "_rough.jpg"));
      // the bump map's slope over the whole floor (about 8 mm a point), for the reflections. Tile joints and steps are left
      // out: they are too fine for this grid, and the joints are drawn on the tiles already
      { const int W = 1408, H = 896; var px = new byte[W * H * 2]; double sx = t.HallX * 2 / W, sz = t.HallZ * 2 / H; var row0 = new double[W + 1]; var row1 = new double[W + 1];
        for (int i = 0; i <= W; i++) row0[i] = t.BumpBase(-t.HallX + i * sx, -t.HallZ, false);
        for (int j = 0; j < H; j++) { for (int i = 0; i <= W; i++) row1[i] = t.BumpBase(-t.HallX + i * sx, -t.HallZ + (j + 1) * sz, false);
          for (int i = 0; i < W; i++) { double gx = (row0[i + 1] - row0[i]) / sx, gz = (row1[i] - row0[i]) / sz;
            px[(j * W + i) * 2] = (byte)Math.Clamp(127.5 + gx * 25 * 127.5, 0, 255); px[(j * W + i) * 2 + 1] = (byte)Math.Clamp(127.5 + gz * 25 * 127.5, 0, 255); }
          (row0, row1) = (row1, row0); }
        var img = Image.CreateFromData(W, H, false, Image.Format.Rg8, px); img.GenerateMipmaps(); m.SetShaderParameter("bump_slope", ImageTexture.CreateFromImage(img)); }
      m.SetShaderParameter("hall_half", new Vector2((float)t.HallX, (float)t.HallZ)); m.SetShaderParameter("tile_size", Tile);
      { string led = "res://assets/tex/" + t.Wear.Replace("floor_wear", "led") + ".png"; if (Dark && ResourceLoader.Exists(led)) { m.SetShaderParameter("led", GD.Load<Texture2D>(led)); m.SetShaderParameter("led_on", 1.0f); } }
      return m;
    }

    /// clear floor behind the layouts on the driver's side (+z), in metres: room to stand back from the track
    public const double Back = 2.0;
    /// which room: 0 the Cave (11 x 7 m, a 2.6 m tiled ceiling), 1 the Warehouse (the web version's hall, 11.2 x 12.8 m and 6 m high)
    public static int Map; public static readonly string[] MapNames = { "CAVE", "WAREHOUSE" };
    /// half the hall's length (x) and depth (z), in metres, and how far a layout is moved towards the far wall
    public static double HalfX => Map == 1 ? 5.6 : 5.5; public static double HalfZ => Map == 1 ? 6.4 : 2.5 + Back / 2; public static double Shift => Map == 1 ? 0 : Back / 2;
    /// the Warehouse's track deck, seen from above (x, z pairs round its edge): the P-tile is laid on it
    public static readonly float[] DeckEdge = { -3.300f, -6.000f, -3.476f, -5.983f, -3.644f, -5.931f, -3.800f, -5.848f, -3.936f, -5.736f, -4.048f, -5.600f, -4.131f, -5.444f, -4.183f, -5.276f, -4.200f, -5.100f, -4.200f, 3.900f, -4.190f, 3.998f, -4.162f, 4.091f, -4.116f, 4.178f, -4.054f, 4.254f, -3.978f, 4.316f, -3.891f, 4.362f, -3.798f, 4.390f, -3.700f, 4.400f, 1.600f, 4.400f, 2.029f, 4.358f, 2.442f, 4.233f, 2.822f, 4.029f, 3.156f, 3.756f, 3.429f, 3.422f, 3.633f, 3.042f, 3.758f, 2.629f, 3.800f, 2.200f, 3.800f, -4.500f, 3.771f, -4.793f, 3.686f, -5.074f, 3.547f, -5.333f, 3.361f, -5.561f, 3.133f, -5.747f, 2.874f, -5.886f, 2.593f, -5.971f, 2.300f, -6.000f };
    public static Node3D Build(Track t) {
      var root = new Node3D { Name = "Hall" };
      float hx = (float)t.HallX, hz = (float)t.HallZ;
      var floorMat = FloorMat(t);
      if (Map == 1) { // the Warehouse: the web version's hall as it is (walls, stand, doors, fence, fixtures), brought over as a model.
        // Only its track deck is changed: the P-tile floor is laid over it, in the deck's own outline. The model sits 12 mm low so the tile is the top surface.
        var room = GD.Load<PackedScene>("res://assets/warehouse.glb").Instantiate<Node3D>(); room.Position = new Vector3(0, -0.012f, 0); root.AddChild(room);
        foreach (var node in room.FindChildren("*", "MeshInstance3D", true, false)) ((MeshInstance3D)node).CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        var pts = new Vector2[DeckEdge.Length / 2]; for (int i = 0; i < pts.Length; i++) pts[i] = new Vector2(DeckEdge[i * 2], DeckEdge[i * 2 + 1]);
        var tri = Geometry2D.TriangulatePolygon(pts); var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles);
        for (int i = 0; i < tri.Length; i++) { st.SetNormal(Vector3.Up); st.AddVertex(new Vector3(pts[tri[i]].X, 0, pts[tri[i]].Y)); }
        root.AddChild(new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = floorMat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Name = "Tile" });
        BuildBarriers(root, t); BuildStartLine(root, t); return root; }
      var floor = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(hx * 2, hz * 2) }, MaterialOverride = floorMat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }; root.AddChild(floor);

      // walls: painted plaster and a dark skirting board, nothing else
      var wall = Mat("wall.jpg", new Color(0.93f, 0.93f, 0.9f), 0.9f); wall.Uv1Triplanar = true; wall.Uv1WorldTriplanar = true; wall.Uv1Scale = Vector3.One / 2.2f;
      var skirt = Mat(null, new Color(0.13f, 0.14f, 0.15f), 0.6f);
      foreach (int s in new[] { -1, 1 }) {
        Box(root, new Vector3(hx * 2, CeilY, 0.06f), new Vector3(0, CeilY / 2, s * (hz + 0.03f)), wall);
        float cut = s > 0 ? Room.Cut : 0;   // the right-hand wall stops short at the far end: the door corner opens off it there
        Box(root, new Vector3(0.06f, CeilY, hz * 2 - cut), new Vector3(s * (hx + 0.03f), CeilY / 2, cut / 2), wall);
        Box(root, new Vector3(hx * 2, 0.07f, 0.012f), new Vector3(0, 0.035f, s * (hz - 0.006f)), skirt);
        Box(root, new Vector3(0.012f, 0.07f, hz * 2 - cut), new Vector3(s * (hx - 0.006f), 0.035f, cut / 2), skirt);
      }
      // ceiling: suspended tiles, with tube-light panels that also light the floor's reflections
      var ceil = Mat("ceiling.jpg", new Color(0.86f, 0.86f, 0.84f), 0.95f, 1); ceil.Uv1Scale = new Vector3(hx * 2 / 3.6f, hz * 2 / 3.6f, 1);
      var cm = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(hx * 2, hz * 2), FlipFaces = true }, Position = new Vector3(0, CeilY, 0), MaterialOverride = ceil, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }; root.AddChild(cm);
      var lamp = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1, 0.99f, 0.95f) };
      var lampOn = new StandardMaterial3D { AlbedoColor = Colors.Black, EmissionEnabled = true, Emission = new Color(1, 0.98f, 0.93f), EmissionEnergyMultiplier = Dark ? 0.015f : 2.2f };
      var frame = Mat(null, new Color(0.75f, 0.76f, 0.77f), 0.5f);
      for (int ix = 0; ix < 4; ix++) for (int iz = 0; iz < 2; iz++) {
        float x = (ix - 1.5f) * (hx * 2 / 4), z = (iz - 0.5f) * (hz * 2 / 2);
        Box(root, new Vector3(1.24f, 0.03f, 0.34f), new Vector3(x, CeilY - 0.015f, z), frame);
        Box(root, new Vector3(1.2f, 0.008f, 0.3f), new Vector3(x, CeilY - 0.034f, z), lampOn);
      }

      Room.Build(root, t, wall, skirt);
      Merge.Static(root, false, Room.Screen);
      BuildDeck(root, t, floorMat);
      BuildBarriers(root, t);
      BuildStartLine(root, t);
      // BuildNeon(root, t); // the neon sign is switched off for now
      return root;
    }

    // is another stretch of the lap well below this height within r of (x, z)? (then this is the bridge over it)
    static bool UnderIt(Track t, double x, double z, double r, double e) {
      for (int k = 0; k < t.N; k++) if (e - t.E[k] > 0.06) { double dx = x - t.X[k], dz = z - t.Z[k]; if (dx * dx + dz * dz < r * r) return true; }
      return false;
    }
    static bool SameLevel(Track t, double x, double z, double r, double e) {
      for (int k = 0; k < t.N; k++) if (Math.Abs(t.E[k] - e) < 0.06) { double dx = x - t.X[k], dz = z - t.Z[k]; if (dx * dx + dz * dz < r * r) return true; }
      return false;
    }

    // the raised road: its surface, board sides down to the floor (left open where the lap runs underneath), legs
    static void BuildDeck(Node3D root, Track t, Material floorMat) {
      int n = t.N; double w = t.Hw + 0.06; const float th = 0.012f;
      var top = new SurfaceTool(); top.Begin(Mesh.PrimitiveType.Triangles);
      var side = new SurfaceTool(); side.Begin(Mesh.PrimitiveType.Triangles);
      Vector3 P(int k, double s, double y) => new Vector3((float)(t.X[k] + t.NX[k] * s * w), (float)y, (float)(t.Z[k] + t.NZ[k] * s * w));
      void Quad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nrm, float u0 = 0, float u1 = 1, float v0 = 0, float v1 = 1) {
        st.SetNormal(nrm); st.SetUV(new Vector2(u0, v0)); st.AddVertex(a); st.SetNormal(nrm); st.SetUV(new Vector2(u1, v0)); st.AddVertex(b); st.SetNormal(nrm); st.SetUV(new Vector2(u1, v1)); st.AddVertex(c);
        st.SetNormal(nrm); st.SetUV(new Vector2(u0, v0)); st.AddVertex(a); st.SetNormal(nrm); st.SetUV(new Vector2(u1, v1)); st.AddVertex(c); st.SetNormal(nrm); st.SetUV(new Vector2(u0, v1)); st.AddVertex(d);
      }
      bool any = false; float run = 0;
      for (int i = 0; i < n; i++) {
        int j = (i + 1) % n; if (t.E[i] <= 0.002 && t.E[j] <= 0.002) continue; any = true;
        double ya = t.E[i] + 0.0008, yb = t.E[j] + 0.0008;
        Vector3 a1 = P(i, 1, ya), a2 = P(i, -1, ya), b1 = P(j, 1, yb), b2 = P(j, -1, yb);
        var up = (b1 - a1).Cross(a2 - a1).Normalized(); if (up.Y < 0) up = -up;
        Quad(top, a1, b1, b2, a2, up);
        float seg = (float)t.Ds, u0 = run / 1.2f, u1 = (run + seg) / 1.2f; run += seg;
        bool bridge = UnderIt(t, (a1.X + a2.X) / 2, (a1.Z + a2.Z) / 2, t.Hw + 0.03, Math.Max(ya, yb));
        // underside of the bridge
        if (bridge) Quad(side, a1 - new Vector3(0, th, 0), b1 - new Vector3(0, th, 0), b2 - new Vector3(0, th, 0), a2 - new Vector3(0, th, 0), Vector3.Down, u0, u1, 0, 0.9f);
        foreach (int s in new[] { 1, -1 }) {
          Vector3 ea = P(i, s, ya), eb = P(j, s, yb); var nrm = new Vector3((float)(t.NX[i] * s), 0, (float)(t.NZ[i] * s));
          bool open = UnderIt(t, (ea.X + eb.X) / 2, (ea.Z + eb.Z) / 2, t.Hw + 0.09, Math.Max(ya, yb));
          float y0a = open ? ea.Y - 0.05f : 0, y0b = open ? eb.Y - 0.05f : 0; // over the lower road only a fascia; elsewhere boards down to the floor
          Quad(side, ea, eb, new Vector3(eb.X, y0b, eb.Z), new Vector3(ea.X, y0a, ea.Z), nrm, u0, u1, 0, Math.Max(0.02f, (ea.Y - y0a) / 1.2f));
        }
      }
      if (!any) return;
      var topMi = new MeshInstance3D { Mesh = top.Commit(), MaterialOverride = floorMat, Name = "Deck", CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided }; root.AddChild(topMi);
      var board = Mat("board.jpg", new Color(0.9f, 0.86f, 0.8f), 0.8f); board.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
      var sideMi = new MeshInstance3D { Mesh = side.Commit(), MaterialOverride = board, Name = "DeckSides", CastShadow = GeometryInstance3D.ShadowCastingSetting.DoubleSided }; root.AddChild(sideMi);
      // legs under the bridge span, beside the lower road
      var leg = Mat("board.jpg", new Color(0.8f, 0.76f, 0.7f), 0.8f);
      for (int i = 0; i < n; i += 10) {
        if (t.E[i] < 0.08) continue;
        foreach (int s in new[] { 1, -1 }) {
          double x = t.X[i] + t.NX[i] * s * (w - 0.03), z = t.Z[i] + t.NZ[i] * s * (w - 0.03);
          if (!UnderIt(t, x, z, t.Hw + 0.1, t.E[i]) || UnderIt(t, x, z, t.Hw + 0.04, t.E[i])) continue; // only right next to the lower lane, never in it
          Box(root, new Vector3(0.04f, (float)t.E[i], 0.04f), new Vector3((float)x, (float)t.E[i] / 2, (float)z), leg, true);
        }
      }
    }

    // barriers: blue and yellow foam blocks on the floor, low white boards along the raised road
    static void BuildBarriers(Node3D root, Track t) {
      int n = t.N; var foam = new List<(Transform3D, Color)>(); var boards = new List<(Transform3D, Color)>();
      Color blue = new Color(0.08f, 0.3f, 0.78f), yellow = new Color(0.96f, 0.76f, 0.06f), white = new Color(0.94f, 0.94f, 0.92f);
      foreach (int s in new[] { 1, -1 }) {
        double carry = 0; int count = 0;
        for (int i = 0; i < n; i++) {
          int j = (i + 1) % n; bool raised = t.Boards || t.E[i] > 0.004 || t.E[j] > 0.004; // boards: on the raised road, and all round a track marked out with them
          double bt = raised ? 0.012 : 0.05, bl = raised ? 0.06 : 0.11, gap = raised ? 0.001 : 0.004, bh = raised ? 0.05 : 0.06, o = t.Hw + bt / 2;
          double ax = t.X[i] + t.NX[i] * s * o, az = t.Z[i] + t.NZ[i] * s * o, bx = t.X[j] + t.NX[j] * s * o, bz = t.Z[j] + t.NZ[j] * s * o;
          double seg = Math.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az)), d = carry;
          while (d < seg) {
            double px = ax + (bx - ax) * d / seg, pz = az + (bz - az) * d / seg, e = t.E[i] + (t.E[j] - t.E[i]) * d / seg;
            if (!SameLevel(t, px, pz, t.Hw + bt / 2 - 0.01, t.E[i])) { // not where the lane crosses itself on the level
              var basis = new Basis(Vector3.Up, (float)Math.Atan2(bx - ax, bz - az));
              float tilt = (float)Math.Atan2(t.E[j] - t.E[i], seg); basis = basis * new Basis(Vector3.Right, -tilt);
              var xf = new Transform3D(basis, new Vector3((float)px, (float)(bh / 2 + e), (float)pz));
              if (raised) boards.Add((xf, t.Boards ? white : (count / 5) % 2 == 0 ? white : blue)); else { foam.Add((xf, (count / 3) % 2 == 0 ? blue : yellow)); }
              count++;
            }
            d += bl + gap;
          }
          carry = d - seg;
        }
      }
      void Emit(List<(Transform3D, Color)> list, Vector3 size, Material mat, string name) {
        if (list.Count == 0) return;
        var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseColors = true, Mesh = new BoxMesh { Size = size } };
        mm.InstanceCount = list.Count; for (int i = 0; i < list.Count; i++) { mm.SetInstanceTransform(i, list[i].Item1); mm.SetInstanceColor(i, list[i].Item2); }
        root.AddChild(new MultiMeshInstance3D { Multimesh = mm, MaterialOverride = mat, Name = name });
      }
      var fm = Mat("foam.jpg", null, 0.95f); fm.VertexColorUseAsAlbedo = true; fm.Uv1Scale = new Vector3(1.5f, 1, 1);
      var bm = Mat(null, null, 0.45f); bm.VertexColorUseAsAlbedo = true;
      Emit(foam, new Vector3(0.05f, 0.06f, 0.11f), fm, "Foam"); Emit(boards, new Vector3(0.012f, 0.05f, 0.06f), bm, "Boards");
    }

    static void BuildStartLine(Node3D root, Track t) {
      var img = Image.CreateEmpty(16, 2, false, Image.Format.Rgb8);
      for (int x = 0; x < 16; x++) for (int y = 0; y < 2; y++) img.SetPixel(x, y, (x + y) % 2 == 0 ? new Color(0.92f, 0.92f, 0.9f) : new Color(0.06f, 0.06f, 0.07f));
      var m = new StandardMaterial3D { AlbedoTexture = ImageTexture.CreateFromImage(img), TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest, Roughness = 0.35f };
      var mi = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2((float)t.Hw * 2, 0.1f) }, MaterialOverride = m, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
      mi.Position = new Vector3((float)t.X[0], (float)t.E[0] + 0.0015f, (float)t.Z[0]);
      mi.Rotation = new Vector3(0, (float)Math.Atan2(t.NX[0], t.NZ[0]) + Mathf.Pi / 2, 0); root.AddChild(mi);
    }

    // The one thing on the walls: a neon sign, "Drift Master", with its transformer, and a cable hanging down to a socket.
    static void BuildNeon(Node3D root, Track t) {
      float hz = (float)t.HallZ, wz = -hz; const float cy = 1.6f, w = 3.0f, h = w * 640f / 2048f;
      Texture2D tx(string f) => GD.Load<Texture2D>("res://assets/tex/" + f);
      // light on the wall behind the tubes
      var glowM = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoTexture = tx("neon_glow.png"), Transparency = BaseMaterial3D.TransparencyEnum.Alpha }; // on a light wall the glow is a wash of colour, not added brightness
      root.AddChild(new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(w * 2, h * 2) }, Position = new Vector3(0, cy, wz + 0.004f), MaterialOverride = glowM, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Name = "NeonGlow" });
      // the tubes, standing 3 cm off the wall on small clear posts
      var tubeM = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoTexture = tx("neon_tube.png"), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
      root.AddChild(new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(w, h) }, Position = new Vector3(0, cy, wz + 0.03f), MaterialOverride = tubeM, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Name = "NeonTubes" });
      var post = new StandardMaterial3D { AlbedoColor = new Color(0.85f, 0.87f, 0.9f, 0.55f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 0.2f };
      foreach (float px in new[] { -1.33f, -0.8f, -0.32f, 0.29f, 0.78f, 1.3f }) Box(root, new Vector3(0.012f, 0.012f, 0.03f), new Vector3(px, cy - 0.08f, wz + 0.015f), post);
      // coloured light from the sign into the room
      root.AddChild(new OmniLight3D { Position = new Vector3(-0.8f, cy, wz + 0.45f), LightColor = new Color(1f, 0.2f, 0.58f), LightEnergy = 0.55f, OmniRange = 3.6f, OmniAttenuation = 1.4f });
      root.AddChild(new OmniLight3D { Position = new Vector3(0.65f, cy, wz + 0.45f), LightColor = new Color(0.3f, 0.85f, 1f), LightEnergy = 0.5f, OmniRange = 3.6f, OmniAttenuation = 1.4f });
      // transformer under the right-hand end, two thin leads up to the tube ends
      var black = Mat(null, new Color(0.035f, 0.035f, 0.04f), 0.55f); var white = Mat(null, new Color(0.9f, 0.9f, 0.88f), 0.4f); var cord = black; // black flex and transformer against the light wall
      var tr = new Vector3(1.5f, cy - 0.52f, wz + 0.022f); Box(root, new Vector3(0.16f, 0.07f, 0.044f), tr, cord);
      Cable(root, new Vector3(1.4f, cy - 0.13f, wz + 0.03f), new Vector3(1.38f, cy - 0.36f, wz + 0.02f), new Vector3(1.45f, cy - 0.38f, wz + 0.02f), tr + new Vector3(-0.05f, 0.035f, 0), 0.0022f, cord);
      Cable(root, new Vector3(-1.41f, cy - 0.2f, wz + 0.03f), new Vector3(-1.38f, cy - 0.56f, wz + 0.012f), new Vector3(0.4f, cy - 0.5f, wz + 0.012f), tr + new Vector3(-0.08f, 0, 0), 0.0022f, cord);
      // mains cable: out of the transformer, slack down the wall, up into the plug from below
      var sock = new Vector3(2.1f, 0.32f, wz);
      Box(root, new Vector3(0.086f, 0.086f, 0.008f), sock + new Vector3(0, 0, 0.004f), white);
      Box(root, new Vector3(0.04f, 0.04f, 0.028f), sock + new Vector3(0, 0, 0.022f), cord);
      Cable(root, tr + new Vector3(0.08f, 0, 0), tr + new Vector3(0.42f, -0.02f, 0.005f), sock + new Vector3(-0.28f, -0.42f, 0.02f), sock + new Vector3(0, -0.02f, 0.03f), 0.0035f, cord, new Vector3(0, 0.25f, 0));
      // two clips hold it to the wall
      Box(root, new Vector3(0.016f, 0.01f, 0.012f), tr + new Vector3(0.2f, -0.012f, -0.012f), white);
    }

    // a round cable along a cubic curve (p0..p3 are its control points; endTan, if given, is the direction it arrives from)
    static void Cable(Node3D root, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float r, Material mat, Vector3? endTan = null) {
      if (endTan.HasValue) p2 = new Vector3(p2.X, p2.Y, p2.Z) + (p3 - endTan.Value - p2) * 0.0f; // p2 is already placed below the plug
      const int seg = 28, sides = 6; var st = new SurfaceTool(); st.Begin(Mesh.PrimitiveType.Triangles);
      Vector3 At(float u) { float v = 1 - u; return p0 * (v * v * v) + p1 * (3 * v * v * u) + p2 * (3 * v * u * u) + p3 * (u * u * u); }
      var ring = new Vector3[seg + 1, sides]; var nrm = new Vector3[seg + 1, sides];
      for (int i = 0; i <= seg; i++) {
        float u = i / (float)seg; var c = At(u); var tan = (At(Math.Min(1, u + 0.01f)) - At(Math.Max(0, u - 0.01f))).Normalized();
        var a = tan.Cross(Vector3.Back); if (a.Length() < 0.01f) a = tan.Cross(Vector3.Up); a = a.Normalized(); var b = tan.Cross(a).Normalized();
        for (int k = 0; k < sides; k++) { float an = k * Mathf.Tau / sides; nrm[i, k] = a * Mathf.Cos(an) + b * Mathf.Sin(an); ring[i, k] = c + nrm[i, k] * r; }
      }
      for (int i = 0; i < seg; i++) for (int k = 0; k < sides; k++) { int k2 = (k + 1) % sides;
        void V(int ii, int kk) { st.SetNormal(nrm[ii, kk]); st.AddVertex(ring[ii, kk]); }
        V(i, k); V(i + 1, k); V(i + 1, k2); V(i, k); V(i + 1, k2); V(i, k2); }
      var m2 = (StandardMaterial3D)mat.Duplicate(); m2.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
      root.AddChild(new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = m2, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }
  }
}
