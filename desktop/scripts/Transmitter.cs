// The transmitter in the driver's hands, shown in a corner of the screen: a pistol-grip radio of the kind sold with
// 1/10 drift cars, built here from plain shapes (our own model, no maker's marks). Its steering wheel turns and its
// trigger moves with whatever is driving the car (the real radio, a gamepad or the keys), so the driver sees what the
// hands should be doing. Drawn in its own little world, so the hall's lights and the car do not touch it.
using Godot;
using System;

namespace DriftMaster {
  public partial class Transmitter : Control {
    SubViewport _vp; Node3D _wheel, _trigger; StandardMaterial3D _led; float _s, _t;
    public const float WheelTurn = 35f, TriggerPull = 24f, TriggerPush = 16f;   // degrees: the wheel each way, the trigger in and out
    public static Vector3 CamAt = new Vector3(-0.3f, 0.17f, 0.1f);   // from the driver's right, a little ahead and above: the wheel's face and the trigger both show

    public override void _Ready() {
      MouseFilter = MouseFilterEnum.Ignore; CustomMinimumSize = Size = new Vector2(190, 220);   // small, in the bottom right corner, out of the way of the track
      _vp = new SubViewport { Size = new Vector2I(380, 440), TransparentBg = true, OwnWorld3D = true, Msaa3D = Viewport.Msaa.Msaa4X, RenderTargetUpdateMode = SubViewport.UpdateMode.Always }; AddChild(_vp);
      var pic = new TextureRect { Texture = _vp.GetTexture(), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = MouseFilterEnum.Ignore }; pic.SetAnchorsPreset(LayoutPreset.FullRect); AddChild(pic);
      var env = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = new Color(0, 0, 0, 0), AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color(0.8f, 0.85f, 0.95f), AmbientLightEnergy = 0.9f };
      _vp.AddChild(new WorldEnvironment { Environment = env });
      var cam = new Camera3D { Fov = 34, Near = 0.02f, Far = 3 }; _vp.AddChild(cam); cam.LookAtFromPosition(CamAt, new Vector3(0.012f, 0.105f, 0.004f), Vector3.Up);
      _vp.AddChild(new DirectionalLight3D { Rotation = new Vector3(-0.9f, 2.5f, 0), LightEnergy = 1.1f }); _vp.AddChild(new DirectionalLight3D { Rotation = new Vector3(-0.3f, -2.2f, 0), LightEnergy = 0.45f, LightColor = new Color(0.7f, 0.85f, 1f) });
      var root = new Node3D { Scale = new Vector3(-1, 1, 1) }; _vp.AddChild(root);   // built with the wheel at +x; mirrored, so it is on the driver's right (forward is +z)

      // materials: matt black plastic, carbon-look panels (a 2x2 twill drawn here), foam, gloss black, red, silver
      var body = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.102f, 0.11f), Roughness = 0.5f };
      var img = Image.CreateEmpty(64, 64, false, Image.Format.Rgb8);
      for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) { int cx = x / 8, cy = y / 8; bool over = ((cx + cy) % 4) < 2; float f = ((over ? x : y) % 8 + 0.5f) / 8f, round = 1 - 0.7f * Mathf.Pow(Mathf.Abs(f * 2 - 1), 2.5f), v = (over ? 0.16f : 0.07f) * round + 0.015f; img.SetPixel(x, y, new Color(v, v, v * 1.05f)); }
      img.GenerateMipmaps();
      var carbon = new StandardMaterial3D { AlbedoTexture = ImageTexture.CreateFromImage(img), Uv1Triplanar = true, Uv1Scale = new Vector3(40, 40, 40), Roughness = 0.3f, TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic };
      var foam = new StandardMaterial3D { AlbedoColor = new Color(0.17f, 0.17f, 0.18f), Roughness = 1f };
      var gloss = new StandardMaterial3D { AlbedoColor = new Color(0.05f, 0.05f, 0.055f), Roughness = 0.25f, Metallic = 0.3f };
      var red = new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.05f, 0.05f), Roughness = 0.35f };
      var silver = new StandardMaterial3D { AlbedoColor = new Color(0.7f, 0.72f, 0.75f), Roughness = 0.3f, Metallic = 0.8f };
      _led = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.1f, 0.1f), EmissionEnabled = true, Emission = new Color(0.2f, 1f, 0.4f), EmissionEnergyMultiplier = 0 };
      MeshInstance3D Box(Node3D p, Vector3 size, Vector3 pos, Vector3 rotDeg, Material m) { var mi = new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = m, Position = pos, RotationDegrees = rotDeg }; p.AddChild(mi); return mi; }
      MeshInstance3D Cyl(Node3D p, float r, float h, Vector3 pos, Vector3 rotDeg, Material m, float rTop = -1, int seg = 32) { var mi = new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = rTop < 0 ? r : rTop, BottomRadius = r, Height = h, RadialSegments = seg, Rings = 1 }, MaterialOverride = m, Position = pos, RotationDegrees = rotDeg }; p.AddChild(mi); return mi; }

      // the foot it stands on, and the grip rising from it, leaning forward, with carbon-look sides
      Box(root, new Vector3(0.062f, 0.012f, 0.1f), new Vector3(0, 0.006f, 0.004f), Vector3.Zero, body); Box(root, new Vector3(0.052f, 0.012f, 0.085f), new Vector3(0, 0.017f, 0.004f), Vector3.Zero, body);
      var grip = new Node3D { Position = new Vector3(0, 0.022f, -0.012f), RotationDegrees = new Vector3(12, 0, 0) }; root.AddChild(grip);
      Box(grip, new Vector3(0.036f, 0.105f, 0.046f), new Vector3(0, 0.052f, 0), Vector3.Zero, body);
      foreach (int sd in new[] { -1, 1 }) Box(grip, new Vector3(0.002f, 0.085f, 0.03f), new Vector3(sd * 0.0185f, 0.05f, -0.004f), Vector3.Zero, carbon);
      Cyl(grip, 0.018f, 0.105f, new Vector3(0, 0.052f, -0.022f), Vector3.Zero, body);                                   // the rounded back of the grip
      // the head: the box the wheel and the panel sit on, a carbon plate on top with the name, a sloping panel behind
      var head = new Node3D { Position = new Vector3(0, 0.15f, 0.012f) }; root.AddChild(head);
      Box(head, new Vector3(0.058f, 0.05f, 0.11f), Vector3.Zero, Vector3.Zero, body);
      Box(head, new Vector3(0.05f, 0.003f, 0.06f), new Vector3(0, 0.0262f, 0.02f), Vector3.Zero, carbon);
      var name = new Label3D { Text = "DRIFT MASTER", FontSize = 40, PixelSize = 0.00021f, Modulate = new Color(0.45f, 0.95f, 1f), OutlineSize = 0, Position = new Vector3(0, 0.0281f, 0.02f), RotationDegrees = new Vector3(-90, 180, 0), Scale = new Vector3(-1, 1, 1), Shaded = false }; head.AddChild(name);
      var panel = new Node3D { Position = new Vector3(0, 0.017f, -0.05f), RotationDegrees = new Vector3(-38, 0, 0) }; head.AddChild(panel);
      Box(panel, new Vector3(0.056f, 0.006f, 0.05f), Vector3.Zero, Vector3.Zero, body);
      foreach (int sd in new[] { -1, 1 }) { Cyl(panel, 0.0062f, 0.008f, new Vector3(sd * 0.012f, 0.006f, 0.012f), Vector3.Zero, gloss, 0.0052f); Box(panel, new Vector3(0.0012f, 0.0012f, 0.006f), new Vector3(sd * 0.012f, 0.0104f, 0.0135f), Vector3.Zero, silver); }   // steering trim and gyro trim
      for (int k = -1; k <= 1; k++) Box(panel, new Vector3(0.007f, 0.003f, 0.005f), new Vector3(k * 0.012f, 0.004f, -0.006f), Vector3.Zero, gloss);        // light, cruise, speed
      Cyl(panel, 0.0022f, 0.002f, new Vector3(0.021f, 0.0035f, 0.012f), Vector3.Zero, _led);                                                        // lit while a controller is connected
      Box(panel, new Vector3(0.012f, 0.004f, 0.007f), new Vector3(-0.012f, 0.0035f, -0.018f), Vector3.Zero, gloss);                                 // power
      // the steering wheel on the right: a foam tyre on a spoked rim, turning on an axle across the head
      Cyl(head, 0.009f, 0.012f, new Vector3(0.034f, 0.004f, 0.016f), new Vector3(0, 0, 90), body);
      _wheel = new Node3D { Position = new Vector3(0.054f, 0.004f, 0.016f) }; head.AddChild(_wheel);
      Cyl(_wheel, 0.034f, 0.028f, Vector3.Zero, new Vector3(0, 0, 90), foam, -1, 40);
      Cyl(_wheel, 0.0235f, 0.0286f, Vector3.Zero, new Vector3(0, 0, 90), gloss); Cyl(_wheel, 0.019f, 0.0292f, Vector3.Zero, new Vector3(0, 0, 90), new StandardMaterial3D { AlbedoColor = new Color(0.008f, 0.008f, 0.009f), Roughness = 0.8f });
      for (int k = 0; k < 5; k++) { var sp = new Node3D { RotationDegrees = new Vector3(k * 72, 0, 0) }; _wheel.AddChild(sp); foreach (float off in new[] { -0.0035f, 0.0035f }) Box(sp, new Vector3(0.002f, 0.021f, 0.0022f), new Vector3(0.0142f, 0.0115f, off), new Vector3(off > 0 ? -7 : 7, 0, 0), silver); }
      Cyl(_wheel, 0.005f, 0.0305f, Vector3.Zero, new Vector3(0, 0, 90), silver);
      Box(_wheel, new Vector3(0.002f, 0.009f, 0.012f), new Vector3(0.0142f, -0.0125f, 0), Vector3.Zero, red);                                       // the red caliper: it shows how far the wheel is turned
      // the trigger under the head, in front of the grip: pulled in for throttle, pushed out to brake and reverse
      var trig = new StandardMaterial3D { AlbedoColor = new Color(0.32f, 0.33f, 0.36f), Roughness = 0.35f, Metallic = 0.5f };
      _trigger = new Node3D { Position = new Vector3(0, 0.126f, 0.034f) }; root.AddChild(_trigger);
      Box(_trigger, new Vector3(0.012f, 0.036f, 0.007f), new Vector3(0, -0.019f, 0.006f), new Vector3(-12, 0, 0), trig); Box(_trigger, new Vector3(0.012f, 0.007f, 0.02f), new Vector3(0, -0.038f, -0.002f), new Vector3(-12, 0, 0), trig);
      Box(_trigger, new Vector3(0.012f, 0.026f, 0.005f), new Vector3(0, -0.025f, -0.012f), new Vector3(-12, 0, 0), trig);                              // the loop the finger sits in
    }
    /// steer: +1 full left; thr and brk 0..1; lit: a controller is connected
    public void Show(double steer, double thr, double brk, bool lit) {
      float k = 1 - Mathf.Exp(-28f * (float)GetProcessDeltaTime());   // a key is all or nothing: the hand it stands for is quick, not instant
      _s += ((float)Math.Clamp(steer, -1, 1) - _s) * k; _t += ((float)Math.Clamp(thr - brk, -1, 1) - _t) * k;
      _wheel.RotationDegrees = new Vector3(_s * WheelTurn, 0, 0);                                    // left: the top of the wheel goes forward
      _trigger.RotationDegrees = new Vector3(_t > 0 ? _t * TriggerPull : _t * TriggerPush, 0, 0);    // pulled: back towards the grip
      _led.EmissionEnergyMultiplier = lit ? 2.5f : 0;
    }
  }
}
