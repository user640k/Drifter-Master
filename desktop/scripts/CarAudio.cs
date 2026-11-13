// The car's sound, cut from a reference recording of a 1/10 drift car on a polished floor (tools/make_sounds.py):
// mostly the tyres, a broad roar that comes up as they slide.
//  - roar, roll, hiss: textures cut from the recording: sliding, rolling, wheelspin
//  - whine: the motor, a thin whistle that glides with wheel speed, well under the tyres as in the reference
//  - shell: the thin body buzzing and ticking on the chassis, louder the more the car shakes and leans
//  - tick: each hard tyre clicking over each tile joint (a 304.8 mm grid), so the patter speeds up with the car
//  - hit: into a barrier, or landing
// Everything is placed at the car and heard from where the pilot stands, whichever camera is showing: the cameras on
// the car carry no microphone. So the car gets a little quieter and duller as it drives away (less than outdoors:
// the hall carries it), and moves left and right.
using Godot;
using System;

namespace DriftMaster {
  public partial class CarAudio : Node3D {
    AudioStreamPlayer3D _roll, _slide, _scrub, _shell, _tick, _hit, _whine; float _gWhine; AudioListener3D _ear;
    /// The motor's note. A sensored brushless motor of this class has a two-pole rotor and three coils: its torque pulses
    /// six times a turn, and that is the whine heard from the car. The motor's speed is the wheels' speed as a share of
    /// the car's free-running speed, times the motor's own free speed (its kV at 7.4 V). So a 13.5T at 2.5 m/s on the
    /// drift gearing sings at about 550 Hz, a 10.5T at about 650 Hz, and both rise in step with the wheels.
    public double MotorVFree = 10.5, MotorFreeRev = 382; public bool WhineOnly = true;
    float _gRoll, _gSlide, _gScrub, _gShell; bool _started; int _bus; readonly int[] _cellX = new int[4], _cellZ = new int[4]; bool _cells; double _tickWait; float _pendHit;
    public float Volume = 0.8f; public bool Muted, Motor = true; // Motor: the whistle can be switched off (N)
    public static string Solo; // test: hear one layer only
    const float TileS = 0.3048f;

    static AudioStreamWav Load(string name, bool loop) {
      var s = (AudioStreamWav)GD.Load<AudioStream>("res://assets/snd/" + name + ".wav").Duplicate();
      if (loop) { s.LoopMode = AudioStreamWav.LoopModeEnum.Forward; s.LoopBegin = 0; s.LoopEnd = (int)(s.GetLength() * s.MixRate); }
      return s;
    }
    AudioStreamPlayer3D Player(string name, bool loop, int poly = 1) {
      var p = new AudioStreamPlayer3D { Stream = Load(name, loop), Bus = "Car", MaxDb = 6, AttenuationModel = AudioStreamPlayer3D.AttenuationModelEnum.Disabled, AttenuationFilterCutoffHz = 20500, PanningStrength = 0.8f, MaxPolyphony = poly, VolumeDb = -80 };
      AddChild(p); return p; // loops start on the first Step, once they are in place and turned down: no pop at launch
    }

    public override void _Ready() {
      // the room: only a touch. The recorded tyre sounds already carry the room they were recorded in, so more echo on top
      // would be a room inside a room; this only ties the made sounds (shell, ticks, hits, motor) to the same space
      _bus = AudioServer.GetBusIndex("Car"); // (still there after a change of track)
      if (_bus < 0) { _bus = AudioServer.BusCount; AudioServer.AddBus(_bus); AudioServer.SetBusName(_bus, "Car"); AudioServer.SetBusSend(_bus, "Master"); } else while (AudioServer.GetBusEffectCount(_bus) > 0) AudioServer.RemoveBusEffect(_bus, 0);
      AudioServer.AddBusEffect(_bus, new AudioEffectReverb { RoomSize = 0.3f, Damping = 0.5f, Spread = 0.8f, Hipass = 0.2f, PredelayMsec = 10, PredelayFeedback = 0.1f, Dry = 1f, Wet = 0.08f }, 0);
      AudioServer.AddBusEffect(_bus, new AudioEffectLimiter { CeilingDb = -1, ThresholdDb = -6 }, 1);
      _roll = Player("roll", true); _slide = Player("roar", true); _scrub = Player("hiss", true); _shell = Player("shell", true); _whine = Player("whine", true);
      _tick = Player("tick", false, 10); _hit = Player("hit", false, 3);
      _ear = new AudioListener3D(); AddChild(_ear); _ear.TopLevel = true; _ear.MakeCurrent();
    }

    public void Bump(double k) { _pendHit = Math.Max(_pendHit, (float)k); }
    static float Db(float g) => g <= 0.0005f ? -80 : Mathf.LinearToDb(g);
    static float Smooth(float a, float b, float x) { float t = Mathf.Clamp((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); }

    // ear: where the pilot stands (the same whichever camera is showing); paused: everything fades out
    public void Step(CarPhysics c, Transform3D car, Transform3D ear, bool paused, float dt) {
      const bool inside = false; const float camGain = 1; // always heard from the pilot's place, never from inside the car
      GlobalPosition = car.Origin + new Vector3(0, 0.05f, 0); _ear.GlobalTransform = ear;
      // Distance, as measured on a video of a car on a P-tile track from a fixed camera: in a hall the car does not fade
      // the way it would outdoors. Much of what reaches you is the room, so nine times the distance was only a bit
      // over twice as quiet. Loudness goes as distance to the power -0.4 here (outdoors it would be -1). The far car was
      // also a little duller in the video; that is left out, as the engine's low-pass took far too much of the top off.
      float dist = Mathf.Max(0.6f, ear.Origin.DistanceTo(GlobalPosition)), far = Mathf.Pow(2f / dist, 0.4f);   /* (0.4 at first: with the second clip as the guide the car swells more as it comes close and falls away further off) */
      AudioServer.SetBusVolumeDb(_bus, Muted ? -80 : Db(Volume * Volume * camGain * far * 1.45f));
      float sp = (float)Math.Sqrt(c.U * c.U + c.V * c.V), slide = (float)c.Slide, spin = (float)Math.Abs(c.W - c.U), on = paused || c.Air ? 0 : 1;
      float moving = Smooth(0.08f, 1.6f, sp);
      // how much the car is shaking: speed over the tiles, sliding, and the body rocking on its springs
      float shake = Mathf.Clamp(0.1f * sp + 0.55f * slide + 5f * (float)Math.Abs(c.RollV) + 4f * (float)Math.Abs(c.PitchV) + 60f * (float)Math.Abs(c.HeaveV) + 25f * (float)Math.Abs(c.ShellV), 0, 1.4f);
      float k = 1 - Mathf.Exp(-dt / 0.07f);
      float grind = Mathf.Clamp(slide * 0.9f + spin * 0.12f, 0, 1); // how hard the tyres are sliding or spinning
      _gRoll += (on * moving * (0.3f + 0.08f * Mathf.Min(sp / 4, 1)) * (1 - 0.6f * grind) - _gRoll) * k;
      _gSlide += (on * Smooth(0.02f, 0.6f, sp + spin) * grind * 0.8f - _gSlide) * k;
      _gScrub += (on * Mathf.Clamp(spin * 0.06f, 0, 0.3f) * Smooth(0.02f, 0.5f, sp + spin) - _gScrub) * k;
      // the motor: heard when it is driving, a little when coasting; its pitch is the wheels' speed
      float wsp = (float)Math.Abs(c.W); _gWhine += (on * Smooth(0.15f, 1.2f, wsp) * (0.043f + 0.1f * (float)c.Thr) * (inside ? 0.3f : 1f) - _gWhine) * (1 - Mathf.Exp(-dt / 0.05f));
      _gShell += (on * shake * (inside ? 0.2f : 0.03f) - _gShell) * (1 - Mathf.Exp(-dt / 0.04f));
      if (WhineOnly) { _gRoll = _gSlide = _gScrub = _gShell = 0; }   // the sound is being rebuilt one layer at a time, starting with the motor: the tyre, shell and tile layers are silent for now
      if (Solo != null) { if (Solo != "roll") _gRoll = 0; if (Solo != "slide") _gSlide = 0; if (Solo != "scrub") _gScrub = 0; if (Solo != "shell") _gShell = 0; if (Solo != "whine") _gWhine = 0; }
      _roll.VolumeDb = Db(_gRoll); _slide.VolumeDb = Db(_gSlide); _scrub.VolumeDb = Db(_gScrub); _shell.VolumeDb = Db(_gShell); _whine.VolumeDb = Db(Motor ? _gWhine : 0); _whine.PitchScale = Mathf.Clamp((float)(6 * wsp / MotorVFree * MotorFreeRev) / 1000f, 0.22f, 3.2f);
      // tyre noise rises a little with speed (it is the floor going by, not a motor winding up)
      _roll.PitchScale = 0.86f + 0.07f * Mathf.Min(sp, 4.5f); _slide.PitchScale = 0.94f + 0.03f * Mathf.Min(sp, 4.5f); _scrub.PitchScale = 0.95f + 0.04f * Mathf.Min(spin, 5);
      _shell.PitchScale = 0.97f + 0.05f * Mathf.Min(shake, 1);
      if (!_started) { _started = true; foreach (var p in new[] { _roll, _slide, _scrub, _shell, _whine }) p.Play((float)GD.RandRange(0.0, 3.0)); }
      // tile joints under each wheel
      _tickWait -= dt; double sh = Math.Sin(c.Hd), ch = Math.Cos(c.Hd); bool crossed = false;
      for (int i = 0; i < 4; i++) {
        double d = i < 2 ? 0.1285 : -0.1285, s = (i % 2 == 0 ? 1 : -1) * 0.0845, x = c.X + sh * d + ch * s, z = c.Z + ch * d - sh * s;
        int cx = (int)Math.Floor(x / TileS), cz = (int)Math.Floor(z / TileS);
        if (_cells && (cx != _cellX[i] || cz != _cellZ[i])) crossed = true; _cellX[i] = cx; _cellZ[i] = cz;
      }
      _cells = true;
      if (!WhineOnly && crossed && on > 0 && sp > 0.25f && _tickWait <= 0 && (Solo == null || Solo == "tick")) {
        _tickWait = 0.03; _tick.VolumeDb = Db(Mathf.Clamp(0.1f + 0.09f * sp, 0, 0.42f) * (inside ? 1.4f : 1f) * (float)GD.RandRange(0.6, 1.0)); _tick.PitchScale = (float)GD.RandRange(0.85, 1.2); _tick.Play();
      }
      if (_pendHit > 0) { if (!paused && (Solo == null || Solo == "hit")) { _hit.VolumeDb = Db(0.25f + 0.75f * Mathf.Min(1, _pendHit)); _hit.PitchScale = (float)GD.RandRange(0.9, 1.1) * (1.1f - 0.2f * Mathf.Min(1, _pendHit)); _hit.Play(); } _pendHit = 0; }
    }
  }
}
