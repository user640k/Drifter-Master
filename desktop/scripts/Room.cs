// What makes the hall a club's room, after a photo of one: a corner at the far right where the door is (set back from
// the room, with a bare concrete floor), a fire extinguisher and a wall television beside it, a wall of banners behind
// the track, and a window with its blind down in the left wall. None of it is in the car's way.
using Godot;
using System;

namespace DriftMaster {
  public static class Room {
    public const float NookW = 1.8f, NookD = 1.3f;   // the door corner: its width along the far wall's line, and how far it is set back
    /// where the television's screen is, for the game to put the overhead camera's picture on
    public static MeshInstance3D Screen;

    static StandardMaterial3D Flat(Color c, float rough = 0.7f, float metal = 0) => new StandardMaterial3D { AlbedoColor = c, Roughness = rough, Metallic = metal };
    static StandardMaterial3D Tex(string file, float rough = 0.7f) => new StandardMaterial3D { AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/" + file), Roughness = rough, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
    static MeshInstance3D Box(Node3D p, Vector3 size, Vector3 pos, Material m, Vector3? rotDeg = null) { var mi = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = m, Position = pos, RotationDegrees = rotDeg ?? Vector3.Zero }; p.AddChild(mi); return mi; }
    /// a flat picture on a wall: facing +z (yaw 0), +x (90), -x (-90)
    static MeshInstance3D Pic(Node3D p, Vector2 size, Vector3 pos, float yawDeg, Material m) { var mi = new MeshInstance3D { Mesh = new QuadMesh { Size = size }, MaterialOverride = m, Position = pos, RotationDegrees = new Vector3(0, yawDeg, 0), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off }; p.AddChild(mi); return mi; }

    public static void Build(Node3D root, Track t, Material wall, Material skirt) {
      float hx = (float)t.HallX, hz = (float)t.HallZ, H = Hall.CeilY, x1 = hx + NookD, z0 = -hz, z1 = -hz + NookW;
      // ---- the door corner
      var concrete = Flat(new Color(0.56f, 0.5f, 0.42f), 0.85f);
      root.AddChild(new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(NookD, NookW) }, MaterialOverride = concrete, Position = new Vector3(hx + NookD / 2, 0.0005f, z0 + NookW / 2), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
      root.AddChild(new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(NookD, NookW), FlipFaces = true }, MaterialOverride = wall, Position = new Vector3(hx + NookD / 2, H, z0 + NookW / 2), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
      Box(root, new Vector3(NookD + 0.06f, H, 0.06f), new Vector3(hx + NookD / 2 + 0.03f, H / 2, z0 - 0.03f), wall);       // the far wall carries on into it
      Box(root, new Vector3(0.06f, H, NookW), new Vector3(x1 + 0.03f, H / 2, z0 + NookW / 2), wall);                         // its end wall
      Box(root, new Vector3(NookD + 0.06f, H, 0.06f), new Vector3(hx + NookD / 2 + 0.03f, H / 2, z1 + 0.03f), wall);        // the return wall between it and the room
      Box(root, new Vector3(NookD, 0.07f, 0.012f), new Vector3(hx + NookD / 2, 0.035f, z1 - 0.006f), skirt); Box(root, new Vector3(0.012f, 0.07f, NookW), new Vector3(x1 - 0.006f, 0.035f, z0 + NookW / 2), skirt);
      root.AddChild(new OmniLight3D { Position = new Vector3(hx + NookD / 2, H - 0.3f, z0 + NookW / 2), OmniRange = 3f, LightEnergy = 0.5f, LightColor = new Color(1, 0.96f, 0.88f), ShadowEnabled = false });
      // the door: a flush brown door in a dark frame, lever handle, closer at the top
      float dx = hx + 0.72f, dz = z0 + 0.012f; var frame = Flat(new Color(0.2f, 0.17f, 0.15f), 0.6f); var steel = Flat(new Color(0.72f, 0.73f, 0.75f), 0.35f, 0.8f);
      Box(root, new Vector3(0.9f, 2.03f, 0.03f), new Vector3(dx, 1.015f, dz + 0.004f), Tex("door.jpg", 0.45f));
      foreach (float sx in new[] { -0.475f, 0.475f }) Box(root, new Vector3(0.05f, 2.08f, 0.05f), new Vector3(dx + sx, 1.04f, dz + 0.012f), frame);
      Box(root, new Vector3(1.0f, 0.05f, 0.05f), new Vector3(dx, 2.055f, dz + 0.012f), frame);
      Box(root, new Vector3(0.035f, 0.11f, 0.012f), new Vector3(dx + 0.36f, 1.0f, dz + 0.026f), steel); Box(root, new Vector3(0.12f, 0.02f, 0.02f), new Vector3(dx + 0.31f, 1.0f, dz + 0.05f), steel);
      Box(root, new Vector3(0.22f, 0.05f, 0.045f), new Vector3(dx - 0.2f, 1.94f, dz + 0.04f), steel); Box(root, new Vector3(0.3f, 0.012f, 0.012f), new Vector3(dx - 0.05f, 1.975f, dz + 0.075f), steel, new Vector3(0, 18, 0));
      Box(root, new Vector3(0.9f, 0.2f, 0.004f), new Vector3(dx, 0.11f, dz + 0.021f), steel);                              // kick plate
      // two yellow notices on the end wall, by the door
      var caution = Tex("caution.jpg", 0.6f); Pic(root, new Vector2(0.3f, 0.225f), new Vector3(x1 - 0.004f, 1.55f, z0 + 0.45f), -90, caution); Pic(root, new Vector2(0.24f, 0.18f), new Vector3(x1 - 0.004f, 1.2f, z0 + 0.5f), -90, caution);
      // ---- on the far wall beside the corner: the fire extinguisher, and the television
      { var red = Flat(new Color(0.75f, 0.06f, 0.05f), 0.35f); var black = Flat(new Color(0.03f, 0.03f, 0.035f), 0.5f); float ex = hx - 0.28f, ez = z0 + 0.075f;
        root.AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.055f, BottomRadius = 0.055f, Height = 0.36f, RadialSegments = 20 }, MaterialOverride = red, Position = new Vector3(ex, 1.02f, ez) });
        root.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.055f, Height = 0.11f, RadialSegments = 20, Rings = 8 }, MaterialOverride = red, Position = new Vector3(ex, 1.2f, ez) });
        Box(root, new Vector3(0.03f, 0.06f, 0.03f), new Vector3(ex, 1.27f, ez), steel); Box(root, new Vector3(0.1f, 0.018f, 0.025f), new Vector3(ex + 0.02f, 1.31f, ez), black); Box(root, new Vector3(0.09f, 0.014f, 0.022f), new Vector3(ex + 0.02f, 1.285f, ez), black, new Vector3(0, 0, -14));
        Box(root, new Vector3(0.016f, 0.3f, 0.016f), new Vector3(ex - 0.07f, 1.13f, ez + 0.01f), black, new Vector3(0, 0, 6));   // the hose
        Box(root, new Vector3(0.1f, 0.1f, 0.002f), new Vector3(ex, 1.02f, ez + 0.056f), Flat(new Color(0.92f, 0.92f, 0.88f), 0.6f));   // its label
        Box(root, new Vector3(0.13f, 0.03f, 0.02f), new Vector3(ex, 1.1f, z0 + 0.012f), steel); }                            // the bracket
      { float tx = hx - 1.3f, ty = 1.72f, tw = 1.02f, th = tw * 9 / 16; var bezel = Flat(new Color(0.02f, 0.02f, 0.024f), 0.4f);
        Box(root, new Vector3(tw + 0.04f, th + 0.04f, 0.035f), new Vector3(tx, ty, z0 + 0.03f), bezel); Box(root, new Vector3(0.2f, 0.2f, 0.02f), new Vector3(tx, ty, z0 + 0.008f), steel);
        Screen = Pic(root, new Vector2(tw, th), new Vector3(tx, ty, z0 + 0.0485f), 0, Flat(new Color(0.01f, 0.01f, 0.012f), 0.2f)); }
      // ---- the wall of banners, behind the track (width in metres, height from each picture's own shape)
      (int id, float x, float y, float w, float aspect)[] hang = { (1, -3.9f, 1.95f, 1.6f, 1200f / 480), (2, -4.2f, 1.2f, 1.25f, 1000f / 440), (8, -2.45f, 1.15f, 1.45f, 1100f / 380), (6, -2.15f, 1.9f, 1.2f, 1000f / 420),
        (3, -0.75f, 1.95f, 1.1f, 900f / 420), (4, -0.7f, 1.2f, 1.25f, 1000f / 440), (5, 0.6f, 1.95f, 0.95f, 760f / 360), (7, 0.9f, 1.25f, 1.05f, 820f / 380), (9, 2.25f, 1.9f, 1.2f, 900f / 300), (2, 2.45f, 1.3f, 0.95f, 1000f / 440) };
      foreach (var b in hang) Pic(root, new Vector2(b.w, b.w / b.aspect), new Vector3(b.x, b.y, z0 + 0.004f + 0.0006f * b.id), 0, Tex($"wall_banner_{b.id}.jpg", 0.55f));
      // ---- the window in the left wall: a white frame and sill, the blind down, daylight behind it
      { float wz = -0.6f, wy = 1.5f, ww = 1.9f, wh = 1.35f, wx = -hx + 0.004f; var white = Flat(new Color(0.93f, 0.93f, 0.91f), 0.5f);
        var blind = new StandardMaterial3D { AlbedoTexture = GD.Load<Texture2D>("res://assets/tex/blinds.jpg"), EmissionEnabled = true, EmissionTexture = GD.Load<Texture2D>("res://assets/tex/blinds.jpg"), Emission = new Color(1, 1, 1), EmissionOperator = BaseMaterial3D.EmissionOperatorEnum.Multiply, EmissionEnergyMultiplier = 0.95f, AlbedoColor = new Color(0.5f, 0.5f, 0.5f), Roughness = 0.7f };   // lit from behind, but not so bright that the slats are lost
        Pic(root, new Vector2(ww, wh), new Vector3(wx, wy, wz), 90, blind);
        foreach (float s in new[] { -1f, 1f }) { Box(root, new Vector3(0.05f, wh + 0.1f, 0.06f), new Vector3(wx + 0.02f, wy, wz + s * (ww / 2 + 0.03f)), white); Box(root, new Vector3(0.05f, 0.06f, ww + 0.12f), new Vector3(wx + 0.02f, wy + s * (wh / 2 + 0.03f), wz), white); }
        Box(root, new Vector3(0.14f, 0.03f, ww + 0.2f), new Vector3(wx + 0.07f, wy - wh / 2 - 0.045f, wz), white);         // the sill
        root.AddChild(new OmniLight3D { Position = new Vector3(-hx + 0.5f, wy, wz), OmniRange = 4.5f, OmniAttenuation = 1.3f, LightEnergy = 0.6f, LightColor = new Color(0.86f, 0.93f, 1f), ShadowEnabled = false }); }
    }
  }
}
