// Drift Master, desktop version: first playable. One hall, one layout (HILLSIDE), one car (the sedan).
// Keys: W/S or arrows drive, A/D steer, R reset, C camera (pilot's stand, ceiling, on board, stick), P paint, F2 shadows, F3 stats, F4 resolution, F11 full screen, Esc pause.
// Gamepad: right trigger throttle, left trigger brake/reverse, left stick steer, Y reset, X camera.
using Godot;
using System;

namespace DriftMaster {
  public partial class Main : Node3D {
    Track _t; CarPhysics _car; CarView _view; Camera3D _cam; DirectionalLight3D _sun; CarAudio _audio;
    SubViewport _fpvView; Camera3D _fpvCam; Control _fpvScreen; // the on-board picture: its own 720 x 480 frame, shown as 4:3
    Label _hud, _stats, _help, _pause; double _acc; int _camMode = 1; /* 1 the pilot's stand, 2 ceiling, 3 on board, 4 stick */ Vector3 _camPos, _camLook; bool _camSnap = true;
    // lap timing
    double _lap, _last, _best; int _next = 1; bool _timing; double _driftScore;
    // test hooks: --shot=file --frames=n --auto --cam=n --at=index
    float[] _view6; bool _bare; double _poseSteer = double.NaN; /* test and icon shots: the car alone on a clear background, wheels turned */ string _shot; int _frames = 90, _frame; bool _auto, _autoDrift; int _paintIdx; bool _shellOn = true, _clearGlass; int _neonIdx; /* car options, kept between runs in user://settings.cfg */ float[] _scales = { 1f, 0.85f, 0.7f }; int _scaleIdx;
    static readonly (string name, Color col)[] Paints = { ("BLACK", new Color(0.09f, 0.094f, 0.106f)), ("WHITE", new Color(0.86f, 0.87f, 0.85f)), ("RED", new Color(0.72f, 0.05f, 0.06f)), ("BLUE", new Color(0.06f, 0.22f, 0.6f)), ("YELLOW", new Color(0.95f, 0.72f, 0.05f)), ("GREEN", new Color(0.1f, 0.42f, 0.3f)),
      ("HOT PINK", new Color(0.93f, 0.12f, 0.62f)), ("PURPLE", new Color(0.36f, 0.14f, 0.7f)), ("TEAL", new Color(0.03f, 0.5f, 0.48f)), ("LIME", new Color(0.62f, 0.88f, 0.1f)), ("ORANGE", new Color(0.95f, 0.38f, 0.05f)), ("GREY", new Color(0.36f, 0.38f, 0.4f)) };
    int _paintB = 1, _livery, _body; /* second colour, livery, body shell */

    public override void _Ready() {
      int at = -1;
      foreach (var a in OS.GetCmdlineUserArgs()) {
        if (a.StartsWith("--shot=")) _shot = a.Substring(7); else if (a.StartsWith("--frames=")) _frames = int.Parse(a.Substring(9));
        else if (a == "--auto") _auto = true; else if (a == "--drift") _auto = _autoDrift = true; else if (a.StartsWith("--cam=")) _camMode = int.Parse(a.Substring(6)); else if (a.StartsWith("--at=")) at = int.Parse(a.Substring(5));
        else if (a.StartsWith("--paint=")) _paintIdx = int.Parse(a.Substring(8));
        else if (a.StartsWith("--fpv=")) { var q = Array.ConvertAll(a.Substring(6).Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)); CarView.FpvX = q[0]; CarView.FpvY = q[1]; CarView.FpvZ = q[2]; CarView.FpvTilt = q[3]; }
        else if (a.StartsWith("--stick=")) { var q = Array.ConvertAll(a.Substring(8).Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)); CarView.StickY = q[0]; CarView.StickZ = q[1]; CarView.StickTilt = q[2]; }
        else if (a.StartsWith("--body=")) _body = int.Parse(a.Substring(7)); else if (a.StartsWith("--livery=")) _livery = int.Parse(a.Substring(9)); else if (a.StartsWith("--paintb=")) _paintB = int.Parse(a.Substring(9));
        else if (a == "--noshell") _shellOn = false; else if (a == "--clear") _clearGlass = true; else if (a.StartsWith("--neon=")) _neonIdx = int.Parse(a.Substring(7));
        else if (a.StartsWith("--solo=")) CarAudio.Solo = a.Substring(7);
        else if (a == "--bare") _bare = true; else if (a.StartsWith("--steer=")) _poseSteer = double.Parse(a.Substring(8), System.Globalization.CultureInfo.InvariantCulture);
        else if (a.StartsWith("--orbit=")) { _view6 = Array.ConvertAll(a.Substring(8).Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)); _camMode = 8; }
        else if (a.StartsWith("--view=")) { _view6 = Array.ConvertAll(a.Substring(7).Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)); _camMode = 9; }
      }
      // the layout sat at z = -4.9..-1.9 in the big warehouse; here it is centred in a hall of its own (9.0 x 5.0 m)
      _t = Track.FromJson(FileAccess.GetFileAsString("res://assets/hillside.json"), -3.4, 4.5, 2.5);
      if (!_bare) AddChild(Hall.Build(_t)); else { GetViewport().TransparentBg = true; }
      BuildLights();
      if (_shot == null) LoadOptions();
      _car = new CarPhysics { T = _t }; _view = new CarView { Body = _body }; AddChild(_view);
      ApplyOptions();
      if (at >= 0) { int i = at % _t.N; _car.Place(_t.X[i], _t.Z[i], Math.Atan2(_t.TX[i], _t.TZ[i])); } else ResetCar();
      _cam = new Camera3D { Near = 0.02f, Far = 40, Fov = 58 }; AddChild(_cam); _cam.MakeCurrent();
      BuildOnBoard();
      _audio = new CarAudio(); AddChild(_audio); _car.OnBump = k => _audio.Bump(k);
      BuildHud();
      _view.Pose(_car, _t); UpdateCamera(0);
    }

    void BuildLights() {
      var env = new Godot.Environment {
        BackgroundMode = _bare ? Godot.Environment.BGMode.ClearColor : Godot.Environment.BGMode.Color, BackgroundColor = new Color(0.05f, 0.05f, 0.055f),
        AmbientLightSource = Godot.Environment.AmbientSource.Color, AmbientLightColor = new Color(0.82f, 0.84f, 0.88f), AmbientLightEnergy = 0.4f,
        TonemapMode = Godot.Environment.ToneMapper.Filmic, TonemapWhite = 1.6f,
      };
      AddChild(new WorldEnvironment { Environment = env });
      // the tube lights overhead as one soft downward light with a shadow, plus two fills so the walls fall off
      _sun = new DirectionalLight3D { LightEnergy = 0.6f, LightColor = new Color(1, 0.98f, 0.94f), ShadowEnabled = true, DirectionalShadowMode = DirectionalLight3D.ShadowMode.Orthogonal, DirectionalShadowMaxDistance = 7, ShadowBlur = 2.0f, ShadowOpacity = 0.85f, ShadowBias = 0.06f, ShadowNormalBias = 1.5f };
      _sun.RotationDegrees = new Vector3(-78, 24, 0); AddChild(_sun);
      foreach (int s in new[] { -1, 1 }) AddChild(new OmniLight3D { Position = new Vector3(s * 2.2f, Hall.CeilY - 0.4f, 0), OmniRange = 6.5f, LightEnergy = 0.3f, OmniAttenuation = 1.2f, LightColor = new Color(1, 0.97f, 0.92f) });
      // one probe for the whole room: the glossy tiles mirror the ceiling lights
      // the box reaches a little past the floor, walls and ceiling: a surface lying exactly on the box's face is half in, half out,
      // and its reflection flickers on and off
      var probe = new ReflectionProbe { Size = new Vector3((float)_t.HallX * 2 + 0.5f, Hall.CeilY + 0.5f, (float)_t.HallZ * 2 + 0.5f), Position = new Vector3(0, Hall.CeilY / 2, 0), BoxProjection = true, Interior = true, UpdateMode = ReflectionProbe.UpdateModeEnum.Once, AmbientMode = ReflectionProbe.AmbientModeEnum.Disabled, EnableShadows = false, Intensity = 0.3f };
      AddChild(probe);
    }

    // The on-board camera is an analog FPV camera (the Tiny Whoop Pinch, as in the 640KFPV simulator): it draws the room
    // into a 720 x 480 NTSC frame, shown stretched to 4:3 with black bars at the sides. 115 degrees across the diagonal:
    // 75 degrees top to bottom.
    void BuildOnBoard() {
      _fpvView = new SubViewport { Size = new Vector2I(720, 480), RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled, Msaa3D = Viewport.Msaa.Msaa2X }; AddChild(_fpvView);
      _fpvCam = new Camera3D { Near = 0.004f, Far = 25, Fov = 75 }; _fpvView.AddChild(_fpvCam); _fpvCam.MakeCurrent();
      var layer = new CanvasLayer { Layer = 0 }; AddChild(layer);
      var bg = new ColorRect { Color = Colors.Black }; bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
      var box = new AspectRatioContainer { Ratio = 4f / 3f }; box.SetAnchorsPreset(Control.LayoutPreset.FullRect);
      var pic = new TextureRect { Texture = _fpvView.GetTexture(), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale, TextureFilter = CanvasItem.TextureFilterEnum.Linear };
      pic.Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/analog.gdshader") };
      box.AddChild(pic); bg.AddChild(box); layer.AddChild(bg); _fpvScreen = bg; bg.Visible = false;
    }
    void OnBoard(bool on) {
      if (_fpvScreen.Visible == on) return;
      _fpvScreen.Visible = on; _fpvView.RenderTargetUpdateMode = on ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
      GetViewport().Disable3D = on; _view.SetOnBoard(on); // the window itself draws no 3D while on board: only the small frame is rendered
    }

    void BuildHud() {
      var layer = new CanvasLayer(); AddChild(layer);
      Label L(Vector2 pos, int size, Color col) { var l = new Label { Position = pos }; l.AddThemeFontSizeOverride("font_size", size); l.AddThemeColorOverride("font_color", col); l.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.75f)); l.AddThemeConstantOverride("outline_size", 6); layer.AddChild(l); return l; }
      _hud = L(new Vector2(24, 18), 22, Colors.White);
      _stats = L(new Vector2(24, 150), 15, new Color(0.75f, 0.9f, 1f)); _stats.Visible = false;
      _help = L(new Vector2(24, 0), 14, new Color(1, 1, 1, 0.8f)); _help.SetAnchorsPreset(Control.LayoutPreset.BottomLeft); _help.Position = new Vector2(24, -40);
      _help.Text = "W/S drive · A/D steer · R reset · C camera · T body · P colour · O 2nd colour · L livery · B shell · G neon · V windows · M mute · N motor sound · - = volume · F2 shadows · F3 stats · F4 resolution · F11 full screen · Esc pause";
      _help.AnchorTop = 1; _help.AnchorBottom = 1; _help.OffsetTop = -40; _help.OffsetBottom = -14; _help.OffsetLeft = 24;
      _pause = L(Vector2.Zero, 34, Colors.White); _pause.Text = "PAUSED\nEsc resume  ·  Q quit"; _pause.HorizontalAlignment = HorizontalAlignment.Center; _pause.VerticalAlignment = VerticalAlignment.Center; _pause.SetAnchorsPreset(Control.LayoutPreset.FullRect); _pause.Visible = false;
      if (_shot != null) _help.Visible = false;
    }

    // another shell: the car on screen is rebuilt; where it is and how it is moving stay
    void SwapBody() { _view.QueueFree(); _view = new CarView { Body = _body }; AddChild(_view); _view.Pose(_car, _t); }

    void ApplyOptions(bool save = false) {
      var ca = Paints[_paintIdx % Paints.Length].col; var cb = Paints[_paintB % Paints.Length].col;
      var cc = _livery == 4 ? ca.Lerp(cb, 0.5f).Darkened(0.35f) : (ca.Luminance + cb.Luminance > 0.9f ? new Color(0.04f, 0.04f, 0.05f) : new Color(0.92f, 0.92f, 0.9f)); // camo's third tone; else an edge colour that shows on both
      _view.SetPaint(ca, cb, cc, _livery);
      { var bd = CarView.Bodies[_body % CarView.Bodies.Length]; _car.SetBody(bd.kg, bd.cgUp, _view.HalfW, _view.HalfL); }
      _view.SetShell(_shellOn); _view.SetNeon(_neonIdx); _view.SetWindows(_clearGlass);
      if (save && _shot == null) { var cf = new ConfigFile(); cf.SetValue("car", "paint", _paintIdx); cf.SetValue("car", "paint_b", _paintB); cf.SetValue("car", "livery", _livery); cf.SetValue("car", "body", _body); cf.SetValue("car", "shell", _shellOn); cf.SetValue("car", "neon", _neonIdx); cf.SetValue("car", "clear_windows", _clearGlass); cf.Save("user://settings.cfg"); }
    }
    void LoadOptions() {
      var cf = new ConfigFile(); if (cf.Load("user://settings.cfg") != Error.Ok) return;
      _paintIdx = (int)cf.GetValue("car", "paint", 0); _paintB = Math.Clamp((int)cf.GetValue("car", "paint_b", 1), 0, Paints.Length - 1); _livery = Math.Clamp((int)cf.GetValue("car", "livery", 0), 0, CarView.Liveries.Length - 1); _body = Math.Clamp((int)cf.GetValue("car", "body", 0), 0, CarView.Bodies.Length - 1); _shellOn = (bool)cf.GetValue("car", "shell", true); _neonIdx = (int)cf.GetValue("car", "neon", 0); _clearGlass = (bool)cf.GetValue("car", "clear_windows", false);
      _paintIdx = Math.Clamp(_paintIdx, 0, Paints.Length - 1); _neonIdx = Math.Clamp(_neonIdx, 0, CarView.Neons.Length - 1);
    }
    double _sayT; string _sayText = "";
    void Say(string s) { _sayText = s; _sayT = 1.6; }

    void ResetCar() { _car.Place(_t.Start[0], _t.Start[1], _t.Start[2]); _timing = false; _lap = 0; _next = 1; _camSnap = true; }

    public override void _UnhandledInput(InputEvent e) {
      bool key(Key k) => e is InputEventKey ek && ek.Pressed && !ek.Echo && ek.Keycode == k;
      bool pad(JoyButton b) => e is InputEventJoypadButton jb && jb.Pressed && jb.ButtonIndex == b;
      if (key(Key.Escape) || pad(JoyButton.Start)) _pause.Visible = !_pause.Visible;
      else if (key(Key.Q) && _pause.Visible) GetTree().Quit();
      else if (key(Key.R) || pad(JoyButton.Y)) ResetCar();
      else if (key(Key.C) || pad(JoyButton.X)) { _camMode = _camMode >= 4 ? 1 : _camMode + 1; _camSnap = true; }
      else if (key(Key.P)) { _paintIdx = (_paintIdx + 1) % Paints.Length; ApplyOptions(true); Say("COLOUR " + Paints[_paintIdx].name); }
      else if (key(Key.O)) { _paintB = (_paintB + 1) % Paints.Length; ApplyOptions(true); Say("SECOND COLOUR " + Paints[_paintB].name); }
      else if (key(Key.L)) { _livery = (_livery + 1) % CarView.Liveries.Length; ApplyOptions(true); Say("LIVERY " + CarView.Liveries[_livery]); }
      else if (key(Key.T)) { _body = (_body + 1) % CarView.Bodies.Length; SwapBody(); ApplyOptions(true); Say("BODY " + CarView.Bodies[_body].name); }
      else if (key(Key.B)) { _shellOn = !_shellOn; ApplyOptions(true); Say(_shellOn ? "SHELL ON" : "SHELL OFF"); }
      else if (key(Key.G)) { _neonIdx = (_neonIdx + 1) % CarView.Neons.Length; ApplyOptions(true); Say("NEON " + CarView.Neons[_neonIdx].name); }
      else if (key(Key.V)) { _clearGlass = !_clearGlass; ApplyOptions(true); Say(_clearGlass ? "CLEAR WINDOWS" : "TINTED WINDOWS"); }
      else if (key(Key.F2)) _sun.ShadowEnabled = !_sun.ShadowEnabled;
      else if (key(Key.M)) _audio.Muted = !_audio.Muted;
      else if (key(Key.N)) _audio.Motor = !_audio.Motor;
      else if (key(Key.Minus)) _audio.Volume = Math.Max(0, _audio.Volume - 0.1f);
      else if (key(Key.Equal)) _audio.Volume = Math.Min(1, _audio.Volume + 0.1f);
      else if (key(Key.F3)) _stats.Visible = !_stats.Visible;
      else if (key(Key.F4)) { _scaleIdx = (_scaleIdx + 1) % _scales.Length; GetViewport().Scaling3DScale = _scales[_scaleIdx]; }
      else if (key(Key.F11)) { var w = GetWindow(); w.Mode = w.Mode == Window.ModeEnum.Fullscreen ? Window.ModeEnum.Windowed : Window.ModeEnum.Fullscreen; }
    }

    CarInput ReadInput() {
      var inp = new CarInput();
      if (_auto) { // a simple driver for test shots: aim at the lane ahead, steady throttle
        int i = (_t.Idx + 22) % _t.N; double dx = _t.X[i] - _car.X, dz = _t.Z[i] - _car.Z, want = Math.Atan2(dx, dz), dh = want - _car.Hd; dh = Math.Atan2(Math.Sin(dh), Math.Cos(dh));
        inp.Steer = Math.Clamp(dh * 1.6, -1, 1); inp.Thr = _autoDrift ? 1 : 0.34; inp.ThrDigital = _autoDrift; return inp; // --drift: as on a key, the assist holds a slide
      }
      bool up = Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up), dn = Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down);
      bool lf = Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left), rt = Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right);
      inp.Thr = up ? 1 : 0; inp.Brk = dn ? 1 : 0; inp.Steer = (lf ? 1 : 0) - (rt ? 1 : 0); inp.ThrDigital = up;
      if (Input.GetConnectedJoypads().Count > 0) {
        int j = Input.GetConnectedJoypads()[0]; float gt = Input.GetJoyAxis(j, JoyAxis.TriggerRight), gb = Input.GetJoyAxis(j, JoyAxis.TriggerLeft), gs = Input.GetJoyAxis(j, JoyAxis.LeftX);
        if (gt > 0.03f && !up) { inp.Thr = gt; inp.ThrDigital = false; }
        if (gb > 0.03f && !dn) inp.Brk = gb;
        if (Math.Abs(gs) > 0.06f && !lf && !rt) inp.Steer = -Math.Sign(gs) * Math.Pow((Math.Abs(gs) - 0.06) / 0.94, 1.4);
      }
      return inp;
    }

    public override void _Process(double delta) {
      if (!_pause.Visible) {
        var inp = ReadInput(); _acc += Math.Min(delta, 0.05); int idx0 = _t.Idx;
        while (_acc >= CarPhysics.DT) { _car.Step(CarPhysics.DT, inp, true); _acc -= CarPhysics.DT; }
        Laps(idx0, Math.Min(delta, 0.05));
      }
      if (!double.IsNaN(_poseSteer)) { _car.Steer = _poseSteer; _car.Roll = -0.035 * Math.Sign(_poseSteer); }
      _view.Pose(_car, _t); UpdateCamera((float)delta); UpdateHud(); if (_bare) { _hud.Visible = false; }
      { // sound is always heard from where the pilot stands, facing the track: the cameras on the car have no microphone
        var earXf = new Transform3D(Basis.Identity, new Vector3(0, 1.45f, (float)_t.HallZ - 0.15f)); // looking along -z, across the hall
        _audio.Step(_car, _view.GlobalTransform, earXf, _pause.Visible, (float)Math.Min(delta, 0.05)); }
      if (_shot != null && ++_frame >= _frames) { GetViewport().GetTexture().GetImage().SavePng(_shot); GD.Print($"shot {_shot} car {_car.X:F2} {_car.Z:F2} y {_car.Y:F3} speed {Math.Sqrt(_car.U * _car.U + _car.V * _car.V):F2} beta {_car.Beta:F2} fps {Engine.GetFramesPerSecond()}"); GetTree().Quit(); }
    }

    // a lap counts when the car passes the three checkpoints in order and then the line
    void Laps(int idx0, double dt) {
      int n = _t.N, i = _t.Idx; if (_timing) _lap += dt;
      int q = i * 4 / n; // which quarter of the lap
      if (q == _next && _next != 0) _next = (_next + 1) % 4;
      bool crossed = idx0 > n * 7 / 8 && i < n / 8;
      if (crossed) { if (_timing && _next == 0) { _last = _lap; if (_best == 0 || _lap < _best) _best = _lap; } _timing = true; _lap = 0; _next = 1; }
    }

    void UpdateCamera(float dt) {
      var p = new Vector3((float)_car.X, (float)_car.Y, (float)_car.Z); Vector3 pos, look; float fov = 58;
      if (_camMode != 3 && _camMode != 4) { OnBoard(false); _view.ShowStick(false); }
      float hx = (float)_t.HallX - 0.15f, hz = (float)_t.HallZ - 0.15f;
      if (_camMode == 8) { // test view round the car: degrees round from its nose, degrees up, distance, lens
        float az = Mathf.DegToRad(_view6[0]) + (float)_car.Hd, el = Mathf.DegToRad(_view6[1]), d = _view6[2];
        look = p + new Vector3(0, 0.055f, 0); pos = look + new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el)) * d; fov = _view6[3]; _camSnap = true;
        _cam.Fov = fov; _cam.Position = pos; _cam.LookAt(look, Vector3.Up); return; }
      if (_camMode == 9) { pos = new Vector3(_view6[0], _view6[1], _view6[2]); look = new Vector3(_view6[3], _view6[4], _view6[5]); fov = _view6.Length > 6 ? _view6[6] : 50; _camSnap = true; } // test view
      else if (_camMode == 1) { // the driver's stand: where you would stand with the transmitter
        pos = new Vector3(0, 1.45f, hz); look = p + new Vector3(0, 0.04f, 0); fov = Mathf.Clamp(Mathf.RadToDeg(2 * Mathf.Atan(0.75f / pos.DistanceTo(look))), 14, 40);
      } else if (_camMode == 2) { pos = new Vector3(0, Hall.CeilY - 0.05f, 0.01f); look = new Vector3(0, 0, 0); fov = 100; } // from the ceiling
      else if (_camMode == 3) { // on board: the camera rides in the cabin and leans with the body
        OnBoard(true); _view.ShowStick(false); _fpvCam.GlobalTransform = _view.Fpv.GlobalTransform; _camSnap = true; return;
      }
      else if (_camMode == 4) { // the stick camera: fixed to the roof on a short stick, behind and above the car, looking forward
        OnBoard(true); _view.SetOnBoard(false); _view.ShowStick(true); _fpvCam.GlobalTransform = _view.StickCam.GlobalTransform; _camSnap = true; return;
      } else { pos = new Vector3(0, 1.45f, hz); look = p; }
      pos.X = Mathf.Clamp(pos.X, -hx, hx); pos.Z = Mathf.Clamp(pos.Z, -hz, hz);
      float k = _camSnap || dt <= 0 ? 1 : 1 - Mathf.Exp(-dt * 12);
      _camPos = _camPos.Lerp(pos, k); _camLook = _camLook.Lerp(look, _camSnap || dt <= 0 ? 1 : 1 - Mathf.Exp(-dt * 14)); _camSnap = false;
      _cam.Fov = fov; _cam.Position = _camPos;
      if (_camMode == 2) _cam.LookAt(_camLook, Vector3.Forward); else _cam.LookAt(_camLook, Vector3.Up);
    }

    static string T(double s) => s <= 0 ? "--:--.--" : $"{(int)(s / 60):00}:{s % 60:00.00}";
    void UpdateHud() {
      double sp = Math.Sqrt(_car.U * _car.U + _car.V * _car.V);
      _hud.Text = $"LAP   {T(_timing ? _lap : 0)}\nLAST  {T(_last)}\nBEST  {T(_best)}\n{sp * 36:0} km/h scale   ANGLE {Math.Abs(_car.Beta) * 180 / Math.PI:0}°";
      if (_sayT > 0) { _sayT -= GetProcessDeltaTime(); _hud.Text += "\n\n" + _sayText; }
      if (_stats.Visible) _stats.Text = $"{Engine.GetFramesPerSecond()} fps   draw calls {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame):0}   tris {Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame):0}\nresolution {_scales[_scaleIdx] * 100:0}%   shadows {(_sun.ShadowEnabled ? "on" : "off")}   {RenderingServer.GetVideoAdapterName()}";
    }
  }
}
