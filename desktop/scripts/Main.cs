// Drift Master, desktop version: first playable. One hall, one layout (HILLSIDE), one car (the sedan).
// Keys: W/S or arrows drive, A/D steer, R reset, C camera (pilot's stand, ceiling, on board, stick), P paint, F2 shadows, F3 stats, F4 resolution, F11 full screen, Esc pause.
// Gamepad: right trigger throttle, left trigger brake/reverse, left stick steer, Y reset, X camera.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

namespace DriftMaster {
  public partial class Main : Node3D {
    Track _t; CarPhysics _car; CarView _view; Camera3D _cam; DirectionalLight3D _sun; CarAudio _audio;
    SubViewport _fpvView; Camera3D _fpvCam; Control _fpvScreen; // the on-board picture: its own 720 x 480 frame, shown as 4:3
    Label _hud, _stats, _help; Menu _pause; /* the menu (Esc): while it is open the car is paused */ double _acc; int _camMode = 1; /* 1 the pilot's stand, 2 ceiling, 3 on board, 4 stick */ Vector3 _camPos, _camLook; bool _camSnap = true;
    // lap timing
    double _lap, _last, _best; int _next = 1; bool _timing; double _driftScore;
    // test hooks: --shot=file --frames=n --auto --cam=n --at=index
    int[] _menuAt; float[] _view6; bool _bare; double _poseSteer = double.NaN; /* test and icon shots: the car alone on a clear background, wheels turned */ string _shot; int _frames = 90, _frame; bool _auto, _autoDrift; int _paintIdx; bool _shellOn = true, _clearGlass; int _neonIdx; /* car options, kept between runs in user://settings.cfg */ float[] _scales = { 1f, 0.85f, 0.7f }; int _scaleIdx;
    // Body colours: the strong, clean colours of polycarbonate spray paint, as seen through the clear shell
    static readonly (string name, Color col)[] Paints = { ("BLACK", new Color(0.02f, 0.02f, 0.024f)), ("WHITE", new Color(0.88f, 0.88f, 0.86f)), ("RED", new Color(0.8f, 0.03f, 0.05f)), ("BLUE", new Color(0.02f, 0.2f, 0.78f)), ("YELLOW", new Color(0.95f, 0.74f, 0f)), ("GREEN", new Color(0f, 0.5f, 0.2f)),
      ("HOT PINK", new Color(0.9f, 0.14f, 0.66f)), ("PURPLE", new Color(0.42f, 0.08f, 0.78f)), ("TEAL", new Color(0f, 0.6f, 0.58f)), ("LIME", new Color(0.5f, 0.86f, 0.02f)), ("ORANGE", new Color(0.95f, 0.34f, 0.02f)), ("GREY", new Color(0.4f, 0.42f, 0.44f)) };
    // Rim colours seen most on RC drift cars: white, black and the metal finishes, then the loud ones
    static readonly (string name, Color col, bool metal)[] Rims = { ("BLACK", new Color(0.02f, 0.02f, 0.022f), false), ("WHITE", new Color(0.93f, 0.93f, 0.91f), false), ("GUNMETAL", new Color(0.2f, 0.21f, 0.23f), true), ("SILVER", new Color(0.78f, 0.79f, 0.8f), true),
      ("GOLD", new Color(0.83f, 0.62f, 0.22f), true), ("BRONZE", new Color(0.5f, 0.32f, 0.17f), true), ("NEON YELLOW", new Color(0.8f, 0.95f, 0f), false), ("NEON GREEN", new Color(0.15f, 0.92f, 0.1f), false), ("HOT PINK", new Color(0.92f, 0.08f, 0.55f), false),
      ("RED", new Color(0.8f, 0.03f, 0.05f), false), ("BLUE", new Color(0.02f, 0.2f, 0.78f), false), ("PURPLE", new Color(0.42f, 0.08f, 0.78f), false) };
    int _rimIdx, _livery, _body; /* rim colour, bonnet (0 body colour, 1 carbon), body shell */
    /// the tracks: name, data file, how far the layout is moved along z, floor sheet, white boards all round?
    public static readonly (string name, string file, double z0, string wear, bool boards)[] Tracks = { ("HILLSIDE", "hillside", -3.4, "floor_wear", false), ("INFIELD", "infield", 0.0, "floor_wear_infield", true) };
    static bool _trackChosen; public static int TrackIdx; /* kept across a change of track: the scene is loaded again */

    public override void _Ready() {
      int at = -1;
      foreach (var a in OS.GetCmdlineUserArgs()) {
        if (a.StartsWith("--shot=")) _shot = a.Substring(7); else if (a.StartsWith("--frames=")) _frames = int.Parse(a.Substring(9));
        else if (a == "--auto") _auto = true; else if (a == "--drift") _auto = _autoDrift = true; else if (a.StartsWith("--cam=")) _camMode = int.Parse(a.Substring(6)); else if (a.StartsWith("--at=")) at = int.Parse(a.Substring(5));
        else if (a.StartsWith("--paint=")) _paintIdx = int.Parse(a.Substring(8));
        else if (a.StartsWith("--fpv=")) { var q = Array.ConvertAll(a.Substring(6).Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)); CarView.FpvX = q[0]; CarView.FpvY = q[1]; CarView.FpvZ = q[2]; CarView.FpvTilt = q[3]; }
        else if (a.StartsWith("--stick=")) { var q = Array.ConvertAll(a.Substring(8).Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)); CarView.StickY = q[0]; CarView.StickZ = q[1]; CarView.StickTilt = q[2]; }
        else if (a == "--full") _full = true; else if (a.StartsWith("--comp=")) CompTest = a.Substring(7); else if (a == "--tx") _txShot = true; else if (a.StartsWith("--fakeaxis=")) { _radio.Fake = new float[Radio.Axes]; var q = a.Substring(11).Split(','); for (int k = 0; k < q.Length && k < Radio.Axes; k++) _radio.Fake[k] = float.Parse(q[k], System.Globalization.CultureInfo.InvariantCulture); _radio.On = true; _txShot = true; }
        else if (a.StartsWith("--menu=")) _menuAt = Array.ConvertAll(a.Substring(7).Split(','), int.Parse);
        else if (a.StartsWith("--track=")) { TrackIdx = int.Parse(a.Substring(8)); _trackChosen = true; }
        else if (a.StartsWith("--body=")) _body = int.Parse(a.Substring(7)); else if (a.StartsWith("--livery=")) _livery = int.Parse(a.Substring(9)); else if (a.StartsWith("--rims=")) _rimIdx = int.Parse(a.Substring(7));
        else if (a.StartsWith("--camber=")) { var q = a.Substring(9).Split(','); _setup[1] = int.Parse(q[0]); _setup[2] = int.Parse(q[1]); }
        else if (a.StartsWith("--hide=")) CarView.HideTest = a.Substring(7);
        else if (a.StartsWith("--stk=")) { var q = a.Substring(6).Split(','); CarView.StickSide = int.Parse(q[0]); CarView.StickUp = float.Parse(q[1], System.Globalization.CultureInfo.InvariantCulture); CarView.StickFwd = float.Parse(q[2], System.Globalization.CultureInfo.InvariantCulture); }
        else if (a == "--noshell") _shellOn = false; else if (a == "--clear") _clearGlass = true; else if (a.StartsWith("--neon=")) _neonIdx = int.Parse(a.Substring(7));
        else if (a.StartsWith("--solo=")) CarAudio.Solo = a.Substring(7);
        else if (a == "--bare") _bare = true; else if (a.StartsWith("--steer=")) _poseSteer = double.Parse(a.Substring(8), System.Globalization.CultureInfo.InvariantCulture);
        else if (a.StartsWith("--orbit=")) { _view6 = Array.ConvertAll(a.Substring(8).Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)); _camMode = 8; }
        else if (a.StartsWith("--view=")) { _view6 = Array.ConvertAll(a.Substring(7).Split(','), x => float.Parse(x, System.Globalization.CultureInfo.InvariantCulture)); _camMode = 9; }
      }
      // the layout sat at z = -4.9..-1.9 in the big warehouse; here it is centred in a hall of its own (9.0 x 5.0 m)
      if (_shot == null) LoadOptions();
      // the layouts share one small hall (9.0 x 5.0 m). HILLSIDE sat at z = -4.9..-1.9 in the web version's warehouse and is centred here
      var td = Tracks[TrackIdx % Tracks.Length];
      _t = Track.FromJson(FileAccess.GetFileAsString("res://assets/" + td.file + ".json"), td.z0 + Hall.Back / 2, 4.5, 2.5 + Hall.Back / 2); _t.Name = td.name; _t.Wear = td.wear; _t.Boards = td.boards;
      if (!_bare) AddChild(Hall.Build(_t)); else { GetViewport().TransparentBg = true; }
      BuildLights();
      if (!_bare) { // the driver's stand: a low platform with a step up, in the corner behind the track
        var wood = new StandardMaterial3D { AlbedoColor = new Color(0.16f, 0.15f, 0.14f), Roughness = 0.8f }; var e = DriverEye;
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(1.3f, 0.3f, 0.9f) }, MaterialOverride = wood, Position = new Vector3(e.X - 0.05f, 0.15f, e.Z) });
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.6f, 0.15f, 0.3f) }, MaterialOverride = wood, Position = new Vector3(e.X - 0.2f, 0.075f, e.Z - 0.6f) }); }
      _car = new CarPhysics { T = _t }; _view = new CarView { Body = _body }; AddChild(_view);
      ApplyOptions();
      if (at >= 0) { int i = at % _t.N; _car.Place(_t.X[i], _t.Z[i], Math.Atan2(_t.TX[i], _t.TZ[i])); } else ResetCar();
      _cam = new Camera3D { Near = 0.02f, Far = 40, Fov = 58 }; AddChild(_cam); _cam.MakeCurrent();
      BuildOnBoard();
      _audio = new CarAudio(); AddChild(_audio); _car.OnBump = k => { _audio.Bump(k); if (_comp != 0 && _phase == 1) _jP?.Tap(k); }; _audio.Volume = _vol0; _audio.Motor = _motor0; ApplySetup();
      BuildHud(); BuildCompetition();
      _ghostLap = LoadGhost(false); _ghostRun = LoadGhost(true);
      if (CompTest == "solo" || CompTest == "solo2") StartSolo(); else if (CompTest == "park") Park();
      if (_menuAt != null) { _pause.Open(true); _pause.Goto(_menuAt[0], _menuAt[1]); }
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

    // ---- splash: the game starts as a small window with no frame and no background, so the icon floats over the desktop
    // (the engine's own start-up picture is the same icon, the same way). After two seconds it fades and the window becomes
    // the game's: framed, 1280 x 720, in the middle of the screen. If the graphics driver cannot do a see-through window,
    // the icon simply shows on black.
    float _eyeFov = 50; Vector3 _aheadSm;
    /// where the driver stands: on a low stand at the back of the clear floor, in front of the middle of the track, eyes 1.9 m up
    Vector3 DriverEye => new Vector3(0, 1.9f, (float)_t.HallZ - 0.55f);   // in the middle of the long side, facing the layout
    readonly Radio _radio = new(); Transmitter _tx; bool _txOn = true; /* a real RC transmitter as the controller, and the one shown on screen */
    bool _txShot; bool _full; bool Quiet => _shot != null && !_full; /* test shots skip the splash and the music unless asked (--full) */
    static bool _splashShown; TextureRect _splash; double _splashT;
    void BuildSplash(CanvasLayer layer) {
      if (_splashShown || Quiet) { if (!_splashShown) GameWindow(); _splashShown = true; return; } // once a run, not at every change of track
      _splashShown = true; GetViewport().TransparentBg = true; GetViewport().Disable3D = true;
      _splash = new TextureRect { Texture = GD.Load<Texture2D>("res://splash.png"), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered };
      layer.AddChild(_splash); _splashT = 2.4;
    }
    void GameWindow() {
      var w = GetWindow(); GetViewport().TransparentBg = false; GetViewport().Disable3D = _camMode == 3 || _camMode == 4;
      if (_shot != null && !_full) return; // (test shots keep whatever window they were given)
      w.Transparent = false; w.Borderless = false; w.Size = new Vector2I(1280, 720); w.MoveToCenter();
    }
    void StepSplash(double dt) {
      if (_splash == null) return; var sz = GetViewport().GetVisibleRect().Size; float side = Math.Min(sz.X, sz.Y) * 0.94f; _splash.Size = new Vector2(side, side); _splash.Position = (sz - _splash.Size) / 2;
      _splashT -= dt; _splash.Modulate = new Color(1, 1, 1, (float)Math.Clamp(_splashT / 0.5, 0, 1));
      if (_splashT <= 0) { _splash.QueueFree(); _splash = null; GameWindow(); StartMusicLabel(); }
    }

    // ---- music: the web version's track, as if from the hall's speakers (its own bus with the room on it), and a
    // small "now playing" line at the bottom left for the first five seconds
    static AudioStreamPlayer _music; Label _nowPlaying; double _npT; bool _musicOn = true; float _musicVol = 0.5f;
    const string MusicTitle = "Off Road", MusicArtist = "Aylex";
    void BuildMusic(CanvasLayer layer) {
      int bus = AudioServer.GetBusIndex("Music");
      if (bus < 0) { bus = AudioServer.BusCount; AudioServer.AddBus(bus); AudioServer.SetBusName(bus, "Music"); AudioServer.SetBusSend(bus, "Master");
        AudioServer.AddBusEffect(bus, new AudioEffectReverb { RoomSize = 0.62f, Damping = 0.45f, Spread = 1f, Hipass = 0.25f, PredelayMsec = 28, Dry = 0.7f, Wet = 0.3f }, 0); }
      if (_music == null || !IsInstanceValid(_music)) { // it plays on through a change of track: it belongs to the game, not the scene
        var st = GD.Load<AudioStreamMP3>("res://assets/music/off-road.mp3"); st.Loop = true;
        _music = new AudioStreamPlayer { Stream = st, Bus = "Music" }; GetTree().Root.CallDeferred(Node.MethodName.AddChild, _music);
      }
      _nowPlaying = new Label { Visible = false }; _nowPlaying.AddThemeFontSizeOverride("font_size", 16); _nowPlaying.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.75f)); _nowPlaying.AddThemeConstantOverride("outline_size", 5); layer.AddChild(_nowPlaying);
      ApplyMusic(); if (_splash == null) StartMusicLabel();
    }
    void ApplyMusic() {
      int bus = AudioServer.GetBusIndex("Music"); AudioServer.SetBusVolumeDb(bus, _musicOn && !Quiet ? Mathf.LinearToDb(Math.Max(0.0001f, _musicVol * _musicVol * 0.5f)) : -80);
      if (_music != null && IsInstanceValid(_music) && _music.IsInsideTree() && !_music.Playing && _musicOn && !Quiet) _music.Play();
    }
    void StartMusicLabel() { if (_nowPlaying == null || !_musicOn || Quiet) return; _nowPlaying.Text = $"NOW PLAYING   {MusicTitle}  ·  {MusicArtist}"; _nowPlaying.Visible = true; _npT = 5; }
    void StepMusic(double dt) {
      ApplyMusic(); if (_nowPlaying == null || !_nowPlaying.Visible) return;
      var sz = GetViewport().GetVisibleRect().Size; _nowPlaying.Position = new Vector2(24, sz.Y - 74);
      _npT -= dt; _nowPlaying.Modulate = new Color(1, 1, 1, (float)Math.Clamp(_npT / 0.8, 0, 1)); if (_npT <= 0) _nowPlaying.Visible = false;
    }

    void BuildHud() {
      var layer = new CanvasLayer(); AddChild(layer);
      Label L(Vector2 pos, int size, Color col) { var l = new Label { Position = pos }; l.AddThemeFontSizeOverride("font_size", size); l.AddThemeColorOverride("font_color", col); l.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.75f)); l.AddThemeConstantOverride("outline_size", 6); layer.AddChild(l); return l; }
      _hud = L(new Vector2(24, 18), 22, Colors.White);
      _card = L(new Vector2(60, 250), 19, Colors.White); _card.Visible = false; { var bg = new StyleBoxFlat { BgColor = new Color(0.02f, 0.02f, 0.03f, 0.82f), ContentMarginLeft = 22, ContentMarginRight = 22, ContentMarginTop = 16, ContentMarginBottom = 16 }; _card.AddThemeStyleboxOverride("normal", bg); }   /* the judges' score card */
      _stats = L(new Vector2(24, 150), 15, new Color(0.75f, 0.9f, 1f)); _stats.Visible = false;
      _help = L(new Vector2(24, 0), 14, new Color(1, 1, 1, 0.8f)); _help.SetAnchorsPreset(Control.LayoutPreset.BottomLeft); _help.Position = new Vector2(24, -40);
      _help.Text = "ESC  menu and all keys     W / S  drive     A / D  steer     R  reset     C  camera"; // short, so it fits any window; the rest are in the menu
      _help.AnchorTop = 1; _help.AnchorBottom = 1; _help.OffsetTop = -40; _help.OffsetBottom = -14; _help.OffsetLeft = 24;
      _pause = new Menu { Visible = false }; BuildMenu(); layer.AddChild(_pause);
      { var top = new CanvasLayer { Layer = 3 }; AddChild(top); _tx = new Transmitter(); top.AddChild(_tx); }   // above the menu's shade, so it can be watched while setting the radio up
      BuildSplash(layer); BuildMusic(layer);
      if (Quiet) _help.Visible = false;
    }

    // another shell: the car on screen is rebuilt; where it is and how it is moving stay
    void SwapBody() { _view.QueueFree(); _view = new CarView { Body = _body }; AddChild(_view); _view.Pose(_car, _t); }

    // ---- car setup: the 1/10 RWD drift baseline, and only that. Each setting is a short list inside the baseline's range
    // (index kept in user://settings.cfg). Fixed, as the baseline has them: hard plastic tyres for P-tile, near-parallel
    // Ackermann, a 2S pack with boost and turbo timing, a 0.06 s servo.
    static readonly (string name, double vFree, double fStall, double freeRev)[] Motors = { ("10.5T SENSORED", 12.0, 30, 518), ("13.5T SENSORED", 10.5, 24, 382) };   // free speed (m/s at the tyre), pull at rest (N), motor turns a second at 7.4 V
    static readonly string[] SetupKeys = { "motor", "camber_f", "camber_r", "caster", "toe_f", "toe_r", "oil", "ride", "gyro", "spring", "diff", "weight" };
    static readonly double[] CamberFDeg = System.Linq.Enumerable.Range(-11, 23).Select(v => (double)v).ToArray(), CamberRDeg = CamberFDeg;   // every degree from -11 to +11 (the baseline is -6 to -9 front, -1 to -3 rear)
    static readonly string[] CamberNames = System.Linq.Enumerable.Range(-11, 23).Select(v => v == 0 ? "0 (ZERO)" : (v > 0 ? "+" : "") + v + " DEG").ToArray();
    static readonly int[] SetupDefault = { 1, 4, 9, 1, 1, 1, 1, 1, 3, 1, 0, 0 };   // camber -7 front, -2 rear
    static readonly string[][] SetupNames = {
      new[] { "10.5T SENSORED", "13.5T SENSORED" }, CamberNames, CamberNames, new[] { "6 DEG", "8 DEG", "10 DEG" },
      new[] { "1 DEG OUT", "2 DEG OUT", "3 DEG OUT" }, new[] { "1 DEG IN", "2 DEG IN", "3 DEG IN" }, new[] { "150 CST (15WT)", "200 CST (20WT)", "250 CST (25WT)", "300 CST (30WT)" }, new[] { "6 MM", "7 MM", "8 MM" },
      new[] { "40%", "45%", "50%", "55%", "60%", "65%" }, new[] { "1.1 MM (SOFT)", "1.2 MM", "1.3 MM (FIRMER)" }, new[] { "SPOOL", "TIGHT GEAR DIFF" }, new[] { "40 / 60", "37 / 63", "35 / 65" } };
    readonly int[] _setup = (int[])SetupDefault.Clone(); bool _assist = true; double RideMm => new[] { 6.0, 7, 8 }[_setup[7]];
    void ApplySetup() {
      var S = _car.S; int[] u = _setup;
      S.VFree = Motors[u[0]].vFree; S.FStall = Motors[u[0]].fStall; if (_audio != null) { _audio.MotorVFree = S.VFree; _audio.MotorFreeRev = Motors[u[0]].freeRev; _audio.MotorPitch = u[0] == 0 ? 1.4 : 1.3; }
      S.CamberF = CamberFDeg[u[1]]; S.CamberR = CamberRDeg[u[2]]; S.CasterDeg = new[] { 6.0, 8, 10 }[u[3]]; S.ToeF = u[4] + 1; S.ToeR = u[5] + 1;
      S.CMul = new[] { 0.86, 1, 1.12, 1.22 }[u[6]]; S.GyroGain = new[] { 0.4, 0.45, 0.5, 0.55, 0.6, 0.65 }[u[8]]; S.KMul = new[] { 0.71, 1, 1.38 }[u[9]];   // spring rate goes as wire thickness to the fourth power
      S.GearDiff = u[10] == 1; S.WeightRear = new[] { 0.60, 0.63, 0.65 }[u[11]]; S.Assist = _assist;
      S.TireMul = 0.88; S.FFront = 0.5; S.AckK = 0.15; S.EscV = 1.25; S.EscF = 0.95;   // the fixed parts of the baseline
    }

    void BuildMenu() {
      Menu.Row R(string label, Func<string> val, Action<int> ch, string note = "") => new Menu.Row { Label = label, Value = val, Change = ch, Note = note };
      int Cyc(int v, int d, int n) => ((v + d) % n + n) % n;
      var car = new List<Menu.Row> {
        R("BODY", () => CarView.Bodies[_body].name, d => { _body = Cyc(_body, d, CarView.Bodies.Length); SwapBody(); ApplyOptions(true); }, "Five shells on the same chassis. A taller, heavier shell rolls a little more."),
        R("COLOUR", () => Paints[_paintIdx].name, d => { _paintIdx = Cyc(_paintIdx, d, Paints.Length); ApplyOptions(true); }),
        R("BONNET", () => _view.HasBonnet ? CarView.Bonnets[_livery] : "-", d => { _livery = Cyc(_livery, d, CarView.Bonnets.Length); ApplyOptions(true); }, "In the body colour, or in carbon weave."),
        R("RIMS", () => Rims[_rimIdx].name, d => { _rimIdx = Cyc(_rimIdx, d, Rims.Length); ApplyOptions(true); }),
        R("SHELL", () => _shellOn ? "ON" : "OFF", d => { _shellOn = !_shellOn; ApplyOptions(true); }, "Off: the bare chassis, as on a setup stand."),
        R("WINDOWS", () => _clearGlass ? "CLEAR" : "TINTED", d => { _clearGlass = !_clearGlass; ApplyOptions(true); }),
        R("NEON", () => CarView.Neons[_neonIdx].name, d => { _neonIdx = Cyc(_neonIdx, d, CarView.Neons.Length); ApplyOptions(true); }, "Lit tubes under the chassis."),
      };
      string[] labels = { "MOTOR", "FRONT CAMBER", "REAR CAMBER", "FRONT CASTER", "FRONT TOE", "REAR TOE", "SHOCK OIL", "RIDE HEIGHT", "GYRO GAIN", "SPRINGS", "DIFFERENTIAL", "WEIGHT BIAS (F / R)" };
      string[] notes = {
        "10.5T: more wheel speed, for big tracks. 13.5T: smoother torque, for tight layouts. Both on 2S with boost and turbo timing.",
        "Negative: the bottoms of the wheels stand out and the tops lean in; it flattens the leading tyre at high steering angles (-6 to -9 is the baseline). Zero: upright. Positive: the bottoms tuck in and the tops lean out, which gives grip away.", "Negative: bottoms out, tops in. A little (-1 to -3) keeps the rear tyres flat for drive and side bite. Big angles either way put them on an edge: less rear grip.",
        "Self-centring steering, and more camber at full lock.", "Toe-out sharpens the first turn-in and helps start a slide.", "Toe-in steadies the rear through transitions and guards against spinning out.",
        "Thin oil lets the chassis roll and shift its weight quickly on a low-grip floor.", "Low keeps the centre of gravity down; too low and the chassis scrapes.",
        "How much the gyro counter-steers for you. Too much and the front wheels wobble.", "Soft springs let the body roll and load the outside tyres.",
        "A spool turns both rear wheels together. A tight gear diff (thick oil) nearly does.", "More weight on the rear: more forward traction and deeper angles." };
      var setup = new List<Menu.Row>();
      for (int k = 0; k < labels.Length; k++) { int q = k; setup.Add(R(labels[q], () => SetupNames[q][_setup[q]], d => { _setup[q] = Cyc(_setup[q], d, SetupNames[q].Length); ApplySetup(); ApplyOptions(true); }, notes[q])); }
      setup.Add(R("BACK TO DEFAULTS", null, d => { Array.Copy(SetupDefault, _setup, _setup.Length); ApplySetup(); ApplyOptions(true); }, "The middle of the baseline: 13.5T, -7 and -2 camber, 8 caster, 2 toe each end, 200 cSt, 7 mm, 55% gyro, 1.2 mm springs, spool, 40 / 60."));
      string[] cams = { "", "DRIVER", "CEILING", "ON BOARD", "ROOF STICK" };
      var game = new List<Menu.Row> {
        R("TRACK", () => Tracks[TrackIdx].name, d => { TrackIdx = Cyc(TrackIdx, d, Tracks.Length); _trackChosen = true; ApplyOptions(true); GetTree().ReloadCurrentScene(); }, "Changing track builds the hall again and puts the car on the start line."),
        R("CAMERA", () => cams[Math.Clamp(_camMode, 1, 4)], d => { _camMode = 1 + Cyc(_camMode - 1, d, 4); _camSnap = true; }),
        R("VOLUME", () => $"{Math.Round(_audio.Volume * 10)}", d => { _audio.Volume = Math.Clamp(_audio.Volume + 0.1f * d, 0, 1); ApplyOptions(true); }, "The car's sound."),
        R("MUSIC", () => _musicOn ? "ON" : "OFF", d => { _musicOn = !_musicOn; if (!_musicOn && _music != null) _music.Stop(); ApplyOptions(true); if (_musicOn) { ApplyMusic(); StartMusicLabel(); } }, MusicTitle + " by " + MusicArtist + ", heard as if from the hall's speakers."),
        R("MUSIC VOLUME", () => $"{Math.Round(_musicVol * 10)}", d => { _musicVol = Math.Clamp(_musicVol + 0.1f * d, 0, 1); ApplyOptions(true); }),
        R("SHADOWS", () => _sun.ShadowEnabled ? "ON" : "OFF", d => { _sun.ShadowEnabled = !_sun.ShadowEnabled; }),
        R("RESOLUTION", () => $"{_scales[_scaleIdx] * 100:0}%", d => { _scaleIdx = Cyc(_scaleIdx, d, _scales.Length); GetViewport().Scaling3DScale = _scales[_scaleIdx]; }, "Lower it if the frame rate is low."),
        R("FULL SCREEN", () => GetWindow().Mode == Window.ModeEnum.Fullscreen ? "ON" : "OFF", d => { var w = GetWindow(); w.Mode = w.Mode == Window.ModeEnum.Fullscreen ? Window.ModeEnum.Windowed : Window.ModeEnum.Fullscreen; }),
        R("RESET CAR TO THE START", null, d => { ResetCar(); }),
        R("QUIT", null, d => { GetTree().Quit(); }),
      };
      var keys = new List<Menu.Row>();
      foreach (var (k, what) in new[] { ("W / S  or  UP / DOWN", "drive; brake, then reverse"), ("A / D  or  LEFT / RIGHT", "steer"), ("R", "back to the start line"), ("C", "next camera"), ("ESC", "this menu"),
        ("F1", "next track"), ("K", "park the car in its bay"), ("ENTER", "run again, when the score card is up"), ("T   P   O   L", "body, colour, rims, bonnet"), ("B   V   G", "shell, windows, neon"), ("M   - / =", "mute, volume"), ("F2   F3   F4   F11", "shadows, frame rate, resolution, full screen"),
        ("GAMEPAD", "triggers drive and brake, left stick steers, Y reset, X camera, Start menu"), ("RC RADIO", "wheel steers, trigger drives, brakes and reverses: set it up on the CONTROLLER page") }) { string w = what; keys.Add(R(k, () => w, null)); }
      var ctl = new List<Menu.Row> {
        R("DRIVE WITH", () => _radio.On ? "RC RADIO" : "KEYS / GAMEPAD", d => { _radio.On = !_radio.On; ApplyOptions(true); }, "RC RADIO: a real pistol-grip transmitter plugged into the PC (its own USB port, or a simulator dongle on its receiver). Wheel and trigger act one to one, as on the car."),
        R("DEVICE", () => _radio.DeviceName, d => { _radio.NextDevice(d); }, "The radio as the PC sees it. Left and right step through what is plugged in."),
        R("FIND STEERING", () => _radio.Learning == 1 ? _radio.Status : $"CHANNEL {_radio.SteerAxis + 1}", d => { _radio.Learn(1); }, "Press right, then turn the wheel: the channel that moves is taken as steering."),
        R("FIND THROTTLE", () => _radio.Learning == 2 ? _radio.Status : $"CHANNEL {_radio.ThrAxis + 1}", d => { _radio.Learn(2); }, "Press right, then pull the trigger."),
        R("CALIBRATE", () => _radio.Learning == 3 ? _radio.Status : (_radio.Status.StartsWith("CALIB") || _radio.Status.StartsWith("NOTHING") || _radio.Status.StartsWith("PLUG") ? _radio.Status : "START"), d => { _radio.Learn(3); }, "Hands off the wheel and trigger, press right, then move both to their ends a few times within six seconds. Centre and end points are remembered."),
        R("REVERSE STEERING", () => _radio.SteerRev ? "ON" : "OFF", d => { _radio.SteerRev = !_radio.SteerRev; ApplyOptions(true); }, "If the car turns the wrong way."),
        R("REVERSE THROTTLE", () => _radio.ThrRev ? "ON" : "OFF", d => { _radio.ThrRev = !_radio.ThrRev; ApplyOptions(true); }, "If pulling the trigger brakes."),
        R("NOW", () => !_radio.On ? "-" : $"STEER {_radio.Steer * 100:+0;-0;0}%    TRIGGER {_radio.Trigger * 100:+0;-0;0}%", null, "What the game reads from the radio at this moment: 0 at rest, 100 at the ends."),
        R("DRIVING ASSIST", () => _assist ? "ON" : "OFF", d => { _assist = !_assist; ApplySetup(); ApplyOptions(true); }, "A driving aid, not part of the car's setup. On a key: holds the wheelspin for a drift. On a trigger: eases the throttle past the angle the gyro can catch."),
        R("TRANSMITTER ON SCREEN", () => _txOn ? "ON" : "OFF", d => { _txOn = !_txOn; ApplyOptions(true); }, "A transmitter in the corner whose wheel and trigger move with your inputs, to train the hands. It works with the keys and a gamepad too, with no radio plugged in."),
      };
      var comp = new List<Menu.Row> {
        R("JUDGED RUN", () => "START", d => { _pause.Open(false); StartSolo(); }, "One judged lap from the start line, scored like solo qualifying. Line 40: nose to the cones, tail to the yellow dashes, on a natural line (a late swerve at a cone halves it). Angle 40: depth and holding it steady. Style 20: quick clean transitions, staying on the throttle, an early sharp initiation; up to 5 bonus for initiating before the first bend's line. Deductions for straightening up and wall taps; a spin or a stop scores zero."),
        R("GHOST", () => _ghostOn ? "ON" : "OFF", d => { _ghostOn = !_ghostOn; ApplyOptions(true); }, "A see-through copy of your own car to drive against. In free practice it replays your fastest lap, from the line. In a judged run it replays your highest-scoring run. Kept for each track between sessions."),
        R("YOUR BEST", () => (_ghostLap != null ? "LAP " + T(_ghostLap.Mark) : "LAP -") + "     " + (_ghostRun != null ? $"RUN {_ghostRun.Mark:0.0}" : "RUN -"), null, "What the two ghosts on this track are: your fastest lap and your best judged run."),
        R("CLEAR GHOSTS", null, d => { ClearGhosts(); }, "Forget both recordings for this track and start again."),
        R("BACK TO FREE PRACTICE", () => _comp == 0 ? "-" : "STOP", d => { StopComp(); ResetCar(); }),
        R("LAST RESULT", () => _lastResult, null),
        R("PARK THE CAR", null, d => { _pause.Open(false); Park(); }, "Takes the car off the track into the marked bay by the driver's stand. R puts it back on the start line."),
      };
      _pause.Pages.Add(("CAR", car)); _pause.Pages.Add(("SETUP", setup)); _pause.Pages.Add(("TRAINING", comp)); _pause.Pages.Add(("CONTROLLER", ctl)); _pause.Pages.Add(("GAME", game)); _pause.Pages.Add(("KEYS", keys));
    }

    void ApplyOptions(bool save = false) {
      _view.SetPaint(Paints[_paintIdx % Paints.Length].col, _livery == 1); { var rm = Rims[_rimIdx % Rims.Length]; _view.SetRims(rm.col, rm.metal); }
      { var bd = CarView.Bodies[_body % CarView.Bodies.Length]; _car.SetBody(bd.kg, bd.cgUp, _view.HalfW, _view.HalfL); }
      _view.SetCamber((float)CamberFDeg[_setup[1]], (float)CamberRDeg[_setup[2]]); _car.H += (RideMm - 7) * 0.001; _view.RideOffset = (float)((RideMm - 7) * 0.001);
      _view.SetShell(_shellOn); _view.SetNeon(_neonIdx); _view.SetWindows(_clearGlass);
      if (save && _shot == null) { var cf = new ConfigFile(); cf.SetValue("game", "track", TrackIdx); for (int k = 0; k < _setup.Length; k++) cf.SetValue("setup4", SetupKeys[k], _setup[k]); cf.SetValue("game", "assist", _assist); if (_audio != null) { cf.SetValue("game", "volume", _audio.Volume); cf.SetValue("game", "music", _musicOn); cf.SetValue("game", "music_volume", _musicVol); cf.SetValue("game", "motor_sound", _audio.Motor); } _radio.Save(cf); cf.SetValue("game", "transmitter", _txOn); cf.SetValue("game", "ghost", _ghostOn); cf.SetValue("car", "paint", _paintIdx); cf.SetValue("car", "rims", _rimIdx); cf.SetValue("car", "bonnet", _livery); cf.SetValue("car", "body", _body); cf.SetValue("car", "shell", _shellOn); cf.SetValue("car", "neon", _neonIdx); cf.SetValue("car", "clear_windows", _clearGlass); cf.Save("user://settings.cfg"); }
    }
    void LoadOptions() {
      var cf = new ConfigFile(); if (cf.Load("user://settings.cfg") != Error.Ok) return;
      if (!_trackChosen) TrackIdx = Math.Clamp((int)cf.GetValue("game", "track", 0), 0, Tracks.Length - 1);
      for (int k = 0; k < _setup.Length; k++) _setup[k] = Math.Clamp((int)cf.GetValue("setup4", SetupKeys[k], SetupDefault[k]), 0, SetupNames[k].Length - 1);
      _vol0 = (float)(double)cf.GetValue("game", "volume", 0.4); _musicOn = (bool)cf.GetValue("game", "music", true); _musicVol = (float)(double)cf.GetValue("game", "music_volume", 0.5); _radio.Load(cf); _txOn = (bool)cf.GetValue("game", "transmitter", true); _assist = (bool)cf.GetValue("game", "assist", true); _ghostOn = (bool)cf.GetValue("game", "ghost", true); _motor0 = (bool)cf.GetValue("game", "motor_sound", true);
      _paintIdx = (int)cf.GetValue("car", "paint", 0); _rimIdx = Math.Clamp((int)cf.GetValue("car", "rims", 0), 0, Rims.Length - 1); _livery = Math.Clamp((int)cf.GetValue("car", "bonnet", 0), 0, 1); _body = Math.Clamp((int)cf.GetValue("car", "body", 0), 0, CarView.Bodies.Length - 1); _shellOn = (bool)cf.GetValue("car", "shell", true); _neonIdx = (int)cf.GetValue("car", "neon", 0); _clearGlass = (bool)cf.GetValue("car", "clear_windows", false);
      _paintIdx = Math.Clamp(_paintIdx, 0, Paints.Length - 1); _neonIdx = Math.Clamp(_neonIdx, 0, CarView.Neons.Length - 1);
    }
    float _vol0 = 0.4f; /* the browser version's 4 out of 10 */ bool _motor0 = true; double _sayT; string _sayText = "";
    void Say(string s) { _sayText = s; _sayT = 1.6; }

    void ResetCar() { _parked = false; _recOn = false; _ghostPlay = false; if (_comp != 0) { Grid(); return; } _car.Place(_t.Start[0], _t.Start[1], _t.Start[2]); _timing = false; _lap = 0; _next = 1; _camSnap = true; }

    public override void _UnhandledInput(InputEvent e) {
      bool key(Key k) => e is InputEventKey ek && ek.Pressed && !ek.Echo && ek.Keycode == k;
      bool pad(JoyButton b) => !_radio.On && e is InputEventJoypadButton jb && jb.Pressed && jb.ButtonIndex == b;   // (a radio's switches are not a gamepad's buttons)
      if (key(Key.Escape) || pad(JoyButton.Start)) { _pause.Open(!_pause.Visible); _help.Visible = !_pause.Visible && !Quiet; return; }
      if (_pause.Visible) { _pause.Handle(e); return; } // the menu has the keys while it is open
      if (false) { }
      else if (_comp != 0 && _phase == 2 && (key(Key.Enter) || key(Key.Space) || pad(JoyButton.A))) NextRun();
      else if (key(Key.R) || pad(JoyButton.Y)) ResetCar();
      else if (key(Key.K)) Park();
      else if (key(Key.C) || pad(JoyButton.X)) { _camMode = _camMode >= 4 ? 1 : _camMode + 1; _camSnap = true; }
      else if (key(Key.P)) { _paintIdx = (_paintIdx + 1) % Paints.Length; ApplyOptions(true); Say("COLOUR " + Paints[_paintIdx].name); }
      else if (key(Key.O)) { _rimIdx = (_rimIdx + 1) % Rims.Length; ApplyOptions(true); Say("RIMS " + Rims[_rimIdx].name); }
      else if (key(Key.L)) { _livery = 1 - _livery; ApplyOptions(true); Say("BONNET " + CarView.Bonnets[_livery]); }
      else if (key(Key.F1)) { TrackIdx = (TrackIdx + 1) % Tracks.Length; _trackChosen = true; ApplyOptions(true); GetTree().ReloadCurrentScene(); } // another track: the hall is built again
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
      if (!_radio.On && Input.GetConnectedJoypads().Count > 0) {
        int j = Input.GetConnectedJoypads()[0]; float gt = Input.GetJoyAxis(j, JoyAxis.TriggerRight), gb = Input.GetJoyAxis(j, JoyAxis.TriggerLeft), gs = Input.GetJoyAxis(j, JoyAxis.LeftX);
        if (gt > 0.03f && !up) { inp.Thr = gt; inp.ThrDigital = false; }
        if (gb > 0.03f && !dn) inp.Brk = gb;
        if (Math.Abs(gs) > 0.06f && !lf && !rt) inp.Steer = -Math.Sign(gs) * Math.Pow((Math.Abs(gs) - 0.06) / 0.94, 1.4);
      }
      if (_radio.On && !up && !dn && !lf && !rt) { inp = new CarInput(); _radio.Read(ref inp); }   // the radio drives; the keys still work if touched
      return inp;
    }

    public override void _Process(double delta) {
      if (!_pause.Visible) {
        var inp = ReadInput(); _acc += Math.Min(delta, 0.05); int idx0 = _t.Idx;
        while (_acc >= CarPhysics.DT) { StepWorld(inp); _acc -= CarPhysics.DT; }
        Laps(idx0, Math.Min(delta, 0.05));
      }
      if (!double.IsNaN(_poseSteer)) { _car.Steer = _poseSteer; _car.Roll = -0.035 * Math.Sign(_poseSteer); }
      { // the transmitter in the corner follows whatever is driving; the radio's learning steps show in the menu as they go
        if (_radio.Step(delta) || (_pause.Visible && _radio.On)) _pause.Refresh();
        var show = ReadInput(); var vs = GetViewport().GetVisibleRect().Size; _tx.Visible = _txOn && !_bare && _splash == null && (_shot == null || _txShot) && !(_fpvScreen.Visible && vs.X < 900);
        _tx.Position = new Vector2(vs.X - _tx.Size.X - 14, vs.Y - _tx.Size.Y - 44); _tx.Show(show.Steer, show.Thr, show.Brk, Input.GetConnectedJoypads().Count > 0 || _radio.Fake != null); }
      StepSplash(delta); StepMusic(delta);
      StepCompetition(delta); _view.Pose(_car, _t); UpdateCamera((float)delta); UpdateHud();
      { // sound is always heard from where the pilot stands, facing the track: the cameras on the car have no microphone
        var earXf = new Transform3D(Basis.Identity, DriverEye).LookingAt(new Vector3(0, 0.5f, 0), Vector3.Up); // the driver's place: the same corner as the camera, facing the middle of the hall
        _audio.Step(_car, _view.GlobalTransform, earXf, _pause.Visible, (float)Math.Min(delta, 0.05)); }
      if (_shot != null && ++_frame >= _frames) { GetViewport().GetTexture().GetImage().SavePng(_shot); GD.Print($"shot {_shot} car {_car.X:F2} {_car.Z:F2} y {_car.Y:F3} speed {Math.Sqrt(_car.U * _car.U + _car.V * _car.V):F2} beta {_car.Beta:F2} fps {Engine.GetFramesPerSecond()}"); GetTree().Quit(); }
    }

    // a lap counts when the car passes the three checkpoints in order and then the line
    void Laps(int idx0, double dt) {
      int n = _t.N, i = _t.Idx; if (_timing) _lap += dt;
      int q = i * 4 / n; // which quarter of the lap
      if (q == _next && _next != 0) _next = (_next + 1) % 4;
      bool crossed = idx0 > n * 7 / 8 && i < n / 8;
      if (crossed) { bool fin = _timing && _next == 0; double lt = _lap; if (fin) { _last = _lap; if (_best == 0 || _lap < _best) _best = _lap; } _timing = true; _lap = 0; _next = 1; LapCrossed(fin, lt); }
    }

    void UpdateCamera(float dt) {
      var p = new Vector3((float)_car.X, (float)_car.Y, (float)_car.Z); Vector3 pos, look; float fov = 58;
      if (_camMode != 2 && _camMode != 3 && _camMode != 4) { OnBoard(false); _view.ShowStick(false); }
      float hx = (float)_t.HallX - 0.15f, hz = (float)_t.HallZ - 0.15f;
      _view.SetShell(_shellOn && !(_pause.Visible && _pause.Page == "SETUP"));   // on the SETUP page the shell comes off, as on a setup stand: the wheels, arms and shocks are what is being set
      if (_pause.Visible && _pause.Preview && _camMode != 8) { // the CAR page: the car itself, still, on the clear right half of the screen, so each choice shows as it is made
        OnBoard(false); _view.ShowStick(false); bool su = _pause.Page == "SETUP"; float az = Mathf.DegToRad(su ? 20 : 42) + (float)_car.Hd, el = Mathf.DegToRad(su ? 7 : 15);   // SETUP: lower and more from the front, where camber, toe and ride height show
        var dir = new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el)); var side = dir.Cross(Vector3.Up).Normalized();
        look = p + new Vector3(0, 0.05f, 0) + side * 0.2f; pos = look + dir * 0.82f; _camSnap = true; _cam.Fov = 34; _cam.Position = pos; _cam.LookAt(look, Vector3.Up); return; }
      if (_camMode == 8) { // test view round the car: degrees round from its nose, degrees up, distance, lens
        float az = Mathf.DegToRad(_view6[0]) + (float)_car.Hd, el = Mathf.DegToRad(_view6[1]), d = _view6[2];
        look = p + new Vector3(0, 0.055f, 0); pos = look + new Vector3(Mathf.Sin(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Cos(az) * Mathf.Cos(el)) * d; fov = _view6[3]; _camSnap = true;
        _cam.Fov = fov; _cam.Position = pos; _cam.LookAt(look, Vector3.Up); return; }
      if (_camMode == 9) { pos = new Vector3(_view6[0], _view6[1], _view6[2]); look = new Vector3(_view6[3], _view6[4], _view6[5]); fov = _view6.Length > 6 ? _view6[6] : 50; _camSnap = true; } // test view
      else if (_camMode == 1) { // the driver's stand: where you would stand with the transmitter
        // Like a person standing there. The eyes do not zoom: the field of view is a steady, natural one, and only narrows a
        // little when the car is far off (attention, not a lens: a car twice as far still looks a good deal smaller).
        // The gaze runs ahead of the car to where it will be in about half a second, so the track coming up is in view,
        // and it follows smoothly, the way eyes and head track something moving.
        // (the lead eases to its new place over about a third of a second: when the car hits a barrier its speed changes in an
        // instant, and the gaze must not jump with it. Only a trace of the knock shows.)
        var aheadNow = (new Vector3((float)_car.Vx, 0, (float)_car.Vz) * 0.5f).LimitLength(1.0f); _aheadSm = _aheadSm.Lerp(aheadNow, _camSnap || dt <= 0 ? 1 : 1 - Mathf.Exp(-dt * 3f)); var ahead = _aheadSm;
        pos = DriverEye; look = p + new Vector3(0, 0.04f, 0) + ahead; float dEye = pos.DistanceTo(look);
        _eyeFov += (Mathf.Clamp(58f * Mathf.Pow(2.5f / dEye, 0.4f), 34, 58) - _eyeFov) * (_camSnap || dt <= 0 ? 1 : 1 - Mathf.Exp(-dt * 3)); fov = _eyeFov;
      } else if (_camMode == 2) { // from the ceiling: an analog camera too, fixed up there looking straight down
        OnBoard(true); _view.SetOnBoard(false); _view.ShowStick(false); _fpvCam.Fov = 100; _fpvCam.Position = new Vector3(0, Hall.CeilY - 0.05f, 0.01f - (float)Hall.Back / 2); _fpvCam.LookAt(new Vector3(0, 0, -(float)Hall.Back / 2), Vector3.Forward);   /* over the middle of the layout, not of the hall */ _camSnap = true; return; }
      else if (_camMode == 3) { // on board: the camera rides in the cabin and leans with the body
        OnBoard(true); _view.ShowStick(false); _fpvCam.Fov = 75; _fpvCam.GlobalTransform = _view.Fpv.GlobalTransform; _camSnap = true; return;
      }
      else if (_camMode == 4) { // the stick camera: fixed to the roof on a short stick, behind and above the car, looking forward
        OnBoard(true); _view.SetOnBoard(false); _view.ShowStick(true); _fpvCam.Fov = 75; _fpvCam.GlobalTransform = _view.StickCam.GlobalTransform; _camSnap = true; return;
      } else { pos = new Vector3(0, 1.45f, hz); look = p; }
      pos.X = Mathf.Clamp(pos.X, -hx, hx); pos.Z = Mathf.Clamp(pos.Z, -hz, hz);
      float k = _camSnap || dt <= 0 ? 1 : 1 - Mathf.Exp(-dt * 12);
      _camPos = _camPos.Lerp(pos, k); _camLook = _camLook.Lerp(look, _camSnap || dt <= 0 ? 1 : 1 - Mathf.Exp(-dt * (_camMode == 1 ? 9 : 14))); _camSnap = false;
      _cam.Fov = fov; _cam.Position = _camPos;
      if (_camMode == 2) _cam.LookAt(_camLook, Vector3.Forward); else _cam.LookAt(_camLook, Vector3.Up);
    }

    static string T(double s) => s <= 0 ? "--:--.--" : $"{(int)(s / 60):00}:{s % 60:00.00}";
    void UpdateHud() {
      double sp = Math.Sqrt(_car.U * _car.U + _car.V * _car.V);
      _hud.Text = $"{_t.Name}\nLAP   {T(_timing ? _lap : 0)}\nLAST  {T(_last)}\nBEST  {T(_best)}\n{sp * 36:0} km/h scale   ANGLE {Math.Abs(_car.Beta) * 180 / Math.PI:0}°";
      _hud.Visible = !_pause.Visible && !_bare && _splash == null; if (_splash != null) _help.Visible = false; else if (!_pause.Visible && !Quiet) _help.Visible = true;
      { var vs = GetViewport().GetVisibleRect().Size; _help.SetAnchorsPreset(Control.LayoutPreset.TopLeft); _help.Position = new Vector2(24, vs.Y - 40); _help.Size = new Vector2(vs.X - 48, 24); _help.ClipText = true; } // along the bottom edge, never wider than the window
      _hud.Text += CompHud();
      if (_sayT > 0) { _sayT -= GetProcessDeltaTime(); _hud.Text += "\n\n" + _sayText; }
      if (_stats.Visible) _stats.Text = $"{Engine.GetFramesPerSecond()} fps   draw calls {Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame):0}   tris {Performance.GetMonitor(Performance.Monitor.RenderTotalPrimitivesInFrame):0}\nresolution {_scales[_scaleIdx] * 100:0}%   shadows {(_sun.ShadowEnabled ? "on" : "off")}   {RenderingServer.GetVideoAdapterName()}";
    }
  }
}
