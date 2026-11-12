// The room and the track in it, built in code: a small hall just big enough for the layout, a P-tile floor, painted
// walls, a tiled ceiling with tube lights, the raised part of the track with its bridge, and the barriers.
// Style: photo textures on simple shapes (few materials, few draw calls), so it runs on integrated graphics.
using Godot;
using System;
using System.Collections.Generic;

namespace DriftMaster {
  public static class Hall {
    public const float CeilY = 2.6f, Tile = 1.2192f; // the floor texture holds 4 x 4 tiles of 304.8 mm

    static StandardMaterial3D Mat(string tex, Color? col = null, float rough = 0.8f, float uv = 1) {
      var m = new StandardMaterial3D { AlbedoColor = col ?? Colors.White, Roughness = rough, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
      if (tex != null) m.AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/" + tex);
      m.Uv1Scale = new Vector3(uv, uv, 1); return m;
    }
    static MeshInstance3D Box(Node3D parent, Vector3 size, Vector3 pos, Material mat, bool shadow = false) {
      var mi = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = pos, MaterialOverride = mat };
      mi.CastShadow = shadow ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off; parent.AddChild(mi); return mi;
    }
    public static StandardMaterial3D FloorMat() {
      var m = Mat("ptile_albedo.jpg", null, 1f);
      m.RoughnessTexture = GD.Load<Texture2D>("res://assets/tex/ptile_rough.jpg"); m.RoughnessTextureChannel = BaseMaterial3D.TextureChannel.Red;
      m.Uv1Triplanar = true; m.Uv1WorldTriplanar = true; m.Uv1Scale = Vector3.One / Tile; // tiles line up everywhere: floor, slopes and bridge
      return m;
    }

    public static Node3D Build(Track t) {
      var root = new Node3D { Name = "Hall" };
      float hx = (float)t.HallX, hz = (float)t.HallZ;
      var floorMat = FloorMat();
      var floor = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(hx * 2, hz * 2) }, MaterialOverride = floorMat, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }; root.AddChild(floor);

      // walls: painted plaster, a dark skirting board, and a grey band at driver height
      var wall = Mat("wall.jpg", new Color(0.93f, 0.93f, 0.9f), 0.9f); wall.Uv1Triplanar = true; wall.Uv1WorldTriplanar = true; wall.Uv1Scale = Vector3.One / 2.2f;
      var skirt = Mat(null, new Color(0.13f, 0.14f, 0.15f), 0.6f);
      var band = Mat(null, new Color(0.16f, 0.36f, 0.72f), 0.55f);
      foreach (int s in new[] { -1, 1 }) {
        Box(root, new Vector3(hx * 2, CeilY, 0.06f), new Vector3(0, CeilY / 2, s * (hz + 0.03f)), wall);
        Box(root, new Vector3(0.06f, CeilY, hz * 2), new Vector3(s * (hx + 0.03f), CeilY / 2, 0), wall);
        Box(root, new Vector3(hx * 2, 0.07f, 0.012f), new Vector3(0, 0.035f, s * (hz - 0.006f)), skirt);
        Box(root, new Vector3(0.012f, 0.07f, hz * 2), new Vector3(s * (hx - 0.006f), 0.035f, 0), skirt);
        Box(root, new Vector3(hx * 2, 0.09f, 0.008f), new Vector3(0, 1.05f, s * (hz - 0.004f)), band);
        Box(root, new Vector3(0.008f, 0.09f, hz * 2), new Vector3(s * (hx - 0.004f), 1.05f, 0), band);
      }
      // ceiling: suspended tiles, with tube-light panels that also light the floor's reflections
      var ceil = Mat("ceiling.jpg", new Color(0.86f, 0.86f, 0.84f), 0.95f, 1); ceil.Uv1Scale = new Vector3(hx * 2 / 3.6f, hz * 2 / 3.6f, 1);
      var cm = new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(hx * 2, hz * 2), FlipFaces = true }, Position = new Vector3(0, CeilY, 0), MaterialOverride = ceil, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }; root.AddChild(cm);
      var lamp = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = new Color(1, 0.99f, 0.95f) };
      var lampOn = new StandardMaterial3D { AlbedoColor = Colors.Black, EmissionEnabled = true, Emission = new Color(1, 0.98f, 0.93f), EmissionEnergyMultiplier = 2.2f };
      var frame = Mat(null, new Color(0.75f, 0.76f, 0.77f), 0.5f);
      for (int ix = 0; ix < 4; ix++) for (int iz = 0; iz < 2; iz++) {
        float x = (ix - 1.5f) * (hx * 2 / 4), z = (iz - 0.5f) * (hz * 2 / 2);
        Box(root, new Vector3(1.24f, 0.03f, 0.34f), new Vector3(x, CeilY - 0.015f, z), frame);
        Box(root, new Vector3(1.2f, 0.008f, 0.3f), new Vector3(x, CeilY - 0.034f, z), lampOn);
      }

      BuildDeck(root, t, floorMat);
      BuildBarriers(root, t);
      BuildStartLine(root, t);
      BuildDecor(root, t);
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
      var topMi = new MeshInstance3D { Mesh = top.Commit(), MaterialOverride = floorMat, Name = "Deck" }; root.AddChild(topMi);
      var board = Mat("board.jpg", new Color(0.9f, 0.86f, 0.8f), 0.8f); board.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
      var sideMi = new MeshInstance3D { Mesh = side.Commit(), MaterialOverride = board, Name = "DeckSides" }; root.AddChild(sideMi);
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
          int j = (i + 1) % n; bool raised = t.E[i] > 0.004 || t.E[j] > 0.004;
          double bt = raised ? 0.012 : 0.05, bl = raised ? 0.06 : 0.11, gap = raised ? 0.001 : 0.004, bh = raised ? 0.05 : 0.06, o = t.Hw + bt / 2;
          double ax = t.X[i] + t.NX[i] * s * o, az = t.Z[i] + t.NZ[i] * s * o, bx = t.X[j] + t.NX[j] * s * o, bz = t.Z[j] + t.NZ[j] * s * o;
          double seg = Math.Sqrt((bx - ax) * (bx - ax) + (bz - az) * (bz - az)), d = carry;
          while (d < seg) {
            double px = ax + (bx - ax) * d / seg, pz = az + (bz - az) * d / seg, e = t.E[i] + (t.E[j] - t.E[i]) * d / seg;
            if (!SameLevel(t, px, pz, t.Hw + bt / 2 - 0.01, t.E[i])) { // not where the lane crosses itself on the level
              var basis = new Basis(Vector3.Up, (float)Math.Atan2(bx - ax, bz - az));
              float tilt = (float)Math.Atan2(t.E[j] - t.E[i], seg); basis = basis * new Basis(Vector3.Right, -tilt);
              var xf = new Transform3D(basis, new Vector3((float)px, (float)(bh / 2 + e), (float)pz));
              if (raised) boards.Add((xf, (count / 5) % 2 == 0 ? white : blue)); else { foam.Add((xf, (count / 3) % 2 == 0 ? blue : yellow)); }
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

    // a little life: the club name on the wall, a pit table, a tyre stack. Original artwork only.
    static void BuildDecor(Node3D root, Track t) {
      float hx = (float)t.HallX, hz = (float)t.HallZ;
      var title = new Label3D { Text = "DRIFT MASTER", FontSize = 220, PixelSize = 0.0022f, Modulate = new Color(0.1f, 0.11f, 0.13f), OutlineSize = 0, Position = new Vector3(0, 1.75f, -hz + 0.004f), Shaded = false }; root.AddChild(title);
      var sub = new Label3D { Text = "1:10 RC DRIFT  ·  P-TILE  ·  HILLSIDE LAYOUT", FontSize = 64, PixelSize = 0.0022f, Modulate = new Color(0.16f, 0.36f, 0.72f), OutlineSize = 0, Position = new Vector3(0, 1.42f, -hz + 0.004f), Shaded = false }; root.AddChild(sub);
      var wood = Mat("board.jpg", new Color(0.85f, 0.8f, 0.72f), 0.7f); var steel = Mat(null, new Color(0.2f, 0.21f, 0.23f), 0.5f);
      float tx = -hx + 0.75f, tz = hz - 0.32f;
      Box(root, new Vector3(1.2f, 0.03f, 0.5f), new Vector3(tx, 0.72f, tz), wood, true);
      foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 }) Box(root, new Vector3(0.03f, 0.71f, 0.03f), new Vector3(tx + a * 0.56f, 0.355f, tz + b * 0.22f), steel, true);
    }
  }
}
