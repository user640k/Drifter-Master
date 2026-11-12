// The car's sound. No motor whine: what you hear is the car on the floor and in the room.
//  - roll, slide, scrub: the web version's first recorded loops with the motor taken out (tools/make_sounds.py);
//    they come up with speed and with how hard the tyres are sliding, and barely change pitch
//  - shell: the thin body buzzing and ticking on the chassis, louder the more the car shakes and leans
//  - tick: each hard tyre clicking over each tile joint (a 304.8 mm grid), so the patter speeds up with the car
//  - hit: into a barrier, or landing
// Everything is placed at the car and heard from the camera, through a "Car" bus that holds the room: early echoes off
// the tiles and hard walls and a short tail. From inside the car the outside is muffled and the shell is close.
using Godot;
using System;

namespace DriftMaster {
  public partial class CarAudio : Node3D {
    AudioStreamPlayer3D _roll, _slide, _scrub, _shell, _tick, _hit; AudioListener3D _ear;
    float _gRoll, _gSlide, _gScrub, _gShell; bool _started; int _bus; readonly int[] _cellX = new int[4], _cellZ = new int[4]; bool _cells; double _tickWait; float _pendHit;
    public float Volume = 0.8f; public bool Muted; public static string Solo; // test: hear one layer only
    const float TileS = 0.3048f;

    static AudioStreamWav Load(string name, bool loop) {
      var s = (AudioStreamWav)GD.Load<AudioStream>("res://assets/snd/" + name + ".wav").Duplicate();
      if (loop) { s.LoopMode = AudioStreamWav.LoopModeEnum.Forward; s.LoopBegin = 0; s.LoopEnd = (int)(s.GetLength() * s.MixRate); }
      return s;
    }
    AudioStreamPlayer3D Player(string name, bool loop, int poly = 1) {
      var p = new AudioStreamPlayer3D { Stream = Load(name, loop), Bus = "Car", UnitSize = 1.6f, MaxDb = 3, AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.InverseDistance, AttenuationFilterCutoffHz = 9000, AttenuationFilterDb = -12, PanningStrength = 0.8f, MaxPolyphony = poly, VolumeDb = -80 };
      AddChild(p); return p; // loops start on the first Step, once they are in place and turned down: no pop at launch
    }

    public override void _Ready() {
      // the room: a 9 x 5 m hall with a tiled floor and bare walls. Not much absorbs, so there are clear early echoes and
      // about three quarters of a second of tail
      _bus = AudioServer.BusCount; AudioServer.AddBus(_bus); AudioServer.SetBusName(_bus, "Car"); AudioServer.SetBusSend(_bus, "Master");
      AudioServer.AddBusEffect(_bus, new AudioEffectLowPassFilter { CutoffHz = 1900, Resonance = 0.6f }, 0); AudioServer.SetBusEffectEnabled(_bus, 0, false); // on only from inside the car
      AudioServer.AddBusEffect(_bus, new AudioEffectReverb { RoomSize = 0.5f, Damping = 0.32f, Spread = 0.9f, Hipass = 0.12f, PredelayMsec = 16, PredelayFeedback = 0.25f, Dry = 1f, Wet = 0.24f }, 1);
      AudioServer.AddBusEffect(_bus, new AudioEffectLimiter { CeilingDb = -1, ThresholdDb = -6 }, 2);
      _roll = Player("roll", true); _slide = Player("slide", true); _scrub = Player("scrub", true); _shell = Player("shell", true);
      _tick = Player("tick", false, 10); _hit = Player("hit", false, 3);
      _ear = new AudioListener3D(); AddChild(_ear); _ear.TopLevel = true; _ear.MakeCurrent();
    }

    public void Bump(double k) { _pendHit = Math.Max(_pendHit, (float)k); }
    static float Db(float g) => g <= 0.0005f ? -80 : Mathf.LinearToDb(g);
    static float Smooth(float a, float b, float x) { float t = Mathf.Clamp((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); }

    // camGain evens out the cameras: the on-board and stick cameras sit much closer to the car than the others
    // ear: where the camera is; inside: the on-board camera (under the shell); paused: everything fades out
    public void Step(CarPhysics c, Transform3D car, Transform3D ear, bool inside, bool paused, float dt, float camGain = 1) {
      GlobalPosition = car.Origin + new Vector3(0, 0.05f, 0); _ear.GlobalTransform = ear;
      AudioServer.SetBusEffectEnabled(_bus, 0, inside);
      AudioServer.SetBusVolumeDb(_bus, Muted ? -80 : Db(Volume * Volume * camGain));
      float sp = (float)Math.Sqrt(c.U * c.U + c.V * c.V), slide = (float)c.Slide, spin = (float)Math.Abs(c.W - c.U), on = paused || c.Air ? 0 : 1;
      float moving = Smooth(0.08f, 1.6f, sp);
      // how much the car is shaking: speed over the tiles, sliding, and the body rocking on its springs
      float shake = Mathf.Clamp(0.12f * sp + 0.55f * slide + 5f * (float)Math.Abs(c.RollV) + 4f * (float)Math.Abs(c.PitchV) + 60f * (float)Math.Abs(c.HeaveV), 0, 1.4f);
      float k = 1 - Mathf.Exp(-dt / 0.07f);
      _gRoll += (on * moving * (0.34f + 0.1f * Mathf.Min(sp / 4, 1)) * (1 - 0.5f * Mathf.Clamp(slide, 0, 1)) - _gRoll) * k;
      _gSlide += (on * moving * Mathf.Clamp(slide * 1.1f, 0, 1) * 0.55f - _gSlide) * k;
      _gScrub += (on * Mathf.Clamp(slide * 0.45f + spin * 0.07f, 0, 0.6f) * Smooth(0.02f, 0.5f, sp + spin) - _gScrub) * k;
      _gShell += (on * shake * (inside ? 0.75f : 0.4f) - _gShell) * (1 - Mathf.Exp(-dt / 0.04f));
      if (Solo != null) { if (Solo != "roll") _gRoll = 0; if (Solo != "slide") _gSlide = 0; if (Solo != "scrub") _gScrub = 0; if (Solo != "shell") _gShell = 0; }
      _roll.VolumeDb = Db(_gRoll); _slide.VolumeDb = Db(_gSlide); _scrub.VolumeDb = Db(_gScrub); _shell.VolumeDb = Db(_gShell);
      // tyre noise rises a little with speed (it is the floor going by, not a motor winding up)
      _roll.PitchScale = 0.86f + 0.07f * Mathf.Min(sp, 4.5f); _slide.PitchScale = 0.94f + 0.03f * Mathf.Min(sp, 4.5f); _scrub.PitchScale = 0.95f + 0.04f * Mathf.Min(spin, 5);
      _shell.PitchScale = 0.97f + 0.05f * Mathf.Min(shake, 1);
      if (!_started) { _started = true; foreach (var p in new[] { _roll, _slide, _scrub, _shell }) p.Play((float)GD.RandRange(0.0, 3.0)); }
      // tile joints under each wheel
      _tickWait -= dt; double sh = Math.Sin(c.Hd), ch = Math.Cos(c.Hd); bool crossed = false;
      for (int i = 0; i < 4; i++) {
        double d = i < 2 ? 0.1285 : -0.1285, s = (i % 2 == 0 ? 1 : -1) * 0.0845, x = c.X + sh * d + ch * s, z = c.Z + ch * d - sh * s;
        int cx = (int)Math.Floor(x / TileS), cz = (int)Math.Floor(z / TileS);
        if (_cells && (cx != _cellX[i] || cz != _cellZ[i])) crossed = true; _cellX[i] = cx; _cellZ[i] = cz;
      }
      _cells = true;
      if (crossed && on > 0 && sp > 0.25f && _tickWait <= 0 && (Solo == null || Solo == "tick")) {
        _tickWait = 0.03; _tick.VolumeDb = Db(Mathf.Clamp(0.18f + 0.15f * sp, 0, 0.7f) * (inside ? 1.4f : 1f) * (float)GD.RandRange(0.6, 1.0)); _tick.PitchScale = (float)GD.RandRange(0.85, 1.2); _tick.Play();
      }
      if (_pendHit > 0) { if (!paused && (Solo == null || Solo == "hit")) { _hit.VolumeDb = Db(0.25f + 0.75f * Mathf.Min(1, _pendHit)); _hit.PitchScale = (float)GD.RandRange(0.9, 1.1) * (1.1f - 0.2f * Mathf.Min(1, _pendHit)); _hit.Play(); } _pendHit = 0; }
    }
  }
}
